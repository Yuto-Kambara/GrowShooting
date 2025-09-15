// Assets/Scripts/Boss/FinalBossMover.cs
using UnityEngine;

/// <summary>
/// 大ボス用の基本ムーバー雛形。
/// - 入場 → フェーズ0待機（軽い揺れ）
/// - 今後、フェーズ制御やパターン切替を追加する前提の骨組み
/// </summary>
[DisallowMultipleComponent]
public class FinalBossMover : MonoBehaviour
{
    public enum Phase { Entry, Phase0Idle /*, Phase1, Phase2, ...*/ }

    [Header("Entry (入場)")]
    public Vector2 anchorWorld = new Vector2(5.5f, 0f);
    public float entrySpeed = 5f;
    public float arriveDistance = 0.06f;

    [Header("Phase0 Idle")]
    public Vector2 idleOscAmplitude = new Vector2(0.5f, 0.35f);
    public Vector2 idleOscFrequency = new Vector2(0.45f, 0.32f);

    Phase _phase = Phase.Entry;
    Vector2 _anchor;
    Vector2 _phaseOsc;

    void OnEnable()
    {
        _anchor = anchorWorld;
        _phase = Phase.Entry;
        _phaseOsc = new Vector2(Random.value * Mathf.PI * 2f, Random.value * Mathf.PI * 2f);
    }

    void Update()
    {
        switch (_phase)
        {
            case Phase.Entry:
                {
                    Vector2 pos = transform.position;
                    Vector2 to = _anchor - pos;
                    float dist = to.magnitude;
                    if (dist <= arriveDistance)
                    {
                        transform.position = _anchor;
                        _phase = Phase.Phase0Idle;
                    }
                    else
                    {
                        transform.position = pos + to.normalized * entrySpeed * Time.deltaTime;
                    }
                    break;
                }

            case Phase.Phase0Idle:
            default:
                {
                    _phaseOsc += idleOscFrequency * (Mathf.PI * 2f) * Time.deltaTime;
                    Vector2 offset = new Vector2(
                        Mathf.Sin(_phaseOsc.x) * idleOscAmplitude.x,
                        Mathf.Sin(_phaseOsc.y) * idleOscAmplitude.y
                    );
                    transform.position = _anchor + offset;
                    break;
                }
        }
    }

    // 次フェーズへ移行（後で攻撃AIから呼ぶ想定）
    public void GoNextPhase(/*引数で状態を渡してもOK*/)
    {
        // _phase = Phase.Phase1; など追記予定
    }

    public void SetAnchor(Vector2 worldPos)
    {
        anchorWorld = worldPos;
        _anchor = worldPos;
    }
}
