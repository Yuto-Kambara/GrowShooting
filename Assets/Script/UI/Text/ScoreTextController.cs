using UnityEngine;
using TMPro;

public class ScoreTextController: MonoBehaviour
{
    [Header("参照（HP は別スクリプト）")]
    [SerializeField] TextMeshProUGUI scoreTMP;

    void Start()
    {
        // スコア初期化
        int init = ScoreManager.Instance ? ScoreManager.Instance.TotalScore : 0;
        UpdateScore(init);

        // 変更イベント購読
        if (ScoreManager.Instance)
            ScoreManager.Instance.onScoreChanged.AddListener(UpdateScore);
    }

    void UpdateScore(int newScore)
    {
        scoreTMP.text = newScore.ToString("D8"); // 00000000 フォーマット
    }
}
