using UnityEngine;
using System.Collections.Generic;

public class PuzzleBoardSetup : MonoBehaviour
{
    public LevelDatabase levelDatabase;
    [Range(0.1f, 2.0f)] public float padding = 0.5f;

    private List<Vector3> _slotPositions = new List<Vector3>();
    private List<DragController> _piecesOnBoard = new List<DragController>();
    
    // Grid dimensions
    private int _rows;
    private int _cols;

    public void SetupCurrentLevel(int levelNumber)
    {
        LevelConfig config = levelDatabase.GetLevelInfo(levelNumber);
        if (config.puzzleData == null || config.puzzleData.sourceImage == null) return;

        CreateJigsawPieces(config);
        FitCameraToPuzzle(config.rows, config.cols);
        ShufflePieces();
    }

    void CreateJigsawPieces(LevelConfig config)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        _slotPositions.Clear();
        _piecesOnBoard.Clear();
        _rows = config.rows;
        _cols = config.cols;

        Texture2D texture = config.puzzleData.sourceImage;
        float pieceWidth = texture.width / (float)_cols;
        float pieceHeight = texture.height / (float)_rows;
        float unitWidth = pieceWidth / 100f; 
        float unitHeight = pieceHeight / 100f;
        
        float startX = -((_cols * unitWidth) / 2) + (unitWidth / 2);
        float startY = ((_rows * unitHeight) / 2) - (unitHeight / 2);

        int index = 0;
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                // Create logic same as before...
                float x = col * pieceWidth;
                float y = (_rows - 1 - row) * pieceHeight;
                Rect rect = new Rect(x, y, pieceWidth, pieceHeight);
                Sprite newSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                GameObject newPiece = new GameObject($"Piece_{row}_{col}");
                newPiece.transform.parent = transform;

                SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                newPiece.AddComponent<BoxCollider2D>();
                DragController dragController = newPiece.AddComponent<DragController>();

                float posX = startX + (col * unitWidth);
                float posY = startY - (row * unitHeight);
                Vector3 correctPos = new Vector3(posX, posY, 0);

                _slotPositions.Add(correctPos);
                _piecesOnBoard.Add(dragController);

                dragController.board = this;
                dragController.currentSlotIndex = index;
                // Save original grid coordinates for neighbor checking
                dragController.originalGridX = col;
                dragController.originalGridY = row; 

                newPiece.transform.position = correctPos;
                index++;
            }
        }
    }

    void ShufflePieces()
    {
        // Simple shuffle of contents
        int n = _piecesOnBoard.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            DragController temp = _piecesOnBoard[k];
            _piecesOnBoard[k] = _piecesOnBoard[n];
            _piecesOnBoard[n] = temp;
        }

        // Apply positions
        for (int i = 0; i < _piecesOnBoard.Count; i++)
        {
            _piecesOnBoard[i].currentSlotIndex = i;
            _piecesOnBoard[i].UpdatePosition(_slotPositions[i]);
        }
    }
    
    public void OnPieceDragStart(DragController piece)
    {
        // Optional: Highlight slots or sound effect
    }

    // 트랜잭션 처리를 위한 구조체
    private struct PieceSwapInfo
    {
        public DragController Piece;
        public int TargetSlotIndex;
    }

    public void OnPieceDropped(DragController rootPiece)
    {
        // 1. [계산 단계] 드롭된 위치 기준 이동량(Shift) 계산
        int targetRootIndex = GetClosestSlotIndex(rootPiece.transform.position);
        int rootOldIndex = rootPiece.currentSlotIndex;
        
        int oldRow = rootOldIndex / _cols;
        int oldCol = rootOldIndex % _cols;
        int newRow = targetRootIndex / _cols;
        int newCol = targetRootIndex % _cols;
        
        int rowShift = newRow - oldRow;
        int colShift = newCol - oldCol;

        // 이동량이 없으면 제자리 복귀
        if (rowShift == 0 && colShift == 0)
        {
            ReturnGroupToCurrentSlots(rootPiece.group);
            return;
        }

        List<DragController> movingGroup = rootPiece.group.pieces;
        List<PieceSwapInfo> transactionList = new List<PieceSwapInfo>();
        HashSet<int> targetSlotSet = new HashSet<int>();

        // 2. [가상 매핑 1단계] 이동 그룹(M)의 목표 슬롯(T) 계산 및 유효성 검사
        foreach (var movingPiece in movingGroup)
        {
            int currentSlot = movingPiece.currentSlotIndex;
            int r = currentSlot / _cols;
            int c = currentSlot % _cols;
            
            int tr = r + rowShift;
            int tc = c + colShift;

            // 보드 이탈 검사
            if (tr < 0 || tr >= _rows || tc < 0 || tc >= _cols)
            {
                ReturnGroupToCurrentSlots(rootPiece.group);
                return;
            }

            int targetSlot = tr * _cols + tc;
            targetSlotSet.Add(targetSlot);
            
            // 이동 그룹의 트랜잭션 등록
            transactionList.Add(new PieceSwapInfo { Piece = movingPiece, TargetSlotIndex = targetSlot });
        }

        // 3. [가상 매핑 2단계] 장애물(Obstacle) 처리 및 빈 자리(Vacancy) 추적
        // 장애물은 '목표 슬롯(T)'에 있지만 '이동 그룹(M)'에는 없는 조각들입니다.
        // 이들은 역방향으로 추적하여 'T에 속하지 않는 슬롯(Vacancy)'으로 이동해야 합니다.
        
        foreach (int tSlot in targetSlotSet)
        {
            DragController pieceAtTarget = _piecesOnBoard[tSlot];
            
            // 이동 그룹에 속하지 않은 조각 발견 -> 장애물
            if (!movingGroup.Contains(pieceAtTarget))
            {
                // 역추적 시작 (Backtracking)
                int currSlot = tSlot;
                
                // 안전장치: 무한 루프 방지 (최대 맵 크기만큼만 반복)
                int safetyCount = 0;
                int maxIterations = _rows * _cols;

                while (targetSlotSet.Contains(currSlot) && safetyCount < maxIterations)
                {
                    int r = currSlot / _cols;
                    int c = currSlot % _cols;
                    
                    // 이동해 온 방향의 반대로 거슬러 올라감
                    int prevR = r - rowShift;
                    int prevC = c - colShift;
                    
                    // 논리적으로 prev 위치는 항상 보드 내부여야 함 (Valid Move의 역산이므로)
                    currSlot = prevR * _cols + prevC;
                    safetyCount++;
                }
                
                // 최종적으로 찾은 빈 자리(Vacancy)로 장애물 이동 예약
                transactionList.Add(new PieceSwapInfo { Piece = pieceAtTarget, TargetSlotIndex = currSlot });
            }
        }

        // 4. [상태 업데이트] 모든 교환 정보 적용
        
        HashSet<PieceGroup> groupsToRepair = new HashSet<PieceGroup>();

        // 4-1. 장애물 그룹 이탈 처리
        foreach (var info in transactionList)
        {
            if (!movingGroup.Contains(info.Piece))
            {
                // BreakFromGroup 하기 전에 기존 그룹을 기록해둡니다.
                // 이 그룹은 멤버를 잃었으므로(fragmented), 연결성 검사가 필요합니다.
                if (info.Piece.group != null)
                {
                    groupsToRepair.Add(info.Piece.group);
                }
                info.Piece.BreakFromGroup();
            }
        }

        // 4-2. 데이터 일괄 갱신
        // 임시 딕셔너리에 먼저 반영하여 덮어쓰기 문제 방지
        Dictionary<int, DragController> nextBoardState = new Dictionary<int, DragController>();
        
        // 변경되는 조각들 반영
        foreach (var info in transactionList)
        {
            nextBoardState[info.TargetSlotIndex] = info.Piece;
            info.Piece.currentSlotIndex = info.TargetSlotIndex;
        }

        // 기존 보드 상태 업데이트 (변경된 부분만)
        foreach (var kvp in nextBoardState)
        {
            _piecesOnBoard[kvp.Key] = kvp.Value;
        }

        // [중요] 멤버를 잃은 그룹들에 대해 연결성 재확인 (Disband & Regroup)
        // 보드 데이터가 갱신된 후(4-2 이후)에 실행해야 올바른 이웃 검사가 가능합니다.
        foreach (var group in groupsToRepair)
        {
            DisbandAndRegroup(group);
        }

        // 4-3. 물리적 위치 이동
        foreach (var info in transactionList)
        {
            info.Piece.UpdatePosition(_slotPositions[info.TargetSlotIndex]);
        }

        // 5. 결합 및 완료 체크
        CheckConnections(rootPiece.group);
        CheckCompletion();
    }

    void ReturnGroupToCurrentSlots(PieceGroup group)
    {
        foreach(var piece in group.pieces)
        {
            piece.UpdatePosition(_slotPositions[piece.currentSlotIndex]);
        }
    }

    void DisbandAndRegroup(PieceGroup group)
    {
        if (group.pieces.Count == 0) return;

        List<DragController> allPieces = new List<DragController>(group.pieces);
        group.pieces.Clear();

        // 1. Reset everyone to individual groups
        foreach (var p in allPieces)
        {
            p.group = new PieceGroup();
            p.group.AddPiece(p);
            p.UpdateVisuals();
        }

        // 2. Try to reconnect them
        HashSet<DragController> processed = new HashSet<DragController>();
        foreach (var p in allPieces)
        {
            if (processed.Contains(p)) continue;

            CheckConnections(p.group);

            foreach (var member in p.group.pieces)
            {
                processed.Add(member);
            }
        }
    }

    void CheckConnections(PieceGroup group)
    {
        // Iterate through all pieces in the group
        // Check their Neighbors (Up, Down, Left, Right)
        // If Neighbor is the Correct Neighbor (based on OriginalGrid coordinates), Merge.

        // We use a copy of the list because the group will grow during iteration
        List<DragController> piecesToCheck = new List<DragController>(group.pieces);

        foreach (var piece in piecesToCheck)
        {
            CheckNeighbor(piece, 0, -1); // Top (Row -1)
            CheckNeighbor(piece, 0, 1);  // Bottom (Row +1)
            CheckNeighbor(piece, -1, 0); // Left (Col -1)
            CheckNeighbor(piece, 1, 0);  // Right (Col +1)
        }
    }

    void CheckNeighbor(DragController piece, int colOffset, int rowOffset)
    {
        // 1. Calculate Target Grid Coordinates (Where the neighbor SHOULD be in the board)
        int currentBoardIndex = piece.currentSlotIndex;
        int currentRow = currentBoardIndex / _cols;
        int currentCol = currentBoardIndex % _cols;

        int targetRow = currentRow + rowOffset;
        int targetCol = currentCol + colOffset;

        // Boundary check
        if (targetRow < 0 || targetRow >= _rows || targetCol < 0 || targetCol >= _cols) return;

        int targetIndex = targetRow * _cols + targetCol;
        DragController neighbor = _piecesOnBoard[targetIndex];

        // 2. Check if this neighbor is the *Correct* one
        // Their original coordinates should differ by exactly (colOffset, rowOffset)
        if (neighbor.originalGridX == piece.originalGridX + colOffset &&
            neighbor.originalGridY == piece.originalGridY + rowOffset)
        {
            // They are correct neighbors!
            
            // 3. Merge Groups
            if (piece.group != neighbor.group)
            {
                piece.group.MergeGroup(neighbor.group);
                // Play sound?
            }

            // 4. Update Visuals (Hide Borders)
            // 0:Top, 1:Bottom, 2:Left, 3:Right
            // rowOffset -1 = Top, 1 = Bottom
            // colOffset -1 = Left, 1 = Right
            
            if (rowOffset == -1) { piece.HideBorder(0); neighbor.HideBorder(1); } // My Top, Their Bottom
            if (rowOffset == 1)  { piece.HideBorder(1); neighbor.HideBorder(0); } // My Bottom, Their Top
            if (colOffset == -1) { piece.HideBorder(2); neighbor.HideBorder(3); } // My Left, Their Right
            if (colOffset == 1)  { piece.HideBorder(3); neighbor.HideBorder(2); } // My Right, Their Left
        }
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
    
    public void CheckCompletion()
    {
        // Check if all pieces form a SINGLE group
        if (_piecesOnBoard.Count == 0) return;
        
        PieceGroup firstGroup = _piecesOnBoard[0].group;
        if (firstGroup.pieces.Count != _piecesOnBoard.Count) return;

        // Check if the group is in the correct internal order (already done by Merge logic essentially)
        // But we also need to check if the group is rotated? No, no rotation.
        // Just check if the first piece is at a valid index? 
        // Actually, if they are all one group, and we only merge correct neighbors, 
        // then the puzzle IS solved relative to itself.
        // But is it in the center? Doesn't matter for "Completion", but usually users want it centered.
        // The previous logic checked `correctSlotIndex`.
        // If we want "True Completion", every piece must be in `correctSlotIndex`.
        // If the user built the puzzle but it's shifted 1 tile to the right, is it solved?
        // Usually NO. It must be in the frame.
        
        foreach (var piece in _piecesOnBoard)
        {
            int correctIndex = piece.originalGridY * _cols + piece.originalGridX;
            if (piece.currentSlotIndex != correctIndex) return;
        }

        Debug.Log("🎉 레벨 클리어! 🎉");
        Invoke(nameof(LevelComplete), 1.0f);
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