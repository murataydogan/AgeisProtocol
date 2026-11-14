using UnityEngine;

public enum TurretType
{
    MachineGun,
    SlowTower,
    LaserTurret
}

public class TurretController : MonoBehaviour
{
    [Header("Turret Identification")]
    public TurretType turretType;
    public int level = 1;

    [Header("Costs (GDD Balanced)")]
    public int baseCost = 50;
    public int upgradeCost = 40;
    public int repairCost = 15;
    private int totalInvestedScrap = 50;

    [Header("Combat Stats")]
    public float damage = 10f;
    public float fireRate = 0.3f; // seconds between shots
    public float range = 3.0f;
    public float wearPerShot = 0.4f;
    public bool ignoreArmor = false;

    [Header("Slow Stats (for Slow Tower)")]
    public float slowPercent = 0.40f; // 40% at Lv1, 55% at Lv2

    [Header("References")]
    public Durability durability;
    public Transform firePoint;

    private float fireTimer = 0f;
    private EnemyAI currentTarget;

    void Awake()
    {
        if (durability == null) durability = gameObject.AddComponent<Durability>();
        if (GetComponentInChildren<TurretHealthBar>() == null)
            gameObject.AddComponent<TurretHealthBar>();
        if (GetComponent<TurretRangeVisualizer>() == null)
            gameObject.AddComponent<TurretRangeVisualizer>();
    }

    void Start()
    {
        ConfigureStats();
    }

    public void Setup(TurretType type)
    {
        turretType = type;
        level = 1;
        ConfigureStats();
    }

    void ConfigureStats()
    {
        switch (turretType)
        {
            case TurretType.MachineGun:
                baseCost = 50;
                upgradeCost = 40;
                repairCost = 15;
                damage = (level == 1) ? 10f : 15f;
                fireRate = 0.3f;
                range = (level == 1) ? 3.0f : 3.5f;
                wearPerShot = 0.4f;
                ignoreArmor = false;
                break;

            case TurretType.SlowTower:
                baseCost = 75;
                upgradeCost = 50;
                repairCost = 20;
                damage = 0f;
                fireRate = 0.5f;
                range = (level == 1) ? 2.0f : 2.5f;
                wearPerShot = 0.5f; // 1.0 wear per second continuous
                slowPercent = (level == 1) ? 0.40f : 0.55f;
                ignoreArmor = false;
                break;

            case TurretType.LaserTurret:
                baseCost = 180;
                upgradeCost = 90;
                repairCost = 35;
                damage = (level == 1) ? 65f : 100f;
                fireRate = (level == 1) ? 2.0f : 1.8f;
                range = (level == 1) ? 3.5f : 4.0f;
                wearPerShot = 5.0f;
                ignoreArmor = true; // Ignores armor completely
                break;
        }

        totalInvestedScrap = (level == 1) ? baseCost : (baseCost + upgradeCost);
    }

    void Update()
    {
        if (durability != null && durability.IsBroken) return;

        // Hedef yokken zamanlayici en fazla bir atis kadar birikir (bosta bekleyen kule salvo atmasin)
        fireTimer = Mathf.Min(fireTimer + Time.deltaTime, fireRate);

        if (turretType == TurretType.SlowTower)
        {
            ApplySlowAura();
        }
        else
        {
            AcquireTarget();

            if (currentTarget != null)
            {
                // Aim towards target
                Vector3 targetDir = (currentTarget.transform.position - transform.position);
                targetDir.y = 0f;
                if (targetDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(targetDir);
                }

                if (fireTimer >= fireRate)
                {
                    Shoot(currentTarget);
                    fireTimer -= fireRate; // artan sure kaybolmaz (dusuk FPS'te atis hizi sabit kalir)
                }
            }
        }
    }

    void AcquireTarget()
    {
        // Menzildeki, cekirdege YOL UZERINDEN en yakin dusmani hedefler (kus ucusu mesafe degil).
        // Mesafe, dusmanin merkezine olan yatay mesafedir (collider yuzeyi degil).
        EnemyAI best = null;
        float bestRemaining = float.MaxValue;
        float rangeSqr = range * range;
        Vector3 p = transform.position;
        var all = EnemyAI.All;

        for (int i = 0; i < all.Count; i++)
        {
            EnemyAI e = all[i];
            if (e == null || !e.IsAlive) continue;

            Vector3 d = e.transform.position - p;
            d.y = 0f;
            if (d.sqrMagnitude > rangeSqr) continue;

            if (e.RemainingDistance < bestRemaining)
            {
                bestRemaining = e.RemainingDistance;
                best = e;
            }
        }

        currentTarget = best;
    }

    void Shoot(EnemyAI target)
    {
        if (target == null || !target.IsAlive) return;

        // Apply Wear & Tear
        if (durability != null)
        {
            durability.Consume(wearPerShot);
        }

        // Deal damage directly or spawn projectile
        target.TakeDamage(damage, ignoreArmor);
        AegisAudio.Play(turretType == TurretType.LaserTurret ? "laser_shot" : "mg_shot", turretType == TurretType.LaserTurret ? 0.55f : 0.3f);

        // Visual bullet tracer / beam effect
        DrawTracer(transform.position + Vector3.up * 0.5f, target.transform.position + Vector3.up * 0.5f);
    }

    void ApplySlowAura()
    {
        bool hasEnemies = false;
        float rangeSqr = range * range;
        Vector3 p = transform.position;
        var all = EnemyAI.All;

        for (int i = 0; i < all.Count; i++)
        {
            EnemyAI enemy = all[i];
            if (enemy == null || !enemy.IsAlive) continue;

            Vector3 d = enemy.transform.position - p;
            d.y = 0f;
            if (d.sqrMagnitude > rangeSqr) continue;

            enemy.ApplySlow(slowPercent, 0.5f);
            hasEnemies = true;
        }

        // Menzilde dusman varken saniyede 1.0 yipranma
        if (hasEnemies && durability != null)
        {
            durability.Consume(1.0f * Time.deltaTime);
        }
    }

    void DrawTracer(Vector3 from, Vector3 to)
    {
        GameObject lineObj = new GameObject("Tracer");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = (turretType == TurretType.LaserTurret) ? 0.2f : 0.05f;
        lr.endWidth = lr.startWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, from);
        lr.SetPosition(1, to);

        Material mat = new Material(Shader.Find("Sprites/Default"));
        Color tracerColor = (turretType == TurretType.LaserTurret) ? Color.red : Color.yellow;
        mat.color = tracerColor;
        lr.material = mat;

        Destroy(lineObj, (turretType == TurretType.LaserTurret) ? 0.15f : 0.05f);
    }

    public bool UpgradeToLevel2()
    {
        if (level >= 2) return false;

        if (GameEconomy.Instance != null && GameEconomy.Instance.SpendScrap(upgradeCost))
        {
            level = 2;
            ConfigureStats();
            AegisAudio.Play("upgrade", 0.7f, false);
            Transform visual = transform.Find("Visual"); // model varsa Lv.2'de biraz buyur
            if (visual != null) visual.localScale *= 1.12f;
            Debug.Log($"Turret upgraded to Level 2! Damage/Range increased.");
            return true;
        }
        return false;
    }

    public int SellTurret()
    {
        // 60% of total invested scrap returned regardless of durability
        int refundAmount = Mathf.RoundToInt(totalInvestedScrap * 0.60f);
        if (GameEconomy.Instance != null)
        {
            GameEconomy.Instance.AddScrap(refundAmount);
        }

        AegisAudio.Play("sell", 0.7f, false);
        Destroy(gameObject);
        return refundAmount;
    }

    public bool RepairTurret()
    {
        if (durability == null || durability.currentDurability >= durability.maxDurability) return false;

        if (GameEconomy.Instance != null && GameEconomy.Instance.SpendScrap(repairCost))
        {
            durability.RepairFull();
            AegisAudio.Play("repair", 0.7f, false);
            return true;
        }
        return false;
    }
}