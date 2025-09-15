// Assets/Scripts/Boss/FinalBossShooter.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 大ボスの射撃をスクリプトで組む雛形。
/// 後でフェーズ増加・パターン切替・無敵時間等を自由に追加してください。
/// </summary>
public class FinalBossShooter : BossShooterBase
{
    [Header("Phase Durations")]
    public float phase0Duration = 6f;
    public float phase1Duration = 10f;
    public float phase2Duration = 12f;

    protected override IEnumerator MainRoutine()
    {
        // Phase0: プレイヤー狙いビーム（短時間）
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Timeline(new System.Collections.Generic.List<float> { 0.2f, 1.0f, 1.8f }); // 3回だけ撃つ例
            var bullet = EnemyBulletSpec.FromCsv("bullet=beam;width=1.0;dps=12;lifetime=0.75");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase0Duration);
            StopAllShooters();
        }

        // Phase1: 左右固定方向（拡散などに差し替えても良い）
        {
            // 例として、0番シューター=左、1番=右 という運用
            var t = FireTimingSpec.Every(0.5f, 0.0f);
            var b = EnemyBulletSpec.FromCsv("bullet=normal;damage=2;size=1.0");

            SetShooter(0, FireDirSpec.Fixed(Vector2.left), t, b);
            SetShooter(1, FireDirSpec.Fixed(Vector2.right), t, b);
            yield return new WaitForSeconds(phase1Duration);
            StopAllShooters();
        }

        // Phase2: 高速追尾弾（重め）
        {
            var dir = FireDirSpec.AtPlayer();
            var timing = FireTimingSpec.Every(0.65f, 0.0f);
            var bullet = EnemyBulletSpec.FromCsv("bullet=homing;damage=4;size=1.2;speed=7.5;turn=900;near=1.8;break=6.0;maxTime=3.2");

            SetAll(dir, timing, bullet);
            yield return new WaitForSeconds(phase2Duration);
            StopAllShooters();
        }

        // ここでループする／次フェーズへ進む等、自由に拡張可能
    }
}
