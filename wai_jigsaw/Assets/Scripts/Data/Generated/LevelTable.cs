using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
#endif

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 레벨 테이블 레코드 (Excel에서 자동 생성)
    /// </summary>
    [Serializable]
    public class LevelTableRecord
    {
        public int levelID;
        public string ImageName;
        public int Rows;
        public int Cols;
    }

    /// <summary>
    /// 레벨 테이블 래퍼 (JSON 파싱용)
    /// </summary>
    [Serializable]
    public class LevelTableWrapper
    {
        public List<LevelTableRecord> records;
    }

    /// <summary>
    /// 레벨 테이블 관리자
    /// </summary>
    public static class LevelTable
    {
        private static Dictionary<int, LevelTableRecord> _cache;
        private static List<LevelTableRecord> _records;
        private const string JSON_PATH = "Tables/LevelTable";
        private const string JSON_FILENAME = "LevelTable.json";

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
                Debug.Log($"LevelTable: tmp 폴더에서 로드 ({tmpPath})");
            }
#endif

            // tmp에서 못 찾았으면 Resources에서 로드
            if (string.IsNullOrEmpty(jsonText))
            {
                TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
                if (jsonFile == null)
                {
                    Debug.LogError($"LevelTable: '{JSON_PATH}' 파일을 찾을 수 없습니다!");
                    return;
                }
                jsonText = jsonFile.text;
            }

            // JSON 배열을 래퍼로 감싸서 파싱
            string wrappedJson = "{\"records\":" + jsonText + "}";
            LevelTableWrapper wrapper = JsonUtility.FromJson<LevelTableWrapper>(wrappedJson);

            _records = wrapper.records;
            _cache = new Dictionary<int, LevelTableRecord>();

            foreach (var record in _records)
            {
                _cache[record.levelID] = record;
            }

            Debug.Log($"LevelTable: {_cache.Count}개의 레벨 데이터 로드 완료");
        }

        /// <summary>
        /// 특정 레벨 데이터 가져오기
        /// </summary>
        public static LevelTableRecord Get(int levelID)
        {
            if (_cache == null) Load();

            if (_cache.TryGetValue(levelID, out LevelTableRecord record))
            {
                return record;
            }

            Debug.LogWarning($"LevelTable: levelID {levelID}를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 모든 레벨 데이터 가져오기
        /// </summary>
        public static List<LevelTableRecord> GetAll()
        {
            if (_cache == null) Load();
            return _records;
        }

        /// <summary>
        /// 레벨 수 가져오기
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
