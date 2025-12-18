using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float massKg;
    public float dragCoefficient;
    public float frontalArea;

    public float lifeTime = 5f;
    public LayerMask hitMask;

    private Vector3 velocity;
    private float aliveTime;
    private int shotId = -1;

    public void Initialize(AmmoData ammo, Vector3 initialVelocity, LayerMask mask, int shotId = -1)
    {
        if (ammo != null)
        {
            massKg = ammo.MassKg;
            dragCoefficient = ammo.DragCoefficient;
            frontalArea = ammo.FrontalArea;
        }

        velocity = initialVelocity;
        hitMask = (mask == 0) ? Physics.DefaultRaycastLayers : mask;
        aliveTime = 0f;
        this.shotId = shotId;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        Vector3 prevPos = transform.position;

        Vector3 drag = BallisticCalculator.CalculateDragForce(velocity, dragCoefficient, frontalArea);
        Vector3 accel = drag / Mathf.Max(0.00001f, massKg) + Physics.gravity;

        velocity += accel * dt;
        Vector3 nextPos = prevPos + velocity * dt;

        Vector3 dir = nextPos - prevPos;
        float dist = dir.magnitude;

        if (dist > 0f)
        {
            if (Physics.Raycast(prevPos, dir.normalized, out RaycastHit hit, dist, hitMask))
            {
                transform.position = hit.point;
                float speed = velocity.magnitude;
                float energy = 0.5f * Mathf.Max(0.00001f, massKg) * speed * speed;

                var target = hit.collider != null ? hit.collider.GetComponentInParent<ITarget>() : null;
                target?.OnHit(hit, energy, velocity);

                ShotManager.Instance?.RegisterHit(shotId, hit, energy);
                Destroy(gameObject);
                return;
            }
        }

        transform.position = nextPos;
        aliveTime += dt;
        if (aliveTime > lifeTime)
        {
            ShotManager.Instance?.RegisterMiss(shotId, transform.position);
            Destroy(gameObject);
        }
    }
}
