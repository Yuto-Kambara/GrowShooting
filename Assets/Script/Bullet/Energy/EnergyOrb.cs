using UnityEngine;

public class EnergyOrb : MonoBehaviour
{
    Transform target;         // �v���C���[
    float speed;              // �ړ����x
    GrowthSystem growth;      // ���������R�[���p

    // �Ăяo������ Spawn ��ɑ� Init ����
    public void Init(Transform tgt, float spd, GrowthSystem g)
    {
        target = tgt; speed = spd; growth = g;
    }

    void Update()
    {
        if (!target) { Destroy(gameObject); return; }

        // �v���C���[�֒��i
        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // ���B����
        if (Vector3.SqrMagnitude(target.position - transform.position) < 0.04f)
        {
            growth?.OnEnergyAbsorbed(1);   // �� 1 ���Ԃ񐬒�
            Destroy(gameObject);
        }
    }
}
