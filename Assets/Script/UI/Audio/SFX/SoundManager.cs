// Assets/Scripts/Audio/SoundManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace GrowShooting.Audio
{
    /// <summary>
    /// 効果音を一元管理するシングルトン。
    /// AudioSource をプールし、PlayOneShot で同時再生数を気にせず鳴らせる。
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        // ----------------- Inspector -----------------
        [Header("Audio Clips")]
        public AudioClip playerShotClip;
        public AudioClip enemyDownClip;
        public AudioClip playerHitClip;

        [Header("Volumes (0-1)")]
        [Range(0f, 1f)] public float playerShotVolume = 0.8f;
        [Range(0f, 1f)] public float enemyDownVolume = 1.0f;
        [Range(0f, 1f)] public float playerHitVolume = 0.9f;

        [Header("Settings")]
        [SerializeField] private int poolSize = 10;

        // ----------------- Runtime -----------------
        private readonly Queue<AudioSource> _pool = new();
        public static SoundManager Instance { get; private set; }

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

            // AudioSource プール生成
            for (int i = 0; i < poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Enqueue(src);
            }
        }

        /// <summary>指定の効果音を再生する</summary>
        public void Play(SoundEffect effect)
        {
            var (clip, vol) = GetClipAndVolume(effect);
            if (clip == null) return;

            var src = GetAvailableSource();
            src.PlayOneShot(clip, vol);
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
            // プールから取得（再生中なら末尾に回して次を探す）
            foreach (var src in _pool)
            {
                if (!src.isPlaying) return src;
            }
            // 全部再生中なら、最古の AudioSource を再利用
            var recycled = _pool.Dequeue();
            recycled.Stop();
            _pool.Enqueue(recycled);
            return recycled;
        }
    }
}
