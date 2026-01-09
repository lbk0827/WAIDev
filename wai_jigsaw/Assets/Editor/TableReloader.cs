using UnityEditor;
using UnityEngine;
using WaiJigsaw.Data;

namespace WaiJigsaw.Editor
{
    /// <summary>
    /// 테이블 데이터 리로드 에디터 유틸리티
    /// Unity 메뉴: Tools > Reload Tables
    /// </summary>
    public static class TableReloader
    {
        [MenuItem("Tools/Reload Tables %#r")]  // Ctrl+Shift+R 단축키
        public static void ReloadAllTables()
        {
            // 캐시 초기화
            LevelTable.Clear();
            LevelGroupTable.Clear();

            // 다시 로드
            LevelTable.Load();
            LevelGroupTable.Load();

            Debug.Log($"[TableReloader] 테이블 리로드 완료 - LevelTable: {LevelTable.Count}개, LevelGroupTable: {LevelGroupTable.Count}개");
        }

        [MenuItem("Tools/Reload LevelTable")]
        public static void ReloadLevelTable()
        {
            LevelTable.Clear();
            LevelTable.Load();
            Debug.Log($"[TableReloader] LevelTable 리로드 완료 - {LevelTable.Count}개");
        }

        [MenuItem("Tools/Reload LevelGroupTable")]
        public static void ReloadLevelGroupTable()
        {
            LevelGroupTable.Clear();
            LevelGroupTable.Load();
            Debug.Log($"[TableReloader] LevelGroupTable 리로드 완료 - {LevelGroupTable.Count}개");
        }
    }
}
