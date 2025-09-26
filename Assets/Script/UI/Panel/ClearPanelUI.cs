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
    [Tooltip("スコア表示用テキスト（Text または TMP_Text のどちらかを割り当て）")]
    public Text scoreTextLegacy;
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
    public TMP_Text scoreTextTMP;
#endif

    [Tooltip("『New Record』表示。Text/TMP_Text/任意のGameObjectどれでもOK")]
    public GameObject newRecordGO;      // これがあれば SetActive(true/false)
    public Text newRecordLegacy;        // なくてもOK（Textで直接表示/非表示）
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
    public TMP_Text newRecordTMP;       // なくてもOK（TMPで直接表示/非表示）
#endif

    [Tooltip("リトライ（最初から）ボタン")]
    public Button retryButton;
    [Tooltip("スタート（タイトル）に戻るボタン")]
    public Button startButton;

    [Header("Start Scene")]
    [Tooltip("スタート（タイトル）に戻る先のシーン名。Build Settings に登録しておくこと")]
    public string startSceneName = "TitleScene";

    [Header("Formatting")]
    [Tooltip("スコアのフォーマット。{0} に数値が入ります")]
    public string scoreFormat = "SCORE : {0}";
    [Tooltip("3桁区切りで表示する場合 true")]
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
        // 初期は非表示
        ShowNewRecord(false);
    }

    void OnEnable()
    {
        // パネルが表示されたときにスコアを反映＆ベスト判定・保存
        UpdateScoreAndRecord();
    }

    void UpdateScoreAndRecord()
    {
        int current = ScoreManager.Instance ? ScoreManager.Instance.TotalScore : 0;

        // 表示
        string num = useThousandsSeparator ? current.ToString("N0") : current.ToString();
        string text = string.Format(scoreFormat, num);
#if TMP_PRESENT || UNITY_2020_1_OR_NEWER
        if (scoreTextTMP) scoreTextTMP.text = text;
#endif
        if (scoreTextLegacy) scoreTextLegacy.text = text;

        // ベストスコア取得
        bool hasBest = PlayerPrefs.HasKey(BestScoreKey);
        int best = hasBest ? PlayerPrefs.GetInt(BestScoreKey, 0) : 0;

        // 初クリア（= ベスト未保存）または 記録更新で NewRecord
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
        // 一時停止を解除してからロード
        Time.timeScale = 1f;
        var current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }

    void OnStartClicked()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            Debug.LogError("[ClearPanelUI] startSceneName が未設定です。Build Settings に登録済みのシーン名を指定してください。");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(startSceneName);
    }
}
