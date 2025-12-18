using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimLineController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour weaponViewSource;

    [Header("Ballistics")]
    [SerializeField, Range(5, 80)] private int segments = 32;
    [SerializeField] private float timeStep = 0.05f;
    [SerializeField] private float maxDistance = 150f;

    [Header("Visual")]
    [SerializeField] private float textureTiling = 1.25f;

    private IWeaponView weaponView;
    private LineRenderer line;

    private Transform muzzle;
    private float muzzleVelocity = 300f;

    private void Awake()
    {
        weaponView = weaponViewSource as IWeaponView;

        line = GetComponent<LineRenderer>();
        line.enabled = false;
        line.textureMode = LineTextureMode.Tile;
    }

    private void Update()
    {
        if (weaponView == null || muzzle == null || !weaponView.IsAiming)
        {
            line.enabled = false;
            return;
        }

        DrawBallisticCurve();
    }

    private void DrawBallisticCurve()
    {
        Vector3 origin = muzzle.position;
        Vector3 velocity = muzzle.forward * muzzleVelocity;
        Vector3 gravity = Physics.gravity;

        float traveled = 0f;
        Vector3 prevPoint = origin;

        line.positionCount = segments;

        for (int i = 0; i < segments; i++)
        {
            float t = i * timeStep;

            Vector3 point =
                origin +
                velocity * t +
                0.5f * gravity * t * t;

            traveled += Vector3.Distance(prevPoint, point);
            if (traveled > maxDistance)
            {
                line.positionCount = i + 1;
                break;
            }

            line.SetPosition(i, point);
            prevPoint = point;
        }

        line.material.mainTextureScale = new Vector2(segments * textureTiling, 1f);
        line.enabled = true;
    }

    // -------------------------------------------------
    // Runtime wiring (called by WeaponInventoryManager)
    // -------------------------------------------------
    public void SetMuzzle(Transform newMuzzle)
    {
        muzzle = newMuzzle;
    }

    public void SetMuzzleVelocity(float velocity)
    {
        muzzleVelocity = Mathf.Max(1f, velocity);
    }
}
