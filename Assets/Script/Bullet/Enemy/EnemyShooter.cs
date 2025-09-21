// Assets/Scripts/Enemy/EnemyShooter.cs
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("�ˌ��ݒ�")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    [Header("�G�C���ݒ�iCSV �ŏ㏑�������j")]
    public string playerTag = "Player";  // AtPlayer �̃^�[�Q�b�g
    FireDirSpec fireDir = FireDirSpec.Fixed(Vector2.left);

    // ���^�C�~���O�i�����j
    FireTimingSpec timing = FireTimingSpec.Default();

    // �� �ǉ��F�e�̎�ށE�p�����[�^�iCSV�ŏ㏑���j
    EnemyBulletSpec bulletSpec = new EnemyBulletSpec();

    // �v�[���i����Ύg�p�B�������Instantiate�j
    [Header("Pools / Prefabs")]
    public ObjectPool normalPool;
    public ObjectPool beamPool;
    public ObjectPool homingPool;

    public GameObject normalPrefab;  // �v�[�����ݒ莞�̃t�H�[���o�b�N
    public GameObject beamPrefab;
    public GameObject homingPrefab;

    // Default �p
    float cd;

    // Interval / Timeline �p
    float t;
    float nextAt;
    int idx;

    Transform player;

    void Awake()
    {
        // �����v�[���ւ̃t�H�[���o�b�N�i�C�Ӂj
        if (!normalPool) normalPool = PoolManager.Instance?.enemyBulletPool;
        if (!beamPool)  beamPool = PoolManager.Instance?.BeamBulletPool;
        if (!homingPool) homingPool = PoolManager.Instance?.HomingBulletPool;
        // beamPool / homingPool ���K�v�Ȃ� PoolManager �ɒǉ��ŗp�ӂ��A�����ŏE��
    }

    public void ApplyFireDirection(FireDirSpec spec) => fireDir = spec;

    public void ApplyFireTiming(FireTimingSpec spec)
    {
        timing = spec ?? FireTimingSpec.Default();
        cd = 0f; t = 0f; idx = 0;
        nextAt = timing.startDelay;
    }

    public void ApplyBulletSpec(EnemyBulletSpec spec) => bulletSpec = spec ?? new EnemyBulletSpec();

    void OnEnable()
    {
        cd = 0f; t = 0f; idx = 0;
        nextAt = timing.startDelay;
    }

    void Update()
    {
        if (!muzzle) return;

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
        Vector2 dir = ResolveDirection();

        switch (bulletSpec.type)
        {
            case EnemyBulletType.Normal:
                FireNormal(dir);
                break;

            case EnemyBulletType.Beam:
                FireBeam(dir);
                break;

            case EnemyBulletType.Homing:
                FireHoming(dir);
                break;
        }
    }

    Vector2 ResolveDirection()
    {
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
        return dir;
    }

    void FireNormal(Vector2 dir)
    {
        GameObject go = normalPool ? normalPool.Spawn(muzzle.position, Quaternion.identity)
                                   : Instantiate(normalPrefab, muzzle.position, Quaternion.identity);
        if (!go) return;

        go.layer = LayerMask.NameToLayer("EnemyBullet");
        var b = go.GetComponent<Bullet>();
        if (b)
        {
            if (bulletSpec.damage > 0) b.damage = Mathf.RoundToInt(bulletSpec.damage);

            // �T�C�Y�͏㏑���i�ݐς��Ȃ��j
            b.SetSizeMul(bulletSpec.sizeMul);

            // �� �ǉ�: �ʏ�e�X�s�[�h�̏㏑���i���̒l�̂݁j
            if (bulletSpec.normalSpeed > 0f)
            {
                // Bullet ���� API �ɍ��킹�Ăǂ��炩���g�p���Ă�������
                // 1) �t�B�[���h���J�Ȃ�:
                b.speed = bulletSpec.normalSpeed;

                // 2) �Z�b�^�[�֐�������Ȃ�:
                // b.SetSpeed(bulletSpec.normalSpeed);
            }

            b.dir = dir;
        }
    }


    void FireBeam(Vector2 dir)
    {
        GameObject go = beamPool ? beamPool.Spawn(muzzle.position, Quaternion.identity)
                                 : Instantiate(beamPrefab, muzzle.position, Quaternion.identity);
        if (!go) return;

        go.layer = LayerMask.NameToLayer("EnemyBullet");

        var beam = go.GetComponent<BeamProjectile>();
        if (!beam) { Debug.LogError("[EnemyShooter] BeamProjectile �����t�^�̃v���n�u�ł�"); return; }

        beam.Init(dir, bulletSpec.beamWidth, bulletSpec.beamDps, bulletSpec.beamLifetime);
    }

    void FireHoming(Vector2 dir)
    {
        GameObject go = homingPool ? homingPool.Spawn(muzzle.position, Quaternion.identity)
                                   : Instantiate(homingPrefab, muzzle.position, Quaternion.identity);
        if (!go) return;

        go.layer = LayerMask.NameToLayer("EnemyBullet");

        var h = go.GetComponent<HomingProjectile>();
        if (!h) { Debug.LogError("[EnemyShooter] HomingProjectile �����t�^�̃v���n�u�ł�"); return; }

        // ��������
        h.dir = (dir.sqrMagnitude > 1e-6f) ? dir.normalized : Vector2.right;

        // �K�v�Ȃ�Z�Œ�i�w�i����O�ɂ�����Z�B�w�i��0�Ȃ� -0.1f �Ȃǁj
        // h.lockZ = -0.1f;

        h.Init(
            dmg: bulletSpec.damage,
            size: bulletSpec.sizeMul,
            spd: bulletSpec.homingSpeed,
            turn: bulletSpec.homingTurnRate,
            near: bulletSpec.nearDistance,
            brk: bulletSpec.breakDistance,
            maxT: bulletSpec.maxHomingTime,
            lifeSeconds: bulletSpec.life
        );
    }

}
