# Codex Project Notes

이 파일은 Codex가 프로젝트 루트에서 작업을 시작할 때 먼저 참고할 안내다.

## 문서 읽기 원칙

모든 PublicMD와 모든 코드를 먼저 읽지 않는다.

1. `PublicMD/ProjectStructure.md`의 작업별 표에서 관련 기능 문서를 찾는다.
2. 기능이 폴더라면 해당 `README.md`에서 필요한 leaf 문서만 선택한다.
3. leaf의 `변경 유형별 최소 확인 범위`가 지정한 코드와 Unity 에셋만 먼저 연다.
4. 아래 조건에 해당할 때만 루트 공통 문서를 추가한다.

전체 문서 지도와 기능 문서 작성 규칙은 각각 `PublicMD/README.md`, `PublicMD/Systems/README.md`를 참고한다.

## 조건부 필수 문서

### `PublicMD/Game_Plan.md`와 `PublicMD/SPEC.md`

새 게임 시스템, 플레이 경험, 주민·생산·경제·전투·건물·UI 규칙을 설계하거나 바꿀 때 읽는다. 미확정 항목은 `TBD`로 유지하고 구현자가 임의로 게임 규칙을 확정하지 않는다.

### `PublicMD/PLAN.md`

새 기능 구현을 시작하기 전 현재 우선순위, 선행 작업, 완료 조건과 QA checkpoint를 확인해야 할 때 읽는다.

`PLAN.md`가 선택한 기능의 활성 상세 계획을 `PublicMD/Plans` 아래에서 연결하면 그 문서도 읽는다. 완료된 상세 계획은 `PublicMD/Archive/Plans`로 이동한다.

### `PublicMD/ARCHITECTURE.md`

둘 이상의 기능 사이에서 책임을 옮기거나 공통 interface, dependency direction, runtime ownership을 변경할 때 읽는다. 한 기능 내부의 일반 수정에는 해당 Systems 문서를 우선한다.

### `PublicMD/CodeConvention.md`

C# 파일을 새로 만들거나 수정할 때 읽는다. naming, serialized field, interface, Unity lifecycle, null, pooling, transaction과 enum serialization 규칙을 따른다.

### `PublicMD/ProjectStructure.md`

구조 지도와 기능 문서 routing이 필요할 때 읽는다. 구체 클래스·flow·scene 배선은 여기서 다시 찾지 말고 연결된 Systems 문서로 이동한다.

## 현재 핵심 구조

- `WorkerNPC`는 현재 action과 queue lifecycle만 소유한다.
- selector는 다음 semantic 행동과 queue 구성을 소유한다.
- `DestinationDecider`는 직업 중립 utility 판단을 소유한다.
- action은 선택된 행동의 실행·완료·실패·재판단 규칙을 소유한다.
- provider는 facility domain transaction을 소유한다.
- `NPCComponent`는 이동과 animation·도구·방향 presentation adapter다.
- scene runtime 상태는 scene component, 공유 definition과 tuning은 ScriptableObject가 소유한다.

현재 프로젝트에는 `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph 기반 실행기가 없다.

## 새 코드 기본 배치

- 새 action: `Assets/Scripts/System/Action`
- actor runtime·selector·Unity adapter: `Assets/Scripts/System/Actor`
- scene actor·facility component: `Assets/Scripts/Actor`
- scene 조립·registry entry point: `Assets/Scripts/Manager`
- 안정적인 공통 계약: `Assets/Scripts/Interface`
- request/result/value: `Assets/Data/Struct`
- 공유 definition·cost·tuning: `Assets/Data/ScriptableObject/Script`

구체 배치는 관련 Systems 문서의 책임 경계를 우선한다.

## 작업 원칙

- `CLAUDE.md`와 `.claude`는 Claude 전용이다. Codex는 읽거나 수정하지 않는다.
- 실제 코드·Unity 직렬화와 문서가 다르면 코드와 에셋을 현재 사실로 보고 같은 변경에서 주 소유 기능 문서를 교정한다.
- 하나의 production C#은 하나의 Systems leaf 문서만 주 소유한다.
- 세부 기능이 4개 이상인 영역은 폴더형 README와 leaf 문서로 분할한다.
- 기존 파일의 local style이 명확하면 유지하되 새 코드에는 `CodeConvention.md`를 적용한다.
- 사용자의 관련 없는 미커밋 변경을 수정하거나 되돌리지 않는다.
