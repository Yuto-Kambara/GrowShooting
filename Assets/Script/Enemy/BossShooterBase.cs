// Assets/Scripts/Boss/BossShooterBase.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// ボス用の射撃基底。このクラスを継承して各ボスのパターンを記述する。
/// - 子階層にある EnemyShooter を自動収集
/// - まとめて「方向・タイミング・弾スペック」を適用するユーティリティを提供
/// - 有効/無効や一時停止も制御可能
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

        // 開始時は一旦停止しておく（派生先で必要に応じて再設定）
        PauseAll(true);
        StopAllCoroutines();

        // 各ボス固有のコルーチン開始（派生クラスで実装）
        StartCoroutine(MainRoutine());

    }

    protected virtual void OnDisable()
    {
        StopAllCoroutines();
        PauseAll(true);
    }

    /// <summary>メインの射撃ルーチン（派生クラスで実装）</summary>
    protected abstract IEnumerator MainRoutine();

    // -------- ユーティリティ --------

    /// <summary>全 EnemyShooter を有効/無効化（Update 停止）</summary>
    protected void PauseAll(bool pause)
    {
        if (shooters == null) return;
        foreach (var s in shooters) if (s) s.enabled = !pause;
    }

    /// <summary>全 EnemyShooter を一括設定（方向・タイミング・弾スペック）</summary>
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

    /// <summary>インデックスで個別の EnemyShooter を設定（存在しなければ何もしない）</summary>
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

    /// <summary>全 EnemyShooter を停止（コンポーネント無効化）</summary>
    protected void StopAllShooters()
    {
        if (shooters == null) return;
        foreach (var s in shooters) if (s) s.enabled = false;
    }
}
