using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 아이템 테이블 레코드
    /// </summary>
    [Serializable]
    public class ItemTableRecord
    {
        public int Item_Type;       // 아이템 타입 ID (예: 101 = 코인)
        public int Item_Category;   // 아이템 카테고리
        public int Item_GetType;    // 획득 방식
        public int Item_Price;      // 가격
        public string Item_Icon;    // 아이콘 리소스 경로
    }

    /// <summary>
    /// 아이템 테이블 래퍼 (JSON 파싱용)
    /// </summary>
    [Serializable]
    public class ItemTableWrapper
    {
        public List<ItemTableRecord> records;
    }

    /// <summary>
    /// 아이템 테이블 관리자
    /// </summary>
    public static class ItemTable
    {
        private static Dictionary<int, ItemTableRecord> _cache;
        private static List<ItemTableRecord> _records;
        private const string JSON_PATH = "Tables/ItemTable";

        // 아이템 타입 상수
        public const int ITEM_TYPE_COIN = 101;

        /// <summary>
        /// 테이블 데이터 로드
        /// </summary>
        public static void Load()
        {
            if (_cache != null) return;

            TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
            if (jsonFile == null)
            {
                Debug.LogWarning($"ItemTable: '{JSON_PATH}' 파일을 찾을 수 없습니다. 기본값 사용.");
                _cache = new Dictionary<int, ItemTableRecord>();
                _records = new List<ItemTableRecord>();
                return;
            }

            // JSON 배열을 래퍼로 감싸서 파싱
            string wrappedJson = "{\"records\":" + jsonFile.text + "}";
            ItemTableWrapper wrapper = JsonUtility.FromJson<ItemTableWrapper>(wrappedJson);

            _records = wrapper.records ?? new List<ItemTableRecord>();
            _cache = new Dictionary<int, ItemTableRecord>();

            foreach (var record in _records)
            {
                _cache[record.Item_Type] = record;
            }

            Debug.Log($"ItemTable: {_cache.Count}개의 아이템 데이터 로드 완료");
        }

        /// <summary>
        /// 특정 아이템 데이터 가져오기
        /// </summary>
        public static ItemTableRecord Get(int itemType)
        {
            if (_cache == null) Load();

            if (_cache.TryGetValue(itemType, out ItemTableRecord record))
            {
                return record;
            }

            Debug.LogWarning($"ItemTable: Item_Type {itemType}를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 코인 아이템 데이터 가져오기
        /// </summary>
        public static ItemTableRecord GetCoin()
        {
            return Get(ITEM_TYPE_COIN);
        }

        /// <summary>
        /// 코인 아이콘 스프라이트 로드 (OutgameResourcePath 사용)
        /// </summary>
        public static Sprite GetCoinIcon()
        {
            // OutgameResourcePath에서 아이콘 가져오기
            if (OutgameResourcePath.Instance != null)
            {
                return OutgameResourcePath.Instance.GetCoinIcon();
            }

            // Fallback: ItemTable의 Item_Icon 경로 사용
            var coinRecord = GetCoin();
            if (coinRecord == null || string.IsNullOrEmpty(coinRecord.Item_Icon))
            {
                Debug.LogWarning("ItemTable: 코인 아이콘을 찾을 수 없습니다.");
                return null;
            }

            Sprite icon = Resources.Load<Sprite>(coinRecord.Item_Icon);
            if (icon == null)
            {
                icon = Resources.Load<Sprite>($"Sprites/{coinRecord.Item_Icon}");
            }

            return icon;
        }

        /// <summary>
        /// 아이템 타입별 아이콘 스프라이트 로드 (OutgameResourcePath 사용)
        /// </summary>
        public static Sprite GetItemIcon(int itemType)
        {
            // OutgameResourcePath에서 아이콘 가져오기
            if (OutgameResourcePath.Instance != null)
            {
                return OutgameResourcePath.Instance.GetItemIcon(itemType);
            }

            // Fallback: ItemTable의 Item_Icon 경로 사용
            var record = Get(itemType);
            if (record == null || string.IsNullOrEmpty(record.Item_Icon))
            {
                return null;
            }

            Sprite icon = Resources.Load<Sprite>(record.Item_Icon);
            if (icon == null)
            {
                icon = Resources.Load<Sprite>($"Sprites/{record.Item_Icon}");
            }

            return icon;
        }

        /// <summary>
        /// 모든 아이템 데이터 가져오기
        /// </summary>
        public static List<ItemTableRecord> GetAll()
        {
            if (_cache == null) Load();
            return _records;
        }

        /// <summary>
        /// 아이템 수 가져오기
        /// </summary>
        public static int Count
        {
            get
            {
                if (_cache == null) Load();
                return _cache?.Count ?? 0;
            }
        }

        /// <summary>
        /// 캐시 초기화 (재로드용)
        /// </summary>
        public static void Clear()
        {
            _cache = null;
            _records = null;
        }
    }
}
