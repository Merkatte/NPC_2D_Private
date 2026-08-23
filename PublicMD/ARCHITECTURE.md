# NPC Work 2D Architecture

> 문서 기준일: 2026-08-22
> 대상 버전: Unity 6000.3.9f1, URP 2D
> 현재 실행 씬: `Assets/Scenes/SampleScene.unity`, `Assets/Scenes/GuardTest.unity`

## 1. 문서 목적

이 문서는 현재 저장소에 실제로 존재하는 NPC 프로토타입의 런타임 구조와 책임 경계를 정의한다. 장기 게임 기획은 `Game_Plan.md`, 작업 우선순위와 완료 조건은 `PLAN.md`, 실제 파일 위치는 `ProjectStructure.md`, 작성 규칙은 `CodeConvention.md`를 따른다.

과거 프로젝트에 존재했던 `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph, 모집·상인·치료·대규모 물류 시스템은 현재 런타임에 존재하지 않는다. 해당 이름을 현재 구조의 일부로 간주하지 않는다.

이 문서에서는 사실의 수준을 다음처럼 구분한다.

- **현재 구현**: 현재 C# 코드나 씬에 존재하고 실행되는 구조
- **과도기 구현**: 작동하지만 다음 통합 작업에서 교체하기로 한 구조
- **확정 방향**: 새 코드를 추가할 때 지켜야 할 목표 경계
- **미구현**: 기획 또는 후속 과제이며 현재 API로 가정하면 안 되는 기능

## 2. 현재 아키텍처 요약

현재 NPC 실행의 중심 흐름은 다음과 같다.

```text
NPCManager
  -> NPCType별 NPCCreationEntry 선택
  -> NPCStatDefinition.CreateRuntimeStat()
  -> WorkerPool에서 WorkerNPC 획득
  -> WorkerNPC.Init(npcType, stat, selector)

WorkerNPC.Update()
  -> selector.RequestNewActionQueue(...)
  -> Queue<IAction>에서 action 하나 시작
  -> action.Tick()
  -> ActionResult에 따라 다음 action / 전체 replan / 실패 처리

BaseNPCActionSelector
  -> DestinationDecider 또는 전투 상태로 intent 결정
  -> ActionPool에서 action을 빌림
  -> ActionContext를 주입
  -> 실행할 Queue<IAction> 반환
```

핵심 책임은 다음과 같이 나뉜다.

| 영역 | 현재 소유자 | 책임 |
|---|---|---|
| actor lifecycle | `WorkerNPC` | action queue 실행, 결과 처리, replan, 풀 반환 |
| Unity 참조 | `NPCComponent` | Transform, 이동, 방향 전환, Guard 감지 컴포넌트 |
| 개체별 상태 | `NPCStat`, `GuardStat` | 체력, 이동 속도, 욕구, Guard 전투 능력치 |
| 행동 결정 | selector, `DestinationDecider` | 전투 우선순위, 역할/보급 intent, 이동 목적지, 반복 횟수 |
| 행동 실행 | `IAction` 구현 | 이동·섭취·수면·농사·순찰·공격의 실행과 종료 규칙 |
| 씬 목적지 | `DestinationDB` | `BuildingType`에서 위치와 상호작용 제공자 조회 |
| 공유 정의 | ScriptableObject | 초기 stat, action 비용, utility tuning, 생산 규칙 |
| 런타임 시설 상태 | scene component | 농경지 게이지, 창고 수량, 감지 범위 등 인스턴스 상태 |

## 3. 핵심 설계 원칙

### 3.1 결정과 실행을 분리한다

selector와 `DestinationDecider`는 무엇을 할지 결정한다. `IAction`은 선택된 행동을 실행한다. `WorkerNPC`는 둘을 연결하고 queue lifecycle만 관리한다.

- selector는 위치를 직접 이동시키거나 stat을 직접 변경하지 않는다.
- action은 다음 직업 행동을 선택하지 않는다.
- `WorkerNPC`는 Farmer/Guard의 세부 우선순위를 알지 않는다.
- 시설은 NPC의 다음 행동을 결정하지 않고, 요청받은 상호작용의 도메인 규칙만 실행한다.

### 3.2 정의와 런타임 상태를 분리한다

ScriptableObject는 공유 정의와 튜닝이다. 개체별 또는 씬 인스턴스별로 달라지는 값은 ScriptableObject에 저장하지 않는다.

```text
NPCStatDefinition asset -> NPCStat / GuardStat runtime instance
DefaultActionCost asset  -> 여러 action이 읽는 공유 비용·판정 값
NPCDecisionTuning asset  -> utility 계산용 공유 가중치
FarmProductionDefinition -> FarmWorkSite가 읽는 생산 규칙
```

예시는 다음과 같다.

- Guard의 공격력·공격속도·공격범위·순찰반경은 `GuardStat`의 개체별 값이다.
- 순찰 도착 거리, 욕구 증가율, interrupt threshold는 `GuardActionCost`의 공유 정책 값이다.
- 농경지의 현재 phase/progress는 `FarmWorkSite`의 씬 인스턴스 상태다.
- 창고 수량은 `WarehouseInventory`의 런타임 상태다.

런타임 코드가 ScriptableObject 필드를 변경해서는 안 된다.

### 3.3 공통 계약은 의미가 같을 때만 공유한다

범용화의 목적은 이름을 하나로 합치는 것이 아니라, 호출자가 동일한 프로토콜로 서로 다른 구현을 안전하게 실행하도록 만드는 것이다.

- `IStatView`는 모든 NPC가 실제로 공유하는 stat 조회만 가진다.
- `ICombatStatView`는 전투 능력치만 가진 독립 계약이다.
- `IGuardStatView`는 `ICombatStatView`를 확장한다.
- 런타임 객체는 `GuardStat : NPCStat, IGuardStatView` 하나이며 stat 객체를 세 개 만들지 않는다.
- 새 인터페이스는 실제 소비자나 두 번째 구현이 있을 때 추가한다.
- 도메인마다 `IFarmProvider`, `ICookProvider`, `IWhateverProvider`를 계속 만드는 구조는 기본 방향이 아니다.

### 3.4 Unity 오브젝트와 순수 규칙을 분리한다

MonoBehaviour는 씬 참조, lifecycle, 물리, Transform처럼 Unity가 소유하는 책임을 담당한다. 계산과 런타임 상태는 가능한 한 plain C# 타입에 둔다.

- `WorkerNPC`, `NPCComponent`, selector, manager, provider는 현재 MonoBehaviour다.
- `NPCStat`, `GuardStat`, `DestinationDecider`, action 구현은 plain C# 객체다.
- Transform이나 Collider 판정은 stat 객체가 소유하지 않는다.
- 전투 거리 계산은 `CombatRange` utility가 담당한다.

## 4. 생성과 조립

### 4.1 NPC 생성

`NPCManager`는 `NPCType`을 key로 사용하는 `NPCCreationEntry` 목록을 Inspector에서 받는다. 한 row가 selector와 stat definition을 함께 보유하므로 잘못된 역할 조합을 구조적으로 줄인다.

```text
Farmer -> FarmerActionSelector + DefaultStatContext
Guard  -> GuardActionSelector  + GuardStatDefinition
Cook   -> 현재 등록 정책에 따라 selector + DefaultStatContext
```

생성 과정은 다음 순서를 따른다.

1. `NPCType`에 맞는 entry를 조회한다.
2. `NPCStatDefinition.CreateRuntimeStat()`으로 새 runtime stat을 만든다.
3. selector의 `CanUseStat`으로 역할 호환성을 확인한다.
4. `WorkerPool`에서 actor를 가져온다.
5. `WorkerNPC.Init`으로 type, stat, selector를 주입한다.

직업은 현재 생성 후 고정이다. runtime 전직과 stat migration은 미구현이다.

### 4.2 씬 서비스와 registry

현재 씬에는 다음 조립 요소가 있다.

- `ActionPool`: `ActionType`별 plain C# action 재사용
- `WorkerPool`: `NPCGirl.prefab` 기반 `WorkerNPC` 재사용
- `DataManager`: action cost 제공
- `DestinationDB`: 건물별 위치 조회, provider 조회는 `InteractableManager`에 위임
- `NPCManager`: NPC 생성과 role composition
- `InteractableManager`: scene의 `BaseInteractionProvider` 초기화·등록·`(GameObject, ActionType)` 조회 registry

`DataManager.instance`는 현재 존재하는 전역 접근점이지만 권장되는 새 의존성 전달 방식은 아니다. 새 코드는 가능하면 serialized reference, 초기화 인자 또는 명시적 context를 사용한다.

## 5. Action 실행 모델

### 5.1 Action lifecycle

`IAction`의 lifecycle은 다음과 같다.

```text
Init(ActionContext)
  -> Start()
  -> Tick() 반복
  -> Result가 Running이 아니면 WorkerNPC가 처리
  -> Stop() (취소·replan 때)
  -> ActionPool.ReturnAction()
  -> Clear()
```

`DefaultAction`은 공통 result와 lifecycle 보조 메서드를 제공한다.

- `Completed`: 현재 action만 반환하고 queue의 다음 action을 실행한다.
- `ReplanRequested`: 현재 action과 남은 queue를 모두 취소·반환하고 새 queue를 요청한다.
- `Failed`: 현재 action과 남은 queue를 모두 정리한 뒤 새 계획을 요청한다.
- `Running`: 계속 Tick한다.

`Clear()`는 풀링된 인스턴스의 context, timer, cached interface, target 등 모든 실행 상태를 초기화해야 한다.

### 5.2 Queue 소유권

`WorkerNPC`만 active queue와 current action을 소유한다. selector는 완성된 queue를 반환한 뒤 그 lifecycle을 소유하지 않는다. action은 queue를 직접 교체하지 않고 `ReplanRequested`로 의사를 표현한다.

selector가 action을 빌리는 중 실패하면 이미 빌린 action을 모두 반환해야 한다. 부분적으로 구성된 queue를 실행해서는 안 된다.

### 5.3 ActionContext

`ActionContext`는 해당 action 실행에 필요한 actor 참조와 request/capability를 전달하는 값이다.

현재 공통 값은 다음과 같다.

- `NPCComponent`, `NPCStat`
- 고정 destination 또는 `MoveRequest`
- `DefaultActionCost`
- `IInteractionProvider`, `InteractionRequest`

Eat/Drink/Farming 모두 같은 `IInteractionProvider` + `InteractionRequest` 경로 하나만 사용한다. domain별 provider 필드는 없다. `ActionContext`에 역할별 nullable provider를 계속 추가하면 service locator와 유사한 dependency bag이 되므로 금지한다.

## 6. 판단 모델

### 6.1 Selector의 역할

`BaseNPCActionSelector`는 action 대여/반환과 공통 queue 보조 로직을 제공한다. 역할 selector가 우선순위와 queue 구성을 담당한다.

`FarmerActionSelector`의 현재 흐름:

1. `DestinationDecider.Decide`로 Work/Eat/Drink/Sleep/Idle 중 하나를 얻는다.
2. 목적지가 있으면 Move를 앞에 둔다.
3. 결정의 `RepeatCount`만큼 semantic action을 queue에 넣는다.
4. 농사 provider가 유효하지 않으면 현재는 Idle로 낮춘다.

`GuardActionSelector`의 현재 흐름:

1. 유효한 감지 대상이 있으면 combat queue를 최우선으로 만든다.
2. 그렇지 않으면 매 replan마다 utility decider를 호출한다.
3. 보급 intent면 Move + 공급 action을 만든다.
4. 높은 욕구인데 공급 행동을 만들 수 없으면 짧은 Idle로 replan spin을 막는다.
5. 그 외에는 Guard queue를 만든다.

### 6.2 DestinationDecider

`DestinationDecider`는 stat과 현재 위치, 역할별 work cost를 입력받아 `NPCDecision` 하나를 반환하는 plain C# 정책 객체다. action을 만들거나 stat을 변경하지 않는다.

현재 모델은 다음 특성을 가진다.

- Idle, 공급, FarmerWork, GuardDuty 후보를 같은 점수 공간에서 비교
- 비선형 need/health 위험 곡선
- 이동 시간, 행동 시간, 위험 노출, 역할 보상 반영
- 깊이 1~3의 bounded look-ahead
- critical need에서 역할 행동을 막는 안전 tier
- 점수 동률 시 고정 순서로 결정하는 deterministic tie-break
- 한 번의 공급 행동만 반환하고 실제 stat과 위치로 다시 판단

예측된 미래 행동을 미리 queue에 넣지 않는다. 예측 시간은 runtime action 시간의 권위 있는 값이 아니라 utility 계산용 추정치다.

현재 `NPCIntent`에는 Guard 전용 intent가 없다. decider의 plain Idle과 GuardDuty가 모두 비-공급 결과로 selector에 전달되어 동일한 Guard queue로 매핑될 수 있다. 이는 현재 알려진 의미 표현 한계이며, 문서상 의도적인 영구 계약으로 확대하지 않는다.

## 7. Stat 구조

```text
IStatView
  <- NPCStat
       <- GuardStat implements IGuardStatView

ICombatStatView
  <- IGuardStatView
```

`NPCStat`은 모든 NPC가 실제로 가지는 체력, 이동 속도, 피로, 배고픔, 갈증을 소유하고 자체 invariant를 clamp한다. `GuardStat`은 전투 능력치와 순찰 반경을 추가한다.

`ICombatStatView`가 `IStatView`를 상속하지 않는 이유는 공격 실행 코드가 생활 욕구와 체력을 필요로 하지 않기 때문이다. 인터페이스 상속은 객체의 계보가 아니라 소비자가 요구하는 계약을 기준으로 결정한다.

role별 stat subclass는 실제 role 데이터가 생길 때 만든다. 아직 전용 능력치가 없는 Farmer를 위해 빈 `FarmStat`을 만들지 않는다.

## 8. 목적지와 상호작용

### 8.1 현재 구현

`IInteractionProvider`는 Eat, Drink, Farming이 공유하는 공통 실행 프로토콜이다.

```text
Supports(ActionType)
CanInteract(ActionType)
AppendOptions(ActionType, buffer)
TryInteract(InteractionRequest, out InteractionResult)
```

- `Supports`는 구조적 capability(초기화 상태와 무관), `CanInteract`는 `Supports && 현재 operational` 의미다.
- `InteractionRequest`는 `ActionType + OptionId(기본값 없음, item ID 등 안정적 식별자) + Strength(실행 강도, 기본 1)`를 가진다.
- `InteractionResult`는 `ActorEffect` 하나만 노출한다. 성공 여부는 `TryInteract`의 반환값 하나로만 표현하고, `InteractionResult.Success` 같은 별도 필드는 없다.
- 농경지 게이지, item catalogue 같은 도메인 상태는 `InteractionResult`에 담기지 않고 concrete provider(`FarmWorkSite`, `Pub`)가 자신의 read-only property로 노출한다.

`BaseInteractionProvider`(`Assets/Scripts/Actor/BaseInteractionProvider.cs`)가 idempotent 초기화 lifecycle, `Supports`/`CanInteract`/`AppendOptions`/`TryInteract`의 공통 validation과 실패 처리, protected core method로의 dispatch를 소유한다. `Pub`와 `FarmWorkSite`는 이 base를 상속해 각자 `SupportsCore`/`TryInitializeCore`/`AppendOptionsCore`/`TryInteractCore`만 구현한다.

### 8.2 Provider 조회 경로

```text
DestinationDB.TryGetInteractionProvider(BuildingType, ActionType, out IInteractionProvider)
  -> BuildingType으로 DestinationInfo.DestinationObject를 찾는다
  -> InteractableManager.TryGetInteractionProvider(DestinationObject, ActionType, out provider)로 위임
```

`InteractableManager`는 `BaseInteractionProvider[] _interactables`를 명시적 등록 목록으로 갖고, `(GameObject owner, ActionType)` -> provider component cache를 소유한다. 초기화 시 각 provider의 `TryInitialize`를 한 번 호출하고, 안정적인 `Supports(type)`로 cache key를 구성한다. 조회 성공 조건은 cache entry 존재 + backing component가 destroyed되지 않음 + `CanInteract(type)`이 현재 true. 같은 `(GameObject, ActionType)`에 provider가 둘 이상 등록되면 오류를 한 번 로그하고 `_interactables` 배열에서 먼저 나온 provider만 유지한다.

`DestinationDB`는 `BuildingType -> DestinationObject` 매핑만 소유하며 provider component를 직접 scan하거나 cache하지 않는다. domain별 provider dictionary(`TryGetFarmWorkProvider` 같은)는 없다. 한 destination `GameObject`에 서로 다른 `ActionType`을 지원하는 여러 provider component가 함께 존재할 수 있다(Pub는 Eat과 Drink를 한 component로 함께 지원).

## 9. Guard 전투와 감지

Guard 전투는 다음 책임으로 분리된다.

```text
CircleCollider2D trigger
  -> ProximitySensor2D: layer 기반 collider 감지
  -> GuardPerception: ICombatTarget 변환·중복 제거
  -> GuardActionSelector: 현재 후보 중 target 선택
  -> GuardRuntimeState: per-NPC target handle 보관
  -> MoveAction(dynamic target) / AttackAction 실행
```

- `GuardPerception`은 대상을 선택하지 않는다.
- selector가 전투 우선순위와 target 선택을 담당한다.
- `CombatTargetHandle`은 interface와 Unity object 생존 확인을 함께 보존한다.
- `AttackAction`은 `ICombatStatView`만 요구한다.
- 공격 범위를 벗어나면 target을 버리지 않고 replan하여 추격 queue를 다시 만든다.
- 현재 감지 범위는 `NPCGirl.prefab`의 Sensor `CircleCollider2D.radius`에 저장되어 있으며 stat과 동기화되지 않는다.

`PatrolArea` GameObject는 `GuardTest.unity`에 존재하지만 사각 영역 순찰 데이터 전달은 아직 구현되지 않았다. 현재 `GuardAction`은 `GuardStat.GuardRadius`로 계산한 결정적 원형 순찰점을 돈다.

## 10. 농경지와 창고

농경지 기능은 현재 작업 중인 vertical slice다.

```text
FarmingAction 완료
  -> IInteractionProvider.TryInteract(InteractionRequest(Farming, strength: 1f))
  -> FarmWorkSite
       Growing: progress 증가, max에서 Harvesting 전환
       Harvesting: 수확량 생성 -> IInventory.TryAdd -> 성공 시 progress 감소
  -> WarehouseInventory 수량 증가
```

`FarmWorkSite`는 `BaseInteractionProvider`를 상속해 `ActionType.Farming`만 지원하는 `IInteractionProvider`다. `AppendOptions`는 base의 기본 empty 구현을 그대로 쓴다(Farming은 선택 가능한 option이 없는 interaction). `Strength`는 현재 worker efficiency로 사용되며 항상 1이다(향후 Farmer 숙련도 seam).

현재 invariant:

- 완료한 Farming action 하나가 work 한 번을 적용한다.
- Growing에서 max에 도달한 같은 work가 수확까지 동시에 수행하지 않는다.
- 창고가 전체 수량을 받지 못하면 수확 게이지를 줄이지 않는다.
- 생산 정의는 `FarmProductionDefinition`, runtime phase/progress는 `FarmWorkSite`가 소유한다.
- 난수는 `SeededRandomSource`를 통해 결정적으로 생성한다.

현재 직접 farm→warehouse 적재는 운반 시스템 전의 임시 수직 슬라이스다. 운반·용량·예약·저장/불러오기·숙련도 반영은 미구현이다.

## 10.5 UI 기본 라우팅

현재 UI는 실제 화면 제작 전의 공통 routing/lifecycle 기반만 구현되어 있다.

```text
caller
  -> IUIService.TryShow(category enum, optional source)
  -> UIManager
       PopupType -> PopBase registry -> popup open/close stack
       HoverType + IHoverInfoSource -> HoverBase registry -> current hover
```

`UIManager`는 외부 facade이자 popup/hover category coordinator다. 개별 화면의 Text, gauge, animation, domain formatting은 알지 않으며 `PopBase[]`와 `HoverBase[]` 두 serialized registry만 dictionary로 변환한다. `PopBase`는 popup open/close hook을, `HoverBase`는 현재 source의 소유권과 주기적 `HoverInfo` 갱신을 소유한다. 다른 source의 종료 요청이 현재 hover를 닫지 않도록 source identity를 확인한다.

게임 도메인 component는 concrete UI view나 `UIManager`를 직접 참조하지 않는다. hover 입력 adapter가 `IHoverInfoSource`를 제공하고 `IUIService`에 표시 의도를 전달한다. 현재 concrete popup, farm hover view, pointer/raycast adapter와 scene wiring은 미구현이다.

## 11. 난수와 재현성

게임플레이 결과에 영향을 주는 난수는 `UnityEngine.Random`을 직접 사용하지 않고 `IRandomSource`를 통해 공급한다. 현재 `SeededRandomSource`는 `System.Random`과 serialized seed를 사용한다.

`DestinationDecider`는 난수를 사용하지 않고 고정 tie-break를 적용한다. 같은 stat, 위치, scene data, tuning에는 같은 결정을 반환해야 한다.

`Assets/TestOnly/TestEnemyRainSpawner.cs`는 개발 전용 시각 테스트 도구이므로 `UnityEngine.Random` 사용 예외다. 이 예외를 production gameplay 코드로 복사하지 않는다.

## 12. 의존 방향

허용되는 주 의존 방향은 다음과 같다.

```text
Manager / Scene composition
  -> selector / pool / definition / scene provider

WorkerNPC
  -> selector + IAction + NPCComponent + NPCStat

selector
  -> decider + destination lookup + action pool + request/context

action
  -> ActionContext 안의 명시적 capability + stat/component

provider
  -> 자신의 definition + 자신의 runtime state + 좁은 외부 capability

plain runtime/data types
  -X-> scene manager lookup
```

금지하는 방향:

- `NPCStat`이 selector, destination, manager 또는 Transform을 참조
- action이 `Find*`, singleton, scene registry로 의존성을 직접 찾음
- provider가 selector나 `WorkerNPC`의 다음 계획을 결정
- `DestinationDecider`가 action을 생성하거나 runtime stat을 변경
- `ActionContext`에 모든 서비스와 모든 역할 전용 provider를 누적
- shared ScriptableObject에 per-NPC 또는 per-facility runtime 값을 기록

## 13. 실패와 안전성

- 필수 serialized reference가 없으면 `Awake`/`Start` 경계에서 원인을 한 번 명확히 로그한다.
- action 시작 전제 실패는 `Failed`로 종료하고 stat 또는 시설 상태를 부분 변경하지 않는다.
- 재판단이 필요한 정상 상황은 `ReplanRequested`를 사용한다.
- 시설 transaction은 외부 저장 성공과 내부 상태 변경 순서를 명시해야 한다.
- pooled action은 성공·실패·중단 경로 모두에서 반환 가능해야 한다.
- Unity object를 interface로 보관할 때 destroyed-object의 fake null을 고려한다.
- fallback은 crash와 frame-by-frame replan spin을 막아야 하며, 조용히 잘못된 역할 행동을 수행하게 해서는 안 된다.

## 14. 현재 씬과 검증 경계

`SampleScene.unity`는 Farmer 중심 기본 실행 환경이고, `GuardTest.unity`는 Guard selector, 적, PatrolArea, 전투/utility 테스트 도구를 포함한다. `NPCGirl.prefab`은 Farmer와 Guard가 공유하는 actor prefab이며 optional Guard 감지 구성을 포함한다.

프로젝트에는 `.asmdef`가 없다. 따라서 모든 runtime/TestOnly 스크립트가 기본 `Assembly-CSharp`에 들어간다. `TestDecisionScenarioProbe`, `TestFarmProductionWindow`, `TestNPCSpawnWindow`, `TestEnemyRainSpawner`는 자동화된 Unity Test Framework 테스트가 아니라 개발용 Play Mode 도구다.

문서나 command-line build 통과만으로 다음을 검증했다고 주장하지 않는다.

- scene serialized reference의 실제 연결
- collider/layer/trigger 설정
- Play Mode에서 queue와 animation/이동의 시간 흐름
- pooling 후 두 번째 spawn의 runtime reset
- 실제 농경지와 창고의 반복 cycle

## 15. 알려진 구조 부채와 다음 경계

우선순위가 높은 구조 부채:

1. action 내부 하드코딩 지속시간과 `NPCDecisionTuning`의 예측 시간을 단일 정의로 맞춘다.
2. GuardDuty와 plain Idle을 `NPCDecision`에서 구별할 필요가 있는지 결정한다.
3. 감지 범위와 향후 성장 stat의 동기화 책임을 정한다.
4. `DataManager.instance` 전역 접근을 명시적 조립으로 점진적으로 대체한다.
5. production 코드와 `TestOnly` 코드를 assembly definition으로 격리할지 결정한다.
6. `Pub`(및 향후 Cook/Shop 등)이 `[SerializeField] ItemDataContext`를 각자 직접 참조하는 현재 방식은 시설마다 Inspector에 같은 asset을 반복 배선해야 하고, 서로 다른 asset을 잘못 물리는 참조 drift를 구조적으로 막지 못한다. 매니저 주입으로 되돌리는 것은 `InteractableManager`의 domain 무지 원칙을 다시 깨므로 피한다. 검토할 대안: (a) `Reset()`에서 `AssetDatabase.FindAssets`로 프로젝트에 하나뿐인 asset을 자동 채우는 editor-only 편의 기능(명시적 `SerializeField`는 유지, override 가능), (b) `ItemDataContext`에 static self-reference singleton을 둬 `SerializeField` 자체를 없애는 방법(단, `DataManager.instance`와 같은 전역 접근점 계열이므로 이 asset이 앞으로도 프로젝트에 항상 정확히 하나임이 보장될 때만 고려). 2026-08-22 사용자 논의, 아직 미결정·미구현.

`IInteractionProvider`/`IFarmWorkProvider` 통합(Eat/Drink/Farming 공통 프로토콜)과 `BaseInteractable`의 item table 책임 분리는 2026-08-22 `PublicMD/InteractionProvider_Unification_Plan.md` 구현으로 완료되었다.

아직 구현되지 않은 모집, 상인, 치료, 정식 물류, 직업 성장, 저장/불러오기와 concrete UI 화면은 `Game_Plan.md`와 `PLAN.md`에서 관리한다. 해당 시스템이 생기기 전에는 이 문서에 가상의 class/API를 현재 구조처럼 기록하지 않는다.
