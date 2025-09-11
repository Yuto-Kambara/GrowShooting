// Assets/Scripts/Audio/SfxVolumeSlider.cs
using UnityEngine;
using UnityEngine.UI;
using GrowShooting.Audio;

[RequireComponent(typeof(Slider))]
public class SfxVolumeSlider : MonoBehaviour
{
    [Tooltip("�N�����ɕۑ��l��������Ύg������l")]
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
        // 1) ���݂̃}�X�^�[���� or �ۑ��l�ŏ�����
        float initial = defaultValue;
        if (SoundManager.Instance)
            initial = SoundManager.Instance.GetMasterVolume();
        else if (PlayerPrefs.HasKey(SoundManager.SfxPrefKey))
            initial = PlayerPrefs.GetFloat(SoundManager.SfxPrefKey, defaultValue);

        slider.SetValueWithoutNotify(initial);

        // 2) �X���C�_�[���� �� SoundManager �ɔ��f
        slider.onValueChanged.AddListener(OnSliderChanged);

        // 3) ���V�[���ŕύX���ꂽ�Ƃ��������i�C�x���g�w�ǁj
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
            // ��Ƀ^�C�g�������z�u���Ă��铙�ASoundManager�������ł��ۑ�
            PlayerPrefs.SetFloat(SoundManager.SfxPrefKey, Mathf.Clamp01(v));
            PlayerPrefs.Save();
        }
    }

    private void OnMasterChanged(float v)
    {
        // ���V�[���̑���ŕς�����ꍇ�����[�J��UI���X�V
        slider.SetValueWithoutNotify(v);
    }
}
