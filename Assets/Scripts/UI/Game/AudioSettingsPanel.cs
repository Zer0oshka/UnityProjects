using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS.UI
{
    public static class AudioSettingsPanel
    {
        const string MasterVolumeKey = "Audio.MasterVolume";
        const string MusicVolumeKey = "Audio.MusicVolume";
        const string SfxVolumeKey = "Audio.SfxVolume";
        const string MenuVolumeKey = "Audio.MenuVolume";

        public static void Bind(VisualElement root)
        {
            LoadSavedVolumes();

            BindSlider(root, "MasterVolumeSlider", "MasterVolumeValue", SoundMixer.soundMasterVol, MasterVolumeKey);
            BindSlider(root, "MusicVolumeSlider", "MusicVolumeValue", SoundMixer.soundMusicVol, MusicVolumeKey);
            BindSlider(root, "SfxVolumeSlider", "SfxVolumeValue", SoundMixer.soundSFXVol, SfxVolumeKey);
            BindSlider(root, "MenuVolumeSlider", "MenuVolumeValue", SoundMixer.soundMenuVol, MenuVolumeKey);
        }

        static void LoadSavedVolumes()
        {
            ApplySavedVolume(MasterVolumeKey, SoundMixer.soundMasterVol);
            ApplySavedVolume(MusicVolumeKey, SoundMixer.soundMusicVol);
            ApplySavedVolume(SfxVolumeKey, SoundMixer.soundSFXVol);
            ApplySavedVolume(MenuVolumeKey, SoundMixer.soundMenuVol);
        }

        static void ApplySavedVolume(string key, ConfigVar configVar)
        {
            if (!PlayerPrefs.HasKey(key))
            {
                return;
            }

            var value = Mathf.Clamp01(PlayerPrefs.GetFloat(key));
            configVar.Value = value.ToString(CultureInfo.InvariantCulture);
        }

        static void BindSlider(VisualElement root, string sliderName, string valueLabelName, ConfigVar configVar, string prefsKey)
        {
            var slider = root.Q<Slider>(sliderName);
            var valueLabel = root.Q<Label>(valueLabelName);
            if (slider == null)
            {
                return;
            }

            slider.lowValue = 0f;
            slider.highValue = 100f;
            slider.value = configVar.FloatValue * 100f;
            UpdateValueLabel(valueLabel, slider.value);

            slider.RegisterValueChangedCallback(evt =>
            {
                var normalized = Mathf.Clamp01(evt.newValue / 100f);
                configVar.Value = normalized.ToString(CultureInfo.InvariantCulture);
                PlayerPrefs.SetFloat(prefsKey, normalized);
                PlayerPrefs.Save();
                UpdateValueLabel(valueLabel, evt.newValue);
            });
        }

        static void UpdateValueLabel(Label valueLabel, float sliderValue)
        {
            if (valueLabel == null)
            {
                return;
            }

            valueLabel.text = $"{Mathf.RoundToInt(sliderValue)}%";
        }
    }
}
