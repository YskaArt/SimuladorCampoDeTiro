using UnityEngine;

[CreateAssetMenu(menuName = "Ballistics/Ammo")]
public class AmmoData : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string ammoName = "9mm Parabellum";
    [SerializeField] private string caliber = "9x19";

    [Header("Physical properties")]
    [Tooltip("mass in kilograms")]
    [SerializeField] private float massKg = 0.00745f;
    [Tooltip("diameter in meters")]
    [SerializeField] private float diameterMeters = 0.009f;
    [SerializeField] private float dragCoefficient = 0.295f;
    [Tooltip("muzzle velocity in m/s")]
    [SerializeField] private float muzzleVelocity = 350f;

    public string AmmoName => ammoName;
    public string Caliber => caliber;
    public float MassKg => massKg;
    public float Diameter => diameterMeters;
    public float DragCoefficient => dragCoefficient;
    public float MuzzleVelocity => muzzleVelocity;

    public float FrontalArea => Mathf.PI * Mathf.Pow(diameterMeters * 0.5f, 2f);
}
