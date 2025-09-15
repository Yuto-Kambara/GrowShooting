// Assets/Scripts/Boss/MidBossMover.cs
using UnityEngine;

/// <summary>
/// 中ボス用の基本ムーバー雛形。
/// - 生成位置から指定の“停泊点(Anchor)”まで入場 → 以後はゆるく揺れるだけ
/// - 本格挙動は後で実装（ステート/パターンを追加する想定）
/// </summary>
[DisallowMultipleComponent]
public class MidBossMover : MonoBehaviour
{
    [Header("Entry (入場)")]
    [Tooltip("ワールド座標。ここに移動して停泊する")]
    public Vector2 anchorWorld = new Vector2(6f, 0.5f);
    public float entrySpeed = 6f;
    public float arriveDistance = 0.05f;

    [Header("Idle (待機ゆらぎ)")]
    public Vector2 idleOscAmplitude = new Vector2(0.4f, 0.4f);
    public Vector2 idleOscFrequency = new Vector2(0.5f, 0.35f);

    Vector2 _anchor;
    Vector2 _phase;
    bool _arrived;

    void OnEnable()
    {
        _anchor = anchorWorld;
        _phase = new Vector2(Random.value * Mathf.PI * 2f, Random.value * Mathf.PI * 2f);
        _arrived = false;
    }

    void Update()
    {
        if (!_arrived) // 入場
        {
            Vector2 pos = transform.position;
            Vector2 to = _anchor - pos;
            float dist = to.magnitude;
            if (dist <= arriveDistance)
            {
                _arrived = true;
                transform.position = _anchor;
            }
            else
            {
                Vector2 step = to.normalized * entrySpeed * Time.deltaTime;
                transform.position = pos + step;
            }
            return;
        }

        // Idle：停泊点まわりで軽く揺らす（見た目用）
        _phase += idleOscFrequency * (Mathf.PI * 2f) * Time.deltaTime;
        Vector2 offset = new Vector2(
            Mathf.Sin(_phase.x) * idleOscAmplitude.x,
            Mathf.Sin(_phase.y) * idleOscAmplitude.y
        );
        transform.position = _anchor + offset;
    }

    // --- 外部から動的に停泊点を変更したい場合用 ---
    public void SetAnchor(Vector2 worldPos)
    {
        anchorWorld = worldPos;
        _anchor = worldPos;
    }
}
