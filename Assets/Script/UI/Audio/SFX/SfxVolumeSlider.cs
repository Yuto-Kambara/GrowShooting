// Assets/Scripts/Audio/SfxVolumeSlider.cs
using UnityEngine;
using UnityEngine.UI;
using GrowShooting.Audio;

[RequireComponent(typeof(Slider))]
public class SfxVolumeSlider : MonoBehaviour
{
    [Tooltip("起動時に保存値が無ければ使う既定値")]
    [Range(0f, 1f)] public float defaultValue = 0.8f;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
    }

    void OnEnable()
    {
        // 1) 現在のマスター音量 or 保存値で初期化
        float initial = defaultValue;
        if (SoundManager.Instance)
            initial = SoundManager.Instance.GetMasterVolume();
        else if (PlayerPrefs.HasKey(SoundManager.SfxPrefKey))
            initial = PlayerPrefs.GetFloat(SoundManager.SfxPrefKey, defaultValue);

        slider.SetValueWithoutNotify(initial);

        // 2) スライダー操作 → SoundManager に反映
        slider.onValueChanged.AddListener(OnSliderChanged);

        // 3) 他シーンで変更されたときも同期（イベント購読）
        if (SoundManager.Instance)
            SoundManager.Instance.OnMasterVolumeChanged += OnMasterChanged;
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (SoundManager.Instance)
            SoundManager.Instance.OnMasterVolumeChanged -= OnMasterChanged;
    }

    private void OnSliderChanged(float v)
    {
        if (SoundManager.Instance)
            SoundManager.Instance.SetMasterVolume(v);
        else
        {
            // 先にタイトルだけ配置している等、SoundManager未生成でも保存
            PlayerPrefs.SetFloat(SoundManager.SfxPrefKey, Mathf.Clamp01(v));
            PlayerPrefs.Save();
        }
    }

    private void OnMasterChanged(float v)
    {
        // 他シーンの操作で変わった場合もローカルUIを更新
        slider.SetValueWithoutNotify(v);
    }
}
