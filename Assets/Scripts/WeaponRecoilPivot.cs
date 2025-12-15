using UnityEngine;

/// <summary>
/// Recoil pivot: aplica rotación y desplazamiento relativo al pivot (ej. empuñadura).
/// - recoilBackLocal: vector en espacio LOCAL del pivot que define "hacia atrás".
///   Por ejemplo: (0,0,-1) = -Z, (-1,0,0) = -X, etc.
/// - recoilAxis: opcional; si se asigna, se usa recoilAxis.TransformDirection(recoilBackLocal)
///   en lugar de transform.TransformDirection.
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
        // smooth return toward identity
        recoilRotation = Quaternion.Slerp(recoilRotation, Quaternion.identity, Time.deltaTime * rotationReturnSpeed);
        recoilPosition = Vector3.Lerp(recoilPosition, Vector3.zero, Time.deltaTime * positionReturnSpeed);

        transform.localRotation = recoilRotation;
        transform.localPosition = recoilPosition;
    }

    /// <summary>
    /// Add recoil impulse.
    /// - pitchDeg: positive means "muzzle up" (we apply negative inside if needed by convention).
    /// - yawDeg, rollDeg: small randoms.
    /// - backMeters: positive magnitude in meters (function will apply in the chosen local 'back' direction).
    /// </summary>
    public void AddRecoil(float pitchDeg, float yawDeg, float rollDeg, float backMeters)
    {
        // NOTE: we apply pitch as NEGATIVE because in many rigs positive X rotates nose down.
        // If tu modelo requiere la convención opuesta, invertí el signo al llamar a AddRecoil.
        Quaternion rot = Quaternion.Euler(-pitchDeg, yawDeg, rollDeg);
        recoilRotation *= rot;

        // compute world direction for back using recoilAxis if provided
        Vector3 backDirWorld;
        if (recoilAxis != null)
            backDirWorld = recoilAxis.TransformDirection(recoilBackLocal.normalized);
        else
            backDirWorld = transform.TransformDirection(recoilBackLocal.normalized);

        // add local-space displacement by transforming world backDir to local of this pivot
        // we want recoilPosition to be local, so convert the world direction to local
        Vector3 backLocalDelta = transform.InverseTransformDirection(backDirWorld) * backMeters;
        recoilPosition += backLocalDelta;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Avoid degenerate vector
        if (recoilBackLocal == Vector3.zero)
            recoilBackLocal = new Vector3(0f, 0f, -1f);
    }
#endif
}
