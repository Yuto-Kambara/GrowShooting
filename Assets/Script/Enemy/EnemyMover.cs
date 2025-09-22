// Assets/Scripts/Enemy/EnemyMover.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 経路ポイント（登場→曲がり→着地）に沿って移動。
/// 各ポイントに "待機秒数" を指定できる。
/// </summary>
[RequireComponent(typeof(Transform))]
public class EnemyMover : MonoBehaviour
{
    public enum MotionPattern { Linear, SmoothSpline, Arrive }

    // ▼ ウェイポイント：位置 + 待機秒
    public struct Waypoint
    {
        public Vector3 pos;
        public float wait;   // 到達後にこの秒数だけ停止（0で無視）
        public Waypoint(Vector3 p, float w) { pos = p; wait = Mathf.Max(0f, w); }
    }

    [Header("移動設定")]
    public MotionPattern pattern = MotionPattern.Linear;
    public float speed = 2.0f;              // units/sec
    public float arriveSlowRadius = 2f;     // Arrive: 減速開始半径
    public float arriveStopRadius = 0.05f;  // Arrive: 到達判定
    public float stopThreshold = 0.05f;     // SmoothSpline: 停止判定用の近接距離

    [Header("経路デバッグ")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1f, 0.7f, 0.2f, 0.8f);

    // 内部
    List<Waypoint> waypoints;  // 0:登場位置, 1..n-1:目的地
    int targetIdx = 1;         // 次に向かうウェイポイント
    bool waiting = false;
    float waitTimer = 0f;

    // SmoothSpline 用
    List<Vector3> sampled;         // 補間用サンプル列
    float splineCursor = 0f;       // サンプル列上の“距離もどき”カーソル
    int nextStopIndex = 1;         // オリジナルWPのうち、次に停止判定するインデックス
    HashSet<int> stoppedAt = new(); // 停止済みWPの管理（Splineで再判定しない用）

    public void InitPath(List<Waypoint> wps, MotionPattern mp, float spd)
    {
        if (wps == null || wps.Count < 2) { enabled = false; return; }

        pattern = mp;
        speed = spd;
        waypoints = wps;

        // 初期位置 & 初期待機（登場地点の @wait を尊重）
        transform.position = waypoints[0].pos;
        targetIdx = 1;
        waiting = waypoints[0].wait > 0f;
        waitTimer = waypoints[0].wait;

        if (pattern == MotionPattern.SmoothSpline)
        {
            sampled = SampleCatmullRom(ExtractPositions(waypoints), 24);
            splineCursor = 0f;
            nextStopIndex = 1;
            stoppedAt.Clear();
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        // 待機フェーズ
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) waiting = false;
            else return;
        }

        switch (pattern)
        {
            case MotionPattern.Linear: StepLinear(); break;
            case MotionPattern.Arrive: StepArrive(); break;
            case MotionPattern.SmoothSpline: StepSpline(); break;
        }
    }

    // ===== 直線で折れ曲がる（各点で停止可） =====
    void StepLinear()
    {
        if (targetIdx >= waypoints.Count) { enabled = false; return; }

        Vector3 target = waypoints[targetIdx].pos;
        Vector3 to = target - transform.position;
        float dist = to.magnitude;

        // ★ 到達・スナップ判定：1フレームの移動量で届くならスナップして到達処理
        float step = speed * Time.deltaTime;
        if (dist <= step)
        {
            transform.position = target;
            float w = waypoints[targetIdx].wait;
            if (w > 0f) { waiting = true; waitTimer = w; }
            targetIdx++;
            if (targetIdx >= waypoints.Count) { enabled = false; }
            return;
        }

        // 通常の等速移動
        transform.position += (to / dist) * step;
    }

    // ===== 中継点はシーク、最後は減速到着（各点で停止可） =====
    void StepArrive()
    {
        // 中継点に対しては等速シーク + 停止
        if (targetIdx < waypoints.Count - 1)
        {
            Vector3 target = waypoints[targetIdx].pos;
            Vector3 to = target - transform.position;
            float dist = to.magnitude;

            // ★ Linear 同様にスナップ
            float step = speed * Time.deltaTime;
            if (dist <= step)
            {
                transform.position = target;
                float w = waypoints[targetIdx].wait;
                if (w > 0f) { waiting = true; waitTimer = w; }
                targetIdx++;
                return;
            }

            transform.position += (to / dist) * step;
            return;
        }

        // 最終点は Arrive（減速）
        Vector3 final = waypoints[waypoints.Count - 1].pos;
        Vector3 d = final - transform.position;
        float distF = d.magnitude;

        if (distF <= arriveStopRadius)
        {
            transform.position = final;
            float w = waypoints[waypoints.Count - 1].wait;
            if (w > 0f) { waiting = true; waitTimer = w; }
            else { enabled = false; }
            return;
        }

        float desiredSpeed = (distF < arriveSlowRadius)
            ? Mathf.Lerp(0f, speed, distF / arriveSlowRadius)
            : speed;

        transform.position += d.normalized * desiredSpeed * Time.deltaTime;
    }

    // ===== Catmull-Rom 補間（元WPの近傍で停止） =====
    void StepSpline()
    {
        if (sampled == null || sampled.Count < 2) { StepLinear(); return; }

        // 次に停止すべき“元のウェイポイント”に近づいたら停止
        if (nextStopIndex < waypoints.Count)
        {
            Vector3 stopPos = waypoints[nextStopIndex].pos;
            if (Vector3.Distance(transform.position, stopPos) <= stopThreshold && !stoppedAt.Contains(nextStopIndex))
            {
                // スナップして停止
                transform.position = stopPos;
                float w = waypoints[nextStopIndex].wait;
                if (w > 0f) { waiting = true; waitTimer = w; }
                stoppedAt.Add(nextStopIndex);
                nextStopIndex++;
                return;
            }
        }

        // 等間隔サンプルを「だいたい等速」で進める
        float step = speed * Time.deltaTime;
        splineCursor += step;

        int idx = Mathf.FloorToInt(splineCursor);
        if (idx >= sampled.Count - 1)
        {
            transform.position = sampled[sampled.Count - 1];
            // 最終WPでの待機を尊重
            float w = waypoints[waypoints.Count - 1].wait;
            if (w > 0f) { waiting = true; waitTimer = w; }
            else { enabled = false; }
            return;
        }
        float ft = splineCursor - idx;
        transform.position = Vector3.Lerp(sampled[idx], sampled[idx + 1], ft);
    }

    // ---- ユーティリティ ----
    static List<Vector3> ExtractPositions(List<Waypoint> wps)
    {
        var list = new List<Vector3>(wps.Count);
        foreach (var w in wps) list.Add(w.pos);
        return list;
    }

    List<Vector3> SampleCatmullRom(List<Vector3> pts, int samplesPerSeg)
    {
        var s = new List<Vector3>();
        if (pts.Count < 2) return s;

        for (int i = 0; i < pts.Count - 1; i++)
        {
            Vector3 p0 = i == 0 ? pts[i] : pts[i - 1];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[i + 1];
            Vector3 p3 = (i + 2 < pts.Count) ? pts[i + 2] : pts[i + 1];

            for (int k = 0; k < samplesPerSeg; k++)
            {
                float t = k / (float)samplesPerSeg;
                s.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        s.Add(pts[pts.Count - 1]);
        return s;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t; float t3 = t2 * t;
        return 0.5f * ((2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || waypoints == null || waypoints.Count == 0) return;
        Gizmos.color = gizmoColor;
        for (int i = 0; i < waypoints.Count - 1; i++)
            Gizmos.DrawLine(waypoints[i].pos, waypoints[i + 1].pos);

        foreach (var w in waypoints)
            Gizmos.DrawWireSphere(w.pos, 0.06f);
    }
#endif
}
