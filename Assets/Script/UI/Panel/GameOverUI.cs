// Assets/Scripts/UI/GameOverUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] CanvasGroup panel;     // ゲームオーバーパネル（Canvas下の子）
    [SerializeField] Button retryButton;    // リトライボタン

    bool shown = false;

    void Awake()
    {
        if (!panel) panel = GetComponentInChildren<CanvasGroup>(true);
        if (panel)
        {
            panel.alpha = 0f;
            panel.blocksRaycasts = false;
            panel.interactable = false;
            panel.gameObject.SetActive(false);
        }
        if (retryButton)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }
    }

    public void Show()
    {
        if (shown) return;
        shown = true;

        if (panel)
        {
            panel.gameObject.SetActive(true);
            panel.alpha = 1f;
            panel.blocksRaycasts = true;
            panel.interactable = true;
        }

        // ゲーム全体を一時停止
        Time.timeScale = 0f;
        // 必要なら効果音等も止める場合：
        // AudioListener.pause = true;
    }

    void OnRetryClicked()
    {
        // 再開してからロード
        Time.timeScale = 1f;
        // AudioListener.pause = false;

        // 現在シーンを再ロード（完全初期化）
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
