# Hard 난이도 입장 플로우 정리 (영상 관찰 + 기획 스펙 v1.1)

본 문서는 (1) 제공된 영상에서 관찰된 Hard 진입 흐름을 요약하고, (2) 프로젝트 요구사항에 맞춰 수정된 Hard 진입 기획 스펙(v1.1)을 정리한 것입니다.  
전제: 모바일 세로(Portrait), 레벨 기반 진행, 난이도는 LevelTable의 Difficulty 컬럼으로 판정.

---

## 1. 영상에서 확인된 Hard 진입 플로우 (관찰 요약)

> 아래는 영상에서 확인된 “Hard 난이도 입장” 체감 흐름을 단계별로 정리한 내용입니다.

### 1.1 단계 흐름
1) **Home 화면**
- 중앙에 이미지 프리뷰(격자 오버레이 느낌) 노출
- 하단에 **플레이 버튼**
- 텍스트/요소로 **현재 레벨**이 표시됨(예: 레벨 24)

2) **플레이 버튼 탭**
- 즉시 화면 전환(페이드/컷)
- **로딩 상태** 진입: 단색 배경 + **스피너**

3) **플레이 화면 진입**
- 상단에 **레벨 표기**, 우측 설정(기어) 아이콘
- 퍼즐 보드/타일(조각) 준비가 시작됨

4) **Hard 인트로 오버레이 등장**
- 화면 중앙 배너 형태로 **“하드 레벨(Hard)”** 메시지 노출
- 배경/보드가 **레드 톤(위험/고난도)** 로 강조됨
- 타일/조각이 단계적으로 채워지는 **준비/딜링(Deal) 연출**이 함께 보임

5) **인트로 종료 후 플레이 가능 상태**
- Hard 배너가 사라지고 정상 플레이가 가능한 상태로 전환

### 1.2 핵심 인사이트
- Hard 진입은 단순 UI 표시가 아니라:
  - **별도 인트로 배너**
  - **레드 톤 강조(틴트/비네팅)**
  - **짧은 준비 연출(딜링/배치)**
  - **입력 잠금이 자연스러운 타이밍(연출 끝나고 플레이)**
  을 통해 “난이도 변화”를 명확히 인지시키는 구조임.

---

## 2. Hard 난이도 입장 기획 스펙 v1.1 (수정 반영 최종안)

### 2.1 변경 요약(요구사항 반영)
- **LevelIntro 화면은 없음** → `Home → Loading → (Hard 인트로) → Play`
- **Hard 인트로 스킵 불가**
- **Hard 인트로는 재생될 수 있음**(재진입/이어하기 포함)
- Hard 판정 기준은 **LevelTable의 Difficulty 컬럼**을 참조(절대 규칙)

---

## 3. Difficulty 판정 규칙 (확정)

### 3.1 Hard 여부 판정
- Hard 여부는 오직 **LevelTable.Difficulty** 로 결정
  - 예: `Difficulty == Hard` (enum 또는 문자열)
- 조각 수(pieceCount/grid)는 난이도 판정에 사용하지 않음  
  (단, 레벨 테이블에서 조각 수를 증가시키는 밸런싱은 별도 가능)

### 3.2 LevelTable 최소 컬럼(필수)
- `LevelNumber` (int)
- `ImageId` (string)
- `GridCols` (int)
- `GridRows` (int)
- `Difficulty` (Easy / Normal / Hard)

---

## 4. 화면/상태 플로우 (선형 진행, LevelIntro 없음)

### 4.1 메인 플로우
1) **Home → Start**
- 진행 중 세션이 없으면: 새 세션 시작
- 진행 중 세션이 있으면: Continue(이어하기)

2) **Loading**
- 이미지 로드/리사이즈
- 퍼즐 조각 생성 및 초기 배치 준비
- 이어하기인 경우 저장된 상태 복원 적용 준비

3) **분기**
- `LevelTable.Difficulty == Hard` → `Play_EnterHard` → `Play`
- 그 외 → `Play`

### 4.2 상태(State) 정의 (권장)
- `Home`
- `Loading`
- `Play_EnterHard`  (Hard 전용 진입 연출)
- `Play`
- `Result`
- `Gallery`

---

## 5. Hard 인트로(Play_EnterHard) UX 스펙

### 5.1 실행 조건(중요)
- 아래 조건이면 **항상** Hard 인트로를 재생한다.
  - 해당 레벨의 `LevelTable.Difficulty == Hard`
  - 새 시작/이어하기/재진입 구분 없이 동일하게 적용 가능

### 5.2 스킵 규칙
- **스킵 기능 없음**
  - 탭/스와이프/뒤로가기 등으로 인트로를 건너뛰지 못함
  - 인트로 재생 중에는 **Input Lock ON** 유지

### 5.3 연출 시퀀스(고정)
- 권장 총 길이: **1.0 ~ 1.5초**
- 시퀀스 진행 중: **InputLock = true**

#### Step 1) 입력 잠금
- `InputLock = true`

#### Step 2) 레드 톤 강조
- `HardTintOverlay ON`
  - 레드 틴트 또는 비네팅(반투명 15~25% 권장)

#### Step 3) Hard 배너 노출
- `HardBanner.Show()`
  - 중앙 배너 텍스트: “HARD” 또는 “하드 레벨”
  - 노출 시간: 0.6~0.8초 권장
  - 선택: 사운드/햅틱 1회(설정 토글 준수)

#### Step 4) 준비/딜링 연출(가벼운 구현)
- `Deal/Prepare Animation`
  - 조각/트레이/UI 요소가 순차적으로 등장하는 느낌
  - 0.4~0.7초 권장

#### Step 5) 배너 종료
- `HardBanner.Hide()` (0.2~0.3초 페이드 아웃)

#### Step 6) 틴트 종료
- `HardTintOverlay OFF`
  - (MVP 권장: Play 중에는 OFF)
  - (옵션: Play 중 약하게 유지하는 모드도 가능)

#### Step 7) 입력 잠금 해제 및 Play 전환
- `InputLock = false`
- `GoTo Play`

---

## 6. 다시하기(Retry) 규칙 (수정 반영)

### 6.1 Retry 처리
- Home에서 Continue로 진입하는 경우에도:
  - `Loading`에서 세션 복원 완료
  - 레벨의 `Difficulty == Hard`이면 **Hard 인트로를 다시 재생**
- 주의(안정성):
  - Hard 인트로는 **복원/생성 완료 후**, 실제 입력 가능 상태 전환 직전에 실행해야 함  
    (인트로가 끝났는데 조각/보드가 아직 준비되지 않아 입력을 받아버리는 상황 방지)

---

## 7. Unity 구현 지시 (코딩 A.I. 전달용 핵심)

### 7.1 분기 로직(의사코드)
- `StartPlay(levelNumber, isResume)`:
  - `levelConfig = LevelTable.Get(levelNumber)`
  - `LoadAndPreparePuzzle(levelConfig, isResume)`
  - 준비 완료 후:

```text
if (levelConfig.Difficulty == Hard)
    Run EnterHardSequence()    // Resume 포함 항상 실행 가능
GoTo Play
