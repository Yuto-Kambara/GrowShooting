// Assets/Scripts/Enemy/EnemyMover.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �o�H�|�C���g�i�o�ꁨ�Ȃ��聨���n�j�ɉ����Ĉړ��B
/// �e�|�C���g�� "�ҋ@�b��" ���w��ł���B
/// </summary>
[RequireComponent(typeof(Transform))]
public class EnemyMover : MonoBehaviour
{
    public enum MotionPattern { Linear, SmoothSpline, Arrive }

    // �� �E�F�C�|�C���g�F�ʒu + �ҋ@�b
    public struct Waypoint
    {
        public Vector3 pos;
        public float wait;   // ���B��ɂ��̕b��������~�i0�Ŗ����j
        public Waypoint(Vector3 p, float w) { pos = p; wait = Mathf.Max(0f, w); }
    }

    [Header("�ړ��ݒ�")]
    public MotionPattern pattern = MotionPattern.Linear;
    public float speed = 2.0f;              // units/sec
    public float arriveSlowRadius = 2f;     // Arrive: �����J�n���a
    public float arriveStopRadius = 0.05f;  // Arrive: ���B����
    public float stopThreshold = 0.05f;     // SmoothSpline: ��~����p�̋ߐڋ���

    [Header("�o�H�f�o�b�O")]
    public bool drawGizmos = true;
    public Color gizmoColor = new(1f, 0.7f, 0.2f, 0.8f);

    // ����
    List<Waypoint> waypoints;  // 0:�o��ʒu, 1..n-1:�ړI�n
    int targetIdx = 1;         // ���Ɍ������E�F�C�|�C���g
    bool waiting = false;
    float waitTimer = 0f;

    // SmoothSpline �p
    List<Vector3> sampled;         // ��ԗp�T���v����
    float splineCursor = 0f;       // �T���v�����́g�������ǂ��h�J�[�\��
    int nextStopIndex = 1;         // �I���W�i��WP�̂����A���ɒ�~���肷��C���f�b�N�X
    HashSet<int> stoppedAt = new(); // ��~�ς�WP�̊Ǘ��iSpline�ōĔ��肵�Ȃ��p�j

    public void InitPath(List<Waypoint> wps, MotionPattern mp, float spd)
    {
        if (wps == null || wps.Count < 2) { enabled = false; return; }

        pattern = mp;
        speed = spd;
        waypoints = wps;

        // �����ʒu & �����ҋ@�i�o��n�_�� @wait �𑸏d�j
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

        // �ҋ@�t�F�[�Y
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

    // ===== �����Ő܂�Ȃ���i�e�_�Œ�~�j =====
    void StepLinear()
    {
        if (targetIdx >= waypoints.Count) { enabled = false; return; }

        Vector3 target = waypoints[targetIdx].pos;
        Vector3 to = target - transform.position;
        float dist = to.magnitude;

        // �� ���B�E�X�i�b�v����F1�t���[���̈ړ��ʂœ͂��Ȃ�X�i�b�v���ē��B����
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

        // �ʏ�̓����ړ�
        transform.position += (to / dist) * step;
    }

    // ===== ���p�_�̓V�[�N�A�Ō�͌��������i�e�_�Œ�~�j =====
    void StepArrive()
    {
        // ���p�_�ɑ΂��Ă͓����V�[�N + ��~
        if (targetIdx < waypoints.Count - 1)
        {
            Vector3 target = waypoints[targetIdx].pos;
            Vector3 to = target - transform.position;
            float dist = to.magnitude;

            // �� Linear ���l�ɃX�i�b�v
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

        // �ŏI�_�� Arrive�i�����j
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

    // ===== Catmull-Rom ��ԁi��WP�̋ߖT�Œ�~�j =====
    void StepSpline()
    {
        if (sampled == null || sampled.Count < 2) { StepLinear(); return; }

        // ���ɒ�~���ׂ��g���̃E�F�C�|�C���g�h�ɋ߂Â������~
        if (nextStopIndex < waypoints.Count)
        {
            Vector3 stopPos = waypoints[nextStopIndex].pos;
            if (Vector3.Distance(transform.position, stopPos) <= stopThreshold && !stoppedAt.Contains(nextStopIndex))
            {
                // �X�i�b�v���Ē�~
                transform.position = stopPos;
                float w = waypoints[nextStopIndex].wait;
                if (w > 0f) { waiting = true; waitTimer = w; }
                stoppedAt.Add(nextStopIndex);
                nextStopIndex++;
                return;
            }
        }

        // ���Ԋu�T���v�����u�������������v�Ői�߂�
        float step = speed * Time.deltaTime;
        splineCursor += step;

        int idx = Mathf.FloorToInt(splineCursor);
        if (idx >= sampled.Count - 1)
        {
            transform.position = sampled[sampled.Count - 1];
            // �ŏIWP�ł̑ҋ@�𑸏d
            float w = waypoints[waypoints.Count - 1].wait;
            if (w > 0f) { waiting = true; waitTimer = w; }
            else { enabled = false; }
            return;
        }
        float ft = splineCursor - idx;
        transform.position = Vector3.Lerp(sampled[idx], sampled[idx + 1], ft);
    }

    // ---- ���[�e�B���e�B ----
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
