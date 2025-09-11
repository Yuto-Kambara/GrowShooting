// Assets/Scripts/Enemy/Bullets/BeamProjectile.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �w������ɂ܂������L�т�r�[���B�\���͈͓��ɂ���ԁA���y�[�X(DPS)��HP�����B
/// �EBoxCollider2D (isTrigger) �K�{
/// �E�����ڂ�SpriteRenderer����OK�i���̓R�[�h����X�P�[���j
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
    public float tickInterval = 0.1f;  // dps �����̊Ԋu�Ɋ����ė^����

    float t;
    BoxCollider2D col;
    readonly Dictionary<int, float> nextTick = new(); // �Ώۂ��Ƃ̎���_���[�W����

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

        // �����ځ�������̃T�C�Y�FX�����ɒ����AY��������
        float beamLen = 50f; // ��ʊO�܂œ͂��z��̑傫�ߒl
        transform.right = dir; // X����i�s������
        transform.localScale = new Vector3(beamLen, width, 1f);
        col.size = new Vector2(1f, 1f); // �X�P�[���ŐL�΂��O��
        col.offset = new Vector2(0.5f, 0f); // �E�����ɐL�΂�
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
