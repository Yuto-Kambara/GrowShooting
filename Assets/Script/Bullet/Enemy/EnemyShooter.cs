using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("射撃設定")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    [Header("エイム設定（CSV で上書きされる）")]
    public string playerTag = "Player";  // AtPlayer のターゲット
    FireDirSpec fireDir = FireDirSpec.Fixed(Vector2.left);

    ObjectPool pool;    // シングルトンから取得
    float cd;
    Transform player;   // キャッシュ

    void Awake()
    {
        pool = PoolManager.Instance?.enemyBulletPool;
        if (!pool) Debug.LogError("[EnemyShooter] enemyBulletPool が未設定です");
    }

    public void ApplyFireDirection(FireDirSpec spec)
    {
        fireDir = spec;
    }

    void Update()
    {
        if (!pool) return;
        cd -= Time.deltaTime;
        if (cd <= 0f) { Fire(); cd = 1f / fireRate; }
    }

    void Fire()
    {
        var go = pool.Spawn(muzzle.position, Quaternion.identity);
        if (!go) return;

        var b = go.GetComponent<Bullet>();
        go.layer = LayerMask.NameToLayer("EnemyBullet");

        Vector2 dir = Vector2.left; // デフォ
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
