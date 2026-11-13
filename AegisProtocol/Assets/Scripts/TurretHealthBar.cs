using UnityEngine;

public class TurretHealthBar : MonoBehaviour
{
    private Durability durability;
    private GameObject barBackground;
    private GameObject barFill;
    private Renderer fillRenderer;

    void Start()
    {
        durability = GetComponentInParent<Durability>();
        CreateWorldSpaceBar();

        if (durability != null)
        {
            durability.OnDurabilityChanged += UpdateBar;
        }
    }

    void CreateWorldSpaceBar()
    {
        // Background (Black/Gray)
        barBackground = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barBackground.name = "HealthBar_BG";
        barBackground.transform.parent = this.transform;
        barBackground.transform.localPosition = Vector3.up * 1.2f;
        barBackground.transform.localScale = new Vector3(0.9f, 0.15f, 1f);

        Collider bgCol = barBackground.GetComponent<Collider>();
        if (bgCol != null) Destroy(bgCol);

        Renderer bgRend = barBackground.GetComponent<Renderer>();
        if (bgRend != null)
        {
            bgRend.material = new Material(Shader.Find("Sprites/Default"));
            bgRend.material.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        }

        // Fill bar (Green -> Yellow -> Red)
        barFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barFill.name = "HealthBar_Fill";
        barFill.transform.parent = barBackground.transform;
        barFill.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        barFill.transform.localScale = Vector3.one;

        Collider fillCol = barFill.GetComponent<Collider>();
        if (fillCol != null) Destroy(fillCol);

        fillRenderer = barFill.GetComponent<Renderer>();
        if (fillRenderer != null)
        {
            fillRenderer.material = new Material(Shader.Find("Sprites/Default"));
            fillRenderer.material.color = Color.green;
        }
    }

    void LateUpdate()
    {
        // Face camera (Billboard)
        if (Camera.main != null && barBackground != null)
        {
            barBackground.transform.rotation = Camera.main.transform.rotation;
        }

        // Blinking red when below 30% durability
        if (durability != null && durability.currentDurability / durability.maxDurability <= 0.30f)
        {
            if (fillRenderer != null)
            {
                bool blink = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f;
                fillRenderer.material.color = blink ? Color.red : new Color(0.4f, 0f, 0f);
            }
        }
    }

    void UpdateBar(float current, float max)
    {
        if (barFill == null || fillRenderer == null) return;

        float ratio = Mathf.Clamp01(current / max);
        barFill.transform.localScale = new Vector3(ratio, 1f, 1f);
        barFill.transform.localPosition = new Vector3((ratio - 1f) * 0.5f, 0f, -0.01f);

        if (ratio > 0.6f)
        {
            fillRenderer.material.color = Color.green;
        }
        else if (ratio > 0.3f)
        {
            fillRenderer.material.color = Color.yellow;
        }
    }

    void OnDestroy()
    {
        if (durability != null)
        {
            durability.OnDurabilityChanged -= UpdateBar;
        }
    }
}
