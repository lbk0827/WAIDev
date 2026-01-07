# 프로젝트 구조

## 디렉토리 구조

```
Assets/
├── Scenes/
│   ├── LobbyScene.unity          # 로비 씬
│   └── GameScene.unity           # 게임 씬
│
├── Scripts/
│   ├── Core/                     # 핵심 시스템
│   │   ├── GameOptionManager.cs  # 게임 옵션 관리
│   │   └── MonoObject.cs         # MonoBehaviour 베이스
│   │
│   ├── Data/                     # 데이터 레이어
│   │   ├── GameDataContainer.cs  # 게임 데이터 컨테이너 (코인, 진행도)
│   │   ├── Observer.cs           # 옵저버 패턴
│   │   ├── OutgameResourcePath.cs # 리소스 경로 관리
│   │   └── Generated/            # 자동 생성 테이블
│   │       ├── CardTable.cs      # 카드 데이터
│   │       ├── ItemTable.cs      # 아이템 데이터
│   │       ├── LevelTable.cs     # 레벨 데이터
│   │       └── LevelGroupTable.cs # 레벨 그룹 데이터
│   │
│   ├── Puzzle/                   # 퍼즐 시스템 (분리된 클래스)
│   │   ├── PieceGroup.cs         # 피스 그룹 관리
│   │   ├── PieceCardController.cs # 카드 플립 시스템 (미사용)
│   │   └── PuzzleAnimationController.cs # 애니메이션 시스템 (미사용)
│   │
│   ├── UI/                       # UI 시스템
│   │   ├── UIView.cs             # UI 뷰 베이스
│   │   ├── UIMediator.cs         # UI 중재자 베이스
│   │   ├── GameUIMediator.cs     # 게임 씬 UI 중재자
│   │   ├── LobbyUIMediator.cs    # 로비 씬 UI 중재자
│   │   ├── CoinDisplay.cs        # 코인 표시 UI
│   │   ├── SettingsPopup.cs      # 설정 팝업
│   │   └── LevelClearSequence.cs # 레벨 클리어 연출
│   │
│   ├── GameManager.cs            # 게임 상태 관리 (Singleton)
│   ├── UIManager.cs              # UI 패널 관리
│   ├── LevelManager.cs           # 레벨 로드/관리
│   ├── LevelGroupManager.cs      # 레벨 그룹 관리
│   ├── SceneTransitionManager.cs # 씬 전환 관리
│   ├── PuzzleBoardSetup.cs       # 퍼즐 보드 생성/로직 (~1,470줄)
│   ├── DragController.cs         # 피스 드래그/프레임 (~1,094줄)
│   ├── GroupBorderRenderer.cs    # 그룹 테두리 렌더링 (~920줄)
│   ├── PuzzleData.cs             # ScriptableObject
│   ├── LobbyGridManager.cs       # 로비 그리드 관리
│   └── LobbyCardSlot.cs          # 로비 카드 슬롯
│
├── Resources/                    # 런타임 로드 리소스
│   └── Textures/                 # 퍼즐 이미지
│
├── Sprites/                      # 스프라이트 에셋
│
├── Materials/                    # 머티리얼 (프레임 셰이더 등)
│
└── Prefabs/                      # 프리팹
```

## 핵심 클래스 설명

### 게임 플로우

| 클래스 | 역할 |
|--------|------|
| `GameManager` | 게임 상태 관리, 씬 간 데이터 전달 |
| `LevelManager` | 레벨 정보 로드, 현재 레벨 관리 |
| `SceneTransitionManager` | 로비 ↔ 게임 씬 전환 |

### 퍼즐 시스템

| 클래스 | 역할 |
|--------|------|
| `PuzzleBoardSetup` | 퍼즐 조각 생성, 슬롯 배치, 스왑/병합 로직 |
| `DragController` | 개별 조각 드래그, 프레임 렌더링, 카드 플립 |
| `PieceGroup` | 연결된 조각들의 그룹 관리 |
| `GroupBorderRenderer` | 그룹 외곽 테두리 LineRenderer |

### UI 시스템

| 클래스 | 역할 |
|--------|------|
| `UIManager` | 패널 활성화/비활성화 |
| `GameUIMediator` | 게임 씬 UI 이벤트 처리 |
| `LobbyUIMediator` | 로비 씬 UI 이벤트 처리 |
| `LevelClearSequence` | 클리어 연출 (DOTween 시퀀스) |

### 데이터 시스템

| 클래스 | 역할 |
|--------|------|
| `GameDataContainer` | 코인, 진행도 저장/로드 (PlayerPrefs) |
| `LevelTable` | 레벨별 설정 (행/열, 보상) |
| `ItemTable` | 아이템 아이콘 등 |

## 씬 구성

### LobbyScene
- 레벨 선택 그리드
- 코인 표시
- 설정 버튼

### GameScene
- 퍼즐 보드
- 상단 UI (레벨 텍스트, 설정 버튼)
- 클리어 연출 UI

## 의존성 흐름

```
GameManager (Singleton)
    ├── LevelManager
    ├── UIManager
    └── SceneTransitionManager

PuzzleBoardSetup
    ├── DragController (각 피스)
    │   └── PieceGroup
    │       └── GroupBorderRenderer
    └── LevelClearSequence
```
