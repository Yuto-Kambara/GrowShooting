using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("射撃設定")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    ObjectPool pool;       // ← 参照をローカル保持
    float cd;

    void Awake()
    {
        // シングルトンから取得
        pool = PoolManager.Instance?.enemyBulletPool;
        if (!pool) Debug.LogError("[EnemyShooter] enemyBulletPool が未設定です");
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
        b.dir = Vector2.left;
    }
}
