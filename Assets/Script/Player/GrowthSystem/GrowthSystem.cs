// Assets/Script/Player/GrowthSystem/GrowthSystem.cs
using UnityEngine;

public class GrowthSystem : MonoBehaviour
{
    public enum StatType { MaxHP, Regen, Speed, ChargePower, NormalDamage }

    [Header("�I�𒆁iC/E�ŉE�AQ�ō��ɏ���j")]
    public StatType selected = StatType.MaxHP;

    [Header("�z��1��������̑���")]
    public float hpStep = 0.5f;
    public float regenStep = 0.05f;
    public float speedMulStep = 0.02f;
    public float chargeStep = 0.05f;

    // �� �ύX�F�ʏ�U���́u�U���́v�Ɓu�A�ˑ��x(���Z)�v�̗������グ��
    public float damageStep = 0.1f;   // �_���{�� +0.1
    public float rapidStep = 0.5f;   // �A�ˑ��x +0.5 ��/�b�i���Z�j

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

                // �� �����ŗ����グ��i�U���� + �A�ˑ��x(���Z)�j
                case StatType.NormalDamage:
                    stats.AddNormalDamage(damageStep);
                    stats.AddNormalFireRate(rapidStep);
                    break;
            }
        }
        StatsChanged?.Invoke();
    }
}
