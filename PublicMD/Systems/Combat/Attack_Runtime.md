# Attack Runtime

## 기능 목적

선택된 target에 접근한 뒤 attack speed 주기로 damage를 적용하고 target 변화에 따라 완료 또는 재판단하는 공통 공격 실행을 설명한다.

## 현재 실행 흐름

```text
role selector
  -> CombatRange.IsInRange
  -> 필요하면 MoveAction(dynamic target)
  -> AttackAction
     -> ICombatStatView와 CombatRuntimeState 검증
     -> attack interval 누적
     -> target.ApplyDamage(AttackPower)
     -> target 사망·소실·범위 이탈 시 replan/complete
```

Enemy의 `AttackStyle`과 preferred range는 selector의 접근 거리 정책에 쓰인다. 실제 damage 적용 계약은 target이 소유한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Enum/AttackStyle.cs` | melee·ranged 공격 방식 식별 |
| `Assets/Scripts/Interface/ICombatStatView.cs` | 공격력·공격 속도·공격 범위 read model |
| `Assets/Scripts/System/Action/AttackAction.cs` | 선택 target에 대한 주기적 damage와 target 상태 처리 |
| `Assets/Scripts/System/Lib/CombatRange.cs` | Z축을 제외한 공통 2D 사거리 판정 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| damage·attack interval | `AttackAction.cs`, `ICombatStatView.cs`, 공격 role의 stat |
| range 판정 | `CombatRange.cs`, `AttackAction.cs`, role selector |
| melee/ranged 접근 거리 | `AttackStyle.cs`, [Enemy](Enemy.md), [Movement](../NPC_Decision_and_Actions/Movement.md) |
| target 사망·소실 | `AttackAction.cs`, [Targeting and Perception](Targeting_and_Perception.md) |

## 불변 규칙

- attack speed와 range가 유효하지 않으면 구성 오류로 실패한다.
- target의 현재 위치와 생존 상태를 매 attack tick에 검증한다.
- target 불가능 상태는 재판단 대상으로 취급하고, 무적 상태는 target을 유지한 채 damage만 차단한다.
- damage source와 target health source를 중복 보관하지 않는다.
- selector와 action이 동일한 `CombatRange` 규칙을 사용한다.
- `Clear()`에서 combat stat, timer와 cached reference를 모두 초기화한다.

## 관련 문서

- [Targeting and Perception](Targeting_and_Perception.md)
- [Guard](Guard.md)
- [Enemy](Enemy.md)
- [Action Runtime](../NPC_Decision_and_Actions/Action_Runtime.md)

## 문서 갱신 조건

공격 timing, damage, range, attack style 또는 combat stat 계약이 바뀌면 갱신한다.
