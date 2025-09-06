using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ���ׂĂ̔\�͒l���ꌳ�Ǘ����A�O���փC�x���g�Œʒm����n�u
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHP = 5f;               // �ő�̗�
    public float regenRate = 0.5f;         // HP/�b
    public float moveSpeedMul = 1f;        // PlayerController.baseSpeed �~ �W��

    [Header("Damage Multipliers")]
    public float normalDamage = 1f;        // �ʏ�e�_���[�W�{���i�]���ʂ�j

    [Header("Charge Caps (x base caps in Shooter)")]
    [Tooltip("�`���[�W�V���b�g�́w�T�C�Y����x�{���iShooter.baseChargeMaxSizeMul �~ ����j")]
    public float chargeMaxSizeCapMul = 1f;
    [Tooltip("�`���[�W�V���b�g�́w�ő�U���͏���x�{���iShooter.baseChargeMaxDamage �~ ����j")]
    public float chargeMaxDamageCapMul = 1f;

    //�\�\�X�V���C�x���g (UI �═�킪�w��)
    public UnityEvent<float> onMaxHpChanged = new();
    public UnityEvent<float> onRegenChanged = new();
    public UnityEvent<float> onSpeedChanged = new();
    // �݊��p�F�`���[�W�����̑����w�W�i�ŏ��l�ő�\�l�Ɂj
    public UnityEvent<float> onChargeChanged = new();
    public UnityEvent<float> onNormalDmgChanged = new();

    // �ǉ��F����ʂ̃C�x���g�i�K�v�Ȃ�UI�ցj
    public UnityEvent<float> onChargeMaxSizeCapChanged = new();
    public UnityEvent<float> onChargeMaxDamageCapChanged = new();

    // �����Q��
    Health hp;
    PlayerController pc;
    Shooter shooter;

    void Awake()
    {
        hp = GetComponent<Health>();
        pc = GetComponent<PlayerController>();
        shooter = GetComponentInChildren<Shooter>();

        ApplyAll(); // �������f
    }

    //�\�\�ȉ� public API �\�\
    public void AddMaxHP(float v)
    {
        maxHP += v;
        hp.SetMax(maxHP, true); // hp ��C�x���g�X�V�� Health ���ŏ���
        onMaxHpChanged.Invoke(maxHP);
        Debug.Log($"[PlayerStats] Max HP �� {maxHP}");
    }

    public void AddRegen(float v)
    {
        regenRate += v;
        onRegenChanged.Invoke(regenRate);
        Debug.Log($"[PlayerStats] Regen �� {regenRate}/s");
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul += mulStep;
        if (pc) pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
        Debug.Log($"[PlayerStats] MoveSpeedMul �� {moveSpeedMul}");
    }

    /// <summary>
    /// �݊�API�FGrowthSystem ����Ă΂������́u�`���[�W�����v�B
    /// �d�l�ύX�ɂ��A�`���[�W�́u�T�C�Y����v�Ɓu�ő�U���͏���v�𓯎��Ɋg�����܂��B
    /// </summary>
    public void AddCharge(float step)
    {
        // ����{���𓯎��ɐL�΂��i�K�v�Ȃ�W���𕪂��Ă�OK�j
        chargeMaxSizeCapMul += step;
        chargeMaxDamageCapMul += step;

        // Shooter �֔��f
        if (shooter)
        {
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }

        // �C�x���g�i�݊��p�ƌʂ̗����j
        float representative = Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
        onChargeChanged.Invoke(representative);
        onChargeMaxSizeCapChanged.Invoke(chargeMaxSizeCapMul);
        onChargeMaxDamageCapChanged.Invoke(chargeMaxDamageCapMul);

        Debug.Log($"[PlayerStats] ChargeCaps �� SizeCap x{chargeMaxSizeCapMul:F2}, DmgCap x{chargeMaxDamageCapMul:F2}");
    }

    public void AddNormalDamage(float v)
    {
        normalDamage += v;
        if (shooter) shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
        Debug.Log($"[PlayerStats] NormalDamageMul �� {normalDamage}");
    }

    //�\�\�����l���֌W�e���֓K�p
    void ApplyAll()
    {
        if (hp) hp.maxHP = Mathf.RoundToInt(maxHP);
        if (pc) pc.speedMul = moveSpeedMul;

        if (shooter)
        {
            shooter.normalDamageMul = normalDamage;
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }
    }
}
