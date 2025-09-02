using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer")]
    [SerializeField] AudioMixer masterMixer;
    const string PARAM = "MasterVolume";

    public float CurrentVolume { get; private set; } = 1f;   // 0.0001–1

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Mixer 初期値 → CurrentVolume へ反映
        if (masterMixer.GetFloat(PARAM, out float db))
            CurrentVolume = Mathf.Pow(10f, db / 20f);
    }

    /// <summary>スライダーから呼ばれる</summary>
    public void SetVolume01(float v)
    {
        v = Mathf.Clamp(v, 0.0001f, 1f);
        masterMixer.SetFloat(PARAM, Mathf.Log10(v) * 20f);
        CurrentVolume = v;
    }
}
