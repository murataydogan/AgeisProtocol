using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WaveConfig
{
    public int waveNumber;
    public float prepTime;
    public float spawnInterval;

    public int swarmerA;
    public int stalkerA;
    public int bruteA;

    public int swarmerB;
    public int stalkerB;
    public int bruteB;
}

public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public int currentWaveIndex = 0;
    public bool isPrepPhase = true;
    public float prepTimer = 15f;
    public bool isWaveActive = false;

    private List<WaveConfig> waves = new List<WaveConfig>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        SetupWaveConfigs();
    }

    void Start()
    {
        StartCoroutine(GameLoopCoroutine());
    }

    void Update()
    {
        // Space key to skip prep phase early (GDD rule)
        if (isPrepPhase && Input.GetKeyDown(KeyCode.Space))
        {
            prepTimer = 0f;
            Debug.Log("Wave started early by player!");
        }
    }

    void SetupWaveConfigs()
    {
        // GDD Wave Table
        // Wave 1: A(10/0/0), Prep 15s, Spawn 1.8s
        // Wave 2: A(16/2/0), Prep 25s, Spawn 1.5s
        // Wave 3: A(22/6/0), Prep 25s, Spawn 1.4s
        // Wave 4: A(28/12/2), Prep 25s, Spawn 1.3s
        // Wave 5: A(24/10/3), B(24/10/2), Prep 25s, Spawn 1.3s
        // Wave 6: A(30/14/5), B(30/12/4), Prep 25s, Spawn 1.2s
        // Wave 7: A(40/20/9), B(36/20/8), Prep 25s, Spawn 1.1s
        waves.Add(new WaveConfig { waveNumber = 1, prepTime = 15f, spawnInterval = 1.8f, swarmerA = 10, stalkerA = 0, bruteA = 0 });
        waves.Add(new WaveConfig { waveNumber = 2, prepTime = 25f, spawnInterval = 1.5f, swarmerA = 16, stalkerA = 2, bruteA = 0 });
        waves.Add(new WaveConfig { waveNumber = 3, prepTime = 25f, spawnInterval = 1.4f, swarmerA = 22, stalkerA = 6, bruteA = 0 });
        waves.Add(new WaveConfig { waveNumber = 4, prepTime = 25f, spawnInterval = 1.3f, swarmerA = 28, stalkerA = 12, bruteA = 2 });
        waves.Add(new WaveConfig { waveNumber = 5, prepTime = 25f, spawnInterval = 1.3f, swarmerA = 24, stalkerA = 10, bruteA = 3, swarmerB = 24, stalkerB = 10, bruteB = 2 });
        waves.Add(new WaveConfig { waveNumber = 6, prepTime = 25f, spawnInterval = 1.2f, swarmerA = 30, stalkerA = 14, bruteA = 5, swarmerB = 30, stalkerB = 12, bruteB = 4 });
        waves.Add(new WaveConfig { waveNumber = 7, prepTime = 25f, spawnInterval = 1.1f, swarmerA = 40, stalkerA = 20, bruteA = 9, swarmerB = 36, stalkerB = 20, bruteB = 8 });
    }

    IEnumerator GameLoopCoroutine()
    {
        for (int i = 0; i < waves.Count; i++)
        {
            currentWaveIndex = i + 1;
            WaveConfig wave = waves[i];

            // Wave 5: 2nd Gate Warning
            if (currentWaveIndex == 5)
            {
                Debug.Log("<color=red>WARNING: 2ND GATE ACTIVE! (Red Gate)</color>");
                if (UIManager.Instance != null)
                    UIManager.Instance.ShowWarningAlert("UYARI: 2. KAPI AKTIF!", 5f);
            }

            // --- PREP PHASE ---
            isPrepPhase = true;
            prepTimer = wave.prepTime;

            while (prepTimer > 0f)
            {
                prepTimer -= Time.deltaTime;
                yield return null;
            }

            isPrepPhase = false;
            isWaveActive = true;
            Debug.Log($"<color=cyan>--- WAVE {currentWaveIndex} / 7 STARTED ---</color>");

            // --- SPAWN PHASE ---
            yield return StartCoroutine(SpawnWaveEnemies(wave));

            // Wait for all enemies to die
            while (FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length > 0)
            {
                yield return new WaitForSeconds(0.5f);
            }

            isWaveActive = false;
            Debug.Log($"<color=green>WAVE {currentWaveIndex} COMPLETED!</color>");

            if (GameEconomy.Instance != null)
                GameEconomy.Instance.CommitWaveScore();
        }

        Debug.Log("<color=yellow>CONGRATULATIONS! All 7 waves cleared!</color>");
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.HandleVictory();
    }

    IEnumerator SpawnWaveEnemies(WaveConfig wave)
    {
        List<EnemyType> listA = BuildEnemyQueue(wave.swarmerA, wave.stalkerA, wave.bruteA);
        List<EnemyType> listB = BuildEnemyQueue(wave.swarmerB, wave.stalkerB, wave.bruteB);

        int maxCount = Mathf.Max(listA.Count, listB.Count);

        for (int i = 0; i < maxCount; i++)
        {
            if (i < listA.Count && GridManager.Instance != null)
                SpawnEnemy(listA[i], GridManager.Instance.waypointsPathA);

            if (i < listB.Count && GridManager.Instance != null)
                SpawnEnemy(listB[i], GridManager.Instance.waypointsPathB);

            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    List<EnemyType> BuildEnemyQueue(int swarmers, int stalkers, int brutes)
    {
        List<EnemyType> queue = new List<EnemyType>();
        for (int i = 0; i < swarmers; i++) queue.Add(EnemyType.Swarmer);
        for (int i = 0; i < stalkers; i++) queue.Add(EnemyType.Stalker);
        for (int i = 0; i < brutes; i++) queue.Add(EnemyType.Brute);
        return queue;
    }

    void SpawnEnemy(EnemyType type, List<Vector3> waypoints)
    {
        GameObject enemyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        enemyObj.name = type.ToString();

        Renderer r = enemyObj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = (type == EnemyType.Swarmer) ? new Color(0.4f, 0.8f, 1f)   // Ice Blue
                             : (type == EnemyType.Stalker) ? new Color(0.1f, 0.5f, 0.8f) // Dark Cyan
                                                           : new Color(0.05f, 0.2f, 0.4f);// Iron Blue
        }

        EnemyAI ai = enemyObj.AddComponent<EnemyAI>();
        ai.Setup(type, waypoints);
    }
}
