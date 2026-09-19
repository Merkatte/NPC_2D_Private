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
  -> 선택된 GuardPost provider의 사각형 영역 안에서 랜덤 목적지 반복 이동
  -> 시간당 hunger/thirst/fatigue 증가
  -> enemy 감지 또는 need interrupt threshold에서 ReplanRequested
```

Guard selector가 target 유지·획득 정책과 combat queue를 소유한다. 공통 perception과 attack 실행은 별도 leaf 문서가 소유한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | 순찰 도착 거리, need 증가, interrupt와 접근 거리 tuning |
| `Assets/Data/ScriptableObject/Script/GuardStatDefinition.cs` | `GuardStat` runtime instance 생성 definition |
| `Assets/Scripts/System/Action/GuardAction.cs` | 장기 순찰, need 증가, 감지·threshold replan 실행 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | 전투 우선, 공급, 경비 queue를 구성하는 Guard policy |
| `Assets/Scripts/System/Actor/GuardStat.cs` | ICombatStatView를 구현하는 Guard combat runtime stat |
| `Assets/Scripts/Actor/GuardPost.cs` | 순찰 위치 제공과 초소 체력·사망·가용성 소유 |
| `Assets/Scripts/Actor/TemporaryGameOverReporter.cs` | 초소 Died 이벤트에서 임시 GameOver 로그를 한 번 출력 |

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
- selector는 GuardStat으로 역할 호환성을 확인하고 초소 provider와 첫 위치를 주입한다. GuardAction은 같은 provider에서 도착마다 다음 위치를 요청한다. 다른 시설이나 다음 semantic 행동을 선택하지 않는다.
- 위치는 BoxCollider2D 로컬 offset/size에서 전용 SeededRandomSource로 추출한 뒤 TransformPoint한다. 비활성 collider는 영역 데이터로만 사용하며 피격 범위가 아니다.
- 이동량은 남은 거리로 제한한다. 영역 누락·초소 사망 시 임의 위치 fallback 없이 timed Idle/재판단한다. 고정 4점·GuardRadius·IGuardStatView는 제거했다.
- GuardPost는 현재 체력의 단일 owner이며 CombatTarget.Initialize(this)로 기존 피격 경로를 사용한다. 체력 0에서 visual만 숨기고 루트는 보존한다. 부활 정책은 없다.
- Friendly 레이어의 별도 피격 collider를 사용하며, 경비 후보는 provider.CanInteract로 사망한 초소를 제외한다. 체력 기본 최대값은 100이다.

## Unity 배선과 검증

`NPCGirl.prefab`의 `CombatPerception`과 Hostile mask sensor를 재사용한다. FarmerTest에는 Guard selector/stat/cost 등록, 별도 GuardPost와 순찰 영역, 수동 Enemy 스포너가 배선된다. GuardTest의 기존 PatrolArea transform을 유지하고 영역 collider/provider를 연결한다. 자동 적 생성은 끈다.

초소 사망 GameOver는 임시 통합 로그이며 기존 기획의 시청 파괴 최종 패배를 대체하지 않는다. 게임 진행 정지·재시작·주민 피격·범용 길찾기는 구현하지 않는다. Play Mode 시나리오는 사용자 지시로 NOT_VERIFIED다.

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
