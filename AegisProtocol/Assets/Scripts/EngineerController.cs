using UnityEngine;

public class EngineerController : MonoBehaviour
{
    public static EngineerController Instance { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 4f; // 4 units/sec

    [Header("Interaction Ranges")]
    public float buildRange = 2f;      // Max 2 tiles for building
    public float scrapPickupRange = 1f; // 1 tile for auto-collecting scrap
    public float turretInteractRange = 1.5f; // 1.5 tiles for repair [R], upgrade [E], sell [X]

    [Header("Starting Position")]
    public Vector2Int startGridPos = new Vector2Int(7, 9);

    [Header("Repair Progress")]
    public float repairHoldTimeRequired = 1.5f; // Hold R for 1.5s
    private float currentRepairHoldTimer = 0f;
    public float RepairProgress => repairHoldTimeRequired > 0f ? currentRepairHoldTimer / repairHoldTimeRequired : 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        transform.position = new Vector3(startGridPos.x + 0.5f, 0.5f, startGridPos.y + 0.5f);
    }

    private void Update()
    {
        if (GameMenu.IsMenuOpen) return;
        HandleMovement();
        HandleTurretInteractions();
    }

    private void HandleMovement()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = new Vector3(moveX, 0f, moveZ).normalized;

        if (moveDir != Vector3.zero)
        {
            transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
            transform.forward = moveDir;
        }

        Vector3 clampedPos = transform.position;
        clampedPos.x = Mathf.Clamp(clampedPos.x, 0.2f, 15.8f);
        clampedPos.z = Mathf.Clamp(clampedPos.z, 0.2f, 9.8f);
        transform.position = clampedPos;
    }

    private void HandleTurretInteractions()
    {
        TurretController nearbyTurret = GetNearestInteractableTurret();
        if (nearbyTurret == null)
        {
            currentRepairHoldTimer = 0f;
            return;
        }

        // [E] - Upgrade to Level 2
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (nearbyTurret.level < 2)
            {
                nearbyTurret.UpgradeToLevel2();
            }
        }

        // [X] - Sell Turret (60% refund)
        if (Input.GetKeyDown(KeyCode.X))
        {
            GridCell cell = GridManager.Instance != null ? GridManager.Instance.GetCellFromWorldPos(nearbyTurret.transform.position) : null;
            if (cell != null) cell.placedTurret = null;

            int refunded = nearbyTurret.SellTurret();
            Debug.Log($"Turret sold for {refunded} Scrap (60% refund)!");
        }

        // [R] - Repair (Hold for 1.5s)
        if (Input.GetKey(KeyCode.R))
        {
            if (nearbyTurret.durability != null && nearbyTurret.durability.currentDurability < nearbyTurret.durability.maxDurability)
            {
                currentRepairHoldTimer += Time.deltaTime;
                if (currentRepairHoldTimer >= repairHoldTimeRequired)
                {
                    nearbyTurret.RepairTurret();
                    currentRepairHoldTimer = 0f;
                }
            }
        }
        else
        {
            currentRepairHoldTimer = 0f;
        }
    }

    public TurretController GetNearestInteractableTurret()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, turretInteractRange);
        TurretController nearest = null;
        float minDist = float.MaxValue;

        foreach (var hit in hits)
        {
            TurretController tc = hit.GetComponent<TurretController>();
            if (tc != null)
            {
                float d = Vector3.Distance(transform.position, tc.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    nearest = tc;
                }
            }
        }

        return nearest;
    }

    public bool IsInRangeForBuilding(Vector3 targetWorldPos)
    {
        Vector2 engPos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 target2D = new Vector2(targetWorldPos.x, targetWorldPos.z);
        return Vector2.Distance(engPos2D, target2D) <= buildRange + 0.5f;
    }

    public bool IsInRangeForTurretInteract(Vector3 turretPos)
    {
        Vector2 engPos2D = new Vector2(transform.position.x, transform.position.z);
        Vector2 turret2D = new Vector2(turretPos.x, turretPos.z);
        return Vector2.Distance(engPos2D, turret2D) <= turretInteractRange + 0.5f;
    }
}