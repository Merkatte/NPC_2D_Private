# Code Convention

> 문서 기준일: 2026-08-22
> 적용 범위: 현재 Unity NPC Work 2D 프로젝트의 C# 코드와 직렬화 asset

## 1. 목적과 우선순위

이 규칙은 단순한 서식보다 책임 분리, 명시적 의존성, 풀링 안전성, Unity 직렬화 안정성을 우선한다.

규칙 적용 우선순위:

1. 현재 기능의 올바른 동작과 기존 serialized reference 보존
2. `ARCHITECTURE.md`의 책임·의존 방향
3. 이 문서의 새 코드 규칙
4. 수정 중인 기존 파일의 지역적 스타일

기존 코드에 혼합된 이름이나 형식이 있다고 해서 그것이 새 표준이 되지는 않는다. 요청 범위와 무관한 대규모 style-only rewrite는 하지 않되, 새 코드와 직접 수정하는 줄에는 이 문서를 적용한다.

## 2. Naming

### 2.1 기본 규칙

- class, struct, enum, interface, method, property: `PascalCase`
- local variable, parameter: `camelCase`
- private field: `_camelCase`
- const: `PascalCase`
- interface: 의미 있는 capability/role 앞에 `I` (`IAction`, `IInventory`)
- abstract base: 공통 template/lifecycle을 제공하면 `Base...`
- bool: `Is`, `Has`, `Can`, `Should`, `Try`로 의미를 드러낸다.

```csharp
private readonly List<Candidate> _candidates = new List<Candidate>();
[SerializeField] private float _arrivalDistance = 0.2f;
private const float MinimumDuration = 0.01f;

public bool HasValidTarget { get; }
public float AttackRange { get; }
```

### 2.2 Property와 method 이름

새 read-only property에는 `Get` 접두사를 붙이지 않는다.

```csharp
public float AttackPower { get; }
public int GetQuantity(int itemId); // 인자가 필요한 조회는 method이므로 허용
```

현재 `IStatView.GetHunger` 같은 legacy property는 별도 호환성 refactor 전까지 유지할 수 있다. 새 interface가 이를 모방할 필요는 없다.

부작용 없이 실패 가능성을 반환하는 API는 `Try...` 패턴을 사용한다.

```csharp
bool TryGetDestinationPos(BuildingType key, out Vector3 position);
bool TryInteract(InteractionRequest request, out InteractionResult result);
```

### 2.3 이름에 책임을 드러낸다

- `...Definition`: 공유 immutable 설정 또는 runtime factory
- `...Cost`: action 실행 비용이나 판정 tuning
- `...Stat`: 개체별 mutable stat
- `...Request`: 실행 요청 값
- `...Result`: 실행 결과 값
- `...View`: read-only 소비 계약
- `...Selector`: 다음 intent/queue 결정
- `...Provider`: 요청을 실행하거나 option을 제공하는 capability
- `...DB`: 씬 배선에서 만든 lookup/cache
- `...Pool`: 생성·회수·재사용 소유자

이름이 넓으면 실제 계약도 넓어야 한다. `IInteractionProvider`는 Eat/Drink/Farming이 공유하는 실제 공통 protocol이다.

## 3. 파일과 폴더

### 3.1 한 파일의 기본 단위

- public primary type 하나를 파일 하나에 둔다.
- 파일 이름은 primary type과 일치시킨다.
- primary type과 생명주기를 공유하는 아주 작은 private/nested serializable row는 같은 파일에 둘 수 있다.
- enum이나 request/result가 독립적으로 여러 소비자에게 쓰이면 별도 파일로 둔다.

### 3.2 현재 폴더 기준

- `Assets/Scripts/Actor`: scene actor와 interactable MonoBehaviour
- `Assets/Scripts/Manager`: scene composition, registry, spawn manager
- `Assets/Scripts/UI`: UI facade, view base, 표시 lifecycle과 UI 전용 routing
- `Assets/Scripts/Interface`: 여러 domain이 공유하는 안정적인 계약
- `Assets/Scripts/Enum`: project-wide identifier
- `Assets/Scripts/System/Action`: `IAction` 구현
- `Assets/Scripts/System/Actor`: NPC stat, selector, perception, runtime target state
- `Assets/Scripts/System/<Domain>`: 충분한 응집도를 가진 domain 상태와 구현
- `Assets/Scripts/System/Lib`: 둘 이상의 흐름에서 쓰는 pool, registry, 순수 utility
- `Assets/Data/Struct`: 작은 request/result/value type
- `Assets/Data/ScriptableObject/Script`: ScriptableObject class definition
- `Assets/Data/ScriptableObject`: asset instance
- `Assets/TestOnly`: 개발 전용 scene 도구

`System`과 `Systems`, `Actor`와 `Actors` 같은 병렬 root를 임의로 만들지 않는다. 현재 tree를 기준으로 한다.

### 3.3 새 폴더 기준

파일 하나를 위해 domain 폴더를 만들지 않는다. 다만 명확한 후속 타입이 예정되었거나 상태·결과·구현이 이미 함께 존재하는 모듈은 별도 폴더를 사용할 수 있다.

`Lib`, `Manager`, `CONST`를 분류하기 어려운 코드의 투기장으로 사용하지 않는다. 코드의 변경 이유와 소유 상태가 같아야 같은 폴더에 둔다.

## 4. 책임 경계

### 4.1 WorkerNPC

`WorkerNPC`는 actor root이자 action queue runner다.

허용:

- current action/queue lifecycle
- selector 호출
- `ActionResult` 처리
- disable/pooling cleanup

금지:

- Farmer/Guard 우선순위 공식
- 목적지 검색 규칙
- 농사·전투·섭취 세부 실행
- 모든 NPC subsystem의 service locator 역할

### 4.2 NPCComponent

`NPCComponent`는 NPC GameObject에 속한 Unity 참조와 얇은 조작을 제공한다. Transform 이동, flip, optional Guard perception처럼 actor-local Unity 기능을 둔다.

게임 정책, utility 계산, 공유 시설 상태를 넣지 않는다. 특정 role 기능이 커지면 전용 component와 adapter로 분리한다.

### 4.3 Stat

`NPCStat`과 subclass는 개체별 runtime 값과 clamp invariant를 소유한다.

- stat은 Transform, Collider, manager를 참조하지 않는다.
- stat은 거리 판정이나 action 선택을 수행하지 않는다.
- 공통 base에는 모든 NPC가 실제로 사용하는 값만 둔다.
- role 전용 값이 실제로 존재할 때만 subclass를 만든다.
- 성장 시스템은 progression 규칙을 결정하고 stat의 명시적 mutation API를 호출하는 방향을 우선한다.

### 4.4 Selector와 Decider

selector는 우선순위와 action queue 구성을 담당한다. decider는 후보 평가 정책을 담당한다.

- selector는 stat/시설 상태를 직접 mutation하지 않는다.
- selector는 action을 `ActionPool`에서 빌리고 context를 주입한다.
- `DestinationDecider`는 action이나 queue를 만들지 않는다.
- utility formula를 여러 selector에 복사하지 않는다.
- combat처럼 role 고유의 즉시 우선순위는 role selector에 둘 수 있다.
- decider가 반환한 예측 미래 행동을 미리 queue에 넣지 않는다.

### 4.5 Action

action은 선택된 행동의 실행, 완료, 실패, 중단 규칙을 소유한다. 다음 장기 목표를 선택하지 않는다.

- `Start()`에서 필수 context와 interface를 검증하고 한 번 캐시한다.
- `Tick()`에서 반복 cast, `GetComponent`, scene search를 하지 않는다.
- 완료 시 side effect가 정확히 한 번만 발생해야 한다.
- 정상적인 환경 변화로 재판단이 필요하면 `ReplanRequested`를 사용한다.
- 전제나 transaction이 실패하면 `Failed`를 사용한다.
- action이 생성한 mutable runtime state는 `Clear()`에서 모두 초기화한다.

### 4.6 Provider와 facility

provider는 요청받은 상호작용의 도메인 규칙과 transaction을 실행한다. NPC의 다음 행동을 선택하지 않는다.

- facility별 phase, progress, inventory는 scene runtime component가 소유한다.
- 정의와 tuning은 ScriptableObject가 소유한다.
- provider가 `WorkerNPC` 또는 selector concrete type을 참조하지 않는다.
- 외부 저장과 내부 상태를 함께 바꾸는 경우 mutation 순서와 rollback/거부 의미를 명시한다.

### 4.7 UI

`UIManager`는 외부 호출자가 concrete view나 Canvas 구조를 알지 않게 하는 facade이자 popup/hover category coordinator다.

- 호출자는 `IUIService`에 표시 의도와 명시적 enum/source만 전달한다.
- `UIManager`는 개별 화면의 domain data를 검색하거나 Text/Slider를 직접 갱신하지 않는다.
- popup/hover가 추가될 때 concrete serialized field를 늘리지 않고 `PopBase[]`/`HoverBase[]` registry에 등록한다.
- `PopBase`와 `HoverBase`는 공통 lifecycle만 소유하며 concrete view가 실제 표현과 animation을 소유한다.
- hover source는 `IHoverInfoSource`를 통해 `HoverInfo`를 제공하고 domain component가 concrete UI를 참조하지 않게 한다.
- `object` payload, string path, 개별 화면별 거대 `switch`로 compile-time 계약을 숨기지 않는다.

## 5. Action 규칙

### 5.1 Lifecycle 계약

모든 action은 `IAction`과 `DefaultAction`의 현재 lifecycle을 따른다.

```text
Init -> Start -> Tick* -> Completed | ReplanRequested | Failed
                               \-> Stop (취소 경로)
ReturnAction -> Clear
```

- `Start()`는 한 번만 호출된다고 가정하되 재사용 인스턴스의 이전 상태에 의존하지 않는다.
- `Tick()`은 `Running`일 때만 의미 있는 실행을 한다.
- `Pause()`/`Resume()`은 지원하는 상태만 변경한다.
- `Stop()`과 `Clear()`는 부분 초기화 상태에서도 안전해야 한다.
- `Clear()` 후에는 다른 NPC가 같은 action instance를 받아도 이전 context/target/timer가 보이지 않아야 한다.

### 5.2 Result 선택

| 결과 | 사용 시점 |
|---|---|
| `Running` | 아직 실행 중 |
| `Completed` | 선택된 semantic action이 정상 완료됨 |
| `ReplanRequested` | target 이동·사망·감지·욕구 변화 등으로 새 판단 필요 |
| `Failed` | 필수 dependency 누락, 실행 transaction 거부, 유효하지 않은 context |

`Failed`와 `ReplanRequested`는 둘 다 현재 queue를 버리지만 의미가 다르다. 진단 가능한 구성 오류를 정상 replan으로 숨기지 않는다.

### 5.3 ActionContext

`ActionContext`에는 현재 action이 실행될 때 필요한 값만 넣는다.

허용되는 예:

- actor-local component/stat
- 해당 action의 cost asset
- 고정 또는 dynamic move request
- 선택된 provider와 request

금지되는 예:

- `DataManager`, `DestinationDB`, `NPCManager` 전체를 편의를 위해 전달
- 미래에 쓸 가능성만 있는 field
- role마다 nullable service를 계속 추가
- context를 통해 action이 다시 high-level 결정을 수행

새 field를 추가하기 전에 기존 공통 request/provider로 표현할 수 있는지 검토한다.

### 5.4 Queue 구성의 원자성

selector가 여러 action을 대여하다 중간에 실패하면 이미 대여한 action 전부를 반환한다. null action이 섞인 queue나 절반짜리 queue를 반환하지 않는다.

queue는 실행 순서만 표현한다. 숨은 조건 분기나 장기 계획 state machine을 queue data에 넣지 않는다.

## 6. 상호작용 프로토콜

### 6.1 범용화의 기준

`IInteractionProvider`는 “상호작용 가능한 모든 대상”을 표시하는 빈 marker가 아니라 공통 실행 프로토콜이어야 한다. 호출자가 구현 concrete type을 몰라도 다음을 할 수 있어야 한다.

1. 이 action/interaction을 지원하는지 확인한다.
2. 필요하면 선택 가능한 option을 열거한다.
3. 명시적 request를 한 번 실행한다.
4. 공통 result로 성공/실패와 actor effect를 받는다.

범용 interface가 farm progress, cook recipe, pub item list를 모두 property로 노출해서는 안 된다. 도메인 세부 상태는 concrete provider가 소유한다.

### 6.2 확정 패턴

현재 상호작용 구조는 다음 모양이다.

```text
IInteractionProvider
  -> 공통 support/query/execute protocol

BaseInteractionProvider
  -> 공통 validation, 실패 처리, dispatch template

FarmWorkSite / Pub / (미래) Cook...
  -> 자신의 domain rule과 runtime state
```

`Pub`(Eat/Drink)와 `FarmWorkSite`(Farming)가 모두 `BaseInteractionProvider`를 상속해 이 패턴을 따른다. base class는 공유되는 실제 lifecycle이나 validation이 있을 때만 만든다. 단지 상속 계층을 보기 좋게 만들기 위해 빈 base를 만들지 않는다.

### 6.3 금지하는 증식 패턴

다음 구조를 기본 확장 방식으로 사용하지 않는다.

```text
ActionContext.FarmProvider
ActionContext.CookProvider
ActionContext.ShopProvider

DestinationDB.TryGetFarmProvider(...)
DestinationDB.TryGetCookProvider(...)
DestinationDB.TryGetShopProvider(...)
```

새 facility(Cook, Shop, Clinic 등)를 추가할 때는 domain 전용 provider interface나 `DestinationDB.TryGetXxxProvider(...)`를 만들지 않는다. `BaseInteractionProvider`를 상속하고 `InteractableManager._interactables`에 등록해 기존 `IInteractionProvider` 경로로 노출한다.

## 7. Interface 규칙

interface는 소비자 관점의 최소 계약이다.

- concrete class의 모든 public member를 그대로 복사하지 않는다.
- 구현체의 상속 계층을 interface 상속으로 재현하지 않는다.
- 서로 독립적으로 소비되는 능력은 독립 interface로 둔다.
- interface segregation이 runtime 객체 여러 개를 의미하지 않는다.

예:

```text
GuardStat : NPCStat, IGuardStatView
IGuardStatView : ICombatStatView
```

Guard는 `NPCStat`, `ICombatStatView`, `IGuardStatView`라는 객체 세 개를 가지는 것이 아니다. 같은 `GuardStat` 인스턴스를 소비자가 필요한 계약으로 바라본다.

일대일 interface가 허용되는 경우:

- Unity concrete type을 domain code에서 숨겨야 하는 즉시 필요가 있음
- deterministic test double이나 다른 구현이 실제로 필요함
- dependency 방향을 끊는 명확한 효과가 있음

“나중에 필요할지도 모른다”만으로 interface를 만들지 않는다.

## 8. ScriptableObject와 Runtime Data

### 8.1 ScriptableObject 용도

적합한 데이터:

- 여러 인스턴스가 공유하는 designer tuning
- runtime object의 초기 정의/factory
- action 비용과 정책
- utility weight와 prediction parameter
- 생산 규칙과 수확량 범위

부적합한 데이터:

- NPC별 현재 체력·욕구·레벨
- 농경지별 현재 progress/phase
- 창고의 현재 수량
- 현재 combat target
- action timer

### 8.2 Definition/Cost/Stat 경계

- `Stat`: 한 runtime 개체가 현재 가진 능력과 상태
- `Definition`: runtime stat/site를 만들거나 초기화하는 공유 원본
- `Cost`: action 실행으로 소모되는 값 또는 공통 판정 policy

값을 어디에 둘지 애매하면 “인스턴스마다 달라지거나 성장하는가?”, “씬 배치마다 달라지는가?”, “모든 인스턴스가 같은 tuning을 읽는가?”를 먼저 묻는다.

### 8.3 Validation

- serialized 수치는 `OnValidate()`에서 유효 범위를 clamp한다.
- runtime에서도 외부 입력과 asset 누락을 방어한다.
- `OnValidate()`만 믿고 transaction invariant를 생략하지 않는다.
- asset의 기본값은 0일 때 시스템이 무너지는 항목에 대해 명시적으로 설정한다.

## 9. Unity Component 규칙

### 9.1 Serialized dependency

- scene dependency는 `[SerializeField] private`로 명시한다.
- `Awake()`에서 dictionary/cache와 actor-local dependency를 준비한다.
- 다른 object의 `Start()` 순서에 기대는 조립은 피한다.
- 필수 참조 누락은 원인을 포함해 한 번 로그하고 안전하게 비활성화한다.

`FindFirstObjectByType`, `GetComponent`, `TryGetComponent`는 초기 조립이나 TestOnly 편의 도구에서 제한적으로 사용할 수 있다. production `Tick()`/`Update()` hot path에서 scene search를 하지 않는다.

### 9.2 Unity null

`UnityEngine.Object`는 destroyed object에 대해 fake null을 사용한다.

- concrete component는 `if (!component)`로 검사한다.
- interface로 저장하면 Unity truthiness를 직접 사용할 수 없으므로 backing `Component`를 함께 보관하거나 명시적 validity adapter를 사용한다.
- plain C# 객체에는 일반 null 비교를 사용한다.

### 9.3 Lifecycle과 event

- `Awake`: 자기 참조와 cache 초기화
- `OnEnable`: 반복 가능한 subscription 시작
- `OnDisable`: subscription과 runtime 실행 상태 정리
- `OnDestroy`: 외부 resource의 최종 정리가 실제로 필요할 때만 사용

subscription은 같은 owner가 해제한다. pooled GameObject는 disable/enable을 반복하므로 한 번만 실행된다고 가정하지 않는다.

## 10. Pooling

### 10.1 ActionPool

- `ActionType`을 추가하면 factory case도 함께 추가한다.
- 대여자는 반환 책임을 명확히 가진다.
- `ReturnAction`은 `Clear()` 후 pool에 넣는다.
- `Clear()`는 모든 cached reference와 timer를 reset한다.
- unknown `ActionType`은 error를 로그하고 null/실패로 처리한다.

### 10.2 WorkerPool

- spawn 전후 Transform과 runtime state를 명시적으로 초기화한다.
- `WorkerNPC.OnDisable`은 queue와 actor-local runtime state를 정리한다.
- pool에 반환된 worker를 manager의 active 목록에 계속 보관할 경우 별도 lifecycle 정책이 필요하다.

pooling 검증은 첫 실행뿐 아니라 반환 후 두 번째 대여까지 포함한다.

## 11. 실패, Logging, Transaction

### 11.1 Null과 실패 처리

- public/serialized dependency가 null일 수 있으면 경계에서 검사한다.
- null을 빈 성공으로 취급하지 않는다.
- fallback Idle은 crash나 tight loop를 막는 안전 정책일 때 사용한다.
- 잘못된 role stat과 selector 조합은 spawn 전에 거부한다.

### 11.2 Logging

- 매 frame/replan마다 반복되는 같은 오류는 latch 또는 초기화 검증으로 한 번만 로그한다.
- 메시지에는 owner, 누락 dependency, 실패한 key/action type을 포함한다.
- 정상 gameplay 분기를 `Debug.Log`로 계속 출력하지 않는다.
- 상세 decision trace는 tuning flag와 development conditional로 제한한다.

### 11.3 Transaction 순서

여러 상태를 바꾸는 작업은 실패 시 불변식을 먼저 정의한다.

예: 현재 농경지 수확

1. yield를 계산한다.
2. inventory가 전체 수량을 수락하는지 실행한다.
3. 성공한 경우에만 harvest progress를 줄인다.

부분 수락을 허용할지 금지할지는 API 계약으로 명시한다. 반환값을 무시한 채 내부 상태를 먼저 줄이지 않는다.

## 12. 결정성, 시간, 성능

### 12.1 난수

- production gameplay 난수는 `IRandomSource`를 사용한다.
- 동일 seed와 동일 호출 순서는 동일 결과를 내야 한다.
- global `UnityEngine.Random`은 TestOnly 시각 도구 외에는 사용하지 않는다.
- 후보 순회 중 불필요하게 난수를 소비하지 않는다.

### 12.2 시간

- frame 기반 실행은 `Time.deltaTime`을 사용한다.
- utility prediction duration은 runtime duration과 구별해서 이름과 주석에 명시한다.
- 두 값이 같은 의미라면 장기적으로 한 definition을 source of truth로 만든다.
- magic duration을 selector, action, tuning asset에 중복 복사하지 않는다.

### 12.3 Hot path

`Update()`/`Tick()`에서는 다음을 피한다.

- scene-wide `Find*`
- 반복 `GetComponent`
- 매 frame LINQ
- 불필요한 list/string 할당
- reflection
- 매 frame dictionary 재구성

필요한 interface cast와 lookup은 `Start()` 또는 selector queue 구성 시 한 번 수행하고 캐시한다.

## 13. Enum과 Unity 직렬화

Unity가 enum을 정수로 직렬화하므로 기존 serialized enum의 중간 삽입·재정렬·삭제는 asset/scene migration 없이는 금지한다.

- 새 값은 기본적으로 끝에 추가한다.
- 이름 변경도 serialized YAML과 code reference를 함께 감사한다.
- `ActionType` 추가 시 `ActionPool` factory와 selector mapping을 확인한다.
- `BuildingType` 추가 시 모든 관련 scene의 `DestinationDB` 배선을 확인한다.
- `NPCType` 추가 시 `NPCManager._creationEntries`를 확인한다.

script 이동·이름 변경 시 `.meta` GUID를 보존한다. `.meta`를 임의 삭제하거나 새 GUID로 교체하면 scene/prefab reference가 끊어진다.

## 14. 주석과 문서

좋은 주석:

- 코드만으로 드러나지 않는 invariant
- approximation과 authoritative value의 차이
- transaction 순서의 이유
- 임시 compatibility/과도기 경계와 제거 조건
- Unity fake null 또는 serialization 위험

피할 주석:

- 코드 한 줄을 한국어나 영어로 그대로 반복
- 이미 삭제된 계획 문서 번호에만 의존하는 설명
- 미래 구현을 현재 구현처럼 단정
- 이유 없이 “임시”, “나중에 수정”만 적는 TODO

문서는 현재 구현, 확정 방향, 미구현을 구분한다. 구조가 바뀌면 `ARCHITECTURE.md`, `ProjectStructure.md`, 이 문서의 관련 부분을 함께 갱신한다.

## 15. TestOnly와 검증

- `Assets/TestOnly` 코드는 gameplay 시스템의 dependency가 되어서는 안 된다.
- TestOnly window는 public gameplay API를 통해 검증한다.
- probe가 만들 수 없는 조건은 PASS로 가장하지 않고 `SKIP`, `OBS`, `NOT VERIFIED`로 기록한다.
- deterministic decision은 같은 입력을 반복해 같은 결과인지 확인한다.
- command-line build는 compile 검증이며 Play Mode 검증을 대체하지 않는다.

최소 검증 조합:

1. `dotnet build Assembly-CSharp.csproj --no-restore`
2. 관련 reference/금지 패턴 `rg` 확인
3. `git diff --check`
4. 변경 파일 diff 검토
5. 필요한 scene wiring 확인
6. lifecycle·pooling·transaction의 Play Mode 시나리오

## 16. 새 코드 체크리스트

### 책임

- 이 타입의 변경 이유가 하나의 응집된 책임인가?
- selector, action, provider, stat, manager 중 올바른 소유자에 있는가?
- 현재 구현에 없는 미래 시스템을 가정하지 않았는가?

### 의존성

- dependency가 serialized field, init parameter, request/context로 명시되어 있는가?
- action이 manager나 scene을 직접 검색하지 않는가?
- `ActionContext`를 service locator로 키우지 않았는가?

### 데이터

- shared definition과 runtime state가 분리되었는가?
- per-NPC 값이 shared cost asset에 들어가지 않았는가?
- ScriptableObject를 runtime에 mutation하지 않는가?

### 실행

- action side effect가 정확히 한 번 발생하는가?
- `Completed`, `ReplanRequested`, `Failed` 의미가 맞는가?
- queue 구성 실패 시 대여한 action을 모두 반환하는가?
- `Clear()`가 모든 상태를 reset하는가?

### 확장

- 새 domain provider를 추가하기 전에 공통 interaction protocol로 표현 가능한지 검토했는가?
- interface가 실제 소비자의 최소 계약인가?
- 범용 base가 공통 lifecycle/template을 실제로 제공하는가?

### Unity

- Inspector reference와 `.meta` GUID가 보존되는가?
- enum serialization 영향을 확인했는가?
- destroyed Unity object와 pooled enable/disable을 고려했는가?
- Play Mode에서 두 번째 실행까지 검증했는가?
