using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 経路ポイントに沿って移動する汎用ムーバー。
/// ・Linear       : 曲がり点で折れ曲がる等速移動
/// ・SmoothSpline : Catmull-Rom 補間で滑らかに
/// ・Arrive       : 最後の目的地に近づくほど減速（到着）
///
/// 経路は「登場位置(0), 曲がり点(1..n-2), 着地点(n-1)」の順で渡す。
/// </summary>
[RequireComponent(typeof(Transform))]
public class EnemyMover : MonoBehaviour
{
    public enum MotionPattern { Linear, SmoothSpline, Arrive }

    [Header("移動設定")]
    public MotionPattern pattern = MotionPattern.Linear;
    public float speed = 2.0f;         // 目標移動速度（units/sec）
    public float arriveSlowRadius = 2f;// Arrive: 減速開始距離
    public float arriveStopRadius = 0.05f; // Arrive: 到達判定

    [Header("経路デバッグ")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1f, 0.7f, 0.2f, 0.8f);

    // 内部
    List<Vector3> waypoints;   // 経路（ワールド）
    int currentIndex = 1;      // いま向かっている waypoint（0 は登場位置）
    float splineT = 0f;        // Smooth 用パラメータ
    List<Vector3> sampled;     // Smooth のためのサンプル点

    public void InitPath(List<Vector3> points, MotionPattern mp, float spd)
    {
        pattern = mp;
        speed = spd;
        waypoints = points;
        currentIndex = Mathf.Min(1, (waypoints?.Count ?? 0) - 1);
        transform.position = waypoints[0];

        if (pattern == MotionPattern.SmoothSpline)
        {
            // Catmull-Rom を等間隔近似するためにサンプリング
            sampled = SampleCatmullRom(waypoints, 24); // 1セグ24点
            splineT = 0f;
        }
    }

    void Update()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        switch (pattern)
        {
            case MotionPattern.Linear:
                StepLinear();
                break;

            case MotionPattern.SmoothSpline:
                StepSpline();
                break;

            case MotionPattern.Arrive:
                StepArrive();
                break;
        }
    }

    void StepLinear()
    {
        Vector3 target = waypoints[currentIndex];
        Vector3 dir = (target - transform.position);
        float dist = dir.magnitude;

        if (dist <= 0.001f)
        {
            // 次の点へ
            if (++currentIndex >= waypoints.Count)
            {
                // 最終到達→その場で止める（必要なら Destroy など）
                enabled = false;
                return;
            }
            return;
        }

        Vector3 v = dir.normalized * speed;
        transform.position += v * Time.deltaTime;
    }

    void StepSpline()
    {
        if (sampled == null || sampled.Count < 2) { StepLinear(); return; }

        // サンプル列を「線形に」たどる（概ね等速）
        float step = speed * Time.deltaTime;
        // 現在位置から step ぶん先の点を探す
        // ※簡易：splineT を距離正規化せず「サンプル点の index を進める」
        // 実速度のムラが気になる場合は arc-length 再パラメータ化に差し替え可
        splineT += step;
        int idx = Mathf.FloorToInt(splineT);
        if (idx >= sampled.Count - 1)
        {
            transform.position = sampled[sampled.Count - 1];
            enabled = false;
            return;
        }

        float ft = splineT - idx;
        transform.position = Vector3.Lerp(sampled[idx], sampled[idx + 1], ft);
    }

    void StepArrive()
    {
        // 中継点がある場合は順に“シーク”→最後の点に近づいたら減速
        if (currentIndex < waypoints.Count - 1)
        {
            SeekTowards(waypoints[currentIndex], speed);
            if ((waypoints[currentIndex] - transform.position).sqrMagnitude < 0.05f * 0.05f)
                currentIndex++;
            return;
        }

        // 最終目標への Arrive
        Vector3 target = waypoints[waypoints.Count - 1];
        Vector3 to = target - transform.position;
        float dist = to.magnitude;

        if (dist <= arriveStopRadius)
        {
            transform.position = target;
            enabled = false;
            return;
        }

        float desiredSpeed = (dist < arriveSlowRadius)
            ? Mathf.Lerp(0f, speed, dist / arriveSlowRadius)
            : speed;

        SeekTowards(target, desiredSpeed);
    }

    void SeekTowards(Vector3 target, float desiredSpeed)
    {
        Vector3 dir = (target - transform.position).normalized;
        Vector3 vel = dir * desiredSpeed;
        transform.position += vel * Time.deltaTime;
    }

    // --- Catmull-Rom の簡易サンプリング ---
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
        // 最後の頂点も入れる
        s.Add(pts[pts.Count - 1]);
        return s;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // 標準 Catmull-Rom
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
            Gizmos.DrawLine(waypoints[i], waypoints[i + 1]);

        foreach (var p in waypoints)
            Gizmos.DrawWireSphere(p, 0.06f);
    }
#endif
}
