using UnityEngine;
using UnityEngine.SceneManagement;

// Baslangic menusu, ESC ile acilan duraklatma menusu, Kontroller / Ses / Credits ekranlari (OnGUI).
// Menu aciksa Time.timeScale = 0 olur; BuildingManager, EngineerController ve WaveManager
// GameMenu.IsMenuOpen'a bakarak girdiyi yok sayar.
public class GameMenu : MonoBehaviour
{
    public static GameMenu Instance { get; private set; }

    // "Tekrar Oyna" / "Yeniden Baslat" sonrasi sahne yeniden yuklenir; baslangic menusu atlanir.
    public static bool skipMenuOnce = false;

    private enum Screen_ { None, Start, Pause, Controls, Audio, Credits }
    private Screen_ screen = Screen_.Start;
    private Screen_ backTarget = Screen_.Start;

    public static bool IsMenuOpen => Instance != null && Instance.screen != Screen_.None;

    private GUIStyle titleStyle, subStyle, bodyStyle, buttonStyle, headStyle;
    private bool stylesReady = false;
    private float volume = 0.8f;
    private Vector2 creditsScroll;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        volume = PlayerPrefs.GetFloat("aegis_volume", 0.8f);
        AudioListener.volume = volume;

        if (skipMenuOnce)
        {
            skipMenuOnce = false;
            screen = Screen_.None;
            Time.timeScale = 1f;
        }
        else
        {
            screen = Screen_.Start;
            Time.timeScale = 0f;
        }
    }

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsEnded) return;

        switch (screen)
        {
            case Screen_.None:
                screen = Screen_.Pause;
                Time.timeScale = 0f;
                break;
            case Screen_.Pause:
                Resume();
                break;
            case Screen_.Controls:
            case Screen_.Audio:
            case Screen_.Credits:
                screen = backTarget;
                break;
                // Start menusunde ESC bir sey yapmaz
        }
    }

    void Resume()
    {
        screen = Screen_.None;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Time.timeScale = 1f;
    }

    // ─── OnGUI ──────────────────────────────────────────────────
    void OnGUI()
    {
        if (screen == Screen_.None) return;
        EnsureStyles();
        GUI.depth = -100; // diger OnGUI cizimlerinin ustunde

        // Arka plan: baslangicta koyu, duraklatmada yari saydam
        GUI.color = new Color(0.02f, 0.05f, 0.1f, screen == Screen_.Start ? 0.94f : 0.78f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float w = 560f, h = 520f;
        Rect area = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        GUILayout.BeginArea(area);

        switch (screen)
        {
            case Screen_.Start: DrawStart(); break;
            case Screen_.Pause: DrawPause(); break;
            case Screen_.Controls: DrawControls(); break;
            case Screen_.Audio: DrawAudio(); break;
            case Screen_.Credits: DrawCredits(); break;
        }

        GUILayout.EndArea();
    }

    bool Btn(string text)
    {
        return GUILayout.Button(text, buttonStyle, GUILayout.Height(46));
    }

    void DrawStart()
    {
        GUILayout.FlexibleSpace();
        titleStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);
        GUILayout.Label("AEGIS PROTOCOL", titleStyle);
        subStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
        GUILayout.Label("Kutup karakolunu 7 dalga boyunca savun", subStyle);
        if (GameEconomy.HighScore > 0)
        {
            subStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label($"Yüksek Skor: {GameEconomy.HighScore}", subStyle);
        }
        GUILayout.Space(28);

        if (Btn("Oyuna Başla")) Resume();
        GUILayout.Space(8);
        if (Btn("Kontroller")) { backTarget = Screen_.Start; screen = Screen_.Controls; }
        GUILayout.Space(8);
        if (Btn("Ses Ayarları")) { backTarget = Screen_.Start; screen = Screen_.Audio; }
        GUILayout.Space(8);
        if (Btn("Credits")) { backTarget = Screen_.Start; screen = Screen_.Credits; }
        GUILayout.FlexibleSpace();
    }

    void DrawPause()
    {
        GUILayout.FlexibleSpace();
        titleStyle.normal.textColor = Color.white;
        GUILayout.Label("DURAKLATILDI", titleStyle);
        GUILayout.Space(28);

        if (Btn("Devam Et")) Resume();
        GUILayout.Space(8);
        if (Btn("Kontroller")) { backTarget = Screen_.Pause; screen = Screen_.Controls; }
        GUILayout.Space(8);
        if (Btn("Ses Ayarları")) { backTarget = Screen_.Pause; screen = Screen_.Audio; }
        GUILayout.Space(8);
        if (Btn("Yeniden Başlat"))
        {
            skipMenuOnce = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUILayout.Space(8);
        if (Btn("Ana Menü"))
        {
            skipMenuOnce = false;
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        GUILayout.FlexibleSpace();
    }

    void DrawControls()
    {
        GUILayout.FlexibleSpace();
        headStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);
        GUILayout.Label("KONTROLLER", headStyle);
        GUILayout.Space(10);
        bodyStyle.normal.textColor = Color.white;
        GUILayout.Label(
            "WASD / Ok tuşları  —  Mühendisi hareket ettir\n" +
            "1 / 2 / 3  —  Machine Gun / Slow Tower / Laser Turret seç\n" +
            "Sol Tık  —  Seçili kuleyi inşa et (mühendise en fazla 2 kare)\n" +
            "Sağ Tık  —  Seçimi iptal et\n" +
            "R (basılı tut 1,5 sn)  —  Yakındaki kuleyi tamir et\n" +
            "E  —  Kuleyi Lv.2'ye yükselt\n" +
            "X  —  Kuleyi sat (toplam yatırımın %60'ı iade)\n" +
            "Space  —  Hazırlık süresini atla, dalgayı başlat\n" +
            "ESC  —  Duraklat / Menü\n\n" +
            "Hurdaya 1 kare yaklaşınca otomatik toplanır; 15 sn sonra kaybolur.",
            bodyStyle);
        GUILayout.Space(16);
        if (Btn("Geri")) screen = backTarget;
        GUILayout.FlexibleSpace();
    }

    void DrawAudio()
    {
        GUILayout.FlexibleSpace();
        headStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);
        GUILayout.Label("SES AYARLARI", headStyle);
        GUILayout.Space(16);
        bodyStyle.normal.textColor = Color.white;
        GUILayout.Label($"Ana Ses: %{Mathf.RoundToInt(volume * 100f)}", bodyStyle);

        float nv = GUILayout.HorizontalSlider(volume, 0f, 1f, GUILayout.Height(24));
        if (!Mathf.Approximately(nv, volume))
        {
            volume = nv;
            AudioListener.volume = volume;
            PlayerPrefs.SetFloat("aegis_volume", volume);
        }

        GUILayout.Space(24);
        if (Btn("Geri")) { PlayerPrefs.Save(); screen = backTarget; }
        GUILayout.FlexibleSpace();
    }

    void DrawCredits()
    {
        headStyle.normal.textColor = new Color(0.4f, 0.85f, 1f);
        GUILayout.Space(10);
        GUILayout.Label("CREDITS", headStyle);
        GUILayout.Space(6);

        creditsScroll = GUILayout.BeginScrollView(creditsScroll, GUILayout.Height(370));
        bodyStyle.normal.textColor = Color.white;
        GUILayout.Label(
            "Tasarım ve Kod: Murat Aydoğan & Kerem Yıldız\n" +
            "Oyun Motoru: Unity\n\n" +
            "3D Modeller: Kenney (Space Kit, Tower Defense Kit, Nature Kit,\n" +
            "Animated Characters 3) — CC0\n" +
            "Düşman Modeli: Quaternius (Animated Easy Enemies) — CC0\n\n" +
            "Müzik: \"Snow Theme\" — Cleyton Kauffman — CC0\n" +
            "Rüzgâr Ambiyansı: \"Wind Loop\" — AntumDeluge, orijinal ses Jonathan Shaw (InspectorJ)\n" +
            "opengameart.org/content/wind-loop — CC BY 3.0\n" +
            "Ses Efektleri: Kenney Sci-Fi Sounds — CC0\n\n" +
            "Kullanılan hazır kaynakların sahiplerine teşekkürler.",
            bodyStyle);
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        if (Btn("Geri")) screen = backTarget;
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 48, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        subStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        headStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.UpperLeft, wordWrap = true };
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
    }
}