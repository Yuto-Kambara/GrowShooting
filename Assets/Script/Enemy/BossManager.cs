// Assets/Scripts/Boss/BossManager.cs
using UnityEngine;
using UnityEngine.Events;

public enum BossType { Mid, Final }

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Refs")]
    public ParallaxController parallax;  // 背景スクロール（未設定なら Awake で補完）
    public StageFlow stageFlow;          // 通常出現を一時停止
    public GameObject clearPanel;        // 大ボス撃破後に表示（シーン内のもの）

    [Header("Options")]
    public bool stopTimeOnClear = true;  // クリア時に Time.timeScale=0

    public bool IsBossActive { get; private set; }
    public BossType ActiveBossType { get; private set; }

    // UIや他システム向けフック
    public UnityEvent<BossType> OnBossStarted = new();
    public UnityEvent<BossType> OnBossDefeated = new();

    void Awake()
    {
        // シーン内シングルトン（永続化しない）
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 参照補完（シーンごとに取り直す）
        if (!parallax) parallax = FindFirstObjectByType<ParallaxController>();
        if (!stageFlow) stageFlow = FindFirstObjectByType<StageFlow>();

        // clearPanel はシーン側で非アクティブ配置が前提。未割り当てなら軽く探索（任意）
        if (!clearPanel)
        {
            // よくある名前やタグで探す（必要に応じて調整）
            clearPanel = GameObject.Find("ClearPanel");
            // タグ運用しているなら：clearPanel = GameObject.FindWithTag("ClearPanel");
        }

        IsBossActive = false;
    }

    // ===== 外部API =====
    public void BeginBoss(BossType type)
    {
        IsBossActive = true;
        ActiveBossType = type;

        // スクロール停止
        if (parallax) parallax.SetPaused(true);

        // 通常スポーン停止
        if (stageFlow) stageFlow.SetPausedByBoss(true);

        // BGM切替（AudioManager 側の自動購読に任せていても冪等）
        switch (type)
        {
            case BossType.Mid: AudioManager.Instance?.PlayMidBossBgm(); break;
            case BossType.Final: AudioManager.Instance?.PlayFinalBossBgm(); break;
        }

        OnBossStarted.Invoke(type);
    }

    public void EndBoss(BossType type)
    {
        if (!IsBossActive) return;

        OnBossDefeated.Invoke(type);

        if (type == BossType.Final)
        {
            // 念のため、ここで null なら再取得を試みる（シーン側で名前を変えていなければ拾える）
            if (!clearPanel)
                clearPanel = GameObject.Find("ClearPanel"); // or FindWithTag

            // クリア処理
            if (clearPanel)
                clearPanel.SetActive(true);
            else
                Debug.LogWarning("[BossManager] clearPanel が見つかりません。シーン内に非アクティブで配置し、参照を割り当ててください。");

            if (stopTimeOnClear) Time.timeScale = 0f;

            // BGM は AudioManager が OnBossDefeated(Final) を受けて ClearBgm に遷移
        }
        else
        {
            // 中ボス → ステージへ復帰
            AudioManager.Instance?.PlayStageBgm();
            if (parallax) parallax.SetPaused(false);
            if (stageFlow) stageFlow.SetPausedByBoss(false);
        }

        IsBossActive = false;
    }
}
