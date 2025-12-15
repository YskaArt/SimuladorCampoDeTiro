using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/Weapon Pose")]
public class WeaponPoseData : ScriptableObject
{
    [Header("Idle Pose")]
    [SerializeField] private Vector3 idlePosition;
    [SerializeField] private Vector3 idleRotation;

    [Header("Aim Pose")]
    [SerializeField] private Vector3 aimPosition;
    [SerializeField] private Vector3 aimRotation;

    public Vector3 IdlePosition => idlePosition;
    public Quaternion IdleRotation => Quaternion.Euler(idleRotation);

    public Vector3 AimPosition => aimPosition;
    public Quaternion AimRotation => Quaternion.Euler(aimRotation);
}
