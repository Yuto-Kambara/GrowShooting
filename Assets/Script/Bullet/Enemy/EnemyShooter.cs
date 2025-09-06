using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("射撃設定")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    [Header("エイム設定（CSV で上書きされる）")]
    public string playerTag = "Player";  // AtPlayer のターゲット
    FireDirSpec fireDir = FireDirSpec.Fixed(Vector2.left);

    // ★ 追加：撃つタイミング指定
    FireTimingSpec timing = FireTimingSpec.Default();

    ObjectPool pool;    // シングルトンから取得

    // Default 用
    float cd;

    // Interval / Timeline 用
    float t;                // 出現からの経過時間
    float nextAt;           // Interval: 次に撃つ時刻
    int idx;              // Timeline: 次に撃つインデックス

    Transform player;   // キャッシュ

    void Awake()
    {
        pool = PoolManager.Instance?.enemyBulletPool;
        if (!pool) Debug.LogError("[EnemyShooter] enemyBulletPool が未設定です");
    }

    public void ApplyFireDirection(FireDirSpec spec) => fireDir = spec;

    public void ApplyFireTiming(FireTimingSpec spec)
    {
        timing = spec ?? FireTimingSpec.Default();
        // 状態リセット
        cd = 0f; t = 0f; idx = 0;
        nextAt = timing.startDelay;
    }

    void OnEnable()
    {
        // プール戻りの再活性化にも対応
        cd = 0f; t = 0f; idx = 0;
        nextAt = timing.startDelay;
    }

    void Update()
    {
        if (!pool || !muzzle) return;

        switch (timing.mode)
        {
            case FireTimingMode.Default:
                cd -= Time.deltaTime;
                if (cd <= 0f) { Fire(); cd = 1f / Mathf.Max(0.01f, fireRate); }
                break;

            case FireTimingMode.Interval:
                t += Time.deltaTime;
                while (t >= nextAt)
                {
                    Fire();
                    nextAt += Mathf.Max(0.01f, timing.interval);
                }
                break;

            case FireTimingMode.Timeline:
                t += Time.deltaTime;
                while (idx < timing.times.Count && t >= timing.times[idx])
                {
                    Fire();
                    idx++;
                }
                break;
        }
    }

    void Fire()
    {
        var go = pool.Spawn(muzzle.position, Quaternion.identity);
        if (!go) return;

        var b = go.GetComponent<Bullet>();
        go.layer = LayerMask.NameToLayer("EnemyBullet");

        Vector2 dir = Vector2.left; // 既定
        switch (fireDir.mode)
        {
            case FireDirMode.Fixed:
                dir = fireDir.fixedDir;
                break;

            case FireDirMode.AtPlayer:
                if (!player)
                {
                    var pgo = GameObject.FindGameObjectWithTag(playerTag);
                    if (pgo) player = pgo.transform;
                }
                if (player)
                {
                    Vector2 to = (player.position - muzzle.position);
                    if (to.sqrMagnitude > 1e-6f) dir = to.normalized;
                }
                break;
        }

        b.dir = dir;
    }
}
