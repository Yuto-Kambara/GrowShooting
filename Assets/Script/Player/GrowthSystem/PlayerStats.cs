using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ���ׂĂ̔\�͒l���ꌳ�Ǘ����A�O���փC�x���g�Œʒm����n�u�i������j
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats (current)")]
    public float maxHP = 5f;               // ���݂̍ő�̗�
    public float regenRate = 0.5f;         // ���݂̉�/�b
    public float moveSpeedMul = 1f;        // ���݂̈ړ��{���ibase�~mul�j

    [Header("Damage Multipliers (current)")]
    public float normalDamage = 1f;        // ���݂̒ʏ�_���{��

    [Header("Caps (absolute upper bounds)")]
    public float maxHP_Cap = 20f;
    public float regenRate_Cap = 2.0f;
    public float moveSpeedMul_Cap = 1.8f;
    public float normalDamage_Cap = 3.0f;

    [Header("Charge Caps (current, used by Shooter)")]
    [Tooltip("�`���[�W�e�́w�T�C�Y����x�{���iShooter ���̊�ɑ΂��錻�ݒl�j")]
    public float chargeMaxSizeCapMul = 1f;
    [Tooltip("�`���[�W�e�́w�ő�U���͏���x�{���iShooter ���̊�ɑ΂��錻�ݒl�j")]
    public float chargeMaxDamageCapMul = 1f;

    [Header("Charge Cap-of-Caps (absolute upper bounds)")]
    [Tooltip("�T�C�Y����{���̍ŏ��")]
    public float chargeMaxSizeCapMul_Cap = 3.0f;
    [Tooltip("�U���͏���{���̍ŏ��")]
    public float chargeMaxDamageCapMul_Cap = 3.0f;

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

    // ===== ���J�v���p�e�B�iBinder ����Q�Ɓj =====
    public float MaxHP => maxHP;
    public float MaxHP_Cap => maxHP_Cap;
    public float RegenPerSec => regenRate;
    public float RegenPerSec_Cap => regenRate_Cap;
    public float SpeedMul => moveSpeedMul;
    public float SpeedMul_Cap => moveSpeedMul_Cap;
    public float NormalDamageMul => normalDamage;
    public float NormalDamageMul_Cap => normalDamage_Cap;

    // ���[�_�[�p�F�`���[�W�̑�\�l�i���݁^�ŏ���j
    public float ChargePowerMul => Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
    public float ChargePowerMul_Cap => Mathf.Min(chargeMaxSizeCapMul_Cap, chargeMaxDamageCapMul_Cap);

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
        maxHP = Mathf.Min(maxHP + v, maxHP_Cap);
        if (hp) hp.SetMax(maxHP, true);
        onMaxHpChanged.Invoke(maxHP);
        Debug.Log($"[PlayerStats] Max HP �� {maxHP} / Cap {maxHP_Cap}");
    }

    public void AddRegen(float v)
    {
        regenRate = Mathf.Min(regenRate + v, regenRate_Cap);
        onRegenChanged.Invoke(regenRate);
        Debug.Log($"[PlayerStats] Regen �� {regenRate}/s / Cap {regenRate_Cap}");
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul = Mathf.Min(moveSpeedMul + mulStep, moveSpeedMul_Cap);
        if (pc) pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
        Debug.Log($"[PlayerStats] MoveSpeedMul �� {moveSpeedMul} / Cap {moveSpeedMul_Cap}");
    }

    /// <summary>
    /// �݊�API�FGrowthSystem ����Ă΂������́u�`���[�W�����v�B
    /// �d�l�F�`���[�W�́u�T�C�Y����v�u�ő�U���͏���v�𓯎��Ɋg���i���ꂼ��̍ŏ���܂Łj�B
    /// </summary>
    public void AddCharge(float step)
    {
        chargeMaxSizeCapMul = Mathf.Min(chargeMaxSizeCapMul + step, chargeMaxSizeCapMul_Cap);
        chargeMaxDamageCapMul = Mathf.Min(chargeMaxDamageCapMul + step, chargeMaxDamageCapMul_Cap);

        // Shooter �֔��f
        if (shooter)
        {
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }

        // �C�x���g
        float representative = ChargePowerMul;
        onChargeChanged.Invoke(representative);
        onChargeMaxSizeCapChanged.Invoke(chargeMaxSizeCapMul);
        onChargeMaxDamageCapChanged.Invoke(chargeMaxDamageCapMul);

        Debug.Log($"[PlayerStats] ChargeCaps �� SizeCap x{chargeMaxSizeCapMul:F2} /{chargeMaxSizeCapMul_Cap:F2}, " +
                  $"DmgCap x{chargeMaxDamageCapMul:F2} /{chargeMaxDamageCapMul_Cap:F2}");
    }

    public void AddNormalDamage(float v)
    {
        normalDamage = Mathf.Min(normalDamage + v, normalDamage_Cap);
        if (shooter) shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
        Debug.Log($"[PlayerStats] NormalDamageMul �� {normalDamage} / Cap {normalDamage_Cap}");
    }

    //�\�\�����l���֌W�e���֓K�p
    void ApplyAll()
    {
        // ���ݒl��������ŃN�����v���Đ�������ۂ�
        maxHP = Mathf.Min(maxHP, maxHP_Cap);
        regenRate = Mathf.Min(regenRate, regenRate_Cap);
        moveSpeedMul = Mathf.Min(moveSpeedMul, moveSpeedMul_Cap);
        normalDamage = Mathf.Min(normalDamage, normalDamage_Cap);
        chargeMaxSizeCapMul = Mathf.Min(chargeMaxSizeCapMul, chargeMaxSizeCapMul_Cap);
        chargeMaxDamageCapMul = Mathf.Min(chargeMaxDamageCapMul, chargeMaxDamageCapMul_Cap);

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
