# IInteractionProvider Common Protocol Unification Plan

> 보관 경로: `PublicMD/Archive/Plans/InteractionProvider_Unification_Plan.md`
> 구현이 완료된 계획 문서이며 현재 구조의 기준은 루트 아키텍처 문서와 기능별 문서를 따릅니다.

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

따라서 `DestinationDB`는 기존처럼 `BuildingType -> DestinationObject`를 해석하고, provider의 초기화·등록·조회는 `InteractableManager`에 위임할 수 있다. 기존 Eat/Drink의 `DestinationObject`와 `Pub` component 관계는 유지된다.

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
4. `DestinationDB`에서 domain별 provider cache와 lookup을 제거하고, 건물과 scene object의 매핑만 소유하게 한다.
5. `InteractableManager`를 모든 interaction provider의 초기화·등록·조회 책임을 가진 범용 manager로 유지한다.
6. 한 destination GameObject에 서로 다른 `ActionType` provider component가 함께 존재할 수 있게 한다.
7. provider의 공통 초기화 lifecycle, validation과 dispatch는 `BaseInteractionProvider`가 template method로 소유하게 한다.
8. Farm phase/progress/yield transaction은 계속 `FarmWorkSite`가 소유하게 한다.
9. Pub item catalogue와 stat effect 규칙은 계속 `Pub`가 소유하게 한다.
10. provider transaction 실패가 action의 거짓 `Completed`로 처리되지 않게 한다.

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

InteractableManager
  -> scene의 interaction provider 초기화·명시적 등록
  -> (GameObject owner, ActionType)별 provider handle cache
  -> provider의 domain 규칙과 BuildingType은 모름

DestinationDB
  -> BuildingType에서 DestinationLoc/DestinationObject 조회
  -> interaction 조회는 DestinationObject를 InteractableManager에 전달

Selector
  -> provider + InteractionRequest를 ActionContext에 주입

Action
  -> 시간을 포함한 실행 lifecycle 소유
  -> provider.TryInteract(request) 정확히 1회 호출
  -> 공통 ActorEffect 적용 또는 action 고유 비용 적용

BaseInteractionProvider
  -> idempotent 초기화 lifecycle
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

`Supports`와 `CanInteract`를 분리하는 이유는 capability 등록과 runtime 사용 가능 여부를 구분하기 위해서다. `InteractableManager`는 안정적인 `Supports` 결과로 cache를 구성하고, 실제 조회 시 `CanInteract`로 초기화 성공 여부와 현재 상태를 확인한다.

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
    private bool _initializationAttempted;
    private bool _isOperational;
    private string _initializationFailureReason;

    public bool TryInitialize(out string failureReason)
    {
        if (_initializationAttempted)
        {
            failureReason = _initializationFailureReason;
            return _isOperational;
        }

        _initializationAttempted = true;
        _isOperational = TryInitializeCore(out _initializationFailureReason);
        failureReason = _initializationFailureReason;
        return _isOperational;
    }

    public bool Supports(ActionType type)
        => SupportsCore(type);

    public bool CanInteract(ActionType type)
    {
        EnsureInitialized();
        return SupportsCore(type) && _isOperational && CanInteractCore(type);
    }

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

    protected virtual bool CanInteractCore(ActionType type)
        => true;

    protected abstract bool TryInitializeCore(out string failureReason);

    protected virtual void AppendOptionsCore(
        ActionType type,
        List<InteractionOption> buffer)
    {
    }

    protected abstract bool TryInteractCore(
        InteractionRequest request,
        out InteractionResult result);

    private void EnsureInitialized()
    {
        if (!_initializationAttempted)
            TryInitialize(out _);
    }
}
```

공통 base가 소유하는 것:

- idempotent 초기화와 실패 상태 보존
- manager보다 먼저 protocol이 호출되어도 안전한 lazy initialization
- null buffer 방어
- stable support와 runtime availability 결합
- invalid strength 거부
- 실패 result 초기화
- public protocol에서 concrete core로 dispatch

concrete provider가 소유하는 것:

- 지원 `ActionType`
- 초기화 dependency 검증과 operational 조건
- option 생성
- domain request 검증
- transaction과 결과

public protocol method는 derived class에서 제각각 다시 구현하지 않는다. concrete class는 protected core만 override한다.

## 8. Concrete provider 변경

### 8.1 `Pub`

`Pub : BaseInteractionProvider`로 변경한다.

- `_itemInfos`와 item lookup은 `Pub`가 직접 소유한다.
- `[SerializeField] private ItemDataContext _itemDataContext;`를 직접 참조한다.
- `TryInitializeCore`에서 `_itemDataContext.ItemInfos()`를 한 번 읽어 `_itemInfos`를 구성한다.
- 누락된 context나 mapping 실패는 초기화 실패 이유로 반환한다.
- `SupportsCore`: Eat/Drink만 true.
- `AppendOptionsCore`: category의 item을 `InteractionOption(OptionId=item.ID, ActorEffect=item.Effect)`로 추가.
- `TryInteractCore`: request type과 option ID에 맞는 item을 찾고 `InteractionResult(item.Effect)` 반환.
- request의 `Strength`는 현재 소비하지 않는다.

기존 `BaseInteractable`의 item dictionary와 `Init`은 범용 base로 옮기지 않는다.

두 Pub가 같은 `ItemDataContext.asset`을 참조하는 것은 데이터를 복제하는 것이 아니다. 두 component가 같은 공유 definition asset을 명시적으로 참조하는 구조이며, `InteractableManager`가 item domain dependency를 알지 않게 한다.

### 8.2 `FarmWorkSite`

`FarmWorkSite : BaseInteractionProvider`로 변경한다.

- `IFarmWorkProvider` 구현을 제거한다.
- `SupportsCore(ActionType.Farming)`만 true.
- 기존 `Awake()`의 definition/random/inventory 검증을 `TryInitializeCore`로 이동한다.
- concrete dependency는 계속 `FarmWorkSite`의 serialized field로 소유한다.
- `AppendOptionsCore`는 base 기본 empty 구현 사용.
- `TryInteractCore`는 `request.Strength`를 worker efficiency로 사용한다.
- 기존 Growing/Harvesting transaction과 inventory-before-progress invariant는 그대로 유지한다.
- 성공 시 `InteractionResult`는 `default`로 반환한다.
- public `TryApplyWork`라는 두 번째 실행 경로는 남기지 않는다.

Farm의 `Phase`, `CurrentProgress`, `MaxProgress`, `NormalizedProgress` read-only property는 그대로 유지한다.

`FarmWorkResult`는 공통 실행 결과와 중복되고 production 소비자가 없어 삭제한다. TestOnly window는 호출 전후 phase/progress/warehouse quantity를 직접 기록해 마지막 변화량을 보여준다.

## 9. InteractableManager registry와 DestinationDB routing

### 9.1 DestinationInfo

`DestinationInfo`에는 다음 두 scene reference만 남긴다.

```text
BuildingType
DestinationLoc
DestinationObject
```

`InteractProvider` field는 제거한다. Farm 전용 field를 추가하지 않는다.

provider는 `DestinationObject`에 붙은 component로 표현한다. 이 필드는 모든 destination의 공통 scene object이므로 Pub/Well/Inn/Farm/GuardPost가 domain별 Inspector field를 떠안는 문제가 없다.

### 9.2 `InteractableManager`의 명시적 등록과 초기화

`InteractableManager`는 이름과 기존 scene component를 유지한다. 현재 serialized field를 다음 형태로 발전시킨다.

```csharp
[SerializeField] private BaseInteractionProvider[] _interactables;
```

manager는 `Awake()`와 lookup 진입부에서 idempotent `EnsureInitialized()`를 호출한다. 초기화 과정은 다음과 같다.

1. `_interactables`를 serialized 순서대로 순회한다.
2. null/destroyed component를 건너뛰고 원인을 한 번 로그한다.
3. 각 provider의 `TryInitialize(out reason)`를 한 번 호출한다.
4. 모든 `ActionType`을 순회하며 안정적인 `provider.Supports(type)`를 확인한다.
5. `(provider.gameObject, ActionType)` key로 provider handle을 cache한다.

provider handle은 `BaseInteractionProvider` concrete component를 보관한다. 따라서 Unity destroyed-object fake null 검사와 `IInteractionProvider` 호출을 모두 만족하며, interface만 cache할 때 생기는 fake-null 문제를 피한다.

`InteractableManager`가 제공하는 API:

```csharp
bool TryGetInteractionProvider(
    GameObject destinationObject,
    ActionType actionType,
    out IInteractionProvider provider);
```

조회 성공 조건:

- manager 초기화가 완료됨
- `(destinationObject, actionType)` cache entry 존재
- backing component가 destroyed되지 않음
- `Provider.CanInteract(actionType)`가 현재 true

`GetComponent`, scene-wide search, tag lookup을 매 조회나 action `Tick()`에서 수행하지 않는다. provider 등록은 `_interactables` 배열 하나가 명시적 source of truth다.

### 9.3 `DestinationDB`의 routing

`DestinationDB`는 provider component를 scan하거나 cache하지 않는다. `[SerializeField] private InteractableManager _interactableManager;`를 참조하고 다음 순서만 수행한다.

1. `BuildingType`으로 `DestinationInfo`를 조회한다.
2. `DestinationObject`가 유효한지 확인한다.
3. manager에 `(DestinationObject, ActionType)` lookup을 위임한다.

외부 호출 API는 기존 소비자의 의미를 위해 `DestinationDB`에 유지한다.

```csharp
bool TryGetInteractionProvider(
    BuildingType destination,
    ActionType actionType,
    out IInteractionProvider provider);
```

삭제 대상:

- 기존 action type 없는 `TryGetInteractionProvider(BuildingType, out ...)`
- `_farmWorkSites`
- `TryGetFarmWorkProvider(...)`
- `DestinationDB` 내부 provider handle/cache/component scan

이 경계에서 `DestinationDB`는 위치·scene object routing을, `InteractableManager`는 interaction component lifecycle과 registry를 담당한다. 양쪽 모두 Pub/Farm/Cook 같은 domain 종류를 분기하지 않는다.

### 9.4 중복 규칙

같은 GameObject에서 같은 `ActionType`을 지원하는 provider는 하나만 허용한다.

- 한 Pub provider가 Eat과 Drink를 동시에 지원하는 것은 허용한다.
- 서로 다른 provider가 같은 `(GameObject, ActionType)`을 지원하면 `InteractableManager` 초기화 중 오류를 한 번 로그한다.
- cache에는 `_interactables` 배열에서 먼저 나온 provider만 유지해 결과를 결정적으로 만든다.

향후 같은 시설 object에서 같은 action의 여러 provider를 선택해야 한다면 request에 provider identity를 몰래 넣지 않고, 별도 destination object 또는 명시적 composite provider 설계를 먼저 한다.

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

## 13. `InteractableManager`의 범용 manager 책임

`InteractableManager`가 현재 Pub item initializer처럼 보이는 것은 등록된 concrete provider가 Pub뿐이기 때문이다. 이 우연을 장기 책임으로 확정해 `ItemInteractionInitializer`로 축소하지 않는다. class/file과 `.meta` GUID를 그대로 유지하고, 다음 범용 책임을 실제로 부여한다.

- scene에서 사용 가능한 `BaseInteractionProvider`의 명시적 등록 목록 소유
- provider 초기화 lifecycle 시작과 초기화 실패 진단
- `(GameObject owner, ActionType)` capability cache 소유
- destination object와 action type을 통한 provider 조회
- duplicate/null/destroyed provider 검증

반대로 manager가 소유하지 않는 것:

- Pub item table, Farm progress, Cook recipe 같은 domain data
- Eat/Drink/Farming별 `switch` 또는 concrete type 검사
- selector의 행동 우선순위나 utility 계산
- action 실행 시간, stat 변경, transaction 규칙
- 모든 provider에 넘기는 범용 service bag 또는 service locator

따라서 기존 `_dataManager` field와 “manager가 item table을 Pub에 주입”하는 흐름은 제거한다. Pub는 concrete dependency인 `ItemDataContext`를 직접 serialized reference로 소유하고, FarmWorkSite도 기존 concrete dependency를 직접 소유한다. manager는 각 provider의 공통 `TryInitialize`만 호출한다.

이 변경 후 `DataManager.GetItemInfos()`와 `IDataManager.GetItemInfos()`의 호출부가 0건이므로 두 API와 `DataManager._itemDataContext`도 함께 제거한다. `ItemDataContext` 자체는 Pub가 직접 참조하는 공유 definition asset으로 유지한다.

이 제한이 중요하다. 이름만 `InteractableManager`로 남기고 domain dependency를 계속 추가하면 실제로는 God Manager가 된다. 반대로 위 lifecycle·registry 경계를 지키면 현재 Pub뿐인 상태에서도 미래 `Cook`, `Shop`, `Clinic` provider를 같은 절차로 등록할 수 있고 manager 자체는 수정할 필요가 없다.

## 14. 파일 변경 목록

### Rename — `.meta` GUID 보존 필수

| 기존 | 변경 | 이유 |
|---|---|---|
| `Assets/Scripts/Actor/BaseInteractable.cs` | `Assets/Scripts/Actor/BaseInteractionProvider.cs` | 범용 template method base |
| `Assets/Data/Struct/InteractStruct.cs` | `Assets/Data/Struct/InteractionRequest.cs` | request/result 분리 및 이름 일치 |

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
| `Assets/Scripts/Actor/BaseInteractionProvider.cs` | idempotent 초기화 lifecycle과 template method 구현 |
| `Assets/Scripts/Actor/Pub.cs` | `ItemDataContext` 직접 참조와 item domain concrete 구현 |
| `Assets/Scripts/Manager/InteractableManager.cs` | 범용 provider 초기화·등록·조회 cache manager로 확장 |
| `Assets/Scripts/Manager/DataManager.cs` | 사용되지 않는 item context field와 `GetItemInfos()` 제거 |
| `Assets/Scripts/Interface/IDataManager.cs` | 사용되지 않는 `GetItemInfos()` 계약 제거 |
| `Assets/Scripts/System/Farming/FarmWorkSite.cs` | 공통 provider 구현, 기존 transaction 보존 |
| `Assets/Scripts/System/Lib/DestinationDB.cs` | BuildingType→DestinationObject routing 유지, provider 조회를 manager에 위임 |
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
- `PublicMD/Status/PROGRESS.md`: 실제 구현·검증 결과 기록

`PublicMD/Status/Code_Evaluation_Result.md`는 구현자가 수정하지 않는다. 독립 Codex review agent가 소유한다.

## 15. 구현 순서

1. `git status`로 기존 사용자 변경과 overlap 파일을 기록한다.
2. 세 아키텍처 문서와 이 Plan을 다시 읽는다.
3. `InteractionRequest`, `InteractionResult`, `InteractionOption`, `IInteractionProvider` 계약을 먼저 변경한다.
4. `BaseInteractable`을 `BaseInteractionProvider`로 meta 보존 rename하고 초기화 lifecycle과 template method를 구현한다.
5. `Pub`가 `ItemDataContext`를 직접 소유하고 새 base lifecycle로 초기화되게 변경한다.
6. `FarmWorkSite`를 공통 provider로 이전하되 기존 transaction invariant와 concrete dependency ownership을 보존한다.
7. `InteractableManager`를 범용 provider 초기화·등록·조회 cache로 확장한다. class/file rename은 하지 않는다.
8. 호출부가 사라진 `DataManager`/`IDataManager`의 item 전달 API를 제거한다.
9. `DestinationDB`의 domain/provider cache를 제거하고 interaction 조회를 manager에 위임한다.
10. `ActionContext`와 `NPCDecision`의 type/field를 통합한다.
11. `DestinationDecider`, Farmer/Guard selector를 새 lookup/request에 맞춘다.
12. Eat/Drink/Farming action을 새 transaction semantics에 맞춘다.
13. TestOnly 두 파일을 새 protocol에 맞춘다.
14. obsolete IFarm/FarmWorkResult 파일을 참조 0건 확인 후 삭제한다.
15. csproj compile entry를 현재 파일 목록과 일치시킨다.
16. build와 정적 검증을 수행한다.
17. 세 아키텍처 문서를 구현 결과에 맞게 갱신한다.
18. 사용자가 Play Mode 배선을 확인할 항목을 정리한다.
19. 독립 Codex review agent를 실행하고 `PROGRESS.md`를 갱신한다.

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
GetItemInfos
```

`InteractProvider:`라는 구형 scene YAML key는 Unity가 scene을 다시 저장하기 전 남아 있을 수 있다. production C# 참조 0건과 manager registry를 통한 runtime lookup이 우선 검증 대상이며, 사용자 scene 변경을 정리한다는 이유로 YAML을 직접 삭제하지 않는다.

### 구조 검증

- `ActionContext`에는 interaction provider field가 하나뿐이어야 한다.
- `InteractableManager`만 provider registry/cache를 소유해야 한다.
- `InteractableManager`에는 `Pub`, `FarmWorkSite`, `ItemDataContext`, `DataManager` 같은 concrete/domain type 분기나 dependency가 없어야 한다.
- `DestinationDB`에는 domain 이름이 붙은 provider dictionary/lookup과 provider component scan이 없어야 한다.
- `DestinationDB`는 `BuildingType -> DestinationObject -> InteractableManager` routing만 수행해야 한다.
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

### InteractableManager와 DestinationDB

- InteractableManager에 등록된 Eat GameObject의 Pub가 `(GameObject, Eat)` provider로 cache된다.
- InteractableManager에 등록된 Drink GameObject의 Pub가 `(GameObject, Drink)` provider로 cache된다.
- FarmWorkSite를 manager에 등록하고 Farm destination object에 붙이면 `(GameObject, Farming)` provider로 cache된다.
- DestinationDB가 각각의 `BuildingType`을 올바른 object로 변환해 manager 조회 결과를 반환한다.
- 한 GameObject의 provider가 여러 서로 다른 action을 지원할 수 있다.
- 같은 object/action duplicate는 오류 1회와 serialized registration 순서의 deterministic first-provider 정책을 따른다.

## 18. Unity Editor 배선과 직렬화

`InteractableManager` class/file과 기존 `_interactables` field를 유지하므로 기존 두 Pub 등록은 가능한 한 직렬화 상태를 보존한다. 다만 Pub의 item dependency와 DestinationDB→manager 연결은 새 책임 경계에 맞춰 명시적으로 확인해야 한다.

구현 후 사용자가 Unity Editor에서 확인할 항목:

1. 기존 `InteractableManager` component가 그대로 존재하고 `_interactables`의 Pub 2개 참조가 유지되었는지 확인한다.
2. 각 Pub에 동일한 `ItemDataContext.asset`을 할당한다.
3. Farm destination object에 `FarmWorkSite`를 부착하고 이를 `InteractableManager._interactables`에 추가한다.
4. `DestinationDB._interactableManager`에 scene의 manager를 할당한다.
5. `DestinationDB`의 모든 row에서 `DestinationObject` 참조가 유지되었는지 확인한다.
6. FarmWorkSite에 `FarmProductionDefinition`, `SeededRandomSource`, `IInventory` 구현 source를 연결한다.
7. TestFarmProductionWindow 참조를 연결한다.

class/file rename은 기존 `.meta` GUID를 보존하고 `[FormerlySerializedAs]`를 사용한다. 구현자가 scene YAML을 직접 정리하거나 기존 사용자 scene 변경을 덮어쓰지 않는다.

## 19. 위험과 대응

### 초기화 순서

위험: `DestinationDB.Awake`, `InteractableManager.Awake`, TestOnly component의 호출 순서를 scene에서 보장하기 어렵다.

대응: manager와 base provider 초기화를 idempotent하게 만든다. manager의 `Awake()`와 `TryGetInteractionProvider()`가 모두 `EnsureInitialized()`를 사용하고, provider public protocol도 아직 초기화되지 않았다면 같은 초기화 경로를 안전하게 호출한다. Script Execution Order와 scene search에 의존하지 않는다.

### God Manager 팽창

위험: 미래 provider를 추가할 때마다 manager에 data field, concrete type 분기, domain 초기화 코드를 추가하면 다시 결합 지점이 된다.

대응: manager의 허용 책임을 lifecycle·registry·lookup·공통 진단으로 제한한다. concrete dependency는 concrete provider가 serialized reference로 소유하며, manager code에는 `Pub`, `FarmWorkSite`, `Cook` 등의 이름이 등장하지 않아야 한다.

### 명시적 등록 누락

위험: GameObject에 provider를 붙였지만 manager의 `_interactables`에 등록하지 않으면 lookup에 나타나지 않는다.

대응: manager 초기화 시 null/duplicate를 검증하고, DestinationDB lookup 실패는 명확한 진단과 기존 Idle fallback으로 처리한다. 자동 scene-wide discovery는 이번 범위에 넣지 않는다.

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

1. Eat/Drink provider의 owner GameObject와 `DestinationObject`가 달라 `(GameObject, ActionType)` routing으로 기존 의미를 보존할 수 없음.
2. `BaseInteractable` rename 또는 `InteractableManager` field migration으로 scene component/provider 등록 reference가 보존되지 않음.
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
- `InteractableManager`가 provider lifecycle·registry·lookup을 소유하며 concrete domain 분기를 갖지 않는다.
- `DestinationDB`는 interaction registry를 중복 소유하지 않고 manager에 routing한다.
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
