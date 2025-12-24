using UnityEngine;

// 이 스크립트는 '카드' 오브젝트에 붙을 것입니다.
using UnityEngine;
using System.Collections.Generic;

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
        // 마우스의 총 이동량 계산
        Vector3 currentMousePos = GetMouseWorldPos();
        group.OnDragUpdate(currentMousePos);
    }

    private void OnMouseUp()
    {
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
            piece.GetComponent<SpriteRenderer>().sortingOrder = order;
            // 테두리는 더 위에
             var borders = piece.GetComponentsInChildren<SpriteRenderer>();
             foreach(var b in borders)
             {
                 if(b.gameObject != piece.gameObject) // 자기 자신 제외
                    b.sortingOrder = order + 1;
             }
        }
    }
}