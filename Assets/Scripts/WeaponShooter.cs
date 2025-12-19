using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WeaponShooter
/// - Maneja input de disparo, instancia proyectiles, aplica recoil y reproduce audio.
/// - Expone propiedades públicas read-only para que ShotManager/Save lean datos del arma.
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

    [Header("Audio")]
    [SerializeField] private AudioClip shotSFX;
    [SerializeField] private AudioClip emptySFX;

    // runtime
    private float timeBetweenShots;
    private float lastShotTime = -999f;
    private InputAction fireAction;
    private WeaponViewController weaponView;

    private void Awake()
    {
        timeBetweenShots = 60f / Mathf.Max(0.0001f, fireRate);

        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");

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
        // 1) Need a weapon view and be aiming
        if (weaponView == null || !weaponView.IsAiming)
            return;

        // 2) Ensure ShotManager exists
        if (ShotManager.Instance == null)
            return;

        // 3) If no ammo, play empty SFX feedback and exit
        if (!ShotManager.Instance.CanFire())
        {
            if (emptySFX != null)
            {
                //Colocar Sonido de cargador vacio
            }

            return;
        }

        // 4) Respect fire rate
        if (Time.time - lastShotTime < timeBetweenShots)
            return;

        lastShotTime = Time.time;
        bool isLastRound = ShotManager.Instance.CurrentAmmo == 1;

        Fire(isLastRound);
    }

    private void Fire(bool isLastRound)
    {
        if (ammo == null || muzzle == null || bulletPrefab == null)
            return;

        float muzzleVelocity = ammo.MuzzleVelocity * (weapon != null ? weapon.velocityMultiplier : 1f);
        Vector3 dir = muzzle.forward;

        // dispersion (MOA -> small cone)
        float moa = weapon != null ? weapon.accuracyMOA : 6f;
        float angleRad = moa * Mathf.Deg2Rad / 60f;
        Vector2 rnd = Random.insideUnitCircle * angleRad;
        dir = Quaternion.Euler(rnd.x * Mathf.Rad2Deg, rnd.y * Mathf.Rad2Deg, 0f) * dir;

        // register shot (shotId links projectile -> record)
        int shotId = -1;
        if (ShotManager.Instance != null)
            shotId = ShotManager.Instance.RegisterShotFired(muzzle.position, dir, muzzleVelocity);

        // instantiate projectile
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

        // apply recoil
        ApplyRecoil(muzzleVelocity);

        if (shotSFX != null)
        {
            Vector3 soundPos = muzzle != null ? muzzle.position : transform.position;
            AudioManager.Instance?.PlayShot(shotSFX, soundPos);
          
        }
        if (isLastRound && emptySFX != null)
        {
            Vector3 soundPos = muzzle != null ? muzzle.position : transform.position;
            AudioManager.Instance?.PlayShot(emptySFX, soundPos);
        }

        // notify shot manager (decrement ammo, delayed lock handling, events)
        ShotManager.Instance?.NotifyShotFired();
    }

    private void ApplyRecoil(float projectileVelocity)
    {
        if (weaponView == null || ammo == null)
            return;

        float momentum = ammo.MassKg * projectileVelocity;
        momentum *= gasMultiplier;

        float recoilVelocity = momentum / Mathf.Max(0.001f, weaponMassKg);

        Vector3 posImpulse = new Vector3(0f, 0f, -recoilVelocity * positionKickFactor);
        Vector3 rotImpulse = new Vector3(
            -recoilVelocity * rotationKickFactor,
            Random.Range(-0.3f, 0.3f),
            Random.Range(-0.15f, 0.15f)
        );

        weaponView.AddRecoil(posImpulse, rotImpulse);
    }

    // -------------------- RUNTIME SETTERS --------------------

    public void SetWeaponReference(WeaponData newWeapon, AmmoData newAmmo)
    {
        weapon = newWeapon;
        ammo = newAmmo;

        if (weapon != null && weapon.weaponMassKg > 0f)
            weaponMassKg = weapon.weaponMassKg;
    }

    public void SetMuzzle(Transform newMuzzle) => muzzle = newMuzzle;

    public void SetWeaponAudio(AudioClip shot, AudioClip empty)
    {
        shotSFX = shot;
        emptySFX = empty;
    }

    // -------------------- PUBLIC READ-ONLY PROPERTIES --------------------

    public string WeaponName => weapon != null ? weapon.weaponName : "Unknown";
    public float WeaponMassKg => (weapon != null && weapon.weaponMassKg > 0f) ? weapon.weaponMassKg : weaponMassKg;
    public float BarrelLengthMeters => weapon != null ? weapon.barrelLengthMeters : 0f;
    public string AmmoName => ammo != null ? ammo.AmmoName : "Unknown";
    public float AmmoVelocity => ammo != null ? ammo.MuzzleVelocity * (weapon != null ? weapon.velocityMultiplier : 1f) : 0f;
}
