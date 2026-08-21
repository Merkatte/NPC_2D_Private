# Farm Production Gauge Skeleton Plan

> 문서 상태: 구현 전 계획
>
> 작성일: 2026-08-21
>
> 구현 담당: Claude Code
>
> 범위: 농경지 성장·수확 게이지와 최소 창고 입고 골격

## 1. 목표

현재 `FarmingAction`은 일정 시간이 지나면 NPC의 욕구 비용을 적용하고 `Work is done!` 로그를 남긴 뒤 종료된다. 이 행동 완료를 실제 농경지 상태 변화와 연결한다.

구현 후 한 농경지 인스턴스는 다음 순환을 반복한다.

```text
Growing (게이지 0 → 최대치)
  → 게이지가 최대치에 도달
Harvesting (게이지 최대치 → 0)
  → 게이지가 0에 도달
Growing (새 작물 시작)
```

- Farmer가 `FarmingAction`을 한 번 성공적으로 완료할 때마다 농경지에 작업 1회를 적용한다.
- `Growing` 단계에서는 게이지가 설정된 양만큼 증가한다.
- 최대치에 도달하면 `Harvesting` 단계로 전환한다.
- `Harvesting` 단계에서는 작업할 때마다 게이지가 설정된 양만큼 감소한다.
- 게이지가 감소한 작업마다 설정된 최소·최대 범위에서 생산량을 한 번 뽑아 창고에 추가한다.
- 수확 게이지가 0이 되면 같은 농경지는 다시 `Growing` 단계로 돌아간다.

## 2. 이번 구현에서 확정할 동작

아래 항목은 구현자가 추가 질문 없이 사용할 기본 정책이다.

1. 게이지는 Worker가 아니라 **농경지 GameObject 인스턴스별 공유 runtime 상태**다. 여러 Farmer가 같은 농경지에서 일하면 같은 게이지에 기여한다.
2. 최초 상태는 `Growing`, 현재 게이지는 `0`이다.
3. 한 번의 `FarmingAction` 완료는 농경지 작업 1회와 정확히 대응한다.
4. 성장량과 수확 감소량은 `float`로 둔다. 현재 숙련도 배율은 항상 `1f`이며, 숙련도 시스템은 만들지 않는다.
5. 성장 작업으로 최대치에 도달한 순간에는 단계만 `Harvesting`으로 바뀐다. 같은 작업에서 수확물까지 만들지는 않는다.
6. 이후의 수확 작업은 먼저 생산물을 창고에 안전하게 추가한 뒤 게이지를 감소시킨다.
7. 마지막 수확 작업에서도 생산물이 발생하며, 게이지가 0이 되면 단계만 `Growing`으로 바뀐다. 같은 작업에서 새 성장 게이지를 올리지는 않는다.
8. 생산량 범위는 양 끝을 포함한다. 예를 들어 최소 4, 최대 6이면 결과는 `4`, `5`, `6` 중 하나다.
9. 현재 `DestinationDecider`가 여러 `FarmingAction`을 한 큐에 넣을 수 있으므로, 남은 작업은 단계 전환 후에도 계속 실행할 수 있다. 예를 들어 세 번째 작업에서 `Harvesting`으로 전환되면 네 번째 작업부터 수확한다.
10. 이번 골격의 창고는 용량 제한이 없는 최소 구현으로 둔다. 창고 가득 참, 운반, 적재 슬롯은 후속 작업이다.

## 3. 책임과 데이터 소유권

```text
DestinationDecider
  └─ 기존대로 Work intent와 반복 횟수 결정

FarmerActionSelector
  └─ Farm 위치와 IFarmWorkProvider를 ActionContext에 배선

FarmingAction
  └─ 작업 시간과 NPC 욕구 비용 처리
  └─ 완료 시 IFarmWorkProvider.TryApplyWork(...) 한 번 호출

FarmWorkSite
  └─ 현재 단계와 현재 게이지 소유
  └─ 성장/수확 전환 규칙 소유
  └─ 수확량을 계산하고 창고 입고 요청

FarmProductionDefinition (ScriptableObject)
  └─ 게이지 최대치, 기본 성장량, 기본 수확 감소량
  └─ 생산 Item ID, 최소/최대 생산량

WarehouseInventory
  └─ Item ID별 실제 수량 소유
  └─ 유효성 검사와 안전한 추가 API 제공

IRandomSource
  └─ 재현 가능한 정수 난수 제공
```

### Source of truth

- `FarmProductionDefinition`은 공유 설정값만 소유한다.
- `FarmWorkSite`가 현재 단계와 현재 게이지를 소유한다.
- `WarehouseInventory`가 실제 생산물 수량을 소유한다.
- ScriptableObject에는 현재 게이지나 현재 창고 수량을 기록하지 않는다.
- `FarmingActionCost`는 기존처럼 NPC가 작업하며 소비하는 Hunger/Thirst/Fatigue만 소유한다. 농경지 생산 설정을 여기에 추가하지 않는다.

## 4. 기존 `IInteractionProvider`를 사용하지 않는 이유

현재 `IInteractionProvider`는 다음 계약을 사용한다.

- 요청: `ActionType + ItemId`
- 결과: `Success + StatEffect`

이는 음식·음료처럼 아이템을 선택하여 NPC stat에 효과를 적용하는 소비 행동에 맞춰져 있다. 농경지 작업은 게이지 변화, 단계 전환, 생산 Item ID와 생산 수량을 결과로 가지므로 동일 계약에 넣지 않는다.

농사 전용의 좁은 capability인 `IFarmWorkProvider`를 추가한다. 이는 `FarmingAction`과 시설 구현을 분리하고, 순수 테스트 대역을 만들 수 있는 명확한 경계다.

## 5. 신규 타입 설계

### 5.1 `FarmWorkPhase`

경로:

`Assets/Scripts/System/Farming/FarmWorkPhase.cs`

```csharp
public enum FarmWorkPhase
{
    Growing,
    Harvesting,
}
```

기존 project-wide `Enum` 폴더에 넣지 않는다. 현재는 농경지 domain에서만 사용하는 상태이기 때문이다.

### 5.2 `FarmWorkResult`

경로:

`Assets/Scripts/System/Farming/FarmWorkResult.cs`

작업 한 번의 관찰 가능한 결과를 담는 readonly struct로 만든다.

필수 정보:

- `bool Success`
- `FarmWorkPhase PreviousPhase`
- `FarmWorkPhase CurrentPhase`
- `float PreviousProgress`
- `float CurrentProgress`
- `int ProducedItemId` (`생산 없음`은 `-1`)
- `int ProducedQuantity`

필요하다면 `PhaseChanged`, `ProducedAnything`은 계산 property로 제공한다. 결과 struct가 다음 행동을 선택하거나 NPC stat을 변경해서는 안 된다.

### 5.3 `IFarmWorkProvider`

경로:

`Assets/Scripts/System/Farming/IFarmWorkProvider.cs`

권장 계약:

```csharp
public interface IFarmWorkProvider
{
    bool CanApplyWork { get; }
    bool TryApplyWork(float workerEfficiency, out FarmWorkResult result);
}
```

- 현재 `FarmingAction`은 `workerEfficiency: 1f`를 전달한다.
- 추후 Farmer 숙련도가 생기면 호출부가 이 값만 계산하여 넘길 수 있게 한다.
- 이번 작업에서는 `FarmStat`, 숙련도 공식, 생산량 보정 공식을 추가하지 않는다.

### 5.4 `FarmProductionDefinition`

경로:

`Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs`

`ScriptableObject`이며 다음 serialized field와 read-only property를 가진다.

- `_maxProgress`: 게이지 최대치, `> 0`
- `_growthPerWork`: 성장 작업 1회의 기본 증가량, `> 0`
- `_harvestProgressPerWork`: 수확 작업 1회의 기본 감소량, `> 0`
- `_outputItemId`: 창고에 추가할 Item ID, `>= 0`
- `_minimumYield`: 작업 1회당 최소 생산량, `>= 0`
- `_maximumYield`: 작업 1회당 최대 생산량, `>= minimum`

`OnValidate()`에서 범위를 보정하거나 명확한 오류를 보고한다. `CreateAssetMenu`를 제공한다.

초기 테스트 asset 권장값:

```text
MaxProgress             = 100
GrowthPerWork           = 10
HarvestProgressPerWork  = 10
OutputItemId            = 프로젝트에서 사용할 임시 농산물 ID
MinimumYield            = 4
MaximumYield            = 6
```

### 5.5 `IInventory`와 `WarehouseInventory`

경로:

- `Assets/Scripts/Interface/IInventory.cs`
- `Assets/Scripts/System/Inventory/WarehouseInventory.cs`

최소 계약:

```csharp
public interface IInventory
{
    bool TryAdd(int itemId, int quantity);
    int GetQuantity(int itemId);
}
```

`WarehouseInventory`는 `MonoBehaviour`이며 runtime `Dictionary<int, int>`를 소유한다.

- 잘못된 Item ID 또는 음수·0 수량은 mutation 없이 실패한다.
- 정상 입력은 overflow를 검사한 뒤 한 번에 추가한다.
- 이번에는 capacity를 두지 않는다.
- Inspector 디버깅 또는 UI가 필요하면 read-only snapshot/API를 제공하되 dictionary 자체를 외부에 mutable하게 노출하지 않는다.
- 향후 capacity가 추가되면 `accepted quantity`를 반환하는 계약으로 확장하고, 수용되지 않은 생산물을 소실하지 않는 transaction으로 변경한다.

### 5.6 `IRandomSource`와 결정적 구현

경로:

- `Assets/Scripts/Interface/IRandomSource.cs`
- `Assets/Scripts/System/Lib/SeededRandomSource.cs`

권장 계약:

```csharp
public interface IRandomSource
{
    int NextInclusive(int minimum, int maximum);
}
```

`SeededRandomSource`는 Inspector에서 seed를 받고 동일 seed와 동일 호출 순서에서 같은 값을 반환한다.

- `UnityEngine.Random` 전역 상태를 직접 사용하지 않는다.
- `FarmWorkSite.Tick()` 또는 `FarmingAction.Tick()`에서 난수 source를 검색하거나 새로 만들지 않는다.
- 여러 농경지가 같은 source를 공유할지는 씬 배선으로 명시한다.

### 5.7 `FarmWorkSite`

경로:

`Assets/Scripts/System/Farming/FarmWorkSite.cs`

`MonoBehaviour, IFarmWorkProvider`로 구현한다.

Serialized dependencies:

- `FarmProductionDefinition _definition`
- `WarehouseInventory _warehouse`
- `SeededRandomSource _randomSource`

Runtime state:

- `FarmWorkPhase _phase`
- `float _currentProgress`

Read-only 관찰 API:

- `Phase`
- `CurrentProgress`
- `MaxProgress`
- `NormalizedProgress`
- 필요 시 `WorkApplied` 또는 `ProgressChanged` event

초기화 시 필수 참조를 검증한다. 참조가 빠졌거나 definition 값이 유효하지 않으면 `CanApplyWork == false`로 두고 원인을 한 번만 명확히 기록한다.

#### Growing 처리

```text
delta = GrowthPerWork × workerEfficiency
CurrentProgress = min(MaxProgress, CurrentProgress + delta)
if CurrentProgress >= MaxProgress:
    Phase = Harvesting
```

이 작업에서는 생산량이 0이다.

#### Harvesting 처리

```text
yield = RandomSource.NextInclusive(MinimumYield, MaximumYield)
Warehouse.TryAdd(OutputItemId, yield)
  실패 → 게이지와 단계 유지, TryApplyWork 실패
  성공 → CurrentProgress = max(0, CurrentProgress - HarvestProgressPerWork × workerEfficiency)
if CurrentProgress <= 0:
    Phase = Growing
```

창고 추가 성공 전에 게이지를 감소시키지 않는다. 입고 실패로 생산물이 사라지는 상황을 만들지 않는다.

Unity main thread에서 호출되므로 여러 Farmer가 같은 frame에 완료하더라도 각 호출은 순서대로 처리된다. 하나의 `TryApplyWork` 안에서 단계·게이지·창고 결과를 일관되게 갱신한다.

## 6. 기존 파일 변경

### 6.1 `ActionContext.cs`

경로:

`Assets/Data/Struct/ActionContext.cs`

- nullable `IFarmWorkProvider FarmWorkProvider` property를 추가한다.
- 생성자 끝에 optional parameter로 추가하여 기존 호출부를 깨뜨리지 않는다.
- 이는 FarmingAction에 필요한 하나의 명시적 capability다. 이후 role별 service가 계속 추가된다면 role-specific context 분리를 별도 검토하고, 이번에 범용 service locator로 확장하지 않는다.

### 6.2 `DestinationDB.cs`

경로:

`Assets/Scripts/System/Lib/DestinationDB.cs`

- `DestinationInfo`에 Farm 전용 serialized field를 추가하지 않는다.
- 기존 `DestinationInfo.DestinationObject`에 붙은 `FarmWorkSite`를 초기화 시 한 번 찾아 별도 dictionary에 캐시한다.
- `TryGetFarmWorkProvider(BuildingType, out IFarmWorkProvider)`를 제공한다.
- 매 action 또는 매 Tick마다 `GetComponent`하지 않는다.
- Farm이 아닌 Pub/Well/Inn/GuardPost에는 Farm 전용 Inspector field가 생기지 않는다.

### 6.3 `FarmerActionSelector.cs`

경로:

`Assets/Scripts/System/Actor/FarmerActionSelector.cs`

Work intent의 의사결정과 반복 횟수 계산은 변경하지 않는다.

Work용 `ActionContext`를 만들 때:

1. `DestinationDB.TryGetFarmWorkProvider(decision.DestinationKey, out provider)`를 호출한다.
2. provider가 존재하고 `CanApplyWork`이면 Context에 전달한다.
3. provider가 없거나 사용할 수 없으면 FarmingAction 큐를 만들지 않고 안전한 Idle 1회를 반환하며 원인을 진단한다.

이 변경은 새로운 행동 우선순위를 만드는 것이 아니라 selector가 선택한 시설의 실행 capability를 context에 배선하는 최소 통합이다.

### 6.4 `FarmingAction.cs`

경로:

`Assets/Scripts/System/Action/FarmingAction.cs`

- `Start()`에서 `actionContext.FarmWorkProvider`를 한 번 검증하고 private field에 캐시한다.
- `Tick()`에서 provider cast, `GetComponent`, scene lookup을 하지 않는다.
- 작업 시간이 끝나면 `TryApplyWork(1f, out result)`를 정확히 한 번 호출한다.
- 농경지 작업 성공 후에만 기존 Hunger/Thirst/Fatigue 비용을 적용하고 `Complete()`한다.
- provider 호출이 실패하면 NPC 비용과 농경지 상태를 추가 변경하지 않고 `Fail(reason)`한다.
- `Clear()`에서 provider와 마지막 결과 등 pooled runtime state를 초기화한다.
- 기존 `Debug.Log("Work is done!")`는 삭제하거나 phase/progress/production 결과를 포함한 개발용 로그로 교체한다. 반복 작업마다 무조건 문자열을 생성하는 로그는 기본 비활성화한다.

단계 전환 또는 수확 완료만으로 `RequestReplan()`하지 않는다. 현재 반복 Work queue는 남은 행동을 계속 수행한다.

## 7. 변경하지 않는 파일과 시스템

다음 영역은 이번 구현 범위에서 변경하지 않는다.

- `DestinationDecider.cs`의 Utility 수식, Work 후보, 반복 횟수 계산
- `NPCDecision`, `NPCIntent`, `WorkerNPC`, `IAction`, `ActionResult` 계약
- `ActionPool`의 FarmingAction 생성 방식
- `FarmingActionCost`의 NPC 욕구 비용 필드
- `IInteractionProvider`, `InteractRequest`, `InteractResult`, Pub/Well 소비 흐름
- `NPCStat`, `FarmStat`, Farmer 숙련도와 레벨업
- 창고 capacity, 주민 carry inventory, 운반 및 Deposit action
- 실제 작물 종류 전환, 씨앗·비료·계절·날씨·토질
- 농경지 예약, 동시 작업자 제한
- 농경지 화면 UI와 애니메이션
- 저장/불러오기

## 8. 장기 구조와 이번 직접 입고의 관계

`ARCHITECTURE.md`의 장기 목표는 다음 흐름이다.

```text
생산 → 주민 carry → 창고로 이동 → Deposit transaction
```

이번 계획은 농경지 게이지 골격을 빠르게 검증하기 위해 다음 임시 흐름만 구현한다.

```text
수확 작업 → FarmWorkSite → WarehouseInventory 직접 입고
```

따라서 `FarmWorkSite`의 창고 의존은 현재 vertical slice의 명시적 절충이다. 추후 운반 시스템을 도입할 때 수확 결과의 목적지만 `WarehouseInventory`에서 주민 carry capability로 교체하고, 농경지 단계 및 게이지 규칙은 유지할 수 있어야 한다.

## 9. 씬 및 에셋 배선

구현 후 Unity Editor에서 다음을 수행한다.

1. `FarmProductionDefinition` asset을 생성하고 테스트 값을 입력한다.
2. Farm의 `DestinationInfo.DestinationObject`로 지정된 GameObject에 `FarmWorkSite`를 부착한다.
3. 창고 역할을 할 GameObject에 `WarehouseInventory`를 부착한다.
4. scene composition용 GameObject에 `SeededRandomSource`를 부착하고 seed를 정한다.
5. `FarmWorkSite`에 definition, warehouse, random source를 할당한다.
6. `DestinationDB`의 Farm row가 올바른 Farm `DestinationObject`를 참조하는지 확인한다.
7. `OutputItemId`는 현재 Item data에서 충돌하지 않는 실제 농산물 ID로 지정한다.

`DestinationInfo` 배열의 serialized schema는 변경하지 않으므로 기존 Pub/Well/Inn/GuardPost row의 재배선은 필요하지 않아야 한다.

## 10. 테스트 지원

### 권장 TestOnly 도구

신규 파일:

`Assets/TestOnly/TestFarmProductionWindow.cs`

Scene에 Farmer를 소환하지 않고 골격을 검증할 수 있는 작은 `OnGUI` 도구를 추가한다.

필드:

- `FarmWorkSite`
- `WarehouseInventory`
- 관찰할 `OutputItemId`

기능:

- `Apply Work Once`
- `Apply Work 10 Times`
- 현재 phase 표시
- 현재 progress / max / normalized progress 표시
- 현재 창고 item 수량 표시
- 마지막 `FarmWorkResult` 표시

TestOnly 도구는 gameplay state를 소유하지 않고 public API만 호출한다.

## 11. 검증 시나리오

### 정적 검증

1. `dotnet build Assembly-CSharp.csproj --no-restore`가 warning/error 없이 성공한다.
2. 신규 `.cs`가 Unity refresh 전 command-line build에 필요하면 `Assembly-CSharp.csproj`에 compile include를 추가한다.
3. 신규 Unity asset/script에는 `.meta`가 생성되어 있다.
4. `UnityEngine.Random` 직접 호출이 신규 코드에 없다.
5. `FarmProductionDefinition` 또는 다른 ScriptableObject에 runtime progress/warehouse quantity mutation이 없다.
6. `FarmingAction.Tick()`에 반복 `GetComponent`나 scene search가 없다.

### 결정적 기능 검증

초기값 `Max=100`, `Growth=10`, `HarvestDecrease=10`, `Yield=4..6` 기준:

1. 최초 phase는 `Growing`, progress는 0이다.
2. Work 9회 후 `Growing`, progress 90이다.
3. Work 10회 후 `Harvesting`, progress 100이며 창고 생산량은 0이다.
4. Work 11회째 progress가 90으로 감소하고 창고에 4~6개가 추가된다.
5. 수확 Work 10회 후 progress가 0이 되고 phase가 `Growing`으로 돌아온다.
6. 수확 작업 횟수만큼 창고가 증가하며 각 증가량은 항상 4~6 범위다.
7. 같은 seed와 같은 작업 순서에서 생산량 sequence가 동일하다.
8. 여러 Farmer 또는 TestOnly 호출이 같은 FarmWorkSite를 사용하면 하나의 게이지에 누적된다.
9. 잘못된 definition, warehouse 또는 random source 참조에서는 상태나 창고가 일부만 변경되지 않는다.

### 실제 Farmer Play Mode 검증

1. Farmer 한 명을 생성하고 Farm에서 Work하도록 한다.
2. FarmingAction 완료 횟수와 농경지 게이지 증가 횟수가 1:1인지 확인한다.
3. 한 decision에서 여러 FarmingAction이 큐에 들어가도 각 완료마다 정확히 한 번만 적용되는지 확인한다.
4. 성장 완료 후 다음 FarmingAction부터 창고 수량이 증가하는지 확인한다.
5. Farmer가 욕구 행동으로 재계획하거나 action이 취소됐을 때 완료되지 않은 FarmingAction이 게이지를 변경하지 않는지 확인한다.
6. 모든 수확 작업이 끝난 뒤 다시 성장 단계로 순환하는지 확인한다.

## 12. 구현 순서

1. git status를 확인하고 기존 사용자 변경 파일을 식별한다.
2. 관련 문서와 현재 코드를 다시 읽고 이 계획의 전제가 달라지지 않았는지 확인한다.
3. `FarmProductionDefinition`, phase/result/provider 계약을 구현한다.
4. `IInventory`와 최소 `WarehouseInventory`를 구현한다.
5. `IRandomSource`와 결정적 구현을 추가한다.
6. `FarmWorkSite`의 성장·수확 state machine과 atomic warehouse add를 구현한다.
7. `DestinationDB`에 provider 초기 캐시와 Try API를 추가한다.
8. `ActionContext`, `FarmerActionSelector`, `FarmingAction`을 최소 연결한다.
9. TestOnly window를 추가한다.
10. command-line build, grep 검증, diff 검토를 수행한다.
11. Unity scene 수동 배선 항목과 Play Mode 검증 결과를 보고한다.
12. 프로젝트 skill 절차에 따라 독립 Codex review agent를 실행하고 `PublicMD/PROGRESS.md`를 갱신한다.

## 13. 완료 기준

- 농경지 runtime state가 `Growing → Harvesting → Growing`으로 반복된다.
- 완료된 FarmingAction 하나가 농경지 작업 한 번만 적용한다.
- 성장 중에는 창고가 증가하지 않는다.
- 수확 중에는 매 작업마다 설정 범위의 생산량이 창고에 추가되고 게이지가 감소한다.
- 수확물이 창고에 들어가지 못한 경우 게이지가 감소하거나 생산물이 소실되지 않는다.
- 기존 Farmer 욕구 판단, 이동, Work 반복 횟수와 소비 행동이 유지된다.
- Farmer 숙련도 없이도 동작하며, 추후 `workerEfficiency`를 통해 확장할 경계가 존재한다.
- TestOnly 도구 또는 실제 Farmer Play Mode에서 phase, progress, 생산량을 관찰할 수 있다.
- C# build와 관련 검증이 실제 결과와 함께 보고된다.

## 14. 구현 중 중단해야 하는 조건

다음 조건이 발견되면 임의로 범위를 넓히지 말고 사용자에게 보고한다.

- 창고 capacity나 주민 운반을 이번 구현에 반드시 포함해야만 기존 시스템과 연결되는 경우
- 실제 Item ID source가 아직 없어 임시 ID를 코드에 하드코딩해야 하는 경우
- `DestinationDB`가 계획과 달리 Farm의 `DestinationObject`를 안전하게 제공하지 않는 경우
- Farm capability 누락을 안전한 Idle로 처리할 수 없어 무한 Fail/Replan이 생기는 경우
- 숙련도 또는 FarmStat을 지금 추가해야만 작업량을 표현할 수 있는 구조적 blocker가 있는 경우
- 기존 사용자 변경 파일과 충돌하여 덮어쓰기 없이는 구현할 수 없는 경우
