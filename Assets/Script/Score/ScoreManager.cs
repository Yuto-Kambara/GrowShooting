using UnityEngine;
using UnityEngine.Events;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    /// <summary>���݂̑��X�R�A</summary>
    public int TotalScore { get; private set; }

    /// <summary>�X�R�A���ω������Ƃ��ɒʒm (int �V�X�R�A)</summary>
    public UnityEvent<int> onScoreChanged = new();

    void Awake()
    {
        // �V�[�����ŏd�����Ă����玩�g��j���i�i�����͂��Ȃ��j
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // �� DontDestroyOnLoad �͍폜
    }

    /// <summary>�X�R�A��ǉ�</summary>
    public void AddScore(int value)
    {
        if (value <= 0) return;
        TotalScore += value;
        onScoreChanged.Invoke(TotalScore);
    }

    /// <summary>�X�R�A�����Z�b�g</summary>
    public void ResetScore(int startValue = 0)
    {
        TotalScore = startValue;
        onScoreChanged.Invoke(TotalScore);
    }
}
