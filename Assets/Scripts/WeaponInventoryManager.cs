using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mantiene presets y aplica selección en runtime:
/// - Instancia el modelo del arma
/// - Asigna muzzle, sightPoint, frontSight
/// - Configura WeaponShooter, AimLine, CameraAim, WeaponAimController
/// - Asigna SFX del preset al WeaponShooter
/// </summary>
public class WeaponInventoryManager : MonoBehaviour
{
    [Serializable]
    public class WeaponPreset
    {
        public string displayName = "Weapon";
        public string weaponType = "Pistol";
        public WeaponData weaponData;
        public AmmoData ammoData;
        public int magazineSize = 10;
        [TextArea(2, 4)] public string description;
        public GameObject weaponModelPrefab;

        [Header("Audio")]
        public AudioClip shotSFX;
        public AudioClip emptySFX;
    }

    [Header("Presets")]
    [SerializeField] private List<WeaponPreset> presets = new();

    [Header("References")]
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private WeaponPanelUI panelUI;

    private int previewIndex = 0;
    private int activeIndex = -1;
    private bool selectionLocked = false;

    private void Awake()
    {
        if (weaponShooter == null)
            weaponShooter = FindObjectOfType<WeaponShooter>();
    }

    private void Start()
    {
        if (ShotManager.Instance != null)
        {
            ShotManager.Instance.OnFirstShot += OnFirstShotHandler;
            ShotManager.Instance.OnReloaded += OnReloadedHandler;
        }

        if (presets.Count > 0)
        {
            previewIndex = Mathf.Clamp(previewIndex, 0, presets.Count - 1);
            UpdatePanel();
            ApplyPresetToActive(previewIndex, autoEquip: true);
        }
        else
        {
            panelUI?.SetEmpty();
        }
    }

    private void OnDestroy()
    {
        if (ShotManager.Instance != null)
        {
            ShotManager.Instance.OnFirstShot -= OnFirstShotHandler;
            ShotManager.Instance.OnReloaded -= OnReloadedHandler;
        }
    }

    // UI navigation
    public void NextPreview()
    {
        if (selectionLocked || presets.Count == 0) return;
        previewIndex = (previewIndex + 1) % presets.Count;
        UpdatePanel();
    }

    public void PrevPreview()
    {
        if (selectionLocked || presets.Count == 0) return;
        previewIndex = (previewIndex - 1 + presets.Count) % presets.Count;
        UpdatePanel();
    }

    public void SelectPreviewAsActive()
    {
        if (selectionLocked) return;
        ApplyPresetToActive(previewIndex, autoEquip: false);
    }

    // Core equip logic
    private void ApplyPresetToActive(int index, bool autoEquip)
    {
        if (index < 0 || index >= presets.Count) return;

        WeaponPreset p = presets[index];

        // 1) Weapon / Ammo
        if (weaponShooter != null)
            weaponShooter.SetWeaponReference(p.weaponData, p.ammoData);

        ShotManager.Instance?.SetAmmo(p.magazineSize, p.magazineSize);

        // 2) Recoil pivot
        WeaponRecoilPivot recoilPivot =
            weaponShooter != null
                ? weaponShooter.GetComponentInChildren<WeaponRecoilPivot>(true)
                : FindObjectOfType<WeaponRecoilPivot>();

        if (recoilPivot == null)
        {
            Debug.LogError("WeaponInventoryManager: No WeaponRecoilPivot found.");
            return;
        }

        Transform parent = recoilPivot.transform;

        // 3) Clear previous model
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        // 4) Instantiate model
        if (p.weaponModelPrefab != null)
        {
            GameObject modelGO = Instantiate(p.weaponModelPrefab, parent, false);
            WeaponModelConnector connector = modelGO.GetComponent<WeaponModelConnector>();

            if (connector == null)
            {
                Debug.LogWarning($"Weapon prefab {modelGO.name} has no WeaponModelConnector.");
                // still allow SFX assignment below even if connector missing
            }
            else
            {
                // Model offsets
                modelGO.transform.localPosition = connector.modelLocalPositionOffset;
                modelGO.transform.localEulerAngles = connector.modelLocalEulerOffset;

                // 5) Muzzle
                if (connector.muzzle != null)
                    weaponShooter.SetMuzzle(connector.muzzle);

                // 6) Aim line (trayectoria)
                AimLineController aimLine = FindObjectOfType<AimLineController>();
                if (aimLine != null && connector.muzzle != null)
                {
                    aimLine.SetMuzzle(connector.muzzle);
                    if (p.ammoData != null)
                        aimLine.SetMuzzleVelocity(p.ammoData.MuzzleVelocity);
                }

                // 7) CameraAimController (posición base ADS)
                CameraAimController camAim = FindObjectOfType<CameraAimController>();
                if (camAim != null && connector.sightPoint != null)
                {
                    camAim.SetSightPoint(connector.sightPoint);
                }

                // 8) WeaponAimController (ADS real)
                WeaponAimController weaponAim = Camera.main?.GetComponent<WeaponAimController>();
                if (weaponAim != null)
                {
                    weaponAim.SetSightPoints(
                        connector.sightPoint,
                        connector.frontSight
                    );
                }

                // 9) Recoil axis
                if (connector.recoilAxis != null)
                    recoilPivot.SetRecoilAxis(connector.recoilAxis);
            }

            // 10) Assign audio SFX to WeaponShooter (important)
            if (weaponShooter != null)
            {
                weaponShooter.SetWeaponAudio(p.shotSFX, p.emptySFX);
            }
        }
        else
        {
            // If no model prefab we still want to assign SFX so weapon has feedback.
            if (weaponShooter != null)
                weaponShooter.SetWeaponAudio(p.shotSFX, p.emptySFX);
        }

        activeIndex = index;
        selectionLocked = false;
        UpdatePanel();

        Debug.Log($"Weapon equipped: {p.displayName}");
    }

    // UI update
    private void UpdatePanel()
    {
        if (panelUI == null) return;
        if (presets.Count == 0)
        {
            panelUI.SetEmpty();
            return;
        }

        WeaponPreset p = presets[previewIndex];
        bool isActive = previewIndex == activeIndex;

        panelUI.UpdatePanel(
            p.displayName,
            p.weaponType,
            p.weaponData != null ? p.weaponData.weaponName : "N/A",
            p.ammoData != null ? p.ammoData.AmmoName : "N/A",
            p.magazineSize,
            p.description,
            isActive,
            selectionLocked
        );
    }

    // ShotManager events
    private void OnFirstShotHandler()
    {
        selectionLocked = true;
        UpdatePanel();
    }

    private void OnReloadedHandler()
    {
        selectionLocked = false;
        UpdatePanel();
    }

    public void AddPreset(WeaponPreset preset)
    {
        presets.Add(preset);
        UpdatePanel();
    }
}
