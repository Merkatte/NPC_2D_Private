# Farming Action

## 기능 목적

Farmer의 농사 interaction 1회와 NPC 작업 비용·working animation lifecycle을 실행한다.

## 책임 경계

- `FarmingAction`은 주입된 provider에 farming request를 한 번 실행하고 NPC 비용을 적용한다.
- `FarmingActionCost`는 농사 1회가 hunger·thirst·fatigue에 주는 공유 비용이다.
- action은 crop 종류, progress, yield와 crop visual을 소유하지 않는다.

## 현재 실행 흐름

```text
Start -> NPCComponent.SetWorking(true)
Tick until working duration
  -> provider.TryInteract(Farming request)
  -> 성공 시 NPCStat 비용 적용
Complete / Fail / Stop / Clear -> working 표현 정리
```

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Action/FarmingAction.cs` | 농사 interaction 1회와 working lifecycle 실행 |
| `Assets/Data/ScriptableObject/Script/FarmingActionCost.cs` | 농사 1회 NPC 욕구 비용 정의 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 작업 시간·완료·실패 | `FarmingAction.cs`, [Action Runtime](../NPC_Decision_and_Actions/Action_Runtime.md) |
| NPC 비용 | `FarmingAction.cs`, `FarmingActionCost.cs` |
| working animation | `FarmingAction.cs`, [NPC Presentation](../NPC_Presentation.md) |

## 불변 규칙

- action은 scene registry를 검색하거나 다음 행동을 선택하지 않는다.
- transaction 실패 시 NPC 비용을 적용하지 않는다.
- Complete·Fail·Stop·Clear 모든 종료 경로에서 working 표현을 정리한다.
- crop presentation Animator state를 직접 참조하지 않는다.

## Unity 배선과 검증

- `FarmingActionCost.asset`은 DataManager cost 목록과 Farmer selector에 연결된다.
- Farmer NPC Animator는 `IsWorking` bool parameter를 제공한다.

## 알려진 제약과 TBD

- `_workingTime`은 현재 action 내부 값이며 prediction tuning과 자동 동기화되지 않는다.

## 관련 문서

- [Farming index](README.md)
- [Runtime and Transactions](Runtime_and_Transactions.md)
- [NPC Presentation](../NPC_Presentation.md)

## 문서 갱신 조건

FarmingAction lifecycle, NPC 비용 또는 working 표현 호출이 바뀌면 갱신한다.
