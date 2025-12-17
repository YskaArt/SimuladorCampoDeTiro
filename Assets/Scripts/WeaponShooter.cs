using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShooter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponData weapon;
    [SerializeField] private AmmoData ammo;
    [SerializeField] public Transform muzzle; // made public for runtime assignment
    [SerializeField] private LayerMask hitMask;

    [Header("Fire Rate")]
    [Tooltip("rounds per minute")]
    [SerializeField] private float fireRate = 300f;

    [Header("Visual / Prefab")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Recoil")]
    [SerializeField] private WeaponRecoilPivot recoilPivot;

    [Tooltip("weapon mass in kg")]
    [SerializeField] private float weaponMassKg = 0.61f;

    [SerializeField, Range(1f, 1.5f)]
    private float gasMultiplier = 1.25f;

    [Header("References (optional)")]
    [SerializeField] private WeaponViewController weaponView; // usado para bloquear disparo en Idle (opcional)

    private float timeBetweenShots;
    private float lastShotTime = -999f;

    private InputAction fireAction;

    private void Awake()
    {
        timeBetweenShots = 60f / Mathf.Max(0.0001f, fireRate);
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");

        // auto-find if not set
        if (weaponView == null)
            weaponView = GetComponentInParent<WeaponViewController>();

        if (weapon != null && weapon.weaponMassKg > 0f)
            weaponMassKg = weapon.weaponMassKg;
    }

    private void OnEnable()
    {
        fireAction?.Enable();
    }

    private void OnDisable()
    {
        fireAction?.Disable();
    }

    private void Update()
    {
        if (fireAction != null && fireAction.WasPressedThisFrame())
            TryFire();
    }

    private void TryFire()
    {
        if (weaponView == null)
            weaponView = GetComponentInParent<WeaponViewController>();

        // 1. No disparar si no está apuntando
        if (weaponView != null && !weaponView.IsAiming)
            return;

        // 2. No disparar si no hay munición
        if (ShotManager.Instance != null && !ShotManager.Instance.CanFire())
            return;

        // 3. Fire rate
        if (Time.time - lastShotTime < timeBetweenShots)
            return;

        lastShotTime = Time.time;
        Fire();
    }

    private void Fire()
    {
        if (ammo == null || muzzle == null) return;

        float finalVelocity = ammo.MuzzleVelocity * (weapon != null ? weapon.velocityMultiplier : 1f);
        Vector3 direction = muzzle.forward;

        float moa = (weapon != null) ? weapon.accuracyMOA : 6.0f;
        float angleRad = moa * Mathf.Deg2Rad / 60f;
        Vector2 rnd = Random.insideUnitCircle * angleRad;
        direction = Quaternion.Euler(rnd.x * Mathf.Rad2Deg, rnd.y * Mathf.Rad2Deg, 0f) * direction;

        if (bulletPrefab != null)
        {
            GameObject b = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(direction));
            Projectile p = b.GetComponent<Projectile>();
            if (p != null)
            {
                p.Initialize(ammo, direction * finalVelocity, hitMask);
            }
            else
            {
                Debug.LogError("Bullet prefab necesita el script Projectile");
                Destroy(b);
            }
        }

        if (recoilPivot != null)
        {
            float momentum = ammo.MassKg * finalVelocity;
            float effectiveMomentum = momentum * gasMultiplier;
            float pitchDeg = effectiveMomentum * 0.9f;
            float yawDeg = Random.Range(-0.15f, 0.15f);
            float rollDeg = Random.Range(-0.05f, 0.05f);
            float backMove = effectiveMomentum * 0.0006f;
            recoilPivot.AddRecoil(pitchDeg, yawDeg, rollDeg, backMove);
        }

        ShotManager.Instance?.NotifyShotFired();
    }

    // -------------------------
    // Public API for runtime assignment
    // -------------------------
    public void SetWeaponReference(WeaponData newWeapon, AmmoData newAmmo)
    {
        this.weapon = newWeapon;
        this.ammo = newAmmo;

        if (newWeapon != null && newWeapon.weaponMassKg > 0f)
            weaponMassKg = newWeapon.weaponMassKg;

        // recalc timing if weapon has different fireRate? (left for future)
    }

    public void SetMuzzle(Transform muzzleTransform)
    {
        muzzle = muzzleTransform;
    }
    public void SetRecoilPivot(WeaponRecoilPivot pivot)
    {
        recoilPivot = pivot;
    }

    public void SetFireRateRPM(float rpm)
    {
        fireRate = Mathf.Max(0.0001f, rpm);
        timeBetweenShots = 60f / fireRate;
    }
}
