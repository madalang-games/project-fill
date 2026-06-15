using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Services;

namespace Game.OutGame.Settings
{
    public class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private Toggle       _bgmToggle;
        [SerializeField] private Toggle       _sfxToggle;
        [SerializeField] private Toggle       _screenShakeToggle;
        [SerializeField] private Toggle       _hapticToggle;
        [SerializeField] private Slider       _bgmSlider;
        [SerializeField] private Slider       _sfxSlider;
        [SerializeField] private TMP_Dropdown _langDropdown;
        [SerializeField] private Button       _backdropButton;
        [SerializeField] private TMP_Text     _versionText;

        private const string KeyBGM         = "setting_bgm";
        private const string KeySFX         = "setting_sfx";
        private const string KeyScreenShake = "setting_screen_shake";
        private const string KeyHaptic      = "setting_haptic_enabled";

        // Add entry to both arrays in tandem when supporting a new language
        private static readonly Language[] _supportedLangs = { Language.KO, Language.EN };
        private static readonly string[]   _langStringIds  = { "popup.settings.lang.KO", "popup.settings.lang.EN" };

        private void Awake()
        {
            if (SoundManager.Instance != null)
            {
                var snd = SoundManager.Instance;
                _bgmToggle.isOn = !snd.BGMMute;
                _sfxToggle.isOn = !snd.SFXMute;

                if (_bgmSlider != null)
                {
                    _bgmSlider.value = snd.BGMVolume;
                    _bgmSlider.onValueChanged.AddListener(v => snd.BGMVolume = v);
                }
                if (_sfxSlider != null)
                {
                    _sfxSlider.value = snd.SFXVolume;
                    _sfxSlider.onValueChanged.AddListener(v => snd.SFXVolume = v);
                }

                _bgmToggle.onValueChanged.AddListener(v => snd.BGMMute = !v);
                _sfxToggle.onValueChanged.AddListener(v => snd.SFXMute = !v);
            }
            else
            {
                _bgmToggle.isOn = PlayerPrefs.GetInt(KeyBGM, 1) == 1;
                _sfxToggle.isOn = PlayerPrefs.GetInt(KeySFX, 1) == 1;
                _bgmToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeyBGM, v ? 1 : 0); });
                _sfxToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeySFX, v ? 1 : 0); });
            }

            _screenShakeToggle.isOn = PlayerPrefs.GetInt(KeyScreenShake, 1) == 1;
            _screenShakeToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeyScreenShake, v ? 1 : 0); });

            if (_hapticToggle != null)
            {
                _hapticToggle.isOn = PlayerPrefs.GetInt(KeyHaptic, 1) == 1;
                _hapticToggle.onValueChanged.AddListener(v => { PlayerPrefs.SetInt(KeyHaptic, v ? 1 : 0); });
            }

            if (_langDropdown != null && LocalizationService.Instance != null)
            {
                LocalizationService.Instance.OnLanguageChanged += RebuildLangDropdown;
                _langDropdown.onValueChanged.AddListener(OnLangDropdownChanged);
                RebuildLangDropdown();
            }

            if (_backdropButton != null) _backdropButton.onClick.AddListener(Close);
            if (_versionText != null) _versionText.text = $"v{Application.version}";
        }

        private void OnDestroy()
        {
            if (LocalizationService.Instance != null)
                LocalizationService.Instance.OnLanguageChanged -= RebuildLangDropdown;
        }

        private void RebuildLangDropdown()
        {
            if (_langDropdown == null || LocalizationService.Instance == null) return;
            int current = Array.IndexOf(_supportedLangs, LocalizationService.Instance.CurrentLanguage);
            _langDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>();
            for (int i = 0; i < _supportedLangs.Length; i++)
                options.Add(new TMP_Dropdown.OptionData(LocalizationService.Instance.Get(_langStringIds[i])));
            _langDropdown.AddOptions(options);
            _langDropdown.SetValueWithoutNotify(current < 0 ? 0 : current);
            _langDropdown.RefreshShownValue();

            var font = LocalizationService.Instance.GetFont(LocalizationService.Instance.CurrentLanguage);
            if (font != null)
            {
                if (_langDropdown.captionText != null) _langDropdown.captionText.font = font;
                if (_langDropdown.itemText != null)    _langDropdown.itemText.font    = font;
            }
        }

        private void OnLangDropdownChanged(int idx)
        {
            if (idx >= 0 && idx < _supportedLangs.Length && LocalizationService.Instance != null)
                LocalizationService.Instance.SetLanguage(_supportedLangs[idx]);
        }

        private void Close()
        {
            var appear = GetComponent<Core.UI.UIPanelAppear>();
            if (appear != null)
                appear.Disappear(() => UIManager.Instance?.CloseTopPopup());
            else
                UIManager.Instance?.CloseTopPopup();
        }
    }
}
