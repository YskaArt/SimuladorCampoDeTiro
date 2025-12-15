using UnityEngine;

[CreateAssetMenu(menuName = "Ballistics/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identification")]
    public string weaponName = "Glock 19 Gen5";

    [Header("Compatibility")]
    public AmmoData[] supportedAmmo;

    [Header("Characteristics")]
    public float barrelLengthMeters = 0.102f;
    public float accuracyMOA = 6.0f;
    public float velocityMultiplier = 0.95f;

    // optional: mass of weapon for recoil calc (if you prefer centralizing here)
    public float weaponMassKg = 0.61f;
}
