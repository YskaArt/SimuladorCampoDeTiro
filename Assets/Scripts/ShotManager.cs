using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Versión extendida de ShotManager:
/// - expos eventos OnFirstShot, OnMagazineEmpty, OnReloaded
/// - SetAmmo(max, current)
/// </summary>
public class ShotManager : MonoBehaviour
{
    public static ShotManager Instance { get; private set; }

    [Header("Ammo (default)")]
    [SerializeField] private int maxAmmo = 10;
    private int currentAmmo;

    [Header("UI")]
    [SerializeField] private Text ammoText;
    [SerializeField] private GameObject reloadPrompt;
    [SerializeField] private RawImage previewRawImage;
    [SerializeField] private Camera previewCamera;

    [Header("Markers")]
    [SerializeField] private GameObject hitMarkerPrefab;
    [SerializeField] private Transform markersRoot;
    private List<GameObject> markers = new List<GameObject>();

    [Header("References")]
    [SerializeField] private WeaponViewController weaponView;
    [SerializeField] private float markerOffset = 0.01f;

    [Header("Out of Ammo Behavior")]
    [SerializeField][Range(0.1f, 5f)] private float lockDelaySeconds = 1.5f;

    private InputAction reloadAction;

    // events
    public event Action OnFirstShot;
    public event Action OnMagazineEmpty;
    public event Action OnReloaded;

    // Hit record (same as antes)
    public struct HitRecord
    {
        public float time;
        public Vector3 worldPos;
        public Vector3 localPos;
        public float energyJ;
        public string targetName;
    }
    private List<HitRecord> hitRecords = new List<HitRecord>();

    private Coroutine lockCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        currentAmmo = maxAmmo;

        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        reloadAction.Enable();

        if (previewCamera != null && previewRawImage != null && previewCamera.targetTexture == null)
        {
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
        if (lockCoroutine != null) StopCoroutine(lockCoroutine);
    }

    private void Update()
    {
        if (currentAmmo <= 0)
        {
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

    /// <summary>
    /// Set the magazine values (max and current). Call when weapon is equipped.
    /// </summary>
    public void SetAmmo(int newMax, int newCurrent)
    {
        maxAmmo = Mathf.Max(0, newMax);
        currentAmmo = Mathf.Clamp(newCurrent, 0, maxAmmo);
        UpdateUI();
    }

    public void NotifyShotFired()
    {
        // detect first shot (when previous was full mag)
        bool wasFull = (currentAmmo == maxAmmo);

        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        UpdateUI();

        if (wasFull)
            OnFirstShot?.Invoke();

        if (currentAmmo == 0)
        {
            OnMagazineEmpty?.Invoke();

            if (lockCoroutine != null) StopCoroutine(lockCoroutine);
            lockCoroutine = StartCoroutine(DelayedLockAfterLastShot());
        }
    }

    private System.Collections.IEnumerator DelayedLockAfterLastShot()
    {
        yield return new WaitForSeconds(lockDelaySeconds);

        if (weaponView != null)
            weaponView.ForceExitAimLock(true);
        if (reloadPrompt != null)
            reloadPrompt.SetActive(true);

        lockCoroutine = null;
    }

    private void UpdateUI()
    {
        if (ammoText != null)
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }

    public void RegisterHit(RaycastHit hit, float energyJ, Vector3 impactVelocity)
    {
        if (hitMarkerPrefab != null)
        {
            Vector3 spawnPos = hit.point + hit.normal * markerOffset;
            GameObject marker = Instantiate(hitMarkerPrefab, spawnPos, Quaternion.LookRotation(hit.normal));
            if (hit.collider != null && hit.collider.transform != null)
                marker.transform.SetParent(hit.collider.transform, true);
            else if (markersRoot != null)
                marker.transform.SetParent(markersRoot, true);
            markers.Add(marker);
        }

        HitRecord rec = new HitRecord
        {
            time = Time.time,
            worldPos = hit.point,
            localPos = (hit.collider != null) ? hit.collider.transform.InverseTransformPoint(hit.point) : Vector3.zero,
            energyJ = energyJ,
            targetName = hit.collider != null ? hit.collider.name : "Unknown"
        };
        hitRecords.Add(rec);

        Debug.Log($"ShotManager: Registered hit on {rec.targetName} E={rec.energyJ:F2}J at {rec.worldPos}");
    }

    public void Reload()
    {
        if (lockCoroutine != null)
        {
            StopCoroutine(lockCoroutine);
            lockCoroutine = null;
        }

        ClearMarkers();
        hitRecords.Clear();

        currentAmmo = maxAmmo;
        UpdateUI();

        if (weaponView != null)
            weaponView.ForceExitAimLock(false);

        if (reloadPrompt != null)
            reloadPrompt.SetActive(false);

        OnReloaded?.Invoke();
    }

    public void ClearMarkers()
    {
        for (int i = 0; i < markers.Count; i++)
            if (markers[i] != null) Destroy(markers[i]);
        markers.Clear();
    }

    public IReadOnlyList<HitRecord> GetHitRecords() => hitRecords.AsReadOnly();
}
