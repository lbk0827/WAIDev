using UnityEngine;
using System.Collections.Generic;

public class PuzzleBoardSetup : MonoBehaviour
{
    public LevelDatabase levelDatabase;
    [Range(0.1f, 2.0f)] public float padding = 0.5f;

    // 슬롯의 정답 위치(World Position)를 저장하는 리스트
    private List<Vector3> _slotPositions = new List<Vector3>();
    
    // 현재 보드 상태: index는 슬롯 번호, value는 그 슬롯에 있는 조각
    private List<DragController> _piecesOnBoard = new List<DragController>();

    public void SetupCurrentLevel(int levelNumber)
    {
        LevelConfig config = levelDatabase.GetLevelInfo(levelNumber);

        if (config.puzzleData == null || config.puzzleData.sourceImage == null)
        {
            Debug.LogError($"레벨 {levelNumber}에 이미지가 없습니다!");
            return;
        }

        CreateJigsawPieces(config);
        FitCameraToPuzzle(config.rows, config.cols);
        ShufflePieces();
    }

    void CreateJigsawPieces(LevelConfig config)
    {
        // 초기화
        foreach (Transform child in transform) Destroy(child.gameObject);
        _slotPositions.Clear();
        _piecesOnBoard.Clear();

        Texture2D texture = config.puzzleData.sourceImage;
        int rows = config.rows;
        int cols = config.cols;

        float pieceWidth = texture.width / (float)cols;
        float pieceHeight = texture.height / (float)rows;

        float unitWidth = pieceWidth / 100f; 
        float unitHeight = pieceHeight / 100f;
        
        float startX = -((cols * unitWidth) / 2) + (unitWidth / 2);
        float startY = ((rows * unitHeight) / 2) - (unitHeight / 2);

        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // 1. Sprite 생성
                float x = col * pieceWidth;
                float y = (rows - 1 - row) * pieceHeight;
                Rect rect = new Rect(x, y, pieceWidth, pieceHeight);
                Sprite newSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                // 2. GameObject 생성
                GameObject newPiece = new GameObject($"Piece_{index}");
                newPiece.transform.parent = transform;

                SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                
                newPiece.AddComponent<BoxCollider2D>();
                DragController dragController = newPiece.AddComponent<DragController>();

                // 3. 위치 계산 및 데이터 설정
                float posX = startX + (col * unitWidth);
                float posY = startY - (row * unitHeight);
                Vector3 correctPos = new Vector3(posX, posY, 0);

                // 리스트에 등록
                _slotPositions.Add(correctPos);
                _piecesOnBoard.Add(dragController);

                // DragController 설정
                dragController.board = this;
                dragController.correctSlotIndex = index;
                dragController.currentSlotIndex = index; // 처음엔 정답 위치에 생성

                // 위치 배치
                newPiece.transform.position = correctPos;

                index++;
            }
        }
    }

    // 조각들을 슬롯 위에서 랜덤하게 섞습니다.
    void ShufflePieces()
    {
        // 논리적 리스트 섞기 (Fisher-Yates Shuffle)
        int n = _piecesOnBoard.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            DragController temp = _piecesOnBoard[k];
            _piecesOnBoard[k] = _piecesOnBoard[n];
            _piecesOnBoard[n] = temp;
        }

        // 섞인 논리적 순서대로 물리적 위치와 인덱스 업데이트
        for (int i = 0; i < _piecesOnBoard.Count; i++)
        {
            DragController piece = _piecesOnBoard[i];
            
            // 현재 슬롯 위치로 이동
            piece.transform.position = _slotPositions[i];
            piece.currentSlotIndex = i;

            // 섞인 직후 운 좋게 제자리에 갔다면 바로 고정? 
            // 게임의 재미를 위해 섞을 때는 고정 처리를 하지 않거나, 
            // CheckCompletion을 호출하지 않습니다.
            // 여기서는 단순히 위치만 잡습니다.
        }
    }

    // DragController가 드롭되었을 때 호출됩니다.
    public void OnPieceDropped(DragController droppedPiece)
    {
        // 1. 드롭된 위치에서 가장 가까운 슬롯 찾기
        int targetIndex = GetClosestSlotIndex(droppedPiece.transform.position);

        // 2. 예외 처리: 제자리이거나, 교체 대상이 이미 고정(Locked)된 조각인 경우
        DragController targetPiece = _piecesOnBoard[targetIndex];
        if (targetIndex == droppedPiece.currentSlotIndex || targetPiece.isPlaced)
        {
            // 원래 위치로 되돌아감
            droppedPiece.UpdatePosition(_slotPositions[droppedPiece.currentSlotIndex]);
            return;
        }

        // 3. 교체 로직 (Swap)
        SwapPieces(droppedPiece.currentSlotIndex, targetIndex);

        // 4. 고정 및 정답 확인
        CheckPieceLock(targetIndex); // 드롭된 녀석이 간 곳
        CheckPieceLock(droppedPiece.currentSlotIndex); // 원래 있던 녀석이 간 곳
        
        CheckCompletion();
    }

    // 두 슬롯의 조각을 서로 바꿉니다.
    void SwapPieces(int indexA, int indexB)
    {
        DragController pieceA = _piecesOnBoard[indexA];
        DragController pieceB = _piecesOnBoard[indexB];

        // 리스트 내 교체
        _piecesOnBoard[indexA] = pieceB;
        _piecesOnBoard[indexB] = pieceA;

        // 인덱스 정보 업데이트
        pieceA.currentSlotIndex = indexB;
        pieceB.currentSlotIndex = indexA;

        // 물리적 위치 이동 (애니메이션 없이 즉시 이동)
        pieceA.UpdatePosition(_slotPositions[indexB]);
        pieceB.UpdatePosition(_slotPositions[indexA]);
    }

    int GetClosestSlotIndex(Vector3 pos)
    {
        float minDst = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < _slotPositions.Count; i++)
        {
            float dst = Vector3.Distance(pos, _slotPositions[i]);
            if (dst < minDst)
            {
                minDst = dst;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    void CheckPieceLock(int slotIndex)
    {
        DragController piece = _piecesOnBoard[slotIndex];
        
        // 현재 슬롯이 정답 슬롯과 같다면 고정
        if (piece.correctSlotIndex == piece.currentSlotIndex)
        {
            if (!piece.isPlaced)
            {
                piece.LockPiece();
                // 효과음 재생 등을 여기서 할 수 있음
                // Debug.Log($"Piece {piece.correctSlotIndex} Fixed!");
            }
        }
    }

    public void CheckCompletion()
    {
        foreach (var piece in _piecesOnBoard)
        {
            // 아직 제자리가 아닌 조각이 있다면 종료
            if (piece.currentSlotIndex != piece.correctSlotIndex) return;
        }

        Debug.Log("🎉 레벨 클리어! 🎉");
        Invoke(nameof(LevelComplete), 1.0f);
    }

    void FitCameraToPuzzle(int rows, int cols)
    {
        if (_piecesOnBoard.Count == 0) return;

        SpriteRenderer sr = _piecesOnBoard[0].GetComponent<SpriteRenderer>();
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
        GameManager.Instance.OnLevelComplete();
    }

    public void ClearBoard()
    {
        CancelInvoke(nameof(LevelComplete));
        foreach (Transform child in transform) Destroy(child.gameObject);
        _piecesOnBoard.Clear();
        _slotPositions.Clear();
    }
}