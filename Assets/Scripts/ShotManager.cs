using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ShotManager
/// - Registra disparos (RegisterShotFired)
/// - Recibe hits/misses (RegisterHit / RegisterMiss)
/// - Mantiene registros por sesión y guarda JSON/TXT/PNG
/// - Bloquea aim al quedarse sin munición
/// - Ahora registra si el target se movía en el momento del disparo (targetMovingAtShot)
/// </summary>
public class ShotManager : MonoBehaviour
{
    public static ShotManager Instance { get; private set; }

    [Header("Ammo")]
    [SerializeField] private int maxAmmo = 10;
    private int currentAmmo;

    [Header("UI")]
    [SerializeField] private UnityEngine.UI.Text ammoText;
    [SerializeField] private GameObject reloadPrompt;

    [Header("Screenshot/Preview")]
    [SerializeField] private Camera previewCamera;
    [SerializeField] private UnityEngine.UI.RawImage previewRawImage;

    [Header("Markers")]
    [SerializeField] private GameObject hitMarkerPrefab;
    [SerializeField] private Transform markersRoot;
    [SerializeField] private float markerOffset = 0.01f;
    private readonly List<GameObject> markers = new();

    [Header("Behavior")]
    [SerializeField] private float lockDelaySeconds = 1.5f;
    private Coroutine lockCoroutine;

    private InputAction reloadAction;

    // session records
    [Serializable]
    public class ShotRecord
    {
        public int shotId;
        public float time;

        public float targetDistanceMeters;

        public Vector3 predictedPoint; // linea de mira
        public Vector3 hitPoint;

        public bool hit;
        public string targetName;

        public float deviationMeters;
        public float deviationPercent;

        public float angularErrorDeg;
        public float angularErrorMOA;
        public float angularErrorMrad;

        public Vector2 offsetMeters; // x = derecha, y = arriba

        public float energyJ;

        // NEW: whether the target was moving at the moment this shot was fired
        public bool targetMovingAtShot;

        public string explanation;
    }

    [Serializable]
    public class SessionRecord
    {
        public string sessionName;
        public string dateTime;

        public string weaponName;
        public float weaponMassKg;
        public float barrelLengthMeters;

        public string ammoName;
        public float muzzleVelocity;

        public int magazineSize;

        public bool targetMoved; // whether target moved at any time in session

        public List<ShotRecord> shots = new();
        public string screenshotFileName;
    }

    private readonly List<ShotRecord> shotRecords = new();
    private int nextShotId = 0;

    // EVENTS
    public event Action OnFirstShot;
    public event Action OnReloaded;
    public int CurrentAmmo => currentAmmo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        currentAmmo = maxAmmo;

        reloadAction = new InputAction("Reload", InputActionType.Button, "<Keyboard>/r");
        reloadAction.Enable();

        if (previewCamera != null && previewRawImage != null && previewCamera.targetTexture == null)
        {
            RenderTexture rt = new RenderTexture(512, 512, 16);
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
        if (currentAmmo <= 0 && reloadAction.WasPressedThisFrame())
        {
            SaveSession();
            Reload();
        }
    }

    // -------------------- Public API --------------------

    public bool CanFire() => currentAmmo > 0;

    public void SetAmmo(int newMax, int newCurrent)
    {
        maxAmmo = Mathf.Max(1, newMax);
        currentAmmo = Mathf.Clamp(newCurrent, 0, maxAmmo);
        UpdateUI();
    }

    public void NotifyShotFired()
    {
        currentAmmo = Mathf.Max(0, currentAmmo - 1);
        UpdateUI();

        if (currentAmmo == 0)
        {
            if (lockCoroutine != null) StopCoroutine(lockCoroutine);
            lockCoroutine = StartCoroutine(DelayedLockAfterLastShot());
        }
    }

    // RegisterShotFired: devuelve shotId y captura si el target estaba moviéndose
    public int RegisterShotFired(Vector3 muzzlePos, Vector3 muzzleDir, float muzzleVelocity)
    {
        int id = nextShotId++;

        float targetDist = 0f;
        var targetPl = FindObjectOfType<TargetPlacementController>();
        if (targetPl != null) targetDist = targetPl.GetCurrentDistance();

        Vector3 predicted = muzzlePos + muzzleDir.normalized * targetDist;

        // determine whether target was moving now
        bool targetMovingNow = TargetMover.Instance != null && TargetMover.Instance.IsMoving;

        ShotRecord rec = new ShotRecord
        {
            shotId = id,
            time = Time.time,
            targetDistanceMeters = targetDist,
            predictedPoint = predicted,
            targetMovingAtShot = targetMovingNow
        };

        shotRecords.Add(rec);

        if (shotRecords.Count == 1) OnFirstShot?.Invoke();

        return id;
    }

    // RegisterHit: llamada por Projectile (signature con 3 args)
    public void RegisterHit(int shotId, RaycastHit hit, float energyJ)
    {
        var rec = shotRecords.Find(s => s.shotId == shotId);
        if (rec == null) { Debug.LogWarning($"ShotManager.RegisterHit unknown id {shotId}"); return; }

        rec.hit = true;
        rec.hitPoint = hit.point;
        rec.targetName = hit.collider != null ? hit.collider.name : "Unknown";
        rec.energyJ = energyJ;

        ComputeDeviationAndAngles(rec, hit.point);

        // spawn marker
        SpawnMarker(hit);

        rec.explanation = BuildExplanation(rec);
        Debug.Log($"ShotManager: Hit id={shotId} dev={rec.deviationMeters:F3} m ang={rec.angularErrorDeg:F2}° targetMoving={rec.targetMovingAtShot}");
    }

    public void RegisterMiss(int shotId, Vector3 finalPos)
    {
        var rec = shotRecords.Find(s => s.shotId == shotId);
        if (rec == null) { Debug.LogWarning($"ShotManager.RegisterMiss unknown id {shotId}"); return; }

        rec.hit = false;
        rec.hitPoint = finalPos;
        rec.targetName = "Miss";
        rec.energyJ = 0f;

        ComputeDeviationAndAngles(rec, finalPos);

        rec.explanation = BuildExplanation(rec);
        Debug.Log($"ShotManager: Miss id={shotId} dev={rec.deviationMeters:F3} m ang={rec.angularErrorDeg:F2}° targetMoving={rec.targetMovingAtShot}");
    }

    // -------------------- Calculation helpers --------------------

    private void ComputeDeviationAndAngles(ShotRecord rec, Vector3 actualPoint)
    {
        rec.deviationMeters = Vector3.Distance(rec.predictedPoint, actualPoint);
        rec.deviationPercent = rec.targetDistanceMeters > 0f ? (rec.deviationMeters / rec.targetDistanceMeters) * 100f : 0f;

        // predicted and actual directions using camera/weapon origin
        Vector3 origin = FindObjectOfType<WeaponViewController>()?.transform.position ?? Vector3.zero;
        Vector3 dirPred = (rec.predictedPoint - origin).normalized;
        Vector3 dirReal = (actualPoint - origin).normalized;

        float angleDeg = Vector3.Angle(dirPred, dirReal);
        rec.angularErrorDeg = angleDeg;
        rec.angularErrorMOA = angleDeg * 60f;
        rec.angularErrorMrad = angleDeg * Mathf.Deg2Rad * 1000f;

        Vector3 right = Vector3.Cross(Vector3.up, dirPred).normalized;
        Vector3 up = Vector3.Cross(dirPred, right).normalized;
        Vector3 delta = actualPoint - rec.predictedPoint;

        rec.offsetMeters = new Vector2(Vector3.Dot(delta, right), Vector3.Dot(delta, up));
    }

    private string BuildExplanation(ShotRecord s)
    {
        string hitTxt = s.hit ? $"Impactó en '{s.targetName}'" : "No impactó (Miss)";
        string targetMovingTxt = s.targetMovingAtShot ? " (Target moving)" : "";
        return $"{hitTxt}{targetMovingTxt} a {s.targetDistanceMeters:F1} m | Desviación: {s.deviationMeters:F3} m ({s.deviationPercent:F2}%) | "
             + $"Offset H:{s.offsetMeters.x:F3} m V:{s.offsetMeters.y:F3} m | "
             + $"Error angular: {s.angularErrorDeg:F2}° ({s.angularErrorMOA:F1} MOA) | Energía: {s.energyJ:F1} J";
    }

    // -------------------- Save / Export --------------------

    private void SaveSession()
    {
        if (shotRecords.Count == 0)
        {
            Debug.Log("ShotManager: no shots to save.");
            return;
        }

        var ws = FindObjectOfType<WeaponShooter>();

        SessionRecord session = new SessionRecord
        {
            sessionName = $"session_{DateTime.Now:yyyyMMdd_HHmmss}",
            dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            weaponName = ws != null ? ws.WeaponName : "Unknown",
            weaponMassKg = ws != null ? ws.WeaponMassKg : 0f,
            barrelLengthMeters = ws != null ? ws.BarrelLengthMeters : 0f,
            ammoName = ws != null ? ws.AmmoName : "Unknown",
            muzzleVelocity = ws != null ? ws.AmmoVelocity : 0f,
            magazineSize = maxAmmo,
            shots = new List<ShotRecord>(shotRecords),
            targetMoved = TargetMover.Instance != null ? TargetMover.Instance.WasEverMoved : false,
            screenshotFileName = ""
        };

        string basePath = Application.persistentDataPath;

        // Save PNG first so we can include the filename in JSON
        if (previewCamera != null && previewCamera.targetTexture != null)
        {
            RenderTexture rt = previewCamera.targetTexture;
            RenderTexture current = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            tex.Apply();

            byte[] png = tex.EncodeToPNG();
            string pngName = session.sessionName + "_target.png";
            File.WriteAllBytes(Path.Combine(basePath, pngName), png);
            session.screenshotFileName = pngName;

            Destroy(tex);
            RenderTexture.active = current;
        }

        // Save JSON (includes screenshotFileName now)
        string jsonPath = Path.Combine(basePath, session.sessionName + ".json");
        File.WriteAllText(jsonPath, JsonUtility.ToJson(session, true));

        // Save summary txt
        string summaryPath = Path.Combine(basePath, session.sessionName + "_summary.txt");
        using (StreamWriter sw = new StreamWriter(summaryPath))
        {
            sw.WriteLine($"Weapon: {session.weaponName}");
            sw.WriteLine($"Ammo: {session.ammoName}");
            sw.WriteLine($"Date: {session.dateTime}");
            sw.WriteLine($"Target moved during session: {(session.targetMoved ? "YES" : "NO")}");
            sw.WriteLine("");

            foreach (var s in session.shots)
                sw.WriteLine(s.explanation);

            if (!string.IsNullOrEmpty(session.screenshotFileName))
            {
                sw.WriteLine("");
                sw.WriteLine($"Screenshot: {session.screenshotFileName}");
            }
        }

        Debug.Log($"ShotManager: Session saved to {basePath} as {session.sessionName}");
    }

    // -------------------- Misc helpers --------------------

    private void Reload()
    {
        ClearMarkers();
        shotRecords.Clear();
        nextShotId = 0;

        currentAmmo = maxAmmo;
        UpdateUI();

        FindObjectOfType<WeaponViewController>()?.ForceExitAimLock(false);
        if (reloadPrompt != null) reloadPrompt.SetActive(false);

        OnReloaded?.Invoke();
    }

    private void ClearMarkers()
    {
        foreach (var m in markers)
            if (m != null) Destroy(m);
        markers.Clear();
    }

    private void UpdateUI()
    {
        if (ammoText != null)
            ammoText.text = $"Ammo: {currentAmmo}/{maxAmmo}";
    }

    private System.Collections.IEnumerator DelayedLockAfterLastShot()
    {
        yield return new WaitForSeconds(lockDelaySeconds);
        FindObjectOfType<WeaponViewController>()?.ForceExitAimLock(true);
        if (reloadPrompt != null) reloadPrompt.SetActive(true);
        lockCoroutine = null;
    }

    private void SpawnMarker(RaycastHit hit)
    {
        if (hitMarkerPrefab == null) return;

        Vector3 pos = hit.point + hit.normal * markerOffset;
        GameObject m = Instantiate(hitMarkerPrefab, pos, Quaternion.LookRotation(hit.normal));
        if (hit.collider != null)
            m.transform.SetParent(hit.collider.transform, true);
        else if (markersRoot != null)
            m.transform.SetParent(markersRoot, true);

        markers.Add(m);
    }
}
