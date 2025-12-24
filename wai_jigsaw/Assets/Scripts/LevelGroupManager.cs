using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ====== JSON 파싱용 데이터 클래스 ======

[System.Serializable]
public class LevelGroupItem
{
    public int groupId;        // 그룹 ID (1, 2, 3...)
    public int startLevel;     // 시작 레벨 (1, 26, 51...)
    public int endLevel;       // 끝 레벨 (25, 50, 75...)
    public string rewardImage; // 보상 이미지 경로 (Resources 폴더 기준)
}

[System.Serializable]
public class LevelGroupDataWrapper
{
    public List<LevelGroupItem> groups;
}

/// <summary>
/// 레벨 그룹 데이터를 관리합니다.
/// 25개 레벨 = 1개 그룹, 그룹 클리어 시 이미지 완성
/// </summary>
public class LevelGroupManager : MonoBehaviour
{
    // JSON 파일 이름 (Resources 폴더 내)
    private const string JSON_FILE_NAME = "LevelGroupData";
    private const int GRID_SIZE = 5; // 5x5 그리드

    private LevelGroupDataWrapper _loadedData;
    private Dictionary<int, Sprite[]> _slicedSpritesCache = new Dictionary<int, Sprite[]>();

    private void Awake()
    {
        LoadGroupData();
    }

    /// <summary>
    /// JSON에서 그룹 데이터를 로드합니다.
    /// </summary>
    private void LoadGroupData()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(JSON_FILE_NAME);
        if (jsonFile == null)
        {
            Debug.LogError($"LevelGroupManager: '{JSON_FILE_NAME}.json' 파일을 찾을 수 없습니다!");
            return;
        }

        _loadedData = JsonUtility.FromJson<LevelGroupDataWrapper>(jsonFile.text);
        Debug.Log($"LevelGroupManager: {_loadedData.groups.Count}개의 그룹 데이터를 로드했습니다.");
    }

    /// <summary>
    /// 특정 레벨이 속한 그룹 정보를 반환합니다.
    /// </summary>
    public LevelGroupItem GetGroupForLevel(int levelNumber)
    {
        if (_loadedData == null) LoadGroupData();

        foreach (var group in _loadedData.groups)
        {
            if (levelNumber >= group.startLevel && levelNumber <= group.endLevel)
            {
                return group;
            }
        }

        // 해당 그룹이 없으면 첫 번째 그룹 반환
        Debug.LogWarning($"레벨 {levelNumber}에 해당하는 그룹이 없습니다. 그룹 1을 반환합니다.");
        return _loadedData.groups[0];
    }

    /// <summary>
    /// 그룹의 보상 이미지를 25개(5x5)의 스프라이트 조각으로 분할합니다.
    /// </summary>
    /// <param name="group">분할할 그룹 정보</param>
    /// <returns>25개의 스프라이트 배열 (좌상단부터 우하단 순서)</returns>
    public Sprite[] GetSlicedSprites(LevelGroupItem group)
    {
        // 이미 캐싱되어 있다면 캐시 반환
        if (_slicedSpritesCache.ContainsKey(group.groupId))
        {
            return _slicedSpritesCache[group.groupId];
        }

        // 텍스처 로드
        Texture2D texture = LoadRewardTexture(group.rewardImage);
        if (texture == null)
        {
            Debug.LogError($"보상 이미지를 로드할 수 없습니다: {group.rewardImage}");
            return null;
        }

        // 25개로 분할
        Sprite[] sprites = SliceTexture(texture, GRID_SIZE, GRID_SIZE);

        // 캐시에 저장
        _slicedSpritesCache[group.groupId] = sprites;

        return sprites;
    }

    /// <summary>
    /// Resources 폴더에서 텍스처를 로드합니다.
    /// </summary>
    private Texture2D LoadRewardTexture(string imagePath)
    {
        // 먼저 Sprite로 시도
        Sprite sprite = Resources.Load<Sprite>(imagePath);
        if (sprite != null)
        {
            return sprite.texture;
        }

        // Texture2D로 시도
        Texture2D texture = Resources.Load<Texture2D>(imagePath);
        if (texture != null)
        {
            return texture;
        }

        Debug.LogError($"이미지를 찾을 수 없습니다: {imagePath}");
        return null;
    }

    /// <summary>
    /// 텍스처를 rows x cols 개의 스프라이트로 분할합니다.
    /// </summary>
    private Sprite[] SliceTexture(Texture2D texture, int rows, int cols)
    {
        int totalPieces = rows * cols;
        Sprite[] sprites = new Sprite[totalPieces];

        float pieceWidth = texture.width / (float)cols;
        float pieceHeight = texture.height / (float)rows;

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // 텍스처 좌표 계산 (Unity는 좌하단이 원점)
                // row 0 = 상단, row 4 = 하단이 되도록 y 좌표 뒤집기
                float x = col * pieceWidth;
                float y = (rows - 1 - row) * pieceHeight;

                Rect rect = new Rect(x, y, pieceWidth, pieceHeight);
                Vector2 pivot = new Vector2(0.5f, 0.5f);

                sprites[index] = Sprite.Create(texture, rect, pivot);
                sprites[index].name = $"Piece_{row}_{col}";
                index++;
            }
        }

        return sprites;
    }

    /// <summary>
    /// 그룹 내에서 특정 레벨의 인덱스를 반환합니다. (0 ~ 24)
    /// </summary>
    public int GetLevelIndexInGroup(int levelNumber, LevelGroupItem group)
    {
        return levelNumber - group.startLevel;
    }

    /// <summary>
    /// 모든 그룹 데이터를 반환합니다.
    /// </summary>
    public List<LevelGroupItem> GetAllGroups()
    {
        if (_loadedData == null) LoadGroupData();
        return _loadedData.groups;
    }
}
