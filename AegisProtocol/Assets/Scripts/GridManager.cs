using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Size")]
    public int width = 16;
    public int height = 10;
    public float cellSize = 1.0f;

    [Header("Spawn Protection")]
    public float spawnNoBuildRadius = 2.0f; // Portal ve Kirmizi Kapi etrafinda kule dikilemez (spawn camp engeli)

    [Header("Special Objects")]
    public Transform portalTransform;
    public Transform redGateTransform;
    public Transform coreTransform;

    [Header("Waypoints")]
    public List<Vector3> waypointsPathA = new List<Vector3>();
    public List<Vector3> waypointsPathB = new List<Vector3>();

    private GridCell[,] gridMatrix;

    // Kirmizi Kapi: 5. dalgaya kadar sonuk, 4. dalga bitince yanip soner, sonra tam parlak
    private Renderer redGateRenderer;
    private Light redGateLight;
    private bool redGateActive = false;

    // GDD Color Palette
    private readonly Color colorBuildable = new Color(0.22f, 0.74f, 0.97f); // Grid Cyan #38BDF8
    private readonly Color colorPath = new Color(0.043f, 0.098f, 0.173f);  // Deep Shadow / Dark Road #0B192C
    private readonly Color colorIceRock = new Color(0.118f, 0.227f, 0.373f); // Ice Rock #1E3A5F
    private readonly Color colorCore = new Color(0.96f, 0.62f, 0.043f);     // Plasma Core #F59E0B
    private readonly Color colorPortal = new Color(0.608f, 0.302f, 1f);     // Purple Portal #9B4DFF
    private readonly Color colorRedGate = new Color(0.878f, 0.227f, 0.227f); // Red Gate #E03A3A

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        BuildGridFromLayout();
    }

    public void BuildGridFromLayout()
    {
        while (transform.childCount > 0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }

        gridMatrix = new GridCell[width, height];

        List<Vector2Int> pathACells = GetPathACells();
        List<Vector2Int> pathBCells = GetPathBCells();
        List<Vector2Int> commonCells = GetCommonPathCells();
        List<Vector2Int> rockCells = GetRockCells();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                CellType type = CellType.Buildable;
                Color cellColor = colorBuildable;
                float heightOffset = 0.2f;

                if (rockCells.Contains(pos))
                {
                    type = CellType.IceRock;
                    cellColor = colorIceRock;
                    heightOffset = 0.7f;
                }
                else if (pathACells.Contains(pos))
                {
                    type = CellType.PathA;
                    cellColor = colorPath;
                    heightOffset = 0.1f;
                }
                else if (pathBCells.Contains(pos))
                {
                    type = CellType.PathB;
                    cellColor = colorPath;
                    heightOffset = 0.1f;
                }
                else if (commonCells.Contains(pos))
                {
                    type = CellType.CommonPath;
                    cellColor = colorPath;
                    heightOffset = 0.1f;
                }

                GameObject cellObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cellObj.name = $"Cell_{x}_{y}";
                cellObj.transform.parent = this.transform;
                cellObj.transform.position = new Vector3(x + 0.5f, heightOffset * 0.5f, y + 0.5f);
                cellObj.transform.localScale = new Vector3(0.95f, heightOffset, 0.95f);

                GridCell gridCell = cellObj.AddComponent<GridCell>();
                gridCell.Initialize(x, y, type, cellColor);

                if (type == CellType.PathA || type == CellType.PathB || type == CellType.CommonPath)
                {
                    AddPathIndicator(cellObj, type);
                }

                if (type == CellType.IceRock)
                {
                    AddIceRockDetail(cellObj);
                }

                gridMatrix[x, y] = gridCell;
            }
        }

        SpawnSpecialLandmarks();
        BuildWaypoints();
        ApplySpawnProtection();

        Debug.Log("Aegis Protocol Map initialized successfully according to GDD!");
    }

    void ApplySpawnProtection()
    {
        Vector2[] spawns = { new Vector2(0.5f, 2.5f), new Vector2(0.5f, 8.5f) };
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell c = gridMatrix[x, y];
                if (c == null || c.cellType != CellType.Buildable) continue;
                Vector2 center = new Vector2(x + 0.5f, y + 0.5f);
                foreach (Vector2 s in spawns)
                {
                    if (Vector2.Distance(center, s) <= spawnNoBuildRadius) { c.blockedBySpawn = true; break; }
                }
            }
        }
    }

    void AddPathIndicator(GameObject parentCell, CellType type)
    {
        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
        indicator.name = "DirectionMark";
        indicator.transform.parent = parentCell.transform;
        indicator.transform.localPosition = new Vector3(0f, 0.51f, 0f);
        indicator.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        indicator.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        Color indColor = type == CellType.PathA ? new Color(0.6f, 0.3f, 0.9f, 0.5f) :
                         type == CellType.PathB ? new Color(0.9f, 0.3f, 0.3f, 0.5f) :
                                                  new Color(0.9f, 0.6f, 0.2f, 0.5f);

        Renderer r = indicator.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = indColor;
        }

        Collider c = indicator.GetComponent<Collider>();
        if (c != null) Destroy(c);
    }

    void AddIceRockDetail(GameObject parentCell)
    {
        GameObject rockTop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rockTop.name = "RockCrystal";
        rockTop.transform.parent = parentCell.transform;
        rockTop.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        rockTop.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
        rockTop.transform.localRotation = Quaternion.Euler(15f, 25f, 10f);

        Renderer r = rockTop.GetComponent<Renderer>();
        if (r != null)
        {
            r.material = new Material(r.sharedMaterial);
            r.material.color = new Color(0.2f, 0.35f, 0.5f);
        }

        // Kaya modeli varsa kristal silindiri gizlenir, model kaya hucresinin ustune oturur
        if (ModelLibrary.Attach(parentCell, "rock") != null && r != null)
            r.enabled = false;
    }

    void SpawnSpecialLandmarks()
    {
        // 1. Purple Portal (-1, 2)
        GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        portal.name = "PurplePortal_GateA";
        portal.transform.parent = this.transform;
        portal.transform.position = new Vector3(-0.5f, 1f, 2.5f);
        portal.transform.localScale = new Vector3(1.2f, 0.3f, 1.2f);
        portal.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        portal.GetComponent<Renderer>().material.color = colorPortal;
        AddLight(portal, colorPortal, 4f, 2.5f);
        portalTransform = portal.transform;

        // 2. Red Gate (-1, 8)
        GameObject redGate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        redGate.name = "RedGate_GateB";
        redGate.transform.parent = this.transform;
        redGate.transform.position = new Vector3(-0.5f, 1f, 8.5f);
        redGate.transform.localScale = new Vector3(0.5f, 2f, 1.4f);
        redGate.GetComponent<Renderer>().material.color = colorRedGate;
        AddLight(redGate, colorRedGate, 4f, 2.5f);
        redGateTransform = redGate.transform;
        redGateRenderer = redGate.GetComponent<Renderer>();
        redGateLight = redGate.GetComponentInChildren<Light>();
        SetRedGateGlow(0.25f);

        // 3. Energy Core (16, 4) - 100 HP Target
        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "EnergyCore_Target";
        core.transform.parent = this.transform;
        core.transform.position = new Vector3(16.5f, 1.2f, 4.5f);
        core.transform.localScale = new Vector3(1.6f, 1.5f, 1.6f);
        core.GetComponent<Renderer>().material.color = colorCore;
        AddLight(core, colorCore, 7f, 3.5f);
        coreTransform = core.transform;
        ModelLibrary.Attach(core, "core");
    }

    void SetRedGateGlow(float k)
    {
        if (redGateRenderer != null)
            redGateRenderer.material.color = new Color(colorRedGate.r * k, colorRedGate.g * k, colorRedGate.b * k, 1f);
        if (redGateLight != null)
            redGateLight.intensity = 2.5f * k;
    }

    public void ActivateRedGate()
    {
        if (redGateActive) return;
        redGateActive = true;
        AegisAudio.Play("gate_open", 1f, false);
        StartCoroutine(BlinkRedGate());
    }

    IEnumerator BlinkRedGate()
    {
        for (int i = 0; i < 8; i++)
        {
            SetRedGateGlow(1.6f);
            yield return new WaitForSeconds(0.3f);
            SetRedGateGlow(0.25f);
            yield return new WaitForSeconds(0.3f);
        }
        SetRedGateGlow(1f);
    }

    void AddLight(GameObject parent, Color lightColor, float range, float intensity)
    {
        GameObject lightObj = new GameObject("GlowLight");
        lightObj.transform.parent = parent.transform;
        lightObj.transform.localPosition = Vector3.up * 0.5f;

        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = lightColor;
        l.range = range;
        l.intensity = intensity;
    }

    void BuildWaypoints()
    {
        Vector3[] rawPathA = new Vector3[] {
            new Vector3(-0.5f, 0.2f, 2.5f), // Portal
            new Vector3(0.5f, 0.2f, 2.5f),
            new Vector3(0.5f, 0.2f, 0.5f),
            new Vector3(8.5f, 0.2f, 0.5f),
            new Vector3(8.5f, 0.2f, 2.5f),
            new Vector3(5.5f, 0.2f, 2.5f),
            new Vector3(5.5f, 0.2f, 4.5f),
            new Vector3(8.5f, 0.2f, 4.5f),
            new Vector3(8.5f, 0.2f, 8.5f), // Merge M
            new Vector3(13.5f, 0.2f, 8.5f),
            new Vector3(13.5f, 0.2f, 6.5f),
            new Vector3(11.5f, 0.2f, 6.5f),
            new Vector3(11.5f, 0.2f, 4.5f),
            new Vector3(15.5f, 0.2f, 4.5f), // Stop point in front of core
            new Vector3(16.5f, 0.2f, 4.5f)  // Core
        };

        Vector3[] rawPathB = new Vector3[] {
            new Vector3(-0.5f, 0.2f, 8.5f), // Red Gate
            new Vector3(0.5f, 0.2f, 8.5f),
            new Vector3(1.5f, 0.2f, 8.5f),
            new Vector3(1.5f, 0.2f, 3.5f),
            new Vector3(3.5f, 0.2f, 3.5f),
            new Vector3(3.5f, 0.2f, 8.5f),
            new Vector3(8.5f, 0.2f, 8.5f), // Merge M
            new Vector3(13.5f, 0.2f, 8.5f),
            new Vector3(13.5f, 0.2f, 6.5f),
            new Vector3(11.5f, 0.2f, 6.5f),
            new Vector3(11.5f, 0.2f, 4.5f),
            new Vector3(15.5f, 0.2f, 4.5f),
            new Vector3(16.5f, 0.2f, 4.5f)
        };

        waypointsPathA = new List<Vector3>(rawPathA);
        waypointsPathB = new List<Vector3>(rawPathB);
    }

    public GridCell GetCell(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height && gridMatrix != null)
        {
            return gridMatrix[x, y];
        }
        return null;
    }

    public GridCell GetCellFromWorldPos(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt(worldPos.x);
        int y = Mathf.FloorToInt(worldPos.z);
        return GetCell(x, y);
    }

    private List<Vector2Int> GetPathACells()
    {
        return new List<Vector2Int> {
            new Vector2Int(0,2), new Vector2Int(0,1), new Vector2Int(0,0),
            new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
            new Vector2Int(5,0), new Vector2Int(6,0), new Vector2Int(7,0), new Vector2Int(8,0),
            new Vector2Int(8,1), new Vector2Int(8,2),
            new Vector2Int(7,2), new Vector2Int(6,2), new Vector2Int(5,2),
            new Vector2Int(5,3), new Vector2Int(5,4),
            new Vector2Int(6,4), new Vector2Int(7,4), new Vector2Int(8,4),
            new Vector2Int(8,5), new Vector2Int(8,6), new Vector2Int(8,7)
        };
    }

    private List<Vector2Int> GetPathBCells()
    {
        return new List<Vector2Int> {
            new Vector2Int(0,8), new Vector2Int(1,8),
            new Vector2Int(1,7), new Vector2Int(1,6), new Vector2Int(1,5), new Vector2Int(1,4), new Vector2Int(1,3),
            new Vector2Int(2,3), new Vector2Int(3,3),
            new Vector2Int(3,4), new Vector2Int(3,5), new Vector2Int(3,6), new Vector2Int(3,7), new Vector2Int(3,8),
            new Vector2Int(4,8), new Vector2Int(5,8), new Vector2Int(6,8), new Vector2Int(7,8)
        };
    }

    private List<Vector2Int> GetCommonPathCells()
    {
        return new List<Vector2Int> {
            new Vector2Int(8,8), new Vector2Int(9,8), new Vector2Int(10,8), new Vector2Int(11,8), new Vector2Int(12,8), new Vector2Int(13,8),
            new Vector2Int(13,7), new Vector2Int(13,6),
            new Vector2Int(12,6), new Vector2Int(11,6),
            new Vector2Int(11,5), new Vector2Int(11,4),
            new Vector2Int(12,4), new Vector2Int(13,4), new Vector2Int(14,4), new Vector2Int(15,4)
        };
    }

    private List<Vector2Int> GetRockCells()
    {
        return new List<Vector2Int> {
            new Vector2Int(2,1), new Vector2Int(12,1), new Vector2Int(15,2),
            new Vector2Int(10,6), new Vector2Int(14,7), new Vector2Int(0,9), new Vector2Int(15,9)
        };
    }
}