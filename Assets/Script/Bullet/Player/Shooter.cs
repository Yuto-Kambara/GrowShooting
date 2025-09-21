using UnityEngine;

public class Shooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;

    [Header("Normal Fire")]
    public float fireRate = 8f;
    [SerializeField] private KeyCode normalKey = KeyCode.K;   // ← K トグル

    [Header("Charge Fire (hold L)")]
    [SerializeField] private KeyCode chargeKey = KeyCode.L;
    public float chargeMaxTime = 2.5f;     // ここまでホールドで上限到達
    public AnimationCurve chargeCurve;     // 0→1成長カーブ（未設定なら線形）
    public float previewOffset = 0.2f;     // 自機前の表示距離
    [Min(0.01f)] public float chargeStartSizeMul = 0.3f;  // ★ 追加：初期サイズ倍率（Inspectorで指定）

    [Header("Caps (Base)")]
    public float baseChargeMaxSizeMul = 1.0f;   // サイズ上限（基準スケール×これ）
    public float baseChargeMaxDamage = 1.0f;    // ダメージ上限（絶対値）

    [Header("Growth Hooks (x上限)")]
    [HideInInspector] public float chargeMaxSizeMul = 1f;    // 能力強化でサイズ上限を拡張
    [HideInInspector] public float chargeMaxDamageMul = 1f;  // 能力強化で最大攻撃力を拡張

    [Header("Other Growth Hooks")]
    [HideInInspector] public float normalDamageMul = 1f;     // 通常弾用（既存）

    private ObjectPool normalPool;
    private ObjectPool chargePool;

    // 内部
    float normalCd;
    bool isCharging;
    float hold;
    bool autoFire;                 // K のオン/オフ状態
    ChargeBallet preview;          // 生成済みプレビュー個体参照

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool 未設定");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool 未設定");
    }

    void Update()
    {
        // ---- チャージ入力（L：ホールド）----
        if (Input.GetKeyDown(chargeKey))
        {
            BeginCharge();                 // ★ チャージ開始で通常射撃は停止
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

        // ---- 通常ショット（K：トグル）----
        // チャージ中は常に抑制（発射しない）
        if (!isCharging)
        {
            if (Input.GetKeyDown(normalKey))
            {
                autoFire = !autoFire;
                if (autoFire)
                {
                    // トグルON直後に即1発。その後 fireRate に従って連射
                    FireNormal();
                    normalCd = 1f / fireRate;
                }
                else
                {
                    normalCd = 0f;
                }
            }

            if (autoFire)
            {
                normalCd -= Time.deltaTime;
                if (normalCd <= 0f)
                {
                    FireNormal();
                    normalCd = 1f / fireRate;
                }
            }
        }
        else
        {
            normalCd = 0f;
        }
    }

    void BeginCharge()
    {
        isCharging = true;
        hold = 0f;

        // ★ チャージ開始時に通常射撃を停止
        autoFire = false;
        normalCd = 0f;

        // プレビュー個体を生成して自機前に表示
        var go = chargePool?.Spawn(muzzle.position, Quaternion.identity);
        if (!go) { Debug.LogWarning("[Shooter] Charge preview spawn failed"); return; }
        preview = go.GetComponent<ChargeBallet>();
        if (!preview) { Debug.LogError("[Shooter] Charge prefab has no ChargeBallet"); return; }

        preview.StartPreview(muzzle, previewOffset);
        UpdateChargePreview(); // 押下直後にも1回反映（ここで初期サイズ0.3相当になる）
    }

    void UpdateChargePreview()
    {
        if (!preview) return;

        float t01 = Mathf.Clamp01(hold / Mathf.Max(0.0001f, chargeMaxTime));
        float curve = (chargeCurve != null && chargeCurve.keys.Length > 0) ? chargeCurve.Evaluate(t01) : t01;

        float sizeCapMul = baseChargeMaxSizeMul * Mathf.Max(1f, chargeMaxSizeMul);
        float damageCapAbs = baseChargeMaxDamage * Mathf.Max(1f, chargeMaxDamageMul);

        float startSizeMul = Mathf.Clamp(chargeStartSizeMul, 0.01f, sizeCapMul);
        float sizeMulNow = Mathf.Lerp(startSizeMul, sizeCapMul, curve);

        float baseDmgAbs = Mathf.Max(0f, preview.damage) * Mathf.Max(0.01f, normalDamageMul);
        float desireDmgAbs = Mathf.Lerp(baseDmgAbs, damageCapAbs, curve);
        float dmgNow = Mathf.Min(desireDmgAbs, damageCapAbs);

        // ← ここで与える sizeMulNow が 0.3 など 1 未満でも OK になる
        preview.UpdatePreview(dmgNow, sizeMulNow);
    }

    void EndChargeAndFire()
    {
        if (preview) preview.FireNow();
        preview = null;
        isCharging = false;
        hold = 0f;
        // 通常射撃は自動再開しない（Kで再トグル）
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
