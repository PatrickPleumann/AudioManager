using UnityEngine;
using UnityEngine.UI;

using AudioFramework.Core;

public class CategoryVolumeSliderBinding : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private AudioCategory category;

    private void OnEnable()
    {
        slider.onValueChanged.AddListener(ApplyVolume);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(ApplyVolume);
    }

    private void Start()
    {
        slider.SetValueWithoutNotify(AudioManagerDynamic.GetCategoryVolume(category));
    }

    private void ApplyVolume(float newVolume)
    {
        AudioManagerDynamic.SetCategoryVolume(category, newVolume);
    }
}
