// Assets/Scripts/Boss/FinalBossShooter.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// ��{�X�̎ˌ����X�N���v�g�őg�ސ��`�B
/// ��Ńt�F�[�Y�����E�p�^�[���ؑցE���G���ԓ������R�ɒǉ����Ă��������B
/// </summary>
public class FinalBossShooter : BossShooterBase
{
    [Header("Phase Durations")]
    public float phase0Duration = 6f;
    public float phase1Duration = 10f;
    public float phase2Duration = 12f;

    protected override IEnumerator MainRoutine()
    {
        // Phase0: �v���C���[�_���r�[���i�Z���ԁj
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Timeline(new System.Collections.Generic.List<float> { 0.2f, 1.0f, 1.8f }); // 3�񂾂�����
            var bullet = EnemyBulletSpec.FromCsv("bullet=beam;width=1.0;dps=12;lifetime=0.75");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase0Duration);
            StopAllShooters();
        }

        // Phase1: ���E�Œ�����i�g�U�Ȃǂɍ����ւ��Ă��ǂ��j
        {
            // ��Ƃ��āA0�ԃV���[�^�[=���A1��=�E �Ƃ����^�p
            var t = FireTimingSpec.Every(0.5f, 0.0f);
            var b = EnemyBulletSpec.FromCsv("bullet=normal;damage=2;size=1.0");

            SetShooter(0, FireDirSpec.Fixed(Vector2.left), t, b);
            SetShooter(1, FireDirSpec.Fixed(Vector2.right), t, b);
            yield return new WaitForSeconds(phase1Duration);
            StopAllShooters();
        }

        // Phase2: �����ǔ��e�i�d�߁j
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Every(0.65f, 0.0f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=homing;damage=4;size=1.2;speed=7.5;turn=900;near=1.8;break=6.0;maxTime=3.2");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase2Duration);
            StopAllShooters();
        }

        // �����Ń��[�v����^���t�F�[�Y�֐i�ޓ��A���R�Ɋg���\
    }
}
