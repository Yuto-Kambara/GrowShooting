using UnityEngine;

public class GrowthSystem : MonoBehaviour
{
    public enum StatType { Attack, Move, Heal, MaxHP, Charge }
    [Header("現在選択中の能力（Cで切替）")]
    public StatType selected = StatType.Attack;

    [Header("成長ステップ（1発吸収あたりの効果量）")]
    public float attackFireRateStep = 0.15f;   // 連射力↑（発/秒）
    public float moveSpeedMulStep = 0.01f;   // 移動倍率↑
    [Range(0f, 1f)] public float healPerAbsorb = 0.25f; // 4発でHP+1 など
    [Range(0f, 1f)] public float maxHpPerAbsorb = 0.20f; // 5発で最大HP+1
    public float chargeTimeStep = 0.02f;       // チャージ時間↓（短縮）

    [Header("上限・下限")]
    public float fireRateCap = 20f;
    public float speedMulCap = 2.0f;
    public float minChargeTime = 0.60f;

    // 参照
    PlayerController pc; Health hp; Shooter shooter;

    // 端数蓄積（回復/最大HPは端数を貯める）
    float healProgress;
    float maxHpProgress;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        hp = GetComponent<Health>();
        shooter = GetComponentInChildren<Shooter>();
    }

    void Update()
    {
        // Cで選択能力をローテーション
        if (Input.GetKeyDown(KeyCode.C))
        {
            selected = (StatType)(((int)selected + 1) % System.Enum.GetValues(typeof(StatType)).Length);
            Debug.Log($"[Growth] Selected: {selected}");
        }
    }

    /// <summary>
    /// 敵弾を n 発吸収した時に呼ぶ（AbsorbSystem からコール）
    /// </summary>
    public void OnEnergyAbsorbed(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            switch (selected)
            {
                case StatType.Attack:
                    if (shooter) shooter.fireRate = Mathf.Min(fireRateCap, shooter.fireRate + attackFireRateStep);
                    break;

                case StatType.Move:
                    if (pc) pc.speedMul = Mathf.Min(speedMulCap, pc.speedMul + moveSpeedMulStep);
                    break;

                case StatType.Heal:
                    if (hp)
                    {
                        healProgress += healPerAbsorb;
                        while (healProgress >= 1f)
                        {
                            hp.Heal(1);
                            healProgress -= 1f;
                        }
                    }
                    break;

                case StatType.MaxHP:
                    if (hp)
                    {
                        maxHpProgress += maxHpPerAbsorb;
                        while (maxHpProgress >= 1f)
                        {
                            hp.maxHP += 1;
                            hp.Heal(1);          // 上がったぶん1だけ回復
                            maxHpProgress -= 1f;
                        }
                    }
                    break;

                case StatType.Charge:
                    if (shooter) shooter.chargeTime = Mathf.Max(minChargeTime, shooter.chargeTime - chargeTimeStep);
                    break;
            }
        }
    }
}
