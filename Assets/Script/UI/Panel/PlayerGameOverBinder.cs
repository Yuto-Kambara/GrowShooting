// Assets/Scripts/UI/PlayerDeathGameOverBinder.cs
using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerDeathGameOverBinder : MonoBehaviour
{
    [Header("Optional Reference (assign in Inspector if possible)")]
    [SerializeField] GameOverUI ui;  // ← Inspector で直接割り当て推奨

    Health health;

    void Awake()
    {
        health = GetComponent<Health>();

        // Inspector 未設定なら検索で補完
        if (!ui)
        {
#if UNITY_2023_1_OR_NEWER
            // 非アクティブも検索に含める
            ui = Object.FindFirstObjectByType<GameOverUI>(FindObjectsInactive.Include);
#else
            // 旧API（2023.1 未満）用フォールバック
            ui = Object.FindObjectOfType<GameOverUI>(true);
#endif
        }

        if (!ui)
            Debug.LogWarning("[PlayerDeathGameOverBinder] GameOverUI がシーンに見つかりません。");
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
            Debug.LogWarning("[PlayerDeathGameOverBinder] UIなし。TimeScale=0で停止。");
        }
    }
}
