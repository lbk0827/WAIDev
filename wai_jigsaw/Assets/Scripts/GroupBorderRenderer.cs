using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 그리드 기반 직접 계산 방식으로 그룹 외곽선을 그리는 컴포넌트.
/// CompositeCollider2D 대신 조각들의 위치를 기반으로 외곽선을 직접 계산합니다.
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
    [SerializeField] private int _baseSortingOrder = 2;  // 기본값은 낮게 (드래그 시 PieceGroup.SetSortingOrder에서 높여줌)

    // 컴포넌트 참조
    private LineRenderer _whiteLineRenderer;
    private LineRenderer _blackLineRenderer;

    // 조각 정보
    private List<DragController> _pieces = new List<DragController>();

    // 조각 크기 (첫 번째 조각에서 가져옴)
    private float _pieceWidth;
    private float _pieceHeight;

    // 테두리 중심 보정 오프셋
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
        // LineRenderer 생성 (검은색 - 바깥쪽, 먼저 그림)
        GameObject blackLineObj = new GameObject("BlackBorderLine");
        blackLineObj.transform.SetParent(transform, false);
        blackLineObj.transform.localPosition = Vector3.zero;
        blackLineObj.transform.localRotation = Quaternion.identity;
        blackLineObj.transform.localScale = Vector3.one;
        _blackLineRenderer = blackLineObj.AddComponent<LineRenderer>();
        SetupLineRenderer(_blackLineRenderer, _blackColor, 0f, _baseSortingOrder);

        // LineRenderer 생성 (흰색 - 안쪽, 나중에 그림)
        GameObject whiteLineObj = new GameObject("WhiteBorderLine");
        whiteLineObj.transform.SetParent(transform, false);
        whiteLineObj.transform.localPosition = Vector3.zero;
        whiteLineObj.transform.localRotation = Quaternion.identity;
        whiteLineObj.transform.localScale = Vector3.one;
        _whiteLineRenderer = whiteLineObj.AddComponent<LineRenderer>();
        SetupLineRenderer(_whiteLineRenderer, _whiteColor, 0f, _baseSortingOrder + 1);
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

        // Material 설정
        Material baseMaterial = Resources.Load<Material>("Materials/LineBorderMaterial");
        if (baseMaterial != null)
        {
            lr.material = new Material(baseMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lr.material = new Material(shader);
            }
            else
            {
                Debug.LogWarning("[GroupBorderRenderer] Sprites/Default 셰이더를 찾을 수 없습니다. 기본 Material을 사용합니다.");
                lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
            }
        }
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = sortingOrder;
        lr.sortingLayerName = "Default";
    }

    /// <summary>
    /// 그룹의 조각들을 설정하고 테두리를 업데이트합니다.
    /// </summary>
    public void SetPieces(List<DragController> pieces)
    {
        // 드래그 기준 위치 초기화
        ResetDragBasePositions();

        _pieces.Clear();
        _pieces.AddRange(pieces);

        if (pieces.Count == 0)
        {
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            return;
        }

        // 첫 번째 조각에서 크기 정보 가져오기
        var firstPiece = pieces[0];
        _pieceWidth = firstPiece.pieceWidth;
        _pieceHeight = firstPiece.pieceHeight;

        // 디버그: 전달받은 조각들의 위치 확인
        Debug.Log($"[GroupBorderRenderer] SetPieces 호출: 조각수={pieces.Count}, 첫조각={firstPiece.name}, 첫조각위치=({firstPiece.transform.position.x:F3}, {firstPiece.transform.position.y:F3}), pieceWidth={_pieceWidth:F3}, pieceHeight={_pieceHeight:F3}");

        // pieceWidth/pieceHeight가 0인 경우 SpriteRenderer에서 계산
        if (_pieceWidth <= 0 || _pieceHeight <= 0)
        {
            SpriteRenderer sr = firstPiece.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 spriteSize = sr.sprite.bounds.size;
                Vector3 scale = firstPiece.transform.localScale;
                _pieceWidth = spriteSize.x * scale.x;
                _pieceHeight = spriteSize.y * scale.y;
                Debug.Log($"[GroupBorderRenderer] SpriteRenderer에서 크기 계산: pieceWidth={_pieceWidth:F3}, pieceHeight={_pieceHeight:F3}");
            }
        }

        // 외곽선 계산 및 적용
        CalculateAndApplyOutline();
    }

    /// <summary>
    /// 외곽선을 계산하고 LineRenderer에 적용합니다.
    /// </summary>
    private void CalculateAndApplyOutline()
    {
        if (_pieces.Count == 0 || _pieceWidth <= 0 || _pieceHeight <= 0)
        {
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            Debug.LogWarning($"[GroupBorderRenderer] CalculateAndApplyOutline 중단: pieceCount={_pieces.Count}, pieceWidth={_pieceWidth}, pieceHeight={_pieceHeight}");
            return;
        }

        // 1. 조각들의 월드 위치 수집 (실제 화면 위치 기반)
        List<Vector3> worldPositions = new List<Vector3>();

        foreach (var piece in _pieces)
        {
            // null 체크 (조각이 파괴된 경우)
            if (piece == null) continue;
            worldPositions.Add(piece.transform.position);
        }

        // 유효한 조각이 없으면 리턴
        if (worldPositions.Count == 0)
        {
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            return;
        }

        // 2. 외곽 변(Edge) 찾기 - 월드 위치 기반으로 인접 판단
        List<Edge> outerEdges = FindOuterEdgesFromWorldPositions(worldPositions);

        if (outerEdges.Count == 0)
        {
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            return;
        }

        // 3. 외곽 변들을 연결하여 폐곡선 생성
        List<Vector2> outlinePoints = ConnectEdgesToPath(outerEdges);

        if (outlinePoints.Count < 3)
        {
            _whiteLineRenderer.positionCount = 0;
            _blackLineRenderer.positionCount = 0;
            return;
        }

        // 4. 둥근 모서리 적용
        List<Vector3> smoothedPoints = ApplyRoundedCorners(outlinePoints.ToArray(), _cornerRadius);

        // 5. LineRenderer에 적용
        _whiteLineRenderer.positionCount = smoothedPoints.Count;
        _blackLineRenderer.positionCount = smoothedPoints.Count;

        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            Vector3 worldPos = smoothedPoints[i];
            worldPos.z = 0f;
            _whiteLineRenderer.SetPosition(i, worldPos);
            _blackLineRenderer.SetPosition(i, worldPos);
        }

        // 디버그: 테두리 중심과 조각 중심 비교
        if (_pieces.Count > 0 && _pieces[0] != null && smoothedPoints.Count > 0)
        {
            // 조각들의 바운딩 박스 중심 계산
            Vector3 piecesMin = _pieces[0].transform.position;
            Vector3 piecesMax = _pieces[0].transform.position;
            foreach (var piece in _pieces)
            {
                if (piece == null) continue;
                Vector3 pos = piece.transform.position;
                piecesMin = Vector3.Min(piecesMin, pos);
                piecesMax = Vector3.Max(piecesMax, pos);
            }
            Vector3 piecesCenter = (piecesMin + piecesMax) / 2f;

            // 테두리의 바운딩 박스 중심 계산
            Vector3 borderMin = smoothedPoints[0];
            Vector3 borderMax = smoothedPoints[0];
            foreach (var pt in smoothedPoints)
            {
                borderMin = Vector3.Min(borderMin, pt);
                borderMax = Vector3.Max(borderMax, pt);
            }
            Vector3 borderCenter = (borderMin + borderMax) / 2f;

            Vector3 offset = piecesCenter - borderCenter;
            Debug.Log($"[GroupBorderRenderer] CalculateAndApplyOutline 완료: pieceWidth={_pieceWidth:F3}, pieceHeight={_pieceHeight:F3}, " +
                      $"조각중심=({piecesCenter.x:F3}, {piecesCenter.y:F3}), 테두리중심=({borderCenter.x:F3}, {borderCenter.y:F3}), " +
                      $"오프셋=({offset.x:F3}, {offset.y:F3})");
        }
    }

    /// <summary>
    /// 월드 위치 기반으로 외곽 변(인접 조각이 없는 변)을 찾습니다.
    /// 실제 화면 위치 간의 거리로 인접 여부를 판단합니다.
    /// 각 변은 시계 방향으로 정의됩니다 (외곽선을 따라 시계 방향으로 순회).
    /// </summary>
    private List<Edge> FindOuterEdgesFromWorldPositions(List<Vector3> worldPositions)
    {
        List<Edge> edges = new List<Edge>();

        float halfW = _pieceWidth / 2f;
        float halfH = _pieceHeight / 2f;

        // 인접 판단을 위한 거리 임계값 (조각 크기의 10% 오차 허용)
        float toleranceX = _pieceWidth * 0.1f;
        float toleranceY = _pieceHeight * 0.1f;

        foreach (var cellCenter in worldPositions)
        {
            // 4방향에 인접 조각이 있는지 검사
            bool hasTop = false;
            bool hasBottom = false;
            bool hasLeft = false;
            bool hasRight = false;

            foreach (var otherPos in worldPositions)
            {
                if (otherPos == cellCenter) continue;

                float dx = otherPos.x - cellCenter.x;
                float dy = otherPos.y - cellCenter.y;

                // 위쪽 인접 (dy가 pieceHeight만큼 크고, dx는 거의 0)
                if (Mathf.Abs(dy - _pieceHeight) < toleranceY && Mathf.Abs(dx) < toleranceX)
                {
                    hasTop = true;
                }
                // 아래쪽 인접
                if (Mathf.Abs(dy + _pieceHeight) < toleranceY && Mathf.Abs(dx) < toleranceX)
                {
                    hasBottom = true;
                }
                // 오른쪽 인접
                if (Mathf.Abs(dx - _pieceWidth) < toleranceX && Mathf.Abs(dy) < toleranceY)
                {
                    hasRight = true;
                }
                // 왼쪽 인접
                if (Mathf.Abs(dx + _pieceWidth) < toleranceX && Mathf.Abs(dy) < toleranceY)
                {
                    hasLeft = true;
                }
            }

            // 인접 조각이 없는 방향의 변을 외곽 변으로 추가
            // 시계 방향으로 정의: Top(→), Right(↓), Bottom(←), Left(↑)
            if (!hasTop)
            {
                // Top: 왼쪽에서 오른쪽으로
                Vector2 start = new Vector2(cellCenter.x - halfW, cellCenter.y + halfH);
                Vector2 end = new Vector2(cellCenter.x + halfW, cellCenter.y + halfH);
                edges.Add(new Edge(start, end, EdgeDirection.Top));
            }

            if (!hasRight)
            {
                // Right: 위에서 아래로
                Vector2 start = new Vector2(cellCenter.x + halfW, cellCenter.y + halfH);
                Vector2 end = new Vector2(cellCenter.x + halfW, cellCenter.y - halfH);
                edges.Add(new Edge(start, end, EdgeDirection.Right));
            }

            if (!hasBottom)
            {
                // Bottom: 오른쪽에서 왼쪽으로
                Vector2 start = new Vector2(cellCenter.x + halfW, cellCenter.y - halfH);
                Vector2 end = new Vector2(cellCenter.x - halfW, cellCenter.y - halfH);
                edges.Add(new Edge(start, end, EdgeDirection.Bottom));
            }

            if (!hasLeft)
            {
                // Left: 아래에서 위로
                Vector2 start = new Vector2(cellCenter.x - halfW, cellCenter.y - halfH);
                Vector2 end = new Vector2(cellCenter.x - halfW, cellCenter.y + halfH);
                edges.Add(new Edge(start, end, EdgeDirection.Left));
            }
        }

        return edges;
    }

    /// <summary>
    /// 외곽 변들을 연결하여 연속된 폐곡선 경로를 생성합니다.
    /// 거리 기반 비교로 부동소수점 오차 문제 해결.
    /// </summary>
    private List<Vector2> ConnectEdgesToPath(List<Edge> edges)
    {
        if (edges.Count == 0) return new List<Vector2>();

        // 거리 기반 비교를 위한 임계값
        // 드래그/이동 중 조각 위치가 미세하게 어긋날 수 있으므로 조각 크기의 20%로 설정
        float tolerance = Mathf.Min(_pieceWidth, _pieceHeight) * 0.2f;
        if (tolerance < 0.01f) tolerance = 0.01f;

        List<Vector2> path = new List<Vector2>();
        HashSet<int> usedEdges = new HashSet<int>();

        // 시작 변 선택: 첫 번째 변 사용
        Edge currentEdge = edges[0];
        usedEdges.Add(0);
        path.Add(currentEdge.Start);
        path.Add(currentEdge.End);

        Vector2 currentEnd = currentEdge.End;
        Vector2 pathStart = path[0];

        // 모든 변이 연결될 때까지 반복
        int maxIterations = edges.Count * 2;
        int iterations = 0;

        while (usedEdges.Count < edges.Count && iterations < maxIterations)
        {
            iterations++;
            bool foundNext = false;
            float bestDistance = float.MaxValue;
            int bestEdgeIdx = -1;
            bool bestUseStart = true; // true: candidate.Start와 연결, false: candidate.End와 연결

            // 현재 끝점과 가장 가까운 미사용 변 찾기
            for (int i = 0; i < edges.Count; i++)
            {
                if (usedEdges.Contains(i)) continue;

                Edge candidate = edges[i];

                float distToStart = Vector2.Distance(currentEnd, candidate.Start);
                float distToEnd = Vector2.Distance(currentEnd, candidate.End);

                if (distToStart < bestDistance && distToStart < tolerance)
                {
                    bestDistance = distToStart;
                    bestEdgeIdx = i;
                    bestUseStart = true;
                }

                if (distToEnd < bestDistance && distToEnd < tolerance)
                {
                    bestDistance = distToEnd;
                    bestEdgeIdx = i;
                    bestUseStart = false;
                }
            }

            if (bestEdgeIdx >= 0)
            {
                Edge nextEdge = edges[bestEdgeIdx];
                if (bestUseStart)
                {
                    // candidate.Start가 currentEnd와 연결
                    path.Add(nextEdge.End);
                    currentEnd = nextEdge.End;
                }
                else
                {
                    // candidate.End가 currentEnd와 연결 (역방향)
                    path.Add(nextEdge.Start);
                    currentEnd = nextEdge.Start;
                }
                usedEdges.Add(bestEdgeIdx);
                foundNext = true;
            }

            if (!foundNext)
            {
                // 연결이 끊어진 경우: 시작점으로 돌아갈 수 있는지 확인
                if (Vector2.Distance(currentEnd, pathStart) < tolerance)
                {
                    // 폐곡선 완성됨
                    break;
                }

                // 디버그: 연결 실패 시 상세 정보 출력
                Debug.LogWarning($"[GroupBorderRenderer] 경로 연결 실패. 사용된 변: {usedEdges.Count}/{edges.Count}");
                Debug.LogWarning($"  현재 끝점: ({currentEnd.x:F2}, {currentEnd.y:F2}), tolerance: {tolerance:F4}");
                Debug.LogWarning($"  조각 크기: width={_pieceWidth:F2}, height={_pieceHeight:F2}");

                // 미사용 변들의 좌표 출력
                for (int i = 0; i < edges.Count; i++)
                {
                    if (usedEdges.Contains(i)) continue;
                    Edge e = edges[i];
                    float d1 = Vector2.Distance(currentEnd, e.Start);
                    float d2 = Vector2.Distance(currentEnd, e.End);
                    Debug.LogWarning($"  미사용 변[{i}]: Start({e.Start.x:F2}, {e.Start.y:F2}) dist={d1:F4}, End({e.End.x:F2}, {e.End.y:F2}) dist={d2:F4}");
                }
                break;
            }
        }

        // 시작점과 끝점이 같으면 마지막 점 제거 (LineRenderer.loop=true이므로)
        if (path.Count > 1 && Vector2.Distance(path[0], path[path.Count - 1]) < tolerance)
        {
            path.RemoveAt(path.Count - 1);
        }

        return path;
    }

    /// <summary>
    /// 좌표를 그리드 단위로 스냅합니다 (정수 좌표로 변환).
    /// </summary>
    private Vector2Int SnapToGrid(Vector2 point, float unit)
    {
        return new Vector2Int(
            Mathf.RoundToInt(point.x / unit),
            Mathf.RoundToInt(point.y / unit)
        );
    }

    /// <summary>
    /// 다각형의 모서리를 둥글게 처리합니다.
    /// 볼록한 모서리(Convex)만 둥글게 하고, 오목한 모서리(Concave)는 직각 유지.
    /// </summary>
    private List<Vector3> ApplyRoundedCorners(Vector2[] points, float radius)
    {
        List<Vector3> result = new List<Vector3>();

        if (radius <= 0.001f || points.Length < 3)
        {
            foreach (var p in points)
            {
                result.Add(new Vector3(p.x, p.y, 0));
            }
            return result;
        }

        // 다각형의 방향 확인 (시계/반시계)
        float signedArea = CalculateSignedArea(points);
        bool isClockwise = signedArea < 0;

        int segmentsPerCorner = 4;

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

            bool isConcave = isClockwise ? (cross < 0) : (cross > 0);

            // 오목한 모서리는 직각 유지
            if (isConcave)
            {
                result.Add(new Vector3(curr.x, curr.y, 0));
                continue;
            }

            float distToPrev = Vector2.Distance(prev, curr);
            float distToNext = Vector2.Distance(curr, next);

            float maxRadius = Mathf.Min(distToPrev / 2f, distToNext / 2f, radius);

            if (maxRadius < 0.001f)
            {
                result.Add(new Vector3(curr.x, curr.y, 0));
                continue;
            }

            Vector2 cornerStart = curr + toPrev * maxRadius;
            Vector2 cornerEnd = curr + toNext * maxRadius;

            for (int j = 0; j <= segmentsPerCorner; j++)
            {
                float t = (float)j / segmentsPerCorner;
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
    /// 위치를 업데이트합니다 (드래그 중 호출).
    /// </summary>
    public void UpdatePosition()
    {
        if (_pieces.Count == 0) return;
        if (_whiteLineRenderer == null || _blackLineRenderer == null) return;

        Vector3 currentGroupCenter = CalculateGroupCenter();

        if (!_hasDragBasePositions && _whiteLineRenderer.positionCount > 0)
        {
            _dragBaseWhitePositions = new Vector3[_whiteLineRenderer.positionCount];
            _dragBaseBlackPositions = new Vector3[_blackLineRenderer.positionCount];
            _whiteLineRenderer.GetPositions(_dragBaseWhitePositions);
            _blackLineRenderer.GetPositions(_dragBaseBlackPositions);
            _dragBaseGroupCenter = currentGroupCenter;
            _hasDragBasePositions = true;
        }

        if (!_hasDragBasePositions) return;

        if (_whiteLineRenderer.positionCount != _dragBaseWhitePositions.Length ||
            _blackLineRenderer.positionCount != _dragBaseBlackPositions.Length)
        {
            _hasDragBasePositions = false;
            return;
        }

        Vector3 delta = currentGroupCenter - _dragBaseGroupCenter;

        for (int i = 0; i < _dragBaseWhitePositions.Length; i++)
        {
            _whiteLineRenderer.SetPosition(i, _dragBaseWhitePositions[i] + delta);
        }

        for (int i = 0; i < _dragBaseBlackPositions.Length; i++)
        {
            _blackLineRenderer.SetPosition(i, _dragBaseBlackPositions[i] + delta);
        }
    }

    /// <summary>
    /// 그룹의 중심점을 계산합니다.
    /// </summary>
    private Vector3 CalculateGroupCenter()
    {
        if (_pieces.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        foreach (var piece in _pieces)
        {
            if (piece != null)
            {
                sum += piece.transform.position;
            }
        }
        return sum / _pieces.Count;
    }

    /// <summary>
    /// 드래그 기준 위치 데이터를 초기화합니다.
    /// </summary>
    public void ResetDragBasePositions()
    {
        _hasDragBasePositions = false;
        _dragBaseWhitePositions = null;
        _dragBaseBlackPositions = null;
    }

    // 펌핑 애니메이션용 - 원본 LineRenderer 점들 저장
    private Vector3[] _originalWhiteLinePositions;
    private Vector3[] _originalBlackLinePositions;
    private Vector3 _originalCenter;
    private bool _hasOriginalPositions = false;

    // 드래그용 - 기준 LineRenderer 점들 및 그룹 중심 저장
    private Vector3[] _dragBaseWhitePositions;
    private Vector3[] _dragBaseBlackPositions;
    private Vector3 _dragBaseGroupCenter;
    private bool _hasDragBasePositions = false;

    /// <summary>
    /// 펌핑 애니메이션용 - 그룹 중심 기준으로 스케일 적용
    /// </summary>
    public void UpdatePositionWithScale(Vector3 groupCenter, float scale)
    {
        if (_whiteLineRenderer == null || _blackLineRenderer == null) return;

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

        if (_whiteLineRenderer.positionCount != _originalWhiteLinePositions.Length ||
            _blackLineRenderer.positionCount != _originalBlackLinePositions.Length)
        {
            _hasOriginalPositions = false;
            return;
        }

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

        _borderCenterOffset = 0f;

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

    /// <summary>
    /// LineRenderer의 모든 점을 지정된 오프셋만큼 이동합니다.
    /// useWorldSpace=true일 때 Transform 이동으로는 점이 이동하지 않으므로 직접 이동 필요.
    /// </summary>
    public void MoveAllPoints(Vector3 offset)
    {
        bool moved = false;
        int whiteCount = _whiteLineRenderer != null ? _whiteLineRenderer.positionCount : 0;
        int blackCount = _blackLineRenderer != null ? _blackLineRenderer.positionCount : 0;

        Debug.Log($"[GroupBorderRenderer] MoveAllPoints 시작: whiteCount={whiteCount}, blackCount={blackCount}, offset={offset}, gameObject={gameObject.name}");

        if (_whiteLineRenderer != null && _whiteLineRenderer.positionCount > 0)
        {
            Vector3[] positions = new Vector3[_whiteLineRenderer.positionCount];
            _whiteLineRenderer.GetPositions(positions);

            // 이동 전 첫 번째 점 좌표
            Vector3 beforeFirst = positions[0];

            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] += offset;
            }
            _whiteLineRenderer.SetPositions(positions);

            // 이동 후 첫 번째 점 좌표 확인
            Vector3[] afterPositions = new Vector3[_whiteLineRenderer.positionCount];
            _whiteLineRenderer.GetPositions(afterPositions);

            Debug.Log($"[GroupBorderRenderer] White 첫번째 점: 이전=({beforeFirst.x:F3}, {beforeFirst.y:F3}) → 이후=({afterPositions[0].x:F3}, {afterPositions[0].y:F3})");

            moved = true;
        }

        if (_blackLineRenderer != null && _blackLineRenderer.positionCount > 0)
        {
            Vector3[] positions = new Vector3[_blackLineRenderer.positionCount];
            _blackLineRenderer.GetPositions(positions);
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] += offset;
            }
            _blackLineRenderer.SetPositions(positions);
            moved = true;
        }

        if (!moved)
        {
            Debug.LogWarning($"[GroupBorderRenderer] MoveAllPoints: LineRenderer가 없거나 점이 없습니다. white={_whiteLineRenderer != null} (count={whiteCount}), black={_blackLineRenderer != null} (count={blackCount})");
        }
    }

    /// <summary>
    /// 테두리의 바운딩 박스 중심을 반환합니다.
    /// </summary>
    public Vector3 GetBorderCenter()
    {
        if (_whiteLineRenderer == null || _whiteLineRenderer.positionCount == 0)
            return Vector3.zero;

        Vector3[] positions = new Vector3[_whiteLineRenderer.positionCount];
        _whiteLineRenderer.GetPositions(positions);

        Vector3 min = positions[0];
        Vector3 max = positions[0];

        for (int i = 1; i < positions.Length; i++)
        {
            min = Vector3.Min(min, positions[i]);
            max = Vector3.Max(max, positions[i]);
        }

        return (min + max) / 2f;
    }

    /// <summary>
    /// 조각들의 바운딩 박스 중심을 반환합니다.
    /// </summary>
    public Vector3 GetPiecesCenter()
    {
        if (_pieces == null || _pieces.Count == 0)
            return Vector3.zero;

        Vector3 min = _pieces[0].transform.position;
        Vector3 max = _pieces[0].transform.position;

        foreach (var piece in _pieces)
        {
            if (piece == null) continue;
            Vector3 pos = piece.transform.position;
            min = Vector3.Min(min, pos);
            max = Vector3.Max(max, pos);
        }

        return (min + max) / 2f;
    }

    /// <summary>
    /// 테두리를 조각들의 중심에 맞춰 보정합니다.
    /// </summary>
    public void AlignBorderToPieces()
    {
        Vector3 borderCenter = GetBorderCenter();
        Vector3 piecesCenter = GetPiecesCenter();
        Vector3 offset = piecesCenter - borderCenter;

        Debug.Log($"[GroupBorderRenderer] AlignBorderToPieces 호출: 테두리중심=({borderCenter.x:F3}, {borderCenter.y:F3}), 조각중심=({piecesCenter.x:F3}, {piecesCenter.y:F3}), 오프셋=({offset.x:F3}, {offset.y:F3})");

        if (offset.sqrMagnitude > 0.001f)
        {
            Debug.Log($"[GroupBorderRenderer] AlignBorderToPieces: 테두리 이동 적용");
            MoveAllPoints(offset);
        }
        else
        {
            Debug.Log($"[GroupBorderRenderer] AlignBorderToPieces: 오프셋이 너무 작아 이동 생략 (sqrMag={offset.sqrMagnitude:F6})");
        }
    }

    /// <summary>
    /// 테두리를 지정된 중심 위치에 맞춰 보정합니다.
    /// </summary>
    public void AlignBorderToCenter(Vector3 targetCenter)
    {
        Vector3 borderCenter = GetBorderCenter();
        Vector3 offset = targetCenter - borderCenter;

        Debug.Log($"[GroupBorderRenderer] AlignBorderToCenter 호출: 테두리중심=({borderCenter.x:F3}, {borderCenter.y:F3}), 목표중심=({targetCenter.x:F3}, {targetCenter.y:F3}), 오프셋=({offset.x:F3}, {offset.y:F3})");

        if (offset.sqrMagnitude > 0.0001f)
        {
            Debug.Log($"[GroupBorderRenderer] AlignBorderToCenter: 테두리 이동 적용");
            MoveAllPoints(offset);
        }
        else
        {
            Debug.Log($"[GroupBorderRenderer] AlignBorderToCenter: 오프셋이 너무 작아 이동 생략 (sqrMag={offset.sqrMagnitude:F6})");
        }
    }

    // ====== Edge 구조체 ======

    private enum EdgeDirection
    {
        Top,
        Bottom,
        Left,
        Right
    }

    private struct Edge
    {
        public Vector2 Start;
        public Vector2 End;
        public EdgeDirection Direction;

        public Edge(Vector2 start, Vector2 end, EdgeDirection direction)
        {
            Start = start;
            End = end;
            Direction = direction;
        }
    }
}
