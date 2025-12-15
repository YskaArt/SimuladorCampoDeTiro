using UnityEngine;

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
        hitMask = mask;
        aliveTime = 0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 prevPos = transform.position;

        // forces
        Vector3 drag = BallisticCalculator.CalculateDragForce(velocity, dragCoefficient, frontalArea);
        Vector3 accel = drag / massKg + Physics.gravity;

        velocity += accel * dt;
        Vector3 nextPos = prevPos + velocity * dt;

        // raycast for collision
        Vector3 dir = nextPos - prevPos;
        float dist = dir.magnitude;

        if (dist > 0f)
        {
            if (Physics.Raycast(prevPos, dir.normalized, out RaycastHit hit, dist, hitMask))
            {
                // move exactly to hit point
                transform.position = hit.point;

                // notify manager so it places marker / records hit
                ShotManager.Instance?.RegisterHit(hit);

                // destroy this projectile
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
