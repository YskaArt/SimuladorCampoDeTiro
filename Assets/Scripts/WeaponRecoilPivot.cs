using UnityEngine;

public class WeaponRecoilPivot : MonoBehaviour
{
    [SerializeField] private float rotationReturnSpeed = 14f;
    [SerializeField] private float positionReturnSpeed = 18f;
    [SerializeField] private Vector3 recoilBackLocal = new Vector3(0f, 0f, -1f);
    [SerializeField] private Transform recoilAxis;

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

        Vector3 backDirWorld = recoilAxis != null ? recoilAxis.TransformDirection(recoilBackLocal.normalized)
                                                 : transform.TransformDirection(recoilBackLocal.normalized);
        Vector3 backLocalDelta = transform.InverseTransformDirection(backDirWorld) * backMeters;
        recoilPosition += backLocalDelta;
    }

    public void SetRecoilAxis(Transform t) => recoilAxis = t;

#if UNITY_EDITOR
    private void OnValidate() { if (recoilBackLocal == Vector3.zero) recoilBackLocal = new Vector3(0f, 0f, -1f); }
#endif
}
