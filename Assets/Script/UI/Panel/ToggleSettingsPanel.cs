using UnityEngine;

public class ToggleSettingsPanel : MonoBehaviour
{
    [Header("開閉対象のパネル")]
    [SerializeField] GameObject settingsPanel;     // ← SettingsPanel をドラッグ

    [Header("開いている間はポーズ？")]
    [SerializeField] bool pauseWhileOpen = true;

    bool isOpen;

    // ボタンの OnClick から呼ばれる公開メソッド
    public void Toggle()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);

        if (pauseWhileOpen)
            Time.timeScale = isOpen ? 0f : 1f;     // 0 = ポーズ / 1 = 通常
    }

    // 念のため、シーン遷移時にタイムスケールを戻す
    void OnDisable()
    {
        if (pauseWhileOpen) Time.timeScale = 1f;
    }
}
