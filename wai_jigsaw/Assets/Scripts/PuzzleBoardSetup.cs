using UnityEngine;
using System.Collections.Generic;

public class PuzzleBoardSetup : MonoBehaviour
{
    public LevelDatabase levelDatabase;
    [Range(0.1f, 2.0f)] public float padding = 0.5f;

    // 내부 변수
    private List<GameObject> _pieces = new List<GameObject>();

    public void SetupCurrentLevel(int levelNumber)
    {
        // 1. 레벨 정보 가져오기
        LevelConfig config = levelDatabase.GetLevelInfo(levelNumber);

        if (config.puzzleData == null || config.puzzleData.sourceImage == null)
        {
            Debug.LogError($"레벨 {levelNumber}에 이미지가 없습니다!");
            return;
        }

        // 2. 이미지 자동 자르기 및 생성
        CreateJigsawPieces(config);

        // 3. 카메라 조정 (섞기 전에 카메라 크기를 먼저 맞춰야 영역이 정확합니다)
        FitCameraToPuzzle(config.rows, config.cols);

        // 4. 조각 섞기
        ShufflePieces();
    }

    // ★ 핵심 기능: 이미지를 코드로 잘라서 조각 생성
    void CreateJigsawPieces(LevelConfig config)
    {
        // 기존 조각 청소
        foreach (Transform child in transform) Destroy(child.gameObject);
        _pieces.Clear();

        Texture2D texture = config.puzzleData.sourceImage;
        int rows = config.rows;
        int cols = config.cols;

        // 조각 하나의 크기 계산 (전체 이미지 크기 / 개수)
        float pieceWidth = texture.width / (float)cols;
        float pieceHeight = texture.height / (float)rows;

        // 배치 시작 위치 계산 (중앙 정렬용)
        // Unity Unit 단위로 변환 (Pixels Per Unit 기본값 100 가정)
        float unitWidth = pieceWidth / 100f; 
        float unitHeight = pieceHeight / 100f;
        
        float startX = -((cols * unitWidth) / 2) + (unitWidth / 2);
        float startY = ((rows * unitHeight) / 2) - (unitHeight / 2);

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // 1. 텍스처에서 잘라낼 영역(Rect) 계산
                // 텍스처 좌표계는 (0,0)이 왼쪽 아래입니다. 위에서부터 자르려면 Y 계산 주의.
                float x = col * pieceWidth;
                float y = (rows - 1 - row) * pieceHeight; // 위에서 아래로 순서 맞춤

                Rect rect = new Rect(x, y, pieceWidth, pieceHeight);

                // 2. Sprite 생성 (자르기)
                Sprite newSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                // 3. 게임 오브젝트 생성
                GameObject newPiece = new GameObject($"Piece_{row}_{col}");
                newPiece.transform.parent = transform;

                // 4. 컴포넌트 부착
                SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                
                newPiece.AddComponent<BoxCollider2D>();
                DragController dragController = newPiece.AddComponent<DragController>();

                // 5. 정답 위치 계산 및 할당
                float posX = startX + (col * unitWidth);
                float posY = startY - (row * unitHeight);

                // DragController에 정답 위치와 보드(자기 자신) 참조를 알려줍니다.
                dragController.correctPosition = new Vector3(posX, posY, 0);
                dragController.board = this;

                // (임시) 생성 시에는 정답 위치에 먼저 배치합니다.
                // 이 위치는 잠시 후 ShufflePieces()에 의해 랜덤 위치로 변경됩니다.
                newPiece.transform.position = new Vector3(posX, posY, 0);
                
                _pieces.Add(newPiece);
            }
        }
    }

    // ★ 개선된 기능: 조각들을 격자(Grid) 형태로 정렬하여 배치합니다.
    void ShufflePieces()
    {
        // 1. 조각 순서 섞기 (List 사용)
        List<GameObject> shuffledList = new List<GameObject>(_pieces);
        for (int i = 0; i < shuffledList.Count; i++)
        {
            GameObject temp = shuffledList[i];
            int randomIndex = Random.Range(i, shuffledList.Count);
            shuffledList[i] = shuffledList[randomIndex];
            shuffledList[randomIndex] = temp;
        }

        // 2. 격자 배치 설정
        Camera mainCam = Camera.main;
        float camHeight = mainCam.orthographicSize * 2;
        float camWidth = camHeight * mainCam.aspect;

        // 조각 크기 확인 (첫 번째 조각 기준)
        SpriteRenderer sr = _pieces[0].GetComponent<SpriteRenderer>();
        float pieceW = sr.bounds.size.x;
        float pieceH = sr.bounds.size.y;

        // 격자 열 개수 결정 (화면 너비에 맞춰 적절히 배치)
        int gridCols = Mathf.Max(3, Mathf.FloorToInt(camWidth / (pieceW * 1.1f)));
        float spacing = 0.1f; // 조각 사이 간격

        // 시작 위치 계산 (화면 왼쪽 하단 부근)
        float startX = -(gridCols - 1) * (pieceW + spacing) / 2f;
        float startY = -(camHeight / 2f) + (pieceH / 2f) + padding;

        // 3. 조각 배치
        for (int i = 0; i < shuffledList.Count; i++)
        {
            int row = i / gridCols;
            int col = i % gridCols;

            float posX = startX + col * (pieceW + spacing);
            float posY = startY + row * (pieceH + spacing);

            shuffledList[i].transform.position = new Vector3(posX, posY, 0);
        }
    }
    
    // ★ 추가된 기능: 모든 조각이 맞춰졌는지 검사합니다.
    public void CheckCompletion()
    {
        foreach (var piece in _pieces)
        {
            // 단 하나의 조각이라도 제자리에 놓여있지 않다면, 함수를 즉시 종료합니다.
            if (!piece.GetComponent<DragController>().isPlaced)
            {
                return;
            }
        }

        // 모든 조각이 제자리에 놓였다면, 이 코드가 실행됩니다.
        Debug.Log("🎉 레벨 클리어! 🎉");
        
        // 다음 레벨로 넘어가는 기존 로직을 호출합니다.
        // 약간의 딜레이를 주어 완성된 그림을 볼 시간을 줍니다.
        Invoke(nameof(LevelComplete), 1.5f);
    }

    void FitCameraToPuzzle(int rows, int cols)
    {
        if (_pieces.Count == 0) return;

        // 첫 번째 조각의 크기로 전체 크기 유추
        SpriteRenderer sr = _pieces[0].GetComponent<SpriteRenderer>();
        float pieceW = sr.bounds.size.x;
        float pieceH = sr.bounds.size.y;

        float totalW = cols * pieceW;
        float totalH = rows * pieceH;

        Camera mainCam = Camera.main;
        float screenAspect = mainCam.aspect;
        float sizeH = (totalH / 2) + padding;
        float sizeW = ((totalW / screenAspect) / 2) + padding;

        mainCam.orthographicSize = Mathf.Max(sizeH, sizeW);
    }
    
    public void LevelComplete()
    {
        // GameManager에게 레벨이 완료되었음을 알립니다.
        GameManager.Instance.OnLevelComplete();
    }

    public void ClearBoard()
    {
        // 혹시 실행 중인 Invoke가 있다면 취소합니다.
        CancelInvoke(nameof(LevelComplete));

        // 모든 조각을 파괴하고 리스트를 비웁니다.
        foreach (Transform child in transform) Destroy(child.gameObject);
        _pieces.Clear();
    }
}