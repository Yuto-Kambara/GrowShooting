using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("�ˌ��ݒ�")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    [Header("�G�C���ݒ�iCSV �ŏ㏑�������j")]
    public string playerTag = "Player";  // AtPlayer �̃^�[�Q�b�g
    FireDirSpec fireDir = FireDirSpec.Fixed(Vector2.left);

    // �� �ǉ��F���^�C�~���O�w��
    FireTimingSpec timing = FireTimingSpec.Default();

    ObjectPool pool;    // �V���O���g������擾

    // Default �p
    float cd;

    // Interval / Timeline �p
    float t;                // �o������̌o�ߎ���
    float nextAt;           // Interval: ���Ɍ�����
    int idx;              // Timeline: ���Ɍ��C���f�b�N�X

    Transform player;   // �L���b�V��

    void Awake()
    {
        pool = PoolManager.Instance?.enemyBulletPool;
        if (!pool) Debug.LogError("[EnemyShooter] enemyBulletPool �����ݒ�ł�");
    }

    public void ApplyFireDirection(FireDirSpec spec) => fireDir = spec;

    public void ApplyFireTiming(FireTimingSpec spec)
    {
        timing = spec ?? FireTimingSpec.Default();
        // ��ԃ��Z�b�g
        cd = 0f; t = 0f; idx = 0;
        nextAt = timing.startDelay;
    }

    void OnEnable()
    {
        // �v�[���߂�̍Ċ������ɂ��Ή�
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

        Vector2 dir = Vector2.left; // ����
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
