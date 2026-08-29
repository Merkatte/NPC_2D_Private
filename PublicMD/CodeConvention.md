# Code Convention

> 문서 기준일: 2026-08-29
> 이 문서는 모든 C# 코드에 공통으로 적용되는 작성·검증 규칙만 소유한다. 특정 클래스의 현재 책임과 흐름은 `PublicMD/Systems`를 따른다.

## 1. 우선순위

충돌할 때 다음 순서를 따른다.

1. 컴파일·runtime 안정성
2. Unity serialization과 lifecycle 안전성
3. 문서화된 책임과 의존 방향
4. 기존 파일의 명확한 local style
5. 이 문서의 일반 규칙

새 코드는 현재 구조에 없는 미래 시스템을 추측해 abstraction을 만들지 않는다.

## 2. Naming

- type, method, public property: `PascalCase`
- parameter, local variable: `camelCase`
- private field: `_camelCase`
- serialized private field: `[SerializeField] private Type _field;`
- interface: 실제 capability나 view를 나타내는 `IName`
- bool: `Is`, `Has`, `Can`, `Should`, `Requires`
- 실패 가능한 조회·실행: `Try...`

이름에 구현 수단보다 책임과 의미를 드러낸다. `Manager`, `Helper`, `Util`, `Data` 같은 넓은 이름은 실제 범위가 분명할 때만 사용한다.

## 3. 파일과 폴더

- 기본적으로 public top-level type 하나당 파일 하나를 사용하고 파일명과 type명을 맞춘다.
- partial, 작은 직렬화 row, 강하게 결합된 private helper만 예외로 둔다.
- 새 파일은 namespace가 아니라 변경 이유와 runtime 책임에 맞는 기존 domain 폴더에 둔다.
- 두 기능 이상에서 같은 의미로 쓰이지 않는 코드를 `Lib`나 공통 interface로 올리지 않는다.
- TestOnly code는 production gameplay가 참조하지 않는다.

구체 배치 판단은 `ProjectStructure.md`와 관련 Systems 문서를 따른다.

## 4. Field, Property, Method

- Inspector dependency는 public field 대신 serialized private field로 둔다.
- 외부 읽기가 필요하면 읽기 전용 property를 제공한다.
- mutable collection을 그대로 노출하지 않고 필요한 read-only view나 query를 제공한다.
- method 하나는 하나의 명확한 단계나 transaction을 표현한다.
- bool 반환이 실패를 의미하면 실패 시 out 값의 상태를 명시하고 안전한 기본값을 넣는다.
- `Try...`가 false를 반환한 뒤 일부 상태가 변경된 채 남지 않게 한다.

## 5. 책임과 의존성

- 결정을 내리는 코드와 선택된 행동을 실행하는 코드를 분리한다.
- scene 조립, runtime state, shared definition, presentation을 한 class에 섞지 않는다.
- dependency는 serialized field, init parameter, request/context 중 적절한 경계로 명시한다.
- action·domain logic에서 `Find*`, global singleton, concrete UI 탐색으로 dependency를 숨기지 않는다.
- context나 manager를 편의상 service locator로 키우지 않는다.
- 한 concrete class를 가리기만 하는 일대일 interface는 실제 대체 구현·test seam·dependency inversion 필요가 없으면 만들지 않는다.

## 6. Interface와 Value Object

- interface는 소비자 관점의 최소 계약만 포함한다.
- 서로 독립적으로 소비되는 capability는 분리한다.
- 구현체의 상속 계층을 interface 상속으로 그대로 복제하지 않는다.
- request, result, context는 경계 사이에 필요한 값만 전달하고 행동 로직이나 scene lookup을 소유하지 않는다.
- nullable field를 계속 추가하기 전에 기존 capability나 별도 request로 표현 가능한지 검토한다.

## 7. ScriptableObject와 Runtime Data

ScriptableObject에 적합한 값:

- 여러 인스턴스가 공유하는 designer tuning
- runtime object 초기 definition과 factory
- action 비용, 정책과 생산 규칙

ScriptableObject에 두지 않는 값:

- actor별 현재 체력·욕구·target·timer
- scene 배치별 progress·inventory·예약
- 현재 UI 표시 상태

serialized 수치는 `OnValidate()`에서 유효 범위를 보정하되 runtime 외부 입력도 별도로 검증한다. 공유 asset을 runtime에서 mutation하지 않는다.

## 8. Unity Component

### 8.1 Lifecycle

- `Awake`: 자기 참조, dictionary와 cache 초기화
- `OnEnable`: 반복 가능한 subscription 시작
- `OnDisable`: subscription과 actor-local 실행 상태 정리
- `OnDestroy`: 외부 resource의 최종 정리가 실제로 필요할 때만 사용

subscription을 시작한 owner가 해제한다. pooled object는 enable/disable이 반복된다고 가정한다.

### 8.2 Serialized dependency

- 필수 참조는 Inspector 또는 명시적 init에서 조립한다.
- 다른 object의 `Start()` 순서에 의존하지 않는다.
- 필수 참조 누락은 owner와 field 의미를 포함해 한 번 로그하고 안전하게 비활성화하거나 실패한다.
- hot path에서 `GetComponent`, `TryGetComponent`, scene search를 반복하지 않는다.

### 8.3 Unity null

- concrete `UnityEngine.Object`는 `if (!component)` 형태로 확인한다.
- interface로 저장한 Unity object는 backing `Component`나 명시적 validity adapter와 함께 확인한다.
- plain C# object에는 일반 null 비교를 사용한다.

## 9. 실패와 Logging

- null이나 잘못된 입력을 빈 성공으로 취급하지 않는다.
- 정상 환경 변화와 구성 오류를 별도 결과로 표현한다.
- fallback은 crash나 tight loop를 막는 안전 정책일 때만 사용한다.
- 반복되는 같은 오류는 초기화 검증이나 latch로 한 번만 출력한다.
- 오류 메시지에는 owner, 누락 dependency, 실패 key/type을 포함한다.
- 정상 gameplay branch를 매 frame 로그하지 않는다.
- 상세 trace는 tuning flag와 development conditional로 제한한다.

## 10. Pooling과 Reuse

- 대여자는 반환 책임을 명확히 가진다.
- 반환 전에 instance의 reset/clear 계약을 실행한다.
- timer, cached reference, target, flag와 partial initialization을 모두 정리한다.
- 대여 중간에 실패하면 이미 대여한 모든 객체를 반환한다.
- 첫 실행뿐 아니라 반환 후 두 번째 대여를 검증한다.

## 11. Transaction

여러 상태를 바꾸는 작업은 다음 순서를 기본으로 한다.

1. 입력과 dependency를 검증한다.
2. 결과를 계산한다.
3. 외부 시스템이 전체 transaction을 수락하는지 확인한다.
4. 성공한 경우에만 내부 mutable 상태를 변경한다.
5. side effect와 결과를 정확히 한 번 확정한다.

부분 성공을 허용하는지 금지하는지는 API 계약에 명시한다. 반환값을 무시하고 내부 상태를 먼저 소모하지 않는다.

## 12. 결정성, 시간, 성능

### 12.1 난수

- production gameplay 난수는 주입 가능한 random source를 사용한다.
- 같은 seed와 호출 순서는 같은 결과를 내야 한다.
- 후보 순회 중 불필요하게 난수를 소비하지 않는다.
- global `UnityEngine.Random`은 TestOnly 시각 도구 외 production 규칙에서 사용하지 않는다.

### 12.2 시간

- frame 기반 실행은 `Time.deltaTime`, UI의 real-time poll은 의도에 따라 `Time.unscaledTime`을 사용한다.
- runtime duration과 예측 duration을 이름과 주석으로 구분한다.
- 같은 의미의 magic duration을 여러 위치에 복사하지 않는다.

### 12.3 Hot path

`Update()`와 `Tick()`에서 다음을 피한다.

- scene-wide `Find*`
- 반복 component lookup과 reflection
- 매 frame LINQ, list·string 할당
- 매 frame dictionary 재구성

가능한 lookup과 interface cast는 초기화 또는 실행 시작 경계에서 한 번 수행하고 캐시한다.

## 13. Enum과 Unity Serialization

- serialized enum의 기존 값을 재정렬·삭제·중간 삽입하지 않는다.
- 새 값은 기본적으로 끝에 추가한다.
- 이름이나 type 변경 시 scene, prefab, ScriptableObject YAML reference를 함께 감사한다.
- script·asset 이동 시 `.meta` GUID를 보존한다.
- serialized field 이름 변경은 `FormerlySerializedAs` 또는 명시적 migration을 검토한다.

## 14. 주석과 문서

주석에는 코드만으로 드러나지 않는 invariant, approximation, transaction 이유, Unity serialization/null 위험을 기록한다. 코드 한 줄을 번역하거나 미래 구현을 현재 사실처럼 적지 않는다.

문서는 `현재 구현`, `계획`, `TBD`를 구분한다. 구조가 바뀌면 모든 공통 문서를 고치는 대신 주 소유 Systems 문서와 필요한 상위 문서만 갱신한다.

## 15. 검증

최소 검증 조합:

1. compile 또는 관련 정적 검사
2. reference·금지 패턴 검색
3. `git diff --check`
4. 변경 diff와 사용자 기존 변경 분리 확인
5. 관련 scene·prefab·asset 직렬화 배선 확인
6. lifecycle, pooling, transaction의 두 번째 실행 시나리오

TestOnly probe가 만들 수 없는 조건은 PASS로 가장하지 않고 `SKIP`, `OBS`, `NOT VERIFIED`로 기록한다. command-line compile은 Play Mode 검증을 대신하지 않는다.

## 16. 새 코드 체크리스트

- 변경 이유가 하나의 응집된 책임인가?
- 주 소유 기능 문서의 경계를 따르는가?
- dependency가 명시적으로 조립되는가?
- shared definition과 mutable runtime state가 분리되는가?
- 실패와 재판단의 의미가 구분되는가?
- transaction side effect가 정확히 한 번 발생하는가?
- pooled reuse에서 이전 상태가 남지 않는가?
- enum과 `.meta`의 Unity 직렬화 영향을 확인했는가?
- 관련 Systems 문서의 파일 표·흐름·배선을 함께 갱신했는가?
