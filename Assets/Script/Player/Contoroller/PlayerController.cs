using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動パラメータ")]
    public float baseSpeed = 6f;
    public Vector2 maxBounds = new(8.5f, 4.5f);

    [Header("UI パネルと余白")]
    public RectTransform uiPanel;          // 下部 UI の外枠
    public float bottomPadding = 0.3f;     // UI 上端から離す距離（world 単位）

    float minYWorld;                       // 計算後の下限 Y
    Rigidbody2D rb;
    Vector2 input;
    [HideInInspector] public float speedMul = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        if (!uiPanel)
        {
            Debug.LogWarning("[PlayerController] uiPanel 未設定 → 仮  -4.5 で固定");
            minYWorld = -4.5f;
            return;
        }

        //--- ① UI 上端をスクリーン座標で取得 --------------------------
        Vector3[] corners = new Vector3[4];
        uiPanel.GetWorldCorners(corners);          // Overlay でも「ワールド≒スクリーン」
        float uiTopScreenY = corners[1].y;         // 左上コーナー

        //--- ② スクリーン → ワールド変換 ------------------------------
        Camera cam = Camera.main;
        Vector3 worldTop = cam.ScreenToWorldPoint(new Vector3(0, uiTopScreenY, cam.nearClipPlane));

        //--- ③ パディングを足して最終下限 ------------------------------
        minYWorld = worldTop.y + bottomPadding;
    }

    void Update()
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    void FixedUpdate()
    {
        Vector2 v = input * baseSpeed * speedMul;
        Vector2 p = rb.position + v * Time.fixedDeltaTime;

        p.x = Mathf.Clamp(p.x, -maxBounds.x, maxBounds.x);
        p.y = Mathf.Clamp(p.y, minYWorld, maxBounds.y);

        rb.MovePosition(p);
    }
}
