// Assets/Scripts/Boss/MidBossShooter.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 中ボスの射撃をスクリプトで組む雛形。
/// - ここに Phase を増やしていき、SetAll/SetShooter で EnemyShooter を制御する
/// - CSV は使わない（StageFlow 側でボス行はCSVの射撃適用をしない）
/// </summary>
public class MidBossShooter : BossShooterBase
{
    [Header("Phase Durations")]
    public float phase0Duration = 6f;
    public float phase1Duration = 8f;

    protected override IEnumerator MainRoutine()
    {
        // 例：Phase0 … 左向きに通常弾を等間隔
        {
            var dir = FireDirSpec.Fixed(Vector2.left);
            var timing = FireTimingSpec.Every(interval: 0.6f, delay: 0.3f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=normal;damage=2;size=1.0");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase0Duration);
            StopAllShooters();
        }

        // 例：Phase1 … プレイヤー狙いの追尾弾をやや高頻度
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Every(interval: 0.8f, delay: 0.0f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=homing;damage=3;size=1.1;speed=6.5;turn=720;near=2.0;break=5.0;maxTime=3.0");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase1Duration);
            StopAllShooters();
        }

        // 以降、必要に応じて Phase を継ぎ足してください
        // while(true) { … } などでループしてもOK
    }
}
