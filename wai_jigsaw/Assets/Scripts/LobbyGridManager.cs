using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 로비의 5x5 레벨 그리드를 생성하고 관리합니다.
/// </summary>
public class LobbyGridManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LevelGroupManager _levelGroupManager;
    [SerializeField] private Transform _gridContainer;      // GridLayoutGroup이 붙은 부모
    [SerializeField] private GameObject _cardSlotPrefab;    // LobbyCardSlot 프리팹

    [Header("Card Back Settings")]
    [SerializeField] private Sprite _cardBackSprite;        // 카드 뒷면 스프라이트

    [Header("Debug")]
    [SerializeField] private bool _showDebugInfo = true;

    private List<LobbyCardSlot> _cardSlots = new List<LobbyCardSlot>();
    private LevelGroupItem _currentGroup;
    private int _currentLevel;

    private const int GRID_SIZE = 5;
    private const int CARDS_PER_GROUP = 25;

    /// <summary>
    /// 현재 레벨에 맞는 그리드를 생성합니다.
    /// </summary>
    public void SetupGrid(int currentLevel)
    {
        _currentLevel = currentLevel;

        // 1. 현재 레벨이 속한 그룹 가져오기
        _currentGroup = _levelGroupManager.GetGroupForLevel(currentLevel);

        if (_showDebugInfo)
        {
            Debug.Log($"LobbyGridManager: 그룹 {_currentGroup.groupId} 로드 " +
                      $"(레벨 {_currentGroup.startLevel}~{_currentGroup.endLevel})");
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
            int levelNumber = _currentGroup.startLevel + i;
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
        // 루트 오브젝트 (Button + CanvasGroup)
        GameObject slotObj = new GameObject("CardSlot");
        slotObj.transform.SetParent(_gridContainer, false);

        // RectTransform 설정
        RectTransform rectTransform = slotObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(100, 100);

        // Button 컴포넌트
        Button button = slotObj.AddComponent<Button>();

        // 뒷면 이미지 (배경)
        GameObject backObj = new GameObject("BackImage");
        backObj.transform.SetParent(slotObj.transform, false);
        RectTransform backRect = backObj.AddComponent<RectTransform>();
        backRect.anchorMin = Vector2.zero;
        backRect.anchorMax = Vector2.one;
        backRect.sizeDelta = Vector2.zero;
        backRect.offsetMin = Vector2.zero;
        backRect.offsetMax = Vector2.zero;

        Image backImage = backObj.AddComponent<Image>();
        if (_cardBackSprite != null)
        {
            backImage.sprite = _cardBackSprite;
        }
        else
        {
            backImage.color = new Color(0.3f, 0.3f, 0.4f, 1f); // 기본 어두운 색
        }

        // 앞면 이미지 (조각)
        GameObject frontObj = new GameObject("FrontImage");
        frontObj.transform.SetParent(slotObj.transform, false);
        RectTransform frontRect = frontObj.AddComponent<RectTransform>();
        frontRect.anchorMin = Vector2.zero;
        frontRect.anchorMax = Vector2.one;
        frontRect.sizeDelta = Vector2.zero;
        frontRect.offsetMin = Vector2.zero;
        frontRect.offsetMax = Vector2.zero;

        Image frontImage = frontObj.AddComponent<Image>();
        frontImage.preserveAspect = true;
        frontObj.SetActive(false); // 기본은 숨김

        // 레벨 번호 텍스트
        GameObject textObj = new GameObject("LevelText");
        textObj.transform.SetParent(slotObj.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TMPro.TMP_Text levelText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        levelText.alignment = TMPro.TextAlignmentOptions.Center;
        levelText.fontSize = 36;
        levelText.color = Color.white;
        levelText.fontStyle = TMPro.FontStyles.Bold;

        // LobbyCardSlot에 참조 연결 (Reflection 대신 직접 설정)
        LobbyCardSlot cardSlot = slotObj.AddComponent<LobbyCardSlot>();

        // SerializeField를 런타임에 설정하기 위해 public 메서드 사용
        cardSlot.SetReferences(frontImage, backImage, levelText, button);

        return slotObj;
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
    /// 특정 레벨이 클리어되었을 때 호출합니다.
    /// </summary>
    public void OnLevelCleared(int levelNumber)
    {
        // 현재 표시 중인 그룹에 해당하는 레벨인지 확인
        if (_currentGroup == null) return;

        int index = levelNumber - _currentGroup.startLevel;
        if (index >= 0 && index < _cardSlots.Count)
        {
            _cardSlots[index].SetCleared();
        }

        // 다음 레벨을 현재 레벨로 표시
        int nextIndex = index + 1;
        if (nextIndex < _cardSlots.Count)
        {
            _cardSlots[nextIndex].SetAsCurrent();
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
