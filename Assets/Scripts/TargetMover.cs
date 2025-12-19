using UnityEngine;

/// <summary>
/// Movimiento lateral del target basado en desplazamiento angular,
/// escalado automáticamente según la distancia seleccionada.
/// Compatible con TargetPlacementController.
/// </summary>
[DisallowMultipleComponent]
public class TargetMover : MonoBehaviour
{
    public static TargetMover Instance { get; private set; }

    [Header("Angular Movement")]
    [Tooltip("Desplazamiento TOTAL en grados (ej: 4 = ±2°)")]
    public float angularTravelDeg = 4f;

    [Tooltip("Velocidad de barrido angular")]
    public float angularSpeed = 1.2f;

    [Header("Axis")]
    public Vector3 localAxis = Vector3.right;

    public bool IsMoving { get; private set; }
    public bool WasEverMoved { get; private set; }

    private Vector3 initialLocalPos;
    private float phase;
    private Transform t;

    private TargetPlacementController placement;
    private float currentDistance = 25f;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else Instance = this;

        t = transform;
        initialLocalPos = t.localPosition;

        placement = FindObjectOfType<TargetPlacementController>();
        if (placement != null)
            currentDistance = placement.GetCurrentDistance();
    }

    private void Update()
    {
        if (!IsMoving) return;

        // actualizar distancia actual
        if (placement != null)
            currentDistance = placement.GetCurrentDistance();

        phase += Time.deltaTime * angularSpeed;

        // convertir grados a desplazamiento en metros
        float halfAngleRad = (angularTravelDeg * 0.5f) * Mathf.Deg2Rad;
        float maxOffsetMeters = Mathf.Tan(halfAngleRad) * currentDistance;

        float raw = Mathf.PingPong(phase, maxOffsetMeters * 2f) - maxOffsetMeters;
        Vector3 offset = localAxis.normalized * raw;

        t.localPosition = initialLocalPos + offset;
    }

    // ---------- UI / API ----------

    public void ToggleMovement()
    {
        if (IsMoving) StopMovement();
        else StartMovement();
    }

    public void StartMovement()
    {
        IsMoving = true;
        WasEverMoved = true;
    }

    public void StopMovement()
    {
        IsMoving = false;
    }

    public void ResetPosition()
    {
        IsMoving = false;
        phase = 0f;
        t.localPosition = initialLocalPos;
    }

    public void RebindInitialPosition()
    {
        initialLocalPos = t.localPosition;
        phase = 0f;
    }
}
