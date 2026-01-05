using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
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

        // 1. 현재 레벨이 속한 그룹 가져오기
        _currentGroup = _levelGroupManager.GetGroupForLevel(currentLevel);

        if (_showDebugInfo)
        {
            Debug.Log($"LobbyGridManager: 그룹 {_currentGroup.GroupID} 로드 " +
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

        // 4. 25개의 카드 슬롯 생성
        for (int i = 0; i < CARDS_PER_GROUP; i++)
        {
            int levelNumber = _currentGroup.StartLevel + i;
            CreateCardSlot(levelNumber, pieceSprites[i]);
        }

        if (_showDebugInfo)
        {
            Debug.Log($"LobbyGridManager: {_cardSlots.Count}개의 카드 슬롯 생성 완료");
        }
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
        ApplyRoundedMaterial(frontImage);
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
}
