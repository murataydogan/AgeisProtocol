using UnityEngine;

// Attach this to any turret. Draws a range ring on the ground when Engineer is nearby.
public class TurretRangeVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private TurretController turret;
    private bool isVisible = false;
    private const int segments = 48;

    void Start()
    {
        turret = GetComponent<TurretController>();

        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments;
        lineRenderer.startWidth = 0.07f;
        lineRenderer.endWidth = 0.07f;
        lineRenderer.useWorldSpace = true;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.material = mat;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (turret == null) return;

        bool engineerNearby = EngineerController.Instance != null &&
                              EngineerController.Instance.IsInRangeForTurretInteract(transform.position);

        if (engineerNearby != isVisible)
        {
            isVisible = engineerNearby;
            lineRenderer.enabled = isVisible;
        }

        if (isVisible)
        {
            DrawRangeCircle();
        }
    }

    void DrawRangeCircle()
    {
        float r = turret.range;
        Color ringColor = turret.turretType == TurretType.LaserTurret ? new Color(1f, 0.3f, 0.3f, 0.9f)
                        : turret.turretType == TurretType.SlowTower  ? new Color(0.2f, 0.8f, 1f, 0.9f)
                                                                      : new Color(1f, 1f, 0.2f, 0.9f);

        lineRenderer.startColor = ringColor;
        lineRenderer.endColor   = ringColor;

        Vector3 center = new Vector3(transform.position.x, 0.08f, transform.position.z);

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * r;
            float z = Mathf.Sin(angle) * r;
            lineRenderer.SetPosition(i, center + new Vector3(x, 0f, z));
        }
    }
}
