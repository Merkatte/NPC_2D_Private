# Farming and Harvest Actions

## 기능 목적

Farmer의 농사·수확 interaction 1회와 NPC 작업 비용·working animation lifecycle을 실행한다.

## 책임 경계

- `FarmingAction`은 주입된 provider에 farming request를 한 번 실행하고 NPC 비용을 적용한다.
- `HarvestAction`은 주입된 provider에 cargo를 실은 harvest request를 한 번 실행하고 같은 NPC 비용을 적용한다.
- `FarmingActionCost`는 작업 1회가 hunger·thirst·fatigue에 주는 공유 비용이며 Farming과 Harvest가 함께 사용한다.
- action은 crop 종류, progress, yield, 봇짐 용량과 crop visual을 소유하지 않는다.
- 창고까지의 운반과 입고 실행은 [Inventory and Items](../Inventory_and_Items.md)의 `DepositAction`이 소유한다.

## 현재 실행 흐름

```text
Start -> BaseWorkingAction이 NPCComponent.SetWorking(true)
  -> provider/request 누락: Fail (selector 배선 오류)
  -> Harvest에 cargo 없음: Fail
  -> CanInteract false: RequestReplan (정상적인 환경 변화)
Tick until working duration (둘 다 3초)
  -> ProviderStillUsable() false: RequestReplan
  -> provider.TryInteract(request)
     -> false: RequestReplan
     -> true: NPCStat 비용 적용 후 Complete
Complete / Fail / RequestReplan / Stop / Clear -> working 표현 정리
```

`Fail`은 selector 배선이 깨진 경우에만 사용한다. 밭이 이미 다른 Farmer에게 수확되었거나 phase가 바뀐 것, 봇짐이 가득 차 수락량이 0인 것은 모두 정상 상태 변화이므로 `RequestReplan`으로 처리한다. Harvest는 부분 수락(`acceptedQuantity`가 yield보다 작음)도 실제 작업 성과로 보고 비용을 적용한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Action/FarmingAction.cs` | 농사 interaction 1회와 working lifecycle 실행 |
| `Assets/Scripts/System/Action/HarvestAction.cs` | cargo를 실은 수확 interaction 1회와 working lifecycle 실행 |
| `Assets/Data/ScriptableObject/Script/FarmingActionCost.cs` | 농사·수확 1회 NPC 욕구 비용 정의 |

`BaseWorkingAction`(working 표현 배관과 `ProviderStillUsable`)은 [Action Runtime](../NPC_Decision_and_Actions/Action_Runtime.md)이 소유한다.

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 작업 시간·완료·실패 | `FarmingAction.cs`, `HarvestAction.cs`, [Action Runtime](../NPC_Decision_and_Actions/Action_Runtime.md) |
| NPC 비용 | `FarmingAction.cs`, `HarvestAction.cs`, `FarmingActionCost.cs` |
| 수확물 목적지·부분 수락 | `HarvestAction.cs`, [Runtime and Transactions](Runtime_and_Transactions.md), [Inventory and Items](../Inventory_and_Items.md) |
| working animation | `BaseWorkingAction.cs`, [NPC Presentation](../NPC_Presentation.md) |

## 불변 규칙

- action은 scene registry를 검색하거나 다음 행동을 선택하지 않는다.
- transaction 실패 시 NPC 비용을 적용하지 않는다.
- Complete·Fail·ReplanRequested·Stop·Clear 모든 종료 경로에서 working 표현을 정리한다.
- 환경 변화는 `RequestReplan`, 필수 dependency 부재만 `Fail`로 보고한다.
- `Tick()`의 provider 재확인은 이미 캐싱된 provider 참조만 사용하고 scene search를 하지 않는다.
- crop presentation Animator state를 직접 참조하지 않는다.

## Unity 배선과 검증

- `FarmingActionCost.asset`은 DataManager cost 목록과 Farmer selector에 연결된다. Harvest는 별도 cost asset 없이 이 asset을 재사용한다.
- Farmer NPC Animator는 `IsWorking` bool parameter를 제공한다.
- `ActionPool`은 `Harvest`를 포함한 모든 `ActionType`을 prewarm하므로 새 action은 `ActionPool.Create`에 case를 함께 추가한다.

## 알려진 제약과 TBD

- `_workingTime`은 현재 action 내부 값이며 prediction tuning과 자동 동기화되지 않는다.
- Harvest 전용 animation clip은 없고 Farming과 같은 working 표현을 사용한다.
- Harvest는 queue당 1회만 대여되므로 decider의 `safeRepeats` batch 가정과 단위가 다르다. 욕구는 매 replan마다 재평가되므로 현재는 무해하다.
- Play Mode 검증은 2026-09-04 기준 `NOT VERIFIED`다.

## 관련 문서

- [Farming index](README.md)
- [Runtime and Transactions](Runtime_and_Transactions.md)
- [Inventory and Items](../Inventory_and_Items.md)
- [NPC Presentation](../NPC_Presentation.md)

## 문서 갱신 조건

Farming/Harvest action lifecycle, NPC 비용, 결과 보고 기준 또는 working 표현 호출이 바뀌면 갱신한다.
