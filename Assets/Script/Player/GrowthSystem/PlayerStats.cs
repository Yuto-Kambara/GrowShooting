using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// すべての能力値を一元管理し、外部へイベントで通知するハブ（上限つき）
/// </summary>
[RequireComponent(typeof(Health), typeof(PlayerController))]
public class PlayerStats : MonoBehaviour
{
    [Header("Core Stats (current)")]
    public float maxHP = 5f;               // 現在の最大体力
    public float regenRate = 0.5f;         // 現在の回復/秒
    public float moveSpeedMul = 1f;        // 現在の移動倍率（base×mul）

    [Header("Damage Multipliers (current)")]
    public float normalDamage = 1f;        // 現在の通常ダメ倍率

    [Header("Caps (absolute upper bounds)")]
    public float maxHP_Cap = 20f;
    public float regenRate_Cap = 2.0f;
    public float moveSpeedMul_Cap = 1.8f;
    public float normalDamage_Cap = 3.0f;

    [Header("Charge Caps (current, used by Shooter)")]
    [Tooltip("チャージ弾の『サイズ上限』倍率（Shooter 側の基準に対する現在値）")]
    public float chargeMaxSizeCapMul = 1f;
    [Tooltip("チャージ弾の『最大攻撃力上限』倍率（Shooter 側の基準に対する現在値）")]
    public float chargeMaxDamageCapMul = 1f;

    [Header("Charge Cap-of-Caps (absolute upper bounds)")]
    [Tooltip("サイズ上限倍率の最上限")]
    public float chargeMaxSizeCapMul_Cap = 3.0f;
    [Tooltip("攻撃力上限倍率の最上限")]
    public float chargeMaxDamageCapMul_Cap = 3.0f;

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

    // ===== 公開プロパティ（Binder から参照） =====
    public float MaxHP => maxHP;
    public float MaxHP_Cap => maxHP_Cap;
    public float RegenPerSec => regenRate;
    public float RegenPerSec_Cap => regenRate_Cap;
    public float SpeedMul => moveSpeedMul;
    public float SpeedMul_Cap => moveSpeedMul_Cap;
    public float NormalDamageMul => normalDamage;
    public float NormalDamageMul_Cap => normalDamage_Cap;

    // レーダー用：チャージの代表値（現在／最上限）
    public float ChargePowerMul => Mathf.Min(chargeMaxSizeCapMul, chargeMaxDamageCapMul);
    public float ChargePowerMul_Cap => Mathf.Min(chargeMaxSizeCapMul_Cap, chargeMaxDamageCapMul_Cap);

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
        maxHP = Mathf.Min(maxHP + v, maxHP_Cap);
        if (hp) hp.SetMax(maxHP, true);
        onMaxHpChanged.Invoke(maxHP);
        Debug.Log($"[PlayerStats] Max HP → {maxHP} / Cap {maxHP_Cap}");
    }

    public void AddRegen(float v)
    {
        regenRate = Mathf.Min(regenRate + v, regenRate_Cap);
        onRegenChanged.Invoke(regenRate);
        Debug.Log($"[PlayerStats] Regen → {regenRate}/s / Cap {regenRate_Cap}");
    }

    public void AddSpeed(float mulStep)
    {
        moveSpeedMul = Mathf.Min(moveSpeedMul + mulStep, moveSpeedMul_Cap);
        if (pc) pc.speedMul = moveSpeedMul;
        onSpeedChanged.Invoke(moveSpeedMul);
        Debug.Log($"[PlayerStats] MoveSpeedMul → {moveSpeedMul} / Cap {moveSpeedMul_Cap}");
    }

    /// <summary>
    /// 互換API：GrowthSystem から呼ばれる既存の「チャージ強化」。
    /// 仕様：チャージの「サイズ上限」「最大攻撃力上限」を同時に拡張（それぞれの最上限まで）。
    /// </summary>
    public void AddCharge(float step)
    {
        chargeMaxSizeCapMul = Mathf.Min(chargeMaxSizeCapMul + step, chargeMaxSizeCapMul_Cap);
        chargeMaxDamageCapMul = Mathf.Min(chargeMaxDamageCapMul + step, chargeMaxDamageCapMul_Cap);

        // Shooter へ反映
        if (shooter)
        {
            shooter.chargeMaxSizeMul = chargeMaxSizeCapMul;
            shooter.chargeMaxDamageMul = chargeMaxDamageCapMul;
        }

        // イベント
        float representative = ChargePowerMul;
        onChargeChanged.Invoke(representative);
        onChargeMaxSizeCapChanged.Invoke(chargeMaxSizeCapMul);
        onChargeMaxDamageCapChanged.Invoke(chargeMaxDamageCapMul);

        Debug.Log($"[PlayerStats] ChargeCaps → SizeCap x{chargeMaxSizeCapMul:F2} /{chargeMaxSizeCapMul_Cap:F2}, " +
                  $"DmgCap x{chargeMaxDamageCapMul:F2} /{chargeMaxDamageCapMul_Cap:F2}");
    }

    public void AddNormalDamage(float v)
    {
        normalDamage = Mathf.Min(normalDamage + v, normalDamage_Cap);
        if (shooter) shooter.normalDamageMul = normalDamage;
        onNormalDmgChanged.Invoke(normalDamage);
        Debug.Log($"[PlayerStats] NormalDamageMul → {normalDamage} / Cap {normalDamage_Cap}");
    }

    //――初期値を関係各所へ適用
    void ApplyAll()
    {
        // 現在値側も上限でクランプして整合性を保つ
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
