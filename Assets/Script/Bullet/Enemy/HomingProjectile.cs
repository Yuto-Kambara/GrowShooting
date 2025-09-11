// Assets/Scripts/Enemy/Bullets/HomingProjectile.cs
using UnityEngine;

/// <summary>
/// Bullet と同等の設計で動く追尾弾:
/// - Update内でdirを回頭→Translateで移動
/// - life経過で自動消滅
/// - Trigger衝突で相手のHealthにダメージ→消滅
/// - Z固定(lockZ)で背景の後ろへ潜らない
/// 追尾は「一定距離まで近づく or 時間切れ」で解除し、その後は直進。
/// </summary>
public class HomingProjectile : MonoBehaviour
{
    [Header("Movement/Damage (Bullet準拠)")]
    public float speed = 6f;
    public float damage = 1f;
    public Vector2 dir = Vector2.right; // 発射時の向き（EnemyShooter から設定）
    public float life = 6f;

    [Header("Homing params")]
    public float turnRate = 540f;     // 旋回速度(deg/sec)
    public float nearDistance = 2f;   // ここまで近づいたら「いつでも解除可」状態に
    public float breakDistance = 5f;  // 一度近づいた後、ここより離れたら追尾解除
    public float maxHomingTime = 2.5f;// 追尾継続の最大時間
    public string playerTag = "Player";

    [Header("Render/Sorting helpers")]
    public float lockZ = 0f;          // Zを固定（背景の後ろに潜るのを防止）
    public float sizeMul = 1f;        // 見た目スケール倍率（CSVから上書き）

    [Header("Sprite Facing")]         // NEW: 見た目の向き調整
    [Tooltip("スプライト基準が右向きでない場合の補正角（度）。例：上向き基準なら +90。")]
    public float spriteAngleOffsetDeg = 0f; // NEW

    // 内部
    Transform target;
    float tLife;
    float tHoming;
    bool wasNear;
    bool homingActive = true;

    void OnEnable()
    {
        // リセット
        tLife = 0f; tHoming = 0f; wasNear = false; homingActive = true;

        // 対象取得
        var p = GameObject.FindGameObjectWithTag(playerTag);
        target = p ? p.transform : null;

        // スケール反映
        if (sizeMul > 0f) transform.localScale = Vector3.one * sizeMul;

        // Z固定（描画順の安定化）
        var pos = transform.position; pos.z = lockZ; transform.position = pos;

        AlignSpriteToDir(); // NEW: 起動時にも向き合わせ
    }

    /// <summary>EnemyShooter からの初期化</summary>
    public void Init(float dmg, float size, float spd, float turn, float near, float brk, float maxT, float lifeSeconds)
    {
        damage = Mathf.Max(0f, dmg);
        sizeMul = Mathf.Max(0.01f, size);
        speed = Mathf.Max(0.01f, spd);
        turnRate = Mathf.Max(1f, turn);
        nearDistance = Mathf.Max(0f, near);
        breakDistance = Mathf.Max(nearDistance + 0.01f, brk);
        maxHomingTime = Mathf.Max(0.1f, maxT);
        life = Mathf.Max(maxHomingTime, lifeSeconds); // 追尾終了後も少しは飛び続けられるよう、最低でもmaxHomingTime
        transform.localScale = Vector3.one * sizeMul;

        AlignSpriteToDir(); // NEW: 初期化直後も向き合わせ
    }

    void Update()
    {
        // --- 追尾ロジック ---
        if (homingActive && target != null)
        {
            tHoming += Time.deltaTime;

            // 一度十分近づいたかを判定
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= nearDistance) wasNear = true;

            // 近づいた“後”に離れる、または一定時間経過で追尾終了
            if (wasNear && (dist >= breakDistance || tHoming >= maxHomingTime))
                homingActive = false;

            if (homingActive)
            {
                // 現方位 dir → 目標方位への角度差を turnRate 内で詰める
                float curDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Vector2 to = (target.position - transform.position);
                if (to.sqrMagnitude > 1e-6f)
                {
                    float tgtDeg = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    float maxStep = turnRate * Time.deltaTime;
                    float newDeg = Mathf.MoveTowardsAngle(curDeg, tgtDeg, maxStep);
                    float rad = newDeg * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                }
            }
        }

        // NEW: 進行方向にスプライトを向ける
        AlignSpriteToDir();

        // --- 移動（Bullet と同じく Translate World） ---
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        // Z を固定
        var pos = transform.position; pos.z = lockZ; transform.position = pos;

        // --- 寿命 ---
        tLife += Time.deltaTime;
        if (tLife > life) gameObject.SetActive(false);
    }

    // NEW: dir の向きにスプライトを回す（右向き基準）
    void AlignSpriteToDir()
    {
        if (dir.sqrMagnitude < 1e-8f) return;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngleOffsetDeg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Bullet と同じ当たり方（Health API 名はあなたの実装に合わせて）
        if (gameObject.layer == LayerMask.NameToLayer("EnemyBullet") && other.CompareTag("Player"))
        {
            // 例: other.GetComponent<Health>()?.Take(damage);
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
        else if (gameObject.layer == LayerMask.NameToLayer("PlayerBullet") && other.CompareTag("Enemy"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
    }
}
