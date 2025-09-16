using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] PlayerStats playerStats;       // 最大HP/Cap のソース
    [SerializeField] Health playerHealth;           // 現在HPのソース
    [SerializeField] Image fillImage;               // ★ 9-slice 用（type = Sliced 推奨）
    [SerializeField] RectTransform frameRect;       // 外枠Rect（未指定なら fill の親）

    [Header("色とアニメ")]
    [SerializeField] Color fullColor = new(0.00f, 0.55f, 0.70f);
    [SerializeField] Color emptyColor = new(0.75f, 0.00f, 0.00f);
    [SerializeField, Min(0.1f)] float hpLerpSpeed = 5f;  // ← 中バーの補間（幅を補間）

    [Header("レイアウト")]
    [SerializeField, Min(0f)] float frameEdgeBufferPx = 8f; // 親の左右バッファ（枠用）
    [SerializeField, Min(0f)] float horizontalMarginPx = 2f; // 枠内側の余白（Fill用）

    // 内部状態
    RectTransform fillRect;
    RectTransform parentRect;

    float lastMaxHP = 1f;  // 直前の MaxHP（増減の差分用）
    float baseMaxHP = 1f;  // 起動時の MaxHP（減少時の基準）
    float minFrameWidth = 1f;  // 起動時に決める初期枠長（下限）

    float viewRatio = 1f;    // 現HP/MaxHP（表示用）
    float targetRatio = 1f;    // 現HP/MaxHP（目標）

    void Awake()
    {
        if (!playerStats) playerStats = GetComponentInParent<PlayerStats>();
        if (!playerHealth) playerHealth = GetComponentInParent<Health>();
        if (!fillImage)
        {
            Debug.LogError("[HPBarController] fillImage 未設定"); enabled = false; return;
        }
        if (!frameRect) frameRect = fillImage.transform.parent as RectTransform;

        if (!playerStats || !playerHealth || !frameRect)
        {
            Debug.LogError("[HPBarController] 参照不足（PlayerStats/Health/frameRect）");
            enabled = false; return;
        }

        // ★ Fill 画像は 9-slice を使う（中央だけ伸ばす）
        fillImage.type = Image.Type.Sliced;

        fillRect = fillImage.rectTransform;
        parentRect = frameRect.parent as RectTransform;
        if (!parentRect) parentRect = frameRect; // 保険
    }

    void Start()
    {
        // 1) 起動時の「使用可能幅」（親幅−両端バッファ）
        float parentUsableW = GetParentUsableWidth();

        // 2) 初期比率 = 現 MaxHP / MaxHP_Cap
        float maxCap = Mathf.Max(0.0001f, playerStats.MaxHP_Cap);
        float maxCurr = Mathf.Clamp(playerStats.MaxHP, 0f, maxCap);
        float ratio0 = Mathf.Clamp01(maxCurr / maxCap);

        // 3) 初期枠長 = 親の使用可能幅 × ratio0  （←ご要望どおり）
        float initFrameW = Mathf.Max(1f, parentUsableW * ratio0);

        // 状態を確定
        baseMaxHP = maxCurr;
        lastMaxHP = maxCurr;
        minFrameWidth = initFrameW;

        // 4) 初期レイアウト即時反映（枠位置は左にバッファぶん寄せ）
        ApplyFrameWidth(initFrameW);

        // 5) 中バー（現在HP）即時反映（幅ベース）
        UpdateImmediate(playerHealth.hp, playerStats.MaxHP);

        // 6) イベント購読
        playerStats.onMaxHpChanged.AddListener(OnStatsMaxHpChanged);
        playerHealth.onHpChanged.AddListener(OnHealthChanged);
    }

    void OnDestroy()
    {
        if (playerStats) playerStats.onMaxHpChanged.RemoveListener(OnStatsMaxHpChanged);
        if (playerHealth) playerHealth.onHpChanged.RemoveListener(OnHealthChanged);
    }

    void Update()
    {
        // 中バーは「幅」を補間して伸縮（fillAmount は使用しない）
        float statsMax = Mathf.Max(0.0001f, playerStats.MaxHP);
        float curRatio = Mathf.Clamp01(playerHealth.hp / statsMax);

        // 目標比率は OnHealthChanged で更新しているが、保険で再計算してもよい
        targetRatio = curRatio;

        viewRatio = Mathf.MoveTowards(viewRatio, targetRatio, hpLerpSpeed * Time.deltaTime);

        // 現在の枠幅から、Fill の「最大幅」を取り、viewRatio 分だけ横幅を与える
        float frameW = GetRectWidth(frameRect);
        float fillMaxW = Mathf.Max(0f, frameW - (horizontalMarginPx * 2f));
        SetFillWidth(fillMaxW * viewRatio);

        // 色補間（任意）
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    /* ======= コールバック ======= */

    // 現在HPの変更：割合ターゲットだけ更新（実適用は Update で幅補間）
    void OnHealthChanged(float current, float _maxIgnored)
    {
        float statsMax = Mathf.Max(0.0001f, playerStats.MaxHP);
        targetRatio = Mathf.Clamp01(current / statsMax);
    }

    // 最大HPが変化：単位長ロジックで「枠幅」を即時反映（左右バッファを守る）
    void OnStatsMaxHpChanged(float newMax)
    {
        float delta = newMax - lastMaxHP;
        float parentMaxW = GetParentUsableWidth(); // 親幅 − 2×バッファ
        float currentW = GetRectWidth(frameRect);

        if (Mathf.Abs(delta) > Mathf.Epsilon)
        {
            float newWidth = currentW;

            if (delta > 0f)
            {
                // 増加：Cap までの残りに応じて“今の枠幅→親有効幅”の残差を割り当て
                float denom = Mathf.Max(0.0001f, playerStats.MaxHP_Cap - lastMaxHP);
                float unit = (parentMaxW - currentW) / denom;
                newWidth = currentW + unit * delta;
            }
            else
            {
                // 減少：起動時の枠幅を下限として対称に縮小
                float denomDown = Mathf.Max(0.0001f, lastMaxHP - baseMaxHP);
                float unitDown = (currentW - minFrameWidth) / denomDown;
                newWidth = currentW + unitDown * delta; // delta<0
            }

            // 即時反映（クランプ：下限=初期枠長、上限=親有効幅）
            ApplyFrameWidth(Mathf.Clamp(newWidth, minFrameWidth, parentMaxW));
            lastMaxHP = newMax;
        }

        // HP 比率も最新 Max で更新（幅は Update で反映）
        float statsMax = Mathf.Max(0.0001f, newMax);
        targetRatio = Mathf.Clamp01(playerHealth.hp / statsMax);
    }

    /* ======= 即時反映ユーティリティ ======= */

    void UpdateImmediate(float current, float statsMax)
    {
        float max = Mathf.Max(0.0001f, statsMax);
        viewRatio = targetRatio = Mathf.Clamp01(current / max);

        // 現枠幅に対する Fill 幅を即時適用
        float frameW = GetRectWidth(frameRect);
        float fillMaxW = Mathf.Max(0f, frameW - (horizontalMarginPx * 2f));
        SetFillWidth(fillMaxW * viewRatio);

        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    void ApplyFrameWidth(float frameW)
    {
        var pos = frameRect.anchoredPosition;
        frameRect.anchoredPosition = new Vector2(frameEdgeBufferPx, pos.y);

        // 枠の横幅
        frameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, frameW);

        // Fill の「最大幅」は毎フレーム計算するためここでは設定しないが、
        // 初期化直後だけは一旦反映しても良い
        float fillMaxW = Mathf.Max(0f, frameW - (horizontalMarginPx * 2f));
        SetFillWidth(Mathf.Clamp(fillMaxW * viewRatio, 0f, fillMaxW));

        // Fill 位置も枠の内側左端に寄せる
        var fpos = fillRect.anchoredPosition;
        fillRect.anchoredPosition = new Vector2(horizontalMarginPx, fpos.y);
    }

    void SetFillWidth(float w)
    {
        // 9-slice の中央だけが伸びる前提で、横幅を直接制御
        fillRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(0f, w));
    }

    float GetParentUsableWidth()
    {
        float pw = GetRectWidth(parentRect);
        // 親の「使用可能幅」= 親幅 − 2×バッファ
        return Mathf.Max(1f, pw - (frameEdgeBufferPx * 2f));
    }

    static float GetRectWidth(RectTransform rt)
    {
        if (!rt) return 0f;
        float w = rt.rect.width;
        if (w <= 1f) w = Mathf.Max(1f, rt.sizeDelta.x); // レイアウト前の保険
        return w;
    }

    
}
