// Assets/Script/Player/GrowthSystem/GrowthSystem.cs
using UnityEngine;

public class GrowthSystem : MonoBehaviour
{
    public enum StatType { MaxHP, Regen, Speed, ChargePower, NormalDamage }

    [Header("選択中（C/Eで右、Qで左に巡回）")]
    public StatType selected = StatType.MaxHP;

    [Header("吸収1発あたりの増分")]
    public float hpStep = 0.5f;
    public float regenStep = 0.05f;
    public float speedMulStep = 0.02f;
    public float chargeStep = 0.05f;

    // ★ 変更：通常攻撃は「攻撃力」と「連射速度(加算)」の両方を上げる
    public float damageStep = 0.1f;   // ダメ倍率 +0.1
    public float rapidStep = 0.5f;   // 連射速度 +0.5 発/秒（加算）

    PlayerStats stats;

    public event System.Action StatsChanged;

    void Awake() => stats = GetComponent<PlayerStats>();

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) SelectNext();
        else if (Input.GetKeyDown(KeyCode.E)) SelectPrev();
    }

    public void SelectNext()
    {
        int n = System.Enum.GetValues(typeof(StatType)).Length;
        selected = (StatType)(((int)selected + 1) % n);
    }

    public void SelectPrev()
    {
        int n = System.Enum.GetValues(typeof(StatType)).Length;
        selected = (StatType)(((int)selected - 1 + n) % n);
    }

    public void OnEnergyAbsorbed(int count = 1)
    {
        if (!stats) return;
        for (int i = 0; i < count; i++)
        {
            switch (selected)
            {
                case StatType.MaxHP: stats.AddMaxHP(hpStep); break;
                case StatType.Regen: stats.AddRegen(regenStep); break;
                case StatType.Speed: stats.AddSpeed(speedMulStep); break;
                case StatType.ChargePower: stats.AddCharge(chargeStep); break;

                // ★ ここで両方上げる（攻撃力 + 連射速度(加算)）
                case StatType.NormalDamage:
                    stats.AddNormalDamage(damageStep);
                    stats.AddNormalFireRate(rapidStep);
                    break;
            }
        }
        StatsChanged?.Invoke();
    }
}
