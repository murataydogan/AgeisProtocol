using UnityEngine;
using System;

public class GameEconomy : MonoBehaviour
{
    public static GameEconomy Instance { get; private set; }

    [Header("Core & Scrap")]
    public int currentScrap = 150;     // Initial Scrap: 150
    public int coreHealth = 100;       // Initial Core HP: 100
    public int maxCoreHealth = 100;

    [Header("Score")]
    public int totalScore = 0;
    public int wavePendingScore = 0;

    // Son dalga tamamlanma bilgisi (UI "Dalga Tamamlandi +N" mesaji ve skor sayimi icin)
    [HideInInspector] public int lastWaveCommit = 0;
    [HideInInspector] public int lastCommitWave = 0;
    [HideInInspector] public int commitCounter = 0;

    public const string HighScoreKey = "aegis_highscore";
    public static int HighScore => PlayerPrefs.GetInt(HighScoreKey, 0);
    public bool IsNewHighScore { get; private set; }

    public event Action<int> OnScrapChanged;
    public event Action<int> OnCoreHealthChanged;
    public event Action<int, int> OnScoreChanged;
    public event Action OnGameOver;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public bool CanAfford(int amount) => currentScrap >= amount;

    public bool SpendScrap(int amount)
    {
        if (CanAfford(amount))
        {
            currentScrap -= amount;
            OnScrapChanged?.Invoke(currentScrap);
            return true;
        }
        return false;
    }

    public void AddScrap(int amount)
    {
        currentScrap += amount;
        OnScrapChanged?.Invoke(currentScrap);
    }

    public void DamageCore(int damage)
    {
        if (coreHealth <= 0) return; // oyun zaten bitti
        AegisAudio.Play("core_hit", 0.9f, false);
        coreHealth = Mathf.Max(0, coreHealth - damage);
        OnCoreHealthChanged?.Invoke(coreHealth);

        if (coreHealth <= 0)
        {
            OnGameOver?.Invoke();
            Debug.Log("<color=red>GAME OVER! Energy Core has been destroyed.</color>");
        }
    }

    public void AddPendingScore(int score)
    {
        wavePendingScore += score;
        OnScoreChanged?.Invoke(totalScore, wavePendingScore);
    }

    // Oyun bitince bir kez cagrilir; toplam skor rekoru gectiyse kaydeder.
    public bool SaveHighScore()
    {
        if (totalScore > HighScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, totalScore);
            PlayerPrefs.Save();
            IsNewHighScore = true;
        }
        return IsNewHighScore;
    }

    public void CommitWaveScore(int wave = 0)
    {
        lastWaveCommit = wavePendingScore;
        lastCommitWave = wave;
        commitCounter++;
        AegisAudio.Play("wave_done", 0.8f, false);

        totalScore += wavePendingScore;
        wavePendingScore = 0;
        OnScoreChanged?.Invoke(totalScore, wavePendingScore);
    }
}