// Assets/Scripts/Audio/SoundManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace GrowShooting.Audio
{
    /// <summary>
    /// ���ʉ����ꌳ�Ǘ�����V���O���g���B
    /// AudioSource ���v�[�����APlayOneShot �œ����Đ������C�ɂ����点��B
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

            // AudioSource �v�[������
            for (int i = 0; i < poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Enqueue(src);
            }
        }

        /// <summary>�w��̌��ʉ����Đ�����</summary>
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
            // �v�[������擾�i�Đ����Ȃ疖���ɉ񂵂Ď���T���j
            foreach (var src in _pool)
            {
                if (!src.isPlaying) return src;
            }
            // �S���Đ����Ȃ�A�ŌÂ� AudioSource ���ė��p
            var recycled = _pool.Dequeue();
            recycled.Stop();
            _pool.Enqueue(recycled);
            return recycled;
        }
    }
}
