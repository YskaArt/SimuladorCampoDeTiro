using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mantiene presets y aplica selección en runtime (modelo, muzzle, sight, recoil axis, weapon/ ammo).
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
    }

    [SerializeField] private List<WeaponPreset> presets = new();
    [SerializeField] private WeaponShooter weaponShooter;
    [SerializeField] private WeaponPanelUI panelUI;

    private int previewIndex = 0;
    private int activeIndex = -1;
    private bool selectionLocked = false;

    private void Awake()
    {
        if (weaponShooter == null) weaponShooter = FindObjectOfType<WeaponShooter>();
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
        else panelUI?.SetEmpty();
    }

    private void OnDestroy()
    {
        if (ShotManager.Instance != null)
        {
            ShotManager.Instance.OnFirstShot -= OnFirstShotHandler;
            ShotManager.Instance.OnReloaded -= OnReloadedHandler;
        }
    }

    public void NextPreview() { if (selectionLocked || presets.Count == 0) return; previewIndex = (previewIndex + 1) % presets.Count; UpdatePanel(); }
    public void PrevPreview() { if (selectionLocked || presets.Count == 0) return; previewIndex = (previewIndex - 1 + presets.Count) % presets.Count; UpdatePanel(); }
    public void SelectPreviewAsActive() { if (selectionLocked) return; ApplyPresetToActive(previewIndex, autoEquip: false); }

    private void ApplyPresetToActive(int index, bool autoEquip)
    {
        if (index < 0 || index >= presets.Count) return;
        var p = presets[index];
        if (weaponShooter != null) weaponShooter.SetWeaponReference(p.weaponData, p.ammoData);
        ShotManager.Instance?.SetAmmo(p.magazineSize, p.magazineSize);

        var recoilPivot = weaponShooter != null ? weaponShooter.GetComponentInChildren<WeaponRecoilPivot>(true) : FindObjectOfType<WeaponRecoilPivot>();
        if (recoilPivot == null) { Debug.LogError("No WeaponRecoilPivot found."); return; }

        var parent = recoilPivot.transform;
        for (int i = parent.childCount - 1; i >= 0; i--) Destroy(parent.GetChild(i).gameObject);

        if (p.weaponModelPrefab != null)
        {
            GameObject modelGO = Instantiate(p.weaponModelPrefab, parent, false);
            var connector = modelGO.GetComponent<WeaponModelConnector>();
            if (connector != null)
            {
                modelGO.transform.localPosition = connector.modelLocalPositionOffset;
                modelGO.transform.localEulerAngles = connector.modelLocalEulerOffset;

                if (connector.muzzle != null) weaponShooter.SetMuzzle(connector.muzzle);

                var aimLine = FindObjectOfType<AimLineController>();
                if (aimLine != null && connector.muzzle != null)
                {
                    aimLine.SetMuzzle(connector.muzzle);
                    if (p.ammoData != null) aimLine.SetMuzzleVelocity(p.ammoData.MuzzleVelocity);
                }

                var camAim = FindObjectOfType<CameraAimController>();
                if (camAim != null && connector.sightPoint != null) camAim.SetSightPoint(connector.sightPoint);
                if (connector.recoilAxis != null) recoilPivot.SetRecoilAxis(connector.recoilAxis);
            }
            else Debug.LogWarning($"Weapon prefab {modelGO.name} has no WeaponModelConnector.");
        }

        activeIndex = index;
        selectionLocked = false;
        UpdatePanel();
        Debug.Log($"Weapon equipped: {p.displayName}");
    }

    private void UpdatePanel()
    {
        if (panelUI == null) return;
        if (presets.Count == 0) { panelUI.SetEmpty(); return; }
        var p = presets[previewIndex];
        bool isActive = previewIndex == activeIndex;
        panelUI.UpdatePanel(p.displayName, p.weaponType,
            p.weaponData != null ? p.weaponData.weaponName : "N/A",
            p.ammoData != null ? p.ammoData.AmmoName : "N/A",
            p.magazineSize, p.description, isActive, selectionLocked);
    }

    private void OnFirstShotHandler() { selectionLocked = true; UpdatePanel(); }
    private void OnReloadedHandler() { selectionLocked = false; UpdatePanel(); }

    public void AddPreset(WeaponPreset preset) { presets.Add(preset); UpdatePanel(); }
}
