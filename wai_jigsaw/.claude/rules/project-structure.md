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
│   │   ├── CollectionPopup.cs    # 컬렉션 팝업
│   │   ├── CollectionChapterCard.cs # 컬렉션 챕터 카드
│   │   ├── ChapterDetailPopup.cs # 챕터 상세 팝업
│   │   ├── LevelClearSequence.cs # 레벨 클리어 연출
│   │   ├── ChapterClearSequence.cs # 챕터 클리어 연출
│   │   ├── HardIntroSequence.cs  # Hard 난이도 인트로 연출
│   │   └── CelebrationController.cs # 클리어 축하 파티클
│   │
│   ├── GameManager.cs            # 게임 상태 관리 (Singleton)
│   ├── UIManager.cs              # UI 패널 관리
│   ├── LevelManager.cs           # 레벨 로드/관리
│   ├── LevelGroupManager.cs      # 레벨 그룹 관리
│   ├── SceneTransitionManager.cs # 씬 전환 관리
│   ├── PuzzleBoardSetup.cs       # 퍼즐 보드 생성/로직
│   ├── DragController.cs         # 피스 드래그/프레임
│   ├── GroupBorderRenderer.cs    # 그룹 테두리 렌더링
│   ├── PuzzleData.cs             # ScriptableObject
│   ├── LobbyGridManager.cs       # 로비 그리드 관리
│   └── LobbyCardSlot.cs          # 로비 카드 슬롯
│
├── Resources/
│   ├── Sprites/
│   │   ├── Levels/               # 레벨 퍼즐 이미지 (Level1.png ~ Level25.png)
│   │   ├── LevelGroups/          # 챕터 완성 이미지 (Reward_Group01.png ~)
│   │   ├── CardBacks/            # 카드 뒷면 이미지
│   │   ├── Common/               # 공통 UI 스프라이트
│   │   ├── Effects/              # 이펙트 스프라이트
│   │   └── Popup/                # 팝업 관련 스프라이트
│   │       └── CollectionPopup/  # 컬렉션 팝업 스프라이트
│   │
│   ├── Materials/                # 머티리얼 (프레임 셰이더 등)
│   │
│   └── Tables/                   # JSON 데이터 테이블
│       ├── LevelTable.json       # 레벨 정보 (행/열, 난이도, 보상)
│       ├── LevelGroupTable.json  # 챕터 정보 (레벨 범위, 이미지 경로)
│       ├── CardTable.json        # 카드 뒷면 정보
│       └── ItemTable.json        # 아이템 정보
│
├── Shaders/                      # 커스텀 셰이더
│   ├── RoundedSprite.shader      # 둥근 모서리 스프라이트
│   └── RoundedFrame.shader       # 둥근 모서리 프레임
│
├── Prefabs/                      # 프리팹
│
└── Fonts/                        # 폰트 에셋
```

## 핵심 클래스 설명

### 게임 플로우

| 클래스 | 역할 |
|--------|------|
| `GameManager` | 게임 상태 관리, 씬 간 데이터 전달 |
| `LevelManager` | 레벨 정보 로드, 현재 레벨 관리 |
| `LevelGroupManager` | 레벨 그룹(챕터) 정보 관리, 이미지 분할 |
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
| `LevelClearSequence` | 레벨 클리어 연출 (DOTween 시퀀스) |
| `ChapterClearSequence` | 챕터 클리어 연출 (이미지 합체 → 컬렉션 이동) |
| `HardIntroSequence` | Hard 난이도 레벨 시작 연출 |
| `CelebrationController` | 클리어 시 파티클 연출 |
| `CollectionPopup` | 컬렉션 팝업 (완성된 챕터 이미지 목록) |
| `SettingsPopup` | 설정 팝업 (사운드, 레벨 이동 등) |

### 로비 시스템

| 클래스 | 역할 |
|--------|------|
| `LobbyGridManager` | 5x5 레벨 카드 그리드 생성/관리 |
| `LobbyCardSlot` | 개별 레벨 카드 슬롯 (앞면/뒷면 전환) |

### 데이터 시스템

| 클래스 | 역할 |
|--------|------|
| `GameDataContainer` | 코인, 진행도 저장/로드 (PlayerPrefs) |
| `LevelTable` | 레벨별 설정 (행/열, 난이도, 보상) |
| `LevelGroupTable` | 챕터별 설정 (레벨 범위, 이미지 경로) |
| `CardTable` | 카드 뒷면 스프라이트 경로 |
| `ItemTable` | 아이템 정보 |

## 씬 구성

### LobbyScene
- 5x5 레벨 카드 그리드
- 코인 표시
- 설정/컬렉션 버튼
- 플레이 버튼
- 챕터 클리어 연출

### GameScene
- 퍼즐 보드
- 상단 UI (레벨 텍스트, 설정 버튼)
- Hard 인트로 연출
- 레벨 클리어 연출
- 축하 파티클

## 의존성 흐름

```
GameManager (Singleton, DontDestroyOnLoad)
    ├── LevelManager (Singleton, DontDestroyOnLoad)
    ├── LevelGroupManager (Singleton, DontDestroyOnLoad)
    └── SceneTransitionManager

LobbyScene:
    LobbyUIMediator
        ├── LobbyGridManager
        │   └── LobbyCardSlot (x25)
        ├── ChapterClearSequence
        ├── CollectionPopup
        └── SettingsPopup

GameScene:
    GameUIMediator
        ├── PuzzleBoardSetup
        │   ├── DragController (각 피스)
        │   │   └── PieceGroup
        │   │       └── GroupBorderRenderer
        │   └── LevelClearSequence
        ├── HardIntroSequence
        ├── CelebrationController
        └── SettingsPopup
```

## 리소스 경로 규칙

| 리소스 타입 | 경로 | 테이블 필드 |
|-------------|------|-------------|
| 레벨 이미지 | `Sprites/Levels/{ImageName}` | LevelTable.ImageName |
| 챕터 이미지 | `Sprites/LevelGroups/{ImageName}` | LevelGroupTable.ImageName |
| 카드 뒷면 | `Sprites/CardBacks/{SpriteName}` | CardTable.CardBackSprite |
