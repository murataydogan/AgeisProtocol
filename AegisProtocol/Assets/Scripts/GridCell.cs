using UnityEngine;

public enum CellType
{
    Buildable,
    PathA,
    PathB,
    CommonPath,
    IceRock
}

public class GridCell : MonoBehaviour
{
    public int gridX;
    public int gridY;
    public CellType cellType;
    public GameObject placedTurret;
    public bool blockedBySpawn = false; // Portal/Kirmizi Kapi etrafinda insa yasagi

    private Renderer cellRenderer;
    private Color defaultColor;
    private Color hoverColor = new Color(0.35f, 0.85f, 1f, 1f);

    public bool IsBuildable => cellType == CellType.Buildable && placedTurret == null && !blockedBySpawn;

    public void Initialize(int x, int y, CellType type, Color color)
    {
        gridX = x;
        gridY = y;
        cellType = type;
        defaultColor = color;

        cellRenderer = GetComponent<Renderer>();
        if (cellRenderer != null)
        {
            cellRenderer.material = new Material(cellRenderer.sharedMaterial);
            cellRenderer.material.color = color;
        }
    }

    public void SetHover(bool isHovered, bool isValidRange = true)
    {
        if (cellRenderer == null) return;

        if (isHovered)
        {
            if (IsBuildable)
            {
                cellRenderer.material.color = isValidRange ? Color.green : Color.yellow;
            }
            else
            {
                cellRenderer.material.color = Color.red;
            }
        }
        else
        {
            cellRenderer.material.color = defaultColor;
        }
    }

    public Vector3 GetWorldCenter()
    {
        return transform.position + Vector3.up * 0.1f;
    }
}