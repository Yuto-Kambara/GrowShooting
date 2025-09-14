// Assets/Scripts/Boss/BossManager.cs
using UnityEngine;
using UnityEngine.Events;

public enum BossType { Mid, Final }

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Refs")]
    public ParallaxController parallax;  // �w�i�X�N���[���i�Ȃ����Find�ŕ⊮�j
    public StageFlow stageFlow;          // �ʏ�o�����ꎞ��~�����邽��
    public GameObject clearPanel;        // ��{�X���j��ɕ\������p�l���i��A�N�e�B�u�Œu���Ă����j

    [Header("Options")]
    public bool stopTimeOnClear = true;  // �N���A���� Time.timeScale=0

    public bool IsBossActive { get; private set; }
    public BossType ActiveBossType { get; private set; }

    // UI�⑼�V�X�e�������t�b�N
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

    // ===== �O��API =====
    public void BeginBoss(BossType type)
    {
        IsBossActive = true;
        ActiveBossType = type;

        // �X�N���[����~
        if (parallax) parallax.SetPaused(true);

        // �ʏ�X�|�[����~
        if (stageFlow) stageFlow.SetPausedByBoss(true);

        // BGM�ؑ�
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
            // �N���A����
            if (clearPanel) clearPanel.SetActive(true);
            if (stopTimeOnClear) Time.timeScale = 0f;
            // BGM �͂��̂܂܁i�����W���O���������Ȃ炱���Őؑցj
        }
        else
        {
            // ���{�X �� �X�e�[�W�֕��A
            AudioManager.Instance?.PlayStageBgm();
            if (parallax) parallax.SetPaused(false);
            if (stageFlow) stageFlow.SetPausedByBoss(false);
        }

        IsBossActive = false;
    }
}
