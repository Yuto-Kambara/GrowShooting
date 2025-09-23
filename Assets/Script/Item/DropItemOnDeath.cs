// Assets/Scripts/Item/DropItemOnDeath.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class DropItemOnDeath : MonoBehaviour
{
    [Header("Drop")]
    public StatPickup pickupPrefab;
    [Range(0f, 1f)] public float dropChance = 1.0f;
    public Vector2 spawnOffset = Vector2.zero;

    [Header("Pickup Overrides")]
    public int boostCount = 10;
    public float blinkAfter = 8f;
    public float despawnAfter = 12f;
    public bool randomizeStatEveryDrop = true;

    void Awake()
    {
        var hp = GetComponent<Health>();
        if (hp) hp.onDeath.AddListener(OnDead);
    }

    void OnDead()
    {
        if (!pickupPrefab) return;
        if (Random.value > Mathf.Clamp01(dropChance)) return;

        var pos = (Vector2)transform.position + spawnOffset;
        var p = Instantiate(pickupPrefab, pos, Quaternion.identity);

        // è„èëÇ´
        p.boostCount = boostCount;
        p.blinkAfter = blinkAfter;
        p.despawnAfter = despawnAfter;

        if (randomizeStatEveryDrop)
        {
            int count = System.Enum.GetValues(typeof(GrowthSystem.StatType)).Length;
            p.statType = (GrowthSystem.StatType)Random.Range(0, count);
        }
    }
}
