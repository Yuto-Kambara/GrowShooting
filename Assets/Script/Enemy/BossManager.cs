// Assets/Scripts/Boss/BossManager.cs
using UnityEngine;
using UnityEngine.Events;

public enum BossType { Mid, Final }

public class BossManager : MonoBehaviour
{
    public static BossManager Instance { get; private set; }

    [Header("Refs")]
    public ParallaxController parallax;  // �w�i�X�N���[���i���ݒ�Ȃ� Awake �ŕ⊮�j
    public StageFlow stageFlow;          // �ʏ�o�����ꎞ��~
    public GameObject clearPanel;        // ��{�X���j��ɕ\���i�V�[�����̂��́j

    [Header("Options")]
    public bool stopTimeOnClear = true;  // �N���A���� Time.timeScale=0

    public bool IsBossActive { get; private set; }
    public BossType ActiveBossType { get; private set; }

    // UI�⑼�V�X�e�������t�b�N
    public UnityEvent<BossType> OnBossStarted = new();
    public UnityEvent<BossType> OnBossDefeated = new();

    void Awake()
    {
        // �V�[�����V���O���g���i�i�������Ȃ��j
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // �Q�ƕ⊮�i�V�[�����ƂɎ�蒼���j
        if (!parallax) parallax = FindFirstObjectByType<ParallaxController>();
        if (!stageFlow) stageFlow = FindFirstObjectByType<StageFlow>();

        // clearPanel �̓V�[�����Ŕ�A�N�e�B�u�z�u���O��B�����蓖�ĂȂ�y���T���i�C�Ӂj
        if (!clearPanel)
        {
            // �悭���閼�O��^�O�ŒT���i�K�v�ɉ����Ē����j
            clearPanel = GameObject.Find("ClearPanel");
            // �^�O�^�p���Ă���Ȃ�FclearPanel = GameObject.FindWithTag("ClearPanel");
        }

        IsBossActive = false;
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

        // BGM�ؑցiAudioManager ���̎����w�ǂɔC���Ă��Ă��p���j
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
            // �O�̂��߁A������ null �Ȃ�Ď擾�����݂�i�V�[�����Ŗ��O��ς��Ă��Ȃ���ΏE����j
            if (!clearPanel)
                clearPanel = GameObject.Find("ClearPanel"); // or FindWithTag

            // �N���A����
            if (clearPanel)
                clearPanel.SetActive(true);
            else
                Debug.LogWarning("[BossManager] clearPanel ��������܂���B�V�[�����ɔ�A�N�e�B�u�Ŕz�u���A�Q�Ƃ����蓖�ĂĂ��������B");

            if (stopTimeOnClear) Time.timeScale = 0f;

            // BGM �� AudioManager �� OnBossDefeated(Final) ���󂯂� ClearBgm �ɑJ��
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
