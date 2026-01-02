using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 아웃게임 리소스 경로 관리 (ScriptableObject)
    /// - Inspector에서 아이템별 아이콘 스프라이트를 직접 할당
    /// - ReferenceProject의 OutgameResourcePath 간소화 버전
    /// </summary>
    [CreateAssetMenu(fileName = "OutgameResourcePath", menuName = "WaiJigsaw/OutgameResourcePath", order = 0)]
    public class OutgameResourcePath : ScriptableObject
    {
        private static OutgameResourcePath _instance;
        public static OutgameResourcePath Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<OutgameResourcePath>("OutgameResourcePath");
                    if (_instance == null)
                    {
                        Debug.LogError("[OutgameResourcePath] Resources/OutgameResourcePath.asset을 찾을 수 없습니다!");
                    }
                }
                return _instance;
            }
        }

        [Header("Item Icons")]
        [Tooltip("아이템 타입별 아이콘 설정")]
        public ItemIconSetting[] ItemIconSettings;

        /// <summary>
        /// 아이템 아이콘 설정
        /// </summary>
        [Serializable]
        public class ItemIconSetting
        {
            [Tooltip("아이템 타입 ID (예: 101 = 코인)")]
            public int ItemType;

            [Tooltip("아이콘 스프라이트")]
            public Sprite IconSprite;
        }

        /// <summary>
        /// 아이템 타입으로 아이콘 설정 가져오기
        /// </summary>
        public ItemIconSetting GetItemIconSetting(int itemType)
        {
            if (ItemIconSettings == null) return null;

            foreach (var setting in ItemIconSettings)
            {
                if (setting.ItemType == itemType)
                {
                    return setting;
                }
            }
            return null;
        }

        /// <summary>
        /// 아이템 타입으로 아이콘 스프라이트 가져오기
        /// </summary>
        public Sprite GetItemIcon(int itemType)
        {
            var setting = GetItemIconSetting(itemType);
            return setting?.IconSprite;
        }

        /// <summary>
        /// 코인 아이콘 가져오기 (편의 메서드)
        /// </summary>
        public Sprite GetCoinIcon()
        {
            return GetItemIcon(ItemTable.ITEM_TYPE_COIN);
        }

        #region Editor Validation
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Editor에서 중복 ItemType 체크
            if (ItemIconSettings == null) return;

            var seen = new HashSet<int>();
            foreach (var setting in ItemIconSettings)
            {
                if (seen.Contains(setting.ItemType))
                {
                    Debug.LogWarning($"[OutgameResourcePath] 중복된 ItemType: {setting.ItemType}");
                }
                seen.Add(setting.ItemType);
            }
        }
#endif
        #endregion
    }
}
