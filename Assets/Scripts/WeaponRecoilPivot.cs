using UnityEngine;

/// <summary>
/// Recoil pivot: aplica rotación y desplazamiento relativo al pivot (ej. empuñadura).
/// </summary>
public class WeaponRecoilPivot : MonoBehaviour
{
    [Header("Recoil Settings")]
    [SerializeField] private float rotationReturnSpeed = 14f;
    [SerializeField] private float positionReturnSpeed = 18f;

    [Header("Axis / Direction")]
    [Tooltip("Local vector that points 'backwards' from the muzzle. Example: (-1,0,0) if -X is back.")]
    [SerializeField] private Vector3 recoilBackLocal = new Vector3(0f, 0f, -1f);

    [Tooltip("Optional: use this transform's forward/right/up to compute back direction instead of this pivot.")]
    [SerializeField] private Transform recoilAxis; // usually muzzle or weapon model root

    // internal state (local offsets applied to this pivot)
    private Quaternion recoilRotation = Quaternion.identity;
    private Vector3 recoilPosition = Vector3.zero;

    private void LateUpdate()
    {
        recoilRotation = Quaternion.Slerp(recoilRotation, Quaternion.identity, Time.deltaTime * rotationReturnSpeed);
        recoilPosition = Vector3.Lerp(recoilPosition, Vector3.zero, Time.deltaTime * positionReturnSpeed);

        transform.localRotation = recoilRotation;
        transform.localPosition = recoilPosition;
    }

    public void AddRecoil(float pitchDeg, float yawDeg, float rollDeg, float backMeters)
    {
        Quaternion rot = Quaternion.Euler(-pitchDeg, yawDeg, rollDeg);
        recoilRotation *= rot;

        Vector3 backDirWorld;
        if (recoilAxis != null)
            backDirWorld = recoilAxis.TransformDirection(recoilBackLocal.normalized);
        else
            backDirWorld = transform.TransformDirection(recoilBackLocal.normalized);

        Vector3 backLocalDelta = transform.InverseTransformDirection(backDirWorld) * backMeters;
        recoilPosition += backLocalDelta;
    }

    /// <summary>
    /// Setter to allow runtime assignment by inventory manager.
    /// </summary>
    public void SetRecoilAxis(Transform t)
    {
        recoilAxis = t;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (recoilBackLocal == Vector3.zero)
            recoilBackLocal = new Vector3(0f, 0f, -1f);
    }
#endif
}
