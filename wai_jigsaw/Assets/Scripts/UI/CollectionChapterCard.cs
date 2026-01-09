using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 컬렉션 팝업 내 챕터 카드 UI
    /// - 클리어 시: 챕터 이미지 + 이름 + 레벨 구간
    /// - 미클리어 시: 자물쇠 아이콘 + 레벨 구간
    /// </summary>
    public class CollectionChapterCard : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image _chapterImage;    // 챕터 이미지
        [SerializeField] private Image _lockIcon;        // 자물쇠 아이콘
        [SerializeField] private TMP_Text _chapterName;  // 챕터 이름 (예: "Italy")
        [SerializeField] private TMP_Text _levelRange;   // 레벨 구간 (예: "1-25")
        [SerializeField] private Button _button;         // 클릭용 버튼

        // 데이터
        private LevelGroupTableRecord _groupData;
        private bool _isCleared;
        private Action<LevelGroupTableRecord, bool> _onClickCallback;

        /// <summary>
        /// 외부에서 참조를 설정합니다 (프리팹 없이 코드로 생성 시).
        /// </summary>
        public void SetReferences(Image chapterImage, Image lockIcon, TMP_Text chapterName, TMP_Text levelRange, Button button)
        {
            _chapterImage = chapterImage;
            _lockIcon = lockIcon;
            _chapterName = chapterName;
            _levelRange = levelRange;
            _button = button;
        }

        /// <summary>
        /// 챕터 카드를 초기화합니다.
        /// </summary>
        /// <param name="group">레벨 그룹 데이터</param>
        /// <param name="isCleared">챕터 클리어 여부</param>
        /// <param name="chapterSprite">챕터 이미지 (클리어 시)</param>
        /// <param name="lockSprite">자물쇠 아이콘</param>
        /// <param name="onClickCallback">클릭 콜백</param>
        public void Initialize(
            LevelGroupTableRecord group,
            bool isCleared,
            Sprite chapterSprite,
            Sprite lockSprite,
            Action<LevelGroupTableRecord, bool> onClickCallback)
        {
            _groupData = group;
            _isCleared = isCleared;
            _onClickCallback = onClickCallback;

            // 레벨 구간 텍스트 설정
            if (_levelRange != null)
            {
                _levelRange.text = $"{group.StartLevel}-{group.EndLevel}";
            }

            if (isCleared)
            {
                // 클리어 상태: 이미지 + 이름 표시
                SetClearedState(chapterSprite, group.GroupName);
            }
            else
            {
                // 미클리어 상태: 자물쇠 표시
                SetLockedState(lockSprite);
            }

            // 버튼 이벤트 등록
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnCardClicked);
            }
        }

        /// <summary>
        /// 클리어 상태 설정
        /// </summary>
        private void SetClearedState(Sprite chapterSprite, string chapterName)
        {
            // 챕터 이미지 표시
            if (_chapterImage != null)
            {
                _chapterImage.gameObject.SetActive(true);
                _chapterImage.sprite = chapterSprite;
            }

            // 챕터 이름 표시
            if (_chapterName != null)
            {
                _chapterName.gameObject.SetActive(true);
                _chapterName.text = chapterName ?? $"Chapter {_groupData.GroupID}";
            }

            // 자물쇠 숨김
            if (_lockIcon != null)
            {
                _lockIcon.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 미클리어 (잠금) 상태 설정
        /// </summary>
        private void SetLockedState(Sprite lockSprite)
        {
            // 챕터 이미지 숨김
            if (_chapterImage != null)
            {
                _chapterImage.gameObject.SetActive(false);
            }

            // 챕터 이름 숨김
            if (_chapterName != null)
            {
                _chapterName.gameObject.SetActive(false);
            }

            // 자물쇠 표시
            if (_lockIcon != null)
            {
                _lockIcon.gameObject.SetActive(true);
                if (lockSprite != null)
                {
                    _lockIcon.sprite = lockSprite;
                }
            }
        }

        /// <summary>
        /// 카드 클릭 시 호출됩니다.
        /// </summary>
        private void OnCardClicked()
        {
            _onClickCallback?.Invoke(_groupData, _isCleared);
        }
    }
}
