using UnityEngine;

// Handles Game Over and Victory screens via OnGUI.
// Subscribed to GameEconomy events.
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    private bool isGameOver = false;
    private bool isVictory = false;
    public bool IsEnded => isGameOver || isVictory;

    private GUIStyle bigTitleStyle;
    private GUIStyle subStyle;
    private GUIStyle buttonStyle;
    private bool stylesInit = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (GameEconomy.Instance != null)
            GameEconomy.Instance.OnGameOver += HandleGameOver;
    }

    public void HandleVictory()
    {
        if (IsEnded) return;
        isVictory = true;
        AegisAudio.Play("victory", 0.9f, false);
        if (GameEconomy.Instance != null) GameEconomy.Instance.SaveHighScore();
    }

    void HandleGameOver()
    {
        if (IsEnded) return; // ayni karede birden fazla dusman cekirdegi vurabilir
        isGameOver = true;
        AegisAudio.Play("game_over", 0.9f, false);
        if (GameEconomy.Instance != null) GameEconomy.Instance.SaveHighScore();
        // Pause the game
        Time.timeScale = 0f;
    }

    void OnGUI()
    {
        if (!isGameOver && !isVictory) return;

        InitStyles();

        // Dim background
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float panelW = 520f;
        float panelH = 400f;
        float px = (Screen.width - panelW) * 0.5f;
        float py = (Screen.height - panelH) * 0.5f;

        GUILayout.BeginArea(new Rect(px, py, panelW, panelH));
        GUILayout.FlexibleSpace();

        if (isGameOver)
        {
            bigTitleStyle.normal.textColor = new Color(1f, 0.2f, 0.2f);
            GUILayout.Label("OYUN BİTTİ", bigTitleStyle);
            GUILayout.Space(10);
            subStyle.normal.textColor = Color.white;
            GUILayout.Label("Enerji Çekirdeği yok edildi!", subStyle);
        }
        else
        {
            bigTitleStyle.normal.textColor = new Color(0.3f, 1f, 0.5f);
            GUILayout.Label("ZAFERİN!", bigTitleStyle);
            GUILayout.Space(10);
            subStyle.normal.textColor = Color.white;
            GUILayout.Label("Tüm 7 dalga başarıyla savunuldu!", subStyle);
        }

        // Final Score
        GUILayout.Space(14);
        int finalScore = GameEconomy.Instance != null ? GameEconomy.Instance.totalScore : 0;
        GUILayout.Label($"Final Skor: {finalScore}", subStyle);

        subStyle.normal.textColor = new Color(0.75f, 0.85f, 0.95f);
        GUILayout.Label($"Yüksek Skor: {GameEconomy.HighScore}", subStyle);
        if (GameEconomy.Instance != null && GameEconomy.Instance.IsNewHighScore)
        {
            subStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            GUILayout.Label("YENİ REKOR!", subStyle);
        }
        subStyle.normal.textColor = Color.white;

        GUILayout.Space(24);

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Tekrar Oyna", buttonStyle, GUILayout.Width(160), GUILayout.Height(48)))
        {
            Time.timeScale = 1f;
            GameMenu.skipMenuOnce = true; // tekrar oynada baslangic menusu atlanir
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Çıkış", buttonStyle, GUILayout.Width(100), GUILayout.Height(48)))
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();
    }

    void InitStyles()
    {
        if (stylesInit) return;
        stylesInit = true;

        bigTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 52,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        subStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        subStyle.normal.textColor = Color.white;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
    }

    void OnDestroy()
    {
        if (GameEconomy.Instance != null)
            GameEconomy.Instance.OnGameOver -= HandleGameOver;
        // Restore time scale in case
        Time.timeScale = 1f;
    }
}