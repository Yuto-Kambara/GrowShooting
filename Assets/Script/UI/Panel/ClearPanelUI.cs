// Assets/Scripts/UI/ClearPanelUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
using TMPro;
#endif

public class ClearPanelUI : MonoBehaviour
{
    private const string BestScoreKey = "BestScore";

    [Header("UI Refs")]
    [Tooltip("�X�R�A�\���p�e�L�X�g�iText �܂��� TMP_Text �̂ǂ��炩�����蓖�āj")]
    public Text scoreTextLegacy;
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
    public TMP_Text scoreTextTMP;
#endif

    [Tooltip("�wNew Record�x�\���BText/TMP_Text/�C�ӂ�GameObject�ǂ�ł�OK")]
    public GameObject newRecordGO;      // ���ꂪ����� SetActive(true/false)
    public Text newRecordLegacy;        // �Ȃ��Ă�OK�iText�Œ��ڕ\��/��\���j
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
    public TMP_Text newRecordTMP;       // �Ȃ��Ă�OK�iTMP�Œ��ڕ\��/��\���j
#endif

    [Tooltip("���g���C�i�ŏ�����j�{�^��")]
    public Button retryButton;
    [Tooltip("�X�^�[�g�i�^�C�g���j�ɖ߂�{�^��")]
    public Button startButton;

    [Header("Start Scene")]
    [Tooltip("�X�^�[�g�i�^�C�g���j�ɖ߂��̃V�[�����BBuild Settings �ɓo�^���Ă�������")]
    public string startSceneName = "TitleScene";

    [Header("Formatting")]
    [Tooltip("�X�R�A�̃t�H�[�}�b�g�B{0} �ɐ��l������܂�")]
    public string scoreFormat = "SCORE : {0}";
    [Tooltip("3����؂�ŕ\������ꍇ true")]
    public bool useThousandsSeparator = true;

    [Header("New Record Text")]
    public string newRecordText = "NEW RECORD!";

    void Awake()
    {
        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
        if (startButton)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartClicked);
        }
        // �����͔�\��
        ShowNewRecord(false);
    }

    void OnEnable()
    {
        // �p�l�����\�����ꂽ�Ƃ��ɃX�R�A�𔽉f���x�X�g����E�ۑ�
        UpdateScoreAndRecord();
    }

    void UpdateScoreAndRecord()
    {
        int current = ScoreManager.Instance ? ScoreManager.Instance.TotalScore : 0;

        // �\��
        string num = useThousandsSeparator ? current.ToString("N0") : current.ToString();
        string text = string.Format(scoreFormat, num);
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
        if (scoreTextTMP) scoreTextTMP.text = text;
#endif
        if (scoreTextLegacy) scoreTextLegacy.text = text;

        // �x�X�g�X�R�A�擾
        bool hasBest = PlayerPrefs.HasKey(BestScoreKey);
        int best = hasBest ? PlayerPrefs.GetInt(BestScoreKey, 0) : 0;

        // ���N���A�i= �x�X�g���ۑ��j�܂��� �L�^�X�V�� NewRecord
        bool isNewRecord = !hasBest || current > best;

        if (isNewRecord)
        {
            PlayerPrefs.SetInt(BestScoreKey, current);
            PlayerPrefs.Save();
        }

        ShowNewRecord(isNewRecord);
    }

    void ShowNewRecord(bool on)
    {
        if (newRecordGO) newRecordGO.SetActive(on);

        if (newRecordLegacy)
        {
            newRecordLegacy.gameObject.SetActive(on);
            if (on) newRecordLegacy.text = newRecordText;
        }
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
        if (newRecordTMP)
        {
            newRecordTMP.gameObject.SetActive(on);
            if (on) newRecordTMP.text = newRecordText;
        }
#endif
    }

    void OnRetryClicked()
    {
        // �ꎞ��~���������Ă��烍�[�h
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void OnStartClicked()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            Debug.LogError("[ClearPanelUI] startSceneName �����ݒ�ł��BBuild Settings �ɓo�^�ς݂̃V�[�������w�肵�Ă��������B");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }
}
