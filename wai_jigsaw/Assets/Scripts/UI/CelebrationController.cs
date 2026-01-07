using UnityEngine;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 레벨 클리어 시 축하 연출을 관리하는 컨트롤러
    ///
    /// 연출 구성:
    /// 1. 하단 좌우 폭발 - 화면 양쪽에서 색종이가 중앙으로 뿌려짐 (1회성)
    /// 2. 상단 낙하 - 화면 상단에서 색종이가 비처럼 떨어짐 (Loop)
    /// </summary>
    public class CelebrationController : MonoBehaviour
    {
        [Header("====== Particle Systems ======")]
        [Tooltip("하단 왼쪽 폭발 파티클")]
        [SerializeField] private ParticleSystem _burstLeftParticle;
        [Tooltip("하단 오른쪽 폭발 파티클")]
        [SerializeField] private ParticleSystem _burstRightParticle;
        [Tooltip("상단 낙하 파티클 (Loop)")]
        [SerializeField] private ParticleSystem _fallingParticle;

        [Header("====== Settings ======")]
        [Tooltip("하단 폭발 후 상단 낙하 시작까지 딜레이 (초)")]
        [SerializeField] private float _fallingStartDelay = 0.3f;

        // 상태 추적
        private bool _isPlaying = false;

        /// <summary>
        /// 축하 연출을 시작합니다.
        /// </summary>
        public void Play()
        {
            Debug.Log($"[CelebrationController] Play() 호출됨 - isPlaying: {_isPlaying}");

            if (_isPlaying) return;
            _isPlaying = true;

            Debug.Log($"[CelebrationController] 파티클 상태 - BurstLeft: {_burstLeftParticle != null}, BurstRight: {_burstRightParticle != null}, Falling: {_fallingParticle != null}");

            // 1. 하단 좌우 폭발 (동시 재생)
            PlayBurstEffect();

            // 2. 상단 낙하 (딜레이 후 시작)
            Invoke(nameof(PlayFallingEffect), _fallingStartDelay);
        }

        /// <summary>
        /// 축하 연출을 중지합니다.
        /// </summary>
        public void Stop()
        {
            _isPlaying = false;
            CancelInvoke();

            if (_burstLeftParticle != null)
            {
                _burstLeftParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_burstRightParticle != null)
            {
                _burstRightParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (_fallingParticle != null)
            {
                _fallingParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        /// <summary>
        /// 하단 좌우 폭발 효과를 재생합니다.
        /// </summary>
        private void PlayBurstEffect()
        {
            if (_burstLeftParticle != null)
            {
                Debug.Log($"[CelebrationController] BurstLeft Play - Position: {_burstLeftParticle.transform.position}, Active: {_burstLeftParticle.gameObject.activeInHierarchy}");
                _burstLeftParticle.Play();
            }
            else
            {
                Debug.LogWarning("[CelebrationController] BurstLeft 파티클이 할당되지 않았습니다!");
            }

            if (_burstRightParticle != null)
            {
                Debug.Log($"[CelebrationController] BurstRight Play - Position: {_burstRightParticle.transform.position}, Active: {_burstRightParticle.gameObject.activeInHierarchy}");
                _burstRightParticle.Play();
            }
            else
            {
                Debug.LogWarning("[CelebrationController] BurstRight 파티클이 할당되지 않았습니다!");
            }
        }

        /// <summary>
        /// 상단 낙하 효과를 재생합니다. (Loop)
        /// </summary>
        private void PlayFallingEffect()
        {
            if (_fallingParticle != null && _isPlaying)
            {
                _fallingParticle.Play();
            }
        }

        /// <summary>
        /// 연출이 재생 중인지 확인합니다.
        /// </summary>
        public bool IsPlaying => _isPlaying;

        private void OnDisable()
        {
            Stop();
        }
    }
}
