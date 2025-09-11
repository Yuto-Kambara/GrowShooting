using UnityEngine;
using System.Collections.Generic;

public class AbsorbSystem : MonoBehaviour
{
    [Header("吸収パラメータ")]
    public float pullRadius = 1.2f;       // 粒子を出す範囲
    public GameObject energyPrefab;       // EnergyOrb のプレハブ
    public float energySpeed = 8f;        // 粒子の飛行速度

    [Header("生成レート")]
    [Tooltip("範囲内にいる弾1発あたり、この間隔ごとに1個ずつ生成")]
    public float spawnInterval = 0.5f;

    [Tooltip("弾が範囲に入った瞬間に1個出すか（true なら即1個＋以後は interval 間隔）")]
    public bool spawnOnEnter = true;

    [Tooltip("各弾から出す最大個数（0 なら無制限）")]
    public int maxPerBullet = 0;

    private GrowthSystem growth;
    private int bulletMask;

    // 弾コンポーネント（BulletEnergyFlag）の InstanceID をキーに、次回生成時刻＆生成数を管理
    private readonly Dictionary<int, float> _nextSpawnAt = new();
    private readonly Dictionary<int, int> _spawnCount = new();

    void Awake()
    {
        growth = GetComponentInParent<GrowthSystem>();
        bulletMask = 1 << LayerMask.NameToLayer("EnemyBullet");
    }

    void OnDisable()
    {
        _nextSpawnAt.Clear();
        _spawnCount.Clear();
    }

    //========= pullRadius 内の敵弾から、一定間隔で EnergyOrb を出す =========
    void Update()
    {
        // 近くの敵弾を検知（弾は動かさない）
        var hits = Physics2D.OverlapCircleAll(transform.position, pullRadius, bulletMask);

        // 可視弾集合（辞書の掃除用）
        var visibleIds = new HashSet<int>();

        float now = Time.time;
        foreach (var h in hits)
        {
            // 以前は 1回限り用に BulletEnergyFlag.spawned を使っていたが、今回は「存在する限り出し続ける」ので
            // BulletEnergyFlag の有無だけ判定に使う（このコンポーネントが付いている弾のみ対象）
            var flag = h.GetComponent<BulletEnergyFlag>();
            if (!flag) continue;

            int id = flag.GetInstanceID();
            visibleIds.Add(id);

            // 上限個数に達しているなら何もしない
            if (maxPerBullet > 0 && _spawnCount.TryGetValue(id, out int cnt) && cnt >= maxPerBullet)
                continue;

            // まだ登録されていない弾（範囲に入ったばかり）
            if (!_nextSpawnAt.ContainsKey(id))
            {
                if (spawnOnEnter)
                {
                    SpawnEnergy(h.transform.position);
                    _spawnCount[id] = 1;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
                else
                {
                    _spawnCount[id] = 0;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
                continue;
            }

            // 次回時刻に達していれば生成
            if (now >= _nextSpawnAt[id])
            {
                // 上限チェック（直前で到達していないことは確認済みだが二重防御）
                int produced = _spawnCount.TryGetValue(id, out int c) ? c : 0;
                if (maxPerBullet == 0 || produced < maxPerBullet)
                {
                    SpawnEnergy(h.transform.position);
                    _spawnCount[id] = produced + 1;
                    _nextSpawnAt[id] = now + Mathf.Max(0.01f, spawnInterval);
                }
            }
        }

        // 辞書の掃除：範囲外へ出た/破棄された弾のエントリを除去
        if (_nextSpawnAt.Count > 0)
        {
            _tmpKeys.Clear();
            foreach (var key in _nextSpawnAt.Keys)
                if (!visibleIds.Contains(key))
                    _tmpKeys.Add(key);
            foreach (var key in _tmpKeys)
            {
                _nextSpawnAt.Remove(key);
                _spawnCount.Remove(key);
            }
        }
    }

    // 生成ヘルパ
    void SpawnEnergy(Vector3 at)
    {
        if (!energyPrefab || !growth) return;
        var orb = Instantiate(energyPrefab, at, Quaternion.identity);
        var e = orb.GetComponent<EnergyOrb>();
        if (e) e.Init(transform.root, energySpeed, growth);
    }

    // 使い捨てバッファ（GC削減）
    static readonly List<int> _tmpKeys = new();

#if UNITY_EDITOR
    // 半径可視化
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, pullRadius);
    }
#endif
}
