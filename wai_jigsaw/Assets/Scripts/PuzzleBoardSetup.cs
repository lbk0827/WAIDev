using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WaiJigsaw.Data;

public class PuzzleBoardSetup : MonoBehaviour
{
    [Header("Camera")]
    [Range(0.1f, 2.0f)] public float padding = 0.5f;

    [Header("Board Size")]
    [Tooltip("보드 전체 크기 스케일 (1.0 = 기본, 0.8 = 80% 크기)")]
    [Range(0.5f, 1.2f)] public float boardScale = 1.0f;

    [Header("Piece Spacing")]
    [Tooltip("그룹화되지 않은 조각들 사이의 간격 (셰이더 Padding으로 표현)")]
    [Range(0f, 0.5f)] public float pieceSpacing = 0.15f;

    [Header("Rounded Corners")]
    [Tooltip("퍼즐 조각의 둥근 모서리 반경 (0 = 사각형, 0.5 = 최대 둥글기)")]
    [Range(0f, 0.5f)] public float cornerRadius = 0.05f;
    [Tooltip("둥근 모서리 셰이더 (Assets/Shaders/RoundedSprite.shader)")]
    public Shader roundedCornerShader;

    [Header("Card Slot")]
    [Tooltip("카드 슬롯 배경 색상")]
    public Color slotBackgroundColor = new Color(0.85f, 0.85f, 0.85f, 1f);  // 밝은 회색
    [Tooltip("슬롯 크기 비율 (1.0 = 시각적 카드 크기와 동일)")]
    [Range(0.5f, 1.2f)] public float slotSizeRatio = 1.0f;
    [Tooltip("슬롯 테두리 두께 (슬롯 크기 대비 비율)")]
    [Range(0.005f, 0.05f)] public float slotBorderThickness = 0.015f;
    [Tooltip("슬롯 테두리 색상")]
    public Color slotBorderColor = new Color(0.2f, 0.2f, 0.2f, 1f);  // 진한 회색/검은색

    [Header("Card Border (Frame)")]
    [Tooltip("하얀 테두리 두께 (조각 크기 대비 비율)")]
    [Range(0.01f, 0.1f)] public float whiteBorderRatio = 0.025f;
    [Tooltip("검정 테두리 두께 (조각 크기 대비 비율)")]
    [Range(0.005f, 0.05f)] public float blackBorderRatio = 0.008f;

    [Header("Card Intro Animation")]
    [Tooltip("카드가 날아가는 속도 (초)")]
    [Range(0.1f, 1.0f)] public float cardFlyDuration = 0.3f;
    [Tooltip("카드 사이의 딜레이 (초)")]
    [Range(0.01f, 0.2f)] public float cardFlyDelay = 0.05f;
    [Tooltip("카드 뒤집기 딜레이 (초)")]
    [Range(0.01f, 0.1f)] public float cardFlipDelay = 0.03f;
    [Tooltip("카드 뒤집기 시간 (초)")]
    [Range(0.1f, 0.5f)] public float cardFlipDuration = 0.25f;

    [Header("Swap Animation")]
    [Tooltip("스왑 시 카드 이동 시간 (초)")]
    [Range(0.1f, 0.8f)] public float swapAnimationDuration = 0.3f;

    [Header("Merge Pumping Animation")]
    [Tooltip("합쳐질 때 펌핑 최대 스케일 (1.0 기준)")]
    [Range(1.0f, 1.3f)] public float pumpingScale = 1.15f;
    [Tooltip("펌핑 애니메이션 시간 (초)")]
    [Range(0.1f, 0.5f)] public float pumpingDuration = 0.2f;

    private List<Vector3> _slotPositions = new List<Vector3>();
    private List<DragController> _piecesOnBoard = new List<DragController>();
    private List<GameObject> _cardSlots = new List<GameObject>();  // 카드 슬롯 배경

    // Grid dimensions
    private int _rows;
    private int _cols;

    // 조각 크기 (spacing 계산용)
    private float _unitWidth;
    private float _unitHeight;

    // 카드 뒷면 스프라이트
    private Sprite _cardBackSprite;

    // 인트로 애니메이션 상태
    private bool _isPlayingIntro = false;

    public void SetupCurrentLevel(int levelNumber)
    {
        LevelConfig config = LevelManager.Instance.GetLevelInfo(levelNumber);
        if (config.puzzleData == null || config.puzzleData.sourceImage == null) return;

        // 카드 뒷면 스프라이트 로드
        LoadCardBackSprite();

        CreateJigsawPieces(config);
        FitCameraToPuzzle(config.rows, config.cols);

        // 인트로 애니메이션 시작
        StartCoroutine(PlayIntroAnimation());
    }

    /// <summary>
    /// CardTable에서 카드 뒷면 스프라이트를 로드합니다.
    /// </summary>
    private void LoadCardBackSprite()
    {
        CardTable.Load();
        _cardBackSprite = CardTable.LoadCardBackSprite(1); // 기본 카드 사용
    }

    void CreateJigsawPieces(LevelConfig config)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        _slotPositions.Clear();
        _piecesOnBoard.Clear();
        _cardSlots.Clear();
        _rows = config.rows;
        _cols = config.cols;

        Texture2D texture = config.puzzleData.sourceImage;
        float pieceWidth = texture.width / (float)_cols;
        float pieceHeight = texture.height / (float)_rows;

        // Sprite.Create 기본 PPU=100 기준 Unity Unit 크기
        _unitWidth = pieceWidth / 100f;
        _unitHeight = pieceHeight / 100f;

        // 슬롯 간격 = 조각 크기 (물리적 간격 없음, EdgeCover로 간격 표현)
        float slotWidth = _unitWidth;
        float slotHeight = _unitHeight;

        // 퍼즐 시작점 (좌상단 기준, 중앙 정렬)
        float startX = -((_cols * slotWidth) / 2) + (slotWidth / 2);
        float startY = ((_rows * slotHeight) / 2) - (slotHeight / 2);

        // 카드 뭉치 위치 (마지막 슬롯 = 오른쪽 하단)
        float deckPosX = startX + ((_cols - 1) * slotWidth);
        float deckPosY = startY - ((_rows - 1) * slotHeight);
        Vector3 deckPosition = new Vector3(deckPosX, deckPosY, 0);

        // 1단계: 카드 슬롯 배경 생성은 카드 생성 후로 이동 (카드의 실제 bounds 크기 참조 필요)

        int index = 0;
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                // 스프라이트 잘라내기 (인접 조각과의 경계선 제거를 위해 1픽셀 오버랩)
                float x = col * pieceWidth;
                float y = (_rows - 1 - row) * pieceHeight;

                // 오버랩 픽셀 (가장자리가 아닌 경우에만 적용)
                float overlapPixels = 1f;
                float overlapLeft = (col > 0) ? overlapPixels : 0;
                float overlapRight = (col < _cols - 1) ? overlapPixels : 0;
                float overlapBottom = (row < _rows - 1) ? overlapPixels : 0;  // row가 작을수록 위쪽, texture y는 아래가 0
                float overlapTop = (row > 0) ? overlapPixels : 0;

                // 텍스처 좌표 확장 (오버랩 적용)
                float rectX = x - overlapLeft;
                float rectY = y - overlapBottom;
                float rectWidth = pieceWidth + overlapLeft + overlapRight;
                float rectHeight = pieceHeight + overlapTop + overlapBottom;

                // 피벗 조정 (오버랩으로 인한 크기 변화 보정)
                float pivotX = (0.5f * pieceWidth + overlapLeft) / rectWidth;
                float pivotY = (0.5f * pieceHeight + overlapBottom) / rectHeight;

                Rect rect = new Rect(rectX, rectY, rectWidth, rectHeight);
                Sprite newSprite = Sprite.Create(texture, rect, new Vector2(pivotX, pivotY));

                // 오브젝트 생성
                GameObject newPiece = new GameObject($"Piece_{row}_{col}");
                newPiece.transform.parent = transform;

                SpriteRenderer sr = newPiece.AddComponent<SpriteRenderer>();
                sr.sprite = newSprite;
                sr.sortingOrder = 1;

                newPiece.AddComponent<BoxCollider2D>();
                DragController dragController = newPiece.AddComponent<DragController>();

                // 슬롯 위치 계산 (spacing 포함)
                float posX = startX + (col * slotWidth);
                float posY = startY - (row * slotHeight);
                Vector3 slotPos = new Vector3(posX, posY, 0);

                _slotPositions.Add(slotPos);
                _piecesOnBoard.Add(dragController);

                dragController.board = this;
                dragController.currentSlotIndex = index;
                dragController.originalGridX = col;
                dragController.originalGridY = row;

                // 조각 크기 정보 전달
                dragController.pieceWidth = _unitWidth;
                dragController.pieceHeight = _unitHeight;

                // 테두리 두께 설정 (프레임 생성 전에 호출)
                dragController.SetBorderThickness(whiteBorderRatio, blackBorderRatio);

                // 카드 비주얼 초기화 (뒷면 상태로 시작)
                dragController.InitializeCardVisuals(_cardBackSprite);

                // 둥근 모서리 셰이더 적용 (Inspector에서 참조한 셰이더 전달)
                dragController.ApplyRoundedCornerShader(cornerRadius, roundedCornerShader);

                // Padding 설정 (spacing의 절반) - EdgeCover 대체
                dragController.SetDefaultPadding(pieceSpacing / 2f);

                // 초기 위치: 카드 뭉치 (오른쪽 하단)
                // 카드가 겹쳐 보이도록 약간의 오프셋 적용
                float stackOffset = index * 0.02f;
                newPiece.transform.position = deckPosition + new Vector3(stackOffset, stackOffset, -index * 0.001f);

                // 인트로 중에는 드래그 불가
                dragController.SetDraggable(false);

                index++;
            }
        }

        // 카드 생성 완료 후 슬롯 생성 (카드의 실제 화면 크기 참조)
        if (_piecesOnBoard.Count > 0)
        {
            SpriteRenderer firstCardSR = _piecesOnBoard[0].GetComponent<SpriteRenderer>();
            Vector3 cardScale = _piecesOnBoard[0].transform.localScale;

            // sr.bounds.size는 빌드에서 localScale을 반영하지 않을 수 있음
            // 따라서 sprite.bounds.size × localScale로 실제 화면 크기 계산
            Vector2 spriteSize = firstCardSR.sprite.bounds.size;
            Vector2 actualCardSize = new Vector2(spriteSize.x * cardScale.x, spriteSize.y * cardScale.y);

            // 슬롯 크기 계산 - 슬롯끼리 맞닿는 느낌으로 하기 위해 pieceSpacing의 절반만 적용
            // 기존: (1f - pieceSpacing) → 변경: (1f - pieceSpacing * 0.3f)
            float slotSpacingFactor = 0.3f;  // pieceSpacing의 30%만 적용 (슬롯을 더 크게)
            float visiblePieceWidth = actualCardSize.x * (1f - pieceSpacing * slotSpacingFactor);
            float visiblePieceHeight = actualCardSize.y * (1f - pieceSpacing * slotSpacingFactor);

            Debug.Log($"[PuzzleBoardSetup] 슬롯 크기 계산 - spriteSize={spriteSize}, cardScale={cardScale}, actualCardSize={actualCardSize}, pieceSpacing={pieceSpacing}, visibleSize=({visiblePieceWidth}, {visiblePieceHeight})");

            CreateCardSlots(startX, startY, slotWidth, slotHeight, visiblePieceWidth, visiblePieceHeight);
        }
    }

    /// <summary>
    /// 카드 슬롯 배경을 생성합니다.
    /// </summary>
    /// <param name="startX">시작 X 좌표</param>
    /// <param name="startY">시작 Y 좌표</param>
    /// <param name="slotWidth">슬롯 간격 (위치 계산용)</param>
    /// <param name="slotHeight">슬롯 간격 (위치 계산용)</param>
    /// <param name="pieceWidth">실제 조각 너비 (슬롯 크기용)</param>
    /// <param name="pieceHeight">실제 조각 높이 (슬롯 크기용)</param>
    void CreateCardSlots(float startX, float startY, float slotWidth, float slotHeight, float pieceWidth, float pieceHeight)
    {
        // 슬롯 컨테이너 생성
        GameObject slotContainer = new GameObject("CardSlots");
        slotContainer.transform.SetParent(transform, false);

        // 슬롯 크기 = 실제 조각 크기 * 비율
        float actualSlotWidth = pieceWidth * slotSizeRatio;
        float actualSlotHeight = pieceHeight * slotSizeRatio;

        // 프레임 셰이더 로드
        Shader frameShader = Shader.Find("Custom/RoundedFrame");

        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                // 슬롯 위치 계산
                float posX = startX + (col * slotWidth);
                float posY = startY - (row * slotHeight);

                // 슬롯 GameObject 생성
                GameObject slot = new GameObject($"Slot_{row}_{col}");
                slot.transform.SetParent(slotContainer.transform, false);
                slot.transform.position = new Vector3(posX, posY, 0.1f);  // 카드보다 뒤에 (z = 0.1)

                // SpriteRenderer 추가
                SpriteRenderer sr = slot.AddComponent<SpriteRenderer>();
                sr.sprite = CreateSlotSprite();
                sr.color = slotBackgroundColor;
                sr.sortingOrder = -2;  // 테두리(-1)보다 뒤에

                // 슬롯 크기 조절
                slot.transform.localScale = new Vector3(actualSlotWidth, actualSlotHeight, 1f);

                // 둥근 모서리 셰이더 적용
                if (roundedCornerShader != null)
                {
                    Material slotMaterial = new Material(roundedCornerShader);
                    sr.material = slotMaterial;

                    // PropertyBlock으로 설정
                    MaterialPropertyBlock block = new MaterialPropertyBlock();
                    sr.GetPropertyBlock(block);

                    // UV는 전체 스프라이트 (0,0,1,1)
                    block.SetVector("_UVRect", new Vector4(0, 0, 1, 1));
                    block.SetFloat("_CornerRadius", cornerRadius);
                    block.SetVector("_CornerRadii", new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
                    block.SetVector("_Padding", Vector4.zero);  // 슬롯은 패딩 없음

                    sr.SetPropertyBlock(block);
                }

                // 테두리 프레임 추가
                if (frameShader != null)
                {
                    CreateSlotBorder(slot, actualSlotWidth, actualSlotHeight, frameShader);
                }

                _cardSlots.Add(slot);
            }
        }

        Debug.Log($"[PuzzleBoardSetup] 카드 슬롯 {_cardSlots.Count}개 생성됨 (테두리 포함)");
    }

    /// <summary>
    /// 슬롯에 테두리 프레임을 추가합니다.
    /// </summary>
    void CreateSlotBorder(GameObject slot, float slotWidth, float slotHeight, Shader frameShader)
    {
        // 테두리 GameObject 생성
        GameObject border = new GameObject("Border");
        border.transform.SetParent(slot.transform, false);
        border.transform.localPosition = Vector3.zero;

        // SpriteRenderer 추가
        SpriteRenderer borderSr = border.AddComponent<SpriteRenderer>();
        borderSr.sprite = CreateSlotSprite();
        borderSr.color = slotBorderColor;
        borderSr.sortingOrder = -1;  // 배경(-2)보다 위, 카드(1)보다 아래

        // 스케일 = 부모와 동일 (localScale이므로 1,1,1)
        border.transform.localScale = Vector3.one;

        // 프레임 셰이더 적용
        Material borderMaterial = new Material(frameShader);
        borderSr.material = borderMaterial;

        // PropertyBlock으로 설정
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        borderSr.GetPropertyBlock(block);

        block.SetFloat("_FrameThickness", slotBorderThickness);
        block.SetVector("_CornerRadii", new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        block.SetVector("_HideDirections", Vector4.zero);  // 모든 방향 표시

        borderSr.SetPropertyBlock(block);
    }

    /// <summary>
    /// 슬롯용 1x1 픽셀 스프라이트를 생성합니다.
    /// </summary>
    Sprite CreateSlotSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);  // PPU = 1
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

        float baseSize = Mathf.Max(sizeH, sizeW);

        // boardScale 적용 (스케일이 작을수록 카메라 줌아웃 → 보드가 작아 보임)
        // boardScale 1.0 = 기본, 0.8 = 보드가 80% 크기로 보임
        mainCam.orthographicSize = baseSize / boardScale;
    }

    // ====== 인트로 애니메이션 ======

    /// <summary>
    /// 레벨 시작 인트로 애니메이션을 재생합니다.
    /// 1. 먼저 셔플 순서 결정
    /// 2. 셔플된 순서로 카드가 날아감
    /// 3. 모든 카드가 도착하면 동시에 뒤집힘 (셔플된 상태로 보임)
    /// </summary>
    private IEnumerator PlayIntroAnimation()
    {
        _isPlayingIntro = true;

        int totalCards = _piecesOnBoard.Count;

        // 0단계: 먼저 셔플 순서 결정 (실제 위치 이동 없이 데이터만 셔플)
        ShufflePiecesData();

        // 1단계: 카드가 좌상단부터 순서대로 날아감 (이미 셔플된 순서)
        int flyingCount = 0;

        for (int i = 0; i < totalCards; i++)
        {
            DragController piece = _piecesOnBoard[i];
            Vector3 targetPos = _slotPositions[i];

            // 카드 날아가는 애니메이션 시작
            StartCoroutine(FlyCardToPosition(piece, targetPos, cardFlyDuration, () =>
            {
                flyingCount++;
            }));

            // 다음 카드 발사 대기
            yield return new WaitForSeconds(cardFlyDelay);
        }

        // 모든 카드가 도착할 때까지 대기
        while (flyingCount < totalCards)
        {
            yield return null;
        }

        // 약간의 대기 후 뒤집기
        yield return new WaitForSeconds(0.2f);

        // 2단계: 모든 카드를 동시에 뒤집기 (이미 셔플된 상태)
        int flippedCount = 0;

        for (int i = 0; i < totalCards; i++)
        {
            DragController piece = _piecesOnBoard[i];

            // 약간의 딜레이를 두고 연속으로 뒤집기 (웨이브 효과)
            StartCoroutine(DelayedFlip(piece, i * cardFlipDelay, () =>
            {
                flippedCount++;
            }));
        }

        // 모든 카드가 뒤집힐 때까지 대기
        while (flippedCount < totalCards)
        {
            yield return null;
        }

        // 약간의 대기
        yield return new WaitForSeconds(0.3f);

        // 3단계: 초기 연결 체크 및 게임 시작
        CheckInitialConnections();

        // 드래그 활성화
        foreach (var piece in _piecesOnBoard)
        {
            piece.SetDraggable(true);
        }

        _isPlayingIntro = false;
        Debug.Log("인트로 애니메이션 완료. 게임 시작!");
    }

    /// <summary>
    /// 조각 데이터만 셔플합니다 (위치 이동 없이).
    /// </summary>
    void ShufflePiecesData()
    {
        int n = _piecesOnBoard.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            DragController temp = _piecesOnBoard[k];
            _piecesOnBoard[k] = _piecesOnBoard[n];
            _piecesOnBoard[n] = temp;
        }

        // 슬롯 인덱스만 업데이트 (위치는 나중에 fly 애니메이션에서 설정)
        for (int i = 0; i < _piecesOnBoard.Count; i++)
        {
            _piecesOnBoard[i].currentSlotIndex = i;
        }
    }

    /// <summary>
    /// 카드를 목표 위치로 부드럽게 이동시킵니다.
    /// </summary>
    private IEnumerator FlyCardToPosition(DragController piece, Vector3 targetPos, float duration, System.Action onComplete)
    {
        Vector3 startPos = piece.transform.position;
        float elapsed = 0f;

        // 살짝 위로 올라갔다가 내려오는 곡선 효과
        float arcHeight = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease out 효과 (끝에서 느려짐)
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            // 직선 이동
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, easedT);

            // 아크 효과 (포물선)
            float arc = arcHeight * Mathf.Sin(t * Mathf.PI);
            linearPos.y += arc;

            piece.transform.position = linearPos;

            yield return null;
        }

        piece.transform.position = targetPos;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 딜레이 후 카드를 뒤집습니다.
    /// </summary>
    private IEnumerator DelayedFlip(DragController piece, float delay, System.Action onComplete)
    {
        yield return new WaitForSeconds(delay);
        piece.FlipCard(cardFlipDuration, onComplete);
    }

    // ====== 스왑 애니메이션 ======

    /// <summary>
    /// 조각을 목표 위치로 부드럽게 이동시킵니다 (스왑용).
    /// </summary>
    private IEnumerator SmoothMovePiece(DragController piece, Vector3 targetPos, float duration)
    {
        Vector3 startPos = piece.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Ease out 효과 (끝에서 느려짐)
            float easedT = 1f - Mathf.Pow(1f - t, 2f);

            piece.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            yield return null;
        }

        piece.transform.position = targetPos;
    }

    /// <summary>
    /// 그룹 전체를 부드럽게 이동시킵니다.
    /// </summary>
    private IEnumerator SmoothMoveGroup(PieceGroup group, DragController anchorPiece, Vector3 anchorTargetPos, float duration)
    {
        // [중요] 코루틴 시작 시점의 조각 리스트 스냅샷 저장
        // 코루틴 실행 중 그룹에 새 조각이 병합되어도 안전하게 동작
        List<DragController> piecesSnapshot = new List<DragController>(group.pieces);

        // 각 조각의 시작 위치와 목표 위치 계산
        Vector3 offset = anchorTargetPos - anchorPiece.transform.position;
        Dictionary<DragController, Vector3> targetPositions = new Dictionary<DragController, Vector3>();
        Dictionary<DragController, Vector3> startPositions = new Dictionary<DragController, Vector3>();

        foreach (var piece in piecesSnapshot)
        {
            startPositions[piece] = piece.transform.position;
            targetPositions[piece] = piece.transform.position + offset;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easedT = 1f - Mathf.Pow(1f - t, 2f);

            foreach (var piece in piecesSnapshot)
            {
                piece.transform.position = Vector3.Lerp(startPositions[piece], targetPositions[piece], easedT);
            }

            // 애니메이션 중에도 테두리 위치 업데이트 (조각과 함께 이동)
            group.UpdateGroupBorderPosition();

            yield return null;
        }

        // 최종 위치 보정
        foreach (var piece in piecesSnapshot)
        {
            piece.transform.position = targetPositions[piece];
        }

        // 그룹 테두리 위치 업데이트
        group.UpdateGroupBorder();
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

        // 모서리 업데이트
        UpdateAllPieceCorners();

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

        // 4-3. 물리적 위치 이동 (애니메이션 적용)
        // 드래그 중인 그룹은 부드럽게 목표 위치로 이동
        StartCoroutine(SmoothMoveGroup(rootPiece.group, rootPiece, _slotPositions[rootPiece.currentSlotIndex], swapAnimationDuration));

        // 장애물(스왑된 조각들)은 부드럽게 개별 slot 위치로 이동
        List<DragController> swappedPieces = new List<DragController>();
        foreach (var info in transactionList)
        {
            if (!movingGroup.Contains(info.Piece))
            {
                StartCoroutine(SmoothMovePiece(info.Piece, _slotPositions[info.TargetSlotIndex], swapAnimationDuration));
                swappedPieces.Add(info.Piece);
            }
        }

        // 5. 결합 및 완료 체크 (연쇄 병합 포함) - 약간의 딜레이 후 실행
        // 드래그한 그룹과 스왑된 조각들 모두 연결 체크
        StartCoroutine(DelayedConnectionCheckWithSwapped(rootPiece.group, swappedPieces, swapAnimationDuration));
    }

    /// <summary>
    /// 스왑 애니메이션 후 연결 체크를 수행합니다. (드래그 그룹 + 스왑된 조각들)
    /// </summary>
    private IEnumerator DelayedConnectionCheckWithSwapped(PieceGroup draggedGroup, List<DragController> swappedPieces, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 병합 전 그룹 크기 기록
        int draggedGroupSizeBefore = draggedGroup.pieces.Count;
        Dictionary<PieceGroup, int> swappedGroupSizes = new Dictionary<PieceGroup, int>();
        foreach (var piece in swappedPieces)
        {
            if (!swappedGroupSizes.ContainsKey(piece.group))
            {
                swappedGroupSizes[piece.group] = piece.group.pieces.Count;
            }
        }

        // 1. 드래그한 그룹의 연결 체크
        CheckConnectionsRecursive(draggedGroup);

        // 2. 스왑된 조각들의 연결 체크 (각 조각의 그룹에 대해)
        HashSet<PieceGroup> checkedGroups = new HashSet<PieceGroup>();
        checkedGroups.Add(draggedGroup);  // 이미 체크한 그룹은 제외

        foreach (var piece in swappedPieces)
        {
            // 이미 체크한 그룹은 스킵 (드래그 그룹에 병합되었을 수 있음)
            if (checkedGroups.Contains(piece.group)) continue;

            CheckConnectionsRecursive(piece.group);
            checkedGroups.Add(piece.group);
        }

        // 모든 조각의 모서리 업데이트
        UpdateAllPieceCorners();

        // 병합이 발생했는지 확인하고 펌핑 애니메이션 재생
        HashSet<PieceGroup> mergedGroups = new HashSet<PieceGroup>();

        // 드래그 그룹이 커졌으면 병합됨
        if (draggedGroup.pieces.Count > draggedGroupSizeBefore)
        {
            mergedGroups.Add(draggedGroup);
        }

        // 스왑된 조각들의 그룹이 커졌으면 병합됨
        foreach (var piece in swappedPieces)
        {
            if (swappedGroupSizes.TryGetValue(piece.group, out int sizeBefore))
            {
                if (piece.group.pieces.Count > sizeBefore && !mergedGroups.Contains(piece.group))
                {
                    mergedGroups.Add(piece.group);
                }
            }
        }

        // 병합된 그룹의 모든 조각에 펌핑 애니메이션 재생
        foreach (var group in mergedGroups)
        {
            PlayGroupPumpingAnimation(group);
        }

        // 완료 체크
        CheckCompletion();
    }

    /// <summary>
    /// 그룹 내 모든 조각에 펌핑 애니메이션을 재생합니다.
    /// </summary>
    private void PlayGroupPumpingAnimation(PieceGroup group)
    {
        if (group == null || group.pieces.Count == 0) return;
        List<DragController> piecesSnapshot = new List<DragController>(group.pieces);
        StartCoroutine(GroupPumpingCoroutine(piecesSnapshot, pumpingScale, pumpingDuration));
    }

    private IEnumerator GroupPumpingCoroutine(List<DragController> pieces, float maxScale, float duration)
    {
        if (pieces == null || pieces.Count == 0) yield break;

        Vector3 groupCenter = Vector3.zero;
        foreach (var piece in pieces)
            if (piece != null) groupCenter += piece.transform.position;
        groupCenter /= pieces.Count;

        Dictionary<DragController, Vector3> originalPositions = new Dictionary<DragController, Vector3>();
        Dictionary<DragController, Vector3> originalScales = new Dictionary<DragController, Vector3>();
        Dictionary<DragController, Vector3> offsetFromCenter = new Dictionary<DragController, Vector3>();

        foreach (var piece in pieces)
        {
            if (piece != null)
            {
                originalPositions[piece] = piece.transform.position;
                originalScales[piece] = piece.transform.localScale;
                offsetFromCenter[piece] = piece.transform.position - groupCenter;
            }
        }

        float halfDuration = duration / 2f;
        float elapsed = 0f;

        // 그룹 테두리 참조 (첫 번째 조각의 그룹에서 가져옴)
        PieceGroup pieceGroup = pieces.Count > 0 && pieces[0] != null ? pieces[0].group : null;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float easedT = 1f - (1f - t) * (1f - t);
            float currentScale = Mathf.Lerp(1f, maxScale, easedT);

            foreach (var piece in pieces)
            {
                if (piece != null && originalScales.ContainsKey(piece))
                {
                    piece.transform.localScale = originalScales[piece] * currentScale;
                    piece.transform.position = groupCenter + offsetFromCenter[piece] * currentScale;
                }
            }

            // 그룹 테두리도 함께 스케일 적용
            pieceGroup?.UpdateGroupBorderWithScale(groupCenter, currentScale);

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);
            float easedT = t * t;
            float currentScale = Mathf.Lerp(maxScale, 1f, easedT);

            foreach (var piece in pieces)
            {
                if (piece != null && originalScales.ContainsKey(piece))
                {
                    piece.transform.localScale = originalScales[piece] * currentScale;
                    piece.transform.position = groupCenter + offsetFromCenter[piece] * currentScale;
                }
            }

            // 그룹 테두리도 함께 스케일 적용
            pieceGroup?.UpdateGroupBorderWithScale(groupCenter, currentScale);

            yield return null;
        }

        foreach (var piece in pieces)
        {
            if (piece != null && originalPositions.ContainsKey(piece))
            {
                piece.transform.localScale = originalScales[piece];
                piece.transform.position = originalPositions[piece];
            }
        }

        // 펌핑 완료 후 스케일 데이터 초기화
        pieceGroup?.ResetGroupBorderScaleData();
    }

    /// <summary>
    /// 모든 조각의 둥근 모서리 가시성을 업데이트합니다.
    /// </summary>
    void UpdateAllPieceCorners()
    {
        foreach (var piece in _piecesOnBoard)
        {
            piece.UpdateCornersBasedOnGroup();
        }
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

        // 그룹 테두리 위치 업데이트
        group.UpdateGroupBorder();
    }

    void DisbandAndRegroup(PieceGroup group)
    {
        if (group.pieces.Count == 0) return;

        // 기존 그룹의 테두리 제거 (중요: pieces.Clear() 전에 호출해야 함)
        group.DestroyGroupBorder();

        List<DragController> allPieces = new List<DragController>(group.pieces);
        group.pieces.Clear();

        // 1. Reset everyone to individual groups (Padding, Corners, Borders 복원 포함)
        foreach (var p in allPieces)
        {
            p.group = new PieceGroup();
            p.group.AddPiece(p);
            p.UpdateVisuals();
            p.ShowAllBorders();      // 모든 테두리 복원
            p.RestoreAllPadding();   // 모든 Padding 복원
            p.RestoreAllCorners();   // 모든 모서리 복원
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

        // 3. 재연결 후 모서리 업데이트
        foreach (var p in allPieces)
        {
            p.UpdateCornersBasedOnGroup();
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

            // 4. Update Visuals (Padding 제거로 이미지 연결)
            // 0:Top, 1:Bottom, 2:Left, 3:Right
            // rowOffset -1 = Top, 1 = Bottom
            // colOffset -1 = Left, 1 = Right

            // Padding 제거 (셰이더 기반 - EdgeCover 대체)
            if (rowOffset == -1) { piece.RemovePadding(0); neighbor.RemovePadding(1); } // My Top, Their Bottom
            if (rowOffset == 1)  { piece.RemovePadding(1); neighbor.RemovePadding(0); } // My Bottom, Their Top
            if (colOffset == -1) { piece.RemovePadding(2); neighbor.RemovePadding(3); } // My Left, Their Right
            if (colOffset == 1)  { piece.RemovePadding(3); neighbor.RemovePadding(2); } // My Right, Their Left

            // 테두리도 함께 숨기기 (개별 프레임용 - 그룹 테두리에서는 사용 안 함)
            if (rowOffset == -1) { piece.HideBorder(0); neighbor.HideBorder(1); }
            if (rowOffset == 1)  { piece.HideBorder(1); neighbor.HideBorder(0); }
            if (colOffset == -1) { piece.HideBorder(2); neighbor.HideBorder(3); }
            if (colOffset == 1)  { piece.HideBorder(3); neighbor.HideBorder(2); }

            // 5. 그룹 테두리 업데이트 (CompositeCollider2D + LineRenderer 방식)
            piece.group.UpdateGroupBorder();
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

        // 각 조각의 그룹 테두리 먼저 제거 (GroupBorder는 루트에 생성되므로 별도 처리 필요)
        HashSet<PieceGroup> processedGroups = new HashSet<PieceGroup>();
        foreach (var piece in _piecesOnBoard)
        {
            if (piece != null && piece.group != null && !processedGroups.Contains(piece.group))
            {
                piece.group.DestroyGroupBorder();
                processedGroups.Add(piece.group);
            }
        }

        // 퍼즐 조각 제거
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