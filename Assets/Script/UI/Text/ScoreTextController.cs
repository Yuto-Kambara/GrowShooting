using UnityEngine;
using TMPro;

public class ScoreTextController: MonoBehaviour
{
    [Header("�Q�ƁiHP �͕ʃX�N���v�g�j")]
    [SerializeField] TextMeshProUGUI scoreTMP;

    void Start()
    {
        // �X�R�A������
        int init = ScoreManager.Instance ? ScoreManager.Instance.TotalScore : 0;
        UpdateScore(init);

        // �ύX�C�x���g�w��
        if (ScoreManager.Instance)
            ScoreManager.Instance.onScoreChanged.AddListener(UpdateScore);
    }

    void UpdateScore(int newScore)
    {
        scoreTMP.text = newScore.ToString("D8"); // 00000000 �t�H�[�}�b�g
    }
}
