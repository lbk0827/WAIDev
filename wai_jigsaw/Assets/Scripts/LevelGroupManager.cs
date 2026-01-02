using UnityEngine;
using System.Collections.Generic;
using WaiJigsaw.Data;

/// <summary>
/// 레벨 그룹 데이터를 관리합니다.
/// 25개 레벨 = 1개 그룹, 그룹 클리어 시 이미지 완성
/// </summary>
public class LevelGroupManager : MonoBehaviour
{
    public static LevelGroupManager Instance { get; private set; }

    private const int GRID_SIZE = 5; // 5x5 그리드

    private Dictionary<int, Sprite[]> _slicedSpritesCache = new Dictionary<int, Sprite[]>();

    private void Awake()
    {
        // 싱글턴 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // LevelGroupTable 초기화
        LevelGroupTable.Load();
    }

    /// <summary>
    /// 특정 레벨이 속한 그룹 정보를 반환합니다.
    /// </summary>
    public LevelGroupTableRecord GetGroupForLevel(int levelNumber)
    {
        return LevelGroupTable.GetGroupForLevel(levelNumber);
    }

    /// <summary>
    /// 그룹의 보상 이미지를 25개(5x5)의 스프라이트 조각으로 분할합니다.
    /// </summary>
    /// <param name="group">분할할 그룹 정보</param>
    /// <returns>25개의 스프라이트 배열 (좌상단부터 우하단 순서)</returns>
    public Sprite[] GetSlicedSprites(LevelGroupTableRecord group)
    {
        // 이미 캐싱되어 있다면 캐시 반환
        if (_slicedSpritesCache.ContainsKey(group.GroupID))
        {
            return _slicedSpritesCache[group.GroupID];
        }

        // 텍스처 로드
        Texture2D texture = LoadRewardTexture(group.ImageName);
        if (texture == null)
        {
            Debug.LogError($"보상 이미지를 로드할 수 없습니다: {group.ImageName}");
            return null;
        }

        // 25개로 분할
        Sprite[] sprites = SliceTexture(texture, GRID_SIZE, GRID_SIZE);

        // 캐시에 저장
        _slicedSpritesCache[group.GroupID] = sprites;

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
    public int GetLevelIndexInGroup(int levelNumber, LevelGroupTableRecord group)
    {
        return levelNumber - group.StartLevel;
    }

    /// <summary>
    /// 모든 그룹 데이터를 반환합니다.
    /// </summary>
    public List<LevelGroupTableRecord> GetAllGroups()
    {
        return LevelGroupTable.GetAll();
    }
}
