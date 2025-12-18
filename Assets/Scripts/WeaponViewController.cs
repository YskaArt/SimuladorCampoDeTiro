using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponViewController : MonoBehaviour, IWeaponView
{
    public enum WeaponState { Idle, Aim }

    [Header("Poses (local)")]
    public Vector3 idleLocalPosition = new Vector3(0.15f, -0.15f, 0.35f);
    public Vector3 idleLocalRotation = new Vector3(0f, 10f, 0f);
    public Vector3 aimLocalPosition = new Vector3(0f, -0.05f, 0.15f);
    public Vector3 aimLocalRotation = new Vector3(0f, 0f, 0f);

    [Header("Aim")]
    public float aimSensitivity = 1f;
    public float maxAimAngle = 6f;

    [Header("Sway")]
    public float breathAmplitudeDeg = 0.5f;
    public float breathFrequency = 0.25f;
    public float tremorAmplitudeDeg = 0.18f;
    public float tremorFrequency = 6f;
    [Range(0f, 1f)] public float aimSwayReduction = 0.6f;

    [Header("Inertia / Smooth")]
    public float mouseInertiaMultiplier = 0.6f;
    public float mouseInertiaReturnSpeed = 6f;
    public float transformLerpSpeed = 12f;

    [Header("Recoil decay")]
    public float recoilPosReturn = 10f;
    public float recoilRotReturn = 8f;

    private WeaponState currentState = WeaponState.Idle;
    public bool IsAiming => currentState == WeaponState.Aim && !aimLocked;

    private InputAction aimAction;
    private InputAction mouseDeltaAction;

    private Vector2 aimAngles;
    private Vector2 reactiveOffset;
    private Vector3 recoilPosOffset;
    private Vector3 recoilRotEuler;
    private Vector2 aimAnglesVelocity;
    private bool aimLocked;

    private void Awake()
    {
        aimAction = new InputAction("Aim", InputActionType.Button, "<Mouse>/rightButton");
        mouseDeltaAction = new InputAction("MouseDelta", InputActionType.PassThrough, "<Mouse>/delta");
    }

    private void OnEnable()
    {
        aimAction?.Enable();
        mouseDeltaAction?.Enable();
    }

    private void OnDisable()
    {
        aimAction?.Disable();
        mouseDeltaAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        currentState = aimLocked ? WeaponState.Idle : (aimAction != null && aimAction.IsPressed() ? WeaponState.Aim : WeaponState.Idle);

        if (IsAiming)
        {
            if (Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
        }
        else
        {
            if (Cursor.lockState != CursorLockMode.None) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        HandleMouseAim();
        ApplySwayAndPoseWithRecoil();
    }

    public void ForceExitAimLock(bool locked)
    {
        aimLocked = locked;
        if (locked) { currentState = WeaponState.Idle; ResetAimOffsets(); }
    }

    private void HandleMouseAim()
    {
        Vector2 md = mouseDeltaAction != null ? mouseDeltaAction.ReadValue<Vector2>() : Vector2.zero;

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
            aimAngles = Vector2.SmoothDamp(aimAngles, Vector2.zero, ref aimAnglesVelocity, 0.08f, Mathf.Infinity, Time.deltaTime);
            reactiveOffset = Vector2.Lerp(reactiveOffset, Vector2.zero, Time.deltaTime * mouseInertiaReturnSpeed);
        }
    }

    private void ApplySwayAndPoseWithRecoil()
    {
        float breathAmp = breathAmplitudeDeg * (IsAiming ? aimSwayReduction : 1f);
        float breath = (Mathf.PerlinNoise(Time.time * breathFrequency, 0f) - 0.5f) * 2f * breathAmp;
        float tremAmp = tremorAmplitudeDeg * (IsAiming ? aimSwayReduction : 1f);
        float trem = (Mathf.PerlinNoise(100f + Time.time * tremorFrequency, 0f) - 0.5f) * 2f * tremAmp;

        Vector3 swayRot = new Vector3(aimAngles.x + reactiveOffset.x + breath + trem * 0.5f,
                                       aimAngles.y + reactiveOffset.y + trem * 0.5f, 0f);

        Vector3 targetPosLocal = IsAiming ? aimLocalPosition : idleLocalPosition;
        Vector3 targetRotEuler = IsAiming ? aimLocalRotation : idleLocalRotation;
        Vector3 finalPosLocal = targetPosLocal + recoilPosOffset;

        Quaternion finalRot = Quaternion.Euler(targetRotEuler) * Quaternion.Euler(swayRot) * Quaternion.Euler(recoilRotEuler);

        transform.localPosition = Vector3.Lerp(transform.localPosition, finalPosLocal, Time.deltaTime * transformLerpSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, finalRot, Time.deltaTime * transformLerpSpeed);

        recoilPosOffset = Vector3.Lerp(recoilPosOffset, Vector3.zero, Time.deltaTime * recoilPosReturn);
        recoilRotEuler = Vector3.Lerp(recoilRotEuler, Vector3.zero, Time.deltaTime * recoilRotReturn);
    }

    public void AddRecoil(Vector3 posImpulseLocal, Vector3 rotImpulseEuler)
    {
        recoilPosOffset += posImpulseLocal;
        recoilRotEuler += rotImpulseEuler;
    }

    public void ResetAimOffsets()
    {
        aimAngles = Vector2.zero;
        reactiveOffset = Vector2.zero;
        aimAnglesVelocity = Vector2.zero;
    }
}
