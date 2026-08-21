# IInteractionProvider Common Protocol Unification Plan

> 작성일: 2026-08-22
> 상태: 구현 전 사용자 검토·승인 대기
> 구현 대상: 현재 공급 전용 `IInteractionProvider`와 농사 전용 `IFarmWorkProvider`의 단일 실행 프로토콜 통합
> 구현 도구: Claude Code Opus Plan 검토 후, 사용자 승인 시 Sonnet 구현

## 1. 배경

현재 프로젝트에는 이름은 범용적이지만 실제로는 Eat/Drink에 맞춰진 `IInteractionProvider`가 있다.

```csharp
bool CanInteract(ActionType type);
void AppendOptions(ActionType type, List<InteractionOption> buffer);
bool TryInteraction(InteractRequest request, out InteractResult result);
```

현재 request/result도 공급 행동에 맞춰져 있다.

- `InteractRequest`: `ActionType + ItemId`
- `InteractResult`: `Success + StatEffect`
- `InteractionOption`: `ActionType + ItemId + StatEffect`

농경지 production slice는 이 계약으로 phase, progress, worker efficiency, 수확 transaction을 표현하기 어려워 다음 별도 경로를 추가했다.

```text
IFarmWorkProvider
ActionContext.FarmWorkProvider
DestinationDB.TryGetFarmWorkProvider(...)
FarmingAction -> IFarmWorkProvider.TryApplyWork(...)
```

이 구조를 그대로 확장하면 `ICookProvider`, `IShopProvider`, `IClinicProvider`와 domain별 `ActionContext` field 및 `DestinationDB.TryGet...`가 계속 늘어날 가능성이 높다.

이번 작업의 목적은 모든 domain 상태를 한 interface에 넣는 것이 아니다. 서로 다른 시설을 동일한 **지원 여부 조회 → option 열거 → 명시적 request 실행 → 공통 result 반환** 절차로 호출할 수 있게 만드는 것이다.

## 2. 현재 코드에서 확인한 전제

### 2.1 실제 호출부

`IInteractionProvider`는 현재 다음 코드에서 사용된다.

- `DestinationDecider`: Eat/Drink option 열거와 예측 `StatEffect` 계산
- `FarmerActionSelector`, `GuardActionSelector`: 선택된 provider와 request를 `ActionContext`에 주입
- `EatAction`, `DrinkAction`: provider transaction 실행
- `TestDecisionScenarioProbe`: 실제 scene option과 결정성 검증
- `DestinationDB`: `BuildingType`에서 provider 조회

`IFarmWorkProvider`는 다음 코드에서만 사용된다.

- `ActionContext.FarmWorkProvider`
- `DestinationDB.TryGetFarmWorkProvider`
- `FarmerActionSelector`
- `FarmingAction`
- `FarmWorkSite`

### 2.2 Scene 구조

`SampleScene.unity`와 `GuardTest.unity`의 Eat/Drink destination은 다음 조건을 만족한다.

- `DestinationInfo.DestinationObject`가 실제 Eat/Drink GameObject를 가리킨다.
- 해당 GameObject에 `Pub` component가 붙어 있다.
- 현재 `DestinationInfo.InteractProvider`도 같은 GameObject의 `Pub`를 가리킨다.

따라서 `DestinationDB`가 `DestinationObject`에서 provider component를 초기화 시 한 번 수집하도록 바꿔도 기존 Eat/Drink 배선은 유지할 수 있다.

Farm destination의 `DestinationObject`도 이미 존재하지만 `FarmWorkSite` scene 부착은 아직 사용자 배선 단계다. 이번 refactor는 이 component를 자동 생성하지 않는다.

### 2.3 현재 작업 트리 주의사항

다음 파일은 이미 농경지 구현 과정에서 수정되었거나 신규 상태다.

- `ActionContext.cs`
- `FarmingAction.cs`
- `FarmerActionSelector.cs`
- `DestinationDB.cs`
- `FarmWorkSite.cs` 및 `System/Farming` 신규 파일
- `GuardTest.unity` 및 기타 사용자 변경

구현자는 이 변경을 reset하거나 이전 commit 버전으로 덮어쓰지 않는다. 대상 파일은 현재 working tree를 기준으로 필요한 부분만 수정한다. 씬 YAML은 이번 작업에서 직접 편집하지 않는다.

## 3. 목표

1. `IInteractionProvider`를 item 공급에 한정되지 않은 공통 실행 프로토콜로 만든다.
2. Eat, Drink, Farming이 같은 provider/request/result 경로를 사용하게 한다.
3. `ActionContext`에서 `FarmWorkProvider`를 제거하고 `InteractionProvider + InteractionRequest` 한 경로만 남긴다.
4. `DestinationDB`에서 domain별 provider cache와 lookup을 제거한다.
5. 한 destination GameObject에 서로 다른 `ActionType` provider component가 함께 존재할 수 있게 한다.
6. provider의 공통 validation과 dispatch는 `BaseInteractionProvider`가 template method로 소유하게 한다.
7. Farm phase/progress/yield transaction은 계속 `FarmWorkSite`가 소유하게 한다.
8. Pub item catalogue와 stat effect 규칙은 계속 `Pub`가 소유하게 한다.
9. provider transaction 실패가 action의 거짓 `Completed`로 처리되지 않게 한다.

## 4. 명시적 제외 범위

이번 pass에서는 다음을 구현하지 않는다.

- Sleep/Inn을 provider 기반으로 이전
- Cook, Shop, Clinic 등 신규 facility
- 운반, 창고 용량, reservation, save/load
- Farmer 숙련도 또는 progression
- utility 공식과 look-ahead 구조 변경
- 새로운 `ActionType`, `NPCIntent`, `BuildingType` 추가
- generic payload, `object`, reflection, service locator 기반 result 전달
- provider event bus 또는 UI read-model 아키텍처
- scene에서 `FarmWorkSite`, inventory, definition을 자동 배선
- 기존 `WorkerNPC`, `IAction`, action queue lifecycle 변경

Sleep은 향후 option 없는 interaction의 추가 사례가 될 수 있지만, Farming이 이미 비-item interaction을 검증하므로 이번 통합의 필수 조건이 아니다.

## 5. 최종 책임 구조

```text
DestinationObject
  -> one or more BaseInteractionProvider components

DestinationDB
  -> (BuildingType, ActionType)별 provider handle cache
  -> provider의 domain 규칙은 모름

Selector
  -> provider + InteractionRequest를 ActionContext에 주입

Action
  -> 시간을 포함한 실행 lifecycle 소유
  -> provider.TryInteract(request) 정확히 1회 호출
  -> 공통 ActorEffect 적용 또는 action 고유 비용 적용

BaseInteractionProvider
  -> 공통 support/availability/request validation
  -> concrete provider의 core method로 dispatch

Pub
  -> item option, item lookup, actor StatEffect

FarmWorkSite
  -> Growing/Harvesting, progress, yield, inventory transaction
```

시설의 runtime 상태는 concrete provider에 남는다. `IInteractionProvider`나 `ActionContext`가 farm progress, recipe, stock 같은 domain property를 노출하지 않는다.

## 6. 공통 프로토콜 설계

### 6.1 `IInteractionProvider`

목표 계약:

```csharp
public interface IInteractionProvider
{
    bool Supports(ActionType type);
    bool CanInteract(ActionType type);
    void AppendOptions(ActionType type, List<InteractionOption> buffer);
    bool TryInteract(InteractionRequest request, out InteractionResult result);
}
```

각 method의 의미:

- `Supports(type)`: 이 component가 해당 action 종류를 구조적으로 지원하는지 반환한다. 초기화 순서나 현재 재고 상태와 무관한 안정적 capability다.
- `CanInteract(type)`: 지금 이 순간 해당 interaction을 실행 가능한지 반환한다. `Supports && IsOperational` 의미다.
- `AppendOptions(type, buffer)`: 선택 가능한 option이 있는 interaction만 buffer에 추가한다. Farming처럼 option이 없는 interaction은 아무것도 추가하지 않는다.
- `TryInteract(request, out result)`: transaction을 정확히 한 번 실행한다. 반환 bool이 성공의 단일 source of truth다.

`Supports`와 `CanInteract`를 분리하는 이유는 `DestinationDB.Awake()` 시점에 Pub item initialization이나 `FarmWorkSite.Awake()` 순서를 신뢰하지 않고도 capability cache를 만들기 위해서다.

### 6.2 `InteractionRequest`

기존 `InteractRequest`를 다음 형태로 일반화한다.

```csharp
public readonly struct InteractionRequest
{
    public const int NoOptionId = -1;

    public ActionType Type { get; }
    public int OptionId { get; }
    public float Strength { get; }

    public bool HasOption => OptionId >= 0;
    public bool HasValidStrength =>
        Strength > 0f && !float.IsNaN(Strength) && !float.IsInfinity(Strength);

    public InteractionRequest(
        ActionType type,
        int optionId = NoOptionId,
        float strength = 1f);
}
```

현재 mapping:

| Action | `OptionId` | `Strength` |
|---|---:|---:|
| Eat | item ID | 1 |
| Drink | item ID | 1 |
| Farming | `NoOptionId` | 현재 1, 향후 worker efficiency |

`OptionId`는 item 전용 이름이 아니다. 향후 recipe, service option 같은 안정된 식별자로도 사용할 수 있다. provider가 option을 사용하지 않으면 `NoOptionId`를 허용한다.

`Strength`는 모든 provider가 반드시 수치 배율로 사용한다는 뜻이 아니다. 실행 강도가 필요한 provider가 소비할 수 있는 공통 실행 파라미터이며, Pub는 현재 이를 무시하고 Farm은 work efficiency로 사용한다.

### 6.3 `InteractionResult`

이번 pass에서는 result를 의도적으로 작게 유지한다.

```csharp
public readonly struct InteractionResult
{
    public StatEffect ActorEffect { get; }
    public bool HasActorEffect => ActorEffect != null;

    public InteractionResult(StatEffect actorEffect);
}
```

- 성공 여부는 `TryInteract`의 bool 하나로만 표현한다.
- Eat/Drink는 `ActorEffect`를 반환한다.
- Farming은 actor effect가 없으므로 `default` result를 반환한다. Farming 비용은 기존처럼 `FarmingActionCost`를 action이 적용한다.
- farm phase/progress/warehouse quantity는 `FarmWorkSite`와 `WarehouseInventory`에서 조회한다.

generic dictionary, `object Payload`, domain result union은 추가하지 않는다. 실제 두 번째 공통 결과 요구가 생길 때 확장한다.

### 6.4 `InteractionOption`

기존 item 전용 이름을 일반화한다.

```csharp
public readonly struct InteractionOption
{
    public ActionType Type { get; }
    public int OptionId { get; }
    public StatEffect ActorEffect { get; }
}
```

`DestinationDecider`는 이번 pass에서도 Eat/Drink option만 평가한다. utility 수식은 변경하지 않고 `ItemId -> OptionId`, `Effect -> ActorEffect` 이름만 따라간다.

## 7. `BaseInteractionProvider` template method

기존 item 전용 `BaseInteractable`을 범용 base로 교체한다.

```csharp
public abstract class BaseInteractionProvider : MonoBehaviour, IInteractionProvider
{
    protected virtual bool IsOperational => true;

    public bool Supports(ActionType type)
        => SupportsCore(type);

    public bool CanInteract(ActionType type)
        => SupportsCore(type) && IsOperational;

    public void AppendOptions(ActionType type, List<InteractionOption> buffer)
    {
        if (buffer == null || !CanInteract(type))
            return;

        AppendOptionsCore(type, buffer);
    }

    public bool TryInteract(InteractionRequest request, out InteractionResult result)
    {
        result = default;

        if (!request.HasValidStrength || !CanInteract(request.Type))
            return false;

        return TryInteractCore(request, out result);
    }

    protected abstract bool SupportsCore(ActionType type);

    protected virtual void AppendOptionsCore(
        ActionType type,
        List<InteractionOption> buffer)
    {
    }

    protected abstract bool TryInteractCore(
        InteractionRequest request,
        out InteractionResult result);
}
```

공통 base가 소유하는 것:

- null buffer 방어
- stable support와 runtime availability 결합
- invalid strength 거부
- 실패 result 초기화
- public protocol에서 concrete core로 dispatch

concrete provider가 소유하는 것:

- 지원 `ActionType`
- operational 조건
- option 생성
- domain request 검증
- transaction과 결과

public protocol method는 derived class에서 제각각 다시 구현하지 않는다. concrete class는 protected core만 override한다.

## 8. Concrete provider 변경

### 8.1 `Pub`

`Pub : BaseInteractionProvider`로 변경한다.

- `_itemInfos`와 item lookup은 `Pub`가 직접 소유한다.
- `InitializeItems(...)`를 통해 item table을 받는다.
- `IsOperational => _itemInfos != null`.
- `SupportsCore`: Eat/Drink만 true.
- `AppendOptionsCore`: category의 item을 `InteractionOption(OptionId=item.ID, ActorEffect=item.Effect)`로 추가.
- `TryInteractCore`: request type과 option ID에 맞는 item을 찾고 `InteractionResult(item.Effect)` 반환.
- request의 `Strength`는 현재 소비하지 않는다.

기존 `BaseInteractable`의 item dictionary와 `Init`은 범용 base로 옮기지 않는다.

### 8.2 `FarmWorkSite`

`FarmWorkSite : BaseInteractionProvider`로 변경한다.

- `IFarmWorkProvider` 구현을 제거한다.
- `SupportsCore(ActionType.Farming)`만 true.
- `IsOperational => _isOperational`.
- `AppendOptionsCore`는 base 기본 empty 구현 사용.
- `TryInteractCore`는 `request.Strength`를 worker efficiency로 사용한다.
- 기존 Growing/Harvesting transaction과 inventory-before-progress invariant는 그대로 유지한다.
- 성공 시 `InteractionResult`는 `default`로 반환한다.
- public `TryApplyWork`라는 두 번째 실행 경로는 남기지 않는다.

Farm의 `Phase`, `CurrentProgress`, `MaxProgress`, `NormalizedProgress` read-only property는 그대로 유지한다.

`FarmWorkResult`는 공통 실행 결과와 중복되고 production 소비자가 없어 삭제한다. TestOnly window는 호출 전후 phase/progress/warehouse quantity를 직접 기록해 마지막 변화량을 보여준다.

## 9. DestinationDB 일반화

### 9.1 DestinationInfo

`DestinationInfo`에는 다음 두 scene reference만 남긴다.

```text
BuildingType
DestinationLoc
DestinationObject
```

`InteractProvider` field는 제거한다. Farm 전용 field를 추가하지 않는다.

provider는 `DestinationObject`에 붙은 component로 표현한다. 이 필드는 모든 destination의 공통 scene object이므로 Pub/Well/Inn/Farm/GuardPost가 domain별 Inspector field를 떠안는 문제가 없다.

### 9.2 Provider cache

`Convert2Dict()`에서 destination마다 다음을 한 번 수행한다.

1. `DestinationObject.GetComponents<MonoBehaviour>()`로 component를 가져온다.
2. `IInteractionProvider` 구현만 선별한다.
3. 모든 `ActionType`을 순회하며 `provider.Supports(type)`를 확인한다.
4. `(BuildingType, ActionType)` key로 provider handle을 cache한다.

provider handle은 다음 두 값을 함께 보관한다.

- `MonoBehaviour Owner`: Unity destroyed-object fake null 확인
- `IInteractionProvider Provider`: 공통 호출 계약

`Tick()` 또는 action 실행 중 `GetComponent`를 호출하지 않는다.

### 9.3 Lookup API

기존 API를 다음 하나로 수렴한다.

```csharp
bool TryGetInteractionProvider(
    BuildingType destination,
    ActionType actionType,
    out IInteractionProvider provider);
```

조회 성공 조건:

- `(BuildingType, ActionType)` cache entry 존재
- backing `MonoBehaviour`가 destroyed되지 않음
- `Provider.CanInteract(actionType)`가 현재 true

삭제 대상:

- 기존 action type 없는 `TryGetInteractionProvider(BuildingType, out ...)`
- `_farmWorkSites`
- `TryGetFarmWorkProvider(...)`

### 9.4 중복 규칙

한 destination에서 같은 `ActionType`을 지원하는 provider는 하나만 허용한다.

- 한 Pub provider가 Eat과 Drink를 동시에 지원하는 것은 허용한다.
- 서로 다른 provider가 같은 `(BuildingType, ActionType)`을 지원하면 `DestinationDB` 초기화 중 오류를 한 번 로그한다.
- cache에는 component 순서상 첫 provider만 유지해 결과를 결정적으로 만든다.

향후 같은 건물에서 같은 action의 여러 provider를 선택해야 한다면 request에 provider identity를 몰래 넣지 않고, 별도 destination key 또는 명시적 composite provider 설계를 먼저 한다.

## 10. Selector와 ActionContext 통합

### 10.1 `ActionContext`

유지:

- `IInteractionProvider InteractionProvider`
- `InteractionRequest? Request`

삭제:

- `IFarmWorkProvider FarmWorkProvider`
- constructor의 `farmWorkProvider` parameter

ActionContext에 Cook/Shop/Clinic 전용 provider field를 추가하지 않는다는 invariant를 XML 또는 inline comment로 짧게 남긴다.

### 10.2 `FarmerActionSelector`

- Work 결정에서 `TryGetInteractionProvider(key, ActionType.Farming, out provider)`를 사용한다.
- provider가 없거나 현재 사용할 수 없으면 기존처럼 Idle로 낮추고 오류를 한 번만 로그한다.
- Work context에도 공통 `provider`와 `request`를 전달한다.
- Farming request는 `InteractionRequest(ActionType.Farming, strength: 1f)`로 만든다.
- 현재 1f는 미래 Farmer 숙련도 seam임을 명시한다.
- Eat/Drink lookup도 `decision.Request.Value.Type` 또는 mapped `ActionType`을 함께 넘긴다.
- `BuildContext`의 `IFarmWorkProvider` parameter를 제거한다.

utility 판단, `RepeatCount`, 이동 queue 구성은 변경하지 않는다.

### 10.3 `GuardActionSelector`

공급 queue에서 새 `TryGetInteractionProvider(destination, actionType, out provider)` signature만 사용한다. 전투 우선순위, Guard duty utility, patrol queue는 변경하지 않는다.

## 11. Action 변경

### 11.1 공통 원칙

`EatAction`, `DrinkAction`, `FarmingAction`은 다음 순서를 따른다.

1. `Start()`에서 provider와 request를 한 번 검증·캐시한다.
2. `Tick()`에서는 timer와 actor-local 유효성만 확인한다.
3. 완료 시점에 `TryInteract`를 정확히 한 번 호출한다.
4. false이면 `Fail(...)`하고 `Complete()`를 호출하지 않는다.
5. true이면 action별 후처리 후 `Complete()`한다.
6. `Clear()`에서 provider, request, timer를 모두 reset한다.

### 11.2 Eat/Drink

- 성공 result에 `ActorEffect`가 있으면 `NPCStat.ApplyStatEffect`를 호출한다.
- 성공했지만 actor effect가 없어도 protocol상 성공으로 볼지 여부는 provider 계약에 따른다. Pub는 항상 effect가 있는 성공만 반환한다.
- 현재 bug인 “`TryInteraction` false여도 `Complete()`”를 제거한다.
- `InteractionResult.Success` 확인은 존재하지 않는다. Try bool만 사용한다.

### 11.3 Farming

- `_farmWorkProvider`를 `_interactionProvider`로 교체한다.
- `actionContext.Request`를 `Start()`에서 캐시한다.
- 완료 시 `TryInteract(Farming request, out _)` 호출.
- provider transaction이 성공한 뒤에만 `FarmingActionCost`를 NPC stat에 적용한다.
- provider 실패 시 농경지 상태와 NPC 욕구 비용 모두 변경되지 않아야 한다.

## 12. DestinationDecider와 value type 변경

### `NPCDecision`

- `InteractRequest? Request`를 `InteractionRequest? Request`로 변경한다.
- public decision 의미, constructor shape, `Idle` factory는 유지한다.

### `DestinationDecider`

- `TryGetInteractionProvider(key, type, out provider)` 사용.
- provider를 받은 뒤 중복 `CanInteract(type)` 호출은 제거할 수 있다.
- `InteractionOption.ItemId -> OptionId`.
- `InteractionOption.Effect -> ActorEffect`.
- 내부 candidate의 `ItemId`는 `OptionId`로 rename한다.
- tie-break는 같은 정수 ID 오름차순을 유지한다.
- `ToDecision`은 `InteractionRequest(c.ActionType, c.OptionId)`를 만든다.

위 이름 변경 외 utility 수식, 위험 곡선, critical tier, look-ahead depth, candidate 종류는 수정하지 않는다.

## 13. Item 초기화 책임

기존 `BaseInteractable`에서 item table을 제거하면 `InteractableManager`는 실제로 Pub item 초기화만 담당하게 된다. 이름과 책임을 맞추기 위해 다음처럼 정리한다.

- `InteractableManager.cs`를 `ItemInteractionInitializer.cs`로 rename한다.
- `.meta`를 함께 이동해 script GUID `4761b464a243d78409e02c7851329d5e`를 보존한다.
- class 이름도 `ItemInteractionInitializer`로 변경한다.
- `[FormerlySerializedAs("_interactables")] [SerializeField] private Pub[] _pubs;`
- `_dataManager`에서 item table을 받아 각 Pub의 `InitializeItems`를 호출한다.
- 초기화는 `Awake()`에서 수행해 모든 `Start()` 이전에 Pub가 operational이 되게 한다.
- null `DataManager`/array는 원인을 한 번 로그하고 안전하게 반환한다.

현재 두 scene의 `_interactables` entry는 모두 `Pub` component이므로 field migration이 가능하다. 새 `IItemDataConsumer` interface나 item-provider base는 두 번째 실제 소비자가 생길 때까지 만들지 않는다.

## 14. 파일 변경 목록

### Rename — `.meta` GUID 보존 필수

| 기존 | 변경 | 이유 |
|---|---|---|
| `Assets/Scripts/Actor/BaseInteractable.cs` | `Assets/Scripts/Actor/BaseInteractionProvider.cs` | 범용 template method base |
| `Assets/Data/Struct/InteractStruct.cs` | `Assets/Data/Struct/InteractionRequest.cs` | request/result 분리 및 이름 일치 |
| `Assets/Scripts/Manager/InteractableManager.cs` | `Assets/Scripts/Manager/ItemInteractionInitializer.cs` | item 초기화라는 실제 책임 표현 |

각 `.cs.meta`도 파일과 함께 이동한다. 새 GUID를 생성하지 않는다.

### 신규

| 파일 | 내용 |
|---|---|
| `Assets/Data/Struct/InteractionResult.cs` | 최소 공통 result (`ActorEffect`) |

신규 `.meta`를 생성한다.

### 수정

| 파일 | 변경 |
|---|---|
| `Assets/Scripts/Interface/IInteractionProvider.cs` | Supports/CanInteract/AppendOptions/TryInteract 계약 |
| `Assets/Data/Struct/InteractionRequest.cs` | 기존 request rename 및 OptionId/Strength 일반화 |
| `Assets/Data/Struct/InteractionOption.cs` | ItemId/Effect를 OptionId/ActorEffect로 일반화 |
| `Assets/Data/Struct/ActionContext.cs` | 공통 provider/request만 유지, farm field 제거 |
| `Assets/Data/Struct/NPCDecision.cs` | request type rename |
| `Assets/Scripts/Actor/BaseInteractionProvider.cs` | template method 구현 |
| `Assets/Scripts/Actor/Pub.cs` | item domain을 concrete class로 이동 |
| `Assets/Scripts/Manager/ItemInteractionInitializer.cs` | Pub 전용 item initialization |
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 공통 provider 구현, 기존 transaction 보존 |
| `Assets/Scripts/System/Lib/DestinationDB.cs` | DestinationObject component scan, 공통 cache |
| `Assets/Scripts/System/Lib/DestinationDecider.cs` | 새 provider lookup과 option 이름 반영 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | Farming도 공통 provider/request 사용 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | 새 lookup signature 반영 |
| `Assets/Scripts/System/Action/EatAction.cs` | 공통 request 실행, 실패 semantics 수정 |
| `Assets/Scripts/System/Action/DrinkAction.cs` | 공통 request 실행, 실패 semantics 수정 |
| `Assets/Scripts/System/Action/FarmingAction.cs` | IFarm 경로 제거, 공통 request 실행 |
| `Assets/TestOnly/TestDecisionScenarioProbe.cs` | type/property/lookup rename 반영 |
| `Assets/TestOnly/TestFarmProductionWindow.cs` | 공통 Farming request로 실행, 전후 상태 표시 |
| `Assembly-CSharp.csproj` | rename/new/delete compile entry 반영 |

### 삭제

| 파일 | 이유 |
|---|---|
| `Assets/Scripts/System/Farming/IFarmWorkProvider.cs` + `.meta` | 공통 provider로 대체 |
| `Assets/Scripts/System/Farming/FarmWorkResult.cs` + `.meta` | 공통 result 및 site read-only 상태로 대체 |

### 구현 후 문서 갱신

- `PublicMD/ARCHITECTURE.md`: 과도기 `IFarmWorkProvider` 설명을 완료 구조로 변경
- `PublicMD/ProjectStructure.md`: 실제 파일 tree와 interaction flow 갱신
- `PublicMD/CodeConvention.md`: 과도기 경고를 제거하고 확정 protocol을 현재 규칙으로 변경
- `PublicMD/PROGRESS.md`: 실제 구현·검증 결과 기록

`PublicMD/Code_Evaluation_Result.md`는 구현자가 수정하지 않는다. 독립 Codex review agent가 소유한다.

## 15. 구현 순서

1. `git status`로 기존 사용자 변경과 overlap 파일을 기록한다.
2. 세 아키텍처 문서와 이 Plan을 다시 읽는다.
3. `InteractionRequest`, `InteractionResult`, `InteractionOption`, `IInteractionProvider` 계약을 먼저 변경한다.
4. `BaseInteractable`을 `BaseInteractionProvider`로 meta 보존 rename하고 template method를 구현한다.
5. `Pub`와 item initializer를 새 base에 맞춘다.
6. `FarmWorkSite`를 공통 provider로 이전하되 기존 transaction invariant를 보존한다.
7. `DestinationDB`를 공통 provider cache로 교체한다.
8. `ActionContext`와 `NPCDecision`의 type/field를 통합한다.
9. `DestinationDecider`, Farmer/Guard selector를 새 lookup/request에 맞춘다.
10. Eat/Drink/Farming action을 새 transaction semantics에 맞춘다.
11. TestOnly 두 파일을 새 protocol에 맞춘다.
12. obsolete IFarm/FarmWorkResult 파일을 참조 0건 확인 후 삭제한다.
13. csproj compile entry를 현재 파일 목록과 일치시킨다.
14. build와 정적 검증을 수행한다.
15. 세 아키텍처 문서를 구현 결과에 맞게 갱신한다.
16. 사용자가 Play Mode 배선을 확인할 항목을 정리한다.
17. 독립 Codex review agent를 실행하고 `PROGRESS.md`를 갱신한다.

삭제를 먼저 하지 않는다. 모든 소비자를 새 protocol로 옮기고 `rg` 참조가 0건인 것을 확인한 뒤 obsolete 파일을 삭제한다.

## 16. 정적 검증

### Build

```powershell
dotnet build Assembly-CSharp.csproj --no-restore
```

기대 결과: 경고 0, 오류 0.

### Reference 제거

다음 이름의 **production C# 및 csproj 참조**가 0건이어야 한다. Plan과 architecture 문서에는 migration 기록으로 구형 이름이 남을 수 있으므로 검색 범위는 `Assets/**/*.cs`와 `Assembly-CSharp.csproj`로 제한한다.

```text
IFarmWorkProvider
FarmWorkProvider
TryGetFarmWorkProvider
FarmWorkResult
BaseInteractable
InteractRequest
InteractResult
TryInteraction
DestinationInfo.InteractProvider
```

`InteractProvider:`라는 구형 scene YAML key는 Unity가 scene을 다시 저장하기 전 남아 있을 수 있다. production C# 참조 0건과 runtime component discovery가 우선 검증 대상이며, 사용자 scene 변경을 정리한다는 이유로 YAML을 직접 삭제하지 않는다.

### 구조 검증

- `ActionContext`에는 interaction provider field가 하나뿐이어야 한다.
- `DestinationDB`에는 domain 이름이 붙은 provider dictionary/lookup이 없어야 한다.
- `FarmWorkSite`와 `Pub`는 `BaseInteractionProvider`를 통해 `IInteractionProvider`를 구현해야 한다.
- action의 `Tick()`에는 `GetComponent`, scene search, provider cast가 없어야 한다.
- `TryInteract` false 경로에서 action이 `Complete()`되지 않아야 한다.
- ScriptableObject runtime mutation이 없어야 한다.
- production gameplay 코드에 새 `UnityEngine.Random` 사용이 없어야 한다.
- `git diff --check`가 통과해야 한다.

## 17. 결정적 동작 검증

### Existing decision probe

`TestDecisionScenarioProbe`의 기존 시나리오를 실행한다.

- Farmer/Guard 공급 결정 결과가 refactor 전과 동일해야 한다.
- 동일 입력 2회 결정 결과가 동일해야 한다.
- option tie-break는 기존 item ID 순서와 같은 `OptionId` 순서를 유지해야 한다.
- utility 점수와 `RepeatCount`는 바뀌지 않아야 한다.

### Farm production window

`TestFarmProductionWindow`는 다음을 검증한다.

1. Farming request 한 번마다 progress가 정확히 한 번 변경된다.
2. Growing max 도달 work가 같은 호출에서 수확까지 수행하지 않는다.
3. Harvesting 호출마다 seed 기반 수확량이 창고에 추가된다.
4. inventory가 거부하면 progress와 NPC 비용이 모두 변하지 않는다.
5. 같은 seed와 같은 호출 순서에서 같은 warehouse 증가 sequence가 나온다.
6. window의 마지막 결과는 호출 성공 여부, phase/progress 전후, warehouse delta를 표시한다.

### Real Farmer

- Work 결정 후 Move → Farming이 정상 실행된다.
- queued Farming action 각 1회가 provider transaction 각 1회와 대응한다.
- action 중단/replan 시 farm work가 적용되지 않는다.
- provider가 operational하지 않으면 selector가 Idle fallback하고 동일 오류를 spam하지 않는다.
- 농사 성공 후에만 Hunger/Thirst/Fatigue 비용이 적용된다.

### Eat/Drink

- 기존 Pub의 item option이 decider에 동일하게 노출된다.
- 성공 시 기존과 같은 `StatEffect`가 적용된다.
- 존재하지 않는 option ID 또는 unavailable provider는 `Failed`가 되고 stat은 변하지 않는다.
- transaction false 이후 action이 `Completed`로 보고되지 않는다.

### DestinationDB

- Eat GameObject의 Pub가 `(Pub, Eat)` provider로 발견된다.
- Drink GameObject의 Pub가 `(Well, Drink)` provider로 발견된다.
- FarmWorkSite를 Farm destination object에 붙이면 `(Farm, Farming)` provider로 발견된다.
- 한 GameObject의 provider가 여러 서로 다른 action을 지원할 수 있다.
- 같은 destination/action duplicate는 오류 1회와 deterministic first-provider 정책을 따른다.

## 18. Unity Editor 배선과 직렬화

이번 refactor 자체 때문에 Pub provider를 다시 drag할 필요는 없어야 한다. Eat/Drink의 `DestinationObject`에 Pub가 이미 붙어 있기 때문이다.

구현 후 사용자가 Unity Editor에서 확인할 항목:

1. `ItemInteractionInitializer` component가 Missing Script가 아닌지 확인한다.
2. 기존 DataManager와 Pub 2개 참조가 rename 후 유지되었는지 확인한다.
3. `DestinationDB`의 모든 row에서 `DestinationObject` 참조가 유지되었는지 확인한다.
4. Farm destination object에 `FarmWorkSite`를 부착한다.
5. FarmWorkSite에 `FarmProductionDefinition`, `SeededRandomSource`, `IInventory` 구현 source를 연결한다.
6. TestFarmProductionWindow 참조를 연결한다.

class/file rename은 기존 `.meta` GUID를 보존하고 `[FormerlySerializedAs]`를 사용한다. 구현자가 scene YAML을 직접 정리하거나 기존 사용자 scene 변경을 덮어쓰지 않는다.

## 19. 위험과 대응

### 초기화 순서

위험: `DestinationDB.Awake`가 Pub item initialization보다 먼저 실행될 수 있다.

대응: cache 구성에는 operational 상태가 아닌 `Supports`를 사용한다. 실제 조회 시 `CanInteract`를 확인한다. Item initializer는 `Awake`에서 Pub를 초기화한다.

### Unity interface fake null

위험: interface reference만 cache하면 component가 destroy된 뒤 일반 null 비교가 실패할 수 있다.

대응: provider interface와 backing `MonoBehaviour`를 handle에 함께 저장한다.

### 같은 action provider 중복

위험: request에 provider identity가 없어 어떤 provider를 선택했는지 재현할 수 없다.

대응: `(BuildingType, ActionType)`당 provider 하나 invariant를 검증하고 첫 component를 결정적으로 유지한다.

### Result의 과도한 범용화

위험: 모든 domain 결과를 담으려다 `object Payload`, 거대한 union, nullable field 묶음이 될 수 있다.

대응: 이번 result는 실제 공통 소비자인 `ActorEffect`만 가진다. farm UI는 concrete site read model을 읽는다.

### ActionContext 재팽창

위험: 다음 feature에서 다시 domain provider field를 추가할 수 있다.

대응: provider/request 한 경로를 문서 invariant로 남기고 새 domain은 먼저 공통 protocol 적합성을 검토한다.

### 현재 working tree 충돌

위험: 농경지 구현 파일과 GuardTest scene에 아직 commit되지 않은 사용자 변경이 있다.

대응: reset/checkout 금지, scene 직접 편집 금지, current file에서 최소 diff로 migration한다. 충돌을 우회할 수 없으면 중단하고 보고한다.

## 20. Stop condition

다음 중 하나가 발생하면 범위를 임의로 넓히지 않고 사용자에게 보고한다.

1. Eat/Drink provider가 `DestinationObject`와 다른 GameObject에 있어 component discovery로 기존 배선을 보존할 수 없음.
2. `BaseInteractable` 또는 `InteractableManager` script GUID rename으로 scene component reference가 보존되지 않음.
3. 공통 request로 옮기기 위해 `WorkerNPC`, `IAction`, queue lifecycle 공개 계약 변경이 필요함.
4. utility 결과를 유지하려면 `DestinationDecider` 수식 자체를 바꿔야 함.
5. 기존 사용자 scene 변경과 병합하지 않고는 구현할 수 없음.
6. 같은 실패 원인으로 세 번 연속 build/validation이 막힘.

## 21. 완료 기준

다음 조건을 모두 만족해야 구현 완료다.

- Eat, Drink, Farming이 같은 `IInteractionProvider` protocol을 사용한다.
- `IFarmWorkProvider` 계열 참조와 파일이 제거된다.
- `ActionContext`에 domain별 provider field가 없다.
- `DestinationDB`에 domain별 provider lookup이 없다.
- `BaseInteractionProvider`가 공통 validation/dispatch를 실제로 제공한다.
- Pub item data와 Farm runtime state가 각 concrete provider에 남는다.
- provider false가 action의 거짓 성공으로 처리되지 않는다.
- build가 경고 0, 오류 0으로 통과한다.
- 기존 decision probe 결과가 유지된다.
- farm과 Pub Play Mode 검증 항목이 실제 결과와 함께 보고된다.
- scene 재배선 필요 여부가 명확히 보고된다.
- 구현 후 세 아키텍처 문서와 `PROGRESS.md`가 갱신된다.
- 독립 Codex review agent가 실행되며, 아직 받지 않은 결과를 통과로 주장하지 않는다.

## 22. Claude Code 실행 지침

이 문서는 구현 승인을 대신하지 않는다. Claude Code는 다음 절차를 따른다.

1. Opus Plan Mode에서 current working tree와 이 Plan의 전제를 재검토한다.
2. 치명적인 전제 오류가 있으면 구현하지 말고 보고한다.
3. 사용자의 명시적 승인 후 Sonnet으로 구현한다.
4. 구현 파일 외 기존 사용자 변경을 revert, overwrite, stage하지 않는다.
5. commit과 push는 사용자가 별도로 요청하기 전에는 하지 않는다.
6. 구현·검증 후 프로젝트 skill 절차에 따라 독립 Codex review agent를 실행한다.
7. 최종 보고 마지막에 신규·수정·rename·삭제 스크립트 목록을 구분해 제시한다.
