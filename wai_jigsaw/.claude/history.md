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

## 주요 파일 위치

| 구분 | 경로 |
|------|------|
| 테이블 스크립트 | `Assets/Scripts/Data/Generated/` |
| 테이블 JSON | `Assets/Resources/Tables/` |
| Excel 원본 | `TableExporter/excel/` |
| 테이블 도구 | `TableExporter/__TABLE_SCRIPT__.exe` |
