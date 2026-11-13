using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Alert system
    private float alertTimer = 0f;
    private string alertMessage = "";

    // Cached GUI styles
    private GUIStyle labelBold;
    private GUIStyle alertStyle;
    private GUIStyle popupStyle;
    private GUIStyle hotbarBtn;
    private bool stylesReady = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowWarningAlert(string message, float duration = 4f)
    {
        alertMessage = message;
        alertTimer = duration;
    }

    // Arayuz dikdortgenleri (OnGUI koordinati, ust-sol orijin); fare bunlarin ustundeyse oyun alanina tiklanmaz
    private Rect hudRect, hotbarRect, popupRect;

    public bool IsPointerOverUI(Vector3 mousePos)
    {
        Vector2 p = new Vector2(mousePos.x, Screen.height - mousePos.y);
        return hudRect.Contains(p) || hotbarRect.Contains(p) || popupRect.Contains(p);
    }

    // ─── OnGUI ──────────────────────────────────────────────────
    void OnGUI()
    {
        popupRect = Rect.zero;
        EnsureStyles();
        DrawTopHUD();
        DrawBottomHotbar();
        DrawTurretInteractPopup();
        DrawAlert();
    }

    // ─── 1. TOP HUD ─────────────────────────────────────────────
    void DrawTopHUD()
    {
        int scrap = GameEconomy.Instance != null ? GameEconomy.Instance.currentScrap : 0;
        int coreHP = GameEconomy.Instance != null ? GameEconomy.Instance.coreHealth : 100;
        int score = GameEconomy.Instance != null ? GameEconomy.Instance.totalScore : 0;
        int pending = GameEconomy.Instance != null ? GameEconomy.Instance.wavePendingScore : 0;
        int wave = WaveManager.Instance != null ? WaveManager.Instance.currentWaveIndex : 1;
        bool prep = WaveManager.Instance != null && WaveManager.Instance.isPrepPhase;
        float prepLeft = WaveManager.Instance != null ? WaveManager.Instance.prepTimer : 0f;

        hudRect = new Rect(8, 8, Screen.width - 16, 52);
        GUI.Box(hudRect, GUIContent.none);
        GUILayout.BeginArea(new Rect(18, 14, Screen.width - 36, 42));
        GUILayout.BeginHorizontal();

        // Scrap
        labelBold.normal.textColor = new Color(1f, 0.85f, 0.2f);
        GUILayout.Label($"HURDA: {scrap}", labelBold, GUILayout.Width(160));

        // Core HP — goes red when low
        labelBold.normal.textColor = coreHP > 30 ? new Color(0.3f, 1f, 0.45f) : Color.red;
        GUILayout.Label($"ÇEKIRDEK: {coreHP}/100", labelBold, GUILayout.Width(190));

        // Wave + prep countdown
        labelBold.normal.textColor = Color.cyan;
        string waveText = prep
            ? $"DALGA: {wave}/7   Hazirlik: {Mathf.CeilToInt(prepLeft)}s  [SPACE: Basla]"
            : $"DALGA: {wave}/7   SAVAS SURUYOR!";
        GUILayout.Label(waveText, labelBold, GUILayout.Width(400));

        // Score
        labelBold.normal.textColor = Color.white;
        string scoreText = pending > 0 ? $"SKOR: {score} (+{pending})" : $"SKOR: {score}";
        GUILayout.Label(scoreText, labelBold);

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    // ─── 2. BOTTOM HOTBAR ───────────────────────────────────────
    void DrawBottomHotbar()
    {
        float barW = 740f;
        float barH = 68f;
        float bx = (Screen.width - barW) * 0.5f;
        float by = Screen.height - barH - 12f;

        hotbarRect = new Rect(bx, by, barW, barH);
        GUI.Box(hotbarRect, GUIContent.none);
        GUILayout.BeginArea(new Rect(bx + 8, by + 8, barW - 16, barH - 16));
        GUILayout.BeginHorizontal();

        DrawTurretBtn("[1] Machine Gun\n50 Hurda", TurretType.MachineGun);
        DrawTurretBtn("[2] Slow Tower\n75 Hurda", TurretType.SlowTower);
        DrawTurretBtn("[3] Laser Turret\n180 Hurda", TurretType.LaserTurret);

        GUILayout.Space(16);

        GUILayout.BeginVertical();
        labelBold.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        GUILayout.Label("[R] Tamir (1.5s)  |  [E] Lv.2 Yukselme  |  [X] Sat (%60 iade)", labelBold);
        GUILayout.Label("Sag Tik: Secimi Iptal  |  Sol Tik: Kule Dik  |  Max Mesafe: 2 Hucre", labelBold);
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void DrawTurretBtn(string label, TurretType type)
    {
        bool selected = BuildingManager.Instance != null && BuildingManager.Instance.selectedTurretType == type;
        bool canAfford = GameEconomy.Instance != null && GameEconomy.Instance.CanAfford(GetCost(type));

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = selected ? Color.cyan :
                              !canAfford ? new Color(0.4f, 0.4f, 0.4f) : Color.white;

        if (GUILayout.Button(label, hotbarBtn, GUILayout.Width(136), GUILayout.Height(48)))
        {
            if (BuildingManager.Instance != null)
            {
                if (selected) BuildingManager.Instance.DeselectTurret();
                else BuildingManager.Instance.SelectTurret(type);
            }
        }

        GUI.backgroundColor = prev;
    }

    // ─── 3. TURRET INTERACT POPUP ───────────────────────────────
    void DrawTurretInteractPopup()
    {
        if (EngineerController.Instance == null) return;

        TurretController tc = EngineerController.Instance.GetNearestInteractableTurret();
        if (tc == null) return;

        // Convert turret world position to screen position
        Vector3 screenPos = Camera.main != null
            ? Camera.main.WorldToScreenPoint(tc.transform.position + Vector3.up * 1.8f)
            : Vector3.zero;

        if (screenPos.z < 0) return; // behind camera

        float popW = 280f;
        float popH = 125f;
        float px = screenPos.x - popW * 0.5f;
        float py = Screen.height - screenPos.y - popH * 0.5f;

        px = Mathf.Clamp(px, 4f, Screen.width - popW - 4f);
        py = Mathf.Clamp(py, 4f, Screen.height - popH - 4f);

        popupRect = new Rect(px, py, popW, popH);
        GUI.Box(popupRect, GUIContent.none, popupStyle);

        GUILayout.BeginArea(new Rect(px + 10, py + 8, popW - 20, popH - 16));

        labelBold.normal.textColor = Color.cyan;
        labelBold.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label($"{tc.turretType}  Lv.{tc.level}", labelBold);

        labelBold.normal.textColor = Color.white;
        float dur = tc.durability != null ? tc.durability.currentDurability : 100f;
        float maxDur = tc.durability != null ? tc.durability.maxDurability : 100f;
        GUILayout.Label($"Dayaniklilik: {dur:0}/{maxDur:0}", labelBold);

        labelBold.alignment = TextAnchor.MiddleLeft;
        GUILayout.Space(2);

        if (tc.durability != null && tc.durability.currentDurability < tc.durability.maxDurability)
            GUILayout.Label($"[R] Tamir  ({tc.repairCost} Hurda) — Basili tut", labelBold);

        if (tc.level < 2)
            GUILayout.Label($"[E] Lv.2 Yukselme  ({tc.upgradeCost} Hurda)", labelBold);

        int sellValue = Mathf.RoundToInt((tc.baseCost + (tc.level > 1 ? tc.upgradeCost : 0)) * 0.6f);
        GUILayout.Label($"[X] Sat  (%60 = {sellValue} Hurda)", labelBold);

        GUILayout.EndArea();

        // Repair progress bar (shown when [R] is held)
        float repairProg = EngineerController.Instance.RepairProgress;
        if (repairProg > 0.01f)
        {
            float barW = popW - 20f;
            float barH = 12f;
            float barX = px + 10f;
            float barY = py + popH - 20f;

            GUI.Box(new Rect(barX, barY, barW, barH), GUIContent.none);
            Color prev = GUI.color;
            GUI.color = new Color(0.3f, 1f, 0.5f);
            GUI.DrawTexture(new Rect(barX, barY, barW * repairProg, barH), Texture2D.whiteTexture);
            GUI.color = prev;
        }
    }

    // ─── 4. ALERT BANNER ────────────────────────────────────────
    void DrawAlert()
    {
        if (alertTimer <= 0f) return;
        alertTimer -= Time.deltaTime;

        if (Mathf.PingPong(Time.time * 7f, 1f) > 0.5f)
        {
            GUI.Label(new Rect(0, Screen.height * 0.22f, Screen.width, 70f), alertMessage, alertStyle);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────
    int GetCost(TurretType t) => t == TurretType.MachineGun ? 50 : t == TurretType.SlowTower ? 75 : 180;

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        labelBold = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        labelBold.normal.textColor = Color.white;

        alertStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        alertStyle.normal.textColor = Color.red;

        popupStyle = new GUIStyle(GUI.skin.box);

        hotbarBtn = new GUIStyle(GUI.skin.button)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }
}