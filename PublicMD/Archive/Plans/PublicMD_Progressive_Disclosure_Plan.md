# PublicMD 점진적 공개 구조 전환 계획

> 승인일: 2026-08-29
> 상태: 구현 완료
> 이 문서는 전환 당시의 결정 기록이다. 현재 문서 읽기 규칙은 `PublicMD/README.md`와 `PublicMD/ProjectStructure.md`를 따른다.

## 목표

에이전트가 큰 공통 문서와 관련 없는 코드를 반복해서 읽지 않도록 다음 탐색 구조를 만든다.

```text
AGENTS.md
  -> ProjectStructure.md
    -> 기능 문서 또는 기능 README
      -> 세부 기능 leaf
        -> 변경 유형별 최소 스크립트
```

## 승인된 결정

- 최상위 기능 영역은 9개로 유지한다.
- 스크립트 수가 아니라 독립적으로 변경 가능한 세부 기능 수로 문서를 분할한다.
- 자체 흐름, 별도 변경 진입점, 독립 변경 가능성을 모두 가진 세부 기능이 4개 이상이면 폴더형 인덱스를 사용한다.
- 판단·action은 5개 leaf, 전투는 4개 leaf로 분할한다.
- 상위 기능 README는 선택과 의존 관계만 설명하고 스크립트 목록을 갖지 않는다.
- production C#은 정확히 한 leaf 문서에서만 주 소유하고 한 줄 책임을 기록한다.
- leaf는 변경 유형별 최소 확인 파일을 안내한다.
- 기능 문서, 루트 문서, AGENTS와 활성 구현·리뷰 스킬을 한 작업에서 동기화한다.
- 이전 루트 문서의 별도 사본은 만들지 않고 Git history로 보존한다.
- C# API, gameplay와 Unity 에셋은 변경하지 않는다.

## 구현 구조

```text
PublicMD/Systems/
├─ NPC_Runtime.md
├─ NPC_Decision_and_Actions/
│  ├─ README.md
│  ├─ Decision_Policy.md
│  ├─ Selector_and_Queue.md
│  ├─ Action_Runtime.md
│  ├─ Movement.md
│  └─ Needs_Actions.md
├─ Farming.md
├─ Combat/
│  ├─ README.md
│  ├─ Targeting_and_Perception.md
│  ├─ Attack_Runtime.md
│  ├─ Guard.md
│  └─ Enemy.md
├─ Interaction_and_Destinations.md
├─ Inventory_and_Items.md
├─ NPC_Presentation.md
├─ UI.md
└─ Spawning_and_Pooling.md
```

## 완료 기준

- production C# 전부가 하나의 주 소유 문서에 등록된다.
- 활성 Markdown link와 파일 경로가 모두 유효하다.
- 공통 문서는 지도·공통 원칙·범용 규칙만 소유한다.
- 구현·리뷰 지침은 변경 범위와 주 소유 leaf만 읽는다.
- `CLAUDE.md`와 `.claude`는 읽거나 수정하지 않는다.
- 사용자 production 코드와 Unity 에셋 변경을 보존한다.
