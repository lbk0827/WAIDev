using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using WaiJigsaw.Data;
using WaiJigsaw.Core;

/// <summary>
/// 로비의 5x5 레벨 그리드를 생성하고 관리합니다.
/// - MonoObject 상속으로 표준화된 생명주기 관리
/// - Observer 자동 해제
/// </summary>
public class LobbyGridManager : MonoObject
{
    [Header("References")]
    [SerializeField] private LevelGroupManager _levelGroupManager;
    [SerializeField] private Transform _gridContainer;      // GridLayoutGroup이 붙은 부모
    [SerializeField] private GameObject _cardSlotPrefab;    // LobbyCardSlot 프리팹

    [Header("Card Back Settings")]
    [SerializeField] private Sprite _cardBackSprite;        // 카드 뒷면 스프라이트

    [Header("Card Visual Settings")]
    [SerializeField] private float _whiteBorderWidth = 3f;   // 하얀 테두리 두께 (픽셀)
    [SerializeField] private float _blackBorderWidth = 2f;   // 검정 테두리 두께 (픽셀)
    [SerializeField] private float _cornerRadius = 0.08f;    // 둥근 모서리 반경 (0~0.5)

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    private List<LobbyCardSlot> _cardSlots = new List<LobbyCardSlot>();
    private LevelGroupTableRecord _currentGroup;
    private int _currentLevel;
    private Material _roundedUIMaterial;

    private const int GRID_SIZE = 5;
    private const int CARDS_PER_GROUP = 25;

    // 카드 플립 연출 상태
    private bool _isPlayingClearAnimation = false;

    // 방금 클리어한 레벨 (챕터 클리어 체크용)
    private int _justClearedLevelForChapterCheck = -1;

    /// <summary>
    /// 카드 플립 연출이 재생 중인지 여부
    /// </summary>
    public bool IsPlayingClearAnimation => _isPlayingClearAnimation;

    /// <summary>
    /// 클리어 애니메이션 완료 이벤트 (UI 차단 해제용)
    /// </summary>
    public event Action OnClearAnimationComplete;

    /// <summary>
    /// 챕터 클리어 이벤트 (챕터의 마지막 레벨 클리어 시)
    /// 매개변수: (클리어한 그룹 정보, 완성된 이미지 스프라이트)
    /// </summary>
    public event Action<LevelGroupTableRecord, Sprite> OnChapterCleared;

    /// <summary>
    /// 현재 그룹 정보 (외부 접근용)
    /// </summary>
    public LevelGroupTableRecord CurrentGroup => _currentGroup;

    /// <summary>
    /// 둥근 모서리 Material을 초기화합니다.
    /// </summary>
    private void InitializeRoundedMaterial()
    {
        if (_roundedUIMaterial != null) return;

        Shader roundedShader = Shader.Find("UI/RoundedUI");
        if (roundedShader != null)
        {
            _roundedUIMaterial = new Material(roundedShader);
            _roundedUIMaterial.SetFloat("_CornerRadius", _cornerRadius);
        }
        else
        {
            Debug.LogWarning("[LobbyGridManager] UI/RoundedUI 셰이더를 찾을 수 없습니다.");
        }
    }

    #region MonoObject Lifecycle

    protected override void OnEnabled()
    {
        // LevelClearedEvent 구독 (MonoObject가 자동 해제 관리)
        RegisterLevelClearedObserver(OnLevelClearedEvent);
    }

    protected override void OnCleanup()
    {
        ClearGrid();
    }

    #endregion

    /// <summary>
    /// 현재 레벨에 맞는 그리드를 생성합니다.
    /// </summary>
    public void SetupGrid(int currentLevel)
    {
        _currentLevel = currentLevel;

        // 0. 둥근 모서리 Material 초기화
        InitializeRoundedMaterial();

        // 1. 방금 클리어한 레벨 확인 (Consume하지 않고 Peek만)
        int justClearedLevel = GameDataContainer.Instance.PeekJustClearedLevel();

        // 2. 표시할 그룹 결정
        LevelGroupTableRecord targetGroup;

        // 방금 클리어한 레벨이 있고, 해당 레벨이 챕터의 마지막 레벨인 경우
        // → 클리어 연출을 위해 이전 챕터(클리어한 챕터)를 먼저 표시
        if (justClearedLevel > 0)
        {
            LevelGroupTableRecord clearedGroup = _levelGroupManager.GetGroupForLevel(justClearedLevel);
            if (clearedGroup != null && justClearedLevel == clearedGroup.EndLevel)
            {
                // 챕터의 마지막 레벨을 클리어함 → 클리어 연출을 위해 해당 챕터 표시
                targetGroup = clearedGroup;

                if (_showDebugInfo)
                {
                    Debug.Log($"[LobbyGridManager] 챕터 클리어 연출을 위해 그룹 {clearedGroup.GroupID} 유지");
                }
            }
            else
            {
                // 일반 레벨 클리어 → 현재 레벨 기준 그룹 표시
                targetGroup = _levelGroupManager.GetGroupForLevel(currentLevel);
            }
        }
        else
        {
            // 클리어 연출 없음 → 현재 레벨 기준 그룹 표시
            targetGroup = _levelGroupManager.GetGroupForLevel(currentLevel);
        }

        _currentGroup = targetGroup;

        if (_showDebugInfo)
        {
            Debug.Log($"LobbyGridManager: 그룹 {_currentGroup.GroupID} 로드 " +
                      $"(레벨 {_currentGroup.StartLevel}~{_currentGroup.EndLevel})");
        }

        // 3. 기존 카드 슬롯 정리
        ClearGrid();

        // 4. 보상 이미지를 25조각으로 분할
        Sprite[] pieceSprites = _levelGroupManager.GetSlicedSprites(_currentGroup);

        if (pieceSprites == null || pieceSprites.Length != CARDS_PER_GROUP)
        {
            Debug.LogError("보상 이미지 분할에 실패했습니다!");
            return;
        }

        // 5. 25개의 카드 슬롯 생성
        for (int i = 0; i < CARDS_PER_GROUP; i++)
        {
            int levelNumber = _currentGroup.StartLevel + i;
            CreateCardSlot(levelNumber, pieceSprites[i]);
        }

        if (_showDebugInfo)
        {
            Debug.Log($"LobbyGridManager: {_cardSlots.Count}개의 카드 슬롯 생성 완료");
        }

        // 6. 방금 클리어한 레벨이 있으면 카드 플립 연출 실행
        TryPlayClearAnimation();
    }

    /// <summary>
    /// 방금 클리어한 레벨이 있으면 카드 플립 연출을 실행합니다.
    /// </summary>
    private void TryPlayClearAnimation()
    {
        int justClearedLevel = GameDataContainer.Instance.ConsumeJustClearedLevel();

        if (justClearedLevel < 0)
        {
            // 연출할 레벨 없음
            return;
        }

        // 현재 그룹에 해당하는 레벨인지 확인
        if (_currentGroup == null)
        {
            return;
        }

        int index = justClearedLevel - _currentGroup.StartLevel;

        if (index < 0 || index >= _cardSlots.Count)
        {
            // 다른 그룹의 레벨이면 무시
            if (_showDebugInfo)
            {
                Debug.Log($"[LobbyGridManager] 클리어 레벨 {justClearedLevel}은 현재 그룹에 없음");
            }
            return;
        }

        LobbyCardSlot cardSlot = _cardSlots[index];
        if (cardSlot == null)
        {
            return;
        }

        if (_showDebugInfo)
        {
            Debug.Log($"[LobbyGridManager] 레벨 {justClearedLevel} 카드 플립 연출 시작");
        }

        // 연출 상태 설정
        _isPlayingClearAnimation = true;

        // 챕터 클리어 체크를 위해 클리어한 레벨 저장
        _justClearedLevelForChapterCheck = justClearedLevel;

        // 카드 플립 애니메이션 실행
        cardSlot.PlayClearFlipAnimation(() =>
        {
            // 연출 완료
            _isPlayingClearAnimation = false;
            OnClearAnimationComplete?.Invoke();

            if (_showDebugInfo)
            {
                Debug.Log($"[LobbyGridManager] 레벨 {justClearedLevel} 카드 플립 연출 완료");
            }

            // 챕터 클리어 체크 (마지막 레벨 클리어 시)
            CheckAndTriggerChapterClear();
        });
    }

    /// <summary>
    /// 챕터 클리어 여부를 체크하고 이벤트를 발생시킵니다.
    /// </summary>
    private void CheckAndTriggerChapterClear()
    {
        if (_currentGroup == null || _justClearedLevelForChapterCheck < 0)
        {
            return;
        }

        // 마지막 레벨을 클리어한 경우에만 챕터 클리어
        if (_justClearedLevelForChapterCheck != _currentGroup.EndLevel)
        {
            _justClearedLevelForChapterCheck = -1;
            return;
        }

        // 모든 카드가 앞면 상태인지 확인 (25개 모두 클리어)
        bool allCardsCleared = true;
        for (int i = 0; i < _cardSlots.Count; i++)
        {
            int levelNumber = _currentGroup.StartLevel + i;
            if (!GameManager.Instance.IsLevelCleared(levelNumber))
            {
                allCardsCleared = false;
                break;
            }
        }

        if (!allCardsCleared)
        {
            _justClearedLevelForChapterCheck = -1;
            return;
        }

        if (_showDebugInfo)
        {
            Debug.Log($"[LobbyGridManager] 챕터 {_currentGroup.GroupID} 클리어! (레벨 {_currentGroup.StartLevel}~{_currentGroup.EndLevel})");
        }

        // 완성 이미지 로드
        Sprite completedSprite = _levelGroupManager.LoadGroupImage(_currentGroup);

        // 챕터 클리어 이벤트 발생
        OnChapterCleared?.Invoke(_currentGroup, completedSprite);

        // 체크 완료
        _justClearedLevelForChapterCheck = -1;
    }

    /// <summary>
    /// 개별 카드 슬롯을 생성합니다.
    /// </summary>
    private void CreateCardSlot(int levelNumber, Sprite pieceSprite)
    {
        // 프리팹이 없으면 기본 카드 생성
        GameObject slotObj;
        if (_cardSlotPrefab != null)
        {
            slotObj = Instantiate(_cardSlotPrefab, _gridContainer);
        }
        else
        {
            slotObj = CreateDefaultCardSlot();
        }

        slotObj.name = $"CardSlot_Lv{levelNumber}";

        // LobbyCardSlot 컴포넌트 가져오기 또는 추가
        LobbyCardSlot cardSlot = slotObj.GetComponent<LobbyCardSlot>();
        if (cardSlot == null)
        {
            cardSlot = slotObj.AddComponent<LobbyCardSlot>();
        }

        // 클리어 상태 확인
        bool isCleared = GameManager.Instance.IsLevelCleared(levelNumber);
        bool isCurrent = (levelNumber == _currentLevel);

        // 카드 초기화
        cardSlot.Initialize(levelNumber, pieceSprite, isCleared, isCurrent);

        _cardSlots.Add(cardSlot);
    }

    /// <summary>
    /// 프리팹이 없을 때 기본 카드 슬롯을 생성합니다.
    /// </summary>
    private GameObject CreateDefaultCardSlot()
    {
        float totalBorderWidth = _whiteBorderWidth + _blackBorderWidth;

        // 루트 오브젝트 (Button)
        GameObject slotObj = new GameObject("CardSlot");
        slotObj.transform.SetParent(_gridContainer, false);

        // RectTransform 설정
        RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        // Button 컴포넌트
        Button button = slotObj.AddComponent<Button>();

        // === 1. 검정 테두리 (가장 바깥) ===
        GameObject blackBorderObj = new GameObject("BlackBorder");
        blackBorderObj.transform.SetParent(slotObj.transform, false);
        RectTransform blackBorderRect = blackBorderObj.AddComponent<RectTransform>();
        blackBorderRect.anchorMin = Vector2.zero;
        blackBorderRect.anchorMax = Vector2.one;
        blackBorderRect.sizeDelta = Vector2.zero;
        blackBorderRect.offsetMin = Vector2.zero;
        blackBorderRect.offsetMax = Vector2.zero;

        Image blackBorderImage = blackBorderObj.AddComponent<Image>();
        blackBorderImage.color = Color.black;
        ApplyRoundedMaterial(blackBorderImage);

        // === 2. 하얀 테두리 (검정 안쪽) ===
        GameObject whiteBorderObj = new GameObject("WhiteBorder");
        whiteBorderObj.transform.SetParent(slotObj.transform, false);
        RectTransform whiteBorderRect = whiteBorderObj.AddComponent<RectTransform>();
        whiteBorderRect.anchorMin = Vector2.zero;
        whiteBorderRect.anchorMax = Vector2.one;
        whiteBorderRect.sizeDelta = Vector2.zero;
        whiteBorderRect.offsetMin = new Vector2(_blackBorderWidth, _blackBorderWidth);
        whiteBorderRect.offsetMax = new Vector2(-_blackBorderWidth, -_blackBorderWidth);

        Image whiteBorderImage = whiteBorderObj.AddComponent<Image>();
        whiteBorderImage.color = Color.white;
        ApplyRoundedMaterial(whiteBorderImage);

        // === 3. 뒷면 이미지 (배경) - 테두리 안쪽 ===
        GameObject backObj = new GameObject("BackImage");
        backObj.transform.SetParent(slotObj.transform, false);
        RectTransform backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.sizeDelta = Vector2.zero;
        backRect.offsetMin = new Vector2(totalBorderWidth, totalBorderWidth);
        backRect.offsetMax = new Vector2(-totalBorderWidth, -totalBorderWidth);

        Image backImage = backObj.AddComponent<Image>();
        if (_cardBackSprite != null)
        {
            backImage.sprite = _cardBackSprite;
        }
        else
        {
            backImage.color = new Color(0.3f, 0.3f, 0.4f, 1f); // 기본 어두운 색
        }
        ApplyRoundedMaterial(backImage);

        // === 4. 앞면 이미지 (조각) - 테두리 안쪽 ===
        GameObject frontObj = new GameObject("FrontImage");
        frontObj.transform.SetParent(slotObj.transform, false);
        RectTransform frontRect = frontObj.AddComponent<RectTransform>();
        frontRect.anchorMin = Vector2.zero;
        frontRect.anchorMax = Vector2.one;
        frontRect.sizeDelta = Vector2.zero;
        frontRect.offsetMin = new Vector2(totalBorderWidth, totalBorderWidth);
        frontRect.offsetMax = new Vector2(-totalBorderWidth, -totalBorderWidth);

        Image frontImage = frontObj.AddComponent<Image>();
        frontImage.preserveAspect = false; // 셀 크기에 맞게 채움
        // 앞면 이미지는 기본 머티리얼 사용 (둥근 모서리 적용 안 함)
        frontObj.SetActive(false); // 기본은 숨김

        // === 5. 레벨 번호 텍스트 ===
        GameObject textObj = new GameObject("LevelText");
        textObj.transform.SetParent(slotObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(totalBorderWidth, totalBorderWidth);
        textRect.offsetMax = new Vector2(-totalBorderWidth, -totalBorderWidth);

        TMPro.TMP_Text levelText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        levelText.alignment = TMPro.TextAlignmentOptions.Center;
        levelText.fontSize = 36;
        levelText.color = Color.white;
        levelText.fontStyle = TMPro.FontStyles.Bold;

        // LobbyCardSlot에 참조 연결
        LobbyCardSlot cardSlot = slotObj.AddComponent<LobbyCardSlot>();
        cardSlot.SetReferences(frontImage, backImage, levelText, button);

        return slotObj;
    }

    /// <summary>
    /// 이미지에 둥근 모서리 Material을 적용합니다.
    /// </summary>
    private void ApplyRoundedMaterial(Image image)
    {
        if (_roundedUIMaterial != null && image != null)
        {
            image.material = _roundedUIMaterial;
        }
    }

    /// <summary>
    /// 기존 그리드를 정리합니다.
    /// </summary>
    public void ClearGrid()
    {
        foreach (var slot in _cardSlots)
        {
            if (slot != null)
            {
                Destroy(slot.gameObject);
            }
        }
        _cardSlots.Clear();
    }

    /// <summary>
    /// LevelClearedEvent 핸들러 (Observer 패턴)
    /// </summary>
    private void OnLevelClearedEvent(LevelClearedEvent evt)
    {
        // 씬 전환 등으로 오브젝트가 파괴된 경우 무시
        if (this == null) return;

        // 현재 표시 중인 그룹에 해당하는 레벨인지 확인
        if (_currentGroup == null) return;

        int levelNumber = evt.ClearedLevel;
        int index = levelNumber - _currentGroup.StartLevel;

        if (index >= 0 && index < _cardSlots.Count)
        {
            // 카드 슬롯이 파괴되지 않았는지 확인
            if (_cardSlots[index] != null)
            {
                _cardSlots[index].SetCleared();
            }
        }

        // 다음 레벨을 현재 레벨로 표시
        int nextIndex = index + 1;
        if (nextIndex < _cardSlots.Count)
        {
            // 카드 슬롯이 파괴되지 않았는지 확인
            if (_cardSlots[nextIndex] != null)
            {
                _cardSlots[nextIndex].SetAsCurrent();
            }
        }

        if (_showDebugInfo)
        {
            Debug.Log($"[LobbyGridManager] 레벨 클리어 이벤트 수신: Level {levelNumber}");
        }
    }

    /// <summary>
    /// 그리드 상태를 갱신합니다.
    /// </summary>
    public void RefreshGrid()
    {
        if (_currentGroup == null) return;
        SetupGrid(GameManager.Instance.CurrentLevel);
    }

    /// <summary>
    /// 모든 카드의 테두리를 페이드 아웃합니다 (챕터 클리어 연출용).
    /// </summary>
    /// <param name="duration">페이드 시간</param>
    public void FadeOutAllCardBorders(float duration)
    {
        foreach (var cardSlot in _cardSlots)
        {
            if (cardSlot == null) continue;

            // BlackBorder 찾기
            Transform blackBorder = cardSlot.transform.Find("BlackBorder");
            if (blackBorder != null)
            {
                Image blackImage = blackBorder.GetComponent<Image>();
                if (blackImage != null)
                {
                    blackImage.DOFade(0f, duration);
                }
            }

            // WhiteBorder 찾기
            Transform whiteBorder = cardSlot.transform.Find("WhiteBorder");
            if (whiteBorder != null)
            {
                Image whiteImage = whiteBorder.GetComponent<Image>();
                if (whiteImage != null)
                {
                    whiteImage.DOFade(0f, duration);
                }
            }
        }
    }

    /// <summary>
    /// 그리드 컨테이너의 RectTransform을 반환합니다.
    /// </summary>
    public RectTransform GetGridContainerRect()
    {
        return _gridContainer as RectTransform;
    }

    #region Card Dealing Animation

    [Header("Card Dealing Animation")]
    [SerializeField] private float _cardDealDelay = 0.05f;      // 카드 간 딜레이
    [SerializeField] private float _cardDealDuration = 0.3f;    // 개별 카드 애니메이션 시간
    [SerializeField] private Vector2 _cardDealStartOffset = new Vector2(300f, 0f);  // 시작 오프셋 (오른쪽에서)

    // 딜링 애니메이션 완료 이벤트
    public event Action OnDealingAnimationComplete;

    /// <summary>
    /// 다음 챕터 그리드를 딜링 애니메이션과 함께 생성합니다.
    /// 챕터 클리어 시퀀스에서 사용됩니다.
    /// </summary>
    /// <param name="currentLevel">현재 레벨 (다음 챕터의 첫 레벨)</param>
    public void SetupGridWithDealingAnimation(int currentLevel)
    {
        Debug.Log($"[LobbyGridManager] SetupGridWithDealingAnimation 호출 - currentLevel: {currentLevel}");

        if (_levelGroupManager == null)
        {
            Debug.LogError("[LobbyGridManager] _levelGroupManager가 null입니다!");
            return;
        }

        _currentLevel = currentLevel;

        // 0. 둥근 모서리 Material 초기화
        InitializeRoundedMaterial();

        // 1. 현재 레벨 기준 그룹 표시 (PeekJustClearedLevel 무시)
        LevelGroupTableRecord targetGroup = _levelGroupManager.GetGroupForLevel(currentLevel);

        if (targetGroup == null)
        {
            Debug.LogError($"[LobbyGridManager] GetGroupForLevel({currentLevel})이 null을 반환했습니다!");
            return;
        }

        _currentGroup = targetGroup;

        if (_showDebugInfo)
        {
            Debug.Log($"[LobbyGridManager] 딜링 애니메이션으로 그룹 {_currentGroup.GroupID} 로드 " +
                      $"(레벨 {_currentGroup.StartLevel}~{_currentGroup.EndLevel})");
        }

        // 2. 기존 카드 슬롯 정리
        ClearGrid();

        // 3. 보상 이미지를 25조각으로 분할
        Sprite[] pieceSprites = _levelGroupManager.GetSlicedSprites(_currentGroup);

        if (pieceSprites == null || pieceSprites.Length != CARDS_PER_GROUP)
        {
            Debug.LogError("보상 이미지 분할에 실패했습니다!");
            return;
        }

        // 4. 25개의 카드 슬롯 생성 (비활성화 상태로)
        for (int i = 0; i < CARDS_PER_GROUP; i++)
        {
            int levelNumber = _currentGroup.StartLevel + i;
            CreateCardSlot(levelNumber, pieceSprites[i]);
        }

        // 5. 딜링 애니메이션 시작
        StartCoroutine(PlayDealingAnimation());
    }

    /// <summary>
    /// 카드 딜링 애니메이션 코루틴
    /// </summary>
    private IEnumerator PlayDealingAnimation()
    {
        _isPlayingClearAnimation = true;

        if (_showDebugInfo)
        {
            Debug.Log("[LobbyGridManager] 카드 딜링 애니메이션 시작");
        }

        // 모든 카드의 원래 위치 저장 및 시작 위치로 이동
        List<Vector3> originalPositions = new List<Vector3>();
        foreach (var cardSlot in _cardSlots)
        {
            if (cardSlot == null) continue;

            RectTransform rect = cardSlot.GetComponent<RectTransform>();
            originalPositions.Add(rect.anchoredPosition);

            // 시작 위치로 이동 (오른쪽 바깥)
            rect.anchoredPosition += _cardDealStartOffset;

            // 처음에는 투명하게
            CanvasGroup cg = cardSlot.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = cardSlot.gameObject.AddComponent<CanvasGroup>();
            }
            cg.alpha = 0f;
        }

        // 카드 하나씩 날아오기
        for (int i = 0; i < _cardSlots.Count; i++)
        {
            if (_cardSlots[i] == null) continue;

            RectTransform rect = _cardSlots[i].GetComponent<RectTransform>();
            CanvasGroup cg = _cardSlots[i].GetComponent<CanvasGroup>();
            Vector3 targetPos = originalPositions[i];

            // 이동 + 페이드 인 애니메이션
            rect.DOAnchorPos(targetPos, _cardDealDuration).SetEase(Ease.OutQuad);
            cg.DOFade(1f, _cardDealDuration * 0.5f);

            // 다음 카드까지 딜레이
            yield return new WaitForSeconds(_cardDealDelay);
        }

        // 마지막 카드 애니메이션 완료 대기
        yield return new WaitForSeconds(_cardDealDuration);

        _isPlayingClearAnimation = false;

        if (_showDebugInfo)
        {
            Debug.Log("[LobbyGridManager] 카드 딜링 애니메이션 완료");
        }

        OnDealingAnimationComplete?.Invoke();
    }

    #endregion
}
