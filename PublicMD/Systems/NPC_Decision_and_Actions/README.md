# NPC Decision and Actions

## 기능 목적

NPC가 다음 행동을 판단하고, 실행 가능한 action queue를 구성하며, action을 실행·재사용하는 전체 영역의 인덱스다. 이 문서는 세부 구현이나 전체 스크립트 목록을 소유하지 않고 현재 작업에 필요한 하위 문서를 선택하게 한다.

## 세부 기능 라우팅

| 변경하려는 내용 | 읽을 문서 |
|---|---|
| utility 점수, 위험도, look-ahead, `NPCDecision` | [Decision Policy](Decision_Policy.md) |
| role selector, queue 조립, action 대여·반환 | [Selector and Queue](Selector_and_Queue.md) |
| `IAction`, lifecycle, result, context, cost base | [Action Runtime](Action_Runtime.md) |
| 고정·동적 목표 이동, stopping distance | [Movement](Movement.md) |
| Eat, Drink, Sleep, Idle 실행 | [Needs Actions](Needs_Actions.md) |

## 전체 흐름

```text
role selector
  -> DestinationDecider 또는 role 전용 우선순위
  -> NPCDecision
  -> ActionPool에서 action 대여
  -> ActionContext로 실행 dependency 주입
  -> queue 반환
  -> WorkerNPC가 action lifecycle 실행
```

## 세부 기능 간 의존 관계

```text
Decision Policy -> semantic decision
Selector and Queue -> decision을 action sequence로 변환
Action Runtime -> 공통 실행 계약과 상태
Movement / Needs Actions -> 계약을 구현하는 leaf action
NPC Runtime -> 완성된 queue를 소비
```

## 공유 불변 규칙

- 판단은 action instance를 생성하거나 실행하지 않는다.
- selector는 utility 공식을 재구현하지 않고 판단 결과를 queue로 번역한다.
- action은 다음 장기 목표를 결정하거나 scene service를 직접 검색하지 않는다.
- queue 구성 중 하나라도 실패하면 이미 대여한 action을 전부 반환한다.
- pooled action은 `Clear()` 뒤 다른 NPC가 이전 context를 볼 수 없어야 한다.

## 외부 기능 연결

- Farmer의 농사 실행: [Farming](../Farming.md)
- Guard·Enemy의 role 우선순위와 전투 queue: [Combat](../Combat/README.md)
- 목적지와 provider: [Interaction and Destinations](../Interaction_and_Destinations.md)
- queue 소비: [NPC Runtime](../NPC_Runtime.md)

## 알려진 제약과 TBD

- need와 utility tuning은 아직 prototype balance다.
- 새 role이 공통 decider를 사용할지 자체 판단만 사용할지는 role 책임에 따라 결정한다.

## 문서 갱신 조건

세부 기능 수, 기능 간 의존 방향, 작업 라우팅이 바뀌면 이 인덱스를 갱신한다. 세부 클래스 변경은 해당 leaf 문서만 갱신한다.
