using UnityEngine;
using System.Collections.Generic;
using WaiJigsaw.Data;

[System.Serializable]
public struct LevelConfig
{
    public int levelNumber;
    public PuzzleData puzzleData;
    public int rows;
    public int cols;
}

public class LevelManager : MonoBehaviour
{
    private Dictionary<int, LevelConfig> _levelCache = new Dictionary<int, LevelConfig>();

    private void Awake()
    {
        // LevelTable 초기화
        LevelTable.Load();
    }

    public LevelConfig GetLevelInfo(int levelNumber)
    {
        // 1. 이미 캐싱된 설정이 있다면 반환
        if (_levelCache.ContainsKey(levelNumber))
        {
            return _levelCache[levelNumber];
        }

        // 2. LevelTable에서 해당 레벨 찾기
        LevelTableRecord record = LevelTable.Get(levelNumber);

        // 레벨 데이터가 없으면 레벨 1로 대체
        if (record == null)
        {
            Debug.LogWarning($"Level {levelNumber} 데이터가 없습니다. 레벨 1로 대체합니다.");
            record = LevelTable.Get(1);
            if (record == null) return new LevelConfig();
        }

        // 3. LevelConfig 객체 생성 및 리소스 로드
        LevelConfig config = new LevelConfig();
        config.levelNumber = record.levelID;
        config.rows = record.Rows;
        config.cols = record.Cols;

        // PuzzleData 생성 (ScriptableObject를 런타임에 생성)
        config.puzzleData = ScriptableObject.CreateInstance<PuzzleData>();

        // 이미지 로드 (Resources/Sprites 폴더)
        Sprite loadedSprite = Resources.Load<Sprite>($"Sprites/{record.ImageName}");
        if (loadedSprite == null)
        {
            // 루트에서도 찾아봄
            loadedSprite = Resources.Load<Sprite>(record.ImageName);
        }

        if (loadedSprite != null)
        {
            config.puzzleData.sourceImage = loadedSprite.texture;
        }
        else
        {
            Debug.LogError($"LevelManager: 이미지 '{record.ImageName}'를 Resources 폴더에서 찾을 수 없습니다!");
        }

        // 캐시에 저장
        _levelCache[levelNumber] = config;

        return config;
    }

    /// <summary>
    /// 전체 레벨 수 반환
    /// </summary>
    public int GetTotalLevelCount()
    {
        return LevelTable.Count;
    }
}