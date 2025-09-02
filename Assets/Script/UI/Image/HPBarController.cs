using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    [Header("�Q��")]
    [SerializeField] Health playerHealth;   // �v���C���[�� Health
    [SerializeField] Image fillImage;      // Image Type = Filled, Horizontal, Origin=Left

    [Header("�F�E�A�j��")]
    [SerializeField] Color fullColor = new(0.0f, 0.55f, 0.70f); // �n
    [SerializeField] Color emptyColor = new(0.75f, 0.0f, 0.0f);  // �Ԍn
    [SerializeField, Min(0.1f)] float lerpSpeed = 5f;            // ��ԑ��x

    float targetRatio = 1f;   // �ړI�l
    float viewRatio = 1f;   // �\�����̒l

    void Start()
    {
        if (!playerHealth || !fillImage)
        {
            Debug.LogError("[HPBarController] �Q�Ƃ����ݒ�ł�"); enabled = false; return;
        }

        // �����l
        UpdateImmediate(playerHealth.hp, playerHealth.maxHP);

        // �C�x���g�w��
        playerHealth.onHpChanged.AddListener(UpdateTarget);
    }

    void Update()
    {
        // �X���[�Y�ɕ��
        viewRatio = Mathf.MoveTowards(viewRatio, targetRatio, lerpSpeed * Time.deltaTime);
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    // ----- �C�x���g�R�[���o�b�N -----
    void UpdateTarget(int current, int max)
    {
        targetRatio = (max == 0) ? 0f : (float)current / max;
    }

    // ----- �������f�p�iStart ���Ȃǁj -----
    void UpdateImmediate(int current, int max)
    {
        targetRatio = viewRatio = (max == 0) ? 0f : (float)current / max;
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }
}
