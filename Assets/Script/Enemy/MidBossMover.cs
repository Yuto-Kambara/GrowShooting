// Assets/Scripts/Boss/MidBossMover.cs
using UnityEngine;

/// <summary>
/// ���{�X�p�̊�{���[�o�[���`�B
/// - �����ʒu����w��́g�┑�_(Anchor)�h�܂œ��� �� �Ȍ�͂�邭�h��邾��
/// - �{�i�����͌�Ŏ����i�X�e�[�g/�p�^�[����ǉ�����z��j
/// </summary>
[DisallowMultipleComponent]
public class MidBossMover : MonoBehaviour
{
    [Header("Entry (����)")]
    [Tooltip("���[���h���W�B�����Ɉړ����Ē┑����")]
    public Vector2 anchorWorld = new Vector2(6f, 0.5f);
    public float entrySpeed = 6f;
    public float arriveDistance = 0.05f;

    [Header("Idle (�ҋ@��炬)")]
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
        if (!_arrived) // ����
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

        // Idle�F�┑�_�܂��Ōy���h�炷�i�����ڗp�j
        _phase += idleOscFrequency * (Mathf.PI * 2f) * Time.deltaTime;
        Vector2 offset = new Vector2(
            Mathf.Sin(_phase.x) * idleOscAmplitude.x,
            Mathf.Sin(_phase.y) * idleOscAmplitude.y
        );
        transform.position = _anchor + offset;
    }

    // --- �O�����瓮�I�ɒ┑�_��ύX�������ꍇ�p ---
    public void SetAnchor(Vector2 worldPos)
    {
        anchorWorld = worldPos;
        _anchor = worldPos;
    }
}
