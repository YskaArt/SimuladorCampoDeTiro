using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ShotManager : MonoBehaviour
{
    public static ShotManager Instance { get; private set; }

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 10;
    private int currentAmmo;

    [Header("UI")]
    [SerializeField] private Text ammoText;             // asignar Text UI
    [SerializeField] private GameObject reloadPrompt;   // panel / text "Press R to reload"
    [SerializeField] private RawImage previewRawImage;  // donde se mostrará la camera
    [SerializeField] private Camera previewCamera;      // camera que apunta al objetivo (optional)

    [Header("Markers")]
    [SerializeField] private GameObject hitMarkerPrefab; // pequeño prefab (sphere) para marcar impactos
    [SerializeField] private Transform markersRoot;      // opcional parent (e.g., target root)
    private List<GameObject> markers = new List<GameObject>();

    [Header("References")]
    [SerializeField] private WeaponViewController weaponView; // para bloquear aim
    [SerializeField] private float markerOffset = 0.01f; // to avoid z-fight

    private InputAction reloadAction;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        currentAmmo = maxAmmo;

        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        reloadAction.Enable();

        if (previewCamera != null && previewRawImage != null && previewCamera.targetTexture == null)
        {
            // create a RenderTexture for preview (small)
            int w = 256, h = 256;
            RenderTexture rt = new RenderTexture(w, h, 16);
            rt.Create();
            previewCamera.targetTexture = rt;
            previewRawImage.texture = rt;
        }

        UpdateUI();
        if (reloadPrompt != null) reloadPrompt.SetActive(false);
    }

    private void OnDestroy()
    {
        reloadAction.Disable();
    }

    private void Update()
    {
        if (currentAmmo <= 0)
        {
            if (reloadPrompt != null) reloadPrompt.SetActive(true);

            if (reloadAction.WasPressedThisFrame())
            {
                Reload();
            }
        }
    }

    public bool CanFire()
    {
        return currentAmmo > 0;
    }

    public void NotifyShotFired()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        UpdateUI();

        if (currentAmmo == 0)
        {
            // out of ammo: force exit aim and block aiming
            if (weaponView != null)
            {
                weaponView.ForceExitAimLock(true);
            }
            if (reloadPrompt != null) reloadPrompt.SetActive(true);
        }
    }

    private void UpdateUI()
    {
        if (ammoText != null)
        {
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
        }
    }

    public void RegisterHit(RaycastHit hit)
    {
        // spawn marker slightly off the surface to avoid z-fighting, parent to hit.transform
        if (hitMarkerPrefab == null) return;

        Vector3 spawnPos = hit.point + hit.normal * markerOffset;
        GameObject marker = Instantiate(hitMarkerPrefab, spawnPos, Quaternion.LookRotation(hit.normal));
        // parent
        if (hit.collider != null && hit.collider.transform != null)
            marker.transform.SetParent(hit.collider.transform, true);
        else if (markersRoot != null)
            marker.transform.SetParent(markersRoot, true);

        markers.Add(marker);

        // You may want to update some UI (e.g., mark coordinates)
    }

    public void Reload()
    {
        // clear markers
        ClearMarkers();

        currentAmmo = maxAmmo;
        UpdateUI();

        if (weaponView != null)
        {
            weaponView.ForceExitAimLock(false); // unlock aiming
        }

        if (reloadPrompt != null) reloadPrompt.SetActive(false);
    }

    public void ClearMarkers()
    {
        for (int i = 0; i < markers.Count; i++)
        {
            if (markers[i] != null) Destroy(markers[i]);
        }
        markers.Clear();
    }
}
