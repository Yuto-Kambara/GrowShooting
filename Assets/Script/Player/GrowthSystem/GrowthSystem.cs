using UnityEngine;
using static UnityEngine.CullingGroup;

public class GrowthSystem : MonoBehaviour
{
    public enum StatType { MaxHP, Regen, Speed, ChargePower, NormalDamage }

    [Header("‘I‘ð’†iC‚Å„‰ñj")]
    public StatType selected = StatType.MaxHP;

    [Header("‹zŽû1”­‚ ‚½‚è‚Ì‘•ª")]
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
        if (Input.GetKeyDown(KeyCode.C))
        {
            selected = (StatType)(((int)selected + 1) % System.Enum.GetValues(typeof(StatType)).Length);
        }
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
