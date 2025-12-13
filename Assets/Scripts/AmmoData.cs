using UnityEngine;

[CreateAssetMenu(menuName = "Ballistics/Ammo")]
public class AmmoData : ScriptableObject
{
    [Header("Identificación")]
    [SerializeField] private string ammoName;
    [SerializeField] private string caliber;

    [Header("Propiedades físicas")]
    [SerializeField] private float massKg;            // masa del proyectil (kg)
    [SerializeField] private float diameterMeters;    // diámetro del proyectil
    [SerializeField] private float dragCoefficient;   // Cd
    [SerializeField] private float muzzleVelocity;    // m/s (desde arma estándar)

    public string AmmoName => ammoName;
    public float MassKg => massKg;
    public float Diameter => diameterMeters;
    public float DragCoefficient => dragCoefficient;
    public float MuzzleVelocity => muzzleVelocity;

    public float FrontalArea =>
        Mathf.PI * Mathf.Pow(diameterMeters * 0.5f, 2f);
}
