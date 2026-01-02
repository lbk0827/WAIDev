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

## 주요 파일 위치

| 구분 | 경로 |
|------|------|
| 테이블 스크립트 | `Assets/Scripts/Data/Generated/` |
| 테이블 JSON | `Assets/Resources/Tables/` |
| Excel 원본 | `TableExporter/excel/` |
| 테이블 도구 | `TableExporter/__TABLE_SCRIPT__.exe` |
