using UnityEngine;

/// <summary>
/// K：通常ショット（押している間、fireRate に従って連射）
/// L：チャージショット（押し始め→ため、離したときに chargeTime 以上なら発射）
/// ※L でため始めたら通常ショットは即停止＆抑制
/// </summary>
public class Shooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;

    [Header("Fire Params")]
    public float fireRate = 8f;       // 発/秒（Kを押している間の連射レート）
    public float chargeTime = 1.2f;   // これ以上ためたらチャージ弾

    [Header("Keys")]
    [SerializeField] private KeyCode normalKey = KeyCode.K;  // 通常ショット
    [SerializeField] private KeyCode chargeKey = KeyCode.L;  // チャージショット

    private ObjectPool chargePool;
    private ObjectPool normalPool;

    [HideInInspector] public float normalDamageMul = 1f;
    [HideInInspector] public float chargePowerMul = 1f;

    // --- 内部状態 ---
    float normalCd;          // 通常ショット用クールダウン
    float chargeHold;        // チャージ保持時間
    bool  isCharging;        // いまチャージ中か

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool が未設定です");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool が未設定です");
    }

    void Update()
    {
        // ==============================
        // 1) チャージ入力（L）
        // ==============================
        if (Input.GetKeyDown(chargeKey))
        {
            // チャージ開始：通常ショットを即停止
            isCharging = true;
            chargeHold = 0f;
            StopNormalFire();
        }

        if (isCharging)
        {
            chargeHold += Time.deltaTime;
            // チャージ中は通常ショットは抑制される（下の通常処理で !isCharging を見る）
        }

        if (Input.GetKeyUp(chargeKey))
        {
            if (chargeHold >= chargeTime)
            {
                FireCharge();
            }
            // リセット
            isCharging = false;
            chargeHold = 0f;
        }

        // ==============================
        // 2) 通常ショット入力（K）
        //    ※チャージ中は発射しない
        // ==============================
        if (!isCharging)
        {
            // 押した瞬間に即発射させ、以後は fireRate に従って連射
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
            else
            {
                // キーを離している間はCDをリセットしておくと次回押下で即発射できる
                normalCd = 0f;
            }
        }
    }

    // --- Helpers -----------------------------------------------------

    void StopNormalFire()
    {
        // 通常ショット用のクールダウンをリセット（直後に弾が出ないように）
        normalCd = 0f;
    }

    void FireNormal()
    {
        if (!normalPool || !muzzle) return;

        var go = normalPool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul);
    }

    void FireCharge()
    {
        if (!chargePool || !muzzle) return;

        var go = chargePool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul * chargePowerMul);

        // 注意：プーリング運用ではスケールが累積しないように
        // Bullet 側で初期スケールを復元する仕組みがあると安全です。
        go.transform.localScale *= chargePowerMul;   // サイズも拡大
    }
}
