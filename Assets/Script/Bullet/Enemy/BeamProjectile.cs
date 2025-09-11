// Assets/Scripts/Enemy/Bullets/BeamProjectile.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 指定方向にまっすぐ伸びるビーム。表示範囲内にいる間、一定ペース(DPS)でHPを削る。
/// ・BoxCollider2D (isTrigger) 必須
/// ・見た目はSpriteRenderer等でOK（幅はコードからスケール）
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class BeamProjectile : MonoBehaviour
{
    [Header("Runtime (set by spawner)")]
    public float lifetime = 0.7f;
    public float dps = 10f;
    public float width = 0.5f;
    public Vector2 dir = Vector2.left;

    [Header("Damage tick")]
    public float tickInterval = 0.1f;  // dps をこの間隔に割って与える

    float t;
    BoxCollider2D col;
    readonly Dictionary<int, float> nextTick = new(); // 対象ごとの次回ダメージ時刻

    void Awake()
    {
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        t = 0f;
        nextTick.Clear();
    }

    public void Init(Vector2 direction, float beamWidth, float dpsValue, float lifeSeconds)
    {
        dir = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.left;
        width = beamWidth;
        dps = dpsValue;
        lifetime = lifeSeconds;

        // 見た目＆当たりのサイズ：X方向に長く、Y方向が幅
        float beamLen = 50f; // 画面外まで届く想定の大きめ値
        transform.right = dir; // X軸を進行方向へ
        transform.localScale = new Vector3(beamLen, width, 1f);
        col.size = new Vector2(1f, 1f); // スケールで伸ばす前提
        col.offset = new Vector2(0.5f, 0f); // 右方向に伸ばす
    }

    void Update()
    {
        t += Time.deltaTime;
        if (t >= lifetime) { gameObject.SetActive(false); }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var hp = other.GetComponentInParent<Health>();
        if (!hp) return;

        int id = other.GetInstanceID();
        float now = Time.time;
        if (!nextTick.TryGetValue(id, out float nt) || now >= nt)
        {
            int dmg = Mathf.RoundToInt(dps * tickInterval);
            if (dmg > 0) hp.Take(dmg);
            nextTick[id] = now + tickInterval;
        }
    }
}
