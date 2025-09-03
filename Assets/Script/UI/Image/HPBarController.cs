using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Health.hp / maxHP �� float �ɂȂ����ł� HP �o�[����
/// </summary>
public class HPBarController : MonoBehaviour
{
    [Header("�Q�ƃI�u�W�F�N�g")]
    [SerializeField] Health playerHealth;   // float �� Health
    [SerializeField] Image fillImage;      // Type = Filled / Horizontal / Left

    [Header("�F�ƃA�j��")]
    [SerializeField] Color fullColor = new(0.00f, 0.55f, 0.70f);
    [SerializeField] Color emptyColor = new(0.75f, 0.00f, 0.00f);
    [SerializeField, Min(0.1f)] float lerpSpeed = 5f;

    float targetRatio = 1f;   // �ŐV�l
    float viewRatio = 1f;   // �\���l�i��ԗp�j

    /*------------------------------*/
    void Start()
    {
        if (!playerHealth || !fillImage)
        {
            Debug.LogError("[HPBarController] �Q�Ƃ����ݒ�"); enabled = false; return;
        }

        // �����l�𔽉f
        UpdateImmediate(playerHealth.hp, playerHealth.maxHP);

        // HP �ύX�C�x���g���w�ǁifloat,float�j
        playerHealth.onHpChanged.AddListener(UpdateTarget);
    }

    /*------------------------------*/
    void Update()
    {
        // ��Ԃ��Ċ��炩�ɕ\��
        viewRatio = Mathf.MoveTowards(viewRatio, targetRatio, lerpSpeed * Time.deltaTime);
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }

    /*------------------------------*/
    // HP ���ς�������ɌĂ΂��R�[���o�b�N
    void UpdateTarget(float current, float max)
    {
        targetRatio = (max <= 0f) ? 0f : current / max;
    }

    // Start ���ȂǊ��S�ɑ����f���������p
    void UpdateImmediate(float current, float max)
    {
        targetRatio = viewRatio = (max <= 0f) ? 0f : current / max;
        fillImage.fillAmount = viewRatio;
        fillImage.color = Color.Lerp(emptyColor, fullColor, viewRatio);
    }
}
