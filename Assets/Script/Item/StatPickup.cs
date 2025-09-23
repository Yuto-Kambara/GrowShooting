// Assets/Scripts/Item/StatPickup.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StatPickup : MonoBehaviour
{
    [Header("Effect")]
    public GrowthSystem.StatType statType = GrowthSystem.StatType.MaxHP;
    [Min(1)] public int boostCount = 10;   // ← インスペクタで回数調整

    // GrowthSystem が見つからない場合のフォールバック値
    public float hpStepFallback = 0.5f;
    public float regenStepFallback = 0.05f;
    public float speedMulStepFallback = 0.02f;
    public float chargeStepFallback = 0.05f;
    public float damageStepFallback = 0.1f;

    [Header("Lifetime")]
    public float blinkAfter = 8f;          // 何秒後に点滅開始
    public float despawnAfter = 12f;       // 何秒後に消滅
    public float blinkInterval = 0.12f;

    [Header("FX / Misc")]
    public string playerTag = "Player";
    public Vector2 initialImpulse = new(0f, 1.2f); // ふわっと上に
    public bool autoResetRotation = true;

    SpriteRenderer sr;
    Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        TryGetComponent(out rb);

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (rb) rb.AddForce(initialImpulse, ForceMode2D.Impulse);
        StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        // 点滅まで待機
        yield return new WaitForSeconds(blinkAfter);

        // 点滅
        float t = 0f, dur = Mathf.Max(0f, despawnAfter - blinkAfter);
        while (t < dur)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (autoResetRotation) transform.rotation = Quaternion.identity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // プレイヤーの Stats / GrowthSystem を取得
        var stats = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        var growth = other.GetComponent<GrowthSystem>() ?? other.GetComponentInParent<GrowthSystem>();

        if (stats)
        {
            ApplyBoost(stats, growth);
        }

        // SE/エフェクトを入れるならここ
        Destroy(gameObject);
    }

    void ApplyBoost(PlayerStats stats, GrowthSystem growth)
    {
        // ステップ値を GS から読む（見つからなければフォールバック）
        float hp = growth ? growth.hpStep : hpStepFallback;
        float regen = growth ? growth.regenStep : regenStepFallback;
        float speed = growth ? growth.speedMulStep : speedMulStepFallback;
        float charge = growth ? growth.chargeStep : chargeStepFallback;
        float dmg = growth ? growth.damageStep : damageStepFallback;

        int n = Mathf.Max(1, boostCount);
        for (int i = 0; i < n; i++)
        {
            switch (statType)
            {
                case GrowthSystem.StatType.MaxHP: stats.AddMaxHP(hp); break;
                case GrowthSystem.StatType.Regen: stats.AddRegen(regen); break;
                case GrowthSystem.StatType.Speed: stats.AddSpeed(speed); break;
                case GrowthSystem.StatType.ChargePower: stats.AddCharge(charge); break;
                case GrowthSystem.StatType.NormalDamage: stats.AddNormalDamage(dmg); break;
            }
        }

        // もし GrowthSystem の UI 等を更新したいなら、ここでイベントを叩くのも可
        // growth?.OnEnergyAbsorbed(0); // 0回でも StatsChanged を起こしたい場合など
    }
}
