// Assets/Scripts/Boss/BossMarker.cs
using UnityEngine;

[DisallowMultipleComponent]
public class BossMarker : MonoBehaviour
{
    public BossType bossType = BossType.Mid;

    void OnEnable()
    {
        BossManager.Instance?.BeginBoss(bossType);
    }

    // ボス専用スクリプトから明示的に呼んでもよい
    public void NotifyDefeated()
    {
        BossManager.Instance?.EndBoss(bossType);
        // ここで自分を破壊/非表示にするのは専用スクリプトの責務
    }

    void OnDestroy()
    {
        // 明示通知が無かった場合のフォールバック（プール運用なら注意）
        if (BossManager.Instance && BossManager.Instance.IsBossActive &&
            BossManager.Instance.ActiveBossType == bossType)
        {
            BossManager.Instance.EndBoss(bossType);
        }
    }
}
