using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Maneja input de disparo, creación de proyectiles y recoil.
/// Expone propiedades públicas read-only para que ShotManager/Save lean datos del arma.
/// </summary>
public class WeaponShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponData weapon;
    [SerializeField] private AmmoData ammo;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LayerMask hitMask;

    [Header("Fire Rate")]
    [SerializeField] private float fireRate = 300f; // RPM

    [Header("Projectile")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Recoil")]
    [SerializeField] private float weaponMassKg = 0.6f;
    [SerializeField] private float gasMultiplier = 1.25f;
    [SerializeField] private float positionKickFactor = 0.015f;
    [SerializeField] private float rotationKickFactor = 2.5f;

    private float timeBetweenShots;
    private float lastShotTime = -999f;
    private InputAction fireAction;
    private WeaponViewController weaponView;

    private void Awake()
    {
        timeBetweenShots = 60f / Mathf.Max(0.0001f, fireRate);
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        fireAction.Enable();
        weaponView = GetComponentInParent<WeaponViewController>();
        if (weapon != null && weapon.weaponMassKg > 0f) weaponMassKg = weapon.weaponMassKg;
    }

    private void OnDestroy() => fireAction.Disable();

    private void Update()
    {
        if (fireAction != null && fireAction.WasPressedThisFrame())
            TryFire();
    }

    private void TryFire()
    {
        if (weaponView == null || !weaponView.IsAiming) return;
        if (ShotManager.Instance == null || !ShotManager.Instance.CanFire()) return;
        if (Time.time - lastShotTime < timeBetweenShots) return;
        lastShotTime = Time.time;
        Fire();
    }

    private void Fire()
    {
        if (ammo == null || muzzle == null || bulletPrefab == null) return;

        float muzzleVelocity = ammo.MuzzleVelocity * (weapon != null ? weapon.velocityMultiplier : 1f);
        Vector3 dir = muzzle.forward;

        float moa = weapon != null ? weapon.accuracyMOA : 6f;
        float angleRad = moa * Mathf.Deg2Rad / 60f;
        Vector2 rnd = Random.insideUnitCircle * angleRad;
        dir = Quaternion.Euler(rnd.x * Mathf.Rad2Deg, rnd.y * Mathf.Rad2Deg, 0f) * dir;

        int shotId = ShotManager.Instance != null
            ? ShotManager.Instance.RegisterShotFired(muzzle.position, dir, muzzleVelocity)
            : -1;

        GameObject bullet = Instantiate(bulletPrefab, muzzle.position, Quaternion.LookRotation(dir));
        Projectile p = bullet.GetComponent<Projectile>();
        if (p != null)
        {
            p.Initialize(ammo, dir * muzzleVelocity, hitMask, shotId);
        }
        else
        {
            Debug.LogError("Bullet prefab necesita el script Projectile");
            Destroy(bullet);
            return;
        }

        ApplyRecoil(muzzleVelocity);
        ShotManager.Instance?.NotifyShotFired();
    }

    private void ApplyRecoil(float projectileVelocity)
    {
        if (weaponView == null || ammo == null) return;

        float momentum = ammo.MassKg * projectileVelocity * gasMultiplier;
        float recoilVelocity = momentum / Mathf.Max(0.001f, weaponMassKg);

        Vector3 posImpulse = new Vector3(0f, 0f, -recoilVelocity * positionKickFactor);
        Vector3 rotImpulse = new Vector3(-recoilVelocity * rotationKickFactor,
                                         Random.Range(-0.3f, 0.3f),
                                         Random.Range(-0.15f, 0.15f));
        weaponView.AddRecoil(posImpulse, rotImpulse);
    }

    // runtime setters
    public void SetWeaponReference(WeaponData newWeapon, AmmoData newAmmo)
    {
        weapon = newWeapon;
        ammo = newAmmo;
        if (weapon != null && weapon.weaponMassKg > 0f) weaponMassKg = weapon.weaponMassKg;
    }

    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;

    // read-only properties for other systems
    public string WeaponName => weapon != null ? weapon.weaponName : "Unknown";
    public float WeaponMassKg => (weapon != null && weapon.weaponMassKg > 0f) ? weapon.weaponMassKg : weaponMassKg;
    public float BarrelLengthMeters => weapon != null ? weapon.barrelLengthMeters : 0f;
    public string AmmoName => ammo != null ? ammo.AmmoName : "Unknown";
    public float AmmoVelocity => ammo != null ? ammo.MuzzleVelocity * (weapon != null ? weapon.velocityMultiplier : 1f) : 0f;
}
