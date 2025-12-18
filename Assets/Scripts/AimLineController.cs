using UnityEngine;

/// <summary>
/// Dibuja la trayectoria balística en varios puntos usando integración simple.
/// Permite runtime SetMuzzle / SetMuzzleVelocity y consulta de punto predicho.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class AimLineController : MonoBehaviour
{
    [SerializeField, Range(8, 256)] private int maxSegments = 64;
    [SerializeField] private float timeStep = 0.05f;
    [SerializeField] private float maxDistance = 150f;
    [SerializeField] private float textureTiling = 1.25f;

    [SerializeField] private MonoBehaviour weaponViewSource;
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
        Vector3 vel = muzzle.forward * muzzleVelocity;
        Vector3 g = Physics.gravity;

        float traveled = 0f;
        Vector3 prev = origin;
        int actualCount = 0;

        line.positionCount = maxSegments;

        for (int i = 0; i < maxSegments; i++)
        {
            float t = i * timeStep;
            Vector3 p = origin + vel * t + 0.5f * g * t * t;

            traveled += Vector3.Distance(prev, p);
            line.SetPosition(i, p);
            prev = p;
            actualCount = i + 1;

            if (traveled >= maxDistance) break;
        }

        line.positionCount = actualCount;
        if (line.material != null)
            line.material.mainTextureScale = new Vector2(actualCount * textureTiling, 1f);

        line.enabled = true;
    }

    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;
    public void SetMuzzleVelocity(float v) => muzzleVelocity = Mathf.Max(1f, v);

    public Vector3 GetPredictedPointAtDistance(float meters)
    {
        if (muzzle == null) return Vector3.zero;
        Vector3 origin = muzzle.position;
        Vector3 vel = muzzle.forward * muzzleVelocity;
        Vector3 g = Physics.gravity;

        float traveled = 0f;
        Vector3 prev = origin;
        int steps = Mathf.Max(8, maxSegments);

        for (int i = 0; i < steps; i++)
        {
            float t = i * timeStep;
            Vector3 p = origin + vel * t + 0.5f * g * t * t;
            traveled += Vector3.Distance(prev, p);
            prev = p;
            if (traveled >= meters) return p;
        }

        float tFinal = (steps - 1) * timeStep;
        return origin + vel * tFinal + 0.5f * g * tFinal * tFinal;
    }
}
