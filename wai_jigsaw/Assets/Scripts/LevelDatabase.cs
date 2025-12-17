using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct LevelConfig
{
    public int levelNumber;         // 레벨 1
    public PuzzleData puzzleData;   // 사용할 이미지 데이터
    
    [Header("조각 개수 설정")]
    public int rows; // 세로 줄 수 (예: 4)
    public int cols; // 가로 칸 수 (예: 6)
    // 결과: 4 * 6 = 24조각
}

[CreateAssetMenu(fileName = "New Level Database", menuName = "Jigsaw/Level Database")]
public class LevelDatabase : ScriptableObject
{
    public List<LevelConfig> levels;

    public LevelConfig GetLevelInfo(int currentLevel)
    {
        foreach (var level in levels)
        {
            if (level.levelNumber == currentLevel)
                return level;
        }
        return levels[0]; // 없으면 1레벨 반환
    }
}