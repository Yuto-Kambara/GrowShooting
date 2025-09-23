// Assets/Script/Player/GrowthSystem/PlayerStats.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// すべての能力値を一元管理し、外部へイベントで通知するハブ（上限つき）
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats (current)")]
    public float maxHP = 5f;
    public float regenRate = 0.5f;
    public float moveSpeedMul = 1f;

    [Header("Damage Multipliers (current)")]
    public float normalDamage = 1f;           // 既存：通常弾ダメ倍率

    // ★ 追加：通常弾の連射速度（発/秒）への “加算” 値
    [Header("Normal Rapid (current, additive shots/sec)")]
    public float normalRapidAdd = 0f;         // 例：+0.5 で 0.5発/秒 早くなる

    [Header("Caps (absolute upper bounds)")]
    public float maxHP_Cap = 20f;
    public float regenRate_Cap = 2.0f;
    public float moveSpeedMul_Cap = 1.8f;
    public float normalDamage_Cap = 3.0f;
    public float normalRapidAdd_Cap = 6.0f;   // ★ 追加：連射加算の上限（お好みで）

    [Header("Charge Caps (current, used by Shooter)")]
    public float chargeMaxSizeCapMul = 1f;
    public float chargeMaxDamageCapMul = 1f;

    [Header("Charge Cap-of-Caps (absolute upper bounds)")]
    public float chargeMaxSizeCapMul_Cap = 3.0f;
    public float chargeMaxDamageCapMul_Cap = 3.0f;

    //――更新時イベント
    public UnityEvent<float> onMaxHpChanged = new();
    public UnityEvent<float> onRegenChanged = new();
    public UnityEvent<float> onSpeedChanged = new();
    public UnityEvent<float> onChargeChanged = new();
    public UnityEvent<float> onNormalDmgChanged = new();

    // ★ 追加：連射速度変更イベント（必要ならUIで使用）
    public UnityEvent<float> onNormalRapidChanged = new();

    // 内部参照
    Health hp;
    PlayerController pc;
    Shooter shooter;

    // 公開プロパティ
    public float MaxHP => maxHP;
    public float MaxHP_Cap => maxHP_Cap;
    public float RegenPerSec => regenRate;
    public float RegenPerSec_Cap => regenRate_Cap;
    public float SpeedMul => moveSpeedMul;
    public float SpeedMul_Cap => moveSpeedMul_Cap;
    public float NormalDamageMul => normalDamage;
    public float NormalDamageMul_Cap => normalDamage_Cap;
    public float ChargePowerMul => Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
    public float ChargePowerMul_Cap => Mathf.Min(chargeMaxSizeCapMul_Cap, chargeMaxDamageCapMul_Cap);

    void Awake()
    {
        hp = GetComponent<Health>();
        pc = GetComponent<PlayerController>();
        shooter = GetComponentInChildren<Shooter>();
        ApplyAll(); // 初期反映
    }

    public void AddMaxHP(float v)
    {
        maxHP = Mathf.Min(maxHP + v, maxHP_Cap);
        if (hp) hp.SetMax(maxHP, true);
        onMaxHpChanged.Invoke(maxHP);
    }

    public void AddRegen(float v)
    {
        regenRate = Mathf.Min(regenRate + v, regenRate_Cap);
        onRegenChanged.Invoke(regenRate);
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul = Mathf.Min(moveSpeedMul + mulStep, moveSpeedMul_Cap);
        if (pc) pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
    }

    public void AddCharge(float step)
    {
        chargeMaxSizeCapMul = Mathf.Min(chargeMaxSizeCapMul + step, chargeMaxSizeCapMul_Cap);
        chargeMaxDamageCapMul = Mathf.Min(chargeMaxDamageCapMul + step, chargeMaxDamageCapMul_Cap);
        if (shooter)
        {
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }
        float representative = Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
        onChargeChanged.Invoke(representative);
    }

    public void AddNormalDamage(float v)
    {
        normalDamage = Mathf.Min(normalDamage + v, normalDamage_Cap);
        if (shooter) shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
    }

    // ★ 追加：通常弾の連射速度（発/秒）を “加算” で強化
    public void AddNormalFireRate(float addShotsPerSec)
    {
        normalRapidAdd = Mathf.Min(normalRapidAdd + Mathf.Max(0f, addShotsPerSec), normalRapidAdd_Cap);
        if (shooter) shooter.normalFireRateAdd = normalRapidAdd;
        onNormalRapidChanged.Invoke(normalRapidAdd);
    }

    void ApplyAll()
    {
        // 上限クランプ
        maxHP = Mathf.Min(maxHP, maxHP_Cap);
        regenRate = Mathf.Min(regenRate, regenRate_Cap);
        moveSpeedMul = Mathf.Min(moveSpeedMul, moveSpeedMul_Cap);
        normalDamage = Mathf.Min(normalDamage, normalDamage_Cap);
        normalRapidAdd = Mathf.Min(normalRapidAdd, normalRapidAdd_Cap);

        if (hp) hp.maxHP = Mathf.RoundToInt(maxHP);
        if (pc) pc.speedMul = moveSpeedMul;

        if (shooter)
        {
            shooter.normalDamageMul = normalDamage;     // 既存
            shooter.normalFireRateAdd = normalRapidAdd; // ★ 追加
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }
    }
}
