using UnityEngine;

public class AbsorbSystem : MonoBehaviour
{
    [Header("�z���p�����[�^")]
    public float pullRadius = 1.2f;      // ���q���o���͈�
    public GameObject energyPrefab;      // EnergyOrb �̃v���n�u
    public float energySpeed = 8f;       // ���q�̔�s���x

    GrowthSystem growth; int bulletMask;

    void Awake()
    {
        growth = GetComponentInParent<GrowthSystem>();
        bulletMask = 1 << LayerMask.NameToLayer("EnemyBullet");
    }

    //========= �@ �z�������O�F�e�{�̂��G�ꂽ��]���ʂ�z�� =========
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("EnemyBullet"))
        {
            other.gameObject.SetActive(false);      // �e������
            growth?.OnEnergyAbsorbed(1);            // ���� 1 ���Ԃ�
        }
    }

    //========= �A pullRadius ���ɓ������e���� EnergyOrb ���o�� =========
    void Update()
    {
        // �߂��̓G�e�����m�i�e�͓������Ȃ��I�j
        var hits = Physics2D.OverlapCircleAll(transform.position, pullRadius, bulletMask);

        foreach (var h in hits)
        {
            var flag = h.GetComponent<BulletEnergyFlag>();
            if (!flag || flag.spawned) continue;    // ���ɐ����ς݂Ȃ�X�L�b�v

            flag.spawned = true;                    // �����ς݃}�[�N

            // EnergyOrb ��e�̈ʒu�ɐ������CInit �Ńv���C���[�֌�����
            var orb = Instantiate(energyPrefab, h.transform.position, Quaternion.identity);
            orb.GetComponent<EnergyOrb>().Init(transform.root, energySpeed, growth);
        }
    }
}
