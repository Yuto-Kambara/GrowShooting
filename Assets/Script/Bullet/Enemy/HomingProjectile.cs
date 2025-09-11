// Assets/Scripts/Enemy/Bullets/HomingProjectile.cs
using UnityEngine;

/// <summary>
/// Bullet �Ɠ����̐݌v�œ����ǔ��e:
/// - Update����dir���񓪁�Translate�ňړ�
/// - life�o�߂Ŏ�������
/// - Trigger�Փ˂ő����Health�Ƀ_���[�W������
/// - Z�Œ�(lockZ)�Ŕw�i�̌��֐���Ȃ�
/// �ǔ��́u��苗���܂ŋ߂Â� or ���Ԑ؂�v�ŉ������A���̌�͒��i�B
/// </summary>
public class HomingProjectile : MonoBehaviour
{
    [Header("Movement/Damage (Bullet����)")]
    public float speed = 6f;
    public float damage = 1f;
    public Vector2 dir = Vector2.right; // ���ˎ��̌����iEnemyShooter ����ݒ�j
    public float life = 6f;

    [Header("Homing params")]
    public float turnRate = 540f;     // ���񑬓x(deg/sec)
    public float nearDistance = 2f;   // �����܂ŋ߂Â�����u���ł������v��Ԃ�
    public float breakDistance = 5f;  // ��x�߂Â�����A������藣�ꂽ��ǔ�����
    public float maxHomingTime = 2.5f;// �ǔ��p���̍ő厞��
    public string playerTag = "Player";

    [Header("Render/Sorting helpers")]
    public float lockZ = 0f;          // Z���Œ�i�w�i�̌��ɐ���̂�h�~�j
    public float sizeMul = 1f;        // �����ڃX�P�[���{���iCSV����㏑���j

    // ����
    Transform target;
    float tLife;
    float tHoming;
    bool wasNear;
    bool homingActive = true;

    void OnEnable()
    {
        // ���Z�b�g
        tLife = 0f; tHoming = 0f; wasNear = false; homingActive = true;

        // �Ώێ擾
        var p = GameObject.FindGameObjectWithTag(playerTag);
        target = p ? p.transform : null;

        // �X�P�[�����f
        if (sizeMul > 0f) transform.localScale = Vector3.one * sizeMul;

        // Z�Œ�i�`�揇�̈��艻�j
        var pos = transform.position; pos.z = lockZ; transform.position = pos;
    }

    /// <summary>EnemyShooter ����̏�����</summary>
    public void Init(float dmg, float size, float spd, float turn, float near, float brk, float maxT, float lifeSeconds)
    {
        damage = Mathf.Max(0f, dmg);
        sizeMul = Mathf.Max(0.01f, size);
        speed = Mathf.Max(0.01f, spd);
        turnRate = Mathf.Max(1f, turn);
        nearDistance = Mathf.Max(0f, near);
        breakDistance = Mathf.Max(nearDistance + 0.01f, brk);
        maxHomingTime = Mathf.Max(0.1f, maxT);
        life = Mathf.Max(maxHomingTime, lifeSeconds); // �ǔ��I����������͔�ё�������悤�A�Œ�ł�maxHomingTime
        transform.localScale = Vector3.one * sizeMul;
    }

    void Update()
    {
        // --- �ǔ����W�b�N ---
        if (homingActive && target != null)
        {
            tHoming += Time.deltaTime;

            // ��x�\���߂Â������𔻒�
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= nearDistance) wasNear = true;

            // �߂Â����g��h�ɗ����A�܂��͈�莞�Ԍo�߂Œǔ��I��
            if (wasNear && (dist >= breakDistance || tHoming >= maxHomingTime))
                homingActive = false;

            if (homingActive)
            {
                // ������ dir �� �ڕW���ʂւ̊p�x���� turnRate ���ŋl�߂�
                float curDeg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Vector2 to = (target.position - transform.position);
                if (to.sqrMagnitude > 1e-6f)
                {
                    float tgtDeg = Mathf.Atan2(to.y, to.x) * Mathf.Rad2Deg;
                    float maxStep = turnRate * Time.deltaTime;
                    float newDeg = Mathf.MoveTowardsAngle(curDeg, tgtDeg, maxStep);
                    float rad = newDeg * Mathf.Deg2Rad;
                    dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                }
            }
        }

        // --- �ړ��iBullet �Ɠ����� Translate World�j ---
        transform.Translate(dir.normalized * speed * Time.deltaTime, Space.World);

        // Z ���Œ�i�J�����E�p�����b�N�X�ŃY���Ă����t���[�������j
        var pos = transform.position; pos.z = lockZ; transform.position = pos;

        // --- �����iBullet ���l�j ---
        tLife += Time.deltaTime;
        if (tLife > life) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Bullet �Ɠ����������
        if (gameObject.layer == LayerMask.NameToLayer("EnemyBullet") && other.CompareTag("Player"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
        else if (gameObject.layer == LayerMask.NameToLayer("PlayerBullet") && other.CompareTag("Enemy"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
    }
}
