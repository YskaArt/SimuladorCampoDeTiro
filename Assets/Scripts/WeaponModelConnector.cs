using UnityEngine;

/// <summary>
/// WeaponModelConnector
/// --------------------
/// Define puntos clave del modelo del arma para conectar:
/// - Muzzle (disparo)
/// - SightPoint (posición de cámara ADS)
/// - FrontSight (alineación real de miras)
/// - RecoilAxis (eje de retroceso)
/// - Offsets visuales del modelo
/// </summary>
public class WeaponModelConnector : MonoBehaviour
{
    [Header("Model Offsets")]
    public Vector3 modelLocalPositionOffset;
    public Vector3 modelLocalEulerOffset;

    [Header("Weapon References")]
    [Tooltip("Punto exacto de salida del proyectil")]
    public Transform muzzle;

    [Tooltip("Punto donde se posiciona la cámara al apuntar (ADS)")]
    public Transform sightPoint;

    [Tooltip("Punto de referencia del guión / mira frontal del arma")]
    public Transform frontSight;

    [Tooltip("Eje de rotación para el recoil")]
    public Transform recoilAxis;
}
