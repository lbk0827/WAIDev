using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 카드 테이블 레코드 (Excel에서 자동 생성)
    /// </summary>
    [Serializable]
    public class CardTableRecord
    {
        public int CardID;
        public string CardName;
        public string CardBackSprite;  // Resources 경로
    }

    /// <summary>
    /// 카드 테이블 래퍼 (JSON 파싱용)
    /// </summary>
    [Serializable]
    public class CardTableWrapper
    {
        public List<CardTableRecord> records;
    }

    /// <summary>
    /// 카드 테이블 관리자
    /// </summary>
    public static class CardTable
    {
        private static Dictionary<int, CardTableRecord> _cache;
        private static List<CardTableRecord> _records;
        private const string JSON_PATH = "Tables/CardTable";
        private const string JSON_FILENAME = "CardTable.json";

        /// <summary>
        /// 테이블 데이터 로드
        /// </summary>
        public static void Load()
        {
            if (_cache != null) return;

            string jsonText = null;

#if UNITY_EDITOR
            // 에디터에서는 tmp 폴더 우선 확인
            string tmpPath = Path.Combine(Application.dataPath, "..", "tmp", "Assets", "Resources", "Tables", JSON_FILENAME);
            if (File.Exists(tmpPath))
            {
                jsonText = File.ReadAllText(tmpPath);
                Debug.Log($"CardTable: tmp 폴더에서 로드 ({tmpPath})");
            }
#endif

            // tmp에서 못 찾았으면 Resources에서 로드
            if (string.IsNullOrEmpty(jsonText))
            {
                TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
                if (jsonFile == null)
                {
                    Debug.LogError($"CardTable: '{JSON_PATH}' 파일을 찾을 수 없습니다!");
                    return;
                }
                jsonText = jsonFile.text;
            }

            // JSON 배열을 래퍼로 감싸서 파싱
            string wrappedJson = "{\"records\":" + jsonText + "}";
            CardTableWrapper wrapper = JsonUtility.FromJson<CardTableWrapper>(wrappedJson);

            _records = wrapper.records;
            _cache = new Dictionary<int, CardTableRecord>();

            foreach (var record in _records)
            {
                _cache[record.CardID] = record;
            }

            Debug.Log($"CardTable: {_cache.Count}개의 카드 데이터 로드 완료");
        }

        /// <summary>
        /// 특정 카드 데이터 가져오기
        /// </summary>
        public static CardTableRecord Get(int cardID)
        {
            if (_cache == null) Load();

            if (_cache.TryGetValue(cardID, out CardTableRecord record))
            {
                return record;
            }

            Debug.LogWarning($"CardTable: CardID {cardID}를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 기본 카드 가져오기 (CardID = 1)
        /// </summary>
        public static CardTableRecord GetDefault()
        {
            return Get(1);
        }

        /// <summary>
        /// 모든 카드 데이터 가져오기
        /// </summary>
        public static List<CardTableRecord> GetAll()
        {
            if (_cache == null) Load();
            return _records;
        }

        /// <summary>
        /// 카드 뒷면 스프라이트 로드
        /// </summary>
        public static Sprite LoadCardBackSprite(int cardID)
        {
            CardTableRecord record = Get(cardID);
            if (record == null) return null;

            Sprite sprite = Resources.Load<Sprite>(record.CardBackSprite);
            if (sprite == null)
            {
                Debug.LogWarning($"CardTable: 스프라이트를 찾을 수 없습니다: {record.CardBackSprite}");
            }
            return sprite;
        }

        /// <summary>
        /// 카드 수 가져오기
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
