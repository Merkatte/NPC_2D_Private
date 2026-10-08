# Needs Actions

## 기능 목적

Eat, Drink, Sleep, Idle, Wander의 실행 규칙을 설명한다. 어떤 행동을 선택할지는 decision policy가, provider transaction은 interaction 시스템이 소유한다.

## 현재 실행 흐름

```text
Eat/Drink
  -> BaseBuildingAction.Start(): inside 표현
  -> provider.TryInteract(request)
  -> InteractionResult.StatEffect 적용
  -> inside 표현 정리 후 완료

Sleep
  -> inside 표현
  -> timer 경과
  -> fatigue 회복

Idle
  -> 짧은 안전 대기 후 완료
```

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Action/DrinkAction.cs` | Drink provider transaction과 stat effect 적용 |
| `Assets/Scripts/System/Action/EatAction.cs` | Eat provider transaction과 stat effect 적용 |
| `Assets/Scripts/System/Action/IdleAction.cs` | queue가 비거나 fallback일 때 짧은 안전 대기 |
| `Assets/Scripts/System/Action/SleepAction.cs` | 선택적 초당 회복과 기존 시간 후 회복을 지원하는 실내 action |
| `Assets/Scripts/System/Action/WanderAction.cs` | 배회 이동·휴식·욕구 변화·실행 수명 |
| `Assets/Data/ScriptableObject/Script/WanderActionCost.cs` | 이동/휴식 시간과 욕구 증가 수치 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| Eat·Drink 효과 | 해당 action, [Interaction and Destinations](../Interaction_and_Destinations.md), [Inventory and Items](../Inventory_and_Items.md) |
| Sleep 시간·회복 | `SleepAction.cs`, [Decision Policy](Decision_Policy.md) 예측 duration |
| Idle 시간 | `IdleAction.cs`, [Decision Policy](Decision_Policy.md) Idle 평가 |
| 실내 표현 정리 | 대상 action, `BaseBuildingAction.cs`, [NPC Presentation](../NPC_Presentation.md) |

## 불변 규칙

- Eat·Drink는 selector가 선택한 provider와 request만 사용한다.
- provider transaction 실패는 stat을 변경하지 않고 `Failed`로 끝난다.
- 완료·실패·취소·재판단·pool 반환 모든 경로에서 inside 표현을 정리한다.
- runtime duration과 decision의 예상 duration이 다른 의미라면 이름과 문서에서 구분한다.

## 관련 문서

- [Decision Policy](Decision_Policy.md)
- [Action Runtime](Action_Runtime.md)
- [Interaction and Destinations](../Interaction_and_Destinations.md)
- [NPC Presentation](../NPC_Presentation.md)

## 문서 갱신 조건

생활 action의 transaction, timing, stat effect, fallback 또는 건물 표현 lifecycle이 바뀌면 갱신한다.

## 불만도·태업 연결

WanderAction은 주민 공통 배회 실행이다. selector가 도달 가능한 목표를 선택하면 NPCPathFollower로 최대 5초 이동하고 1초 쉰다(기존 WanderActionCost 기본값). 빠르게 도착하면 바로 휴식하며 이동·휴식의 허기/갈증/피로 비용 0.3/초를 유지한다. 경로 실패는 재판단하고 Stop/Clear는 follower·timer·context를 정리한다. 태업 중 허용되며 회복해도 이번 구간을 마친 뒤 재판단한다. 배회 실행 변경은 WanderAction/WanderActionCost와 [Movement](Movement.md)를 먼저 확인한다. 공유 SO에는 actor 상태를 쓰지 않는다.

## 선택적 여관 회복

주거 연결 주민의 SleepAction은 ConfigureRecovery로 받은 HousingLifeSettings의 여관 속도(초기 10/초)를 적용해 피로 0에서 완료한다. Clear는 설정과 타이머를 제거하므로 다른 씬/주민으로 설정이 새지 않는다. 설정 없는 기존 호출은 2초 후 완전 회복한다. HomeStayAction은 [Housing Life](../Housing/Life.md)가 주 소유하며 집의 기본 피로 회복은 별도다.
