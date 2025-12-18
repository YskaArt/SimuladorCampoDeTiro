using UnityEngine;

[DisallowMultipleComponent]
public class WeaponModelConnector : MonoBehaviour
{
    public Transform muzzle;
    public Transform sightPoint;
    public Transform recoilAxis;
    public Vector3 modelLocalPositionOffset = Vector3.zero;
    public Vector3 modelLocalEulerOffset = Vector3.zero;

    private void Reset()
    {
        if (muzzle == null) muzzle = transform.Find("Muzzle");
        if (sightPoint == null) sightPoint = transform.Find("SightPoint") ?? transform.Find("Sight");
        if (recoilAxis == null) recoilAxis = transform.Find("RecoilAxis");
    }
}
