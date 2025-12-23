using UnityEngine;

// 이 스크립트는 '카드' 오브젝트에 붙을 것입니다.
public class DragController : MonoBehaviour
{
    // ====== 스냅 기능 관련 변수 ======
    [HideInInspector] public PuzzleBoardSetup board;  // 퍼즐 보드판 참조
    [HideInInspector] public int correctSlotIndex;    // 정답 슬롯 인덱스
    [HideInInspector] public int currentSlotIndex;    // 현재 위치한 슬롯 인덱스
    [HideInInspector] public bool isPlaced = false;   // 정답 위치에 고정되었는지 여부

    // ====== Unity 인스펙터에 노출될 변수 선언부 ======
    
    private Vector3 _dragOffset;
    private float _cameraZDepth;
    private SpriteRenderer _spriteRenderer;
    private int _originalSortingOrder;

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _cameraZDepth = Camera.main.transform.position.z;
    }

    private void OnMouseDown()
    {
        // 이미 고정된 조각은 움직이지 않음
        if (isPlaced) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        _cameraZDepth = screenPoint.z;
        _dragOffset = transform.position - GetMouseWorldPos();

        // 드래그 중엔 맨 앞으로 표시
        _originalSortingOrder = _spriteRenderer.sortingOrder;
        _spriteRenderer.sortingOrder = 100;
    }

    private void OnMouseDrag()
    {
        if (isPlaced) return;
        transform.position = GetMouseWorldPos() + _dragOffset;
    }

    private void OnMouseUp()
    {
        if (isPlaced) return;

        // 드래그 종료 시, 보드에게 "나 여기서 손 놓았어"라고 알림
        // 보드가 위치 계산, 교체(Swap), 정답 확인 등을 처리함
        if (board != null)
        {
            board.OnPieceDropped(this);
        }

        // 레이어 복구는 board에서 처리하거나, 위치 확정 후 재설정
        _spriteRenderer.sortingOrder = _originalSortingOrder;
    }
    
    // 외부에서 강제로 위치를 옮길 때 사용 (Swap 애니메이션 등)
    public void UpdatePosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void LockPiece()
    {
        isPlaced = true;
        _spriteRenderer.color = new Color(0.9f, 0.9f, 0.9f); // 약간 어둡게 처리하여 고정됨을 표시
        _spriteRenderer.sortingOrder = 0; // 뒤로 보냄
    }

    // ====== 도우미 함수 (Helper Method) ======
    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _cameraZDepth;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}