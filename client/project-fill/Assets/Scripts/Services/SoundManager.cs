using Game.Core;
using UnityEngine;

namespace Game.Services
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] SfxCatalog sfxCatalog;
        [SerializeField] string resourcesCatalogPath = "SfxCatalog";

        [SerializeField] private AudioSource _bgmSource;
        [SerializeField] private AudioSource _sfxSource;

        private const string KeyBGMVolume = "sound_bgm_volume";
        private const string KeySFXVolume = "sound_sfx_volume";
        private const string KeyBGMMute = "sound_bgm_mute";
        private const string KeySFXMute = "sound_sfx_mute";

        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;
        private bool _bgmMute = false;
        private bool _sfxMute = false;

        private readonly System.Collections.Generic.Dictionary<SfxId, float> _lastPlayedAt = new();

        public float BGMVolume
        {
            get => _bgmVolume;
            set
            {
                _bgmVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyBGMVolume, _bgmVolume);
                UpdateVolumes();
            }
        }

        public float SFXVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeySFXVolume, _sfxVolume);
                UpdateVolumes();
            }
        }

        public bool BGMMute
        {
            get => _bgmMute;
            set
            {
                _bgmMute = value;
                PlayerPrefs.SetInt(KeyBGMMute, _bgmMute ? 1 : 0);
                UpdateVolumes();
            }
        }

        public bool SFXMute
        {
            get => _sfxMute;
            set
            {
                _sfxMute = value;
                PlayerPrefs.SetInt(KeySFXMute, _sfxMute ? 1 : 0);
                UpdateVolumes();
            }
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmVolume = PlayerPrefs.GetFloat(KeyBGMVolume, 0.7f);
            _sfxVolume = PlayerPrefs.GetFloat(KeySFXVolume, 0.8f);
            _bgmMute = PlayerPrefs.GetInt(KeyBGMMute, 0) == 1;
            _sfxMute = PlayerPrefs.GetInt(KeySFXMute, 0) == 1;

            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
            }
            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.loop = false;
                _sfxSource.playOnAwake = false;
            }

            if (sfxCatalog == null)
                sfxCatalog = Resources.Load<SfxCatalog>(resourcesCatalogPath);

            UpdateVolumes();
        }

        private void OnEnable()  => SfxEventBus.Requested += PlaySfx;
        private void OnDisable() => SfxEventBus.Requested -= PlaySfx;

        private void UpdateVolumes()
        {
            if (_bgmSource != null)
                _bgmSource.volume = _bgmMute ? 0f : _bgmVolume;
            if (_sfxSource != null)
                _sfxSource.volume = _sfxMute ? 0f : _sfxVolume;
        }

        public void PlayBGM(AudioClip clip)
        {
            if (_bgmSource == null || clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void StopBGM()
        {
            if (_bgmSource != null) _bgmSource.Stop();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (_sfxSource == null || clip == null || _sfxMute) return;
            _sfxSource.PlayOneShot(clip, _sfxVolume);
        }

        public void PlaySfx(SfxId id)
        {
            if (_sfxSource == null || _sfxMute || sfxCatalog == null) return;
            if (!sfxCatalog.TryGet(id, out var entry) || entry.clip == null) return;

            if (entry.cooldownSeconds > 0f &&
                _lastPlayedAt.TryGetValue(id, out var lastPlayed) &&
                Time.unscaledTime - lastPlayed < entry.cooldownSeconds)
                return;

            float minPitch = Mathf.Min(entry.pitchRange.x, entry.pitchRange.y);
            float maxPitch = Mathf.Max(entry.pitchRange.x, entry.pitchRange.y);
            _sfxSource.pitch = Mathf.Approximately(minPitch, maxPitch)
                ? minPitch
                : Random.Range(minPitch, maxPitch);
            _sfxSource.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume) * _sfxVolume);
            _lastPlayedAt[id] = Time.unscaledTime;
        }
    }
}
