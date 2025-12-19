using UnityEngine;

/// <summary>
/// WeaponAimController (attach to the player's Camera)
/// - Positions camera at sightPoint while aiming (works with CameraAimController)
/// - If the sightPoint is inside weapon geometry, pushes the camera backward along its forward
///   until it's no longer colliding with the weapon colliders (up to maxBackDistance).
/// - Smooths motion and rotation. Exposes SetSightPoints(...) to be called when weapon model is swapped.
/// </summary>
[RequireComponent(typeof(Camera))]
public class WeaponAimController : MonoBehaviour
{
    [Header("Source")]
    [Tooltip("A component that implements IWeaponView (usually WeaponViewController)")]
    [SerializeField] private MonoBehaviour weaponViewSource;

    [Header("Collision / Safety")]
    [Tooltip("Layer(s) that contain weapon model colliders (so camera won't penetrate).")]
    [SerializeField] private LayerMask weaponLayerMask = 0;
    [Tooltip("Radius used to test camera-space overlap with weapon geometry")]
    [SerializeField] private float checkRadius = 0.06f;
    [Tooltip("Maximum distance to push the camera backwards to avoid intersection (meters)")]
    [SerializeField] private float maxBackDistance = 0.45f;
    [Tooltip("Step used to probe when pushing back (meters). Lower -> more precise, heavier CPU.")]
    [SerializeField] private float backStep = 0.02f;

    [Header("Smoothing")]
    [SerializeField] private float positionLerp = 12f;
    [SerializeField] private float rotationLerp = 20f;

    [Header("Optional: temporarily reduce near clip plane while aiming")]
    [SerializeField] private bool adjustNearClip = true;
    [SerializeField] private float aimNearClip = 0.01f;

    private IWeaponView weaponView;
    private Camera cam;

    // runtime sight targets (assigned when weapon is equipped)
    private Transform sightPoint;   // where camera aims to be (eye position of the weapon)
    private Transform frontSight;   // optional front sight marker (for extra alignment if needed)

    // internal
    private Vector3 desiredPosition;
    private Quaternion desiredRotation;
    private float originalNearClip;
    private bool configured = false;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        weaponView = weaponViewSource as IWeaponView;
        originalNearClip = cam.nearClipPlane;
    }

    private void OnDestroy()
    {
        if (cam != null && adjustNearClip)
            cam.nearClipPlane = originalNearClip;
    }

    private void LateUpdate()
    {
        if (!configured || weaponView == null || sightPoint == null)
            return;

        if (weaponView.IsAiming)
        {
            // target (raw) is the sightPoint world transform
            Vector3 rawPos = sightPoint.position;
            Quaternion rawRot = sightPoint.rotation;

            // compute safe position: if rawPos intersects weapon geometry, push back
            Vector3 safePos = ComputeSafeCameraPosition(rawPos, rawRot);

            desiredPosition = safePos;
            desiredRotation = rawRot;

            // optionally adjust near clip (small) to avoid Z-fight with very close geometry
            if (adjustNearClip)
            {
                cam.nearClipPlane = aimNearClip;
            }
        }
        else
        {
            // when not aiming we don't override camera; CameraAimController will restore default pos/rot
            if (adjustNearClip)
                cam.nearClipPlane = originalNearClip;
            return;
        }

        // smooth apply
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * positionLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Time.deltaTime * rotationLerp);
    }

    /// <summary>
    /// Compute a camera position starting at rawPos. If rawPos overlaps the weapon geometry
    /// (weaponLayerMask) we move the camera backwards along its forward (negative forward)
    /// in steps up to maxBackDistance until no overlap is detected.
    /// </summary>
    private Vector3 ComputeSafeCameraPosition(Vector3 rawPos, Quaternion rawRot)
    {
        // quick test: if no weapon layer assigned, just return raw
        if (weaponLayerMask == 0)
            return rawPos;

        // check if rawPos is free
        if (!Physics.CheckSphere(rawPos, checkRadius, weaponLayerMask, QueryTriggerInteraction.Ignore))
            return rawPos;

        // step backwards along camera's back direction (we want to move away from geometry)
        Vector3 backDir = -(rawRot * Vector3.forward); // camera.forward in rawRot space
        // note: rawRot * Vector3.forward is camera's forward if camera were at rawRot
        float moved = 0f;
        Vector3 candidate = rawPos;

        // try increments
        while (moved <= maxBackDistance)
        {
            candidate = rawPos + backDir * moved;
            if (!Physics.CheckSphere(candidate, checkRadius, weaponLayerMask, QueryTriggerInteraction.Ignore))
            {
                return candidate;
            }
            moved += backStep;
        }

        // if still colliding after maxBackDistance, return the raw pos pushed the full amount
        return rawPos + backDir * maxBackDistance;
    }

    /// <summary>
    /// Assign runtime sight transforms. Call this when you spawn / swap weapon model.
    /// </summary>
    public void SetSightPoints(Transform newSightPoint, Transform newFrontSight = null)
    {
        sightPoint = newSightPoint;
        frontSight = newFrontSight;
        configured = sightPoint != null;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (sightPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(sightPoint.position, checkRadius);
            if (frontSight != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(sightPoint.position, frontSight.position);
                Gizmos.DrawWireSphere(frontSight.position, checkRadius * 0.6f);
            }
            // draw vector back
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(sightPoint.position, -(sightPoint.rotation * Vector3.forward) * maxBackDistance);
        }
    }
#endif
}
