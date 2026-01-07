# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 필수 참조 문서

다음 문서를 반드시 참조할 것:

| 문서 | 경로 | 설명 |
|------|------|------|
| 프로젝트 구조 | `.claude/rules/project-structure.md` | 디렉토리/클래스 구조 |
| 멘토 역할 | `.claude/rules/mentor-role.md` | 비개발자 지원 가이드 |
| 리팩토링 규칙 | `.claude/rules/refactoring-rule.md` | 코드 분리/정리 기준 |
| 작업 히스토리 | `.claude/rules/history.md` | 전체 프로젝트 변경 이력 |

---

## Project Overview

Unity 2D jigsaw puzzle game (wai_jigsaw). Players solve puzzles by dragging and connecting puzzle pieces. Written primarily in Korean.

## Tech Stack

- **Engine**: Unity 2022.3 LTS (2D Template)
- **Language**: C# (.NET Standard 2.1)
- **Rendering**: Unity 2D with SpriteRenderer, Orthographic Camera
- **UI**: Unity UI (uGUI) + TextMeshPro
- **Animation**: DOTween
- **Physics**: 2D Colliders (BoxCollider2D) for mouse input detection
- **Data Storage**: PlayerPrefs (level progress), ScriptableObject (level/puzzle configs)
- **Input**: Legacy Input System (Input.mousePosition, OnMouseDown/Drag/Up)

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

| 한국어 | English | 설명 |
|--------|---------|------|
| 조각 (jogak) | piece | 퍼즐 조각 |
| 레벨 (level) | level | 게임 레벨 |
| 퍼즐 (puzzle) | puzzle | 퍼즐 |
| 그룹 (group) | group | 연결된 조각들 |
| 슬롯 (slot) | slot | 그리드 위치 |
| 보드 (board) | board | 퍼즐 보드 |
| 테두리 (teturi) | border | 조각/그룹 테두리 |
| 병합 (byeonghap) | merge | 그룹 합치기 |
