// Assets/Scripts/Enemy/EnemyShooter.cs
using UnityEngine;

public class EnemyShooter : MonoBehaviour
{
    [Header("射撃設定")]
    public Transform muzzle;
    public float fireRate = 1.5f;

    [Header("エイム設定（CSV で上書きされる）")]
    public string playerTag = "Player";  // AtPlayer のターゲット
    FireDirSpec fireDir = FireDirSpec.Fixed(Vector2.left);

    // 撃つタイミング（既存）
    FireTimingSpec timing = FireTimingSpec.Default();

    // ★ 追加：弾の種類・パラメータ（CSVで上書き）
    EnemyBulletSpec bulletSpec = new EnemyBulletSpec();

    // プール（あれば使用。無ければInstantiate）
    [Header("Pools / Prefabs")]
    public ObjectPool normalPool;
    public ObjectPool beamPool;
    public ObjectPool homingPool;

    public GameObject normalPrefab;  // プール未設定時のフォールバック
    public GameObject beamPrefab;
    public GameObject homingPrefab;

    // Default 用
    float cd;

    // Interval / Timeline 用
    float t;
    float nextAt;
    int idx;

    Transform player;

    void Awake()
    {
        // 既存プールへのフォールバック（任意）
        if (!normalPool) normalPool = PoolManager.Instance?.enemyBulletPool;
        if (!beamPool)  beamPool = PoolManager.Instance?.BeamBulletPool;
        if (!homingPool) homingPool = PoolManager.Instance?.HomingBulletPool;
        // beamPool / homingPool も必要なら PoolManager に追加で用意し、ここで拾う
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

            // サイズは上書き（累積しない）
            b.SetSizeMul(bulletSpec.sizeMul);

            // ★ 追加: 通常弾スピードの上書き（正の値のみ）
            if (bulletSpec.normalSpeed > 0f)
            {
                // Bullet 側の API に合わせてどちらかを使用してください
                // 1) フィールド公開なら:
                b.speed = bulletSpec.normalSpeed;

                // 2) セッター関数があるなら:
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
        if (!beam) { Debug.LogError("[EnemyShooter] BeamProjectile が未付与のプレハブです"); return; }

        beam.Init(dir, bulletSpec.beamWidth, bulletSpec.beamDps, bulletSpec.beamLifetime);
    }

    void FireHoming(Vector2 dir)
    {
        GameObject go = homingPool ? homingPool.Spawn(muzzle.position, Quaternion.identity)
                                   : Instantiate(homingPrefab, muzzle.position, Quaternion.identity);
        if (!go) return;

        go.layer = LayerMask.NameToLayer("EnemyBullet");

        var h = go.GetComponent<HomingProjectile>();
        if (!h) { Debug.LogError("[EnemyShooter] HomingProjectile が未付与のプレハブです"); return; }

        // 初期向き
        h.dir = (dir.sqrMagnitude > 1e-6f) ? dir.normalized : Vector2.right;

        // 必要ならZ固定（背景より手前にしたいZ。背景が0なら -0.1f など）
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
