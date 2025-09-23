// Assets/Scripts/UI/GameOverUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] CanvasGroup panel;     // �Q�[���I�[�o�[�p�l���iCanvas���̎q�j
    [SerializeField] Button retryButton;    // ���g���C�{�^��

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

        // �Q�[���S�̂��ꎞ��~
        Time.timeScale = 0f;
        // �K�v�Ȃ���ʉ������~�߂�ꍇ�F
        // AudioListener.pause = true;
    }

    void OnRetryClicked()
    {
        // �ĊJ���Ă��烍�[�h
        Time.timeScale = 1f;
        // AudioListener.pause = false;

        // ���݃V�[�����ă��[�h�i���S�������j
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}
