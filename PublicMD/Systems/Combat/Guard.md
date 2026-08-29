# Guard

## 기능 목적

Guard의 전투 우선순위, 공급 판단, 경비 지점 순찰과 role 전용 stat·cost를 설명한다.

## 현재 실행 흐름

```text
GuardActionSelector
  -> 유효한 combat target이 있거나 새 후보 획득
     -> Move? -> Attack
  -> target이 없으면 DestinationDecider
     -> Eat/Drink/Sleep 또는 Guard duty

GuardAction
  -> 원형 patrol point 이동
  -> 시간당 hunger/thirst/fatigue 증가
  -> enemy 감지 또는 need interrupt threshold에서 ReplanRequested
```

Guard selector가 target 유지·획득 정책과 combat queue를 소유한다. 공통 perception과 attack 실행은 별도 leaf 문서가 소유한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | 순찰 형태, need 증가, interrupt와 접근 거리 tuning |
| `Assets/Data/ScriptableObject/Script/GuardStatDefinition.cs` | `GuardStat` runtime instance 생성 definition |
| `Assets/Scripts/Interface/IGuardStatView.cs` | combat stat에 guard radius를 추가한 read model |
| `Assets/Scripts/System/Action/GuardAction.cs` | 장기 순찰, need 증가, 감지·threshold replan 실행 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | 전투 우선, 공급, 경비 queue를 구성하는 Guard policy |
| `Assets/Scripts/System/Actor/GuardStat.cs` | Guard combat 능력과 guard radius runtime stat |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| 전투 우선순위·target 유지 | `GuardActionSelector.cs`, [Targeting and Perception](Targeting_and_Perception.md) |
| 공격 queue·접근 거리 | `GuardActionSelector.cs`, [Attack Runtime](Attack_Runtime.md), [Movement](../NPC_Decision_and_Actions/Movement.md) |
| 순찰 경로·중단 | `GuardAction.cs`, `GuardActionCost.cs`, `GuardStat.cs` |
| 공급·경비 utility | `GuardActionSelector.cs`, [Decision Policy](../NPC_Decision_and_Actions/Decision_Policy.md) |
| Guard stat asset | `GuardStatDefinition.cs`, `GuardStat.cs`, scene creation entry |

## 불변 규칙

- combat target은 공급·순찰보다 즉시 높은 우선순위를 가진다.
- `GuardAction`은 고정 duration으로 완료되지 않고 감지·need threshold에서 재판단한다.
- guard duty의 decision 평가 duration은 runtime action duration이 아니다.
- selector는 `GuardStat` 호환성과 perception 존재를 검증한다.
- 순찰 이동은 `NPCComponent.Move()`를 통해 공통 animation 경로를 사용한다.

## Unity 배선과 검증

`NPCGirl.prefab`의 `CombatPerception`과 Hostile mask sensor가 필요하다. `GuardTest.unity`의 Guard selector, action pool, destination, cost/tuning, creation entry 배선을 함께 검증한다.

## 알려진 제약과 TBD

- Guard는 공유 NPC prefab을 사용하며 전용 art/presentation 분리는 없다.
- 일반 NPC가 공격받는 target이 되는 정책은 아직 결정되지 않았다.

## 관련 문서

- [Targeting and Perception](Targeting_and_Perception.md)
- [Attack Runtime](Attack_Runtime.md)
- [Decision Policy](../NPC_Decision_and_Actions/Decision_Policy.md)
- [Spawning and Pooling](../Spawning_and_Pooling.md)

## 문서 갱신 조건

Guard 우선순위, 순찰, interrupt, stat·cost 또는 scene 배선이 바뀌면 갱신한다.
