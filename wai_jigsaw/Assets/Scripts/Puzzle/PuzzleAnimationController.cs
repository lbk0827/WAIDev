using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 퍼즐 애니메이션을 관리합니다.
/// - 인트로 애니메이션 (카드 날아가기, 뒤집기)
/// - 스왑 애니메이션 (조각/그룹 이동)
/// - 펌핑 애니메이션 (병합 시 효과)
///
/// PuzzleBoardSetup에서 분리된 클래스입니다.
/// </summary>
public class PuzzleAnimationController : MonoBehaviour
{
    // ====== 인트로 애니메이션 설정 ======
    [Header("Card Intro Animation")]
    [Tooltip("카드가 날아가는 속도 (초)")]
    [Range(0.1f, 1.0f)] public float cardFlyDuration = 0.3f;
    [Tooltip("카드 사이의 딜레이 (초)")]
    [Range(0.01f, 0.2f)] public float cardFlyDelay = 0.05f;
    [Tooltip("카드 뒤집기 딜레이 (초)")]
    [Range(0.01f, 0.1f)] public float cardFlipDelay = 0.03f;
    [Tooltip("카드 뒤집기 시간 (초)")]
    [Range(0.1f, 0.5f)] public float cardFlipDuration = 0.25f;

    // ====== 스왑 애니메이션 설정 ======
    [Header("Swap Animation")]
    [Tooltip("스왑 시 카드 이동 시간 (초)")]
    [Range(0.1f, 0.8f)] public float swapAnimationDuration = 0.3f;

    // ====== 펌핑 애니메이션 설정 ======
    [Header("Merge Pumping Animation")]
    [Tooltip("합쳐질 때 펌핑 최대 스케일 (1.0 기준)")]
    [Range(1.0f, 1.3f)] public float pumpingScale = 1.15f;
    [Tooltip("펌핑 애니메이션 시간 (초)")]
    [Range(0.1f, 0.5f)] public float pumpingDuration = 0.2f;

    // ====== 상태 ======
    private bool _isPlayingIntro = false;
    public bool IsPlayingIntro => _isPlayingIntro;

    #region Intro Animation

    /// <summary>
    /// 레벨 시작 인트로 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="pieces">퍼즐 조각 리스트</param>
    /// <param name="slotPositions">슬롯 위치 리스트</param>
    /// <param name="onShuffleData">셔플 데이터 콜백</param>
    /// <param name="onIntroComplete">인트로 완료 콜백</param>
    public void PlayIntroAnimation(
        List<DragController> pieces,
        List<Vector3> slotPositions,
        System.Action onShuffleData,
        System.Action onIntroComplete)
    {
        StartCoroutine(IntroAnimationCoroutine(pieces, slotPositions, onShuffleData, onIntroComplete));
    }

    private IEnumerator IntroAnimationCoroutine(
        List<DragController> pieces,
        List<Vector3> slotPositions,
        System.Action onShuffleData,
        System.Action onIntroComplete)
    {
        _isPlayingIntro = true;

        int totalCards = pieces.Count;

        // 0단계: 먼저 셔플 순서 결정 (실제 위치 이동 없이 데이터만 셔플)
        onShuffleData?.Invoke();

        // 1단계: 카드가 좌상단부터 순서대로 날아감 (이미 셔플된 순서)
        int flyingCount = 0;

        for (int i = 0; i < totalCards; i++)
        {
            DragController piece = pieces[i];
            Vector3 targetPos = slotPositions[i];

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
            DragController piece = pieces[i];

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

        _isPlayingIntro = false;

        // 인트로 완료 콜백
        onIntroComplete?.Invoke();

        Debug.Log("인트로 애니메이션 완료. 게임 시작!");
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

    #endregion

    #region Swap Animation

    /// <summary>
    /// 조각을 목표 위치로 부드럽게 이동시킵니다 (스왑용).
    /// </summary>
    public void MovePiece(DragController piece, Vector3 targetPos, System.Action onComplete = null)
    {
        StartCoroutine(SmoothMovePieceCoroutine(piece, targetPos, swapAnimationDuration, onComplete));
    }

    private IEnumerator SmoothMovePieceCoroutine(DragController piece, Vector3 targetPos, float duration, System.Action onComplete)
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
        onComplete?.Invoke();
    }

    /// <summary>
    /// 그룹 전체를 부드럽게 이동시킵니다.
    /// </summary>
    public void MoveGroup(PieceGroup group, DragController anchorPiece, Vector3 anchorTargetPos, System.Action onComplete = null)
    {
        StartCoroutine(SmoothMoveGroupCoroutine(group, anchorPiece, anchorTargetPos, swapAnimationDuration, onComplete));
    }

    private IEnumerator SmoothMoveGroupCoroutine(PieceGroup group, DragController anchorPiece, Vector3 anchorTargetPos, float duration, System.Action onComplete)
    {
        // [중요] 코루틴 시작 시점의 조각 리스트 스냅샷 저장
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

            // 애니메이션 중에도 테두리 위치 업데이트
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

        onComplete?.Invoke();
    }

    #endregion

    #region Pumping Animation

    /// <summary>
    /// 그룹 내 모든 조각에 펌핑 애니메이션을 재생합니다.
    /// </summary>
    public void PlayGroupPumpingAnimation(PieceGroup group)
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

        // 그룹 테두리 참조
        PieceGroup pieceGroup = pieces.Count > 0 && pieces[0] != null ? pieces[0].group : null;

        // 확대
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

            pieceGroup?.UpdateGroupBorderWithScale(groupCenter, currentScale);

            yield return null;
        }

        // 축소
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

            pieceGroup?.UpdateGroupBorderWithScale(groupCenter, currentScale);

            yield return null;
        }

        // 원래 상태 복원
        foreach (var piece in pieces)
        {
            if (piece != null && originalPositions.ContainsKey(piece))
            {
                piece.transform.localScale = originalScales[piece];
                piece.transform.position = originalPositions[piece];
            }
        }

        pieceGroup?.ResetGroupBorderScaleData();
    }

    #endregion
}
