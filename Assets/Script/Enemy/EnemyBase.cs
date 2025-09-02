using UnityEngine;
using System.Collections;

public class EnemyBase : MonoBehaviour
{
    [Header("移動")]
    public float speed = 2f;

    [Header("ヒット時シェイク")]
    public float shakeAmplitude = 0.15f;
    public float shakeDuration = 0.12f;
    public int shakeCycles = 2;       // 何往復するか

    [Header("撃破スコア")]
    public int scoreValue = 100;

    Camera cam;
    Coroutine shakeCo;

    void Awake()
    {
        cam = Camera.main;
        var hp = GetComponent<Health>();
        if (hp)
        {
            hp.onHit.AddListener(StartShake);
            hp.onDeath.AddListener(AddKillScore);
        }
    }

    void Update()
    {
        transform.Translate(Vector2.left * speed * Time.deltaTime, Space.World);

        // 画面外破棄
        Vector3 v = cam.WorldToViewportPoint(transform.position);
        if (v.x < -0.05f || v.y < -0.1f || v.y > 1.1f)
            Destroy(gameObject);
    }

    //----------------------------------------------------
    void StartShake()
    {
        if (shakeCo != null) StopCoroutine(shakeCo);
        shakeCo = StartCoroutine(HitShake());
    }

    IEnumerator HitShake()
    {
        float elapsed = 0f;
        float baseY = transform.localPosition.y;

        while (elapsed < shakeDuration)
        {
            // 0‒1 のサイクルを sin で ±1 → 振幅を掛ける
            float phase = elapsed / shakeDuration * Mathf.PI * 2f * shakeCycles;
            float offset = Mathf.Sin(phase) * shakeAmplitude;

            Vector3 pos = transform.localPosition;
            pos.y = baseY + offset;         // ★X はいじらない／Y だけ足す
            transform.localPosition = pos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 終了時に Y を元に戻す
        Vector3 p = transform.localPosition;
        p.y = baseY;
        transform.localPosition = p;
        shakeCo = null;
    }
    void AddKillScore()
    {
        if (ScoreManager.Instance) ScoreManager.Instance.AddScore(scoreValue);
    }
}
