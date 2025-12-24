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
