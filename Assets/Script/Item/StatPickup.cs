// Assets/Scripts/Item/StatPickup.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StatPickup : MonoBehaviour
{
    [Header("Effect")]
    public GrowthSystem.StatType statType = GrowthSystem.StatType.MaxHP;
    [Min(1)] public int boostCount = 10;   // �� �C���X�y�N�^�ŉ񐔒���

    // GrowthSystem ��������Ȃ��ꍇ�̃t�H�[���o�b�N�l
    public float hpStepFallback = 0.5f;
    public float regenStepFallback = 0.05f;
    public float speedMulStepFallback = 0.02f;
    public float chargeStepFallback = 0.05f;
    public float damageStepFallback = 0.1f;

    [Header("Lifetime")]
    public float blinkAfter = 8f;          // ���b��ɓ_�ŊJ�n
    public float despawnAfter = 12f;       // ���b��ɏ���
    public float blinkInterval = 0.12f;

    [Header("FX / Misc")]
    public string playerTag = "Player";
    public Vector2 initialImpulse = new(0f, 1.2f); // �ӂ���Ə��
    public bool autoResetRotation = true;

    SpriteRenderer sr;
    Rigidbody2D rb;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        TryGetComponent(out rb);

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (rb) rb.AddForce(initialImpulse, ForceMode2D.Impulse);
        StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        // �_�ł܂őҋ@
        yield return new WaitForSeconds(blinkAfter);

        // �_��
        float t = 0f, dur = Mathf.Max(0f, despawnAfter - blinkAfter);
        while (t < dur)
        {
            if (sr) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(blinkInterval);
            t += blinkInterval;
        }

        Destroy(gameObject);
    }

    void Update()
    {
        if (autoResetRotation) transform.rotation = Quaternion.identity;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // �v���C���[�� Stats / GrowthSystem ���擾
        var stats = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        var growth = other.GetComponent<GrowthSystem>() ?? other.GetComponentInParent<GrowthSystem>();

        if (stats)
        {
            ApplyBoost(stats, growth);
        }

        // SE/�G�t�F�N�g������Ȃ炱��
        Destroy(gameObject);
    }

    void ApplyBoost(PlayerStats stats, GrowthSystem growth)
    {
        // �X�e�b�v�l�� GS ����ǂށi������Ȃ���΃t�H�[���o�b�N�j
        float hp = growth ? growth.hpStep : hpStepFallback;
        float regen = growth ? growth.regenStep : regenStepFallback;
        float speed = growth ? growth.speedMulStep : speedMulStepFallback;
        float charge = growth ? growth.chargeStep : chargeStepFallback;
        float dmg = growth ? growth.damageStep : damageStepFallback;

        int n = Mathf.Max(1, boostCount);
        for (int i = 0; i < n; i++)
        {
            switch (statType)
            {
                case GrowthSystem.StatType.MaxHP: stats.AddMaxHP(hp); break;
                case GrowthSystem.StatType.Regen: stats.AddRegen(regen); break;
                case GrowthSystem.StatType.Speed: stats.AddSpeed(speed); break;
                case GrowthSystem.StatType.ChargePower: stats.AddCharge(charge); break;
                case GrowthSystem.StatType.NormalDamage: stats.AddNormalDamage(dmg); break;
            }
        }

        // ���� GrowthSystem �� UI �����X�V�������Ȃ�A�����ŃC�x���g��@���̂���
        // growth?.OnEnergyAbsorbed(0); // 0��ł� StatsChanged ���N���������ꍇ�Ȃ�
    }
}
