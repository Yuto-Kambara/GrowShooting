using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;

    [Header("Normal Fire")]
    public float fireRate = 8f;
    [SerializeField] private KeyCode normalKey = KeyCode.K;

    [Header("Charge Fire (hold L)")]
    [SerializeField] private KeyCode chargeKey = KeyCode.L;
    public float chargeMaxTime = 2.5f;     // ここまでホールドで上限到達
    public AnimationCurve chargeCurve;     // 0→1成長カーブ（未設定なら線形）
    public float previewOffset = 0.2f;     // 自機前の表示距離

    [Header("Caps (Base)")]
    public float baseChargeMaxSizeMul = 1.0f;   // サイズ上限（基準スケール×これ）
    public float baseChargeMaxDamage = 1.0f;     // ダメージ上限（絶対値）

    [Header("Growth Hooks (x上限)")]
    [HideInInspector] public float chargeMaxSizeMul = 1f;    // 能力強化でサイズ上限を拡張
    [HideInInspector] public float chargeMaxDamageMul = 1f;  // 能力強化で最大攻撃力を拡張

    [Header("Other Growth Hooks")]
    [HideInInspector] public float normalDamageMul = 1f;     // 通常弾用（既存）
    // ※チャージ弾の“最大”は上記2つで拡張。必要なら通常加算分を初期値へ乗せてもOK。

    private ObjectPool normalPool;
    private ObjectPool chargePool;

    // 内部
    float normalCd;
    bool isCharging;
    float hold;
    ChargeBallet preview;     // 生成済みプレビュー個体参照

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool 未設定");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool 未設定");
    }

    void Update()
    {
        // ---- チャージ入力 ----
        if (Input.GetKeyDown(chargeKey))
        {
            BeginCharge();
        }
        if (isCharging)
        {
            hold += Time.deltaTime;
            UpdateChargePreview();
        }
        if (Input.GetKeyUp(chargeKey))
        {
            EndChargeAndFire();
        }

        // ---- 通常ショット（チャージ中は抑制）----
        if (!isCharging)
        {
            if (Input.GetKeyDown(normalKey))
            {
                FireNormal();
                normalCd = 1f / fireRate;
            }
            else if (Input.GetKey(normalKey))
            {
                normalCd -= Time.deltaTime;
                if (normalCd <= 0f)
                {
                    FireNormal();
                    normalCd = 1f / fireRate;
                }
            }
            else normalCd = 0f;
        }
    }

    void BeginCharge()
    {
        isCharging = true;
        hold = 0f;

        // プレビュー個体を生成して自機前に表示
        var go = chargePool?.Spawn(muzzle.position, Quaternion.identity);
        if (!go) { Debug.LogWarning("[Shooter] Charge preview spawn failed"); return; }
        preview = go.GetComponent<ChargeBallet>();
        if (!preview) { Debug.LogError("[Shooter] Charge prefab has no ChargeBallet"); return; }

        preview.StartPreview(muzzle, previewOffset);
        UpdateChargePreview(); // 押下直後にも1回反映
    }

    void UpdateChargePreview()
    {
        if (!preview) return;

        // ホールド進捗 0..1（上限到達まで）
        float t01 = Mathf.Clamp01(hold / Mathf.Max(0.0001f, chargeMaxTime));
        float curve = (chargeCurve != null && chargeCurve.keys.Length > 0) ? chargeCurve.Evaluate(t01) : t01;

        // 上限（能力強化を適用した“いまの最大値”）
        float sizeCapMul = baseChargeMaxSizeMul * Mathf.Max(1f, chargeMaxSizeMul);
        float damageCapAbs = baseChargeMaxDamage * Mathf.Max(1f, chargeMaxDamageMul);

        // 現在サイズ倍率（1→sizeCapMul）
        float sizeMulNow = Mathf.Lerp(1f, sizeCapMul, curve);

        // 現在の想定ダメージ（「プレハブ既定×通常強化」から上限へ）
        float baseDmgAbs = Mathf.Max(0f, preview.damage) * Mathf.Max(0.01f, normalDamageMul);
        float desireDmgAbs = Mathf.Lerp(baseDmgAbs, damageCapAbs, curve);
        float dmgNow = Mathf.Min(desireDmgAbs, damageCapAbs);

        // 方向も常に更新（自機の向きに追従）
        Vector2 dirNow = muzzle.right;

        preview.UpdatePreview(dmgNow, sizeMulNow);
        // 見た目の向きは ChargeBallet 側で追従済み（Update）
        // Shooter.UpdateChargePreview() の最後に一時的に追加
        Debug.Log($"sizeCapMul={baseChargeMaxSizeMul * Mathf.Max(1f, chargeMaxSizeMul)}, " +
                  $"sizeMulNow={Mathf.Lerp(1f, baseChargeMaxSizeMul * Mathf.Max(1f, chargeMaxSizeMul), curve)}, " +
                  $"goScale={preview.transform.localScale}");

    }

    void EndChargeAndFire()
    {
        if (preview) preview.FireNow();
        preview = null;
        isCharging = false;
        hold = 0f;
    }

    void FireNormal()
    {
        if (!normalPool || !muzzle) return;
        var go = normalPool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        if (!b) return;

        b.damage = Mathf.RoundToInt(b.damage * Mathf.Max(0.01f, normalDamageMul));
        b.dir = muzzle.right;
    }
}
