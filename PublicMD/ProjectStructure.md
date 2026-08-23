# Project Structure

> 문서 기준일: 2026-08-22
> 이 문서는 현재 저장소의 실제 파일과 runtime 역할을 설명한다.

## 1. 구조 한눈에 보기

```text
WorkerNPC                         actor root / action queue runner
  -> BaseNPCActionSelector       다음 queue 결정 및 구성
       -> DestinationDecider     역할·보급 utility 판단
       -> DestinationDB          목적지·상호작용 capability 조회
       -> ActionPool             IAction 대여·반환
  -> IAction                     선택된 행동 실행
       -> ActionContext          실행에 필요한 값과 capability
  -> NPCComponent                Unity 참조·이동·Guard 감지
  -> NPCStat                     개체별 runtime 상태
```

현재 프로젝트에는 `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph가 없다. 새 작업에서 이 과거 구조를 전제로 삼지 않는다.

## 2. 실제 파일 구조

```text
Assets/
  Data/
    CSV/
      ItemData.csv

    Class/
      CONST.cs

    ScriptableObject/
      DefaultStatContext.asset
      FarmingActionCost.asset
      GuardActionCost.asset
      GuardStatDefinition.asset
      ItemDataContext.asset
      NPCDecisionTuning.asset

      Script/
        DefaultActionCost.cs
        DefaultStatContext.cs
        FarmProductionDefinition.cs
        FarmingActionCost.cs
        GuardActionCost.cs
        GuardStatDefinition.cs
        ItemDataContext.cs
        NPCDecisionTuning.cs
        NPCStatDefinition.cs

    Struct/
      ActionContext.cs
      HoverInfo.cs
      InteractionOption.cs
      InteractionRequest.cs
      InteractionResult.cs
      ItemInfo.cs
      MoveRequest.cs
      NPCDecision.cs
      StatEffect.cs

  Prefab/
    NPCGirl.prefab

  Scenes/
    SampleScene.unity
    GuardTest.unity

  Scripts/
    Actor/
      BaseInteractionProvider.cs
      Enemy.cs
      Pub.cs
      WorkerNPC.cs

    Enum/
      ActionResult.cs
      ActionType.cs
      BuildingType.cs
      HoverType.cs
      ItemCategory.cs
      NPCIntent.cs
      NPCType.cs
      PopupType.cs

    Interface/
      IAction.cs
      ICombatStatView.cs
      ICombatTarget.cs
      IDataManager.cs
      IGuardStatView.cs
      IHoverInfoSource.cs
      IInteractionProvider.cs
      IInventory.cs
      IMoveTarget.cs
      IRandomSource.cs
      IStatView.cs
      IUIService.cs

    Manager/
      DataManager.cs
      InteractableManager.cs
      NPCManager.cs

    UI/
      HoverBase.cs
      PopBase.cs
      UIManager.cs

    System/
      Action/
        AttackAction.cs
        DefaultAction.cs
        DrinkAction.cs
        EatAction.cs
        FarmingAction.cs
        GuardAction.cs
        IdleAction.cs
        MoveAction.cs
        SleepAction.cs

      Actor/
        BaseNPCActionSelector.cs
        CombatTargetHandle.cs
        FarmerActionSelector.cs
        GuardActionSelector.cs
        GuardPerception.cs
        GuardRuntimeState.cs
        GuardStat.cs
        NPCComponent.cs
        NPCStat.cs
        ProximitySensor2D.cs

      Farming/
        FarmWorkPhase.cs
        FarmWorkSite.cs

      Inventory/
        WarehouseInventory.cs

      Lib/
        ActionPool.cs
        CombatRange.cs
        CSVParser.cs
        DestinationDB.cs
        DestinationDecider.cs
        SeededRandomSource.cs
        WorkerPool.cs

  TestOnly/
    TestDecisionScenarioProbe.cs
    TestEnemyRainSpawner.cs
    TestFarmProductionWindow.cs
    TestNPCSpawnWindow.cs
```

`Assets/_Recovery`는 Unity 복구 산출물이며 runtime 구조의 일부가 아니다. 현재 `.asmdef`가 없으므로 모든 C# 파일은 기본 `Assembly-CSharp`에 컴파일된다.

## 3. 폴더별 책임

### `Assets/Scripts/Actor`

씬에 직접 존재하는 actor 또는 상호작용 MonoBehaviour를 둔다.

| 파일 | 역할 |
|---|---|
| `WorkerNPC` | NPC actor root, action queue lifecycle의 단일 소유자 |
| `BaseInteractionProvider` | `IInteractionProvider`의 공통 template method base: idempotent 초기화, support/availability/request validation, concrete core로 dispatch |
| `Pub` | `ItemDataContext`를 직접 참조해 Eat/Drink option과 `StatEffect` 제공 |
| `Enemy` | 최소 전투 target 구현과 체력·사망 처리 |

`BaseInteractionProvider`는 domain 규칙을 모른다. item catalogue 책임은 `Pub`가, farm progress/yield 책임은 `Assets/Scripts/System/Farming/FarmWorkSite.cs`가 각자 소유한다.

### `Assets/Scripts/Manager`

씬 수준 조립과 registry 진입점을 둔다.

| 파일 | 역할 |
|---|---|
| `NPCManager` | `NPCType`별 selector + stat definition 조합, NPC spawn |
| `DataManager` | action cost dictionary 제공 |
| `InteractableManager` | scene의 `BaseInteractionProvider` 초기화·등록·`(GameObject, ActionType)` 조회 registry |

manager는 action 세부 실행이나 role utility 공식을 소유하지 않는다.

### `Assets/Scripts/System/Actor`

NPC runtime state, role selector, 감지 adapter를 둔다.

| 묶음 | 파일 | 역할 |
|---|---|---|
| stat | `NPCStat`, `GuardStat` | 개체별 mutable runtime 상태 |
| selector | `BaseNPCActionSelector`, `FarmerActionSelector`, `GuardActionSelector` | 다음 queue 결정과 구성 |
| Unity adapter | `NPCComponent` | 이동·방향·optional perception 접근 |
| perception | `ProximitySensor2D`, `GuardPerception` | 물리 감지와 combat target 변환 |
| target state | `GuardRuntimeState`, `CombatTargetHandle` | Guard별 현재 target과 Unity 생존성 |

### `Assets/Scripts/System/Action`

`IAction` 구현을 둔다. 각 action은 실행 흐름과 결과를 소유한다.

| Action | 주 책임 |
|---|---|
| `DefaultAction` | lifecycle와 `ActionResult` 공통 구현 |
| `MoveAction` | 고정 위치 또는 `IMoveTarget` 추적 이동 |
| `EatAction`, `DrinkAction` | provider request 실행 후 `StatEffect` 적용 |
| `SleepAction` | 일정 시간 뒤 fatigue 회복 |
| `FarmingAction` | 농경지 work 1회와 작업 비용 적용 |
| `GuardAction` | 장기 순찰, 욕구 증가, 감지/interrupt replan |
| `AttackAction` | 공격 주기, 범위, damage, target 상태 처리 |
| `IdleAction` | 짧은 안전 대기 |

### `Assets/Scripts/System/Lib`

두 개 이상의 흐름에서 사용하는 registry, pool, 계산 보조를 둔다.

| 파일 | 역할 |
|---|---|
| `ActionPool` | `ActionType`별 action factory와 재사용 queue |
| `WorkerPool` | `WorkerNPC` GameObject pool |
| `DestinationDB` | `BuildingType`별 scene destination Transform/GameObject 조회, provider 조회는 `InteractableManager`에 위임 |
| `DestinationDecider` | bounded look-ahead utility decision policy |
| `CombatRange` | 2D 거리 판정 |
| `SeededRandomSource` | seed 기반 결정적 난수 |
| `CSVParser` | 단순 CSV row parsing |

`Lib`는 잡다한 코드를 버리는 폴더가 아니다. 특정 도메인 상태나 한 action만 쓰는 규칙은 해당 도메인에 둔다.

### `Assets/Scripts/System/Farming`

농경지 runtime 상태를 둔다. `FarmWorkSite`는 `BaseInteractionProvider`를 상속해 공통 `IInteractionProvider` protocol로 노출되며, 별도의 domain 전용 provider interface는 없다.

| 파일 | 역할 |
|---|---|
| `FarmWorkSite` | `IInteractionProvider` 구현, Growing/Harvesting phase와 progress transaction |
| `FarmWorkPhase` | 농경지 phase 식별 |

work 결과는 별도 result 타입 없이 공통 `InteractionResult`(성공 시 `default`, farming에는 actor effect가 없음)로 표현한다. phase/progress는 `FarmWorkSite`의 read-only property로 조회한다.

### `Assets/Scripts/System/Inventory`

시설 inventory의 runtime 구현을 둔다. 현재는 용량 없는 `WarehouseInventory` 하나이며 item ID별 정수 수량을 보유한다.

### `Assets/Scripts/UI`

UI의 공통 진입점과 표시 lifecycle을 둔다.

| 파일 | 역할 |
|---|---|
| `UIManager` | 외부 `IUIService` facade, `PopupType`/`HoverType` routing, popup stack과 현재 hover 조정 |
| `PopBase` | popup 식별자와 open/close lifecycle hook |
| `HoverBase` | hover source 소유권, 주기적 정보 갱신, show/hide lifecycle hook |

`UIManager`는 개별 popup/hover마다 concrete field를 갖지 않고 `PopBase[]`/`HoverBase[]` registry를 dictionary로 변환한다. 호출자는 concrete view나 하위 controller를 탐색하지 않고 `IUIService.TryShow/TryHide`에 category enum과 필요한 source만 전달한다. concrete UI는 `PopBase` 또는 `HoverBase`를 상속해 실제 Text, gauge, animation 표현을 소유한다.

### `Assets/Scripts/Interface`

프로젝트 여러 영역이 공유하는 안정적인 계약만 둔다.

- 실행: `IAction`, `IInteractionProvider`
- read model: `IStatView`, `ICombatStatView`, `IGuardStatView`
- capability: `ICombatTarget`, `IMoveTarget`, `IInventory`, `IRandomSource`, `IHoverInfoSource`
- data service: `IDataManager`, `IUIService`

한 concrete class의 이름을 감추기 위한 일대일 interface는 이 폴더에 추가하지 않는다. 도메인 전용 계약은 실제 필요가 있으면 도메인 폴더에 두되, provider가 도메인마다 증식하지 않도록 먼저 공통 interaction protocol로 표현 가능한지 검토한다.

### `Assets/Data/Struct`

selector, action, provider 사이를 전달하는 작은 request/result/value object를 둔다.

- `ActionContext`: action 실행 dependency 묶음
- `HoverInfo`: hover view가 그릴 title/description/progress/anchor 값
- `NPCDecision`: decider의 단일 semantic decision
- `MoveRequest`: 고정/동적 이동 목표
- `InteractionRequest`, `InteractionResult`: Eat/Drink/Farming이 공유하는 공통 interaction request/result
- `InteractionOption`: utility 평가용 공급 option
- `StatEffect`: stat delta 묶음
- `ItemInfo`: CSV item row model

이 폴더의 타입은 행동 로직이나 scene lookup을 소유하지 않는다.

### `Assets/Data/ScriptableObject/Script`

공유 정의와 tuning의 C# 타입을 둔다.

| 종류 | 파일 |
|---|---|
| stat factory | `NPCStatDefinition`, `DefaultStatContext`, `GuardStatDefinition` |
| action cost/policy | `DefaultActionCost`, `FarmingActionCost`, `GuardActionCost` |
| decision tuning | `NPCDecisionTuning` |
| farming definition | `FarmProductionDefinition` |
| item table wrapper | `ItemDataContext` |

asset instance는 `Assets/Data/ScriptableObject`에 둔다. 공유 asset은 runtime 상태를 저장하지 않는다.

### `Assets/TestOnly`

Play Mode에서 수동 검증하기 위한 개발 도구를 둔다.

- `TestNPCSpawnWindow`: Farmer/Guard spawn
- `TestEnemyRainSpawner`: Guard 전투용 적 연속 생성
- `TestDecisionScenarioProbe`: utility decision 결정적 시나리오 probe
- `TestFarmProductionWindow`: farm gauge와 warehouse 직접 검증

이 코드는 production gameplay dependency가 되어서는 안 된다. 현재 assembly가 분리되지 않았으므로 build target 제외가 필요한 시점에 `.asmdef` 또는 Editor/Development conditional 정책을 별도로 도입한다.

## 4. Runtime 흐름

### 4.1 NPC 생성

```text
Test/UI command
  -> NPCManager.CreateNPC(NPCType)
  -> NPCCreationEntry 조회
  -> NPCStatDefinition.CreateRuntimeStat()
  -> selector.CanUseStat(stat)
  -> WorkerPool.GetWorker()
  -> WorkerNPC.Init(...)
```

새 role을 추가할 때 enum index로 selector와 stat을 따로 맞추지 않는다. `NPCCreationEntry` 한 row에 호환되는 두 참조를 함께 배선한다.

### 4.2 일반 queue 실행

```text
WorkerNPC.AdvanceQueue()
  -> selector.RequestNewActionQueue(stat, npcType, component)
  -> Move? -> semantic action(s)
  -> Start once
  -> Tick until result changes
```

`Completed`는 다음 action으로 진행하고, `ReplanRequested`와 `Failed`는 남은 queue를 폐기한 뒤 실제 stat/position으로 다시 판단한다.

### 4.3 Farmer

```text
FarmerActionSelector
  -> DestinationDecider.Decide(stat, Farmer, position, farmingCost)
  -> destination/provider 확인
  -> MoveAction (필요한 경우)
  -> Farming/Eat/Drink/Sleep/Idle action
```

Work 결정은 bounded repeat count를 가질 수 있다. 공급 행동은 한 번만 실행하고 queue 종료 후 실제 상태로 다시 판단한다.

### 4.4 Guard

```text
GuardActionSelector
  -> perception target 있으면 Move/Attack
  -> 아니면 DestinationDecider
       -> supply queue 또는 Guard queue

GuardAction.Tick
  -> needs 증가
  -> enemy 감지 시 ReplanRequested
  -> interrupt threshold 도달 시 ReplanRequested
  -> 원형 patrol point 이동
```

Sensor의 `CircleCollider2D`는 prefab scene data이고, 공격/순찰 능력치는 `GuardStat`이다.

### 4.5 상호작용

Eat, Drink, Farming이 모두 같은 흐름을 사용한다.

```text
DestinationDB.TryGetInteractionProvider(BuildingType, ActionType, out provider)
  -> InteractableManager.TryGetInteractionProvider(DestinationObject, ActionType, out provider)

공급(Eat/Drink):
  DestinationDecider -> provider.AppendOptions() -> item/effect 후보 평가
    -> NPCDecision(InteractionRequest)
    -> selector가 provider + request를 ActionContext에 주입
    -> EatAction/DrinkAction.TryInteract()

Farming:
  FarmerActionSelector가 InteractionRequest(Farming, strength: 1f)를 직접 구성
    -> ActionContext에 provider + request 주입
    -> FarmingAction.TryInteract()
```

새 Cook, Shop, Clinic 등을 추가할 때는 domain 전용 provider interface나 `DestinationDB.TryGetXxxProvider(...)`를 만들지 않고, `BaseInteractionProvider`를 상속한 concrete provider를 `InteractableManager._interactables`에 등록하는 같은 경로를 따른다.

## 5. 새 기능을 어디에 추가하는가

### 새 NPC action

1. `ActionType`에 값을 추가한다.
2. `System/Action`에 `DefaultAction` 기반 구현을 만든다.
3. `ActionPool.Create` factory case를 추가한다.
4. selector가 명시적 `ActionContext`를 구성한다.
5. `Clear()`의 pooled-state reset과 모든 결과 경로를 검증한다.

### 새 role

1. 실제 전용 stat이 있을 때만 `NPCStat` subclass와 view interface를 만든다.
2. `BaseNPCActionSelector` subclass를 추가한다.
3. `NPCManager._creationEntries`에 role/selector/stat definition 한 row를 배선한다.
4. 기존 actor root와 action runner는 수정하지 않는 것을 우선한다.

### 새 destination

1. 필요한 `BuildingType`을 추가한다.
2. 씬 `DestinationDB` row에 Transform과 provider를 연결한다.
3. provider가 지원하는 action을 공통 interaction protocol로 노출한다.
4. selector/action에 도메인별 scene lookup을 직접 추가하지 않는다.

### 새 facility runtime state

게이지, 재고, 예약처럼 배치마다 다른 상태는 해당 scene component가 소유한다. ScriptableObject에는 초기값과 규칙만 둔다. NPC stat 또는 selector field에 시설 상태를 캐시하지 않는다.

### 새 gameplay 난수

`IRandomSource`를 주입한다. seed와 호출 순서를 재현 가능하게 유지한다. `UnityEngine.Random`은 TestOnly 시각 도구 외 production 규칙에서 사용하지 않는다.

## 6. 씬과 프리팹 배선

### `NPCGirl.prefab`

현재 공유 NPC actor prefab이다.

- `WorkerNPC`
- `NPCComponent`
- `GuardPerception`
- Sensor child의 `ProximitySensor2D`와 `CircleCollider2D(radius 3)`
- visual child

Farmer도 같은 prefab을 사용하므로 Guard 전용 컴포넌트는 optional 경계로 취급한다. Guard selector는 호환 stat과 perception을 검증해야 하며 Farmer action은 이를 알지 않는다.

### `SampleScene.unity`

기본 Farmer 실행용 manager, pool, destination, interactable 배선을 가진다. 기능 구현 후 실제 scene reference가 연결되어 있는지는 별도로 확인한다.

### `GuardTest.unity`

Farmer/Guard selector, 적, test spawner, GuardPost/PatrolArea를 포함한 통합 검증 씬이다. `PatrolArea`의 시각 오브젝트가 존재하는 것과 bounds 기반 순찰 기능이 구현된 것은 서로 다른 사실이다. 현재 bounds 기반 순찰은 미구현이다.

## 7. 현재 과도기와 주의점

- action runtime duration과 utility prediction duration이 중복 정의되어 drift 가능성이 있다.
- `DataManager.instance`는 남아 있는 전역 상태다.
- `.asmdef`와 자동 테스트 assembly가 없다.
- `CONST.cs`는 현재 의미 있는 도메인 책임이 없는 placeholder다. 새 상수를 넣는 공용 쓰레기통으로 사용하지 않는다.
- Sleep/Inn은 아직 `IInteractionProvider` 기반이 아니다. `DestinationDecider.AddSupplyCandidates`에 `BuildingType.Inn` 특수 분기가 남아 있다.

이 항목들은 현재 구현 사실을 설명하는 것이며 새 코드가 그대로 복제해야 할 패턴은 아니다.

## 8. 문서 갱신 규칙

구조 변경 작업이 끝나면 최소한 다음을 함께 확인한다.

1. 이 문서의 실제 파일 tree와 주요 runtime flow가 맞는가.
2. `ARCHITECTURE.md`의 책임/의존 방향이 새 코드와 맞는가.
3. `CodeConvention.md`가 새 패턴을 허용하거나 금지하는 이유를 설명하는가.
4. 미구현 기능을 현재 구현처럼 기록하지 않았는가.
5. 과도기 코드를 영구 표준처럼 기록하지 않았는가.
