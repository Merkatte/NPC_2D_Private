# Needs Actions

## 기능 목적

Eat, Drink, Sleep, Idle의 실행 규칙을 설명한다. 어떤 행동을 선택할지는 decision policy가, provider transaction은 interaction 시스템이 소유한다.

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
| `Assets/Scripts/System/Action/SleepAction.cs` | 일정 시간 뒤 fatigue를 회복하는 실내 action |

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
