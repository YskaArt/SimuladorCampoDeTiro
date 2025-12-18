using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Mantiene presets y aplica selección en runtime. Al aplicar:
/// - instancia (o reemplaza) el modelo dentro del RecoilPivot bajo WeaponHolder
/// - conecta muzzle -> WeaponShooter.SetMuzzle(...)
/// - conecta sightPoint -> CameraAimController.SetSightPoint(...)
/// - conecta recoilAxis -> WeaponRecoilPivot.SetRecoilAxis(...)
/// - asigna WeaponData/AmmoData al WeaponShooter y SetAmmo en ShotManager.
/// Incluye logs y verificaciones para asegurar que el Panel UI y el modelo cambian correctamente.
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
        [Tooltip("Número de balas en el cargador / clip")]
        public int magazineSize = 10;
        [TextArea(2, 4)]
        public string description;
        public GameObject weaponModelPrefab;
    }

    [Header("Presets")]
    [SerializeField] private List<WeaponPreset> presets = new List<WeaponPreset>();

    [Header("References")]
    [Tooltip("WeaponShooter to assign weapon/ammo to at selection time (should be the single one in the WeaponHolder)")]
    [SerializeField] private WeaponShooter weaponShooter;
    [Tooltip("UI controller (updates texts & buttons)")]
    [SerializeField] private WeaponPanelUI panelUI;

    private int previewIndex = 0;
    private int activeIndex = -1;
    private bool selectionLocked = false;

    private void Awake()
    {
        // prefer inspector-assigned; fallback to Find (but prefer manual assign)
        if (weaponShooter == null)
            weaponShooter = FindObjectOfType<WeaponShooter>();

        if (weaponShooter == null)
            Debug.LogWarning("WeaponInventoryManager: weaponShooter not assigned and none found in scene. Assign the WeaponShooter on WeaponHolder in the inspector.");
    }

    private void Start()
    {
        // subscribe to ShotManager events (if exists)
        if (ShotManager.Instance != null)
        {
            ShotManager.Instance.OnFirstShot += OnFirstShotHandler;
            ShotManager.Instance.OnReloaded += OnReloadedHandler;
        }

        if (presets.Count > 0)
        {
            previewIndex = Mathf.Clamp(previewIndex, 0, presets.Count - 1);
            UpdatePanel();
            // auto-equip first preset (so scene is consistent)
            ApplyPresetToActive(previewIndex, autoEquip: true);
        }
        else
        {
            panelUI?.SetEmpty();
            Debug.LogWarning("WeaponInventoryManager: no presets configured.");
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
        if (selectionLocked)
        {
            Debug.Log("WeaponInventoryManager: selection locked until reload.");
            return;
        }

        ApplyPresetToActive(previewIndex, autoEquip: false);
    }

    /// <summary>
    /// Core: apply preset to runtime weapon shooter + instantiate model under RecoilPivot.
    /// Includes robust cleanup and runtime checks + logs to verify proper assignment.
    /// </summary>
    private void ApplyPresetToActive(int index, bool autoEquip)
    {
        if (index < 0 || index >= presets.Count) return;

        WeaponPreset p = presets[index];

        // 1️⃣ Asignar Weapon / Ammo
        if (weaponShooter != null)
            weaponShooter.SetWeaponReference(p.weaponData, p.ammoData);

        // 2️⃣ Munición
        ShotManager.Instance?.SetAmmo(p.magazineSize, p.magazineSize);

        // 3️⃣ Encontrar RecoilPivot
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

        // 4️⃣ LIMPIAR MODELO ANTERIOR (CLAVE)
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }

        // 5️⃣ Instanciar nuevo modelo
        if (p.weaponModelPrefab != null)
        {
            GameObject modelGO = Instantiate(p.weaponModelPrefab, parent, false);

            WeaponModelConnector connector = modelGO.GetComponent<WeaponModelConnector>();

            if (connector != null)
            {
                modelGO.transform.localPosition = connector.modelLocalPositionOffset;
                modelGO.transform.localEulerAngles = connector.modelLocalEulerOffset;

                if (connector.muzzle != null)
                {
                    Transform muzzleT = connector.muzzle;

                    // WeaponShooter
                    weaponShooter.SetMuzzle(muzzleT);

                    // AimLine
                    var aimLine = FindObjectOfType<AimLineController>();
                    if (aimLine != null)
                    {
                        aimLine.SetMuzzle(muzzleT);

                    }

                    if (p.ammoData != null)
                        aimLine.SetMuzzleVelocity(p.ammoData.MuzzleVelocity);
                }


                CameraAimController camAim = FindObjectOfType<CameraAimController>();
                if (camAim != null && connector.sightPoint != null)
                    camAim.SetSightPoint(connector.sightPoint);

                if (connector.recoilAxis != null)
                    recoilPivot.SetRecoilAxis(connector.recoilAxis);
            }
            else
            {
                Debug.LogWarning($"Weapon prefab {modelGO.name} has no WeaponModelConnector.");
            }
        }

        activeIndex = index;
        selectionLocked = false;

        UpdatePanel();

        Debug.Log($"Weapon equipped: {p.displayName}");
    }


    private void UpdatePanel()
    {
        if (panelUI == null)
            return;

        if (presets.Count == 0)
        {
            panelUI.SetEmpty();
            return;
        }

        var p = presets[previewIndex];
        bool isActive = (previewIndex == activeIndex);
        panelUI.UpdatePanel(p.displayName,
                          p.weaponType,
                          p.weaponData != null ? p.weaponData.weaponName : "N/A",
                          p.ammoData != null ? p.ammoData.AmmoName : "N/A",
                          p.magazineSize,
                          p.description,
                          isActive,
                          selectionLocked);
    }

    // Events from ShotManager
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

    // add presets at runtime if needed
    public void AddPreset(WeaponPreset preset)
    {
        presets.Add(preset);
        UpdatePanel();
    }
}
