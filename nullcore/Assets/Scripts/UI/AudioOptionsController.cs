using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Nullcore.Core;

namespace Nullcore.UI
{
    // The two volume sliders in the options menu. Changes apply and are saved straight away -
    // they are not part of the keybindings Save / Discard flow.
    public class AudioOptionsController : MonoBehaviour
    {
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text sfxValueText;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private TMP_Text musicValueText;

        private void OnEnable()
        {
            Bind(sfxSlider, sfxValueText, GameAudioSettings.SfxVolume, OnSfxChanged);
            Bind(musicSlider, musicValueText, GameAudioSettings.MusicVolume, OnMusicChanged);
        }

        private void OnDisable()
        {
            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
            }
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            }
        }

        private static void Bind(Slider slider, TMP_Text valueText, float current, UnityEngine.Events.UnityAction<float> onChanged)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(current);
            slider.onValueChanged.RemoveListener(onChanged);
            slider.onValueChanged.AddListener(onChanged);
            SetLabel(valueText, current);
        }

        private void OnSfxChanged(float value)
        {
            GameAudioSettings.SetSfxVolume(value);
            SetLabel(sfxValueText, value);
        }

        private void OnMusicChanged(float value)
        {
            GameAudioSettings.SetMusicVolume(value);
            SetLabel(musicValueText, value);
        }

        private static void SetLabel(TMP_Text text, float value)
        {
            if (text != null)
            {
                text.text = Mathf.RoundToInt(value * 100f) + "%";
            }
        }
    }
}
