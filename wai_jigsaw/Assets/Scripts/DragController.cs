using UnityEngine;

// 이 스크립트는 '카드' 오브젝트에 붙을 것입니다.
public class DragController : MonoBehaviour
{
    // ====== 스냅 기능 관련 변수 ======
    [HideInInspector] public PuzzleBoardSetup board;  // 퍼즐 보드판 참조
    [HideInInspector] public Vector3 correctPosition; // 이 조각의 정답 위치
    [HideInInspector] public bool isPlaced = false;   // 현재 정답 위치에 놓였는지 여부

    // ====== Unity 인스펙터에 노출될 변수 선언부 ======
    
    // (1) 드래그 시작 시 오브젝트와 마우스 포인터 간의 위치 차이(Offset)를 저장합니다.
    // 마우스가 오브젝트의 정중앙이 아닌 곳을 잡아도 자연스럽게 따라오도록 해줍니다.
    private Vector3 _dragOffset;
    
    // (2) 카메라의 Z축 위치를 저장합니다.
    // 터치/마우스 위치는 2D 화면 좌표지만, 오브젝트는 3D 공간에 있기 때문에
    // 화면 좌표를 3D 공간 좌표로 변환할 때 필요합니다.
    private float _cameraZDepth;

    // Start()는 오브젝트가 생성될 때 딱 한 번 실행됩니다.
    void Start()
    {
        // 메인 카메라의 Z축 위치를 미리 저장해둡니다. (이 게임은 2D이므로 깊이(Z)는 고정됩니다.)
        _cameraZDepth = Camera.main.transform.position.z;
    }

    // 마우스 버튼을 누르는 순간 딱 한 번 실행되는 함수입니다. (터치 시에도 동일)
    private void OnMouseDown()
    {
        // 이미 제자리에 놓인 조각은 움직이지 않습니다.
        if (isPlaced) return;

        // 1. 현재 오브젝트의 위치(transform.position)를 Screen Point(화면 좌표)로 변환합니다.
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);

        // 2. 마우스의 현재 Z 위치(깊이)를 우리가 저장해 둔 카메라 Z 깊이로 설정합니다.
        // 이 과정을 거쳐야 오브젝트의 깊이가 변하지 않고 2D 평면에서 움직입니다.
        _cameraZDepth = screenPoint.z;

        // 3. 드래그 오프셋 계산: 마우스 위치와 오브젝트 위치의 차이를 저장합니다.
        // GetMouseWorldPos()는 마우스의 화면 좌표를 3D 공간 좌표로 변환해주는 도우미 함수입니다.
        _dragOffset = transform.position - GetMouseWorldPos();

        // 4. (추가 기능): 드래그를 시작할 때 이 카드가 다른 카드들보다 위에 보이도록 깊이를 올려줍니다.
        // Z축이 아닌 순서(Sorting Order)로 처리하는 것이 2D에서 더 좋지만, 
        // 간단한 테스트를 위해 Z축을 살짝 위로 올리는 트릭을 사용하겠습니다.
        transform.position = new Vector3(transform.position.x, transform.position.y, -1f); 
    }

    // 마우스 버튼을 누른 상태에서 움직이는 동안 매 프레임 실행되는 함수입니다.
    private void OnMouseDrag()
    {
        // 이미 제자리에 놓인 조각은 움직이지 않습니다.
        if (isPlaced) return;

        // 1. 현재 마우스 위치에 오프셋을 더하여 새로운 월드 좌표를 계산합니다.
        // 이렇게 하면 마우스가 오브젝트의 어느 부분을 잡았든, 오프셋만큼 떨어진 채로 따라옵니다.
        transform.position = GetMouseWorldPos() + _dragOffset;
    }

    // 마우스 버튼에서 손을 떼는 순간 딱 한 번 실행되는 함수입니다.
    private void OnMouseUp()
    {
        // 이미 자리에 있다면 함수를 종료합니다.
        if (isPlaced) return;

        // 정답 위치와 현재 조각 위치 사이의 거리를 확인합니다.
        if (Vector3.Distance(transform.position, correctPosition) < 1.0f) // 1.0f는 스냅되는 거리(임계값)
        {
            // 거리가 충분히 가까우면 정답 위치로 이동시키고, '놓임' 상태로 변경합니다.
            transform.position = correctPosition;
            isPlaced = true;
            
            // (선택) 조각이 맞춰졌을 때 색을 살짝 어둡게 만드는 시각적 피드백
            GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.8f, 0.8f);

            // 보드에게 조각이 맞춰졌음을 알리고, 전체 퍼즐이 완성되었는지 체크하도록 요청합니다.
            if (board != null)
            {
                board.CheckCompletion();
            }
        }
        else
        {
            // 정답 위치가 아니면, 드래그 시작 시 올렸던 Z축 위치를 다시 0으로 되돌려 놓습니다.
            transform.position = new Vector3(transform.position.x, transform.position.y, 0f);
        }
    }


    // ====== 도우미 함수 (Helper Method) ======

    // 화면 좌표(마우스/터치 위치)를 3D 월드 공간 좌표로 변환해주는 함수
    private Vector3 GetMouseWorldPos()
    {
        // 1. 현재 마우스의 화면 좌표(픽셀 위치)를 가져옵니다.
        Vector3 mouseScreenPos = Input.mousePosition;
        
        // 2. 화면 좌표의 Z 깊이를 이전에 저장한 _cameraZDepth로 설정합니다.
        mouseScreenPos.z = _cameraZDepth;
        
        // 3. Unity 카메라를 사용하여 이 화면 좌표를 3D 월드 좌표로 변환하여 반환합니다.
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }
}