// Assets/Scripts/Boss/BossManager.cs
using UnityEngine;
using UnityEngine.Events;

public enum BossType { Mid, Final }

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Refs")]
    public ParallaxController parallax;  // 背景スクロール（なければFindで補完）
    public StageFlow stageFlow;          // 通常出現を一時停止させるため
    public GameObject clearPanel;        // 大ボス撃破後に表示するパネル（非アクティブで置いておく）

    [Header("Options")]
    public bool stopTimeOnClear = true;  // クリア時に Time.timeScale=0

    public bool IsBossActive { get; private set; }
    public BossType ActiveBossType { get; private set; }

    // UIや他システム向けフック
    public UnityEvent<BossType> OnBossStarted = new();
    public UnityEvent<BossType> OnBossDefeated = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!parallax) parallax = FindObjectOfType<ParallaxController>();
        if (!stageFlow) stageFlow = FindObjectOfType<StageFlow>();
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

        // BGM切替
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
            // クリア処理
            if (clearPanel) clearPanel.SetActive(true);
            if (stopTimeOnClear) Time.timeScale = 0f;
            // BGM はこのまま（勝利ジングル等入れるならここで切替）
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
