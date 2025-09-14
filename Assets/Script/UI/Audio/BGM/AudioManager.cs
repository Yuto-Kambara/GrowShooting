// Assets/Scripts/Audio/AudioManager.cs
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// BGM のクロスフェード再生＋マスター音量（Mixer）の制御。
/// ・シングルトン（DontDestroyOnLoad）
/// ・2枚の AudioSource を用いた安全なクロスフェード
/// ・Stage/MidBoss/FinalBoss 用のBGMスイッチAPI
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] private AudioMixer masterMixer;
    [Tooltip("AudioMixer 側に Exposed したパラメータ名（dB）")]
    [SerializeField] private string masterVolumeParam = "MasterVolume";

    [Header("BGM Clips")]
    [Tooltip("通常ステージBGM")]
    public AudioClip stageBgm;
    [Tooltip("中ボスBGM")]
    public AudioClip midBossBgm;
    [Tooltip("大ボスBGM")]
    public AudioClip finalBossBgm;

    [Header("BGM Settings")]
    [Tooltip("曲切替・停止時の基本クロスフェード時間（秒）")]
    [Range(0f, 5f)] public float crossfadeTime = 0.8f;
    [Tooltip("BGM 出力用の MixerGroup（任意）")]
    [SerializeField] private AudioMixerGroup bgmGroup;

    /// <summary>現在のマスター音量（0.0001〜1.0）。UI などから参照用。</summary>
    public float CurrentVolume { get; private set; } = 1f;

    // --- 内部（クロスフェード） ---
    private AudioSource _a, _b;       // 2枚看板
    private AudioSource _fadeIn;      // 今回フェードインする方
    private AudioSource _fadeOut;     // 今回フェードアウトする方
    private bool _fading;
    private float _fadeT;             // 0..1
    private bool _activeIsA;          // 現在“前面”で鳴っているのが _a なら true
    private float _currentFadeTime;   // 今回のフェード時間（呼び出しごとに上書き）

    private void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Mixer 初期値 → CurrentVolume へ反映
        if (masterMixer && masterMixer.GetFloat(masterVolumeParam, out float db))
            CurrentVolume = Mathf.Pow(10f, db / 20f);

        // BGM用 AudioSource を2つ用意
        _a = gameObject.AddComponent<AudioSource>();
        _b = gameObject.AddComponent<AudioSource>();
        foreach (var s in new[] { _a, _b })
        {
            s.loop = true;
            s.playOnAwake = false;
            s.outputAudioMixerGroup = bgmGroup ? bgmGroup : null;
            s.volume = 0f;
        }

        _activeIsA = false;      // 起動直後はどちらも未再生
        _currentFadeTime = crossfadeTime;
    }

    private void Update()
    {
        if (!_fading) return;

        _fadeT += Time.deltaTime / Mathf.Max(0.01f, _currentFadeTime);
        float t = Mathf.Clamp01(_fadeT);

        if (_fadeIn) _fadeIn.volume = t;
        if (_fadeOut) _fadeOut.volume = 1f - t;

        if (t >= 1f)
        {
            if (_fadeOut && _fadeOut.isPlaying) _fadeOut.Stop();
            if (_fadeIn) _fadeIn.volume = 1f; // 念のため 1 に揃える
            _fading = false;

            // 現在アクティブ更新（停止フェード時は変更なし）
            if (_fadeIn) _activeIsA = (_fadeIn == _a);

            _fadeIn = _fadeOut = null;
        }
    }

    // ===================== 公開API =====================

    /// <summary>UIスライダーなどから呼ぶ。0..1（内部でdBへログ変換）</summary>
    public void SetVolume01(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        if (masterMixer)
            masterMixer.SetFloat(masterVolumeParam, Mathf.Log10(v) * 20f);
        CurrentVolume = v;
    }

    /// <summary>通常ステージBGMへクロスフェード</summary>
    public void PlayStageBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(stageBgm);
    }

    /// <summary>中ボスBGMへクロスフェード</summary>
    public void PlayMidBossBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(midBossBgm);
    }

    /// <summary>大ボスBGMへクロスフェード</summary>
    public void PlayFinalBossBgm()
    {
        _currentFadeTime = crossfadeTime;
        CrossfadeTo(finalBossBgm);
    }

    /// <summary>フェードアウトして停止</summary>
    /// <param name="fadeTime">0以下なら既定 crossfadeTime を使用</param>
    public void StopBgm(float fadeTime = 0.5f)
    {
        _currentFadeTime = (fadeTime > 0f) ? fadeTime : crossfadeTime;
        CrossfadeTo(null); // フェードアウトのみ
    }

    // ===================== 内部処理 =====================

    /// <summary>
    /// clip==null なら停止へフェードアウト。clip!=null ならその曲へクロスフェード。
    /// </summary>
    private void CrossfadeTo(AudioClip clip)
    {
        AudioSource current = _activeIsA ? _a : _b; // 現在“前面”
        AudioSource other = _activeIsA ? _b : _a; // もう一方

        // 同一曲が既に current で鳴っているなら何もしない
        if (clip != null && current.clip == clip && current.isPlaying)
        {
            _fading = false;
            if (current.volume < 1f) current.volume = 1f;
            return;
        }

        if (clip == null)
        {
            // 停止フェード：どちらか鳴っている方を 1→0 に
            _fadeIn = null;
            _fadeOut = current.isPlaying ? current : other;
            if (_fadeOut == null) return; // そもそも何も鳴っていない
            _fadeT = 0f;
            _fading = true;
            return;
        }

        // 新しい曲は「今アクティブでない方」にセットして 0→1 に
        AudioSource incoming = (current == _a) ? _b : _a;
        incoming.clip = clip;
        incoming.time = 0f;
        incoming.volume = 0f;
        incoming.Play();

        _fadeIn = incoming;
        _fadeOut = current.isPlaying ? current : null; // 無音→有音の初回はフェードアウト相手なし
        _fadeT = 0f;
        _fading = true;

        // ★ ここで _activeIsA は切り替えない（フェード完了時に確定）
    }
}
