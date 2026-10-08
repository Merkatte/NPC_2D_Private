# Decision Policy

## 기능 목적

NPC stat, 현재 위치, destination과 provider option을 비교해 하나의 semantic decision을 반환하는 직업 중립 판단 계층을 설명한다.

## 책임 경계

- `DestinationDecider`는 후보 생성, 안전 filtering, utility 계산, bounded look-ahead를 소유한다.
- 공통 `TrySelectNearest`는 DestinationDB의 등록 후보/provider 조회를 받아 활성·CanInteract 가용성과 anchor 제곱거리를 판단한다. utility 후보, Farmer 물류와 Guard fallback이 이 정책을 재사용하며 registry는 시설 선택을 소유하지 않는다.
- 긴급 욕구 여부의 정의도 decider가 소유한다. selector는 `HasCriticalNeed(stat)`으로 같은 임계 판정을 재사용하고 자체 임계값을 두지 않는다.
- `NPCDecision`은 선택된 DestinationInfo/Provider identity, “한 목적지에서 한 행동”과 Work 반복 횟수를 표현한다.
- selector는 결과를 queue로 변환할 뿐 점수 공식을 다시 계산하지 않는다.
- runtime action duration과 예측용 duration은 같은 값으로 간주하지 않는다.

## 현재 실행 흐름

```text
DestinationDecider.Decide(stat, role, position, work cost)
  -> destination/provider에서 유효 후보 수집
  -> 위험한 후보 hard filter
  -> travel + action time + risk + terminal state utility
  -> 최대 3 depth bounded look-ahead
  -> 공급 / role activity / Idle 중 하나 선택
  -> NPCDecision(intent, destination, repeat count, request)
```

공급 후보는 provider가 제공한 item별 `InteractionOption`을 사용한다. Work는 bounded repeat batch를 반환할 수 있지만 공급 action은 한 번 실행한 뒤 실제 상태로 다시 판단한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs` | 위험·시간·보상·look-ahead의 공유 판단 tuning |
| `Assets/Data/Struct/NPCDecision.cs` | decider가 selector에 반환하는 단일 semantic decision 값 |
| `Assets/Scripts/Enum/NPCIntent.cs` | Work·Eat·Drink·Sleep·Guard·Idle 의도 식별 |
| `Assets/Scripts/Interface/IRandomSource.cs` | 재현 가능한 gameplay 난수 계약 |
| `Assets/Scripts/System/Lib/DestinationDecider.cs` | 직업 중립 후보 생성과 bounded look-ahead utility 정책 |
| `Assets/Scripts/System/Lib/SeededRandomSource.cs` | seed 기반 inclusive 정수 난수 구현 |

## 변경 유형별 최소 확인 범위

| 변경 | 최소 파일·문서 |
|---|---|
| utility 식·위험 곡선 | `DestinationDecider.cs`, `NPCDecisionTuning.cs`, `NPCDecision.cs` |
| 후보 destination·provider | `DestinationDecider.cs`, [Interaction and Destinations](../Interaction_and_Destinations.md) |
| Farmer decision 변환 | `DestinationDecider.cs`, [Selector and Queue](Selector_and_Queue.md) |
| 긴급 욕구 임계 | `DestinationDecider.cs`(`HasCriticalNeed`), `NPCDecisionTuning.cs` |
| Guard의 공급·경비 판단 | `DestinationDecider.cs`, [Guard](../Combat/Guard.md) |
| 결정적 난수 | `IRandomSource.cs`, `SeededRandomSource.cs`, 난수를 소비하는 기능 문서 |

## 불변 규칙

- 같은 seed, 입력, 후보 순서에서는 같은 결과를 내야 한다.
- decider는 `IAction`, queue, `ActionPool`, mutable NPC state를 소유하지 않는다.
- 모든 후보는 같은 utility 단위에서 비교한다.
- hard safety filter와 soft risk penalty의 의미를 섞지 않는다.
- 미래 상태는 예측값이며 실제 stat을 미리 변경하지 않는다.
- Guard duty 후보는 GuardPost의 등록 위치와 `ActionType.Guard` provider 가용성을 모두 요구한다. 사망하거나 구성되지 않은 초소는 후보가 되지 않으며, 후보 평가 중 순찰 난수는 소비하지 않는다.

## 검증 도구와 제약

- `Assets/TestOnly/TestDecisionScenarioProbe.cs`: 고정 시나리오와 반복 결정성 검증.
- `Assets/TestOnly/DecisionSmokeItemData.csv`: 공급 option용 smoke data.
- need·시간·보상 수치는 아직 prototype placeholder이며 실제 밸런스로 간주하지 않는다.

## 관련 문서

- [Selector and Queue](Selector_and_Queue.md)
- [Interaction and Destinations](../Interaction_and_Destinations.md)
- [Farming](../Farming/README.md)
- [Guard](../Combat/Guard.md)

## 문서 갱신 조건

후보 모델, utility, look-ahead, tuning, decision shape 또는 gameplay 난수 계약이 바뀌면 갱신한다.

다중 시설에서는 등록된 출입/interaction anchor의 제곱거리로 가용 시설을 선택한다. 동률은 등록 순서다. 실제 선택된 provider를 결과에 보존하며 selector에서 BuildingType으로 다시 조회하지 않는다. Farm/Guard 작업 위치 난수는 시설 선택이 확정된 뒤에만 소비한다. look-ahead와 후보 비교는 난수 상태를 변경하지 않는다.

## 불만도·태업 연결

NeedSnapshot은 IStatView.IsOnStrike를 복사한다. BuildCandidates는 태업이면 role activity를 생성하지 않으며 모든 look-ahead 상태에도 이 값을 유지한다. 공급 후보·긴급 need filtering·utility는 기존 규칙을 사용한다. 미래 회복을 추정해 업무 후보를 만들지 않고 실제 회복된 다음 선택에서 다시 판단한다. 불만 mutation이나 원인 조회는 decider 책임이 아니다.

## 주거 판단 연결

DecideNeeds는 같은 scorer를 사용하면서 role activity를 제외한다. allowSleep=false인 집 생활 판단은 Sleep 후보를 최초와 모든 look-ahead에서 제외한다. NeedSnapshot이 두 제한과 선택적 Sleep 회복 속도를 보존하므로 미래 단계에서도 금지 후보를 추가하지 않는다. 주거 주민의 일반 role 판단에는 여관 회복 속도를 명시적으로 전달하며 예상 시간은 현재 피로/회복 속도다. 미연결 호출은 기존 EstimatedSleepSeconds를 사용한다. decider는 주택 객체·거주 상태·queue를 소유하지 않는다.
