// Assets/Scripts/Audio/SoundManager.cs
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GrowShooting.Audio
{
    /// <summary>
    /// 効果音を一元管理するシングルトン。
    /// AudioSource をプールし、PlayOneShot で同時再生数を気にせず鳴らせる。
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public const string SfxPrefKey = "Audio.SFX.Master";   // PlayerPrefs key

        // ----------------- Inspector -----------------
        [Header("Audio Clips")]
        public AudioClip playerShotClip;
        public AudioClip enemyDownClip;
        public AudioClip playerHitClip;

        [Header("Per-Clip Volumes (0-1)")]
        [Range(0f, 1f)] public float playerShotVolume = 0.8f;
        [Range(0f, 1f)] public float enemyDownVolume = 1.0f;
        [Range(0f, 1f)] public float playerHitVolume = 0.9f;

        [Header("Master SFX Volume (0-1)")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;       // ★ 追加：全SFXのマスター

        [Header("Settings")]
        [SerializeField] private int poolSize = 10;

        // ----------------- Runtime -----------------
        private readonly Queue<AudioSource> _pool = new();
        public static SoundManager Instance { get; private set; }

        // UI側が購読できるイベント（複数シーンのスライダー同期用）
        public event Action<float> OnMasterVolumeChanged;

        private void Awake()
        {
            // Singleton
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // 保存値のロード
            if (PlayerPrefs.HasKey(SfxPrefKey))
                masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxPrefKey, masterVolume));

            // プール生成
            for (int i = 0; i < poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Enqueue(src);
            }

            // 起動時にも通知（他シーンのスライダー即同期用）
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }

        /// <summary>UIスライダー等からマスター音量を設定</summary>
        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(SfxPrefKey, masterVolume);
            PlayerPrefs.Save();
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }

        public float GetMasterVolume() => masterVolume;

        /// <summary>指定の効果音を再生する</summary>
        public void Play(SoundEffect effect)
        {
            var (clip, perClipVol) = GetClipAndVolume(effect);
            if (clip == null) return;

            var src = GetAvailableSource();
            // ★ マスター音量を掛ける
            float finalVol = Mathf.Clamp01(perClipVol) * Mathf.Clamp01(masterVolume);
            src.PlayOneShot(clip, finalVol);
        }

        // --- Helpers -----------------------------------------------------------------

        private (AudioClip clip, float volume) GetClipAndVolume(SoundEffect effect)
        {
            return effect switch
            {
                SoundEffect.PlayerShot => (playerShotClip, playerShotVolume),
                SoundEffect.EnemyDown => (enemyDownClip, enemyDownVolume),
                SoundEffect.PlayerHit => (playerHitClip, playerHitVolume),
                _ => (null, 0f),
            };
        }

        private AudioSource GetAvailableSource()
        {
            foreach (var src in _pool)
                if (!src.isPlaying) return src;

            var recycled = _pool.Dequeue();
            recycled.Stop();
            _pool.Enqueue(recycled);
            return recycled;
        }
    }
}
