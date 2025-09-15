// Assets/Scripts/Boss/MidBossShooter.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// ���{�X�̎ˌ����X�N���v�g�őg�ސ��`�B
/// - ������ Phase �𑝂₵�Ă����ASetAll/SetShooter �� EnemyShooter �𐧌䂷��
/// - CSV �͎g��Ȃ��iStageFlow ���Ń{�X�s��CSV�̎ˌ��K�p�����Ȃ��j
/// </summary>
public class MidBossShooter : BossShooterBase
{
    [Header("Phase Durations")]
    public float phase0Duration = 6f;
    public float phase1Duration = 8f;

    protected override IEnumerator MainRoutine()
    {
        // ��FPhase0 �c �������ɒʏ�e�𓙊Ԋu
        {
            var dir = FireDirSpec.Fixed(Vector2.left);
            var timing = FireTimingSpec.Every(interval: 0.6f, delay: 0.3f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=normal;damage=2;size=1.0");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase0Duration);
            StopAllShooters();
        }

        // ��FPhase1 �c �v���C���[�_���̒ǔ��e����⍂�p�x
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Every(interval: 0.8f, delay: 0.0f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=homing;damage=3;size=1.1;speed=6.5;turn=720;near=2.0;break=5.0;maxTime=3.0");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase1Duration);
            StopAllShooters();
        }

        // �ȍ~�A�K�v�ɉ����� Phase ���p�������Ă�������
        // while(true) { �c } �ȂǂŃ��[�v���Ă�OK
    }
}
