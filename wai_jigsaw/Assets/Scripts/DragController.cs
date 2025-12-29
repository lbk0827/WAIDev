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

    // ====== EdgeCover 시스템 (spacing 영역 가리기) ======
    // 0:Top, 1:Bottom, 2:Left, 3:Right
    private GameObject[] _edgeCovers = new GameObject[4];
    private float _coverSize = 0f;  // spacing/2 크기

    // ====== 카드 시스템 ======
    private SpriteRenderer _cardBackRenderer;   // 카드 뒷면
    private bool _isFlipped = false;            // true = 앞면(퍼즐), false = 뒷면
    private bool _canDrag = false;              // 드래그 가능 여부 (인트로 중에는 불가)

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

        // EdgeCover 복원 (다시 다 가리기)
        RestoreAllEdgeCovers();
    }

    // ====== EdgeCover 제어 메서드 ======

    /// <summary>
    /// 특정 방향의 EdgeCover를 제거합니다 (병합 시 호출).
    /// </summary>
    public void RemoveEdgeCover(int direction)
    {
        if (direction >= 0 && direction < 4 && _edgeCovers[direction] != null)
        {
            _edgeCovers[direction].SetActive(false);
        }
    }

    /// <summary>
    /// 특정 방향의 EdgeCover를 복원합니다 (그룹 분리 시 호출).
    /// </summary>
    public void RestoreEdgeCover(int direction)
    {
        if (direction >= 0 && direction < 4 && _edgeCovers[direction] != null)
        {
            _edgeCovers[direction].SetActive(true);
        }
    }

    /// <summary>
    /// 모든 EdgeCover를 복원합니다 (그룹 완전 분리 시 호출).
    /// </summary>
    public void RestoreAllEdgeCovers()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
            {
                _edgeCovers[i].SetActive(true);
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _cameraZDepth;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    // ====== 카드 시스템 메서드 ======

    /// <summary>
    /// 카드 컴포넌트를 초기화합니다 (뒷면 생성).
    /// </summary>
    public void InitializeCardVisuals(Sprite cardBackSprite, float frameThickness = 0.08f)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;

        // 카드 뒷면 생성 (퍼즐 이미지 위에 덮음)
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

        // 스프라이트 원본 크기 기준으로 스케일 계산
        float backSpriteWidth = backSprite.bounds.size.x;
        float backSpriteHeight = backSprite.bounds.size.y;
        float backScaleX = width / backSpriteWidth;
        float backScaleY = height / backSpriteHeight;
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

        // EdgeCover 생성 (spacing 영역 가리기)
        CreateEdgeCovers();

        // EdgeCover도 뒷면 상태에서는 숨김
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
                _edgeCovers[i].SetActive(false);
        }
    }

    /// <summary>
    /// 커버 크기를 설정합니다 (spacing/2).
    /// </summary>
    public void SetCoverSize(float size)
    {
        _coverSize = size;
    }

    /// <summary>
    /// EdgeCover를 생성합니다 (각 방향의 spacing 영역을 가리는 불투명 커버).
    /// </summary>
    void CreateEdgeCovers()
    {
        Debug.Log($"[EdgeCover] CreateEdgeCovers 호출됨. _coverSize={_coverSize}, pieceWidth={pieceWidth}, pieceHeight={pieceHeight}");

        if (_coverSize <= 0)
        {
            Debug.LogWarning("[EdgeCover] _coverSize가 0 이하입니다. EdgeCover 생성 안됨.");
            return;
        }

        float width = pieceWidth;
        float height = pieceHeight;

        // 커버 색상 (Main Camera 배경색과 자동 동기화)
        Color coverColor = Camera.main.backgroundColor;
        coverColor.a = 1f; // 알파값 강제 1 (배경색 알파가 0일 수 있음)
        Debug.Log($"[EdgeCover] Camera backgroundColor: R={coverColor.r}, G={coverColor.g}, B={coverColor.b}, A={coverColor.a}");

        // 각 방향별 위치와 크기
        // 0:Top, 1:Bottom, 2:Left, 3:Right
        // EdgeCover는 이미지 가장자리에 위치하여 spacing/2 만큼 덮음
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, (height - _coverSize) / 2, 0),   // Top: 상단 가장자리
            new Vector3(0, -(height - _coverSize) / 2, 0),  // Bottom: 하단 가장자리
            new Vector3(-(width - _coverSize) / 2, 0, 0),   // Left: 좌측 가장자리
            new Vector3((width - _coverSize) / 2, 0, 0)     // Right: 우측 가장자리
        };

        Vector3[] scales = new Vector3[]
        {
            new Vector3(width, _coverSize, 1),      // Top (가로로 긴 막대)
            new Vector3(width, _coverSize, 1),      // Bottom
            new Vector3(_coverSize, height, 1),     // Left (세로로 긴 막대)
            new Vector3(_coverSize, height, 1)      // Right
        };

        string[] names = new string[] { "EdgeCover_Top", "EdgeCover_Bottom", "EdgeCover_Left", "EdgeCover_Right" };

        for (int i = 0; i < 4; i++)
        {
            GameObject cover = new GameObject(names[i]);
            cover.transform.SetParent(transform, false);
            cover.transform.localPosition = positions[i];

            SpriteRenderer sr = cover.AddComponent<SpriteRenderer>();
            // PPU=1인 스프라이트 생성 (스케일이 Unity 단위와 1:1 매칭되도록)
            sr.sprite = CreateUnitSprite();
            sr.color = coverColor;
            sr.sortingOrder = 2; // 퍼즐 이미지(1) 위, 카드뒷면(3) 아래

            // 스케일 적용 (PPU=1이므로 스케일 = 실제 Unity 크기)
            cover.transform.localScale = scales[i];

            _edgeCovers[i] = cover;

            Debug.Log($"[EdgeCover] {names[i]} 생성됨. 위치={positions[i]}, 크기={scales[i]}");
        }
    }

    /// <summary>
    /// PPU=1인 1x1 픽셀 스프라이트를 생성합니다 (EdgeCover용).
    /// </summary>
    Sprite CreateUnitSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // PPU = 1
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

        // EdgeCover 활성화 (spacing 영역 가리기)
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
                _edgeCovers[i].SetActive(true);
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

        // EdgeCover 활성화
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
                _edgeCovers[i].SetActive(true);
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

        // EdgeCover 숨김
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
                _edgeCovers[i].SetActive(false);
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
    /// 그룹 내 위치에 따라 모서리 가시성을 업데이트합니다 (현재 미사용).
    /// </summary>
    public void UpdateCornersBasedOnGroup()
    {
        // 현재 미사용
    }
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
    /// 두 그룹을 병합합니다 (카드 이동 없음 - EdgeCover 제거로 이미지 연결).
    /// 카드들은 슬롯 위치에 그대로 유지되고, EdgeCover만 제거되어 이미지가 연결됩니다.
    /// </summary>
    public void MergeGroupWithSnap(PieceGroup otherGroup, DragController anchorPiece, DragController connectingPiece)
    {
        if (otherGroup == this) return;

        // 카드 위치 이동 없이 그룹만 병합
        // EdgeCover 제거는 CheckNeighbor에서 처리됨
        foreach (var piece in otherGroup.pieces)
        {
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

            // 카드 뒷면 (맨 위)
            if (piece.CardBackRenderer != null)
            {
                piece.CardBackRenderer.sortingOrder = order + 3;
            }

            // 테두리 (퍼즐 이미지 위)
            var allRenderers = piece.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allRenderers)
            {
                // 자기 자신, 카드 뒷면 제외
                if (sr.gameObject == piece.gameObject) continue;
                if (piece.CardBackRenderer != null && sr == piece.CardBackRenderer) continue;

                // 나머지는 테두리
                sr.sortingOrder = order + 1;
            }
        }
    }
}