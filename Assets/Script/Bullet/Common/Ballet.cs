using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;
    public float damage = 1f;
    public Vector2 dir = Vector2.right;
    public float life = 3f;

    [Header("Size (�~Prefab�)")]
    [Tooltip("1=Prefab��T�C�Y�BCSV�┭�ˑ����� SetSizeMul() �ŏ㏑����")]
    public float sizeMul = 1f;

    float t;
    Vector3 baseScale;   // Prefab�̊�X�P�[���i�ݐϖh�~�p�j

    void Awake()
    {
        // �v�[���ė��p�ł�����Ȃ��悤�APrefab���_�̃X�P�[�����L��
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        t = 0f;
        // ���O�� sizeMul ��K�p�i�v�[�����A���̗ݐϖh�~�j
        ApplySize();
    }

    void Update()
    {
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
        t += Time.deltaTime;
        if (t > life) gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (gameObject.layer == LayerMask.NameToLayer("PlayerBullet") && other.CompareTag("Enemy"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
        else if (gameObject.layer == LayerMask.NameToLayer("EnemyBullet") && other.CompareTag("Player"))
        {
            other.GetComponent<Health>()?.Take(damage);
            gameObject.SetActive(false);
        }
    }

    // ===== �ǉ�API�F���ˑ�����T�C�Y�{����ݒ� =====
    public void SetSizeMul(float mul)
    {
        sizeMul = Mathf.Max(0.01f, mul);
        ApplySize();
    }

    void ApplySize()
    {
        // ��X�P�[�� �~ sizeMul�i���{�g�k�j�BCollider2D��Transform�X�P�[���ŒǏ]���܂�
        transform.localScale = new Vector3(baseScale.x * sizeMul,
                                           baseScale.y * sizeMul,
                                           baseScale.z);
    }
}
