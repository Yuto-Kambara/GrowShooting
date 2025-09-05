using GrowShooting.Audio;
using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("移動（EnemyMover がある場合はそちらを優先）")]
    public float speed = 2f;
    public bool useEnemyMoverIfAvailable = true;
    public bool fallbackLinearMove = true;
    public Vector2 fallbackDir = Vector2.left;

    [Header("到着/画面外での破棄")]
    public bool despawnOnArrival = true;
    public bool despawnOffscreen = true;
    [Tooltip("画面端からの余白（Viewport）")]
    public float offscreenMargin = 0.05f;

    [Tooltip("一度でも画面内に入ってからでないと画面外破棄しない")]
    public bool requireVisibleOnce = true;

    [Tooltip("出現直後はこの秒数だけ画面外でも破棄しない（保険）")]
    public float offscreenGraceSeconds = 1.0f;

    [Header("ヒット時シェイク")]
    public float shakeAmplitude = 0.15f;
    public float shakeDuration = 0.12f;
    public int shakeCycles = 2;

    [Header("撃破スコア")]
    public int scoreValue = 100;

    Camera cam;
    Coroutine shakeCo;
    EnemyMover mover;

    // --- 追加フラグ ---
    bool hasBeenVisible = false;   // 画面内に一度でも入ったか
    float spawnTime;               // 出現時刻

    void Awake()
    {
        cam = Camera.main;
        mover = GetComponent<EnemyMover>();

        var hp = GetComponent<Health>();
        if (hp)
        {
            hp.onDeath.AddListener(AddKillScore);
            hp.onDeath.AddListener(RingOnDestroy);
        }

        if (fallbackDir.sqrMagnitude > 0f) fallbackDir.Normalize(); else fallbackDir = Vector2.left;
        spawnTime = Time.time;
    }

    // SpriteRenderer 等が付いている場合、可視になった瞬間にコールされる
    void OnBecameVisible() { hasBeenVisible = true; }

    void Update()
    {
        // 1) EnemyMover があれば任せる（到着で停止→破棄任意）
        if (useEnemyMoverIfAvailable && mover)
        {
            if (!mover.enabled && despawnOnArrival)
            {
                Destroy(gameObject);
                return;
            }
        }
        // 2) フォールバック直進
        else if (fallbackLinearMove)
        {
            transform.Translate((Vector3)fallbackDir * speed * Time.deltaTime, Space.World);
        }

        // 3) 画面外破棄（改善版）
        if (despawnOffscreen && cam)
        {
            Vector3 v = cam.WorldToViewportPoint(transform.position);
            bool inside =
                v.z > 0f && // カメラの前
                v.x > 0f - offscreenMargin && v.x < 1f + offscreenMargin &&
                v.y > 0f - offscreenMargin && v.y < 1f + offscreenMargin;

            if (inside) hasBeenVisible = true;

            // 破棄条件：
            //  - 「一度は画面内に入った」場合のみ画面外で破棄
            //  - まだ入っていなくても、猶予時間を過ぎたら破棄（迷子対策）
            bool graceOver = (Time.time - spawnTime) >= offscreenGraceSeconds;

            if (!inside && ((requireVisibleOnce && hasBeenVisible) || (!requireVisibleOnce && graceOver)))
            {
                Destroy(gameObject);
            }
        }
    }

    void AddKillScore()
    {
        if (ScoreManager.Instance) ScoreManager.Instance.AddScore(scoreValue);
    }

    void RingOnDestroy()
    {
        SoundManager.Instance.Play(SoundEffect.EnemyDown);
    }

}
