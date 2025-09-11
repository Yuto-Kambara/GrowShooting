using UnityEngine;

public class AbsorbSystem : MonoBehaviour
{
    [Header("吸収パラメータ")]
    public float pullRadius = 1.2f;      // 粒子を出す範囲
    public GameObject energyPrefab;      // EnergyOrb のプレハブ
    public float energySpeed = 8f;       // 粒子の飛行速度

    GrowthSystem growth; int bulletMask;

    void Awake()
    {
        growth = GetComponentInParent<GrowthSystem>();
        bulletMask = 1 << LayerMask.NameToLayer("EnemyBullet");
    }

    //========= pullRadius 内に入った弾から EnergyOrb を出す =========
    void Update()
    {
        // 近くの敵弾を検知（弾は動かさない！）
        var hits = Physics2D.OverlapCircleAll(transform.position, pullRadius, bulletMask);

        foreach (var h in hits)
        {
            var flag = h.GetComponent<BulletEnergyFlag>();
            if (!flag || flag.spawned) continue;    // 既に生成済みならスキップ

            flag.spawned = true;                    // 生成済みマーク

            // EnergyOrb を弾の位置に生成し，Init でプレイヤーへ向ける
            var orb = Instantiate(energyPrefab, h.transform.position, Quaternion.identity);
            orb.GetComponent<EnergyOrb>().Init(transform.root, energySpeed, growth);
        }
    }
}
