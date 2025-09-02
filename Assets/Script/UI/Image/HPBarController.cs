using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] Health playerHealth;   // プレイヤーの Health
    [SerializeField] Image fillImage;      // Image Type = Filled, Horizontal, Origin=Left

    [Header("色・アニメ")]
    [SerializeField] Color fullColor = new(0.0f, 0.55f, 0.70f); // 青系
    [SerializeField] Color emptyColor = new(0.75f, 0.0f, 0.0f);  // 赤系
    [SerializeField, Min(0.1f)] float lerpSpeed = 5f;            // 補間速度

    float targetRatio = 1f;   // 目的値
    float viewRatio = 1f;   // 表示中の値

    void Start()
    {
        if (!playerHealth || !fillImage)
        {
            Debug.LogError("[HPBarController] 参照が未設定です"); enabled = false; return;
        }

        // 初期値
        UpdateImmediate(playerHealth.hp, playerHealth.maxHP);

        // イベント購読
        playerHealth.onHpChanged.AddListener(UpdateTarget);
    }

    void Update()
    {
        // スムーズに補間
        viewRatio = Mathf.MoveTowards(viewRatio, targetRatio, lerpSpeed * Time.deltaTime);
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    // ----- イベントコールバック -----
    void UpdateTarget(int current, int max)
    {
        targetRatio = (max == 0) ? 0f : (float)current / max;
    }

    // ----- 即時反映用（Start 時など） -----
    void UpdateImmediate(int current, int max)
    {
        targetRatio = viewRatio = (max == 0) ? 0f : (float)current / max;
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }
}
