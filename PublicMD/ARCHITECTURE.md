# NPC Work 2D Architecture

> 문서 기준일: 2026-08-29
> 이 문서는 여러 기능에 공통으로 적용되는 안정적인 구조 원칙과 의존 방향만 소유한다. 현재 기능별 클래스와 배선은 `PublicMD/Systems`를 따른다.

## 1. 목적

이 프로젝트는 Unity 2D 정착지 prototype으로, NPC가 자신의 runtime 상태와 주변 capability를 바탕으로 행동을 선택하고 pooled action을 실행한다. 구조의 목표는 기능을 늘릴 때 actor root, selector, action, provider와 UI가 서로의 세부 구현을 흡수하지 않게 하는 것이다.

전체 기능 지도는 [ProjectStructure](ProjectStructure.md), 실제 구현은 [Systems](Systems/README.md), 보편적 작성 규칙은 [CodeConvention](CodeConvention.md)을 기준으로 한다.

## 2. 핵심 설계 원칙

### 2.1 결정과 실행을 분리한다

```text
Policy/Selector: 무엇을 할지, 어떤 순서로 할지
Action: 선택된 행동을 어떻게 끝낼지
Actor runtime: queue와 현재 action을 언제 진행·폐기할지
```

- policy는 action instance, queue와 mutable actor state를 소유하지 않는다.
- selector는 점수 공식을 복제하지 않고 semantic decision을 queue로 번역한다.
- action은 다음 장기 목표를 고르거나 scene registry를 검색하지 않는다.
- actor root는 role별 우선순위와 domain transaction을 소유하지 않는다.

### 2.2 정의와 runtime 상태를 분리한다

- ScriptableObject: 여러 인스턴스가 공유하는 초기 definition, cost, tuning.
- plain runtime object: NPC별 stat, 현재 target, action timer.
- scene component: 농장 progress, 창고 수량, facility availability처럼 배치별 상태.

공유 asset에 현재 체력, target, progress, inventory 수량을 기록하지 않는다.

### 2.3 Unity object와 순수 규칙을 분리한다

- MonoBehaviour는 scene reference, lifecycle, physics, transform과 presentation adapter를 담당한다.
- 순수 계산은 가능한 한 명시적 입력과 작은 interface/value를 사용한다.
- Unity interface reference의 생존성은 backing `UnityEngine.Object`와 함께 검증한다.
- hot path에서 scene-wide search나 반복 component lookup을 하지 않는다.

### 2.4 의미가 같은 capability만 공유한다

공통 interface는 소비자가 concrete type을 몰라도 동일한 의미로 사용할 수 있는 최소 protocol이어야 한다. 미래 가능성만으로 일대일 interface를 만들거나 domain마다 provider 계약을 복제하지 않는다.

```text
IInteractionProvider -> 지원 조회, option, request 실행, result
IInventory           -> 수량 transaction
ICombatTarget        -> 생존, 위치, damage
IHoverInfoSource     -> UI가 읽을 표시 정보
```

domain별 phase, recipe, item table, target 정책은 concrete owner가 유지한다.

## 3. 조립과 runtime 경계

### 3.1 생성

scene composition root가 role, selector, stat definition과 prefab/pool을 조립한다. spawn 전에 selector와 runtime stat의 호환성을 검사하고, actor root에는 이미 조립된 dependency를 전달한다.

세부 구조: [Spawning and Pooling](Systems/Spawning_and_Pooling.md)

### 3.2 Action lifecycle

```text
Init -> Start -> Tick* -> Completed | ReplanRequested | Failed
                         cancellation -> Stop
Return -> Clear -> pool
```

- `Completed`: 선택 행동이 정상 완료되어 다음 queue 항목으로 진행한다.
- `ReplanRequested`: 정상적인 환경 변화로 남은 예측을 버리고 다시 판단한다.
- `Failed`: 구성 또는 transaction 전제가 깨져 남은 queue를 폐기한다.
- `Clear()` 뒤에는 이전 actor의 context, timer, target, flag가 없어야 한다.

세부 구조: [Action Runtime](Systems/NPC_Decision_and_Actions/Action_Runtime.md), [NPC Runtime](Systems/NPC_Runtime.md)

### 3.3 판단

공통 판단은 stat, 현재 위치, destination과 provider option을 입력받고 하나의 semantic decision을 반환한다. role selector는 combat처럼 즉시 처리해야 하는 role 우선순위와 decision-to-queue 변환만 소유한다.

세부 구조: [NPC Decision and Actions](Systems/NPC_Decision_and_Actions/README.md)

### 3.4 상호작용과 transaction

selector는 registry에서 capability를 선택해 request와 함께 action context에 넣는다. provider는 request를 검증하고 domain transaction을 실행한다. 외부 상태와 내부 상태를 함께 변경할 때는 외부 성공을 확인한 뒤 내부 상태를 소모한다.

세부 구조: [Interaction and Destinations](Systems/Interaction_and_Destinations.md)

### 3.5 전투

물리 감지는 후보만 제공하고 role selector가 target 선택·유지 정책을 결정한다. 선택 target은 actor별 runtime state가 소유하며 이동과 공격 action은 그 handle을 소비한다.

세부 구조: [Combat](Systems/Combat/README.md)

### 3.6 UI

domain은 표시 가능한 read model을 제공하고 UI facade는 category별 view lifecycle을 조정한다. 입력 adapter는 domain concrete type이나 concrete view를 몰라야 한다.

세부 구조: [UI](Systems/UI.md)

## 4. 의존 방향

허용되는 기본 방향:

```text
Scene composition / Manager
  -> Actor runtime + Selector
     -> pure policy / registry / pool
     -> Action
        -> Context + capability interface
Domain runtime -> small UI source contract -> UI facade/view
ScriptableObject definition -> creates/configures runtime object
```

피해야 하는 역방향:

- action -> manager·selector·scene search
- provider -> actor root·role selector
- UI view -> gameplay mutation과 domain search
- definition asset -> per-instance mutable state
- actor root -> role별 utility와 facility transaction
- TestOnly -> production gameplay dependency

## 5. 실패와 안전성

- 필수 dependency 누락은 초기화 경계에서 한 번 진단하고 안전하게 실행을 거부한다.
- 정상 gameplay 변화와 구성 오류를 같은 결과로 숨기지 않는다.
- 여러 action을 대여하는 queue 구성은 원자적이어야 한다.
- 외부 transaction 반환값을 확인하기 전에 내부 자원을 소모하지 않는다.
- pooled object는 첫 실행뿐 아니라 반환 후 두 번째 실행까지 같은 불변식을 만족해야 한다.
- 반복 오류 로그는 frame마다 출력하지 않고 초기화 검증이나 latch를 사용한다.

## 6. 결정성과 시간

- production gameplay 난수는 주입된 `IRandomSource`를 사용한다.
- 같은 seed와 호출 순서는 같은 결과를 내야 한다.
- runtime duration과 decision prediction duration은 의미를 구분한다.
- 동일 의미의 tuning을 selector, action과 asset에 중복 저장하지 않는다.

## 7. Unity와 직렬화 경계

- scene dependency는 serialized reference 또는 명시적 init으로 조립한다.
- enum은 Unity YAML에 정수로 저장되므로 재정렬·중간 삽입 시 migration이 필요하다.
- script·asset 이동 시 `.meta` GUID를 보존한다.
- pooled GameObject는 enable/disable 반복을 전제로 subscription과 runtime state를 정리한다.
- scene·prefab·controller 상세 배선은 해당 Systems 문서가 유일하게 소유한다.

## 8. 현재 구조 부채

- production과 TestOnly assembly가 분리되지 않았다.
- Worker pool의 active actor 제거·despawn lifecycle이 완성되지 않았다.
- prefab catalog와 기존 단일 worker pool이 아직 하나의 생성 경로로 통합되지 않았다.
- NPC의 일반 combat target 정책과 Enemy production spawn 경로가 미결정이다.
- 씨앗 선택을 포함한 농장 생산 대상 변경 규칙이 미결정이다.

세부 상태와 변경 위치는 각 Systems 문서의 `알려진 제약과 TBD`를 따른다.

## 9. 구조 변경 시 갱신 규칙

- 한 기능 내부 구현 변경: 해당 leaf 문서만 갱신한다.
- 기능 간 책임·의존 방향 변경: 이 문서와 관련 leaf 문서를 함께 갱신한다.
- 새 시스템 추가: `ProjectStructure.md` 라우팅과 `Systems/README.md`를 갱신한다.
- 세부 기능이 4개 이상이 된 영역: 폴더형 인덱스로 분할한다.
