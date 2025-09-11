using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Motion")]
    public float speed = 12f;
    public float damage = 1f;
    public Vector2 dir = Vector2.right;
    public float life = 3f;

    [Header("Size (×Prefab基準)")]
    [Tooltip("1=Prefab基準サイズ。CSVや発射側から SetSizeMul() で上書き可")]
    public float sizeMul = 1f;

    float t;
    Vector3 baseScale;   // Prefabの基準スケール（累積防止用）

    void Awake()
    {
        // プール再利用でも崩れないよう、Prefab時点のスケールを記憶
        baseScale = transform.localScale;
    }

    void OnEnable()
    {
        t = 0f;
        // 直前の sizeMul を適用（プール復帰時の累積防止）
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

    // ===== 追加API：発射側からサイズ倍率を設定 =====
    public void SetSizeMul(float mul)
    {
        sizeMul = Mathf.Max(0.01f, mul);
        ApplySize();
    }

    void ApplySize()
    {
        // 基準スケール × sizeMul（等倍拡縮）。Collider2DもTransformスケールで追従します
        transform.localScale = new Vector3(baseScale.x * sizeMul,
                                           baseScale.y * sizeMul,
                                           baseScale.z);
    }
}
