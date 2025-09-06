using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// すべての能力値を一元管理し、外部へイベントで通知するハブ
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats")]
    public float maxHP = 5f;               // 最大体力
    public float regenRate = 0.5f;         // HP/秒
    public float moveSpeedMul = 1f;        // PlayerController.baseSpeed × 係数

    [Header("Damage Multipliers")]
    public float normalDamage = 1f;        // 通常弾ダメージ倍率（従来通り）

    [Header("Charge Caps (x base caps in Shooter)")]
    [Tooltip("チャージショットの『サイズ上限』倍率（Shooter.baseChargeMaxSizeMul × これ）")]
    public float chargeMaxSizeCapMul = 1f;
    [Tooltip("チャージショットの『最大攻撃力上限』倍率（Shooter.baseChargeMaxDamage × これ）")]
    public float chargeMaxDamageCapMul = 1f;

    //――更新時イベント (UI や武器が購読)
    public UnityEvent<float> onMaxHpChanged = new();
    public UnityEvent<float> onRegenChanged = new();
    public UnityEvent<float> onSpeedChanged = new();
    // 互換用：チャージ強化の総合指標（最小値で代表値に）
    public UnityEvent<float> onChargeChanged = new();
    public UnityEvent<float> onNormalDmgChanged = new();

    // 追加：上限別のイベント（必要ならUIへ）
    public UnityEvent<float> onChargeMaxSizeCapChanged = new();
    public UnityEvent<float> onChargeMaxDamageCapChanged = new();

    // 内部参照
    Health hp;
    PlayerController pc;
    Shooter shooter;

    void Awake()
    {
        hp = GetComponent<Health>();
        pc = GetComponent<PlayerController>();
        shooter = GetComponentInChildren<Shooter>();

        ApplyAll(); // 初期反映
    }

    //――以下 public API ――
    public void AddMaxHP(float v)
    {
        maxHP += v;
        hp.SetMax(maxHP, true); // hp やイベント更新は Health 側で処理
        onMaxHpChanged.Invoke(maxHP);
        Debug.Log($"[PlayerStats] Max HP → {maxHP}");
    }

    public void AddRegen(float v)
    {
        regenRate += v;
        onRegenChanged.Invoke(regenRate);
        Debug.Log($"[PlayerStats] Regen → {regenRate}/s");
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul += mulStep;
        if (pc) pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
        Debug.Log($"[PlayerStats] MoveSpeedMul → {moveSpeedMul}");
    }

    /// <summary>
    /// 互換API：GrowthSystem から呼ばれる既存の「チャージ強化」。
    /// 仕様変更により、チャージの「サイズ上限」と「最大攻撃力上限」を同時に拡張します。
    /// </summary>
    public void AddCharge(float step)
    {
        // 上限倍率を同時に伸ばす（必要なら係数を分けてもOK）
        chargeMaxSizeCapMul += step;
        chargeMaxDamageCapMul += step;

        // Shooter へ反映
        if (shooter)
        {
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }

        // イベント（互換用と個別の両方）
        float representative = Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
        onChargeChanged.Invoke(representative);
        onChargeMaxSizeCapChanged.Invoke(chargeMaxSizeCapMul);
        onChargeMaxDamageCapChanged.Invoke(chargeMaxDamageCapMul);

        Debug.Log($"[PlayerStats] ChargeCaps → SizeCap x{chargeMaxSizeCapMul:F2}, DmgCap x{chargeMaxDamageCapMul:F2}");
    }

    public void AddNormalDamage(float v)
    {
        normalDamage += v;
        if (shooter) shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
        Debug.Log($"[PlayerStats] NormalDamageMul → {normalDamage}");
    }

    //――初期値を関係各所へ適用
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
