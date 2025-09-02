using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("�ˌ��ݒ�")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    ObjectPool pool;       // �� �Q�Ƃ����[�J���ێ�
    float cd;

    void Awake()
    {
        // �V���O���g������擾
        pool = PoolManager.Instance?.enemyBulletPool;
        if (!pool) Debug.LogError("[EnemyShooter] enemyBulletPool �����ݒ�ł�");
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
