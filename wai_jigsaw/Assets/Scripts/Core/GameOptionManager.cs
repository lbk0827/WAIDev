using UnityEngine;

namespace WaiJigsaw.Core
{
    /// <summary>
    /// 게임 옵션을 관리하는 싱글턴 매니저
    /// - BGM On/Off
    /// - SFX On/Off
    /// - Haptic (진동) On/Off
    /// PlayerPrefs를 사용하여 설정 저장
    /// </summary>
    public class GameOptionManager : MonoBehaviour
    {
        public static GameOptionManager Instance { get; private set; }

        private const string BGM_KEY = "GameOption_BGM";
        private const string SFX_KEY = "GameOption_SFX";
        private const string HAPTIC_KEY = "GameOption_Haptic";

        // 기본값은 모두 활성화
        private bool _bgmEnabled = true;
        private bool _sfxEnabled = true;
        private bool _hapticEnabled = true;

        public bool BGMEnabled
        {
            get => _bgmEnabled;
            set
            {
                _bgmEnabled = value;
                PlayerPrefs.SetInt(BGM_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplyBGMSetting();
            }
        }

        public bool SFXEnabled
        {
            get => _sfxEnabled;
            set
            {
                _sfxEnabled = value;
                PlayerPrefs.SetInt(SFX_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                ApplySFXSetting();
            }
        }

        public bool HapticEnabled
        {
            get => _hapticEnabled;
            set
            {
                _hapticEnabled = value;
                PlayerPrefs.SetInt(HAPTIC_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 저장된 설정을 로드합니다.
        /// </summary>
        private void LoadSettings()
        {
            _bgmEnabled = PlayerPrefs.GetInt(BGM_KEY, 1) == 1;
            _sfxEnabled = PlayerPrefs.GetInt(SFX_KEY, 1) == 1;
            _hapticEnabled = PlayerPrefs.GetInt(HAPTIC_KEY, 1) == 1;

            // 로드 후 설정 적용
            ApplyBGMSetting();
            ApplySFXSetting();
        }

        /// <summary>
        /// BGM 설정을 적용합니다.
        /// </summary>
        private void ApplyBGMSetting()
        {
            // TODO: AudioManager가 구현되면 여기서 BGM 볼륨 조절
            // AudioListener.volume for BGM 또는 별도 AudioMixer 사용
            Debug.Log($"[GameOptionManager] BGM: {(_bgmEnabled ? "ON" : "OFF")}");
        }

        /// <summary>
        /// SFX 설정을 적용합니다.
        /// </summary>
        private void ApplySFXSetting()
        {
            // TODO: AudioManager가 구현되면 여기서 SFX 볼륨 조절
            Debug.Log($"[GameOptionManager] SFX: {(_sfxEnabled ? "ON" : "OFF")}");
        }

        /// <summary>
        /// 진동을 재생합니다. (HapticEnabled가 true일 때만)
        /// </summary>
        public void PlayHaptic()
        {
            if (!_hapticEnabled) return;

#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
            Debug.Log("[GameOptionManager] Haptic played");
        }

        /// <summary>
        /// 커스텀 진동을 재생합니다. (Android만 지원)
        /// </summary>
        public void PlayHapticLight()
        {
            if (!_hapticEnabled) return;

            // 기본 진동 사용 (추후 Nice Vibrations 등의 라이브러리 통합 가능)
#if UNITY_ANDROID || UNITY_IOS
            // 짧은 진동 - 현재는 기본 진동만 지원
            Handheld.Vibrate();
#endif
        }
    }
}
