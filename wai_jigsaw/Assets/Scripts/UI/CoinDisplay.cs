using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Data;
using System.Collections;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 코인 잔액 표시 UI 컴포넌트
    /// - ReferenceProject의 BTN_Common_Coin 패턴 참고
    /// - Observer 패턴으로 코인 변경 시 자동 갱신
    /// </summary>
    public class CoinDisplay : MonoBehaviour, IObserver<CoinChangedEvent>
    {
        [Header("UI References")]
        [SerializeField] private Image _coinIcon;           // 코인 아이콘
        [SerializeField] private TMP_Text _coinAmountText;  // 코인 수량 텍스트

        [Header("Animation Settings")]
        [SerializeField] private bool _useCountingAnimation = true;  // 카운팅 애니메이션 사용 여부
        [SerializeField] private float _countingDuration = 0.5f;     // 카운팅 애니메이션 시간

        private int _displayedAmount = 0;
        private Coroutine _countingCoroutine;

        #region Lifecycle

        private void Start()
        {
            // 코인 아이콘 설정
            SetupCoinIcon();

            // 초기 코인 표시
            _displayedAmount = GameDataContainer.Instance.Coin;
            UpdateCoinText(_displayedAmount);

            // Observer 등록
            GameDataContainer.Instance.AddCoinChangedObserver(this);
        }

        private void OnDestroy()
        {
            // Observer 해제
            if (GameDataContainer.Instance != null)
            {
                GameDataContainer.Instance.RemoveCoinChangedObserver(this);
            }

            if (_countingCoroutine != null)
            {
                StopCoroutine(_countingCoroutine);
            }
        }

        #endregion

        #region Setup

        /// <summary>
        /// 코인 아이콘 설정
        /// </summary>
        private void SetupCoinIcon()
        {
            if (_coinIcon == null) return;

            // OutgameResourcePath 또는 ItemTable에서 코인 아이콘 로드
            Sprite coinSprite = ItemTable.GetCoinIcon();
            if (coinSprite != null)
            {
                _coinIcon.sprite = coinSprite;
            }
        }

        #endregion

        #region IObserver Implementation

        public void OnChanged(CoinChangedEvent data)
        {
            if (_useCountingAnimation && gameObject.activeInHierarchy)
            {
                // 카운팅 애니메이션으로 표시
                if (_countingCoroutine != null)
                {
                    StopCoroutine(_countingCoroutine);
                }
                _countingCoroutine = StartCoroutine(CountingAnimation(data.OldAmount, data.NewAmount));
            }
            else
            {
                // 즉시 업데이트
                _displayedAmount = data.NewAmount;
                UpdateCoinText(_displayedAmount);
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// 코인 카운팅 애니메이션
        /// </summary>
        private IEnumerator CountingAnimation(int fromAmount, int toAmount)
        {
            float elapsed = 0f;

            while (elapsed < _countingDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _countingDuration);

                // Ease out quad
                t = 1f - (1f - t) * (1f - t);

                _displayedAmount = Mathf.RoundToInt(Mathf.Lerp(fromAmount, toAmount, t));
                UpdateCoinText(_displayedAmount);

                yield return null;
            }

            _displayedAmount = toAmount;
            UpdateCoinText(_displayedAmount);
            _countingCoroutine = null;
        }

        #endregion

        #region UI Update

        /// <summary>
        /// 코인 텍스트 업데이트
        /// </summary>
        private void UpdateCoinText(int amount)
        {
            if (_coinAmountText == null) return;

            // 1000 이상이면 K 단위로 표시 (예: 1.2K)
            if (amount >= 1000)
            {
                float kAmount = amount / 1000f;
                _coinAmountText.text = $"{kAmount:0.#}K";
            }
            else
            {
                _coinAmountText.text = amount.ToString();
            }
        }

        /// <summary>
        /// 수동으로 코인 표시 새로고침
        /// </summary>
        public void Refresh()
        {
            _displayedAmount = GameDataContainer.Instance.Coin;
            UpdateCoinText(_displayedAmount);
        }

        #endregion
    }
}
