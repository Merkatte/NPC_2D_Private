# Claude Project Notes

이 파일은 Claude Code가 프로젝트 루트에서 작업을 시작할 때 먼저 참고할 프로젝트 안내입니다.

## 참조 금지 항목

- `AGENTS.md` 파일과 `.codex` 폴더는 오로지 Codex를 위한 전용 파일/폴더입니다.
- Claude는 어떤 작업에서도, 어떤 이유로도 `AGENTS.md`와 `.codex`의 내용을 읽거나 참조하지 마세요.
- `AGENTS.md`와 `.codex`는 수정, 삭제, 생성 등 어떤 형태의 변경도 하지 마세요.

## PublicMD 문서 인덱스

프로젝트 루트의 `PublicMD` 폴더에는 장기 유지되는 기획, 구조, 코드 규칙, 진행 상태 문서가 있습니다. 전체 안내는 `PublicMD/README.md`가 소유하며, 이 섹션은 Claude가 작업 시 어떤 문서를 먼저 읽어야 하는지 요약합니다. 모든 문서를 무조건 다 읽을 필요는 없으니 아래 라우팅을 따라 필요한 문서만 골라 읽으세요.

### 기본 읽기 흐름

```text
PublicMD/ProjectStructure.md (구조 지도 + 작업별 문서 라우팅표)
  -> PublicMD/Systems/ 안의 관련 기능 문서
     -> 기능이 폴더형(Systems/Combat, Systems/NPC_Decision_and_Actions)이면
        해당 README.md에서 leaf 문서를 다시 선택
```

### `PublicMD/ProjectStructure.md`

역할:

- 전체 구조 지도와 기능별 문서 routing table을 소유합니다. **이 문서는 Claude가 매번 모든 스크립트를 직접 탐색하지 않고도 필요한 문서와 폴더를 바로 찾아가게 하려고 만들어졌습니다. 구조나 책임 배치 관련 작업에서는 코드베이스를 먼저 훑기 전에 이 문서의 라우팅표부터 확인하세요.**
- 현재 구조: `NPCManager`/`WorkerPool` -> `WorkerNPC` -> `BaseNPCActionSelector` -> `DestinationDecider`/`DestinationDB`/`ActionPool` -> `IAction`/`NPCStat`/`NPCComponent`. **`WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, `WorkerActionSet`, Behavior Graph 기반 실행기는 현재 코드베이스에 존재하지 않으므로 참조하지 마세요.**
- 작업 종류(queue 처리, selector/action, 이동, Farming, Combat, destination/provider, inventory, presentation, UI, spawning 등)별로 어떤 `PublicMD/Systems/` 문서를 읽어야 하는지 표로 정리되어 있습니다.
- 최상위 폴더(`Assets/Scripts/Actor`, `Manager`, `System/Actor`, `System/Action`, `System/Farming`, `System/Inventory`, `System/Lib`, `System/Mapper`, `UI`, `Interface`, `Assets/Data/Struct`, `Assets/Data/ScriptableObject/Script`, `Assets/TestOnly`)의 책임과 의존 방향, 새 기능을 어디에 배치할지도 여기서 확인합니다.

읽어야 하는 경우:

- worker/NPC 행동 흐름을 수정할 때
- 새 `IAction`, selector, destination, movement, decision policy를 추가하거나 변경할 때
- 어느 폴더·문서에 코드를 둬야 할지 판단이 필요할 때
- 다른 `PublicMD` 문서를 읽기 전에, 이번 작업에 실제로 필요한 문서가 무엇인지 특정할 때

### `PublicMD/Systems/`

역할:

- 기능별(NPC_Runtime, NPC_Decision_and_Actions, Farming, Combat, Interaction_and_Destinations, Inventory_and_Items, NPC_Presentation, UI, Spawning_and_Pooling) 현재 실행 흐름, 주 소유 스크립트, Unity 배선을 소유합니다.
- `Combat/`과 `NPC_Decision_and_Actions/`는 세부 기능이 4개 이상이라 폴더형 인덱스(`README.md` + leaf 문서)로 분할되어 있습니다. 이 두 영역을 다룰 때는 하위 `README.md`의 라우팅표에서 필요한 leaf 문서만 선택해서 읽습니다.
- `ProjectStructure.md`의 라우팅표가 가리키는 문서만 읽으면 되고, 관련 없는 다른 기능 문서까지 읽을 필요는 없습니다.

읽어야 하는 경우:

- `ProjectStructure.md`가 라우팅한 구체 기능 문서를 읽을 때
- 특정 도메인(농사, 전투, 판단/액션, 상호작용·목적지, 인벤토리, 표현, UI, 스포닝)의 클래스 흐름이나 Unity 배선을 확인·수정할 때

### `PublicMD/CodeConvention.md`

역할:

- 이 Unity 프로젝트의 모든 C# 코드에 공통 적용되는 작성·검증 규칙을 설명합니다.
- 네이밍, 파일·폴더 배치, 책임 분리, interface·value object 사용, ScriptableObject vs runtime state, Unity lifecycle/serialization, pooling, transaction, 결정성, hot path, 주석·문서 기준, 검증 체크리스트를 정리합니다.

읽어야 하는 경우:

- C# 스크립트를 새로 만들거나 수정할 때
- private field, serialized field, public property, enum, interface 이름을 정할 때
- action, selector, provider, manager 등 책임 경계를 판단할 때
- Unity lifecycle, serialization, null check, pooling, transaction 스타일을 맞춰야 할 때
- 기존 코드와 새 코드의 스타일 차이를 줄여야 할 때

### 그 외 루트 문서

- `PublicMD/Game_Plan.md`: 핵심 재미, 플레이 경험, 상위 게임 기획. 게임 규칙을 새로 설계하거나 바꿀 때 먼저 읽습니다.
- `PublicMD/SPEC.md`: 확정 요구사항과 미결정 질문. `Game_Plan.md`와 함께 게임 규칙 작업 시 읽습니다.
- `PublicMD/PLAN.md`: 구현 roadmap, 우선순위, 완료 조건. 다음에 무엇을 할지 판단할 때 읽습니다.
- `PublicMD/ARCHITECTURE.md`: 여러 기능에 공통인 설계 원칙과 의존 방향. 여러 기능의 책임·의존 방향을 함께 바꿀 때 읽습니다.

### `PublicMD/Status/`

- `PublicMD/Status/PROGRESS.md` (이전 경로 `PublicMD/PROGRESS.md`): 누적 구현 진행 기록.
- `PublicMD/Status/Code_Evaluation_Result.md` (이전 경로 `PublicMD/Code_Evaluation_Result.md`): 최신 코드 리뷰 결과.

### `PublicMD/Archive/Plans/`

- 구현이 끝났거나 현재 기준이 아닌 상세 계획을 보관합니다. 당시 경로·판단을 그대로 보존한 기록이므로 현재 구조 판단의 근거로 사용하지 않습니다.

## 작업 원칙

- 구조나 책임 배치가 관련된 작업은 `ProjectStructure.md`의 라우팅표를 먼저 읽고, 거기서 지목하는 `Systems/` 문서만 이어서 읽으세요. 라우팅 없이 코드베이스 전체를 먼저 훑지 마세요.
- C# 코드 변경 작업은 `CodeConvention.md`를 먼저 읽으세요.
- worker 행동 변경은 selector(무엇을 할지, queue 구성), action(선택된 행동의 실행·성공/실패/취소), provider(destination과 domain transaction), `WorkerNPC`(queue 소비와 공통 runtime) 중 알맞은 책임 위치에 둡니다. `WorkerAI`/`WorkerActionPlan`/`WorkerActionContext`는 현재 구조에 없으므로 새 코드를 이 이름으로 만들지 마세요.
- 새 worker 행동은 보통 `Assets/Scripts/System/Action` 아래의 새 `IAction` 구현으로 시작합니다. 어떤 selector가 이를 사용할지는 `ProjectStructure.md`와 해당 `Systems/` 문서에서 확인합니다.
- 새 기능이 어떤 최상위 폴더(Manager, System/Actor, System/Action, Interface, Data/Struct 등)에 속하는지는 `ProjectStructure.md`의 폴더 책임표를 따릅니다.
- 기존 파일의 로컬 스타일이 명확하면 그 스타일을 우선 유지하되, 새 코드에는 `CodeConvention.md`의 규칙을 적용합니다.
- 문서와 실제 코드·Unity 직렬화가 다르면 코드를 현재 사실로 보고, 같은 작업에서 주 소유 `Systems/` 문서를 갱신합니다.

## AGENTS.md 최신본 반영 (Claude 참고 원칙)

아래는 Codex 전용 `AGENTS.md`가 최근 갱신되며 정리된 원칙을 Claude 관점으로 옮겨 기록한 것이다. 위 섹션(`참조 금지 항목`, `PublicMD 문서 인덱스`, `작업 원칙`)이 더 구체적인 라우팅표를 담고 있으므로 내용이 겹치면 위쪽을 우선한다.

### 문서 읽기 원칙

모든 PublicMD와 모든 코드를 먼저 읽지 않는다.

1. `PublicMD/ProjectStructure.md`의 작업별 표에서 관련 기능 문서를 찾는다.
2. 기능이 폴더라면 해당 `README.md`에서 필요한 leaf 문서만 선택한다.
3. leaf의 `변경 유형별 최소 확인 범위`가 지정한 코드와 Unity 에셋만 먼저 연다.
4. 아래 `조건부 필수 문서`에 해당할 때만 루트 공통 문서를 추가한다.

전체 문서 지도와 기능 문서 작성 규칙은 각각 `PublicMD/README.md`, `PublicMD/Systems/README.md`를 참고한다.

### 조건부 필수 문서

#### `PublicMD/Game_Plan.md`와 `PublicMD/SPEC.md`

새 게임 시스템, 플레이 경험, 주민·생산·경제·전투·건물·UI 규칙을 설계하거나 바꿀 때 읽는다. 미확정 항목은 `TBD`로 유지하고 구현자가 임의로 게임 규칙을 확정하지 않는다.

#### `PublicMD/PLAN.md`

새 기능 구현을 시작하기 전 현재 우선순위, 선행 작업, 완료 조건과 QA checkpoint를 확인해야 할 때 읽는다.

`PLAN.md`가 선택한 기능의 활성 상세 계획을 `PublicMD/Plans` 아래에서 연결하면 그 문서도 읽는다. 완료된 상세 계획은 `PublicMD/Archive/Plans`로 이동한다.

#### `PublicMD/ARCHITECTURE.md`

둘 이상의 기능 사이에서 책임을 옮기거나 공통 interface, dependency direction, runtime ownership을 변경할 때 읽는다. 한 기능 내부의 일반 수정에는 해당 Systems 문서를 우선한다.

#### `PublicMD/CodeConvention.md`

C# 파일을 새로 만들거나 수정할 때 읽는다. naming, serialized field, interface, Unity lifecycle, null, pooling, transaction과 enum serialization 규칙을 따른다.

#### `PublicMD/ProjectStructure.md`

구조 지도와 기능 문서 routing이 필요할 때 읽는다. 구체 클래스·flow·scene 배선은 여기서 다시 찾지 말고 연결된 Systems 문서로 이동한다.

### 현재 핵심 구조

- `WorkerNPC`는 현재 action과 queue lifecycle만 소유한다.
- selector는 다음 semantic 행동과 queue 구성을 소유한다.
- `DestinationDecider`는 직업 중립 utility 판단을 소유한다.
- action은 선택된 행동의 실행·완료·실패·재판단 규칙을 소유한다.
- provider는 facility domain transaction을 소유한다.
- `NPCComponent`는 이동과 animation·도구·방향 presentation adapter다.
- scene runtime 상태는 scene component, 공유 definition과 tuning은 ScriptableObject가 소유한다.

현재 프로젝트에는 `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph 기반 실행기가 없다.

### 새 코드 기본 배치

- 새 action: `Assets/Scripts/System/Action`
- actor runtime·selector·Unity adapter: `Assets/Scripts/System/Actor`
- scene actor·facility component: `Assets/Scripts/Actor`
- scene 조립·registry entry point: `Assets/Scripts/Manager`
- 안정적인 공통 계약: `Assets/Scripts/Interface`
- request/result/value: `Assets/Data/Struct`
- 공유 definition·cost·tuning: `Assets/Data/ScriptableObject/Script`

구체 배치는 관련 Systems 문서의 책임 경계를 우선한다.

### 작업 원칙 (Claude 버전)

- `AGENTS.md`와 `.codex`는 Codex 전용이다. Claude는 어떤 작업에서도, 어떤 이유로도 읽거나 수정하지 않는다(위 `참조 금지 항목` 섹션과 동일한 규칙).
- 실제 코드·Unity 직렬화와 문서가 다르면 코드와 에셋을 현재 사실로 보고 같은 변경에서 주 소유 기능 문서를 교정한다.
- 하나의 production C#은 하나의 Systems leaf 문서만 주 소유한다.
- 세부 기능이 4개 이상인 영역은 폴더형 README와 leaf 문서로 분할한다.
- 기존 파일의 local style이 명확하면 유지하되 새 코드에는 `CodeConvention.md`를 적용한다.
- 사용자의 관련 없는 미커밋 변경을 수정하거나 되돌리지 않는다.

## 답변 규칙

- 다른 언어 없이 한국어로 답변한다. (문서·코드·주석·커밋 메시지 등은 이 규칙과 무관하게 기존 관례를 따른다.)
