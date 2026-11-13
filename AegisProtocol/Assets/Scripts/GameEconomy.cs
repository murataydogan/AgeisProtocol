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

    public void CommitWaveScore()
    {
        totalScore += wavePendingScore;
        wavePendingScore = 0;
        OnScoreChanged?.Invoke(totalScore, wavePendingScore);
    }
}
