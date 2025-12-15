using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponShooter : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WeaponData weapon;
    [SerializeField] private AmmoData ammo;
    [SerializeField] private Transform muzzle;
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
        timeBetweenShots = 60f / fireRate;
        fireAction = new InputAction("Fire", InputActionType.Button, "<Mouse>/leftButton");
        fireAction.Enable();

        // auto-find if not set
        if (weaponView == null)
            weaponView = GetComponentInParent<WeaponViewController>();

        if (weapon != null && weapon.weaponMassKg > 0f)
            weaponMassKg = weapon.weaponMassKg;
    }

    private void OnDestroy()
    {
        fireAction.Disable();
    }

    private void Update()
    {
        if (fireAction.WasPressedThisFrame())
            TryFire();
    }

    private void TryFire()
    {
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

        // ... (existing dispersion + shot creation) ...

        // instantiate projectile (uses Projectile.Initialize)
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

        // realtime recoil via RecoilPivot (as ya tenés)
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

        // notify shot manager
        ShotManager.Instance?.NotifyShotFired();
    }

}
