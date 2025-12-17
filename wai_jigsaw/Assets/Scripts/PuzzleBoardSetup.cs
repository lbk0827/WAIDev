using UnityEngine;
using System.Collections.Generic;

public class PuzzleBoardSetup : MonoBehaviour
{
    [Header("설정: 플레이할 퍼즐 데이터")]
    public PuzzleData currentPuzzleData; // 방금 만든 데이터 파일을 여기에 연결

    [Header("설정: 난이도 선택")]
    public PuzzleDifficulty difficulty = PuzzleDifficulty.Easy; // 드롭다운으로 선택 가능

    [Header("화면 여백")]
    [Range(0.1f, 2.0f)]
    public float padding = 0.5f;

    // 내부적으로 사용할 변수들
    private List<Sprite> _currentPieces;
    private int _rows;
    private int _cols;

    void Start()
    {
        // 1. 선택된 난이도에 따라 데이터 가져오기
        LoadPuzzleData();

        // 2. 조각 생성 및 배치
        CreatePuzzlePieces();

        // 3. 카메라 조정
        FitCameraToPuzzle();
    }

    void LoadPuzzleData()
    {
        if (currentPuzzleData == null)
        {
            Debug.LogError("퍼즐 데이터가 연결되지 않았습니다!");
            return;
        }

        // 난이도(Enum)에 따라 알맞은 리스트와 행/열 개수를 꺼내옵니다.
        switch (difficulty)
        {
            case PuzzleDifficulty.Easy:
                _currentPieces = currentPuzzleData.easyPieces;
                _rows = currentPuzzleData.easyRows;
                _cols = currentPuzzleData.easyCols;
                break;
            case PuzzleDifficulty.Normal:
                _currentPieces = currentPuzzleData.normalPieces;
                _rows = currentPuzzleData.normalRows;
                _cols = currentPuzzleData.normalCols;
                break;
            case PuzzleDifficulty.Hard:
                _currentPieces = currentPuzzleData.hardPieces;
                _rows = currentPuzzleData.hardRows;
                _cols = currentPuzzleData.hardCols;
                break;
        }
    }

    void CreatePuzzlePieces()
    {
        if (_currentPieces == null || _currentPieces.Count == 0) return;
        
        // 기존에 배치된 조각이 있다면 싹 지우고 시작 (재시작 시 필요)
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        float pieceWidth = _currentPieces[0].bounds.size.x;
        float pieceHeight = _currentPieces[0].bounds.size.y;

        float startX = -((_cols * pieceWidth) / 2) + (pieceWidth / 2);
        float startY = ((_rows * pieceHeight) / 2) - (pieceHeight / 2);

        for (int i = 0; i < _currentPieces.Count; i++)
        {
            GameObject newPiece = new GameObject($"Piece_{i}");
            
            // 이미지 설정
            SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
            sr.sprite = _currentPieces[i];

            // 컴포넌트 추가
            newPiece.AddComponent<BoxCollider2D>();
            newPiece.AddComponent<DragController>();

            // 위치 계산 (행/열 계산 시 cols 변수 사용 주의)
            int col = i % _cols;
            int row = i / _cols;

            float posX = startX + (col * pieceWidth);
            float posY = startY - (row * pieceHeight);

            newPiece.transform.position = new Vector3(posX, posY, 0);
            newPiece.transform.parent = this.transform;
        }
    }

    void FitCameraToPuzzle()
    {
        if (_currentPieces == null || _currentPieces.Count == 0) return;

        float pieceWidth = _currentPieces[0].bounds.size.x;
        float pieceHeight = _currentPieces[0].bounds.size.y;

        float totalBoardWidth = _cols * pieceWidth;
        float totalBoardHeight = _rows * pieceHeight;

        Camera mainCam = Camera.main;
        float screenAspect = mainCam.aspect;

        float sizeBasedOnHeight = (totalBoardHeight / 2) + padding;
        float sizeBasedOnWidth = ((totalBoardWidth / screenAspect) / 2) + padding;

        mainCam.orthographicSize = Mathf.Max(sizeBasedOnHeight, sizeBasedOnWidth);
    }
}