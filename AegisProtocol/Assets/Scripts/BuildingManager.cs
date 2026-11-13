using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    // -1 = nothing selected
    private int selectedTurretIndex = -1;
    public TurretType? selectedTurretType => selectedTurretIndex >= 0 ? (TurretType?)((TurretType)selectedTurretIndex) : null;

    private GameObject ghostPreviewObj;
    private GridCell currentHoveredCell;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        HandleKeyboardSelection();
        HandleMouseRaycast();
        HandlePlacementAndCancel();
    }

    // ─── Keyboard Selection ─────────────────────────────────────
    void HandleKeyboardSelection()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            SelectTurret(TurretType.MachineGun);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            SelectTurret(TurretType.SlowTower);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            SelectTurret(TurretType.LaserTurret);
    }

    public void SelectTurret(TurretType type)
    {
        selectedTurretIndex = (int)type;
        CreateGhostPreview();
        Debug.Log($"Turret selected: {type}");
    }

    public void DeselectTurret()
    {
        selectedTurretIndex = -1;
        if (ghostPreviewObj != null) Destroy(ghostPreviewObj);
        if (currentHoveredCell != null) { currentHoveredCell.SetHover(false); currentHoveredCell = null; }
    }

    // ─── Mouse Raycast ─────────────────────────────────────────
    void HandleMouseRaycast()
    {
        if (Camera.main == null) return;

        GridCell hoveredCell = null;

        // Fare arayuzun uzerindeyken hucre secme (tiklama arayuzden gecip kule dikmesin)
        bool overUI = UIManager.Instance != null && UIManager.Instance.IsPointerOverUI(Input.mousePosition);

        if (!overUI && GridManager.Instance != null)
        {
            // Kule, dusman, hurda collider'lari isin onunu kesmesin diye zemin duzlemiyle (y=0.2) kesistiriyoruz
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane ground = new Plane(Vector3.up, new Vector3(0f, 0.2f, 0f));
            if (ground.Raycast(ray, out float dist))
                hoveredCell = GridManager.Instance.GetCellFromWorldPos(ray.GetPoint(dist));
        }

        if (hoveredCell != currentHoveredCell)
        {
            if (currentHoveredCell != null) currentHoveredCell.SetHover(false);
            currentHoveredCell = hoveredCell;
        }

        if (currentHoveredCell != null && selectedTurretIndex >= 0)
        {
            bool inRange = EngineerController.Instance != null &&
                           EngineerController.Instance.IsInRangeForBuilding(currentHoveredCell.transform.position);

            currentHoveredCell.SetHover(true, inRange);

            if (ghostPreviewObj != null)
            {
                ghostPreviewObj.SetActive(true);
                ghostPreviewObj.transform.position = currentHoveredCell.GetWorldCenter() + Vector3.up * 0.4f;

                bool canBuild = currentHoveredCell.IsBuildable && inRange && CanAfford((TurretType)selectedTurretIndex);
                Renderer gr = ghostPreviewObj.GetComponent<Renderer>();
                if (gr != null)
                    gr.material.color = canBuild ? new Color(0f, 1f, 0.2f, 0.45f) : new Color(1f, 0.2f, 0.2f, 0.45f);
            }
        }
        else if (ghostPreviewObj != null)
        {
            ghostPreviewObj.SetActive(false);
        }
    }

    // ─── Left Click = Place, Right Click = Cancel ──────────────
    void HandlePlacementAndCancel()
    {
        if (Input.GetMouseButtonDown(1))
        {
            DeselectTurret();
            return;
        }

        if (Input.GetMouseButtonDown(0) && selectedTurretIndex >= 0 && currentHoveredCell != null
            && !(UIManager.Instance != null && UIManager.Instance.IsPointerOverUI(Input.mousePosition)))
            TryBuildTurret(currentHoveredCell, (TurretType)selectedTurretIndex);
    }

    // ─── Build Logic ───────────────────────────────────────────
    void TryBuildTurret(GridCell cell, TurretType type)
    {
        if (!cell.IsBuildable)
        {
            Debug.LogWarning("Cannot build: cell not buildable or already occupied.");
            return;
        }

        if (EngineerController.Instance != null && !EngineerController.Instance.IsInRangeForBuilding(cell.transform.position))
        {
            Debug.LogWarning("Cannot build: Engineer is too far! (Max 2 tiles)");
            return;
        }

        int cost = GetTurretCost(type);
        if (GameEconomy.Instance == null || !GameEconomy.Instance.SpendScrap(cost))
        {
            Debug.LogWarning($"Not enough Scrap! Needed: {cost}");
            return;
        }

        // Create turret placeholder
        GameObject turret = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        turret.name = $"{type}_{cell.gridX}_{cell.gridY}";
        turret.transform.position = cell.GetWorldCenter() + Vector3.up * 0.4f;
        turret.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);

        Renderer r = turret.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = GetTurretColor(type);
        }

        TurretController tc = turret.AddComponent<TurretController>();
        tc.Setup(type);
        cell.placedTurret = turret;

        Debug.Log($"Built {type} at ({cell.gridX},{cell.gridY}) for {cost} Scrap.");
    }

    // ─── Ghost Preview ─────────────────────────────────────────
    void CreateGhostPreview()
    {
        if (ghostPreviewObj != null) Destroy(ghostPreviewObj);

        ghostPreviewObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ghostPreviewObj.name = "GhostPreview";
        ghostPreviewObj.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);

        Collider c = ghostPreviewObj.GetComponent<Collider>();
        if (c != null) Destroy(c);

        Renderer r = ghostPreviewObj.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0f, 1f, 0.2f, 0.45f);
            r.material = mat;
        }
    }

    // ─── Helpers ───────────────────────────────────────────────
    bool CanAfford(TurretType type) =>
        GameEconomy.Instance != null && GameEconomy.Instance.CanAfford(GetTurretCost(type));

    int GetTurretCost(TurretType type) => type switch
    {
        TurretType.MachineGun => 50,
        TurretType.SlowTower => 75,
        TurretType.LaserTurret => 180,
        _ => 0
    };

    Color GetTurretColor(TurretType type) => type switch
    {
        TurretType.MachineGun => new Color(0.85f, 0.85f, 0.85f), // Gray-Silver
        TurretType.SlowTower => new Color(0.2f, 0.75f, 1f),    // Ice Blue
        TurretType.LaserTurret => new Color(1f, 0.3f, 0.3f),   // Red
        _ => Color.white
    };
}