using UnityEngine;
using static UnityEngine.CullingGroup;

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
    public float damageStep = 0.1f;

    PlayerStats stats;

    public event System.Action StatsChanged;

    void Awake() => stats = GetComponent<PlayerStats>();

    void Update()
    {
        // 右回り：E（新規）／C（互換）
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SelectNext();
        }
        // 左回り：Q（新規）
        else if (Input.GetKeyDown(KeyCode.E))
        {
            SelectPrev();
        }
    }

    // ---- 公開API：外部（UIなど）からも呼べるように ----
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
                case StatType.NormalDamage: stats.AddNormalDamage(damageStep); break;
            }
        }
        StatsChanged?.Invoke();
    }
}
