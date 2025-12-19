using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Coloca el Target a una distancia exacta en metros (25, 50, 100).
/// Compatible con TMP_Dropdown.
/// </summary>
public class TargetPlacementController : MonoBehaviour
{
    [Header("References")]
    public Transform originTransform;
    public Transform targetTransform;


    [Header("Distances (meters)")]
    public float[] presetDistances = { 25f, 50f, 100f };

    [Header("Placement")]
    public float targetHeight = 1.5f;
    public bool snapToSurface = true;
    public LayerMask surfaceMask = ~0;

    [Header("Rotation")]
    [Tooltip("Typical target model needs X = -90°")]
    public bool applyFixedXRotation = true;
    public float fixedXRotation = -90f;
    public Vector3 extraEulerOffset;

    [Header("UI (TMP)")]
    public TMP_Dropdown distanceDropdown;
    public TMP_Text currentDistanceText;

    private float currentDistance;

    private void Awake()
    {
        if (originTransform == null && Camera.main != null)
            originTransform = Camera.main.transform;

        if (distanceDropdown != null)
        {
            distanceDropdown.ClearOptions();
            var options = new List<string>();
            foreach (float d in presetDistances)
                options.Add($"{d} m");

            distanceDropdown.AddOptions(options);
            distanceDropdown.onValueChanged.AddListener(SetDistanceIndex);
        }


        if (presetDistances.Length > 0)
            PlaceAtMeters(presetDistances[0]);
    }

    public void SetDistanceIndex(int index)
    {
        index = Mathf.Clamp(index, 0, presetDistances.Length - 1);
        PlaceAtMeters(presetDistances[index]);
    }

    public void PlaceAtMeters(float meters)
    {
        if (originTransform == null || targetTransform == null)
            return;

        currentDistance = meters;

        Vector3 origin = originTransform.position;
        Vector3 dir = originTransform.forward;
        Vector3 desired = origin + dir * meters;

        Vector3 finalPos = desired;

        if (snapToSurface)
        {
            if (Physics.Raycast(origin, dir, out RaycastHit hit, meters + 1f, surfaceMask))
            {
                finalPos = hit.point + hit.normal * targetHeight;
            }
            else
            {
                finalPos.y = origin.y + targetHeight;
            }
        }
        else
        {
            finalPos.y = origin.y + targetHeight;
        }

        targetTransform.position = finalPos;
        TargetMover.Instance?.RebindInitialPosition();
        // yaw only (horizontal look)
        Vector3 lookDir = origin - finalPos;
        lookDir.y = 0f;

        float yaw = lookDir.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(lookDir).eulerAngles.y
            : targetTransform.eulerAngles.y;

        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        if (applyFixedXRotation)
            rot *= Quaternion.Euler(fixedXRotation, 0f, 0f);

        rot *= Quaternion.Euler(extraEulerOffset);
        targetTransform.rotation = rot;

        UpdateUIText();
    }

    private void UpdateUIText()
    {
        if (currentDistanceText != null)
            currentDistanceText.text = $"{currentDistance:0} m";
    }

    public float GetCurrentDistance() => currentDistance;
}
