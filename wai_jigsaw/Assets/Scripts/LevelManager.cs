using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class LevelItem
{
    public int levelID;
    public string imageName;
    public int rows;
    public int cols;
}

[System.Serializable]
public class LevelDataWrapper
{
    public List<LevelItem> levels;
}

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
    // JSON 파일 이름 (확장자 제외, Resources 폴더 내 위치)
    private const string JSON_FILE_NAME = "LevelData";

    private Dictionary<int, LevelConfig> _levelCache = new Dictionary<int, LevelConfig>();
    private LevelDataWrapper _loadedData;

    private void Awake()
    {
        LoadLevelData();
    }

    private void LoadLevelData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(JSON_FILE_NAME);
        if (jsonFile == null)
        {
            Debug.LogError($"LevelManager: '{JSON_FILE_NAME}' 파일을 찾을 수 없습니다! Assets/Resources 폴더를 확인하세요.");
            return;
        }

        _loadedData = JsonUtility.FromJson<LevelDataWrapper>(jsonFile.text);
        Debug.Log($"LevelManager: {_loadedData.levels.Count}개의 레벨 데이터를 로드했습니다.");
    }

    public LevelConfig GetLevelInfo(int levelNumber)
    {
        // 1. 이미 캐싱된(만들어진) 설정이 있다면 반환
        if (_levelCache.ContainsKey(levelNumber))
        {
            return _levelCache[levelNumber];
        }

        // 2. JSON 데이터에서 해당 레벨 찾기
        if (_loadedData == null) LoadLevelData();

        LevelItem item = _loadedData.levels.FirstOrDefault(l => l.levelID == levelNumber);
        
        // 레벨 데이터가 없으면 기본값(또는 마지막 레벨) 반환하거나 에러 처리
        if (item == null)
        {
            Debug.LogWarning($"Level {levelNumber} 데이터가 JSON에 없습니다. 레벨 1로 대체합니다.");
            item = _loadedData.levels.FirstOrDefault(l => l.levelID == 1);
            if(item == null) return new LevelConfig(); // 비상용 빈 객체
        }

        // 3. LevelConfig 객체 생성 및 리소스 로드
        LevelConfig config = new LevelConfig();
        config.levelNumber = item.levelID;
        config.rows = item.rows;
        config.cols = item.cols;
        
        // PuzzleData 생성 (ScriptableObject를 런타임에 생성)
        config.puzzleData = ScriptableObject.CreateInstance<PuzzleData>();
        
        // 이미지 로드 (Resources/Sprites 폴더 안에 이미지가 있어야 함)
        // 주의: JSON의 imageName이 "Pepe"라면 Resources/Pepe 또는 Resources/Sprites/Pepe 여야 함.
        // 여기서는 편의상 Resources 루트나 Sprites 하위 둘 다 찾도록 처리 가능하지만,
        // 표준적으로 Resources/Sprites/ 폴더를 권장합니다.
        
        Sprite loadedSprite = Resources.Load<Sprite>($"Sprites/{item.imageName}");
        if (loadedSprite == null)
        {
            // 경로가 안 맞을 수 있으니 루트에서도 찾아봄
            loadedSprite = Resources.Load<Sprite>(item.imageName);
        }

        if (loadedSprite != null)
        {
            config.puzzleData.sourceImage = loadedSprite.texture;
        }
        else
        {
            Debug.LogError($"LevelManager: 이미지 '{item.imageName}'를 Resources 폴더에서 찾을 수 없습니다!");
        }

        // 캐시에 저장
        _levelCache[levelNumber] = config;

        return config;
    }
}