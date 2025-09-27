using UnityEngine;
using UnityEngine.UI;

public class TitleUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] GameObject titlePanel;
    [SerializeField] GameObject howToPanel;
    [SerializeField] GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] Button startBtn;
    [SerializeField] Button howToBtn, howToBackBtn;
    [SerializeField] Button settingsBtn, settingsBackBtn;
    [SerializeField] Button quitBtn;   // PC用

    void Start()
    {
        // ボタンへリスナー登録
        startBtn.onClick.AddListener(() => SceneLoader.LoadGame());

        howToBtn.onClick.AddListener(() => howToPanel.SetActive(true));
        howToBtn.onClick.AddListener(() => titlePanel.SetActive(false));
        howToBackBtn.onClick.AddListener(() => howToPanel.SetActive(false));
        howToBackBtn.onClick.AddListener(() => titlePanel.SetActive(true));

        settingsBtn.onClick.AddListener(() => settingsPanel.SetActive(true));
        settingsBtn.onClick.AddListener(() => titlePanel.SetActive(false));
        settingsBackBtn.onClick.AddListener(() => settingsPanel.SetActive(false));
        settingsBackBtn.onClick.AddListener(() => titlePanel.SetActive(true));

#if UNITY_WEBGL || UNITY_ANDROID || UNITY_IOS
        quitBtn.gameObject.SetActive(false);
#else
        quitBtn.onClick.AddListener(() => Application.Quit());
#endif
    }
}
