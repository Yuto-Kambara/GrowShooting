using UnityEngine;

/// <summary>
/// K�F�ʏ�V���b�g�i�����Ă���ԁAfireRate �ɏ]���ĘA�ˁj
/// L�F�`���[�W�V���b�g�i�����n�߁����߁A�������Ƃ��� chargeTime �ȏ�Ȃ甭�ˁj
/// ��L �ł��ߎn�߂���ʏ�V���b�g�͑���~���}��
/// </summary>
public class Shooter : MonoBehaviour
{
    [Header("Refs")]
    public Transform muzzle;

    [Header("Fire Params")]
    public float fireRate = 8f;       // ��/�b�iK�������Ă���Ԃ̘A�˃��[�g�j
    public float chargeTime = 1.2f;   // ����ȏソ�߂���`���[�W�e

    [Header("Keys")]
    [SerializeField] private KeyCode normalKey = KeyCode.K;  // �ʏ�V���b�g
    [SerializeField] private KeyCode chargeKey = KeyCode.L;  // �`���[�W�V���b�g

    private ObjectPool chargePool;
    private ObjectPool normalPool;

    [HideInInspector] public float normalDamageMul = 1f;
    [HideInInspector] public float chargePowerMul = 1f;

    // --- ������� ---
    float normalCd;          // �ʏ�V���b�g�p�N�[���_�E��
    float chargeHold;        // �`���[�W�ێ�����
    bool  isCharging;        // ���܃`���[�W����

    void Start()
    {
        normalPool = PoolManager.Instance?.NormalBulletPool;
        if (!normalPool) Debug.LogError("[Shooter] normalPool �����ݒ�ł�");
        chargePool = PoolManager.Instance?.ChargeBulletPool;
        if (!chargePool) Debug.LogError("[Shooter] chargePool �����ݒ�ł�");
    }

    void Update()
    {
        // ==============================
        // 1) �`���[�W���́iL�j
        // ==============================
        if (Input.GetKeyDown(chargeKey))
        {
            // �`���[�W�J�n�F�ʏ�V���b�g�𑦒�~
            isCharging = true;
            chargeHold = 0f;
            StopNormalFire();
        }

        if (isCharging)
        {
            chargeHold += Time.deltaTime;
            // �`���[�W���͒ʏ�V���b�g�͗}�������i���̒ʏ폈���� !isCharging ������j
        }

        if (Input.GetKeyUp(chargeKey))
        {
            if (chargeHold >= chargeTime)
            {
                FireCharge();
            }
            // ���Z�b�g
            isCharging = false;
            chargeHold = 0f;
        }

        // ==============================
        // 2) �ʏ�V���b�g���́iK�j
        //    ���`���[�W���͔��˂��Ȃ�
        // ==============================
        if (!isCharging)
        {
            // �������u�Ԃɑ����˂����A�Ȍ�� fireRate �ɏ]���ĘA��
            if (Input.GetKeyDown(normalKey))
            {
                FireNormal();
                normalCd = 1f / fireRate;
            }
            else if (Input.GetKey(normalKey))
            {
                normalCd -= Time.deltaTime;
                if (normalCd <= 0f)
                {
                    FireNormal();
                    normalCd = 1f / fireRate;
                }
            }
            else
            {
                // �L�[�𗣂��Ă���Ԃ�CD�����Z�b�g���Ă����Ǝ��񉟉��ő����˂ł���
                normalCd = 0f;
            }
        }
    }

    // --- Helpers -----------------------------------------------------

    void StopNormalFire()
    {
        // �ʏ�V���b�g�p�̃N�[���_�E�������Z�b�g�i����ɒe���o�Ȃ��悤�Ɂj
        normalCd = 0f;
    }

    void FireNormal()
    {
        if (!normalPool || !muzzle) return;

        var go = normalPool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul);
    }

    void FireCharge()
    {
        if (!chargePool || !muzzle) return;

        var go = chargePool.Spawn(muzzle.position, Quaternion.identity);
        var b = go.GetComponent<Bullet>();
        b.damage = Mathf.RoundToInt(b.damage * normalDamageMul * chargePowerMul);

        // ���ӁF�v�[�����O�^�p�ł̓X�P�[�����ݐς��Ȃ��悤��
        // Bullet ���ŏ����X�P�[���𕜌�����d�g�݂�����ƈ��S�ł��B
        go.transform.localScale *= chargePowerMul;   // �T�C�Y���g��
    }
}
