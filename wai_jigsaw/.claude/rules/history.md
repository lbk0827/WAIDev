# 작업 히스토리

## 2024-12-26

### 퍼즐 그룹화 시각 효과 개선
- `pieceSpacing` 변수 추가 (조각 간 간격)
- `MergeGroupWithSnap()` - 그룹 병합 시 spacing 제거
- `CheckConnectionsRecursive()` - 연쇄 병합 처리
- `MoveGroupWithRelativePositions()` - 그룹 이동 시 상대적 위치 유지

### TableExporter 연동
- Excel → JSON 변환 도구 설정
- `config/export_config.json` - 프로젝트 경로 설정
- `config/local_config.json` - 로컬 경로 설정

### 데이터 테이블 시스템 구축
- **LevelTable** (`Assets/Scripts/Data/Generated/LevelTable.cs`)
  - 레벨 정보 (levelID, ImageName, Rows, Cols)
  - JSON: `Assets/Resources/Tables/LevelTable.json`

- **LevelGroupTable** (`Assets/Scripts/Data/Generated/LevelGroupTable.cs`)
  - 그룹 정보 (GroupID, StartLevel, EndLevel, ImageName)
  - JSON: `Assets/Resources/Tables/LevelGroupTable.json`

### 수정된 스크립트
- `LevelManager.cs` - LevelTable 사용으로 변경
- `LevelGroupManager.cs` - LevelGroupTable 사용으로 변경
- `LobbyGridManager.cs` - LevelGroupTableRecord 타입 사용

### 삭제된 파일
- `Assets/Resources/LevelData.json` (기존)
- `Assets/Resources/LevelGroupData.json` (기존)

### 로비 UI 개선
- 카드 앞면 이미지 `preserveAspect = false`
- Cell Size: 150 x 269 (이미지 비율 맞춤)
- 카드 클릭 시 레벨 진입 비활성화 (Play 버튼으로만 진입)

### UI 텍스트 수정
- Play 버튼: "플레이" → "PLAY" (폰트 호환성)

---

## 2025-12-26

### 카드 인트로 연출 시스템 구현

#### CardTable 시스템 추가
- `Assets/Scripts/Data/Generated/CardTable.cs` - 카드 테이블 관리자
- `Assets/Resources/Tables/CardTable.json` - 카드 데이터 (CardID, CardName, CardBackSprite)
- 확장성을 고려한 테이블 기반 카드 관리

#### 카드 비주얼 시스템 (DragController.cs)
- `InitializeCardVisuals()` - 카드 프레임 및 뒷면 생성
- `FlipCard()` - 카드 뒤집기 애니메이션 (X축 스케일 기반)
- `ShowFrontImmediate()`, `ShowBackImmediate()` - 즉시 전환
- `SetDraggable()` - 인트로 중 드래그 방지
- 카드 프레임 (크림색 배경), 카드 뒷면 (파란색 기본)

#### 인트로 애니메이션 (PuzzleBoardSetup.cs)
- `PlayIntroAnimation()` - 레벨 시작 연출 코루틴
  1. 카드 뭉치(오른쪽 하단 마지막 슬롯)에서 시작
  2. 좌상단부터 순서대로 각 슬롯으로 날아감 (포물선 효과)
  3. 모든 카드 도착 후 웨이브 효과로 뒤집기
  4. 셔플 후 게임 시작
- Inspector 조절 가능 파라미터:
  - `cardFlyDuration` (0.3초) - 카드 비행 시간
  - `cardFlyDelay` (0.05초) - 카드 간 딜레이
  - `cardFlipDelay` (0.03초) - 뒤집기 딜레이
  - `cardFlipDuration` (0.25초) - 뒤집기 시간

#### 필요한 리소스
- `Assets/Resources/Sprites/Card_Back_Basic.png` - 카드 뒷면 이미지 필요
  - CardTable.json에서 경로 지정: "Sprites/Card_Back_Basic"

### 둥근 모서리 시스템 구현

#### DragController 수정
- `_cornerObjects[4]`, `_cornerRenderers[4]` - 4개 모서리 오브젝트
- `InitializeRoundedCorners()` - 모서리 스프라이트 초기화
- `CreateCornerSprite()` - 프로그래매틱 모서리 스프라이트 생성
- `UpdateCornersBasedOnGroup()` - 그룹 내 위치 기반 모서리 가시성 업데이트
- `SetCornerVisible()` - 개별 모서리 표시/숨김

#### 모서리 가시성 로직
- 개별 조각: 4개 모서리 모두 표시
- 그룹화 시: 인접 조각 방향의 모서리 숨김
  - 오른쪽 인접 → TopRight, BottomRight 숨김
  - 왼쪽 인접 → TopLeft, BottomLeft 숨김
  - 위 인접 → TopLeft, TopRight 숨김
  - 아래 인접 → BottomLeft, BottomRight 숨김
  - 대각선 인접 → 해당 모서리 숨김

#### PuzzleBoardSetup 수정
- `UpdateAllPieceCorners()` - 모든 조각의 모서리 업데이트
- `CheckInitialConnections()` 후 모서리 업데이트 호출
- `OnPieceDropped()` 후 모서리 업데이트 호출

---

## 2025-12-29

### WhiteFrame 테두리 경계선 문제 해결 시도

#### 문제 현상
- 병합된 카드 사이에 WhiteFrame으로 인한 경계선이 보임
- `_HideDirections`로 방향별 숨김 처리해도 경계선 잔상 발생

#### 방안 1: 방향별 프레임 두께 조절 (셰이더 수정)
- `RoundedFrame.shader` 수정
  - `_FrameThickness` (float) → `_FrameThicknesses` (Vector4)로 변경
  - `innerBoxSDF()` 함수 추가하여 방향별 두께 적용
- `DragController.cs` 수정
  - `SetFrameThicknessAt()` - 특정 방향 프레임 두께 설정
  - `RestoreFrameThicknessAt()` - 특정 방향 두께 복원
  - `RestoreAllFrameThicknesses()` - 모든 방향 두께 복원
- **결과**: 일부 개선되었으나 회색 경계선 여전히 보임 (안티앨리어싱 아티팩트)

#### 방안 3: 4개 개별 Edge + 4개 Corner 오브젝트
- 프레임을 8개의 개별 오브젝트로 분리
  - `_whiteFrameEdges[4]`, `_blackFrameEdges[4]` - 상/하/좌/우 Edge
  - `_whiteCorners[4]`, `_blackCorners[4]` - TL/TR/BL/BR Corner
- `RoundedCorner.shader` 신규 생성 - 1/4 원 렌더링
- `CreateFrameBorders()` → `CreateFrameEdges()` + `CreateFrameCorners()`
- `UpdateCornerVisibility()` - 모서리 가시성 업데이트
- **결과**: 모서리가 직각으로 표시되는 문제 발생

#### 최종 결과
- 모든 변경사항 롤백 (Discard)
- 추후 재작업 필요

---

### CardSlot 크기 조정

#### 변경 내용
- **파일**: `Assets/Scripts/PuzzleBoardSetup.cs`
- `slotSizeRatio`: `0.98f` → `1.0f`
- CardSlot 크기가 카드와 정확히 동일하게 표시됨

---

### 관련 셰이더 정보 (참고용)

#### RoundedFrame.shader 주요 속성
- `_FrameThickness` - 프레임 두께 (UV 비율)
- `_HideDirections` - 방향별 숨김 (Top, Bottom, Left, Right)
- `_CornerRadii` - 개별 모서리 반경 (TL, TR, BL, BR)

#### RoundedSprite.shader 주요 속성
- `_CornerRadius` - 기본 모서리 반경
- `_CornerRadii` - 개별 모서리 반경
- `_Padding` - 패딩 (Left, Right, Top, Bottom)
- `_UVRect` - UV 정규화 영역

---

## 2025-12-30 ~ 2026-01-02

### 씬 분리
- LobbyScene / GameScene 분리
- 각 씬별 독립적인 UI 및 게임 로직 관리

### 펌핑(Pumping) 연출 시스템

#### PuzzleBoardSetup.cs 수정
- `pumpingScale` (1.15f) - 합쳐질 때 최대 스케일
- `pumpingDuration` (0.2f) - 펌핑 애니메이션 시간
- 카드 그룹이 합쳐질 때 스케일 애니메이션 적용

#### 버그 수정
- 그룹 펌핑 연출 관련 버그 수정

### Settings 팝업 시스템

#### 추가된 기능
- 인게임 설정 버튼 추가
- SettingsPopup 프리팹 생성
- DimBackground 노출 처리
- 팝업 열린 상태에서 아래 UI 상호작용 차단

### CardSlots 개선
- 인게임 CardSlots에 검정 테두리 추가
- `slotBorderThickness` (0.015f) - 테두리 두께
- `slotBorderColor` - 테두리 색상 (진한 회색/검은색)

### 아이템 시스템 구축

#### ItemTable 추가
- **파일**: `Assets/Scripts/Data/Generated/ItemTable.cs`
- **JSON**: `Assets/Resources/Tables/ItemTable.json`
- 아이템 타입 상수: `ITEM_TYPE_COIN = 101`
- 필드: Item_Type, Item_Category, Item_GetType, Item_Price, Item_Icon

#### OutgameResourcePath 추가
- **파일**: `Assets/Scripts/Data/OutgameResourcePath.cs`
- **에셋**: `Assets/Resources/OutgameResourcePath.asset`
- Inspector에서 아이템별 아이콘 스프라이트 직접 할당
- ItemType으로 아이콘 조회 기능

### 코인 시스템

#### GameDataContainer.cs 확장
- `Coin` 프로퍼티 추가
- `AddCoin(int amount)` - 코인 획득
- `SpendCoin(int amount)` - 코인 소비 (잔액 체크)
- `HasEnoughCoin(int amount)` - 잔액 확인
- `CoinChangedEvent` - 코인 변경 이벤트 (Observer 패턴)
- PlayerPrefs 저장/로드 지원

#### LevelTable 확장
- 스테이지 클리어 시 획득 코인 필드 추가

#### CoinDisplay.cs 추가
- **파일**: `Assets/Scripts/UI/CoinDisplay.cs`
- 화면 상단 코인 잔액 표시 UI
- Observer 패턴으로 코인 변경 시 자동 갱신
- 카운팅 애니메이션 (Ease Out Quad)
- 1000 이상 시 K 단위 표시 (예: 1.2K)

### 레벨 콘텐츠 확장
- Level 15 ~ 25 이미지 추가
- LevelTable.json 업데이트 (25개 레벨)

---

## 2026-01-02

### 이미지-프레임 사이 투명 틈 이슈 발견 및 해결 시도

#### 문제 현상
- 퍼즐 이미지와 하얀 테두리(WhiteFrame) 사이에 투명한 틈이 보임
- RoundedSprite.shader의 `_Padding`이 이미지 가장자리를 잘라내지만, WhiteFrame이 그 영역을 덮지 못함

#### 시도한 해결 방법: 프레임 두께에 패딩 추가
- `ApplyShaderToFrames()`에서 WhiteFrame 두께 계산 시 padding 포함
```csharp
float paddingRatioX = Mathf.Max(_padding.x, _padding.y);
float paddingRatioY = Mathf.Max(_padding.z, _padding.w);
float avgPaddingRatio = (paddingRatioX + paddingRatioY) / 2f;
float whiteFrameThicknessUV = _whiteBorderThickness + _blackBorderThickness + avgPaddingRatio;
```
- **원리**: padding으로 투명해진 이미지 가장자리를 WhiteFrame이 덮도록 두께 확장

#### 병합 카드 테두리 연결 문제 시도 (실패 → 롤백)
- 병합된 카드 사이에서 테두리가 끊어지는 현상 해결 시도
- **프레임 확장 방식** 구현:
  - `_frameExtendDirections` (Vector4) - 각 방향 확장 상태
  - `_originalFrameWidth`, `_originalFrameHeight` - 원본 프레임 크기
  - `ApplyFrameExtension()` - 인접 카드 방향으로 프레임 스케일/위치 조정
  - `HideBorder()` → 숨기는 대신 확장하도록 변경
  - `RecalculateCornerRadii()` - 확장 상태에 따른 모서리 반경 재계산
- **결과**: 프레임이 과도하게 확장되어 인접 카드 이미지까지 덮어버림
- **모든 변경사항 롤백 (Discard)**

#### 현재 상태
- 투명 틈 이슈 미해결 상태로 복귀
- 추후 재작업 필요

#### 향후 해결 방향 제안
1. **프레임 두께 확장 방식** (가장 안전)
   - `ApplyShaderToFrames()`에서 WhiteFrame 두께에 padding 추가
   - 프레임 크기/위치 변경 없이 셰이더 속성만 조정
2. 병합 테두리 연결 문제는 별도로 접근 필요

---

## 2026-01-05

### 빌드 환경에서의 렌더링 이슈 수정

#### 1. GroupBorder 크기 문제 해결

**문제 현상**
- Unity Editor에서는 정상이지만, Windows 빌드에서 GroupBorder가 카드 영역을 벗어나 과도하게 크게 렌더링됨

**원인 분석**
- `overlapMargin` 값으로 콜라이더를 확장한 후 수축하는 과정에서 오차 발생
- Editor와 Build 환경의 동작 차이

**수정 내용** (`GroupBorderRenderer.cs`)
```csharp
// 기존: float overlapMargin = 0.1f; (10% 확장)
// 변경: float overlapMargin = 0f;   (확장 없음)
```
- 인접 조각들이 이미 같은 위치에 있으므로 확장 없이도 콜라이더가 병합됨

---

#### 2. CardSlot 크기 문제 해결

**문제 현상**
- Unity Editor에서는 정상이지만, Windows 빌드에서 CardSlot이 카드보다 현저히 작게 표시됨

**원인 분석**
- `SpriteRenderer.bounds.size`가 빌드 환경에서 `localScale`을 반영하지 않음
- Editor에서는 정상 동작하지만 빌드에서는 스케일이 적용되지 않은 원본 크기 반환

**수정 내용** (`PuzzleBoardSetup.cs`)
```csharp
// 기존: actualCardSize = sr.bounds.size (빌드에서 localScale 미반영)
// 변경: actualCardSize = sprite.bounds.size × localScale (명시적 계산)
Vector2 spriteSize = firstCardSR.sprite.bounds.size;
Vector2 actualCardSize = new Vector2(spriteSize.x * cardScale.x, spriteSize.y * cardScale.y);
```

**슬롯 크기 미세 조정**
```csharp
// 슬롯끼리 맞닿는 느낌을 위해 pieceSpacing의 30%만 적용
float slotSpacingFactor = 0.3f;
float visiblePieceWidth = actualCardSize.x * (1f - pieceSpacing * slotSpacingFactor);
float visiblePieceHeight = actualCardSize.y * (1f - pieceSpacing * slotSpacingFactor);
```

---

#### 3. 드래그 카드 레이어 순서 문제 해결

**문제 현상**
- 드래그 중인 카드가 합쳐진 그룹의 GroupBorder 아래에 배치되어 가려짐

**원인 분석**
- `GroupBorderRenderer`의 `_baseSortingOrder` 기본값이 100으로 설정됨
- 드래그 중인 카드의 sortingOrder도 100이라서 Z 위치에 따라 렌더링 순서 결정

**수정 내용** (`GroupBorderRenderer.cs`)
```csharp
// 기존: [SerializeField] private int _baseSortingOrder = 100;
// 변경: [SerializeField] private int _baseSortingOrder = 2;
```
- 드래그 시 `PieceGroup.SetSortingOrder(100)`으로 높여주므로, 기본값은 낮게 유지

---

#### 4. LineRenderer index out of bounds 오류 해결

**문제 현상**
- 펌핑 애니메이션 중 `LineRenderer.SetPosition index out of bounds!` 오류 발생

**원인 분석**
- `UpdatePositionWithScale()`에서 원본 배열의 길이와 현재 LineRenderer의 `positionCount`가 다를 때 발생
- 펌핑 애니메이션 중에 `DelayedUpdateBorder`가 호출되어 테두리가 재생성되면서 `positionCount` 변경됨

**수정 내용** (`GroupBorderRenderer.cs`)
```csharp
// positionCount와 원본 배열 길이가 다르면 스케일 적용 불가
if (_whiteLineRenderer.positionCount != _originalWhiteLinePositions.Length ||
    _blackLineRenderer.positionCount != _originalBlackLinePositions.Length)
{
    // 원본 데이터 무효화하고 리턴
    _hasOriginalPositions = false;
    return;
}
```

---

### 수정된 파일 요약

| 파일 | 수정 내용 |
|------|-----------|
| `GroupBorderRenderer.cs` | overlapMargin=0, baseSortingOrder=2, positionCount 체크 추가 |
| `PuzzleBoardSetup.cs` | sprite.bounds.size × localScale 계산, slotSpacingFactor 추가 |

---

## 2026-01-06

### GroupBorder 직접 계산 방식으로 전환

#### 문제 현상
- CompositeCollider2D 기반 방식에서 부동소수점 오차로 인해 테두리가 삼각형 모양으로 잘못 그려지는 버그 발생
- pieceWidth(1.43)와 조각 간 거리(1.44)의 차이로 콜라이더 병합 실패

#### 해결 방법
- CompositeCollider2D 의존성 제거
- 월드 위치 기반 직접 계산 방식으로 전환
- `FindOuterEdgesFromWorldPositions()`: 실제 transform.position 간 거리로 인접 판단

#### 핵심 알고리즘
```csharp
// 인접 판단: 두 조각의 월드 위치 차이가 pieceWidth/pieceHeight와 거의 같으면 인접
float dx = otherPos.x - cellCenter.x;
float dy = otherPos.y - cellCenter.y;

// 위쪽 인접 (dy ≈ pieceHeight, dx ≈ 0)
if (Mathf.Abs(dy - _pieceHeight) < toleranceY && Mathf.Abs(dx) < toleranceX)
    hasTop = true;
```

---

### GroupBorder 디버그 로그 (필요 시 복원용)

아래 코드는 GroupBorder 문제 디버깅에 사용했던 로그입니다. 문제 발생 시 복원하여 사용하세요.

#### GroupBorderRenderer.cs

```csharp
// SetPieces() 함수 내부
Debug.Log($"[GroupBorderRenderer] SetPieces 호출 - 조각 수: {pieces.Count}, 조각 목록: {string.Join(", ", pieces.ConvertAll(p => $"({p.originalGridX},{p.originalGridY})"))}");

// pieceWidth/Height가 0일 때
Debug.LogWarning($"[GroupBorderRenderer] pieceWidth/Height가 0 - SpriteRenderer에서 계산: ({_pieceWidth}, {_pieceHeight})");

// CalculateAndApplyOutline() 함수 내부
Debug.LogWarning("[GroupBorderRenderer] 외곽 변을 찾을 수 없습니다.");
Debug.LogWarning($"[GroupBorderRenderer] 외곽선 점이 부족합니다: {outlinePoints.Count}");
Debug.Log($"[GroupBorderRenderer] 외곽선 점 수: {outlinePoints.Count}");
Debug.Log($"[GroupBorderRenderer] LineRenderer 설정 완료 - {smoothedPoints.Count}개 점");

// ConnectEdgesToPath() 함수 내부 - 연결 실패 시
Debug.LogWarning($"[GroupBorderRenderer] 연결 실패 - 사용된 변: {usedEdges.Count}/{edges.Count}, 현재 끝점: {currentEnd}, tolerance: {tolerance}");
for (int i = 0; i < edges.Count; i++)
{
    if (!usedEdges.Contains(i))
    {
        Edge e = edges[i];
        Debug.LogWarning($"  미연결 변[{i}]: Start={e.Start}, End={e.End}, 거리(Start)={Vector2.Distance(currentEnd, e.Start):F4}, 거리(End)={Vector2.Distance(currentEnd, e.End):F4}");
    }
}
Debug.LogWarning($"[GroupBorderRenderer] 일부 변이 연결되지 않음: {usedEdges.Count}/{edges.Count}개 사용됨");
```

#### DragController.cs (PieceGroup 클래스)

```csharp
// UpdateGroupBorder() 함수 시작
Debug.Log($"[PieceGroup] UpdateGroupBorder 호출 - pieces.Count={pieces.Count}, 조각 목록: {string.Join(", ", pieces.ConvertAll(p => $"({p.originalGridX},{p.originalGridY})"))}");

// CreateOrUpdateGroupBorder() 함수 내부
Debug.Log($"[PieceGroup] CreateOrUpdateGroupBorder - pieceWidth={firstPiece.pieceWidth:F4}, pieceHeight={firstPiece.pieceHeight:F4}, whiteWidth={whiteWidth:F4}, blackWidth={blackWidth:F4}, cornerRadius={cornerRadius:F4}");

// GetBorderThicknessWorldSpace() 함수 끝
Debug.Log($"[DragController] GetBorderThicknessWorldSpace - baseSize={baseSize:F3}, totalRatio={totalFrameThickness:F4}, blackRatio={_blackBorderThickness:F4}, whiteWidth={whiteWidth:F4}, blackWidth={blackWidth:F4}");
```

#### PuzzleBoardSetup.cs

```csharp
// CheckConnectionsRecursive() 함수 끝
Debug.Log($"[PuzzleBoardSetup] CheckConnectionsRecursive 완료 - 최종 그룹 크기: {group.pieces.Count}");
```

---

## 2026-01-07

### 병합된 카드 사이 투명 틈 이슈 수정 시도 (롤백됨)

#### 문제 현상
- 병합된 ㄴ 모양(1x2) 카드에서 두 카드 사이에 투명한 틈이 보임
- 2026-01-02에 시도했던 이슈와 동일

#### 시도한 해결 방법 (실패 → 롤백)

**1. RoundedSprite.shader - UV 재매핑 방식**
```glsl
// 음수 패딩 방향으로 UV 범위 조정
float uvMinX = -leftExpand;
float uvMaxX = 1.0 + rightExpand;
textureUV.x = uvMinX + normalizedUV.x * (uvMaxX - uvMinX);
```
- **문제**: UV 전체가 재매핑되어 음수 패딩이 없는 방향도 영향 받음
- **결과**: 이미지가 압축/늘어나서 합쳐진 카드에서 이미지가 어긋나 보임

**2. DragController.cs - WhiteFrame 두께 보정**
```csharp
const float antiAliasingCompensation = 0.005f;
float whiteFrameThicknessUV = _whiteBorderThickness + _blackBorderThickness + antiAliasingCompensation;
```
- **문제**: 프레임이 두꺼워져서 이미지 일부가 가려짐

#### 결론
- 모든 수정 사항 롤백
- 셰이더에서 UV 조작 방식은 부작용이 크므로 적합하지 않음
- 틈 문제는 현재 상태로 유지 (추후 다른 접근법 검토 필요)

---

## 주요 파일 위치

| 구분 | 경로 |
|------|------|
| 테이블 스크립트 | `Assets/Scripts/Data/Generated/` |
| 테이블 JSON | `Assets/Resources/Tables/` |
| Excel 원본 | `TableExporter/excel/` |
| 테이블 도구 | `TableExporter/__TABLE_SCRIPT__.exe` |
