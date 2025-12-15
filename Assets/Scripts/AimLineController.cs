using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimLineController : MonoBehaviour
{
    [SerializeField] private MonoBehaviour weaponViewSource;
    [SerializeField] private Transform muzzle;
    [SerializeField] private float maxDistance = 100f;

    private IWeaponView weaponView;
    private LineRenderer line;

    private void Awake()
    {
        weaponView = weaponViewSource as IWeaponView;
        line = GetComponent<LineRenderer>();
        line.positionCount = 2;
        line.enabled = false;
    }

    private void Update()
    {
        if (weaponView == null || muzzle == null) return;

        if (!weaponView.IsAiming)
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        Vector3 start = muzzle.position;
        Vector3 end = start + muzzle.forward * maxDistance;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
}
