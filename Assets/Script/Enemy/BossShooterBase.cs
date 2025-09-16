// Assets/Scripts/Boss/BossShooterBase.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// �{�X�p�̎ˌ����B���̃N���X���p�����Ċe�{�X�̃p�^�[�����L�q����B
/// - �q�K�w�ɂ��� EnemyShooter ���������W
/// - �܂Ƃ߂āu�����E�^�C�~���O�E�e�X�y�b�N�v��K�p���郆�[�e�B���e�B���
/// - �L��/������ꎞ��~������\
/// </summary>
[DisallowMultipleComponent]
public abstract class BossShooterBase : MonoBehaviour
{
    [Header("Collect Shooters From Children")]
    public bool autoCollectOnEnable = true;

    protected EnemyShooter[] shooters;

    protected virtual void OnEnable()
    {

        if (autoCollectOnEnable || shooters == null || shooters.Length == 0)
            shooters = GetComponentsInChildren<EnemyShooter>(true);

        // �J�n���͈�U��~���Ă����i�h����ŕK�v�ɉ����čĐݒ�j
        PauseAll(true);
        StopAllCoroutines();

        // �e�{�X�ŗL�̃R���[�`���J�n�i�h���N���X�Ŏ����j
        StartCoroutine(MainRoutine());

    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        PauseAll(true);
    }

    /// <summary>���C���̎ˌ����[�`���i�h���N���X�Ŏ����j</summary>
    protected abstract IEnumerator MainRoutine();

    // -------- ���[�e�B���e�B --------

    /// <summary>�S EnemyShooter ��L��/�������iUpdate ��~�j</summary>
    protected void PauseAll(bool pause)
    {
        if (shooters == null) return;
        foreach (var s in shooters) if (s) s.enabled = !pause;
    }

    /// <summary>�S EnemyShooter ���ꊇ�ݒ�i�����E�^�C�~���O�E�e�X�y�b�N�j</summary>
    protected void SetAll(FireDirSpec dir, FireTimingSpec timing, EnemyBulletSpec bullet)
    {
        if (shooters == null) return;
        foreach (var s in shooters)
        {
            if (!s) continue;
            s.ApplyFireDirection(dir);
            s.ApplyFireTiming(timing);
            s.ApplyBulletSpec(bullet);
            s.enabled = true;
        }
    }

    /// <summary>�C���f�b�N�X�Ōʂ� EnemyShooter ��ݒ�i���݂��Ȃ���Ή������Ȃ��j</summary>
    protected void SetShooter(int index, FireDirSpec dir, FireTimingSpec timing, EnemyBulletSpec bullet)
    {
        if (shooters == null || index < 0 || index >= shooters.Length) return;
        var s = shooters[index];
        if (!s) return;
        s.ApplyFireDirection(dir);
        s.ApplyFireTiming(timing);
        s.ApplyBulletSpec(bullet);
        s.enabled = true;
    }

    /// <summary>�S EnemyShooter ���~�i�R���|�[�l���g�������j</summary>
    protected void StopAllShooters()
    {
        if (shooters == null) return;
        foreach (var s in shooters) if (s) s.enabled = false;
    }
}
