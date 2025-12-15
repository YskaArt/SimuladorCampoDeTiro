using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WeaponViewController
/// - Attach to WeaponHolder (under WeaponRig).
/// - Controls Idle/Aim poses (local), mouse aiming offsets, sway, recoil.
/// - Exposes AddRecoil(posLocal, rotEuler).
/// </summary>
public class WeaponViewController : MonoBehaviour, IWeaponView
{
    public enum WeaponState { Idle, Aim }

    [Header("References / Pose (local to WeaponHolder)")]
    [Tooltip("Local position when not aiming")]
    public Vector3 idleLocalPosition = new Vector3(0.15f, -0.15f, 0.35f);
    [Tooltip("Local Euler rotation when not aiming (degrees)")]
    public Vector3 idleLocalRotation = new Vector3(0f, 10f, 0f);

    [Tooltip("Local position when aiming")]
    public Vector3 aimLocalPosition = new Vector3(0f, -0.05f, 0.15f);
    [Tooltip("Local Euler rotation when aiming (degrees)")]
    public Vector3 aimLocalRotation = new Vector3(0f, 0f, 0f);

    [Header("Input / Aim")]
    public float aimSensitivity = 1.0f;
    public float maxAimAngle = 6f;

    [Header("Sway")]
    public float breathAmplitudeDeg = 0.5f;
    public float breathFrequency = 0.25f;
    public float tremorAmplitudeDeg = 0.18f;
    public float tremorFrequency = 6.0f;
    [Range(0f, 1f)]
    public float aimSwayReduction = 0.6f;

    [Header("Inertia / Reactive")]
    public float mouseInertiaMultiplier = 0.6f;
    public float mouseInertiaReturnSpeed = 6f;

    [Header("Spring / Smooth")]
    public float transformLerpSpeed = 12f;

    [Header("Recoil (pos local in meters, rot in degrees)")]
    public float recoilPosReturn = 10f;
    public float recoilRotReturn = 8f;

    // state
    private WeaponState currentState = WeaponState.Idle;
    public bool IsAiming => currentState == WeaponState.Aim && !aimLocked;

    // input
    private InputAction aimAction;
    private InputAction mouseDeltaAction;

    // aim offsets (degrees)
    private Vector2 aimAngles = Vector2.zero;
    private Vector2 reactiveOffset = Vector2.zero;

    // recoil accumulators (local space)
    private Vector3 recoilPosOffset = Vector3.zero;   // meters local
    private Vector3 recoilRotEuler = Vector3.zero;    // degrees local (pitch, yaw, roll)
    private Vector2 aimAnglesVelocity; // usado por SmoothDamp
    private bool aimLocked = false;
    private void Awake()
    {
        aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        mouseDeltaAction = new InputAction("MouseDelta", InputActionType.PassThrough, "<Mouse>/delta");

        aimAction.Enable();
        mouseDeltaAction.Enable();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDestroy()
    {
        aimAction.Disable();
        mouseDeltaAction.Disable();
    }

    private void Update()
    {
        if (aimLocked)
            currentState = WeaponState.Idle;
        else
            currentState = aimAction.IsPressed() ? WeaponState.Aim : WeaponState.Idle;
        

        HandleMouseAim();
        ApplySwayAndPoseWithRecoil();
    }
    public void ForceExitAimLock(bool locked)
    {
        aimLocked = locked;

        if (locked)
        {
            // forzar volver a idle y resetear offsets
            currentState = WeaponState.Idle;
            ResetAimOffsets();
        }
    }

    private void HandleMouseAim()
    {
        Vector2 md = mouseDeltaAction.ReadValue<Vector2>();

        if (IsAiming)
        {
            float yawDelta = md.x * aimSensitivity * Time.deltaTime;
            float pitchDelta = -md.y * aimSensitivity * Time.deltaTime;

            aimAngles.x += pitchDelta;
            aimAngles.y += yawDelta;

            aimAngles.x = Mathf.Clamp(aimAngles.x, -maxAimAngle, maxAimAngle);
            aimAngles.y = Mathf.Clamp(aimAngles.y, -maxAimAngle, maxAimAngle);

            reactiveOffset = Vector2.Lerp(reactiveOffset, new Vector2(-pitchDelta, -yawDelta) * mouseInertiaMultiplier, Time.deltaTime * 10f);
        }
        else
        {
            aimAngles = Vector2.SmoothDamp(aimAngles,Vector2.zero,ref aimAnglesVelocity,0.08f,Mathf.Infinity,Time.deltaTime);

            reactiveOffset = Vector2.Lerp(reactiveOffset, Vector2.zero, Time.deltaTime * mouseInertiaReturnSpeed);
        }
    }

    private void ApplySwayAndPoseWithRecoil()
    {
        // sway
        float breathAmp = breathAmplitudeDeg * (IsAiming ? aimSwayReduction : 1f);
        float breath = (Mathf.PerlinNoise(Time.time * breathFrequency, 0f) - 0.5f) * 2f * breathAmp;
        float tremorAmp = tremorAmplitudeDeg * (IsAiming ? aimSwayReduction : 1f);
        float trem = (Mathf.PerlinNoise(100f + Time.time * tremorFrequency, 0f) - 0.5f) * 2f * tremorAmp;

        Vector3 swayRotation = new Vector3(
            aimAngles.x + reactiveOffset.x + breath + trem * 0.5f,
            aimAngles.y + reactiveOffset.y + trem * 0.5f,
            0f
        );

        // target base pose (local)
        Vector3 targetPosLocal = IsAiming ? aimLocalPosition : idleLocalPosition;
        Vector3 targetRotEuler = IsAiming ? aimLocalRotation : idleLocalRotation;

        // final position = base + recoilPosOffset
        Vector3 finalPosLocal = targetPosLocal + recoilPosOffset;

        // final rotation: base * sway * recoil (using quaternions)
        Quaternion baseRot = Quaternion.Euler(targetRotEuler);
        Quaternion swayQuat = Quaternion.Euler(swayRotation);
        Quaternion recoilQuat = Quaternion.Euler(recoilRotEuler);
        Quaternion finalQuatLocal = baseRot * swayQuat * recoilQuat;

        // apply smooth interpolation
        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosLocal, Time.deltaTime * transformLerpSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalQuatLocal, Time.deltaTime * transformLerpSpeed);

        // decay recoil offsets over time (smooth return)
        recoilPosOffset = Vector3.Lerp(recoilPosOffset, Vector3.zero, Time.deltaTime * recoilPosReturn);
        recoilRotEuler = Vector3.Lerp(recoilRotEuler, Vector3.zero, Time.deltaTime * recoilRotReturn);
    }

    /// <summary>
    /// Public API: apply an instant recoil impulse.
    /// posImpulseLocal: local-space positional impulse (e.g., (0,0,-0.02f))
    /// rotImpulseEuler: local-space euler degrees impulse (pitch up is positive X)
    /// </summary>
    public void AddRecoil(Vector3 posImpulseLocal, Vector3 rotImpulseEuler)
    {
        recoilPosOffset += posImpulseLocal;
        recoilRotEuler += rotImpulseEuler;
    }

    // optional: allow external reset of aim offsets
    public void ResetAimOffsets()
    {
        aimAngles = Vector2.zero;
        reactiveOffset = Vector2.zero;
    }
}
