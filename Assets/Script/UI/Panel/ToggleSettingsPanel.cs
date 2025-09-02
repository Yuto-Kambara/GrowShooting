using UnityEngine;

public class ToggleSettingsPanel : MonoBehaviour
{
    [Header("�J�Ώۂ̃p�l��")]
    [SerializeField] GameObject settingsPanel;     // �� SettingsPanel ���h���b�O

    [Header("�J���Ă���Ԃ̓|�[�Y�H")]
    [SerializeField] bool pauseWhileOpen = true;

    bool isOpen;

    // �{�^���� OnClick ����Ă΂����J���\�b�h
    public void Toggle()
    {
        isOpen = !isOpen;
        settingsPanel.SetActive(isOpen);

        if (pauseWhileOpen)
            Time.timeScale = isOpen ? 0f : 1f;     // 0 = �|�[�Y / 1 = �ʏ�
    }

    // �O�̂��߁A�V�[���J�ڎ��Ƀ^�C���X�P�[����߂�
    void OnDisable()
    {
        if (pauseWhileOpen) Time.timeScale = 1f;
    }
}
