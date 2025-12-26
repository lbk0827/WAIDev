# 작업 히스토리

## 2025-12-26 (2)

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

---

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

## 주요 파일 위치

| 구분 | 경로 |
|------|------|
| 테이블 스크립트 | `Assets/Scripts/Data/Generated/` |
| 테이블 JSON | `Assets/Resources/Tables/` |
| Excel 원본 | `TableExporter/excel/` |
| 테이블 도구 | `TableExporter/__TABLE_SCRIPT__.exe` |
