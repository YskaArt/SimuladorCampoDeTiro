using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAimController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour weaponViewSource;
    [SerializeField] private Transform sightCameraPoint;
    [SerializeField] private float positionSpeed = 12f;
    [SerializeField] private float rotationSpeed = 12f;

    private IWeaponView weaponView;
    private Vector3 defaultWorldPos;
    private Quaternion defaultWorldRot;

    private void Awake()
    {
        weaponView = weaponViewSource as IWeaponView;
        defaultWorldPos = transform.position;
        defaultWorldRot = transform.rotation;
    }

    private void LateUpdate()
    {
        if (weaponView == null || sightCameraPoint == null) return;
        if (weaponView.IsAiming)
        {
            transform.position = Vector3.Lerp(transform.position, sightCameraPoint.position, Time.deltaTime * positionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, sightCameraPoint.rotation, Time.deltaTime * rotationSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, defaultWorldPos, Time.deltaTime * positionSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, defaultWorldRot, Time.deltaTime * rotationSpeed);
        }
    }

    public void SetSightPoint(Transform t) => sightCameraPoint = t;
}
