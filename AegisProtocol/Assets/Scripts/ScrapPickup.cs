using UnityEngine;

public class ScrapPickup : MonoBehaviour
{
    public int scrapAmount = 6;
    public float lifetime = 15f;    // GDD: disappears after 15 seconds
    public float blinkStart = 5f;   // GDD: blinks in final 5 seconds

    private float timer = 0f;
    private Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
    }

    public void Initialize(int amount)
    {
        scrapAmount = amount;
    }

    void Update()
    {
        timer += Time.deltaTime;
        float remaining = lifetime - timer;

        // Auto-collect when Engineer is within 1 tile
        if (EngineerController.Instance != null)
        {
            float dist = Vector3.Distance(transform.position, EngineerController.Instance.transform.position);
            if (dist <= EngineerController.Instance.scrapPickupRange + 0.5f)
            {
                Collect();
                return;
            }
        }

        // Blink in last 5 seconds
        if (remaining <= blinkStart && rend != null)
        {
            rend.enabled = Mathf.PingPong(Time.time * 6f, 1f) > 0.5f;
        }

        // Destroy when lifetime ends
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    void Collect()
    {
        if (GameEconomy.Instance != null)
            GameEconomy.Instance.AddScrap(scrapAmount);

        AegisAudio.Play("scrap", 0.6f);
        Destroy(gameObject);
    }
}