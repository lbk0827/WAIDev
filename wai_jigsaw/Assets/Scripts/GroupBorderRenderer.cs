using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// CompositeCollider2D + LineRenderer를 사용하여 그룹 외곽선을 그리는 컴포넌트.
/// 불규칙한 형태(ㄱ자, T자 등)에서도 연속적인 외곽선을 그릴 수 있습니다.
/// </summary>
public class GroupBorderRenderer : MonoBehaviour
{
    [Header("Border Settings")]
    [SerializeField] private float _whiteBorderWidth = 0.08f;  // 흰색 테두리 두께 (World Space)
    [SerializeField] private float _blackBorderWidth = 0.025f; // 검은색 테두리 두께 (World Space)
    [SerializeField] private float _cornerRadius = 0.05f;      // 모서리 둥글기 반경

    [Header("Colors")]
    [SerializeField] private Color _whiteColor = Color.white;
    [SerializeField] private Color _blackColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [Header("Sorting")]
    [SerializeField] private int _baseSortingOrder = 100;  // 높은 값으로 변경

    // 컴포넌트 참조
    private CompositeCollider2D _compositeCollider;
    private Rigidbody2D _rigidbody;
    private LineRenderer _whiteLineRenderer;
    private LineRenderer _blackLineRenderer;

    // 자식 BoxCollider2D 목록 (정리용)
    private List<BoxCollider2D> _childColliders = new List<BoxCollider2D>();

    // 조각 정보
    private List<DragController> _pieces = new List<DragController>();

    // 외곽선 수축 오프셋 (콜라이더 확장량 + 테두리 중심 보정)
    private float _shrinkOffset = 0f;

    // 테두리 중심 보정 오프셋 (LineRenderer가 중심선 기준으로 그리므로, 흰색 테두리 절반만큼 안쪽으로)
    private float _borderCenterOffset = 0f;

    private void Awake()
    {
        // 마우스 입력을 방해하지 않도록 레이어 설정
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        SetupComponents();
    }

    /// <summary>
    /// 필요한 컴포넌트들을 설정합니다.
    /// </summary>
    private void SetupComponents()
    {
        // Rigidbody2D 설정 (CompositeCollider2D에 필요)
        _rigidbody = GetComponent<Rigidbody2D>();
        if (_rigidbody == null)
        {
            _rigidbody = gameObject.AddComponent<Rigidbody2D>();
        }
        _rigidbody.bodyType = RigidbodyType2D.Kinematic;  // 물리 시뮬레이션 비활성화

        // CompositeCollider2D 설정
        _compositeCollider = GetComponent<CompositeCollider2D>();
        if (_compositeCollider == null)
        {
            _compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
        }
        _compositeCollider.geometryType = CompositeCollider2D.GeometryType.Polygons;
        _compositeCollider.isTrigger = true;  // 물리 충돌 비활성화

        // LineRenderer 생성 (검은색 - 바깥쪽, 먼저 그림)
        GameObject blackLineObj = new GameObject("BlackBorderLine");
        blackLineObj.transform.SetParent(transform, false);
        _blackLineRenderer = blackLineObj.AddComponent<LineRenderer>();
        SetupLineRenderer(_blackLineRenderer, _blackColor, _whiteBorderWidth + _blackBorderWidth, _baseSortingOrder);

        // LineRenderer 생성 (흰색 - 안쪽, 나중에 그림)
        GameObject whiteLineObj = new GameObject("WhiteBorderLine");
        whiteLineObj.transform.SetParent(transform, false);
        _whiteLineRenderer = whiteLineObj.AddComponent<LineRenderer>();
        SetupLineRenderer(_whiteLineRenderer, _whiteColor, _whiteBorderWidth, _baseSortingOrder + 1);
    }

    /// <summary>
    /// LineRenderer 기본 설정
    /// </summary>
    private void SetupLineRenderer(LineRenderer lr, Color color, float width, int sortingOrder)
    {
        lr.useWorldSpace = true;
        lr.loop = true;  // 닫힌 도형
        lr.startWidth = width;
        lr.endWidth = width;
        lr.numCornerVertices = 8;  // 모서리 둥글게
        lr.numCapVertices = 4;

        // Material 설정 (단색) - Sprites/Default가 없을 경우 대비
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
            Debug.LogWarning("[GroupBorderRenderer] Sprites/Default 셰이더를 찾을 수 없어 Unlit/Color 사용");
        }
        lr.material = new Material(shader);
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortingOrder;
        lr.sortingLayerName = "Default";  // 명시적으로 소팅 레이어 설정

        Debug.Log($"[GroupBorderRenderer] LineRenderer 설정 완료 - color: {color}, width: {width}, sortingOrder: {sortingOrder}");
    }

    /// <summary>
    /// 그룹의 조각들을 설정하고 테두리를 업데이트합니다.
    /// </summary>
    public void SetPieces(List<DragController> pieces)
    {
        _pieces.Clear();
        _pieces.AddRange(pieces);

        // 기존 자식 콜라이더 정리
        ClearChildColliders();

        // 각 조각에 대해 BoxCollider2D 생성
        foreach (var piece in pieces)
        {
            CreateColliderForPiece(piece);
        }

        // 콜라이더 병합 후 외곽선 업데이트
        // CompositeCollider2D는 자동으로 다음 프레임에 업데이트됨
        // 즉시 업데이트가 필요하면 Physics2D.SyncTransforms() 호출
        Physics2D.SyncTransforms();
        _compositeCollider.GenerateGeometry();

        UpdateBorderFromCollider();
    }

    /// <summary>
    /// 조각에 대응하는 BoxCollider2D를 생성합니다.
    /// </summary>
    private void CreateColliderForPiece(DragController piece)
    {
        // 조각의 월드 크기 계산
        SpriteRenderer sr = piece.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null) return;

        Vector2 pieceSize = sr.sprite.bounds.size;
        pieceSize.x *= piece.transform.localScale.x;
        pieceSize.y *= piece.transform.localScale.y;

        // 콜라이더 크기를 확장하여 인접 조각과 겹치도록 함
        // CompositeCollider2D가 병합하려면 콜라이더들이 겹쳐야 함
        // 조각 간격이 있으므로 충분히 확장해야 함
        float overlapMargin = 0.10f;  // 10% 확장

        // 수축 오프셋 계산
        // 확장은 중심 기준 양쪽으로 퍼지므로, 각 변에서 수축할 거리 = pieceSize * margin / 2
        if (_shrinkOffset == 0f)
        {
            _shrinkOffset = Mathf.Min(pieceSize.x, pieceSize.y) * overlapMargin * 0.5f;
        }

        pieceSize.x *= (1f + overlapMargin);
        pieceSize.y *= (1f + overlapMargin);

        // 자식 GameObject 생성 (BoxCollider2D 담을 용도)
        GameObject colliderObj = new GameObject($"Collider_{piece.originalGridX}_{piece.originalGridY}");
        colliderObj.layer = LayerMask.NameToLayer("Ignore Raycast");  // 마우스 입력 무시
        colliderObj.transform.SetParent(transform, false);  // 로컬 좌표 사용

        // 부모(GroupBorder)가 (0,0,0)에 있으므로, 조각의 월드 좌표가 곧 로컬 좌표
        colliderObj.transform.localPosition = piece.transform.position;

        // BoxCollider2D 추가
        BoxCollider2D boxCollider = colliderObj.AddComponent<BoxCollider2D>();
        boxCollider.size = pieceSize;
        boxCollider.usedByComposite = true;  // CompositeCollider2D에 병합
        boxCollider.isTrigger = true;  // 물리 충돌 방지

        _childColliders.Add(boxCollider);

        Debug.Log($"[GroupBorderRenderer] 콜라이더 생성 - piece({piece.originalGridX},{piece.originalGridY}), pos={colliderObj.transform.localPosition}, size={pieceSize}, shrinkOffset={_shrinkOffset}");
    }

    /// <summary>
    /// 기존 자식 콜라이더들을 정리합니다.
    /// </summary>
    private void ClearChildColliders()
    {
        foreach (var collider in _childColliders)
        {
            if (collider != null && collider.gameObject != null)
            {
                Destroy(collider.gameObject);
            }
        }
        _childColliders.Clear();
        _shrinkOffset = 0f;  // 수축 오프셋 초기화
        // _borderCenterOffset은 SetBorderWidth에서 설정되므로 여기서 초기화하지 않음
    }

    /// <summary>
    /// CompositeCollider2D에서 외곽선 데이터를 추출하여 LineRenderer에 적용합니다.
    /// </summary>
    private void UpdateBorderFromCollider()
    {
        if (_compositeCollider == null)
        {
            Debug.LogWarning("[GroupBorderRenderer] CompositeCollider2D가 null입니다.");
            return;
        }

        if (_compositeCollider.pathCount == 0)
        {
            // 외곽선이 없으면 숨기기
            Debug.Log($"[GroupBorderRenderer] pathCount = 0, 조각 수 = {_pieces.Count}");
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            return;
        }

        // 가장 많은 점을 가진 Path 찾기 (가장 바깥쪽 외곽선)
        int bestPathIndex = 0;
        int maxPointCount = 0;

        for (int p = 0; p < _compositeCollider.pathCount; p++)
        {
            int count = _compositeCollider.GetPathPointCount(p);
            if (count > maxPointCount)
            {
                maxPointCount = count;
                bestPathIndex = p;
            }
        }

        int pointCount = maxPointCount;
        Debug.Log($"[GroupBorderRenderer] pathCount = {_compositeCollider.pathCount}, 선택된 pathIndex = {bestPathIndex}, pointCount = {pointCount}");

        if (pointCount < 3) return;

        Vector2[] points2D = new Vector2[pointCount];
        _compositeCollider.GetPath(bestPathIndex, points2D);

        // 외곽선 조정 적용:
        // 1. 콜라이더 확장량 (_shrinkOffset) - 병합을 위해 확장한 만큼 복원 (수축)
        // 2. 테두리 중심 보정 (_borderCenterOffset) - 음수면 바깥으로 확장, 양수면 안쪽으로 수축
        float totalOffset = _shrinkOffset + _borderCenterOffset;
        Debug.Log($"[GroupBorderRenderer] 외곽선 조정 - shrinkOffset={_shrinkOffset:F4}, borderCenterOffset={_borderCenterOffset:F4}, total={totalOffset:F4}");
        if (totalOffset > 0.001f)
        {
            // 양수: 안쪽으로 수축
            points2D = ShrinkPolygon(points2D, totalOffset);
        }
        else if (totalOffset < -0.001f)
        {
            // 음수: 바깥으로 확장 (ShrinkPolygon에 음수 offset 전달하면 확장됨)
            points2D = ExpandPolygon(points2D, -totalOffset);
        }

        // 둥근 모서리 적용 (CompositeCollider2D의 점은 로컬 좌표)
        Debug.Log($"[GroupBorderRenderer] ApplyRoundedCorners 호출 - cornerRadius={_cornerRadius:F4}, pointCount={points2D.Length}");
        List<Vector3> smoothedPoints = ApplyRoundedCorners(points2D, _cornerRadius);

        // LineRenderer에 적용
        _whiteLineRenderer.positionCount = smoothedPoints.Count;
        _blackLineRenderer.positionCount = smoothedPoints.Count;

        // 디버그: 첫 번째와 마지막 점 로그
        if (smoothedPoints.Count > 0)
        {
            Vector3 firstLocal = smoothedPoints[0];
            Vector3 firstWorld = transform.TransformPoint(firstLocal);
            Vector3 lastLocal = smoothedPoints[smoothedPoints.Count - 1];
            Vector3 lastWorld = transform.TransformPoint(lastLocal);
            Debug.Log($"[GroupBorderRenderer] 첫 점: local={firstLocal}, world={firstWorld}");
            Debug.Log($"[GroupBorderRenderer] 끝 점: local={lastLocal}, world={lastWorld}");
            Debug.Log($"[GroupBorderRenderer] transform.position={transform.position}, transform.localScale={transform.localScale}");
        }

        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            // CompositeCollider2D의 GetPath()는 로컬 좌표를 반환하므로 월드 좌표로 변환
            Vector3 localPos = smoothedPoints[i];
            Vector3 worldPos = transform.TransformPoint(localPos);
            // Z축: 퍼즐 조각은 보통 z=0, 카메라는 z=-10 정도
            // LineRenderer를 z=0에 가깝게 두어 조각 위에 렌더링
            worldPos.z = 0f;

            _whiteLineRenderer.SetPosition(i, worldPos);
            _blackLineRenderer.SetPosition(i, worldPos);
        }

        Debug.Log($"[GroupBorderRenderer] LineRenderer 포지션 설정 완료 - {smoothedPoints.Count}개 점");
    }

    /// <summary>
    /// 다각형의 모서리를 둥글게 처리합니다.
    /// 볼록한 모서리(Convex, 외각)만 둥글게 하고, 오목한 모서리(Concave, 내각)는 직각 유지.
    /// </summary>
    private List<Vector3> ApplyRoundedCorners(Vector2[] points, float radius)
    {
        List<Vector3> result = new List<Vector3>();

        if (radius <= 0.001f || points.Length < 3)
        {
            // 둥글게 처리 안 함
            foreach (var p in points)
            {
                result.Add(new Vector3(p.x, p.y, 0));
            }
            return result;
        }

        // 다각형의 방향 확인 (시계/반시계)
        // Signed area가 양수면 반시계방향, 음수면 시계방향
        float signedArea = CalculateSignedArea(points);
        bool isClockwise = signedArea < 0;

        int segmentsPerCorner = 4;  // 각 모서리당 세그먼트 수

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 prev = points[(i - 1 + points.Length) % points.Length];
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % points.Length];

            Vector2 toPrev = (prev - curr).normalized;
            Vector2 toNext = (next - curr).normalized;

            // 모서리가 볼록(Convex)인지 오목(Concave)인지 판단
            Vector2 edge1 = curr - prev;
            Vector2 edge2 = next - curr;
            float cross = edge1.x * edge2.y - edge1.y * edge2.x;

            // 다각형 방향에 따라 볼록/오목 판단이 반대
            // 시계방향: cross < 0 이면 오목 (내부로 들어감)
            // 반시계방향: cross > 0 이면 오목
            bool isConcave = isClockwise ? (cross < 0) : (cross > 0);

            // 오목한 모서리(내각 > 180도)는 직각 유지
            if (isConcave)
            {
                result.Add(new Vector3(curr.x, curr.y, 0));
                continue;
            }

            // 두 변의 길이
            float distToPrev = Vector2.Distance(prev, curr);
            float distToNext = Vector2.Distance(curr, next);

            // 적용 가능한 최대 반경 계산
            float maxRadius = Mathf.Min(distToPrev / 2f, distToNext / 2f, radius);

            if (maxRadius < 0.001f)
            {
                result.Add(new Vector3(curr.x, curr.y, 0));
                continue;
            }

            // 모서리 시작점과 끝점
            Vector2 cornerStart = curr + toPrev * maxRadius;
            Vector2 cornerEnd = curr + toNext * maxRadius;

            // 호를 따라 점 생성
            for (int j = 0; j <= segmentsPerCorner; j++)
            {
                float t = (float)j / segmentsPerCorner;

                // 베지어 커브로 더 부드럽게 (간단한 2차 베지어)
                Vector2 q0 = Vector2.Lerp(cornerStart, curr, t);
                Vector2 q1 = Vector2.Lerp(curr, cornerEnd, t);
                Vector2 bezierPoint = Vector2.Lerp(q0, q1, t);

                result.Add(new Vector3(bezierPoint.x, bezierPoint.y, 0));
            }
        }

        return result;
    }

    /// <summary>
    /// 다각형의 Signed Area를 계산합니다.
    /// 양수면 반시계방향, 음수면 시계방향.
    /// </summary>
    private float CalculateSignedArea(Vector2[] points)
    {
        float area = 0f;
        int n = points.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % n];
            area += (next.x - curr.x) * (next.y + curr.y);
        }
        return area / 2f;
    }

    /// <summary>
    /// 다각형을 안쪽으로 수축시킵니다 (각 변을 내부로 평행 이동 후 교차점 계산).
    /// 오목한 모서리와 인접한 변은 이동하지 않습니다.
    /// </summary>
    private Vector2[] ShrinkPolygon(Vector2[] points, float offset)
    {
        if (points.Length < 3 || offset <= 0) return points;

        // 내부 방향 결정을 위해 다각형 중심 계산
        Vector2 center = CalculatePolygonCenter(points);

        // 다각형 방향 확인 (시계/반시계)
        float signedArea = CalculateSignedArea(points);
        bool isClockwise = signedArea < 0;

        int n = points.Length;

        // 먼저 각 모서리가 오목한지 판단
        bool[] isConcaveCorner = new bool[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = points[(i - 1 + n) % n];
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % n];

            Vector2 edge1 = curr - prev;
            Vector2 edge2 = next - curr;
            float cross = edge1.x * edge2.y - edge1.y * edge2.x;
            isConcaveCorner[i] = isClockwise ? (cross < 0) : (cross > 0);
        }

        // 각 변(edge)을 내부로 평행 이동
        // 단, 오목한 모서리에 연결된 변은 이동하지 않음
        Vector2[] offsetEdgeStart = new Vector2[n];
        Vector2[] offsetEdgeEnd = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % n];

            // 이 변의 시작점(i)이나 끝점((i+1)%n)이 오목한 모서리면 이동하지 않음
            int endIdx = (i + 1) % n;
            if (isConcaveCorner[i] || isConcaveCorner[endIdx])
            {
                // 오목한 모서리에 연결된 변은 원래 위치 유지
                offsetEdgeStart[i] = p1;
                offsetEdgeEnd[i] = p2;
                continue;
            }

            Vector2 edgeDir = (p2 - p1).normalized;
            // 법선 벡터 (오른쪽 방향)
            Vector2 normal = new Vector2(edgeDir.y, -edgeDir.x);

            // 법선이 내부(중심)를 향하도록 조정
            Vector2 edgeMid = (p1 + p2) * 0.5f;
            if (Vector2.Dot(normal, center - edgeMid) < 0)
            {
                normal = -normal;
            }

            // 변을 내부 방향으로 offset만큼 이동
            offsetEdgeStart[i] = p1 + normal * offset;
            offsetEdgeEnd[i] = p2 + normal * offset;
        }

        // 인접한 두 변의 교차점을 새 꼭짓점으로 사용
        Vector2[] result = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            // 오목한 모서리는 원래 점 유지
            if (isConcaveCorner[i])
            {
                result[i] = points[i];
                continue;
            }

            int prevIdx = (i - 1 + n) % n;

            // 이전 변: offsetEdgeStart[prevIdx] -> offsetEdgeEnd[prevIdx]
            // 현재 변: offsetEdgeStart[i] -> offsetEdgeEnd[i]
            if (LineLineIntersection(
                offsetEdgeStart[prevIdx], offsetEdgeEnd[prevIdx],
                offsetEdgeStart[i], offsetEdgeEnd[i],
                out Vector2 intersection))
            {
                result[i] = intersection;
            }
            else
            {
                // 평행한 경우 (거의 발생하지 않음) 원래 점 사용
                result[i] = points[i];
            }
        }

        return result;
    }

    /// <summary>
    /// 두 직선의 교차점을 계산합니다.
    /// </summary>
    private bool LineLineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        float d = (p1.x - p2.x) * (p3.y - p4.y) - (p1.y - p2.y) * (p3.x - p4.x);
        if (Mathf.Abs(d) < 0.0001f)
        {
            // 평행한 선
            return false;
        }

        float t = ((p1.x - p3.x) * (p3.y - p4.y) - (p1.y - p3.y) * (p3.x - p4.x)) / d;

        intersection = new Vector2(
            p1.x + t * (p2.x - p1.x),
            p1.y + t * (p2.y - p1.y)
        );

        return true;
    }

    /// <summary>
    /// 다각형의 중심점을 계산합니다.
    /// </summary>
    private Vector2 CalculatePolygonCenter(Vector2[] points)
    {
        Vector2 center = Vector2.zero;
        foreach (var p in points)
        {
            center += p;
        }
        return center / points.Length;
    }

    /// <summary>
    /// 다각형을 바깥쪽으로 확장시킵니다 (각 변을 외부로 평행 이동 후 교차점 계산).
    /// 오목한 모서리와 인접한 변은 이동하지 않습니다.
    /// </summary>
    private Vector2[] ExpandPolygon(Vector2[] points, float offset)
    {
        if (points.Length < 3 || offset <= 0) return points;

        // 외부 방향 결정을 위해 다각형 중심 계산
        Vector2 center = CalculatePolygonCenter(points);

        // 다각형 방향 확인 (시계/반시계)
        float signedArea = CalculateSignedArea(points);
        bool isClockwise = signedArea < 0;

        int n = points.Length;

        // 먼저 각 모서리가 오목한지 판단
        bool[] isConcaveCorner = new bool[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 prev = points[(i - 1 + n) % n];
            Vector2 curr = points[i];
            Vector2 next = points[(i + 1) % n];

            Vector2 edge1 = curr - prev;
            Vector2 edge2 = next - curr;
            float cross = edge1.x * edge2.y - edge1.y * edge2.x;
            isConcaveCorner[i] = isClockwise ? (cross < 0) : (cross > 0);
        }

        // 각 변(edge)을 외부로 평행 이동
        // 단, 오목한 모서리에 연결된 변은 이동하지 않음
        Vector2[] offsetEdgeStart = new Vector2[n];
        Vector2[] offsetEdgeEnd = new Vector2[n];

        for (int i = 0; i < n; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % n];

            // 이 변의 시작점(i)이나 끝점((i+1)%n)이 오목한 모서리면 이동하지 않음
            int endIdx = (i + 1) % n;
            if (isConcaveCorner[i] || isConcaveCorner[endIdx])
            {
                // 오목한 모서리에 연결된 변은 원래 위치 유지
                offsetEdgeStart[i] = p1;
                offsetEdgeEnd[i] = p2;
                continue;
            }

            Vector2 edgeDir = (p2 - p1).normalized;
            // 법선 벡터 (오른쪽 방향)
            Vector2 normal = new Vector2(edgeDir.y, -edgeDir.x);

            // 법선이 외부(중심 반대)를 향하도록 조정
            Vector2 edgeMid = (p1 + p2) * 0.5f;
            if (Vector2.Dot(normal, center - edgeMid) > 0)
            {
                normal = -normal;
            }

            // 변을 외부 방향으로 offset만큼 이동
            offsetEdgeStart[i] = p1 + normal * offset;
            offsetEdgeEnd[i] = p2 + normal * offset;
        }

        // 인접한 두 변의 교차점을 새 꼭짓점으로 사용
        Vector2[] result = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            // 오목한 모서리는 원래 점 유지
            if (isConcaveCorner[i])
            {
                result[i] = points[i];
                continue;
            }

            int prevIdx = (i - 1 + n) % n;

            // 이전 변: offsetEdgeStart[prevIdx] -> offsetEdgeEnd[prevIdx]
            // 현재 변: offsetEdgeStart[i] -> offsetEdgeEnd[i]
            if (LineLineIntersection(
                offsetEdgeStart[prevIdx], offsetEdgeEnd[prevIdx],
                offsetEdgeStart[i], offsetEdgeEnd[i],
                out Vector2 intersection))
            {
                result[i] = intersection;
            }
            else
            {
                // 평행한 경우 (거의 발생하지 않음) 원래 점 사용
                result[i] = points[i];
            }
        }

        return result;
    }

    /// <summary>
    /// 위치를 업데이트합니다 (드래그 중 호출).
    /// </summary>
    public void UpdatePosition()
    {
        if (_pieces.Count == 0) return;

        // 조각들의 콜라이더 위치 업데이트 (로컬 좌표로 설정)
        for (int i = 0; i < _pieces.Count && i < _childColliders.Count; i++)
        {
            if (_childColliders[i] != null && _pieces[i] != null)
            {
                // GroupBorder가 (0,0,0)에 있으므로 조각의 월드 좌표가 곧 로컬 좌표
                _childColliders[i].transform.localPosition = _pieces[i].transform.position;
            }
        }

        // 외곽선 업데이트
        Physics2D.SyncTransforms();
        _compositeCollider.GenerateGeometry();
        UpdateBorderFromCollider();
    }

    // 펌핑 애니메이션용 - 원본 LineRenderer 점들 저장
    private Vector3[] _originalWhiteLinePositions;
    private Vector3[] _originalBlackLinePositions;
    private Vector3 _originalCenter;
    private bool _hasOriginalPositions = false;

    /// <summary>
    /// 펌핑 애니메이션용 - 그룹 중심 기준으로 스케일 적용
    /// </summary>
    public void UpdatePositionWithScale(Vector3 groupCenter, float scale)
    {
        if (_whiteLineRenderer == null || _blackLineRenderer == null) return;

        // 원본 위치가 없으면 현재 위치를 원본으로 저장
        if (!_hasOriginalPositions && _whiteLineRenderer.positionCount > 0)
        {
            _originalWhiteLinePositions = new Vector3[_whiteLineRenderer.positionCount];
            _originalBlackLinePositions = new Vector3[_blackLineRenderer.positionCount];
            _whiteLineRenderer.GetPositions(_originalWhiteLinePositions);
            _blackLineRenderer.GetPositions(_originalBlackLinePositions);
            _originalCenter = groupCenter;
            _hasOriginalPositions = true;
        }

        if (!_hasOriginalPositions) return;

        // 원본 위치를 기준으로 스케일 적용
        for (int i = 0; i < _originalWhiteLinePositions.Length; i++)
        {
            Vector3 offset = _originalWhiteLinePositions[i] - _originalCenter;
            Vector3 scaledPos = groupCenter + offset * scale;
            _whiteLineRenderer.SetPosition(i, scaledPos);
        }

        for (int i = 0; i < _originalBlackLinePositions.Length; i++)
        {
            Vector3 offset = _originalBlackLinePositions[i] - _originalCenter;
            Vector3 scaledPos = groupCenter + offset * scale;
            _blackLineRenderer.SetPosition(i, scaledPos);
        }
    }

    /// <summary>
    /// 펌핑 애니메이션 완료 후 원본 위치 데이터 초기화
    /// </summary>
    public void ResetScaleData()
    {
        _hasOriginalPositions = false;
        _originalWhiteLinePositions = null;
        _originalBlackLinePositions = null;
    }

    /// <summary>
    /// Sorting Order를 설정합니다.
    /// </summary>
    public void SetSortingOrder(int order)
    {
        _baseSortingOrder = order;
        if (_blackLineRenderer != null)
            _blackLineRenderer.sortingOrder = order;
        if (_whiteLineRenderer != null)
            _whiteLineRenderer.sortingOrder = order + 1;
    }

    /// <summary>
    /// 테두리 두께를 설정합니다.
    /// </summary>
    public void SetBorderWidth(float whiteWidth, float blackWidth)
    {
        _whiteBorderWidth = whiteWidth;
        _blackBorderWidth = blackWidth;

        // LineRenderer는 중심선 기준으로 양쪽으로 퍼져서 그려짐
        // 테두리가 이미지 경계 바깥에 그려지려면:
        // - 콜라이더 확장량(_shrinkOffset)만 수축하면 이미지 경계에 맞춰짐
        // - 그 상태에서 LineRenderer가 중심선 기준으로 그려지면 절반은 이미지 안쪽으로 들어감
        // - 테두리가 완전히 이미지 바깥에 그려지려면 추가로 바깥쪽으로 확장해야 함
        // - 즉, _borderCenterOffset을 음수로 설정하여 외곽선을 바깥으로 이동
        float totalBorderWidth = whiteWidth + blackWidth;
        _borderCenterOffset = -totalBorderWidth * 0.5f;  // 테두리 절반만큼 바깥으로 확장

        Debug.Log($"[GroupBorderRenderer] SetBorderWidth - white={whiteWidth:F4}, black={blackWidth:F4}, totalBorder={totalBorderWidth:F4}, borderCenterOffset={_borderCenterOffset:F4}");

        if (_whiteLineRenderer != null)
        {
            _whiteLineRenderer.startWidth = whiteWidth;
            _whiteLineRenderer.endWidth = whiteWidth;
        }

        if (_blackLineRenderer != null)
        {
            _blackLineRenderer.startWidth = whiteWidth + blackWidth;
            _blackLineRenderer.endWidth = whiteWidth + blackWidth;
        }
    }

    /// <summary>
    /// 모서리 반경을 설정합니다 (World Space).
    /// </summary>
    public void SetCornerRadius(float radius)
    {
        _cornerRadius = radius;
        Debug.Log($"[GroupBorderRenderer] SetCornerRadius - radius={radius:F4}");
    }

    /// <summary>
    /// 테두리를 표시하거나 숨깁니다.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (_whiteLineRenderer != null)
            _whiteLineRenderer.enabled = visible;
        if (_blackLineRenderer != null)
            _blackLineRenderer.enabled = visible;
    }

    private void OnDestroy()
    {
        ClearChildColliders();
    }
}
