using UnityEngine;

/// <summary>
/// Helper attached to weapon model prefabs. Exposes transforms the system needs:
/// - muzzle: where bullets spawn
/// - sightPoint: camera position for aiming
/// - recoilAxis: optional transform used by WeaponRecoilPivot
/// Also allows small local tuning offsets if needed.
/// </summary>
[DisallowMultipleComponent]
public class WeaponModelConnector : MonoBehaviour
{
    [Tooltip("Transform at the muzzle tip where projectiles spawn")]
    public Transform muzzle;

    [Tooltip("Transform used by CameraAimController when player aims (eye position)")]
    public Transform sightPoint;

    [Tooltip("Optional transform used as reference for recoil axis calculation")]
    public Transform recoilAxis;

    [Header("Optional tuning (applied to instantiated model)")]
    public Vector3 modelLocalPositionOffset = Vector3.zero;
    public Vector3 modelLocalEulerOffset = Vector3.zero;

    private void Reset()
    {
        // try convenience auto-find by common names
        if (muzzle == null) muzzle = transform.Find("Muzzle");
        if (sightPoint == null) sightPoint = transform.Find("SightPoint") ?? transform.Find("Sight");
        if (recoilAxis == null) recoilAxis = transform.Find("RecoilAxis");
    }
}
