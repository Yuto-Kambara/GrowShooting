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

    // �{�X��p�X�N���v�g���疾���I�ɌĂ�ł��悢
    public void NotifyDefeated()
    {
        BossManager.Instance?.EndBoss(bossType);
        // �����Ŏ�����j��/��\���ɂ���̂͐�p�X�N���v�g�̐Ӗ�
    }

    void OnDestroy()
    {
        // �����ʒm�����������ꍇ�̃t�H�[���o�b�N�i�v�[���^�p�Ȃ璍�Ӂj
        if (BossManager.Instance && BossManager.Instance.IsBossActive &&
            BossManager.Instance.ActiveBossType == bossType)
        {
            BossManager.Instance.EndBoss(bossType);
        }
    }
}
