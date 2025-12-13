using UnityEngine;

[CreateAssetMenu(menuName = "Ballistics/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identificación")]
    [SerializeField] private string weaponName;

    [Header("Compatibilidad")]
    [SerializeField] private AmmoData[] supportedAmmo;

    [Header("Características del arma")]
    [SerializeField] private float barrelLengthMeters;
    [SerializeField] private float accuracyMOA;
    [SerializeField] private float velocityMultiplier; // ajuste por cañón

    public string WeaponName => weaponName;
    public AmmoData[] SupportedAmmo => supportedAmmo;
    public float BarrelLength => barrelLengthMeters;
    public float AccuracyMOA => accuracyMOA;
    public float VelocityMultiplier => velocityMultiplier;
}
