using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BGM マスター音量（AudioManager.SetVolume01）を操作するスライダー用。
/// シーンに複数あっても、表示時に AudioManager の現在値へ同期する。
/// </summary>
[RequireComponent(typeof(Slider))]
public class VolumeController : MonoBehaviour
{
    [Tooltip("AudioManager の初期値が 1.0（デフォルト）だった場合に限り、スライダーの現在値で初期化します。")]
    public bool overrideWhenDefault = true;

    [Tooltip("起動時に 0..1 をスライダーへ強制設定します。")]
    public bool forceSliderRange01 = true;

    private Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();
        if (forceSliderRange01)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            if (slider.wholeNumbers) slider.wholeNumbers = false;
        }

        var mgr = AudioManager.Instance;

        // 1) AudioManager 側がデフォルト(=1.0付近)なら、任意でスライダー値を採用
        if (mgr && overrideWhenDefault && Mathf.Abs(mgr.CurrentVolume - 1f) < 0.0001f && Mathf.Abs(slider.value - 1f) > 0.0001f)
        {
            mgr.SetVolume01(slider.value);
        }

        // 2) UIを AudioManager の現行値に合わせる（存在しない場合は現状維持）
        slider.SetValueWithoutNotify(mgr ? mgr.CurrentVolume : slider.value);
    }

    void OnEnable()
    {
        slider.onValueChanged.AddListener(Apply);

        // 再表示時（別シーン遷移直後など）にも最新値へ合わせる
        var mgr = AudioManager.Instance;
        if (mgr) slider.SetValueWithoutNotify(mgr.CurrentVolume);
    }

    void OnDisable()
    {
        slider.onValueChanged.RemoveListener(Apply);
    }

    private void Apply(float v)
    {
        if (AudioManager.Instance)
            AudioManager.Instance.SetVolume01(v);
    }
}
