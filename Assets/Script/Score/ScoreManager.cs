using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>現在の総スコア</summary>
    public int TotalScore { get; private set; }

    /// <summary>スコアが変化したときに通知 (int 新スコア)</summary>
    public UnityEvent<int> onScoreChanged = new();

    void Awake()
    {
        // シーン内で重複していたら自身を破棄（永続化はしない）
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // ※ DontDestroyOnLoad は削除
    }

    /// <summary>スコアを追加</summary>
    public void AddScore(int value)
    {
        if (value <= 0) return;
        TotalScore += value;
        onScoreChanged.Invoke(TotalScore);
    }

    /// <summary>スコアをリセット</summary>
    public void ResetScore(int startValue = 0)
    {
        TotalScore = startValue;
        onScoreChanged.Invoke(TotalScore);
    }
}
