using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [Header("Ballistics (initialized from AmmoData)")]
    public float massKg;
    public float dragCoefficient;
    public float frontalArea;

    [Header("Simulation")]
    public float lifeTime = 5f;
    public LayerMask hitMask;

    private Vector3 velocity;
    private float aliveTime;

    public void Initialize(AmmoData ammo, Vector3 initialVelocity, LayerMask mask)
    {
        if (ammo != null)
        {
            massKg = ammo.MassKg;
            dragCoefficient = ammo.DragCoefficient;
            frontalArea = ammo.FrontalArea;
        }

        velocity = initialVelocity;
        hitMask = (mask == 0) ? Physics.DefaultRaycastLayers : mask; // <-- garantía
        aliveTime = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 prevPos = transform.position;

        Vector3 drag = BallisticCalculator.CalculateDragForce(velocity, dragCoefficient, frontalArea);
        Vector3 accel = drag / massKg + Physics.gravity;

        velocity += accel * dt;
        Vector3 nextPos = prevPos + velocity * dt;

        Vector3 dir = nextPos - prevPos;
        float dist = dir.magnitude;

        if (dist > 0f)
        {
            if (Physics.Raycast(prevPos, dir.normalized, out RaycastHit hit, dist, hitMask))
            {
                // move exactly to hit point (prevents visual overshoot)
                transform.position = hit.point;

                // compute energy
                float speed = velocity.magnitude;
                float energy = 0.5f * massKg * speed * speed;

                // notify target if implements ITarget
                var target = hit.collider != null ? hit.collider.GetComponentInParent<ITarget>() : null;
                target?.OnHit(hit, energy, velocity);

                // notify shot manager (stores hit record)
                ShotManager.Instance?.RegisterHit(hit, energy, velocity);

                Destroy(gameObject);
                return;
            }
        }

        transform.position = nextPos;

        aliveTime += dt;
        if (aliveTime > lifeTime)
            Destroy(gameObject);
    }
}
