using UnityEngine;
using System;

public class Durability : MonoBehaviour
{
    [Header("Durability Settings")]
    public float maxDurability = 100f;
    public float currentDurability = 100f;

    public bool IsBroken => currentDurability <= 0.01f;

    public event Action<float, float> OnDurabilityChanged;

    void Start()
    {
        currentDurability = maxDurability;
        NotifyChange();
    }

    public void Consume(float amount)
    {
        if (IsBroken) return;

        currentDurability = Mathf.Max(0f, currentDurability - amount);
        NotifyChange();

        if (IsBroken)
        {
            Debug.Log($"Turret {gameObject.name} has worn out and stopped functioning!");
        }
    }

    public void RepairFull()
    {
        currentDurability = maxDurability;
        NotifyChange();
        Debug.Log($"Turret {gameObject.name} repaired to full durability!");
    }

    private void NotifyChange()
    {
        OnDurabilityChanged?.Invoke(currentDurability, maxDurability);
    }
}
