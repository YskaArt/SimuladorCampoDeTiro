using System.Collections;
using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance { get; private set; }

    [SerializeField] private GameObject tracerPrefab; // visual prefab (small bullet/tracer)
    [SerializeField] private float simulationStep = 0.01f; // seconds per sim step
    [SerializeField] private float maxSimTime = 10f;
    [SerializeField] private LayerMask defaultHitMask = ~0;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void SimulateShot(BallisticShot shot, Transform muzzle, LayerMask hitMask)
    {
        StartCoroutine(SimulateShotCoroutine(shot, muzzle, hitMask));
    }

    private IEnumerator SimulateShotCoroutine(BallisticShot shot, Transform muzzle, LayerMask hitMask)
    {
        float t = 0f;
        Vector3 pos = shot.position;
        Vector3 vel = shot.velocity;

        GameObject tracer = null;
        if (tracerPrefab != null)
        {
            tracer = Instantiate(tracerPrefab, pos, Quaternion.LookRotation(vel));
            // optional: make tracer kinematic visual only
            Rigidbody trb = tracer.GetComponent<Rigidbody>();
            if (trb != null) trb.isKinematic = true;
        }

        while (t < maxSimTime)
        {
            Vector3 Fd = BallisticCalculator.CalculateDragForce(vel, shot.dragCoefficient, shot.area);
            Vector3 a = Fd / shot.mass + Physics.gravity;

            // semi-implicit Euler
            vel += a * simulationStep;
            Vector3 nextPos = pos + vel * simulationStep;

            Vector3 dir = nextPos - pos;
            float dist = dir.magnitude;
            if (dist > 0f)
            {
                RaycastHit hit;
                if (Physics.Raycast(pos, dir.normalized, out hit, dist, hitMask == 0 ? defaultHitMask : hitMask))
                {
                    if (tracer != null) tracer.transform.position = hit.point;
                    // impact effects (spawn decals/effects) can go here
                    yield break;
                }
            }

            pos = nextPos;
            if (tracer != null)
            {
                tracer.transform.position = pos;
            }

            if (pos.y < -50f) break;

            t += simulationStep;
            yield return new WaitForSeconds(simulationStep);
        }

        if (tracer != null) Destroy(tracer, 2f);
    }
}
