using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーのチャージ弾（プレビュー→発射の2段階）
/// ・L押下: 自機前に表示（当たり判定OFF/追従/成長）
/// ・L離し: その個体が発射体に切替（当たり判定ON/移動開始）
/// ・敵ヒット: 与ダメ>=敵HP → 貫通（弾は残る）/ それ未満 → 消滅
/// ・1発で複数キル時、ボーナス加点
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChargeBallet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;
    public Vector2 dir = Vector2.right;
    public float life = 3f;

    [Header("Damage / Size")]
    public float damage = 1f;                  // ベースダメージ（プレハブ既定）
    public bool penetrateOnEqual = true;       // 与ダメ==敵HPも貫通扱い

    [Header("Bonus")]
    public int bonusThreshold = 2;
    public int bonusPerKill = 50;

    // ---- 内部状態 ----
    enum Mode { Preview, Fired }
    Mode mode = Mode.Preview;

    float t;                       // Fired中の寿命タイマ
    int killCount;               // この弾で倒した数
    Vector3 baseScale;             // 累積防止用の基準スケール
    readonly HashSet<Health> hitOnce = new();

    Transform anchor;              // プレビュー追従先（muzzle）
    float anchorOffset = 0.2f;     // 自機前に出す距離
    Collider2D col;

    void Awake()
    {
        baseScale = transform.localScale;
        col = GetComponent<Collider2D>();
    }

    void OnEnable()
    {
        // プール復帰時の初期化
        mode = Mode.Preview;
        t = 0f;
        killCount = 0;
        hitOnce.Clear();
        if (col) col.enabled = false; // プレビュー中は当たり判定OFF
    }

    void Update()
    {
        if (mode == Mode.Preview)
        {
            if (anchor)
            {
                // 自機前に追従表示（向きも常に更新）
                dir = anchor.right.normalized;
                transform.position = anchor.position + (Vector3)(dir * anchorOffset);
                transform.right = dir;
            }
            // ※寿命カウントしない
        }
        else // Fired
        {
            transform.Translate(dir * speed * Time.deltaTime, Space.World);
            t += Time.deltaTime;
            if (t > life) gameObject.SetActive(false);
        }
    }

    /// <summary>プレビュー開始（Shooterから呼ぶ）</summary>
    public void StartPreview(Transform followAnchor, float offset = 0.2f)
    {
        mode = Mode.Preview;
        anchor = followAnchor;
        anchorOffset = offset;
        if (col) col.enabled = false;
        // スケール・ダメージは UpdatePreview で随時更新
    }

    /// <summary>プレビュー更新（サイズと想定ダメージを都度反映）</summary>
    public void UpdatePreview(float previewDamage, float scaleMul)
    {
        damage = Mathf.Max(0f, previewDamage);
        transform.localScale = baseScale * Mathf.Max(1f, scaleMul);
    }

    /// <summary>発射へ切替（Shooterから呼ぶ）</summary>
    public void FireNow()
    {
        mode = Mode.Fired;
        t = 0f;
        anchor = null;
        if (col) col.enabled = true;   // 当たり判定ON
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (mode != Mode.Fired) return; // プレビュー中は当たらない
        if (gameObject.layer != LayerMask.NameToLayer("PlayerBullet")) return;
        if (!other.CompareTag("Enemy")) return;

        var hp = other.GetComponent<Health>();
        if (!hp || hitOnce.Contains(hp)) return;
        hitOnce.Add(hp);

        float before = hp.hp;
        hp.Take(damage);

        bool penetrate = penetrateOnEqual ? damage >= before : damage > before;
        if (penetrate)
        {
            killCount++;    // 弾は残す（貫通継続）
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        if (killCount >= bonusThreshold && ScoreManager.Instance)
        {
            int extra = killCount - (bonusThreshold - 1);
            ScoreManager.Instance.AddScore(extra * bonusPerKill);
        }
    }
}
