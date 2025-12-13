using UnityEngine;

public class WeaponShooter : MonoBehaviour
{
    [SerializeField] private WeaponData weapon;
    [SerializeField] private AmmoData ammo;
    [SerializeField] private Transform muzzle;

    public BallisticShot CreateShot()
    {
        float finalVelocity =
            ammo.MuzzleVelocity * weapon.VelocityMultiplier;

        Vector3 direction = ApplyDispersion(muzzle.forward);

        return new BallisticShot
        {
            position = muzzle.position,
            velocity = direction * finalVelocity,
            mass = ammo.MassKg,
            dragCoefficient = ammo.DragCoefficient,
            area = ammo.FrontalArea
        };
    }

    private Vector3 ApplyDispersion(Vector3 direction)
    {
        float moaRad = weapon.AccuracyMOA * Mathf.Deg2Rad / 60f;
        Vector2 random = Random.insideUnitCircle * moaRad;
        return Quaternion.Euler(random.x, random.y, 0f) * direction;
    }
}
