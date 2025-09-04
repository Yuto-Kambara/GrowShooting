using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �o�H�|�C���g�ɉ����Ĉړ�����ėp���[�o�[�B
/// �ELinear       : �Ȃ���_�Ő܂�Ȃ��铙���ړ�
/// �ESmoothSpline : Catmull-Rom ��ԂŊ��炩��
/// �EArrive       : �Ō�̖ړI�n�ɋ߂Â��قǌ����i�����j
///
/// �o�H�́u�o��ʒu(0), �Ȃ���_(1..n-2), ���n�_(n-1)�v�̏��œn���B
/// </summary>
[RequireComponent(typeof(Transform))]
public class EnemyMover : MonoBehaviour
{
    public enum MotionPattern { Linear, SmoothSpline, Arrive }

    [Header("�ړ��ݒ�")]
    public MotionPattern pattern = MotionPattern.Linear;
    public float speed = 2.0f;         // �ڕW�ړ����x�iunits/sec�j
    public float arriveSlowRadius = 2f;// Arrive: �����J�n����
    public float arriveStopRadius = 0.05f; // Arrive: ���B����

    [Header("�o�H�f�o�b�O")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1f, 0.7f, 0.2f, 0.8f);

    // ����
    List<Vector3> waypoints;   // �o�H�i���[���h�j
    int currentIndex = 1;      // ���܌������Ă��� waypoint�i0 �͓o��ʒu�j
    float splineT = 0f;        // Smooth �p�p�����[�^
    List<Vector3> sampled;     // Smooth �̂��߂̃T���v���_

    public void InitPath(List<Vector3> points, MotionPattern mp, float spd)
    {
        pattern = mp;
        speed = spd;
        waypoints = points;
        currentIndex = Mathf.Min(1, (waypoints?.Count ?? 0) - 1);
        transform.position = waypoints[0];

        if (pattern == MotionPattern.SmoothSpline)
        {
            // Catmull-Rom �𓙊Ԋu�ߎ����邽�߂ɃT���v�����O
            sampled = SampleCatmullRom(waypoints, 24); // 1�Z�O24�_
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
            // ���̓_��
            if (++currentIndex >= waypoints.Count)
            {
                // �ŏI���B�����̏�Ŏ~�߂�i�K�v�Ȃ� Destroy �Ȃǁj
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

        // �T���v������u���`�Ɂv���ǂ�i�T�˓����j
        float step = speed * Time.deltaTime;
        // ���݈ʒu���� step �Ԃ��̓_��T��
        // ���ȈՁFsplineT ���������K�������u�T���v���_�� index ��i�߂�v
        // �����x�̃������C�ɂȂ�ꍇ�� arc-length �ăp�����[�^���ɍ����ւ���
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
        // ���p�_������ꍇ�͏��Ɂg�V�[�N�h���Ō�̓_�ɋ߂Â����猸��
        if (currentIndex < waypoints.Count - 1)
        {
            SeekTowards(waypoints[currentIndex], speed);
            if ((waypoints[currentIndex] - transform.position).sqrMagnitude < 0.05f * 0.05f)
                currentIndex++;
            return;
        }

        // �ŏI�ڕW�ւ� Arrive
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

    // --- Catmull-Rom �̊ȈՃT���v�����O ---
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
        // �Ō�̒��_�������
        s.Add(pts[pts.Count - 1]);
        return s;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // �W�� Catmull-Rom
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
