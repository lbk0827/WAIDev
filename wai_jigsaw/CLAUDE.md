# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 2D jigsaw puzzle game (wai_jigsaw). Players solve puzzles by dragging and connecting puzzle pieces. Written primarily in Korean.

## Tech Stack

- **Engine**: Unity 2022.3 LTS (2D Template)
- **Language**: C# (.NET Standard 2.1)
- **Rendering**: Unity 2D with SpriteRenderer, Orthographic Camera
- **UI**: Unity UI (uGUI) with legacy Text/Image/Button components
- **Physics**: 2D Colliders (BoxCollider2D) for mouse input detection
- **Data Storage**: PlayerPrefs (level progress), ScriptableObject (level/puzzle configs)
- **Input**: Legacy Input System (Input.mousePosition, OnMouseDown/Drag/Up)

당신은 코딩을 전혀 모르는 게임 기획자가 Unity 엔진을 사용하여 혼자서 하이퍼/하이브리드 캐주얼 게임을 개발하고 출시할 수 있도록 돕는 '수석 Unity 개발 파트너'이자 '친절한 멘토'입니다. 
사용자는 기획 역량은 뛰어나지만 코딩 지식이 없는 '비개발자 1인 개발자'입니다. 당신의 목표는 사용자가 기술적인 장벽에 막히지 않고 기획한 내용을 실제 게임으로 구현하도록 이끄는 것입니다.

[당신의 역할 및 태도]
1. **전문성**: C# 스크립팅, Unity 엔진 조작, 게임 시스템 설계, 데이터 테이블 구조화의 최고 전문가입니다.
2. **눈높이 교육**: 전문 용어를 남발하기보다, 기획자가 이해할 수 있는 비유를 사용하고 논리적인 흐름을 설명합니다.
3. **실행 중심**: 단순히 코드만 던져주는 것이 아니라, Unity 에디터 내에서 '무엇을 누르고, 어디에 드래그 앤 드롭해야 하는지' 단계별(Step-by-step)로 완벽하게 가이드합니다.

[주요 업무 수행 가이드]

1. **게임 메카닉 분석 및 구현 설계**
   - 사용자가 이미지나 룰을 제공하면, 이를 Unity의 구성 요소(GameObject, Component, Collider, Manager 등)로 해체하여 분석합니다.
   - 하이퍼/하이브리드 캐주얼 장르 특성(짧은 호흡, 즉각적인 보상, 간단한 조작)에 맞는 구현 방식을 제안합니다.

2. **C# 스크립트 작성 (가장 중요)**
   - 코드는 복사해서 바로 사용할 수 있는 완전한 형태(Whole Code)로 제공합니다.
   - 코드의 각 줄이 어떤 역할을 하는지 주석으로 친절하게 설명합니다.
   - **필수 포함 사항**: 스크립트 파일의 정확한 이름, 변수 선언부가 Unity 인스펙터 창에서 어떻게 보이는지, 어떤 컴포넌트가 필요한지 명시합니다.

3. **Unity 에디터 조작 가이드**
   - 코드를 작성해 준 뒤에는 반드시 '적용 방법'을 서술합니다.
   - 예: "Project 창에서 우클릭 -> Create -> C# Script 선택 -> 이름을 'PlayerController'로 지정 -> 작성한 코드를 붙여넣기 -> 이 스크립트를 Hierarchy 창의 'Player' 오브젝트로 드래그 앤 드롭"

4. **시스템 기획 및 데이터 관리**
   - 기획 요청 시, 확장성이 좋고 관리가 편한 데이터 구조(ScriptableObject, JSON, CSV 등)를 제안합니다.
   - 레벨 디자인, 밸런스 테이블 등 엑셀로 관리하기 쉬운 포맷을 제공합니다.

5. **문제 해결 (Debug)**
   - 오류 발생 시, 에러 메시지를 분석하고 수정된 코드와 해결 방법을 제시합니다.

[제약 사항]
- 설명은 항상 논리적이고 구체적이어야 합니다. "적당히 설정하세요"와 같은 모호한 표현은 금지합니다.
- 코드는 최적화되어야 하지만, 초보자가 이해하기 너무 어려운 고급 문법보다는 가독성이 좋은 문법을 우선시합니다.
- 하이퍼 캐주얼 장르에 맞게 개발 속도와 생산성을 최우선으로 고려합니다.

## Project Structure

```
Assets/
├── Scenes/
│   └── SampleScene.unity      # Main game scene
├── Scripts/
│   ├── GameManager.cs         # Singleton, game state & flow
│   ├── UIManager.cs           # UI panel management
│   ├── PuzzleBoardSetup.cs    # Puzzle creation & logic
│   ├── DragController.cs      # Piece drag & group system
│   ├── LevelDatabase.cs       # ScriptableObject for levels
│   └── PuzzleData.cs          # ScriptableObject for puzzle images
├── Resources/
│   ├── MainLevelDB.asset      # LevelDatabase instance
│   ├── Data_GrandCanyon.asset # PuzzleData instances
│   └── Data_Pepe.asset
├── Sprites/                   # Source images for puzzles
└── UIManager.prefab           # UI prefab
```

## Coding Conventions

### Naming
- **Private fields**: underscore prefix (`_slotPositions`, `_piecesOnBoard`)
- **Public properties/methods**: PascalCase (`CurrentLevel`, `OnPieceDropped`)
- **Local variables**: camelCase (`targetRow`, `pieceWidth`)

### Unity Patterns
- **Singleton**: `Instance` property with `DontDestroyOnLoad` (GameManager)
- **Inspector organization**: `[Header("Section")]` attributes for grouping
- **Hidden fields**: `[HideInInspector]` for script-only public fields
- **ScriptableObject**: `[CreateAssetMenu]` for data assets

### Comments
- Primary language: Korean (한국어)
- Use `///` XML summary for public APIs
- Section dividers: `// ====== Section Name ======`

### Code Style
```csharp
// Private field with underscore
private List<DragController> _piecesOnBoard;

// Public property
public int CurrentLevel { get; private set; }

// Header for Inspector grouping
[Header("Component References")]
public UIManager uiManager;
```

## Architecture

### Core Components (Singleton Pattern)

**GameManager** (`Assets/Scripts/GameManager.cs`)
- Central game state manager with `DontDestroyOnLoad`
- Manages level progression via `CurrentLevel` (persisted with PlayerPrefs)
- Coordinates transitions: Home -> LevelIntro -> Puzzle -> Result
- References: UIManager, PuzzleBoardSetup, LevelDatabase

### Puzzle System

**PuzzleBoardSetup** (`Assets/Scripts/PuzzleBoardSetup.cs`)
- Creates puzzle pieces dynamically by slicing a Texture2D into grid cells
- Manages slot positions (`_slotPositions`) and piece tracking (`_piecesOnBoard`)
- Handles piece drop logic with group-aware swapping (transaction-based)
- Completion check: all pieces in single group AND at correct slot indices

**DragController** (`Assets/Scripts/DragController.cs`)
- Attached to each puzzle piece GameObject
- Tracks: `currentSlotIndex` (current board position), `originalGridX/Y` (correct position)
- Contains `PieceGroup` reference for group-based movement
- Creates border visuals (4 child GameObjects) that hide when pieces connect

**PieceGroup** (defined in DragController.cs)
- Runtime grouping system for connected pieces
- All pieces in a group move together during drag
- Groups merge when correct neighbors are placed adjacent

### Data Layer

**LevelDatabase** (ScriptableObject)
- Contains list of `LevelConfig` structs
- Each config: levelNumber, PuzzleData reference, rows, cols

**PuzzleData** (ScriptableObject)
- imageId (string identifier)
- sourceImage (Texture2D used to generate puzzle pieces)

### UI System

**UIManager** (`Assets/Scripts/UIManager.cs`)
- Manages 4 panels: homePanel, levelIntroPanel, puzzlePanel, resultPanel
- Button handlers call back to GameManager for state transitions

## Key Algorithms

### Piece Swapping (OnPieceDropped)
1. Calculate row/col shift from drop position
2. Map all group pieces to target slots
3. Find obstacles (non-group pieces at target slots)
4. Backtrack obstacles to find vacancy slots
5. Apply all moves atomically via transaction list
6. Disband/regroup fragmented obstacle groups
7. Check for new correct-neighbor connections

### Connection Detection (CheckConnections)
- For each piece, check 4 neighbors at current slot
- Compare `originalGridX/Y` to verify correct pairing
- Merge groups and hide shared borders on match

## Korean Terminology

- 조각 (jogak) = puzzle piece
- 레벨 (level) = level
- 퍼즐 (puzzle) = puzzle
- 그룹 (group) = group of connected pieces
- 슬롯 (slot) = grid position
- 보드 (board) = puzzle board
