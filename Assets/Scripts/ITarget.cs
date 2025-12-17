using UnityEngine;

/// <summary>
/// Implementar en objetos que quieran reaccionar a impactos.
/// </summary>
public interface ITarget
{
    /// <summary>
    /// hit: RaycastHit info
    /// energyJ: kinetic energy in joules
    /// impactVelocity: world-space velocity vector at impact
    /// </summary>
    void OnHit(RaycastHit hit, float energyJ, Vector3 impactVelocity);
}
