# Code Evaluation Result

## 평가 목적

2026-08-15 기준 NPC 작업 프로토타입의 전체 구조를 읽기 전용으로 평가했다. 이번 평가는 `DestinationDecider` 리팩터링 자체뿐 아니라 다음 흐름 전체를 대상으로 한다.

```text
NPCManager / WorkerPool
  -> WorkerNPC
  -> Selector
  -> DestinationDecider
  -> NPCDecision
  -> Action queue / ActionPool
  -> IAction
  -> DestinationDB / IInteractionProvider
  -> NPCStat / data assets
```

생산 코드는 수정하지 않았다. 이 문서만 현재 코드에 맞게 갱신했다.

## 검증 범위

- `Assets/Scripts`의 프로젝트 소유 C# 코드 전체
- `Assets/Data`의 NPC 결정 관련 구조체와 ScriptableObject
- `Assets/TestOnly`의 수동 스모크 테스트 도구와 데이터
- `Assets/Scenes/SampleScene.unity`
- `Assets/Prefab/NPCGirl.prefab`
- `PublicMD/CodeConvention.md`
- `PublicMD/ProjectStructure.md`
- `PublicMD/DestinationDecider_Refactor_Plan.md`

검증 결과:

- `dotnet build Assembly-CSharp.csproj --no-restore`: 경고 0, 오류 0
- `git diff --check`: 문제 없음
- 자동화된 Edit Mode/Play Mode 테스트: 없음
- Unity Editor Play Mode 및 수동 스모크 테스트: 실행하지 않음

## 총평

현재 구조의 중심 방향은 좋다. `WorkerNPC`가 실행 루프를, selector가 결정의 큐 변환을, `DestinationDecider`가 정책을, action이 실행을, provider가 구체 아이템 제공과 실행을 담당한다. 특히 구체 `ItemId`와 전체 `StatEffect`를 결정부터 실행까지 전달하도록 바꾼 것은 앞으로 복합 효과 아이템이 늘어날 때 반드시 필요한 수정이었다.

다만 아직은 "확장 가능한 완성 구조"라기보다 "올바른 중심축을 세운 프로토타입"에 가깝다. 가장 큰 약점은 `DestinationDecider`가 아니라 그 바깥의 런타임 수명주기다. 필수 씬 참조는 연결되었지만 Play Mode 검증이 남아 있고, `WorkerNPC`는 모든 NPC를 Farmer로 판단하며, action 계약은 성공/실패/취소를 구분하지 못한다.

구조 평가는 다음과 같다.

- 의사결정과 실행의 분리: 좋음
- 데이터 기반 튜닝 방향: 좋음
- 아이템 예측/실행 일치: 좋음
- 직업 확장성: 아직 Farmer 전용 규칙이 공용 계층에 남음
- 액션 수명주기와 풀 재사용 안전성: 보강 필요
- Unity 씬 조립: 필수 참조 연결 완료, Play Mode 검증 필요
- 테스트와 문서 신뢰도: 낮음

최종 판단: 설계 방향은 승인할 수 있지만, Play Mode 검증 전에는 런타임 기능 완료로 볼 수 없다. 다음 기능을 추가하기 전에 NPC 직업 정체성과 action 상태 모델을 먼저 닫는 편이 비용이 가장 적다.

## 잘된 점

- `DestinationDecider`는 직접 stat을 변경하거나 action을 생성하지 않는다.
- selector는 점수 공식을 다시 계산하지 않고 `NPCDecision`을 action queue로 변환한다.
- provider가 가능한 구체 아이템을 노출하고, 실행 시 같은 `ItemId`를 사용한다.
- Eat/Drink의 랜덤 대체 실행이 제거되어 예측과 실행이 일치한다.
- 작업 비용은 `FarmingActionCost`에서 읽어 예측과 실행이 같은 값을 사용한다.
- 작업 횟수는 실제 `StatEffect`를 반복 적용해 계산하고 상한을 둔다.
- critical need 집합을 판단 시작 시점에 고정하고 단계별 fallback을 둔 정책은 복합 효과 아이템의 교착을 줄인다.
- `DefaultAction.Clear()`와 `FarmingAction.Clear()`가 풀 재사용 상태를 초기화한다.
- `DestinationDB`는 지연 초기화와 등록 키 중복 제거를 수행한다.
- `NPCDecisionTuning`은 private serialized field와 read-only property를 사용하고 필드 간 invariant를 `OnValidate()`에서 보정한다.
- `IdleAction`이 정상적인 무결정 상태의 프레임별 재판단 스핀을 막는다.
- `SampleScene`의 selector에 `NPCDecisionTuning.asset`이 연결되어 있다.
- `SampleScene`의 `InteractableManager`가 `DataManager`, Pub, Well provider를 참조한다.

## 심각도별 발견 사항

### Critical

없음.

### High

#### H-01: 모든 NPC가 의사결정 시 Farmer로 취급된다

- 위치: `Assets/Scripts/Actor/WorkerNPC.cs:14-19`, `Assets/Scripts/Actor/WorkerNPC.cs:33-38`, `Assets/Scripts/Manager/NPCManager.cs:19-28`
- 확인 내용: `NPCManager.CreateNPC(npcType)`은 타입별 selector를 고르지만 `WorkerNPC`는 받은 타입을 저장하지 않고 항상 `NPCType.Farmer`를 selector에 전달한다.
- 영향: Guard와 Cook도 Farmer 작업장을 고르고 Farming 정책을 사용한다. 현재 테스트 창은 모든 `NPCType`을 무작위 생성하므로 이미 노출 가능한 동작 오류다.
- 권장 조치: `WorkerNPC.Init`에 `NPCType`을 전달해 actor identity로 저장하거나, 직업별 decision profile 자체를 selector에 주입해 `WorkerNPC`가 enum을 전달할 필요를 없앤다.

#### H-02: action 수명주기가 실패와 취소를 안전하게 표현하지 못한다

- 위치: `Assets/Scripts/System/Action/DefaultAction.cs:14-23`, `Assets/Scripts/System/Action/DefaultAction.cs:61-65`, `Assets/Scripts/Actor/WorkerNPC.cs:22-45`
- 확인 내용:
  - `Init()`이 즉시 `Start()`를 호출해 큐에서 기다리는 action도 Running 상태가 된다.
  - runner는 `CheckComplete()`만 이해하며 성공, 실패, 취소를 구분하지 않는다.
  - `Stop()`은 Running을 끄지만 Complete로 만들지 않아 이후 tick도 진행도 되지 않는 정지 상태를 만든다.
  - provider 실행 실패도 성공과 같은 Complete로 처리된다.
- 영향: 예약, 인벤토리 소비, 애니메이션, 중단 우선순위가 붙는 순간 큐 정지와 자원 누수가 발생하기 쉽다.
- 권장 조치: `Init`은 데이터 설정만 하고 dequeue 시 `Start`한다. action 상태를 Ready/Running/Succeeded/Failed/Cancelled로 명시하고 runner 한 곳에서 active/queued action을 정리한다.

### Medium

#### M-01: 공용으로 보이는 결정 계층이 실제로는 Farmer 규칙을 안고 있다

- 위치: `Assets/Scripts/System/Lib/DestinationDecider.cs:478-488`, `Assets/Scripts/System/Actor/FarmerActionSelector.cs:115-128`
- 확인 내용: `DestinationDecider`가 `NPCType -> BuildingType.Farm`을 직접 매핑하고 work candidate의 action type을 Farming으로 고정한다. selector도 `NPCIntent.Work -> ActionType.Farming`을 다시 고정한다.
- 영향: Guard/Cook을 추가할 때 공용 decider와 각 selector를 함께 수정해야 한다. 직업 추가가 닫힌 확장이 되지 않는다.
- 권장 조치: 직업별 `WorkDefinition` 또는 decision profile에 작업장 키, action type, 1회 비용, 최소/최대 배치를 담아 decider에 입력한다. 당장 인터페이스 계층을 늘리기보다 두 번째 실제 직업을 구현할 때 추출하는 것이 적절하다.

#### M-02: selector가 잘못된 Work 결정을 다시 1회 작업으로 만든다

- 위치: `Assets/Data/Struct/NPCDecision.cs:16-22`, `Assets/Scripts/System/Actor/FarmerActionSelector.cs:67-74`
- 확인 내용: `NPCDecision`은 repeat invariant를 보장하지 않고 selector는 모든 intent에 `Mathf.Max(1, RepeatCount)`를 적용한다.
- 영향: 잘못 생성된 Work 결정의 0회가 1회로 복구되어 이번 리팩터링이 제거하려던 강제 단일 작업이 재발할 수 있다.
- 권장 조치: Work의 0 이하 repeat는 Idle 또는 invalid decision으로 거부하고, one-shot intent는 생성자/factory에서 1로 고정한다.

#### M-03: non-critical 선택에 Idle 기준점이 없다

- 위치: `Assets/Scripts/System/Lib/DestinationDecider.cs:90-105`, `Assets/Scripts/System/Lib/DestinationDecider.cs:283-299`
- 확인 내용: Work가 불가능하면 utility가 음수인 보급 후보도 무조건 최선 후보로 선택한다.
- 영향: 아무것도 하지 않는 편이 나은 상황에도 멀리 이동해 순손해 아이템을 소비할 수 있다.
- 권장 조치: non-critical 후보는 기본적으로 Idle utility 0과 비교한다. critical fallback은 생존을 위해 별도 정책으로 유지한다.

#### M-04: Worker 풀 반환 시 actor 런타임 상태가 초기화되지 않는다

- 위치: `Assets/Scripts/System/Lib/WorkerPool.cs:49-80`, `Assets/Scripts/Actor/WorkerNPC.cs:8-19`
- 확인 내용: `OnReleaseWorker`는 GameObject 비활성화와 부모 변경만 수행한다. `WorkerNPC`는 current action, queued actions, stat, selector를 그대로 보유한다.
- 영향: 향후 `ReleaseWorker()`를 사용하면 재획득된 NPC가 이전 NPC의 action context와 queue를 이어서 실행할 수 있다. 현재 release 호출처는 없어 잠재 결함이지만 풀 API를 사용하기 전에 반드시 막아야 한다.
- 권장 조치: `WorkerNPC.Clear/Reset`에서 active action과 queued actions를 모두 pool에 반환하고 runtime 참조를 비운 뒤 release한다.

#### M-05: `NPCManager`의 타입별 registry가 실제 worker를 기록하지 않는다

- 위치: `Assets/Scripts/Manager/NPCManager.cs:12-28`
- 확인 내용: 타입별 리스트는 생성하지만 `newWorker`를 리스트에 추가하지 않는다. selector 선택도 enum 정수와 리스트 인덱스가 같다는 전제에 묶여 있다.
- 영향: 조회, 해산, 저장, 타입별 명령 기능이 registry를 신뢰할 수 없고 enum 순서 변경이 씬 설정을 조용히 깨뜨린다.
- 권장 조치: 생성 직후 등록하고 release 시 제거한다. selector는 직렬화 가능한 `NPCType`-selector pair 또는 명시적 lookup으로 구성한다.

#### M-06: `NPCStat.ChangeMoveSpeed`는 양의 증가를 적용할 수 없다

- 위치: `Assets/Scripts/System/Actor/NPCStat.cs:75-83`
- 확인 내용: 새 값을 현재 `_moveSpeed` 자체를 최대값으로 clamp한다.
- 영향: 버프나 장비로 속도를 높일 수 없고 향후 이동시간 예측과 실제 이동 속도의 규칙이 어긋날 수 있다.
- 권장 조치: 별도 max speed 또는 base + modifier 모델을 두거나, 상한이 필요 없다면 하한만 clamp한다.

#### M-07: 핵심 의사결정 규칙에 자동 회귀 테스트가 없다

- 위치: `Assets/Scripts/System/Lib/DestinationDecider.cs`, `Assets/TestOnly/DecisionSmokeItemData.csv`
- 확인 내용: `.asmdef`, NUnit/UnityTest 선언, assertion 사용이 없다. 현재 검증은 컴파일뿐이다.
- 영향: 임계치 경계, 복합 효과, tie-break, 음수 utility, batch count의 작은 변경도 빌드를 통과한다.
- 권장 조치: 의사결정 코어를 Unity scene 객체 없이 테스트 가능한 입력 경계로 만든 뒤 표 기반 Edit Mode 테스트를 추가한다. 씬 배선은 작은 Play Mode smoke test로 별도 검증한다.

### Low

#### L-01: action 시간과 Idle 재판단 주기가 분산된 magic value다

- 위치: `EatAction`, `DrinkAction`, `FarmingAction`, `SleepAction`, `IdleAction`
- 확인 내용: 1f, 2f, 3f 등이 각 action 내부에 흩어져 있고 utility에는 실행 시간이 포함되지 않는다.
- 권장 조치: 실행과 예측이 함께 읽을 수 있는 action definition이 생길 때 duration을 옮긴다. 현재 단계에서는 적어도 named constant로 의도를 드러낸다.

#### L-02: 데이터와 매니저의 방어적 조립 검증이 약하다

- 위치: `Assets/Scripts/Manager/DataManager.cs:16-28`, `Assets/Scripts/Manager/InteractableManager.cs:10-18`
- 확인 내용: null `_costInfos`, null action cost, 중복 `ActionType`, null stat/item context를 명시적으로 검증하지 않는다. `DataManager.instance`는 쓰이지 않는 전역 표면이다.
- 권장 조치: `Awake`/`OnValidate`에서 필수 참조와 중복 키를 구체적인 오류로 검증하고 미사용 static instance는 제거한다.

#### L-03: CSV 파서는 실제 콘텐츠 확장에 취약하다

- 위치: `Assets/Scripts/System/Lib/CSVParser.cs:13-23`, `Assets/Scripts/System/Mapper/ItemInfoCsvMapper.cs:40-48`
- 확인 내용: 쉼표 split만 사용해 quoted comma를 처리하지 못하고, float parsing이 invariant culture를 명시하지 않는다. 중복 item ID도 검증하지 않는다.
- 권장 조치: 콘텐츠에 쉼표/인용부호가 필요해지는 시점에 검증된 CSV parser를 사용하고 ID uniqueness와 invariant numeric parsing을 적용한다.

#### L-04: `ProjectStructure.md`가 현재 코드와 크게 어긋난다

- 위치: `PublicMD/ProjectStructure.md`
- 확인 내용: `FarmerActionSelector`가 비어 있고 action이 대부분 stub이며 `WorkerNPC`가 tick delegate를 쓴다는 설명이 남아 있다. 현재 구현에는 decision, queue, provider, interaction manager가 존재한다.
- 영향: 새 작업자가 잘못된 책임 경계를 기준으로 구현할 가능성이 높다.
- 권장 조치: 런타임 배선 완료 후 현재 흐름과 확장 지점을 기준으로 문서를 갱신한다.

## 구조적 해석

### 지금 유지할 것

`DestinationDecider -> NPCDecision -> Selector -> IAction` 흐름은 유지할 가치가 있다. 특히 한 결정이 한 목적지와 한 의미 단계를 표현하고, 큐 소진 뒤 실제 상태로 다시 판단하는 방식은 현재 규모에서 GOAP보다 단순하고 디버깅하기 쉽다.

`IInteractionProvider`도 좋은 경계다. 판단 시 option을 열거하고 실행 시 concrete request를 검증하는 구조는 향후 재고, 가격, 좌석, 예약을 provider 쪽에 추가하기 쉽다.

### 다음에 추출할 것

두 번째 직업을 실제로 만들 때 `WorkDefinition`을 추출하는 것이 적절하다. 지금 당장 범용 프레임워크를 만들 필요는 없지만, Guard/Cook을 추가하면서 `DestinationDecider`의 switch와 `FarmerActionSelector` 복제를 늘리는 것은 피해야 한다.

action 상태 모델은 두 번째 직업보다 먼저 손보는 편이 좋다. 전투, 치료, 운반, 시설 예약은 실패와 취소가 정상 흐름이기 때문에 현재 bool completion 모델 위에 붙이면 모든 action마다 예외 처리가 퍼진다.

### 아직 만들지 말 것

- 완전한 GOAP 또는 행동 트리 재도입
- 구현체 하나뿐인 역할마다 인터페이스 추가
- 모든 action을 하나의 거대한 data asset으로 통합
- 같은 `BuildingType` 복수 시설 지원이 실제 요구되기 전의 복잡한 destination query 시스템

현재 문제는 추상화 부족보다 런타임 계약과 Unity Play Mode 검증의 미완성에 더 가깝다.

## 권장 작업 순서

1. 연결된 씬 배선으로 수동 Play Mode 스모크 테스트를 통과시킨다.
2. `WorkerNPC`에 실제 NPC identity를 전달하고 Guard/Cook이 Farmer로 판단되는 오류를 막는다.
3. Work repeat invariant와 non-critical Idle utility baseline을 닫는다.
4. action 상태를 명시적으로 만들고 dequeue 시 Start, cancel/cleanup 단일 경로를 확립한다.
5. Worker release/reset과 `NPCManager` registry를 완성한다.
6. 결정 코어의 Edit Mode 테스트를 추가한다.
7. 두 번째 직업 구현 시 `WorkDefinition`을 추출한다.
8. `ProjectStructure.md`를 실제 구조에 맞게 갱신한다.

## 최종 판정

현재 코드는 "방향을 다시 잡아야 하는 구조"가 아니다. 오히려 중심 설계는 보존하고 외곽 계약을 강화해야 하는 구조다. `DestinationDecider` 리팩터링은 예측과 실행의 불일치를 제대로 해결했고, 단일 결정 후 재계획도 현재 게임에 잘 맞는다.

씬 배선은 저장소에 반영되었지만 Play Mode 확인은 아직 필요하다. NPC type 전달과 action lifecycle 문제는 다음 시스템을 붙이기 전에 해결해야 한다. 이 부분을 닫으면 단순 프로토타입을 넘어 실제 기능을 안정적으로 쌓을 수 있는 기반이 된다.
