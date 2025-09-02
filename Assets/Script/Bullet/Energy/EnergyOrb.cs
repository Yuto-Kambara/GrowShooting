using UnityEngine;

public class EnergyOrb : MonoBehaviour
{
    Transform target;         // プレイヤー
    float speed;              // 移動速度
    GrowthSystem growth;      // 成長処理コール用

    // 呼び出し側が Spawn 後に即 Init する
    public void Init(Transform tgt, float spd, GrowthSystem g)
    {
        target = tgt; speed = spd; growth = g;
    }

    void Update()
    {
        if (!target) { Destroy(gameObject); return; }

        // プレイヤーへ直進
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // 到達判定
        if (Vector3.SqrMagnitude(target.position - transform.position) < 0.04f)
        {
            growth?.OnEnergyAbsorbed(1);   // ← 1 発ぶん成長
            Destroy(gameObject);
        }
    }
}
