using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WaiJigsaw.Data;

public class DragController : MonoBehaviour
{
    // ====== 퍼즐 데이터 ======
    [HideInInspector] public PuzzleBoardSetup board;
    [HideInInspector] public int currentSlotIndex;    // 현재 위치한 슬롯 인덱스 (0 ~ N)
    [HideInInspector] public int originalGridX;       // 정답 그리드 좌표 X
    [HideInInspector] public int originalGridY;       // 정답 그리드 좌표 Y

    // ====== 조각 크기 정보 (그룹화 시 위치 조정용) ======
    [HideInInspector] public float pieceWidth;
    [HideInInspector] public float pieceHeight;

    // ====== 그룹 시스템 ======
    public PieceGroup group; // 내가 속한 그룹

    // ====== 시각 효과 (테두리) ======
    // 0:Top, 1:Bottom, 2:Left, 3:Right
    private GameObject[] _borders = new GameObject[4];

    // ====== 카드 시스템 ======
    private SpriteRenderer _cardBackRenderer;   // 카드 뒷면
    private SpriteRenderer _cardFrameRenderer;  // 카드 프레임
    private bool _isFlipped = false;            // true = 앞면(퍼즐), false = 뒷면
    private bool _canDrag = false;              // 드래그 가능 여부 (인트로 중에는 불가)

    // ====== 둥근 모서리 시스템 ======
    // 0: TopLeft, 1: TopRight, 2: BottomLeft, 3: BottomRight
    private GameObject[] _cornerObjects = new GameObject[4];
    private SpriteRenderer[] _cornerRenderers = new SpriteRenderer[4];
    private float _cornerRadius = 0.1f; // 모서리 반지름

    private Vector3 _dragOffset;
    private float _cameraZDepth;
    private SpriteRenderer _spriteRenderer;
    private int _originalSortingOrder;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // 시작 시 자기 자신만의 그룹 생성
        group = new PieceGroup();
        group.AddPiece(this);
    }

    void Start()
    {
        _cameraZDepth = Camera.main.transform.position.z;

        // 테두리가 아직 생성되지 않았으면 생성
        if (_borders[0] == null)
        {
            CreateBorders();
        }
    }

    private void OnMouseDown()
    {
        // 인트로 중에는 드래그 불가
        if (!_canDrag) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        _cameraZDepth = screenPoint.z;

        // 드래그 시작점 저장 (누적 오차 방지용)
        Vector3 mouseStartPos = GetMouseWorldPos();

        // 그룹 전체에게 드래그 시작 알림
        group.OnDragStart(mouseStartPos);
        group.SetSortingOrder(100);

        board.OnPieceDragStart(this);
    }

    private void OnMouseDrag()
    {
        if (!_canDrag) return;

        // 마우스의 총 이동량 계산
        Vector3 currentMousePos = GetMouseWorldPos();
        group.OnDragUpdate(currentMousePos);
    }

    private void OnMouseUp()
    {
        if (!_canDrag) return;

        group.SetSortingOrder(1);

        if (board != null)
        {
            board.OnPieceDropped(this);
        }
    }
    
    // 외부에서 강제로 위치를 옮길 때 사용 (시작 위치 정보까지 갱신해야 함)
    public void UpdatePosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void SetPositionImmediate(Vector3 newPos)
    {
        transform.position = newPos;
    }
    void CreateBorders()
    {
        // 간단하게 4개의 자식 오브젝트(LineRenderer 또는 Sprite)로 테두리를 만듭니다.
        // 여기서는 Pixel 단위 1px 두께의 Sprite를 늘려서 사용한다고 가정하거나,
        // LineRenderer를 사용합니다. LineRenderer가 깔끔합니다.
        
        Color borderColor = new Color(0.2f, 0.2f, 0.2f, 1f); // 진한 회색
        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;
        float thickness = 0.05f; // 테두리 두께

        Vector3 center = Vector3.zero;
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, height/2, 0),  // Top
            new Vector3(0, -height/2, 0), // Bottom
            new Vector3(-width/2, 0, 0),  // Left
            new Vector3(width/2, 0, 0)    // Right
        };

        Vector3[] scales = new Vector3[]
        {
            new Vector3(width, thickness, 1),  // Top
            new Vector3(width, thickness, 1),  // Bottom
            new Vector3(thickness, height, 1), // Left
            new Vector3(thickness, height, 1)  // Right
        };

        for(int i=0; i<4; i++)
        {
            GameObject border = new GameObject($"Border_{i}");
            border.transform.parent = transform;
            border.transform.localPosition = positions[i];
            
            SpriteRenderer sr = border.AddComponent<SpriteRenderer>();
            // 하얀색 1x1 픽셀 스프라이트 생성 (임시)
            sr.sprite = CreatePixelSprite(); 
            sr.color = borderColor;
            sr.sortingOrder = 2; // 조각보다 위에
            
            border.transform.localScale = scales[i];
            _borders[i] = border;
        }
    }

    Sprite CreatePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    // 인접한 짝과 결합되었을 때 테두리 숨기기
    public void UpdateVisuals()
    {
        // Start() 전에 호출될 수 있으므로 테두리가 없으면 먼저 생성
        if (_borders[0] == null)
        {
            CreateBorders();
        }

        // 기본적으로 다 켜고
        for (int i = 0; i < 4; i++)
        {
            if (_borders[i] != null)
            {
                _borders[i].SetActive(true);
            }
        }

        // 그룹 내의 다른 조각들과 관계를 확인하여 겹치는 부분 끄기
        // 로직은 PieceGroup이나 Board에서 호출하여 처리
    }

    public void HideBorder(int direction)
    {
        // 0:Top, 1:Bottom, 2:Left, 3:Right
        // Start() 전에 호출될 수 있으므로 테두리가 없으면 먼저 생성
        if (_borders[0] == null)
        {
            CreateBorders();
        }

        if (direction >= 0 && direction < 4 && _borders[direction] != null)
        {
            _borders[direction].SetActive(false);
        }
    }
    
    // 그룹에서 강제로 탈퇴 (스왑 당할 때 등)
    public void BreakFromGroup()
    {
        if (group != null)
        {
            group.RemovePiece(this);
        }
        
        // 새로운 단독 그룹 생성
        group = new PieceGroup();
        group.AddPiece(this);
        
        // 테두리 초기화 (다시 다 보여줌)
        UpdateVisuals();
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _cameraZDepth;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    // ====== 카드 시스템 메서드 ======

    /// <summary>
    /// 카드 컴포넌트를 초기화합니다 (뒷면, 프레임 생성).
    /// </summary>
    public void InitializeCardVisuals(Sprite cardBackSprite, float frameThickness = 0.08f)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;

        // 프레임 크기 (퍼즐보다 약간 크게)
        float frameWidth = width + (frameThickness * 2);
        float frameHeight = height + (frameThickness * 2);

        // 1. 카드 프레임 생성 (퍼즐 이미지 뒤에 깔리는 배경)
        GameObject frameObj = new GameObject("CardFrame");
        frameObj.transform.SetParent(transform, false);
        frameObj.transform.localPosition = Vector3.zero;

        _cardFrameRenderer = frameObj.AddComponent<SpriteRenderer>();
        Sprite frameSprite = CreatePixelSprite();
        _cardFrameRenderer.sprite = frameSprite;
        _cardFrameRenderer.color = new Color(0.95f, 0.92f, 0.85f); // 크림색 카드 배경
        _cardFrameRenderer.sortingOrder = 0; // 퍼즐 이미지보다 아래

        // 스프라이트 원본 크기 기준으로 스케일 계산
        float frameSpriteWidth = frameSprite.bounds.size.x;
        float frameSpriteHeight = frameSprite.bounds.size.y;
        float frameScaleX = frameWidth / frameSpriteWidth;
        float frameScaleY = frameHeight / frameSpriteHeight;
        frameObj.transform.localScale = new Vector3(frameScaleX, frameScaleY, 1);

        // 2. 카드 뒷면 생성 (프레임 위, 퍼즐 이미지 위에 덮음)
        GameObject backObj = new GameObject("CardBack");
        backObj.transform.SetParent(transform, false);
        backObj.transform.localPosition = Vector3.zero;

        _cardBackRenderer = backObj.AddComponent<SpriteRenderer>();
        Sprite backSprite;
        if (cardBackSprite != null)
        {
            backSprite = cardBackSprite;
            _cardBackRenderer.sprite = backSprite;
        }
        else
        {
            backSprite = CreatePixelSprite();
            _cardBackRenderer.sprite = backSprite;
            _cardBackRenderer.color = new Color(0.2f, 0.3f, 0.5f); // 기본 파란색
        }
        _cardBackRenderer.sortingOrder = 3; // 퍼즐 이미지와 테두리 위에

        // 스프라이트 원본 크기 기준으로 스케일 계산 (프레임과 동일한 크기로)
        float backSpriteWidth = backSprite.bounds.size.x;
        float backSpriteHeight = backSprite.bounds.size.y;
        float backScaleX = frameWidth / backSpriteWidth;
        float backScaleY = frameHeight / backSpriteHeight;
        backObj.transform.localScale = new Vector3(backScaleX, backScaleY, 1);

        // 초기 상태: 뒷면이 보이는 상태
        _isFlipped = false;
        _spriteRenderer.enabled = false; // 퍼즐 이미지 숨김

        // 기존 테두리 숨기기 (뒷면 상태에서는 보이지 않음)
        for (int i = 0; i < 4; i++)
        {
            if (_borders[i] != null)
                _borders[i].SetActive(false);
        }

        // 둥근 모서리 초기화
        InitializeRoundedCorners(frameThickness * 0.8f);
    }

    /// <summary>
    /// 카드를 뒤집습니다 (애니메이션).
    /// </summary>
    public void FlipCard(float duration = 0.3f, System.Action onComplete = null)
    {
        if (_isFlipped)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(FlipAnimation(duration, onComplete));
    }

    private IEnumerator FlipAnimation(float duration, System.Action onComplete)
    {
        float halfDuration = duration / 2f;

        // 1단계: 카드가 옆으로 납작해짐 (뒷면이 사라지는 느낌)
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, t);
            transform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        // 중간: 뒷면 숨기고 앞면 보이기
        _cardBackRenderer.gameObject.SetActive(false);
        _spriteRenderer.enabled = true;

        // 테두리 다시 활성화
        for (int i = 0; i < 4; i++)
        {
            if (_borders[i] != null)
                _borders[i].SetActive(true);
        }

        // 2단계: 카드가 다시 펼쳐짐 (앞면이 나타나는 느낌)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, t);
            transform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
        _isFlipped = true;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 즉시 앞면으로 전환합니다 (애니메이션 없이).
    /// </summary>
    public void ShowFrontImmediate()
    {
        if (_cardBackRenderer != null)
            _cardBackRenderer.gameObject.SetActive(false);

        _spriteRenderer.enabled = true;
        _isFlipped = true;

        // 테두리 활성화
        for (int i = 0; i < 4; i++)
        {
            if (_borders[i] != null)
                _borders[i].SetActive(true);
        }
    }

    /// <summary>
    /// 즉시 뒷면으로 전환합니다 (애니메이션 없이).
    /// </summary>
    public void ShowBackImmediate()
    {
        if (_cardBackRenderer != null)
            _cardBackRenderer.gameObject.SetActive(true);

        _spriteRenderer.enabled = false;
        _isFlipped = false;

        // 테두리 숨김
        for (int i = 0; i < 4; i++)
        {
            if (_borders[i] != null)
                _borders[i].SetActive(false);
        }
    }

    /// <summary>
    /// 드래그 가능 여부를 설정합니다.
    /// </summary>
    public void SetDraggable(bool canDrag)
    {
        _canDrag = canDrag;
    }

    /// <summary>
    /// 카드가 뒤집혔는지 확인합니다.
    /// </summary>
    public bool IsFlipped => _isFlipped;

    /// <summary>
    /// 카드 뒷면 렌더러를 반환합니다.
    /// </summary>
    public SpriteRenderer CardBackRenderer => _cardBackRenderer;

    /// <summary>
    /// 카드 프레임 렌더러를 반환합니다.
    /// </summary>
    public SpriteRenderer CardFrameRenderer => _cardFrameRenderer;

    // ====== 둥근 모서리 메서드 ======

    /// <summary>
    /// 둥근 모서리를 초기화합니다.
    /// </summary>
    public void InitializeRoundedCorners(float cornerRadius)
    {
        _cornerRadius = cornerRadius;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;

        // 모서리 위치 계산
        Vector3[] cornerPositions = new Vector3[]
        {
            new Vector3(-width / 2, height / 2, 0),   // TopLeft
            new Vector3(width / 2, height / 2, 0),    // TopRight
            new Vector3(-width / 2, -height / 2, 0),  // BottomLeft
            new Vector3(width / 2, -height / 2, 0)    // BottomRight
        };

        // 모서리 회전 (각 모서리가 올바른 방향을 향하도록)
        float[] cornerRotations = new float[] { 0f, 90f, -90f, 180f };

        Sprite cornerSprite = CreateCornerSprite(cornerRadius);

        for (int i = 0; i < 4; i++)
        {
            GameObject cornerObj = new GameObject($"Corner_{i}");
            cornerObj.transform.SetParent(transform, false);
            cornerObj.transform.localPosition = cornerPositions[i];
            cornerObj.transform.localRotation = Quaternion.Euler(0, 0, cornerRotations[i]);

            SpriteRenderer sr = cornerObj.AddComponent<SpriteRenderer>();
            sr.sprite = cornerSprite;
            sr.color = _cardFrameRenderer != null ? _cardFrameRenderer.color : new Color(0.95f, 0.92f, 0.85f);
            sr.sortingOrder = 4; // 가장 위에

            _cornerObjects[i] = cornerObj;
            _cornerRenderers[i] = sr;
        }
    }

    /// <summary>
    /// 둥근 모서리 스프라이트를 생성합니다.
    /// </summary>
    private Sprite CreateCornerSprite(float radius)
    {
        int pixelRadius = Mathf.Max(8, Mathf.RoundToInt(radius * 100)); // PPU=100 기준
        Texture2D texture = new Texture2D(pixelRadius, pixelRadius);
        texture.filterMode = FilterMode.Bilinear;

        Color transparent = new Color(1, 1, 1, 0);
        Color solid = Color.white;

        // 모든 픽셀을 투명으로 초기화
        for (int x = 0; x < pixelRadius; x++)
        {
            for (int y = 0; y < pixelRadius; y++)
            {
                texture.SetPixel(x, y, transparent);
            }
        }

        // 원의 1/4 부분만 채우기 (좌상단 모서리 기준)
        for (int x = 0; x < pixelRadius; x++)
        {
            for (int y = 0; y < pixelRadius; y++)
            {
                // 원의 중심에서의 거리 계산
                float dx = pixelRadius - x;
                float dy = pixelRadius - y;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                // 원 바깥쪽이면 채우기 (모서리 곡선 부분)
                if (distance > pixelRadius)
                {
                    texture.SetPixel(x, y, solid);
                }
            }
        }

        texture.Apply();

        // 피봇을 중앙으로 설정
        return Sprite.Create(texture, new Rect(0, 0, pixelRadius, pixelRadius), new Vector2(0.5f, 0.5f), 100f);
    }

    /// <summary>
    /// 특정 모서리의 가시성을 설정합니다.
    /// </summary>
    /// <param name="cornerIndex">0: TopLeft, 1: TopRight, 2: BottomLeft, 3: BottomRight</param>
    /// <param name="visible">표시 여부</param>
    public void SetCornerVisible(int cornerIndex, bool visible)
    {
        if (cornerIndex >= 0 && cornerIndex < 4 && _cornerObjects[cornerIndex] != null)
        {
            _cornerObjects[cornerIndex].SetActive(visible);
        }
    }

    /// <summary>
    /// 모든 모서리를 표시합니다.
    /// </summary>
    public void ShowAllCorners()
    {
        for (int i = 0; i < 4; i++)
        {
            SetCornerVisible(i, true);
        }
    }

    /// <summary>
    /// 그룹 내 위치에 따라 모서리 가시성을 업데이트합니다.
    /// </summary>
    public void UpdateCornersBasedOnGroup()
    {
        if (group == null) return;

        // 기본적으로 모든 모서리 표시
        ShowAllCorners();

        // 그룹 내 다른 조각들과의 관계 확인
        foreach (var otherPiece in group.pieces)
        {
            if (otherPiece == this) continue;

            int dx = otherPiece.originalGridX - originalGridX;
            int dy = otherPiece.originalGridY - originalGridY;

            // 인접한 조각이면 해당 방향의 모서리 숨김
            // 오른쪽에 조각이 있으면 (dx=1, dy=0) -> TopRight, BottomRight 숨김
            // 왼쪽에 조각이 있으면 (dx=-1, dy=0) -> TopLeft, BottomLeft 숨김
            // 위에 조각이 있으면 (dx=0, dy=-1) -> TopLeft, TopRight 숨김
            // 아래에 조각이 있으면 (dx=0, dy=1) -> BottomLeft, BottomRight 숨김

            if (dx == 1 && dy == 0) // 오른쪽
            {
                SetCornerVisible(1, false); // TopRight
                SetCornerVisible(3, false); // BottomRight
            }
            else if (dx == -1 && dy == 0) // 왼쪽
            {
                SetCornerVisible(0, false); // TopLeft
                SetCornerVisible(2, false); // BottomLeft
            }
            else if (dx == 0 && dy == -1) // 위
            {
                SetCornerVisible(0, false); // TopLeft
                SetCornerVisible(1, false); // TopRight
            }
            else if (dx == 0 && dy == 1) // 아래
            {
                SetCornerVisible(2, false); // BottomLeft
                SetCornerVisible(3, false); // BottomRight
            }

            // 대각선 조각 처리 (L자 모양 등)
            if (dx == 1 && dy == -1) SetCornerVisible(1, false);  // 우상단
            if (dx == -1 && dy == -1) SetCornerVisible(0, false); // 좌상단
            if (dx == 1 && dy == 1) SetCornerVisible(3, false);   // 우하단
            if (dx == -1 && dy == 1) SetCornerVisible(2, false);  // 좌하단
        }
    }

    /// <summary>
    /// 모서리 색상을 업데이트합니다.
    /// </summary>
    public void UpdateCornerColors(Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            if (_cornerRenderers[i] != null)
            {
                _cornerRenderers[i].color = color;
            }
        }
    }

    /// <summary>
    /// 모서리 렌더러 배열을 반환합니다.
    /// </summary>
    public SpriteRenderer[] CornerRenderers => _cornerRenderers;
}

// ====== 그룹 클래스 ======
public class PieceGroup
{
    public List<DragController> pieces = new List<DragController>();
    private Dictionary<DragController, Vector3> _startPositions = new Dictionary<DragController, Vector3>();
    private Vector3 _mouseStartWorldPos;

    public void AddPiece(DragController piece)
    {
        if (!pieces.Contains(piece))
        {
            pieces.Add(piece);
            piece.group = this;
        }
    }

    /// <summary>
    /// 두 그룹을 병합하고, 병합되는 그룹의 조각들을 스냅하여 위치를 조정합니다.
    /// anchorPiece: 현재 그룹에서 기준이 되는 조각
    /// connectingPiece: 병합 대상 그룹에서 anchorPiece와 인접한 조각
    /// </summary>
    public void MergeGroupWithSnap(PieceGroup otherGroup, DragController anchorPiece, DragController connectingPiece)
    {
        if (otherGroup == this) return;

        // anchor 조각을 기준으로 connecting 조각이 있어야 할 위치 계산
        int gridDeltaX = connectingPiece.originalGridX - anchorPiece.originalGridX;
        int gridDeltaY = connectingPiece.originalGridY - anchorPiece.originalGridY;

        // spacing 없이 조각이 붙어야 하는 위치
        Vector3 expectedConnectingPos = anchorPiece.transform.position + new Vector3(
            gridDeltaX * anchorPiece.pieceWidth,
            -gridDeltaY * anchorPiece.pieceHeight, // Y는 그리드 좌표와 반대 방향
            0
        );

        // 현재 connecting 조각의 위치와의 차이 (이동해야 할 양)
        Vector3 positionOffset = expectedConnectingPos - connectingPiece.transform.position;

        // 병합 대상 그룹의 모든 조각을 이동시키고 현재 그룹에 추가
        foreach (var piece in otherGroup.pieces)
        {
            piece.transform.position += positionOffset;
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();
    }

    public void MergeGroup(PieceGroup otherGroup)
    {
        if (otherGroup == this) return;

        foreach (var piece in otherGroup.pieces)
        {
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();
    }

    public void RemovePiece(DragController piece)
    {
        if (pieces.Contains(piece))
        {
            pieces.Remove(piece);
        }
    }

    public void OnDragStart(Vector3 mousePos)
    {
        _mouseStartWorldPos = mousePos;
        _startPositions.Clear();
        foreach (var piece in pieces)
        {
            _startPositions[piece] = piece.transform.position;
        }
    }

    public void OnDragUpdate(Vector3 currentMousePos)
    {
        Vector3 delta = currentMousePos - _mouseStartWorldPos;
        foreach (var piece in pieces)
        {
            if (_startPositions.ContainsKey(piece))
            {
                piece.SetPositionImmediate(_startPositions[piece] + delta);
            }
        }
    }

    public void SetSortingOrder(int order)
    {
        foreach (var piece in pieces)
        {
            // 메인 퍼즐 이미지
            piece.GetComponent<SpriteRenderer>().sortingOrder = order;

            // 카드 프레임 (퍼즐 이미지 뒤)
            if (piece.CardFrameRenderer != null)
            {
                piece.CardFrameRenderer.sortingOrder = order - 1;
            }

            // 카드 뒷면 (맨 위에서 2번째)
            if (piece.CardBackRenderer != null)
            {
                piece.CardBackRenderer.sortingOrder = order + 3;
            }

            // 둥근 모서리 (맨 위)
            var cornerRenderers = piece.CornerRenderers;
            if (cornerRenderers != null)
            {
                foreach (var cr in cornerRenderers)
                {
                    if (cr != null)
                    {
                        cr.sortingOrder = order + 4;
                    }
                }
            }

            // 테두리 (퍼즐 이미지 위)
            var allRenderers = piece.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                // 자기 자신, 카드 프레임, 카드 뒷면, 모서리 제외
                if (sr.gameObject == piece.gameObject) continue;
                if (piece.CardFrameRenderer != null && sr == piece.CardFrameRenderer) continue;
                if (piece.CardBackRenderer != null && sr == piece.CardBackRenderer) continue;

                bool isCorner = false;
                if (cornerRenderers != null)
                {
                    foreach (var cr in cornerRenderers)
                    {
                        if (cr == sr) { isCorner = true; break; }
                    }
                }
                if (isCorner) continue;

                // 나머지는 테두리
                sr.sortingOrder = order + 1;
            }
        }
    }
}