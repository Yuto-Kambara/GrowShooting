using UnityEngine;

public class GrowthSystem : MonoBehaviour
{
    public enum StatType { Attack, Move, Heal, MaxHP, Charge }
    [Header("���ݑI�𒆂̔\�́iC�Őؑցj")]
    public StatType selected = StatType.Attack;

    [Header("�����X�e�b�v�i1���z��������̌��ʗʁj")]
    public float attackFireRateStep = 0.15f;   // �A�˗́��i��/�b�j
    public float moveSpeedMulStep = 0.01f;   // �ړ��{����
    [Range(0f, 1f)] public float healPerAbsorb = 0.25f; // 4����HP+1 �Ȃ�
    [Range(0f, 1f)] public float maxHpPerAbsorb = 0.20f; // 5���ōő�HP+1
    public float chargeTimeStep = 0.02f;       // �`���[�W���ԁ��i�Z�k�j

    [Header("����E����")]
    public float fireRateCap = 20f;
    public float speedMulCap = 2.0f;
    public float minChargeTime = 0.60f;

    // �Q��
    PlayerController pc; Health hp; Shooter shooter;

    // �[���~�ρi��/�ő�HP�͒[���𒙂߂�j
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
        // C�őI��\�͂����[�e�[�V����
        if (Input.GetKeyDown(KeyCode.C))
        {
            selected = (StatType)(((int)selected + 1) % System.Enum.GetValues(typeof(StatType)).Length);
            Debug.Log($"[Growth] Selected: {selected}");
        }
    }

    /// <summary>
    /// �G�e�� n ���z���������ɌĂԁiAbsorbSystem ����R�[���j
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
                            hp.Heal(1);          // �オ�����Ԃ�1������
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
