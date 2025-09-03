using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// ���ׂĂ̔\�͒l���ꌳ�Ǘ����A�O���փC�x���g�Œʒm����n�u
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    public float maxHP = 5f;     // �ő�̗�
    public float regenRate = 0.5f;   // HP/�b
    public float moveSpeedMul = 1f;     // PlayerController.baseSpeed �~ �W��
    public float chargePower = 1f;     // �`���[�W�V���b�g�{��
    public float normalDamage = 1f;     // �ʏ�e�_���[�W�{��

    //�\�\�X�V���C�x���g (UI �═�킪�w��)
    public UnityEvent<float> onMaxHpChanged = new();
    public UnityEvent<float> onRegenChanged = new();
    public UnityEvent<float> onSpeedChanged = new();
    public UnityEvent<float> onChargeChanged = new();
    public UnityEvent<float> onNormalDmgChanged = new();

    // �����Q��
    Health hp;
    PlayerController pc;
    Shooter shooter;

    void Awake()
    {
        hp = GetComponent<Health>();
        pc = GetComponent<PlayerController>();
        shooter = GetComponentInChildren<Shooter>();

        ApplyAll();                    // �������f
    }

    //�\�\�ȉ� public API �\�\
    public void AddMaxHP(float v)
    {
        maxHP += v;
        hp.SetMax(maxHP, true);      // hp ��C�x���g�X�V�� Health ���ŏ���
        onMaxHpChanged.Invoke(maxHP);
        Debug.Log($"[PlayerStats] Max HP increased to {maxHP}");
    }

    public void AddRegen(float v)
    {
        regenRate += v;
        onRegenChanged.Invoke(regenRate);
        Debug.Log($"[PlayerStats] Regen rate increased to {regenRate}HP/s");
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul += mulStep;
        pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
        Debug.Log($"[PlayerStats] Move speed multiplier increased to {moveSpeedMul}");
    }

    public void AddCharge(float v)
    {
        chargePower += v;
        shooter.chargePowerMul = chargePower;
        onChargeChanged.Invoke(chargePower);
        Debug.Log($"[PlayerStats] Charge power multiplier increased to {chargePower}");
    }

    public void AddNormalDamage(float v)
    {
        normalDamage += v;
        shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
        Debug.Log($"[PlayerStats] Normal damage multiplier increased to {normalDamage}");
    }

    //�\�\�����l���֌W�e���֓K�p
    void ApplyAll()
    {
        hp.maxHP = Mathf.RoundToInt(maxHP);
        pc.speedMul = moveSpeedMul;
        shooter.normalDamageMul = normalDamage;
        shooter.chargePowerMul = chargePower;
    }
}
