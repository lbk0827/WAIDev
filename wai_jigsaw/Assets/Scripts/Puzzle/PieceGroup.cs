using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 연결된 퍼즐 조각들의 그룹을 관리합니다.
/// - 그룹 내 조각들의 일괄 이동
/// - 그룹 병합 및 분리
/// - 그룹 테두리 렌더링 관리
/// </summary>
public class PieceGroup
{
    // ====== 조각 관리 ======
    public List<DragController> pieces = new List<DragController>();

    // ====== 드래그 상태 ======
    private Dictionary<DragController, Vector3> _startPositions = new Dictionary<DragController, Vector3>();
    private Vector3 _mouseStartWorldPos;

    // ====== 그룹 테두리 렌더러 ======
    private GameObject _borderContainer;
    private GroupBorderRenderer _borderRenderer;

    #region Piece Management

    /// <summary>
    /// 그룹에 조각을 추가합니다.
    /// </summary>
    public void AddPiece(DragController piece)
    {
        if (!pieces.Contains(piece))
        {
            pieces.Add(piece);
            piece.group = this;
        }
    }

    /// <summary>
    /// 그룹에서 조각을 제거합니다.
    /// </summary>
    public void RemovePiece(DragController piece)
    {
        if (pieces.Contains(piece))
        {
            pieces.Remove(piece);
        }
    }

    /// <summary>
    /// 두 그룹을 병합합니다 (기본 병합).
    /// </summary>
    public void MergeGroup(PieceGroup otherGroup)
    {
        if (otherGroup == this) return;

        // 이전 그룹의 테두리 제거
        otherGroup.DestroyGroupBorder();

        foreach (var piece in otherGroup.pieces)
        {
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();
    }

    /// <summary>
    /// 두 그룹을 병합합니다 (카드 이동 없음 - EdgeCover 제거로 이미지 연결).
    /// 카드들은 슬롯 위치에 그대로 유지되고, EdgeCover만 제거되어 이미지가 연결됩니다.
    /// </summary>
    public void MergeGroupWithSnap(PieceGroup otherGroup, DragController anchorPiece, DragController connectingPiece)
    {
        if (otherGroup == this) return;

        // [DEBUG] 그룹 병합 로깅 (틈 버그 디버깅용)
        Debug.Log($"[GapDebug] MergeGroupWithSnap - " +
                  $"Anchor: Grid({anchorPiece.originalGridX},{anchorPiece.originalGridY}) Pos:{anchorPiece.transform.position}, " +
                  $"Connecting: Grid({connectingPiece.originalGridX},{connectingPiece.originalGridY}) Pos:{connectingPiece.transform.position}, " +
                  $"ThisGroupCount: {pieces.Count}, OtherGroupCount: {otherGroup.pieces.Count}");

        // 이전 그룹의 테두리 제거
        otherGroup.DestroyGroupBorder();

        // 카드 위치 이동 없이 그룹만 병합
        // EdgeCover 제거는 CheckNeighbor에서 처리됨
        foreach (var piece in otherGroup.pieces)
        {
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();

        // [DEBUG] 병합 후 상태 로깅
        Debug.Log($"[GapDebug] MergeGroupWithSnap Complete - NewGroupCount: {pieces.Count}");
    }

    #endregion

    #region Drag Handling

    /// <summary>
    /// 드래그 시작 시 호출됩니다.
    /// </summary>
    public void OnDragStart(Vector3 mousePos)
    {
        _mouseStartWorldPos = mousePos;
        _startPositions.Clear();
        foreach (var piece in pieces)
        {
            _startPositions[piece] = piece.transform.position;
        }

        // 드래그 기준 위치 초기화 (드래그 시작 시 새로운 기준점 설정을 위해)
        if (_borderRenderer != null)
        {
            _borderRenderer.ResetDragBasePositions();
        }
    }

    /// <summary>
    /// 드래그 중 호출됩니다.
    /// </summary>
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

        // 그룹 테두리 위치도 업데이트
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePosition();
        }
    }

    #endregion

    #region Sorting Order

    /// <summary>
    /// 그룹 내 모든 조각의 정렬 순서를 설정합니다.
    /// </summary>
    public void SetSortingOrder(int order)
    {
        foreach (var piece in pieces)
        {
            // 메인 퍼즐 이미지
            piece.GetComponent<SpriteRenderer>().sortingOrder = order;

            // 카드 뒷면 (맨 위)
            if (piece.CardBackRenderer != null)
            {
                piece.CardBackRenderer.sortingOrder = order + 10;
            }

            // 그룹이 2개 이상이면 개별 프레임은 숨기고 그룹 테두리 사용
            if (pieces.Count >= 2)
            {
                piece.ShowFrames(false);
            }
            else
            {
                // 단독 조각이면 개별 프레임 표시
                piece.ShowFrames(true);

                // 프레임 테두리 (퍼즐 이미지 위, 오버레이 방식)
                var allRenderers = piece.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in allRenderers)
                {
                    // 자기 자신, 카드 뒷면 제외
                    if (sr.gameObject == piece.gameObject) continue;
                    if (piece.CardBackRenderer != null && sr == piece.CardBackRenderer) continue;

                    // 프레임 종류에 따라 다른 order 적용 (퍼즐 이미지 위에 오버레이)
                    if (sr.gameObject.name == "WhiteFrame")
                        sr.sortingOrder = order + 1;  // 퍼즐 이미지 위
                    else if (sr.gameObject.name == "BlackFrame")
                        sr.sortingOrder = order + 2;  // 하얀 프레임 위
                    else
                        sr.sortingOrder = order + 1;
                }
            }
        }

        // 그룹 테두리 sorting order 업데이트
        if (_borderRenderer != null)
        {
            _borderRenderer.SetSortingOrder(order + 1);
        }
    }

    #endregion

    #region Group Border

    /// <summary>
    /// 그룹 테두리를 업데이트합니다. 2개 이상 조각이 있으면 그룹 테두리 생성/업데이트.
    /// </summary>
    public void UpdateGroupBorder()
    {
        if (pieces.Count < 2)
        {
            // 단독 조각이면 그룹 테두리 제거하고 개별 프레임 표시
            DestroyGroupBorder();
            if (pieces.Count == 1)
            {
                pieces[0].ShowFrames(true);
                pieces[0].ShowAllBorders();
            }
            return;
        }

        // 2개 이상: 개별 프레임 숨기고 그룹 테두리 생성/업데이트
        foreach (var piece in pieces)
        {
            piece.ShowFrames(false);
        }

        CreateOrUpdateGroupBorder();
    }

    /// <summary>
    /// 그룹 테두리를 생성하거나 업데이트합니다.
    /// </summary>
    private void CreateOrUpdateGroupBorder()
    {
        if (pieces.Count < 2) return;

        // 컨테이너가 없으면 생성
        if (_borderContainer == null)
        {
            _borderContainer = new GameObject("GroupBorder");

            // 명시적으로 원점에 배치하고 스케일 1로 설정 (좌표 계산의 기준점)
            _borderContainer.transform.position = Vector3.zero;
            _borderContainer.transform.rotation = Quaternion.identity;
            _borderContainer.transform.localScale = Vector3.one;

            _borderRenderer = _borderContainer.AddComponent<GroupBorderRenderer>();

            // 테두리 두께 및 모서리 반경 설정 (첫 번째 조각 기준)
            var firstPiece = pieces[0];
            float whiteWidth, blackWidth;
            firstPiece.GetBorderThicknessWorldSpace(out whiteWidth, out blackWidth);
            float cornerRadius = firstPiece.GetCornerRadiusWorldSpace();

            _borderRenderer.SetBorderWidth(whiteWidth, blackWidth);
            _borderRenderer.SetCornerRadius(cornerRadius);
        }

        // 조각 목록 전달 및 테두리 업데이트
        _borderRenderer.SetPieces(pieces);
    }

    /// <summary>
    /// 그룹 테두리 위치만 업데이트합니다 (애니메이션 중 호출용).
    /// UpdateGroupBorder()와 달리 조각 목록을 다시 설정하지 않고 위치만 갱신합니다.
    /// </summary>
    public void UpdateGroupBorderPosition()
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePosition();
        }
    }

    /// <summary>
    /// 펌핑 애니메이션용 - 그룹 테두리를 중심 기준으로 스케일 적용
    /// </summary>
    public void UpdateGroupBorderWithScale(Vector3 groupCenter, float scale)
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePositionWithScale(groupCenter, scale);
        }
    }

    /// <summary>
    /// 펌핑 애니메이션 완료 후 테두리 스케일 데이터 초기화
    /// </summary>
    public void ResetGroupBorderScaleData()
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.ResetScaleData();
        }
    }

    /// <summary>
    /// 그룹 테두리를 제거합니다.
    /// </summary>
    public void DestroyGroupBorder()
    {
        if (_borderContainer != null)
        {
            // LineRenderer를 먼저 비활성화하여 즉시 화면에서 숨김
            // (Object.Destroy는 프레임 끝에서 실행되므로)
            if (_borderRenderer != null)
            {
                _borderRenderer.SetVisible(false);
            }
            Object.Destroy(_borderContainer);
            _borderContainer = null;
            _borderRenderer = null;
        }
    }

    /// <summary>
    /// 그룹 테두리 컨테이너의 Transform을 반환합니다.
    /// 클리어 시퀀스에서 보드와 함께 이동시키기 위해 사용됩니다.
    /// </summary>
    public Transform GetBorderContainerTransform()
    {
        if (_borderContainer != null)
        {
            return _borderContainer.transform;
        }
        return null;
    }

    /// <summary>
    /// 그룹 테두리의 LineRenderer 점들을 지정된 오프셋만큼 이동합니다.
    /// </summary>
    public void MoveBorderPoints(Vector3 offset)
    {
        if (_borderRenderer != null)
        {
            Debug.Log($"[PieceGroup] MoveBorderPoints 실행: offset={offset}, borderRenderer={_borderRenderer.gameObject.name}, active={_borderRenderer.gameObject.activeInHierarchy}");
            _borderRenderer.MoveAllPoints(offset);
        }
        else
        {
            Debug.LogWarning($"[PieceGroup] MoveBorderPoints: _borderRenderer가 null입니다. (조각 수: {pieces.Count})");
        }
    }

    /// <summary>
    /// 그룹 테두리를 조각들의 중심에 맞춰 보정합니다.
    /// </summary>
    public void AlignBorderToPieces()
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.AlignBorderToPieces();
        }
    }

    /// <summary>
    /// 그룹 테두리를 지정된 중심 위치에 맞춰 보정합니다.
    /// </summary>
    public void AlignBorderToCenter(Vector3 targetCenter)
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.AlignBorderToCenter(targetCenter);
        }
    }

    #endregion
}
