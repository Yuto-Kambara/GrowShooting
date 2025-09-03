using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health.hp / maxHP が float になった版の HP バー制御
/// </summary>
public class HPBarController : MonoBehaviour
{
    [Header("参照オブジェクト")]
    [SerializeField] Health playerHealth;   // float 版 Health
    [SerializeField] Image fillImage;      // Type = Filled / Horizontal / Left

    [Header("色とアニメ")]
    [SerializeField] Color fullColor = new(0.00f, 0.55f, 0.70f);
    [SerializeField] Color emptyColor = new(0.75f, 0.00f, 0.00f);
    [SerializeField, Min(0.1f)] float lerpSpeed = 5f;

    float targetRatio = 1f;   // 最新値
    float viewRatio = 1f;   // 表示値（補間用）

    /*------------------------------*/
    void Start()
    {
        if (!playerHealth || !fillImage)
        {
            Debug.LogError("[HPBarController] 参照が未設定"); enabled = false; return;
        }

        // 初期値を反映
        UpdateImmediate(playerHealth.hp, playerHealth.maxHP);

        // HP 変更イベントを購読（float,float）
        playerHealth.onHpChanged.AddListener(UpdateTarget);
    }

    /*------------------------------*/
    void Update()
    {
        // 補間して滑らかに表示
        viewRatio = Mathf.MoveTowards(viewRatio, targetRatio, lerpSpeed * Time.deltaTime);
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    /*------------------------------*/
    // HP が変わった時に呼ばれるコールバック
    void UpdateTarget(float current, float max)
    {
        targetRatio = (max <= 0f) ? 0f : current / max;
    }

    // Start 時など完全に即反映したい時用
    void UpdateImmediate(float current, float max)
    {
        targetRatio = viewRatio = (max <= 0f) ? 0f : current / max;
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }
}
