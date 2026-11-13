using UnityEngine;
using System.Collections.Generic;

public enum EnemyType
{
    Swarmer,
    Stalker,
    Brute
}

public class EnemyAI : MonoBehaviour
{
    [Header("Enemy Stats")]
    public EnemyType enemyType;
    public float maxHealth = 45f;
    public float currentHealth = 45f;
    public float armor = 0f;
    public float baseSpeed = 1.6f;
    public float currentSpeed = 1.6f;
    public int coreDamagePerSec = 2;
    public int scrapReward = 6;
    public int scoreReward = 50;

    [Header("Waypoints Navigation")]
    public List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool reachedEnd = false;

    [Header("Status Effects")]
    private float slowTimer = 0f;

    // Aktif tum dusmanlar: kuleler Physics.OverlapSphere yerine bu listeyi tarar (GC ve collider mesafesi sorunu yok)
    public static readonly List<EnemyAI> All = new List<EnemyAI>();

    // Yol uzerinde kat edilen mesafe ve cekirdege kalan yol (A ve B rotalari icin karsilastirilabilir)
    private float traveled = 0f;
    private float totalPathLength = 0f;
    public float RemainingDistance => totalPathLength - traveled;

    private float activeSlow = 0f;

    public bool IsAlive => currentHealth > 0.01f;

    public void Setup(EnemyType type, List<Vector3> pathWaypoints)
    {
        enemyType = type;
        waypoints = new List<Vector3>(pathWaypoints);
        currentWaypointIndex = 0;
        reachedEnd = false;

        traveled = 0f;
        totalPathLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector3 a = waypoints[i], b = waypoints[i + 1];
            a.y = 0f; b.y = 0f;
            totalPathLength += Vector3.Distance(a, b);
        }

        if (!All.Contains(this)) All.Add(this);

        switch (type)
        {
            case EnemyType.Swarmer:
                maxHealth = 90f;
                armor = 0f;
                baseSpeed = 1.6f;
                coreDamagePerSec = 2;
                scrapReward = 6;
                scoreReward = 50;
                transform.localScale = Vector3.one * 0.4f;
                break;

            case EnemyType.Stalker:
                maxHealth = 135f;
                armor = 0f;
                baseSpeed = 2.4f;
                coreDamagePerSec = 4;
                scrapReward = 10;
                scoreReward = 100;
                transform.localScale = Vector3.one * 0.6f;
                break;

            case EnemyType.Brute:
                maxHealth = 600f;
                armor = 6f; // Has armor!
                baseSpeed = 0.7f;
                coreDamagePerSec = 8;
                scrapReward = 25;
                scoreReward = 250;
                transform.localScale = Vector3.one * 0.9f;
                break;
        }

        currentHealth = maxHealth;
        currentSpeed = baseSpeed;

        if (waypoints.Count > 0)
        {
            transform.position = waypoints[0];
        }
    }

    void Update()
    {
        if (!IsAlive) return;

        UpdateSlowEffect();

        if (!reachedEnd)
        {
            FollowPath();
        }
        else
        {
            AttackCore();
        }
    }

    void FollowPath()
    {
        if (currentWaypointIndex >= waypoints.Count)
        {
            reachedEnd = true;
            return;
        }

        Vector3 target = waypoints[currentWaypointIndex];
        Vector3 dir = (target - transform.position);
        dir.y = 0;

        float step = currentSpeed * Time.deltaTime;

        if (dir.magnitude <= step)
        {
            traveled += dir.magnitude;
            transform.position = target;
            currentWaypointIndex++;
            // Stop at cell (15,4) - 1 cell before core
            if (currentWaypointIndex >= waypoints.Count - 1)
            {
                reachedEnd = true;
            }
        }
        else
        {
            traveled += step;
            transform.position += dir.normalized * step;
            if (dir != Vector3.zero)
            {
                transform.forward = dir.normalized;
            }
        }
    }

    private float attackTimer = 0f;
    void AttackCore()
    {
        attackTimer += Time.deltaTime;
        if (attackTimer >= 1.0f) // Saniye başı hasar
        {
            if (GameEconomy.Instance != null)
            {
                GameEconomy.Instance.DamageCore(coreDamagePerSec);
            }
            attackTimer = 0f;
        }
    }

    public void TakeDamage(float rawDamage, bool ignoresArmor)
    {
        if (!IsAlive) return;

        float effectiveDamage;
        if (ignoresArmor)
        {
            effectiveDamage = rawDamage; // Laser Turret
        }
        else
        {
            // GDD Rule: max(3, turret_damage - armor)
            effectiveDamage = Mathf.Max(3f, rawDamage - armor);
        }

        currentHealth -= effectiveDamage;

        if (currentHealth <= 0.01f)
        {
            Die();
        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        // Birden fazla Slow Tower etkisi toplanmaz; en yuksek olan gecerli (GDD kurali)
        activeSlow = Mathf.Max(activeSlow, slowPercent);
        currentSpeed = baseSpeed * (1f - activeSlow);
        slowTimer = duration;
    }

    void UpdateSlowEffect()
    {
        if (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                activeSlow = 0f;
                currentSpeed = baseSpeed;
            }
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Die()
    {
        currentHealth = 0f;

        // Reward score to pending wave score
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddPendingScore(scoreReward);
        }

        // Spawn Scrap
        SpawnScrapPickup();

        Destroy(gameObject);
    }

    void SpawnScrapPickup()
    {
        GameObject scrapObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scrapObj.name = $"Scrap_{scrapReward}";
        scrapObj.transform.position = transform.position + Vector3.up * 0.2f;
        scrapObj.transform.localScale = Vector3.one * 0.3f;

        Renderer r = scrapObj.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = new Color(1f, 0.84f, 0f); // Gold Scrap
        }

        ScrapPickup sp = scrapObj.AddComponent<ScrapPickup>();
        sp.Initialize(scrapReward);
    }
}