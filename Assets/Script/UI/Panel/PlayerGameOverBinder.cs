// Assets/Scripts/UI/PlayerDeathGameOverBinder.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathGameOverBinder : MonoBehaviour
{
    [Header("Optional Reference (assign in Inspector if possible)")]
    [SerializeField] GameOverUI ui;  // �� Inspector �Œ��ڊ��蓖�Đ���

    Health health;

    void Awake()
    {
        health = GetComponent<Health>();

        // Inspector ���ݒ�Ȃ猟���ŕ⊮
        if (!ui)
        {
#if UNITY_2023_1_OR_NEWER
            // ��A�N�e�B�u�������Ɋ܂߂�
            ui = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
#else
            // ��API�i2023.1 �����j�p�t�H�[���o�b�N
            ui = Object.FindObjectOfType<GameOverUI>(true);
#endif
        }

        if (!ui)
            Debug.LogWarning("[PlayerDeathGameOverBinder] GameOverUI ���V�[���Ɍ�����܂���B");
    }

    void OnEnable()
    {
        if (health) health.onDeath.AddListener(OnPlayerDead);
    }

    void OnDisable()
    {
        if (health) health.onDeath.RemoveListener(OnPlayerDead);
    }

    void OnPlayerDead()
    {
        if (ui) ui.Show();
        else
        {
            Time.timeScale = 0f;
            Debug.LogWarning("[PlayerDeathGameOverBinder] UI�Ȃ��BTimeScale=0�Œ�~�B");
        }
    }
}
