// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";

    [Header("BGM Clips")]
    public AudioClip titleBgm;
    public AudioClip stageBgm;
    public AudioClip midBossBgm;
    public AudioClip finalBossBgm;
    public AudioClip clearBgm;

    [Header("BGM Settings")]
    [Range(0f, 5f)] public float crossfadeTime = 0.8f;
    [SerializeField] private AudioMixerGroup bgmGroup;

    [Header("Scene Names")]
    public string titleSceneName = "Title";
    public string playSceneName = "PlayScene";

    public float CurrentVolume { get; private set; } = 1f;

    private AudioSource _a, _b;
    private AudioSource _fadeIn, _fadeOut;
    private bool _fading;
    private float _fadeT;
    private bool _activeIsA;
    private float _currentFadeTime;

    private bool _subscribedBossEvents = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (masterMixer && masterMixer.GetFloat(masterVolumeParam, out float db))
            CurrentVolume = Mathf.Pow(10f, db / 20f);

        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _a, _b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.outputAudioMixerGroup = bgmGroup ? bgmGroup : null;
            s.volume = 0f;
        }
        _activeIsA = false;
        _currentFadeTime = crossfadeTime;

        // シーンロードで自動切替
        SceneManager.sceneLoaded += OnSceneLoaded;

        // ★ 起動直後の現在シーンに対しても即チェック（タイトルから起動時の取りこぼし防止）
        TryAutoPlayFor(SceneManager.GetActiveScene());
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnsubscribeBossEvents();
    }

    void Update()
    {
        if (!_fading) return;

        _fadeT += Time.deltaTime / Mathf.Max(0.01f, _currentFadeTime);
        float t = Mathf.Clamp01(_fadeT);

        if (_fadeIn) _fadeIn.volume = t;
        if (_fadeOut) _fadeOut.volume = 1f - t;

        if (t >= 1f)
        {
            if (_fadeOut && _fadeOut.isPlaying) _fadeOut.Stop();
            if (_fadeIn) _fadeIn.volume = 1f;
            _fading = false;
            if (_fadeIn) _activeIsA = (_fadeIn == _a);
            _fadeIn = _fadeOut = null;
        }
    }

    // ===== シーン連動 =====
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => TryAutoPlayFor(scene);

    void TryAutoPlayFor(Scene scene)
    {
        // BossManager イベント購読は Play シーンでのみ行う
        if (scene.name == playSceneName) TrySubscribeBossEvents();
        else UnsubscribeBossEvents();

        if (scene.name == titleSceneName)
        {
            PlayTitleBgm();     // ★ タイトルに入ったら必ずタイトルBGM
        }
        else if (scene.name == playSceneName)
        {
            PlayStageBgm();     // プレイに入ったらステージBGM
        }
        else
        {
            StopBgm(0.5f);      // その他のシーンでは停止（必要に応じて変更可）
        }
    }

    void TrySubscribeBossEvents()
    {
        if (_subscribedBossEvents) return;
        var bm = BossManager.Instance ?? FindFirstObjectByType<BossManager>();
        if (!bm) return;
        bm.OnBossStarted.AddListener(OnBossStarted);
        bm.OnBossDefeated.AddListener(OnBossDefeated);
        _subscribedBossEvents = true;
    }

    void UnsubscribeBossEvents()
    {
        if (!_subscribedBossEvents) return;
        var bm = BossManager.Instance ?? FindFirstObjectByType<BossManager>();
        if (bm)
        {
            bm.OnBossStarted.RemoveListener(OnBossStarted);
            bm.OnBossDefeated.RemoveListener(OnBossDefeated);
        }
        _subscribedBossEvents = false;
    }

    void OnBossStarted(BossType type)
    {
        if (type == BossType.Mid) PlayMidBossBgm();
        if (type == BossType.Final) PlayFinalBossBgm();
    }

    void OnBossDefeated(BossType type)
    {
        if (type == BossType.Final) PlayClearBgm();
        else PlayStageBgm();
    }

    // ===== 公開API =====
    public void SetVolume01(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        if (masterMixer)
            masterMixer.SetFloat(masterVolumeParam, Mathf.Log10(v) * 20f);
        CurrentVolume = v;
    }

    public void PlayTitleBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(titleBgm);
    }
    public void PlayStageBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(stageBgm);
    }
    public void PlayMidBossBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(midBossBgm);
    }
    public void PlayFinalBossBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(finalBossBgm);
    }
    public void PlayClearBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(clearBgm);
    }
    public void StopBgm(float fadeTime = 0.5f)
    {
        _currentFadeTime = (fadeTime > 0f) ? fadeTime : crossfadeTime;
        CrossfadeTo(null);
    }

    // ===== 内部処理 =====
    void CrossfadeTo(AudioClip clip)
    {
        AudioSource current = _activeIsA ? _a : _b;
        AudioSource other = _activeIsA ? _b : _a;

        if (clip != null && current.clip == clip && current.isPlaying)
        {
            _fading = false;
            if (current.volume < 1f) current.volume = 1f;
            return;
        }

        if (clip == null)
        {
            _fadeIn = null;
            _fadeOut = current.isPlaying ? current : other;
            if (_fadeOut == null) return;
            _fadeT = 0f;
            _fading = true;
            return;
        }

        AudioSource incoming = (current == _a) ? _b : _a;
        incoming.clip = clip;
        incoming.time = 0f;
        incoming.volume = 0f;
        incoming.loop = true;
        incoming.Play();

        _fadeIn = incoming;
        _fadeOut = current.isPlaying ? current : null;
        _fadeT = 0f;
        _fading = true;
    }
}
