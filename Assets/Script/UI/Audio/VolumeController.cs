using UnityEngine;
using UnityEngine.UI;

/// スライダー側に直接付けるバージョン
[RequireComponent(typeof(Slider))]
public class VolumeController : MonoBehaviour
{
    Slider slider;

    void Awake()
    {
        slider = GetComponent<Slider>();              // 自動取得
        var mgr = AudioManager.Instance;

        // AudioManager がまだ未設定なら、このスライダーの値を採用
        if (mgr && mgr.CurrentVolume > 0.999f)        // ← 1 なら未初期とみなす
            mgr.SetVolume01(slider.value);

        // UI 表示を AudioManager の現行値に合わせる
        slider.SetValueWithoutNotify(mgr ? mgr.CurrentVolume : slider.value);
    }

    void OnEnable() => slider.onValueChanged.AddListener(Apply);
    void OnDisable() => slider.onValueChanged.RemoveListener(Apply);

    void Apply(float v) => AudioManager.Instance.SetVolume01(v);
}
