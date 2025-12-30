# WAI Jigsaw - 작업 내역

## 완료된 작업

### CardSlot 크기 조정
- **파일**: `Assets/Scripts/PuzzleBoardSetup.cs`
- **변경 내용**: `slotSizeRatio`를 `0.98f`에서 `1.0f`로 변경
- **목적**: CardSlot 크기를 카드와 동일하게 맞춤

---

## 보류/미해결 작업

### WhiteFrame 테두리 경계선 문제
- **문제**: 병합된 카드 사이에 WhiteFrame으로 인한 경계선이 보임
- **시도한 방안**:
  1. **방안 1**: 방향별 프레임 두께 조절 (셰이더 수정)
     - `_FrameThicknesses` Vector4로 방향별 두께 제어
     - 결과: 일부 개선되었으나 회색 경계선 여전히 보임
  2. **방안 3**: 4개의 개별 Edge 오브젝트 + 4개의 Corner 오브젝트
     - 프레임을 8개의 개별 오브젝트로 분리
     - 결과: 모서리가 직각으로 표시되는 문제 발생
- **현재 상태**: 롤백하여 원래 상태로 복원, 추후 재작업 필요

---

## 관련 파일

### 셰이더
- `Assets/Shaders/RoundedFrame.shader` - 프레임 렌더링 (둥근 모서리 지원)
- `Assets/Shaders/RoundedSprite.shader` - 스프라이트 둥근 모서리

### 핵심 스크립트
- `Assets/Scripts/PuzzleBoardSetup.cs` - 퍼즐 보드 설정, 슬롯 생성
- `Assets/Scripts/DragController.cs` - 드래그 제어, 프레임 관리

---

## 메모

- `_HideDirections` Vector4를 통해 방향별 프레임 숨김 가능 (Top, Bottom, Left, Right)
- `_CornerRadii` Vector4를 통해 개별 모서리 반경 제어 가능 (TL, TR, BL, BR)
