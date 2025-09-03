using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// すべての能力値を一元管理し、外部へイベントで通知するハブ
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    public float maxHP = 5f;     // 最大体力
    public float regenRate = 0.5f;   // HP/秒
    public float moveSpeedMul = 1f;     // PlayerController.baseSpeed × 係数
    public float chargePower = 1f;     // チャージショット倍率
    public float normalDamage = 1f;     // 通常弾ダメージ倍率

    //――更新時イベント (UI や武器が購読)
    public UnityEvent<float> onMaxHpChanged = new();
    public UnityEvent<float> onRegenChanged = new();
    public UnityEvent<float> onSpeedChanged = new();
    public UnityEvent<float> onChargeChanged = new();
    public UnityEvent<float> onNormalDmgChanged = new();

    // 内部参照
    Health hp;
    PlayerController pc;
    Shooter shooter;

    void Awake()
    {
        hp = GetComponent<Health>();
        pc = GetComponent<PlayerController>();
        shooter = GetComponentInChildren<Shooter>();

        ApplyAll();                    // 初期反映
    }

    //――以下 public API ――
    public void AddMaxHP(float v)
    {
        maxHP += v;
        hp.SetMax(maxHP, true);      // hp やイベント更新は Health 側で処理
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

    //――初期値を関係各所へ適用
    void ApplyAll()
    {
        hp.maxHP = Mathf.RoundToInt(maxHP);
        pc.speedMul = moveSpeedMul;
        shooter.normalDamageMul = normalDamage;
        shooter.chargePowerMul = chargePower;
    }
}
