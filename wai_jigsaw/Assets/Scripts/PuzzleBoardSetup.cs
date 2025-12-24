using UnityEngine;
using System.Collections.Generic;

public class PuzzleBoardSetup : MonoBehaviour
{
    public LevelManager levelManager;
    [Range(0.1f, 2.0f)] public float padding = 0.5f;

    [Header("Piece Spacing")]
    [Tooltip("그룹화되지 않은 조각들 사이의 간격")]
    [Range(0f, 0.2f)] public float pieceSpacing = 0.08f;

    private List<Vector3> _slotPositions = new List<Vector3>();
    private List<DragController> _piecesOnBoard = new List<DragController>();

    // Grid dimensions
    private int _rows;
    private int _cols;

    // 조각 크기 (spacing 계산용)
    private float _unitWidth;
    private float _unitHeight;

    public void SetupCurrentLevel(int levelNumber)
    {
        LevelConfig config = levelManager.GetLevelInfo(levelNumber);
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

        // Sprite.Create 기본 PPU=100 기준 Unity Unit 크기
        _unitWidth = pieceWidth / 100f;
        _unitHeight = pieceHeight / 100f;

        // spacing 포함한 슬롯 간격
        float slotWidth = _unitWidth + pieceSpacing;
        float slotHeight = _unitHeight + pieceSpacing;

        // 퍼즐 시작점 (좌상단 기준, 중앙 정렬) - spacing 포함
        float startX = -((_cols * slotWidth) / 2) + (slotWidth / 2);
        float startY = ((_rows * slotHeight) / 2) - (slotHeight / 2);

        int index = 0;
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                // 스프라이트 잘라내기
                float x = col * pieceWidth;
                float y = (_rows - 1 - row) * pieceHeight;
                Rect rect = new Rect(x, y, pieceWidth, pieceHeight);
                Sprite newSprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f));

                // 오브젝트 생성
                GameObject newPiece = new GameObject($"Piece_{row}_{col}");
                newPiece.transform.parent = transform;

                SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                sr.sortingOrder = 1;

                newPiece.AddComponent<BoxCollider2D>();
                DragController dragController = newPiece.AddComponent<DragController>();

                // 위치 설정 (spacing 포함)
                float posX = startX + (col * slotWidth);
                float posY = startY - (row * slotHeight);
                Vector3 slotPos = new Vector3(posX, posY, 0);

                _slotPositions.Add(slotPos);
                _piecesOnBoard.Add(dragController);

                dragController.board = this;
                dragController.currentSlotIndex = index;
                dragController.originalGridX = col;
                dragController.originalGridY = row;

                // 조각 크기 정보 전달 (그룹화 시 위치 조정용)
                dragController.pieceWidth = _unitWidth;
                dragController.pieceHeight = _unitHeight;

                newPiece.transform.position = slotPos;
                index++;
            }
        }
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

        // 퍼즐이 화면에 맞도록 카메라 orthographicSize 계산
        float sizeH = (totalH / 2) + padding;
        float sizeW = ((totalW / screenAspect) / 2) + padding;

        mainCam.orthographicSize = Mathf.Max(sizeH, sizeW);
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

        // [중요] 셔플 후 이미 인접한 정답 조각들을 그룹화
        CheckInitialConnections();
    }

    /// <summary>
    /// 셔플 후 초기 상태에서 이미 맞춰진 조각들을 그룹화합니다.
    /// </summary>
    void CheckInitialConnections()
    {
        // 모든 조각에 대해 연결 체크 (연쇄 병합 적용)
        HashSet<DragController> processed = new HashSet<DragController>();

        foreach (var piece in _piecesOnBoard)
        {
            if (processed.Contains(piece)) continue;

            // 이 조각의 그룹에 대해 연결 체크 (연쇄 병합)
            CheckConnectionsRecursive(piece.group);

            // 처리된 조각들 기록
            foreach (var member in piece.group.pieces)
            {
                processed.Add(member);
            }
        }

        Debug.Log($"초기 연결 체크 완료. 그룹 수: {CountGroups()}");
    }

    /// <summary>
    /// 현재 보드의 그룹 수를 반환합니다. (디버그용)
    /// </summary>
    int CountGroups()
    {
        HashSet<PieceGroup> groups = new HashSet<PieceGroup>();
        foreach (var piece in _piecesOnBoard)
        {
            groups.Add(piece.group);
        }
        return groups.Count;
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

        // 4-3. 물리적 위치 이동 (그룹 내 상대적 위치 유지)
        MoveGroupWithRelativePositions(rootPiece.group, rootPiece, _slotPositions[rootPiece.currentSlotIndex]);

        // 장애물(스왑된 조각들)은 개별 slot 위치로 이동
        foreach (var info in transactionList)
        {
            if (!movingGroup.Contains(info.Piece))
            {
                info.Piece.UpdatePosition(_slotPositions[info.TargetSlotIndex]);
            }
        }

        // 5. 결합 및 완료 체크 (연쇄 병합 포함)
        CheckConnectionsRecursive(rootPiece.group);
        CheckCompletion();
    }

    void ReturnGroupToCurrentSlots(PieceGroup group)
    {
        if (group.pieces.Count == 0) return;

        // 그룹의 첫 번째 조각을 기준으로 상대적 위치 유지하며 이동
        DragController anchorPiece = group.pieces[0];
        MoveGroupWithRelativePositions(group, anchorPiece, _slotPositions[anchorPiece.currentSlotIndex]);
    }

    /// <summary>
    /// 그룹을 이동할 때 내부 조각들의 상대적 위치(스냅된 상태)를 유지합니다.
    /// </summary>
    void MoveGroupWithRelativePositions(PieceGroup group, DragController anchorPiece, Vector3 anchorTargetPos)
    {
        // anchor 조각의 현재 위치와 목표 위치의 차이 계산
        Vector3 offset = anchorTargetPos - anchorPiece.transform.position;

        // 그룹 내 모든 조각을 동일한 offset만큼 이동
        foreach (var piece in group.pieces)
        {
            piece.UpdatePosition(piece.transform.position + offset);
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

        // 2. Try to reconnect them (연쇄 병합 적용)
        HashSet<DragController> processed = new HashSet<DragController>();
        foreach (var p in allPieces)
        {
            if (processed.Contains(p)) continue;

            CheckConnectionsRecursive(p.group);

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

    /// <summary>
    /// 연쇄 병합을 처리합니다. 그룹이 커질 때마다 새로 추가된 조각들도 이웃 체크를 합니다.
    /// </summary>
    void CheckConnectionsRecursive(PieceGroup group)
    {
        HashSet<DragController> checkedPieces = new HashSet<DragController>();
        Queue<DragController> toCheck = new Queue<DragController>();

        // 초기 그룹의 모든 조각을 큐에 추가
        foreach (var piece in group.pieces)
        {
            toCheck.Enqueue(piece);
        }

        while (toCheck.Count > 0)
        {
            DragController piece = toCheck.Dequeue();

            // 이미 체크한 조각은 스킵
            if (checkedPieces.Contains(piece)) continue;
            checkedPieces.Add(piece);

            int prevGroupSize = group.pieces.Count;

            // 4방향 이웃 체크
            CheckNeighbor(piece, 0, -1); // Top
            CheckNeighbor(piece, 0, 1);  // Bottom
            CheckNeighbor(piece, -1, 0); // Left
            CheckNeighbor(piece, 1, 0);  // Right

            // 그룹에 새 조각이 추가되었으면, 아직 체크하지 않은 조각들을 큐에 추가
            if (group.pieces.Count > prevGroupSize)
            {
                foreach (var newPiece in group.pieces)
                {
                    if (!checkedPieces.Contains(newPiece) && !toCheck.Contains(newPiece))
                    {
                        toCheck.Enqueue(newPiece);
                    }
                }
            }
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

            // 3. Merge Groups (스냅하여 spacing 제거)
            if (piece.group != neighbor.group)
            {
                piece.group.MergeGroupWithSnap(neighbor.group, piece, neighbor);
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

    // ====== 디버그 기능 ======

    /// <summary>
    /// [디버그] 퍼즐을 자동으로 완성합니다.
    /// Unity 에디터에서 Inspector 우클릭 메뉴 또는 키보드로 호출
    /// </summary>
    [ContextMenu("Debug: Auto Complete Puzzle")]
    public void DebugAutoComplete()
    {
        if (_piecesOnBoard.Count == 0)
        {
            Debug.LogWarning("퍼즐이 생성되지 않았습니다.");
            return;
        }

        Debug.Log("🔧 디버그: 퍼즐 자동 완성 시작...");

        // 완성된 퍼즐의 시작점 계산 (spacing 없이)
        float startX = -((_cols * _unitWidth) / 2) + (_unitWidth / 2);
        float startY = ((_rows * _unitHeight) / 2) - (_unitHeight / 2);

        // 모든 조각을 원래 위치로 이동 (spacing 없이 밀착)
        foreach (var piece in _piecesOnBoard)
        {
            int correctIndex = piece.originalGridY * _cols + piece.originalGridX;
            piece.currentSlotIndex = correctIndex;

            // spacing 없는 정확한 위치 계산
            float posX = startX + (piece.originalGridX * _unitWidth);
            float posY = startY - (piece.originalGridY * _unitHeight);
            piece.UpdatePosition(new Vector3(posX, posY, 0));
        }

        // 보드 상태 재정렬
        List<DragController> sortedPieces = new List<DragController>(_piecesOnBoard);
        sortedPieces.Sort((a, b) =>
        {
            int indexA = a.originalGridY * _cols + a.originalGridX;
            int indexB = b.originalGridY * _cols + b.originalGridX;
            return indexA.CompareTo(indexB);
        });

        for (int i = 0; i < sortedPieces.Count; i++)
        {
            _piecesOnBoard[i] = sortedPieces[i];
        }

        // 모든 조각을 하나의 그룹으로 합치기
        PieceGroup mainGroup = _piecesOnBoard[0].group;
        for (int i = 1; i < _piecesOnBoard.Count; i++)
        {
            if (_piecesOnBoard[i].group != mainGroup)
            {
                mainGroup.MergeGroup(_piecesOnBoard[i].group);
            }
        }

        // 테두리 업데이트 (인접한 조각 간 테두리 숨기기)
        foreach (var piece in _piecesOnBoard)
        {
            CheckNeighbor(piece, 0, -1);
            CheckNeighbor(piece, 0, 1);
            CheckNeighbor(piece, -1, 0);
            CheckNeighbor(piece, 1, 0);
        }

        Debug.Log("🔧 디버그: 퍼즐 자동 완성됨. 완료 체크 실행...");

        // 완료 체크
        CheckCompletion();
    }

    private void Update()
    {
        // 디버그 단축키: Shift + C = 자동 완성
        #if UNITY_EDITOR
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.C))
        {
            DebugAutoComplete();
        }
        #endif
    }
}