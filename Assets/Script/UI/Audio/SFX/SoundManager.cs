// Assets/Scripts/Audio/SoundManager.cs
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GrowShooting.Audio
{
    /// <summary>
    /// ���ʉ����ꌳ�Ǘ�����V���O���g���B
    /// AudioSource ���v�[�����APlayOneShot �œ����Đ������C�ɂ����点��B
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
        [Range(0f, 1f)] public float masterVolume = 1.0f;       // �� �ǉ��F�SSFX�̃}�X�^�[

        [Header("Settings")]
        [SerializeField] private int poolSize = 10;

        // ----------------- Runtime -----------------
        private readonly Queue<AudioSource> _pool = new();
        public static SoundManager Instance { get; private set; }

        // UI�����w�ǂł���C�x���g�i�����V�[���̃X���C�_�[�����p�j
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

            // �ۑ��l�̃��[�h
            if (PlayerPrefs.HasKey(SfxPrefKey))
                masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxPrefKey, masterVolume));

            // �v�[������
            for (int i = 0; i < poolSize; i++)
            {
                var src = gameObject.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _pool.Enqueue(src);
            }

            // �N�����ɂ��ʒm�i���V�[���̃X���C�_�[�������p�j
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }

        /// <summary>UI�X���C�_�[������}�X�^�[���ʂ�ݒ�</summary>
        public void SetMasterVolume(float v)
        {
            masterVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat(SfxPrefKey, masterVolume);
            PlayerPrefs.Save();
            OnMasterVolumeChanged?.Invoke(masterVolume);
        }

        public float GetMasterVolume() => masterVolume;

        /// <summary>�w��̌��ʉ����Đ�����</summary>
        public void Play(SoundEffect effect)
        {
            var (clip, perClipVol) = GetClipAndVolume(effect);
            if (clip == null) return;

            var src = GetAvailableSource();
            // �� �}�X�^�[���ʂ��|����
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
