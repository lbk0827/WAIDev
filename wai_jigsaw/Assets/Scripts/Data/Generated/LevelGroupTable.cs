using System;
using System.Collections.Generic;
using UnityEngine;

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 레벨 그룹 테이블 레코드 (Excel에서 자동 생성)
    /// </summary>
    [Serializable]
    public class LevelGroupTableRecord
    {
        public int GroupID;
        public int StartLevel;
        public int EndLevel;
        public string ImageName;
        public string GroupName;  // 챕터 이름 (예: "Italy")
    }

    /// <summary>
    /// 레벨 그룹 테이블 래퍼 (JSON 파싱용)
    /// </summary>
    [Serializable]
    public class LevelGroupTableWrapper
    {
        public List<LevelGroupTableRecord> records;
    }

    /// <summary>
    /// 레벨 그룹 테이블 관리자
    /// </summary>
    public static class LevelGroupTable
    {
        private static Dictionary<int, LevelGroupTableRecord> _cache;
        private static List<LevelGroupTableRecord> _records;
        private const string JSON_PATH = "Tables/LevelGroupTable";

        /// <summary>
        /// 테이블 데이터 로드
        /// </summary>
        public static void Load()
        {
            if (_cache != null) return;

            TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
            if (jsonFile == null)
            {
                Debug.LogError($"LevelGroupTable: '{JSON_PATH}' 파일을 찾을 수 없습니다!");
                return;
            }

            // JSON 배열을 래퍼로 감싸서 파싱
            string wrappedJson = "{\"records\":" + jsonFile.text + "}";
            LevelGroupTableWrapper wrapper = JsonUtility.FromJson<LevelGroupTableWrapper>(wrappedJson);

            _records = wrapper.records;
            _cache = new Dictionary<int, LevelGroupTableRecord>();

            foreach (var record in _records)
            {
                _cache[record.GroupID] = record;
            }

            Debug.Log($"LevelGroupTable: {_cache.Count}개의 그룹 데이터 로드 완료");
        }

        /// <summary>
        /// 특정 그룹 데이터 가져오기
        /// </summary>
        public static LevelGroupTableRecord Get(int groupID)
        {
            if (_cache == null) Load();

            if (_cache.TryGetValue(groupID, out LevelGroupTableRecord record))
            {
                return record;
            }

            Debug.LogWarning($"LevelGroupTable: GroupID {groupID}를 찾을 수 없습니다.");
            return null;
        }

        /// <summary>
        /// 특정 레벨이 속한 그룹 가져오기
        /// </summary>
        public static LevelGroupTableRecord GetGroupForLevel(int levelNumber)
        {
            if (_cache == null) Load();

            foreach (var record in _records)
            {
                if (levelNumber >= record.StartLevel && levelNumber <= record.EndLevel)
                {
                    return record;
                }
            }

            // 해당 그룹이 없으면 첫 번째 그룹 반환
            Debug.LogWarning($"레벨 {levelNumber}에 해당하는 그룹이 없습니다. 그룹 1을 반환합니다.");
            return _records.Count > 0 ? _records[0] : null;
        }

        /// <summary>
        /// 모든 그룹 데이터 가져오기
        /// </summary>
        public static List<LevelGroupTableRecord> GetAll()
        {
            if (_cache == null) Load();
            return _records;
        }

        /// <summary>
        /// 그룹 수 가져오기
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
