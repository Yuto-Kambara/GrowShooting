using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("�ړ��p�����[�^")]
    public float baseSpeed = 6f;
    public Vector2 maxBounds = new(8.5f, 4.5f);

    [Header("UI �p�l���Ɨ]��")]
    public RectTransform uiPanel;          // ���� UI �̊O�g
    public float bottomPadding = 0.3f;     // UI ��[���痣�������iworld �P�ʁj

    float minYWorld;                       // �v�Z��̉��� Y
    Rigidbody2D rb;
    Vector2 input;
    [HideInInspector] public float speedMul = 1f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;

        if (!uiPanel)
        {
            Debug.LogWarning("[PlayerController] uiPanel ���ݒ� �� ��  -4.5 �ŌŒ�");
            minYWorld = -4.5f;
            return;
        }

        //--- �@ UI ��[���X�N���[�����W�Ŏ擾 --------------------------
        Vector3[] corners = new Vector3[4];
        uiPanel.GetWorldCorners(corners);          // Overlay �ł��u���[���h���X�N���[���v
        float uiTopScreenY = corners[1].y;         // ����R�[�i�[

        //--- �A �X�N���[�� �� ���[���h�ϊ� ------------------------------
        Camera cam = Camera.main;
        Vector3 worldTop = cam.ScreenToWorldPoint(new Vector3(0, uiTopScreenY, cam.nearClipPlane));

        //--- �B �p�f�B���O�𑫂��čŏI���� ------------------------------
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
