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

- Windows 명령 실행은 사용자의 다른 작업을 방해하는 PowerShell/CMD 창을 띄우지 않는다. 이 규칙은 루트와 모든 서브에이전트에 적용한다.
  2026-09-28 이 환경에서는 `exec_command`의 기본 실행과 `tty: true, login: false` 진단 모두 창 노출이 사용자에게 확인됐다. PTY를 검증된 해결책으로 사용하지 않는다. 원인 수정 또는 별도로 검증된 실행 경로가 마련되기 전에는 동일 경로의 명령을 반복 실행하지 않는다. 단, 사용자가 창 노출을 감수하고 현재 작업을 재개하도록 명시한 경우 해당 작업에 한해 실행하며 호출을 묶어 줄인다. 이번 건물 건설 작업은 사용자가 명시적으로 재개했다.
  별도 프로세스는 `Start-Process -WindowStyle Hidden` 또는 `UseShellExecute=false`와 `CreateNoWindow=true`를 지정한 실행 경로를 사용한다. 눈에 보이는 터미널이 필요한 경우에만 사용자의 명시적 요청에 따라 연다.
  창이 반복 노출되면 해당 실행과 재시도를 즉시 중단하고 실행 경로를 확인한다. 사용자 설정·기본 터미널·전역 프로세스를 임의로 바꾸거나 종료하지 않는다.
- 코드·그래픽·Unity 배선 구현은 각각 `author-unity-code`, `create-project-sprites`, `assemble-unity-objects` 역할의 별도 작업자에게 위임한다. 작은 변경이나 기존 도구 실행도 해당 역할이 담당한다.
  루트는 요구 정리·할당·조정·실제 diff 및 증거 확인·최종 보고를 맡고 해당 역할의 구현을 대신하지 않는다. 작업자 실패 시에도 같은 역할로 재할당한다.
  루트는 작업 기록·공통 문서·스킬 정책을 관리하고 읽기 전용 검사와 검증 명령을 실행할 수 있다. 이 정책/문서 유지보수는 코드·그래픽·배선 구현과 구분한다.
  작업자는 `fork_turns="none"`으로 시작하고 역할 명세·관련 문서·승인된 연결 규약만 전달받는다. 다른 역할로 재사용하지 않는다.
  필요한 역할만 생성하되 독립된 코드·그래픽 작업은 병렬로 진행한다. 배선 담당은 조사·준비를 병행하고 입력 코드·에셋이 준비되고 컴파일된 뒤 적용한다. 같은 Editor의 import·컴파일·저장은 순차 진행한다.
  역할별 수정 경로를 먼저 정하고 실제 diff와 대조한다. 담당 밖 파일은 직접 고치지 않고 루트에 전달한다. 배선용 C# helper도 코드 담당이 작성하고 배선 담당이 실행한다.
  C# 변경은 컴파일·컨벤션·책임 배치·의존 방향·변경 범위 근거와 별도 읽기 전용 리뷰 결과를 필수로 확인한다. 작성자의 자기 보고나 리뷰 실행 시작만으로 완료하지 않는다.
  세부 할당·인계·검사 규칙은 `.codex/skills/orchestrate-unity-work/references/worker-coordination.md`를 따른다. 사용자의 명시적 예외 요청만 역할 경계를 변경할 수 있다.
- 자동 Play Mode 실행과 게임 화면 검증(스크린샷 수집·판독)은 기본 작업 절차와 필수 완료 조건에서 제외한다.
  해당 검증을 위한 재시도·전용 검사 코드 작성도 수행하지 않는다. 사용자가 그 자동 검증을 별도로 명시 요청한 경우에만 범위를 정해 실행한다.
  기존 계획·스킬의 일반적인 runtime/시각 QA 항목은 사람의 확인 항목으로 전달하며 자동 실행 의무로 해석하지 않는다.
  사람의 플레이·화면 확인은 짧게 인계하고 미검증으로 기록하되, 그 부재만으로 구현 완료를 막지 않는다. 실행하지 않은 검증을 통과로 보고하지 않는다.
  컴파일·정적 검사·씬 참조와 구조 검사·책임 및 의존 방향 검증은 유지한다.
- `CLAUDE.md`와 `.claude`는 Claude 전용이다. Codex는 읽거나 수정하지 않는다.
- 실제 코드·Unity 직렬화와 문서가 다르면 코드와 에셋을 현재 사실로 보고 같은 변경에서 주 소유 기능 문서를 교정한다.
- 하나의 production C#은 하나의 Systems leaf 문서만 주 소유한다.
- 세부 기능이 4개 이상인 영역은 폴더형 README와 leaf 문서로 분할한다.
- 기존 파일의 local style이 명확하면 유지하되 새 코드에는 `CodeConvention.md`를 적용한다.
- 사용자의 관련 없는 미커밋 변경을 수정하거나 되돌리지 않는다.
- 씬·프리팹 배선은 YAML 직접 편집으로 진행해도 된다. 먼저 해당 씬의 Hierarchy, 역할별 묶음,
  명명·배치 패턴을 파악하고 그 구조를 유지한다. 배선 방식이나 Editor 연결 부재만으로 재승인을 요구하지 않는다.
  관련 ScriptableObject 참조도 실제 serialized field와 GUID를 확인해 연결하고 기존 변경을 보존한다.
  코드의 책임·의존 방향·컨벤션·컴파일 검증을 우선하며, 배선 검사와 실제 실행 검증 결과는 구분해 기록한다.
