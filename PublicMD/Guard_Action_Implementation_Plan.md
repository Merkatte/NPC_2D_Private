# Guard 장기 행동 및 전투 전환 구현 계획

> 문서 상태: 구현 전 승인용 계획 0.2
>
> 개정일: 2026-08-19
>
> 대상 기능: `ProximitySensor2D`, `GuardPerception`, `GuardActionSelector`, `GuardAction`, `AttackAction`, action queue 중단·재계획
>
> 사용 목적: Claude Code의 `implement-npc-feature` 계획 입력 및 구현 승인 기준
>
> 제품 로드맵 위치: Project N Phase D의 경비·전투 기능을 위한 선행 수직 슬라이스

## 0. 실행 전제

이 문서는 구현 요구사항과 책임 배치를 확정하기 위한 계획이다. 구현 단계에서는 다음 순서를 지킨다.

1. Claude Opus의 Plan Mode에서 이 문서와 실제 코드를 다시 확인한다.
2. 아래 `결정 필요 항목`을 사용자에게 확인하고 구현 계획 승인을 받는다.
3. 승인 이후 Claude Sonnet으로 전환하여 Phase A부터 D까지 순서대로 구현한다.
4. 각 Phase의 종료 조건을 통과한 뒤 다음 Phase로 진행한다.

이 계획은 과거 `WorkerAI/WorkerActionPlan` 구조를 복원한다고 가정하지 않는다. 현재 코드의 `WorkerNPC → BaseNPCActionSelector.RequestNewActionQueue(...) → Queue<IAction>` 구조를 기준으로 한다.

## 1. 사용자 요구사항

### GR-001 — Guard의 욕구 억제

- Guard도 Farmer와 동일하게 배고픔, 갈증, 피로를 해결한다.
- Guard는 Farmer보다 욕구를 더 강하게 억제한다.
- 욕구가 Guard 전용 중단 임계치에 도달하기 전에는 경계를 계속한다.
- 임계치와 초당 증가량은 코드 상수가 아니라 데이터에서 조정한다.

### GR-002 — GuardAction의 장기 실행

- `ActionType.Guard`는 경계 태세를 의미한다.
- Guard는 지정된 경계 중심과 경계 반경 안에서 순찰한다.
- `FarmingAction`처럼 짧은 작업을 반복해서 새 queue를 요청하지 않는다.
- 정상 경계 중에는 완료하지 않고 하나의 `GuardAction`이 계속 실행된다.
- GuardAction은 다음 경우에만 현재 실행을 끝내고 재계획을 요구한다.
  - 해결해야 할 욕구가 Guard 임계치에 도달한 경우
  - GuardPerception에 유효한 전투 대상 후보가 생긴 경우
  - 필수 참조 또는 대상 상태가 유효하지 않아 안전하게 중단해야 하는 경우

### GR-003 — Guard 중 욕구 변화

- GuardAction 실행 중 배고픔, 갈증, 피로가 초당 증가한다.
- 적용량은 `초당 변화량 × Time.deltaTime`으로 계산한다.
- Guard의 초당 욕구 증가량은 FarmingAction의 작업 비용보다 작게 조정할 수 있어야 한다.
- GuardAction은 매 frame `StatEffect` 객체를 새로 만들지 않는다.

### GR-004 — 적 발견과 즉시 queue 중단

- `ProximitySensor2D`가 Trigger 범위에 들어온 물리 후보를 감지한다.
- 각 sensor instance는 serialized LayerMask로 감지할 물리 Layer를 제한할 수 있다.
- 범용 sensor는 후보가 적인지, 치료 대상인지, 아이템인지 판단하지 않는다.
- `GuardPerception`이 sensor 후보 중 `ICombatTarget`을 가진 살아 있는 대상만 Guard용 후보로 변환·캐시한다.
- GuardAction은 직접 Physics 조회나 component 탐색을 하지 않고 GuardPerception의 `HasCandidate`만 확인한다.
- GuardAction은 selector를 직접 호출하지 않는다.
- GuardAction은 runner에 `ReplanRequested` 결과를 남긴다.
- `WorkerNPC`가 현재 action과 남아 있는 queue를 정리한 뒤 기존 `RequestNewActionQueue(...)`를 호출한다.
- GuardActionSelector가 GuardPerception의 유효 후보를 비교하여 실제 타겟을 선택한다.
- 선택한 타겟은 Guard 개인의 GuardRuntimeState에 저장한다.
- `BaseNPCActionSelector`에 `RequestAttackQueue`, `OnEnemyFound` 같은 새 공용 API를 추가하지 않는다.

### GR-005 — AttackAction의 장기 실행

- `ActionType.Attack`은 지정된 현재 타겟을 공격하는 행동이다.
- 한 번 타격할 때마다 새 AttackAction을 요청하지 않는다.
- 하나의 AttackAction이 타겟이 죽을 때까지 공격 타이밍을 반복 관리한다.
- 공격 간격은 NPCStat의 공격 속도를 기준으로 계산한다.
- 타겟이 죽거나 파괴되면 타겟 상태를 비우고 재계획을 요구한다.
- 타겟이 살아 있지만 공격 범위 밖으로 벗어나면 타겟은 유지한 채 재계획을 요구한다.

### GR-006 — 목표 runtime loop

```text
타겟 없음
  → GuardAction 장기 실행
    ├─ 욕구 임계 도달
    │   → ReplanRequested
    │     → Move → Eat/Drink/Sleep
    │       → queue 종료
    │         → RequestNewActionQueue
    │           → Guard
    │
    └─ ProximitySensor2D Trigger에 물리 후보 진입
        → GuardPerception이 유효 ICombatTarget 후보로 캐시
          → GuardAction이 HasCandidate 확인
            → ReplanRequested
              → 남은 queue 전체 반환
                → RequestNewActionQueue
                  → Selector가 후보 중 타겟 선택
                    → 개인 GuardRuntimeState에 타겟 저장
                      → Move(동적 타겟, 공격 거리까지)
                        → AttackAction 장기 실행
                          ├─ 타겟이 멀어짐 → Replan → Move → Attack
                          └─ 타겟 사망 → 타겟 제거 → Replan → Guard
```

## 2. 현재 코드 기준선

계획 작성 시점의 실제 코드는 다음 상태다.

- `ActionType.Guard`, `ActionType.Attack`은 사용자 변경으로 추가되어 있다.
- `NPCType.Guard`는 이미 존재한다.
- `GuardActionSelector.cs`는 새 파일이지만 현재 Farmer selector를 복사한 초기 골격이다.
- Guard selector가 `FarmingActionCost`와 Farmer용 `DestinationDecider` 흐름을 그대로 사용한다.
- `DestinationDecider.GetWorkPlaceKey(...)`는 Guard 작업장을 반환하지 않는다.
- `ActionPool`은 GuardAction과 AttackAction을 생성하지 않는다.
- GuardAction과 AttackAction 구현 파일이 없다.
- `WorkerNPC`는 `RequestNewActionQueue` 호출 시 `NPCType.Farmer`를 고정 전달한다.
- selector가 action을 queue에 넣기 전에 `Init()`을 호출하며, 현재 `Init()`이 즉시 `Start()`까지 호출한다.
- `WorkerNPC`는 남은 queue 전체를 취소·반환하는 경로와 재계획 결과를 구분하는 상태가 없다.
- 현재 공격 대상, 피해 가능 대상, 공격 범위와 공격 속도 계약이 없다.
- 범용 Trigger 감지기와 Guard용 typed perception이 없다.
- `BuildingType`에는 Guard 경계 구역을 나타내는 값이 없다.
- SampleScene과 prefab에는 Guard selector 직렬화 연결이 없다.

따라서 GuardAction만 추가해서는 요구된 loop가 동작하지 않는다. queue lifecycle과 per-NPC combat state를 먼저 정리해야 한다.

## 3. 핵심 설계 결정

### 3.1 타겟은 NPCStat에 저장하지 않는다

`NPCStat`은 체력, 이동 속도, 공격 속도, 욕구처럼 NPC 자신의 수치와 clamp invariant를 소유한다. 현재 타겟은 짧은 수명의 외부 Unity 객체 참조이므로 stat이 아니다.

권장 배치:

```text
WorkerNPC 인스턴스
  → NPCComponent
    ├─ ProximitySensor2D (범용 물리 후보)
    ├─ GuardPerception (Guard용 유효 후보)
    └─ GuardRuntimeState (Guard별 독립 instance)
        → CurrentTarget : ICombatTarget
```

- `GuardRuntimeState`는 ScriptableObject가 아닌 runtime 객체다.
- 각 Guard가 독립 instance를 가져야 한다.
- selector MonoBehaviour에 타겟을 field로 저장하지 않는다. 현재 `NPCManager` 구조에서는 같은 selector가 여러 NPC에 공유될 수 있기 때문이다.
- 전역 static 타겟이나 `DataManager`에 저장하지 않는다.
- `NPCComponent`는 기존 selector API에 이미 전달되므로 sensor, perception과 Guard 전용 runtime state에 접근하는 연결점으로만 사용한다.

### 3.2 action은 selector를 직접 호출하지 않는다

GuardAction과 AttackAction은 `RequestNewActionQueue`를 직접 호출하지 않는다. 대신 action 종료 결과를 구분한다.

권장 action 결과:

```text
Running
Completed
ReplanRequested
Failed
```

- `Completed`: 현재 queue의 다음 action을 실행한다.
- `ReplanRequested`: 현재 action과 남은 queue를 모두 반환하고 기존 selector API로 새 queue를 요청한다.
- `Failed`: 현재 queue를 폐기하고 안전한 재요청 또는 Idle fallback을 수행한다.
- `ReplanRequested`를 만드는 메서드는 `DefaultAction`의 protected API로 둔다.

이 변경은 `IAction`/runner 계약 변경이며 selector 공용 API 추가가 아니다.

### 3.3 runner가 queue lifecycle을 단독 소유한다

`WorkerNPC`는 다음을 단독으로 담당한다.

- queue 요청
- action Init과 실제 Start 시점
- 현재 action Tick
- 완료 action 반환
- 재계획 시 현재 action Stop·반환
- 남은 queue의 모든 action Stop·반환
- queue가 끝났을 때 기존 `RequestNewActionQueue` 호출

selector는 action을 선택하고 context를 설정하지만 action을 실행하지 않는다. queue에 대기 중인 action이 미리 Start되지 않도록 `Init`과 `Start`를 분리한다.

### 3.4 ProximitySensor2D는 범용 물리 감지만 소유한다

`ProximitySensor2D`는 Guard나 Enemy라는 도메인을 모르는 재사용 가능한 Trigger sensor다.

책임:

- Trigger에 들어온 `Collider2D` 등록
- Trigger에서 나간 `Collider2D` 제거
- serialized LayerMask에 포함되지 않은 collider 거부
- 감지 중인 후보와 enter/exit notification을 read-only로 제공
- 파괴되어 exit callback을 보내지 못한 collider의 안전한 정리

비책임:

- 후보가 적인지, 아이템인지, 치료 대상인지 판정
- 타겟 우선순위와 가장 가까운 적 선택
- GuardRuntimeState 변경
- action 중단 또는 selector 호출

LayerMask는 sensor instance마다 다르게 설정한다. Guard의 combat sensor는 Enemy layer, 향후 운반자 sensor는 Item layer처럼 같은 스크립트를 다른 설정으로 재사용할 수 있다. Physics Layer Collision Matrix로 불필요한 충돌 조합을 먼저 차단하고 sensor의 LayerMask로 instance별 범위를 다시 제한한다.

적 하나가 여러 collider를 가질 수 있으므로 raw collider의 단순 존재만으로 적 수를 계산하지 않는다. sensor는 collider 진입·이탈을 정확히 관리하고, domain perception은 동일 target root에 대한 reference count 또는 중복 제거를 담당한다. `OnTriggerStay2D`와 매 frame `GetComponent`는 사용하지 않는다.

### 3.5 GuardPerception은 전투 후보만 변환·캐시한다

`GuardPerception`은 `ProximitySensor2D`와 Guard domain 사이의 adapter다.

- sensor enter/exit notification을 구독한다.
- enable 시 sensor의 현재 후보 snapshot과 동기화하여 이미 범위 안에 있던 대상을 놓치지 않는다.
- disable 시 notification을 해제하고 typed candidate cache를 정리한다.
- 진입한 collider에서 target root의 `ICombatTarget` 구현을 한 번만 해석한다.
- `ICombatTarget.IsAlive`가 참인 대상만 Guard 후보로 유지한다.
- 동일 적의 복수 collider를 하나의 target으로 중복 제거한다.
- destroyed Unity object와 죽은 타겟을 조회 시 또는 상태 변경 시 제거한다.
- `HasCandidate`와 read-only typed candidate collection을 제공한다.
- 실제 타겟 우선순위를 결정하거나 GuardRuntimeState에 타겟을 기록하지 않는다.

GuardAction은 `GuardPerception.HasCandidate`만 읽으므로 매 Tick Physics query나 component lookup을 수행하지 않는다. GuardActionSelector는 재계획 시점에만 typed candidate collection을 순회한다. 비용은 전체 적 수가 아니라 현재 sensor 범위 안의 소수 후보 수에 비례한다.

향후 다른 직업은 같은 `ProximitySensor2D` 위에 `InjuredPerception`, `CollectiblePerception` 같은 별도 adapter를 조합할 수 있다. 두 번째 사용처가 생기기 전에는 범용 reflection 기반 `DetectionFilter` 프레임워크를 만들지 않는다.

### 3.6 Guard selector의 우선순위

`GuardActionSelector.RequestNewActionQueue(...)`의 우선순위는 다음과 같다.

1. 현재 타겟이 유효하고 살아 있는가?
   - 공격 범위 밖: `Move(dynamic target, attack stopping distance) → Attack`
   - 공격 범위 안: `Attack`
2. 현재 타겟이 없고 GuardPerception에 유효 후보가 있는가?
   - 확정된 target policy로 후보 하나 선택
   - 선택한 타겟을 GuardRuntimeState에 기록
   - 공격 범위 밖: `Move(dynamic target, attack stopping distance) → Attack`
   - 공격 범위 안: `Attack`
3. 타겟이 없고 Guard 임계치를 넘은 해결 가능한 욕구가 있는가?
   - 기존 need destination 선택을 사용해 `Move → Eat/Drink/Sleep`
4. 타겟과 해결할 욕구가 없는가?
   - 경계 구역 밖이면 `Move(GuardPost center/range) → Guard`
   - 경계 구역 안이면 `Guard`
5. 필요한 action, destination 또는 상호작용 수단이 없는가?
   - 빈 queue 반복을 만들지 않고 원인을 기록한 뒤 `Idle` backoff를 반환한다.

적은 욕구보다 우선한다. AttackAction 실행 중에는 욕구가 임계치를 넘어도 현재 타겟이 죽거나 공격할 수 없게 될 때까지 공격을 유지한다.

### 3.7 GuardAction은 순찰과 감지 상태 확인을 소유한다

GuardAction은 경계 태세 한 개의 실행 흐름을 소유한다.

- 경계 중심과 반경 안에서 다음 순찰 지점을 선택한다.
- 목적지에 도착하면 같은 action 안에서 다음 지점을 정한다.
- GuardPerception의 `HasCandidate`를 확인한다.
- 후보가 있으면 타겟을 직접 고르거나 저장하지 않고 `ReplanRequested`를 반환한다.
- 재계획 후 GuardActionSelector가 후보를 다시 검증하고 실제 타겟을 선택한다.
- normal Guard 중에는 `Completed`를 반환하지 않는다.
- `Stop()`과 `Clear()`에서 순찰 지점과 임시 상태를 초기화한다.

GuardAction은 `Physics2D.Overlap...`, `OnTriggerEnter2D`, `GetComponent<ICombatTarget>`를 수행하지 않는다. 물리 감지는 sensor, capability 해석은 perception, 실제 선택은 selector가 담당한다.

전역 `UnityEngine.Random` 직접 사용은 기존 결정적 판단 방향과 충돌하므로 금지한다. 순찰점 생성은 주입된 난수 source를 사용하거나, 현재 범위에서는 경계 안의 결정 가능한 지점 순환으로 구현한다.

### 3.8 MoveAction은 동적 목적지를 지원한다

전투 타겟은 이동할 수 있으므로 selector가 queue를 만들 때 캡처한 `Vector3`만 사용하면 안 된다.

권장안은 `ActionContext`에 combat 규칙을 직접 넣는 대신 작은 `MoveRequest` 값을 추가하는 것이다.

`MoveRequest`가 표현할 내용:

- 고정 목적지 또는 동적 `Transform`
- stopping distance
- 현재 목적지 유효 여부

MoveAction은 MoveRequest만 읽으며 적, Guard, 공격을 알지 않는다. 동적 목적지가 사라지면 `ReplanRequested`로 끝난다.

### 3.9 AttackAction의 공격 주기

권장 의미:

- `NPCStat.AttackSpeed` 단위는 초당 공격 횟수(attacks per second)다.
- `attackInterval = 1f / AttackSpeed`로 계산한다.
- 공격 속도가 0 이하이면 action은 안전하게 실패하고 재계획한다.
- 첫 공격은 Start 즉시가 아니라 한 attack interval 후에 발생한다(`결정 필요`).
- 큰 frame delta가 발생해도 누적 timer 오차가 계속 쌓이지 않도록 interval을 차감한다.
- 타격마다 target의 생존 여부와 거리를 다시 검증한다.

## 4. 구현 Phase

아래 Phase A~D는 이 Guard 기능만을 위한 구현 단계다. `PublicMD/PLAN.md`의 제품 Phase A~D와 구분한다.

## Guard Phase A — queue 중단과 재계획 기반

### 목표

장기 action이 안전하게 실행되고, action 결과에 따라 남은 queue를 모두 폐기한 뒤 기존 selector API로 재계획할 수 있게 한다.

### 작업

1. action 실행 결과 enum을 추가한다.
2. `IAction`이 현재 결과를 노출하도록 수정한다.
3. `DefaultAction`에 `Complete`, `RequestReplan`, `Fail`의 분리된 protected 종료 경로를 둔다.
4. `Init()`이 action을 즉시 Start하지 않도록 분리한다.
5. `WorkerNPC`가 dequeue한 현재 action만 Start한다.
6. `WorkerNPC`에 현재 action과 남은 queue를 Stop·반환하는 private 정리 경로를 만든다.
7. `ReplanRequested` 시 기존 `RequestNewActionQueue`를 다시 호출한다.
8. `WorkerNPC.Init(...)`이 `NPCType`을 받아 저장하고 Farmer hardcode를 제거한다.
9. `NPCManager.CreateNPC(npcType)`가 실제 npcType을 WorkerNPC에 전달한다.
10. Eat, Drink, Sleep, Farming, Idle, Move의 기존 완료 동작을 새 결과 계약으로 마이그레이션한다.

### 종료 조건

- queue에 들어간 두 번째 이후 action은 dequeue 전 Start되지 않는다.
- 현재 action이 재계획을 요청하면 현재 action과 남은 action이 각각 정확히 한 번 pool로 반환된다.
- queue가 비었을 때 null reference나 같은 frame 무한 재요청이 발생하지 않는다.
- Farmer의 기존 `Move → Farming`, `Move → Eat/Drink/Sleep` 흐름이 유지된다.
- `BaseNPCActionSelector`의 public queue 요청 API는 추가되지 않는다.

## Guard Phase B — 장기 GuardAction과 욕구 억제

### 목표

적이 없는 상태에서 Guard가 경계 범위 안을 계속 순찰하고, Farmer보다 높은 임계치까지 욕구를 억제한 뒤 생활 행동으로 전환하게 한다.

### 작업

1. `BuildingType.GuardPost` 또는 사용자 승인 명칭을 추가한다.
2. DestinationDB에 Guard 경계 중심을 연결한다.
3. Guard 전용 action tuning asset을 정의한다.
4. tuning에 다음 값을 둔다.
   - 경계 반경
   - 순찰 도착 거리
   - hunger/thirst/fatigue 초당 변화량
   - 욕구별 Guard 중단 임계치
5. `GuardAction`을 구현하고 ActionPool에 등록한다.
6. GuardAction이 정상 순찰 중에는 완료되지 않게 한다.
7. GuardAction이 초당 욕구 변화를 적용한다.
8. 욕구 임계 도달 시 `ReplanRequested`를 반환한다.
9. GuardActionSelector에서 타겟이 없을 때 `need 해결 > Guard` 계획을 구성한다.
10. Guard용 tuning을 Farmer용 tuning과 별도 asset instance로 연결한다.

### 종료 조건

- Guard는 경계 중심에서 지정 반경을 벗어나지 않고 계속 이동·대기한다.
- 적이 없고 욕구가 임계 미만이면 30초 이상 새 queue를 요청하지 않는다.
- Guard 중 욕구가 frame rate와 무관하게 초당 설정값만큼 변한다.
- 욕구 임계 도달 후 `Move → need action`을 수행하고 다시 Guard로 복귀한다.
- 해결 가능한 시설이 없을 때 빈 queue/replan 무한 반복이 발생하지 않는다.
- tuning 수치를 C# 수정 없이 asset에서 변경할 수 있다.

## Guard Phase C — 적 감지와 Move→Attack 계획

### 목표

범용 Trigger sensor와 Guard 전용 perception을 조합한다. GuardAction은 유효 후보의 존재만 보고 현재 queue를 중단하며, 기존 RequestNewActionQueue 흐름에서 selector가 타겟을 선택하고 Move→Attack을 구성하게 한다.

### 작업

1. 최소 `ICombatTarget` 계약을 추가한다.
   - 현재 위치
   - 생존 여부
   - 피해 적용
2. `ProximitySensor2D`를 추가한다.
   - Trigger enter/exit 처리
   - per-instance LayerMask 검사
   - read-only collider 후보와 enter/exit notification
   - 파괴된 collider 정리
3. Guard prefab 자식에 combat sensor GameObject와 `CircleCollider2D(IsTrigger)`를 구성한다.
4. Guard 또는 Enemy 중 필요한 쪽에 `Rigidbody2D`를 구성하고 Physics Layer Collision Matrix를 제한한다.
5. `GuardPerception`을 추가한다.
   - sensor notification 구독·해제
   - enable 시 현재 sensor 후보와 재동기화
   - collider→ICombatTarget 변환
   - 복수 collider reference count 또는 target 중복 제거
   - 살아 있는 typed candidate cache
6. Guard별 `GuardRuntimeState`를 추가한다.
7. NPCComponent가 자신의 combat sensor, GuardPerception과 GuardRuntimeState를 명시적으로 제공한다.
8. GuardAction이 `GuardPerception.HasCandidate`를 확인하고 후보가 있으면 `ReplanRequested`를 반환한다.
9. GuardActionSelector가 현재 타겟을 최우선으로 읽고, 없다면 GuardPerception 후보를 조회한다.
10. selector가 target policy로 유효한 적 하나를 선택하여 GuardRuntimeState에 저장한다.
11. 타겟이 공격 범위 밖이면 동적 MoveRequest를 사용한 Move를 queue에 넣는다.
12. Move 뒤에 하나의 AttackAction을 queue에 넣는다.
13. 타겟이 이미 공격 범위 안이면 AttackAction만 queue에 넣는다.
14. 이동 중 타겟이 사라지면 MoveAction이 재계획을 요청한다.

### 종료 조건

- ProximitySensor2D는 Enemy, Item, Worker 같은 도메인 타입을 참조하지 않는다.
- sensor instance의 LayerMask에 포함되지 않은 collider는 후보에 남지 않는다.
- GuardPerception은 ICombatTarget이 없거나 죽은 대상을 Guard 후보로 노출하지 않는다.
- 동일 적의 복수 collider가 하나의 Guard 후보로 처리된다.
- GuardAction은 직접 Physics query 또는 component lookup을 하지 않는다.
- GuardAction이 유효 후보를 인지하면 일반 `Completed`가 아니라 `ReplanRequested`로 끝난다.
- 타겟 선택과 GuardRuntimeState 기록은 GuardAction이 아니라 selector가 수행한다.
- pending need/idle action이 있더라도 replan 시 모두 반환된다.
- selector에 새 공용 public 요청 API가 생기지 않는다.
- 두 Guard가 서로 다른 GuardRuntimeState와 타겟을 가진다.
- 이동하는 타겟을 고정 좌표가 아니라 현재 위치를 기준으로 추적한다.
- 타겟이 이동 중 파괴되면 null reference 없이 Guard로 복귀한다.

## Guard Phase D — 장기 AttackAction과 전체 loop

### 목표

하나의 AttackAction이 NPCStat의 공격 속도에 따라 같은 타겟을 반복 공격하고, 타겟 사망 또는 거리 이탈 후 올바르게 재계획하게 한다.

### 작업

1. NPCStat과 IStatView에 공격 속도를 추가한다.
2. DefaultStatContext와 runtime stat 생성 경로에 공격 속도를 연결한다.
3. 공격력의 소유 위치를 승인된 결정에 따라 추가한다.
4. AttackAction tuning에 공격 범위 등 action 실행 값을 둔다.
5. `AttackAction`을 구현하고 ActionPool에 등록한다.
6. AttackAction은 Start 시 타겟·stat·tuning을 검증한다.
7. AttackAction은 attack interval을 누적해 같은 action 안에서 반복 타격한다.
8. 타겟이 죽으면 GuardRuntimeState에서 제거하고 `ReplanRequested`를 반환한다.
9. 타겟이 범위 밖이면 타겟을 유지하고 `ReplanRequested`를 반환한다.
10. Stop/Clear에서 공격 timer와 임시 참조를 초기화한다.
11. SampleScene 또는 전용 검증 장면에 최소 damageable target과 Guard 배선을 추가한다.
12. 전체 `Guard → Move → Attack → Guard` loop를 검증한다.

### 종료 조건

- 한 AttackAction이 여러 번 타격하며 타격마다 새 action을 요청하지 않는다.
- 측정된 공격 횟수가 설정한 attacks per second와 허용 오차 안에서 일치한다.
- 타겟이 죽으면 정확히 한 번 타겟을 비우고 Guard로 돌아간다.
- 타겟이 멀어지면 `Move → Attack`을 재구성하며 AttackAction을 중복 실행하지 않는다.
- 공격 중 욕구 임계치를 넘더라도 살아 있는 현재 타겟을 우선한다.
- action pool 재사용 후 이전 타겟과 timer가 남지 않는다.

## 5. 파일 변경 계획

정확한 이름은 Claude Opus Plan Mode에서 현재 로컬 스타일과 충돌 여부를 다시 확인한다.

### 새 파일 후보

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Enum/ActionResult.cs` | Running/Completed/ReplanRequested/Failed 결과 |
| `Assets/Scripts/Interface/ICombatTarget.cs` | 위치·생존·피해 적용의 최소 전투 대상 계약 |
| `Assets/Scripts/System/Actor/ProximitySensor2D.cs` | LayerMask 기반 범용 Trigger 후보 감지와 enter/exit notification |
| `Assets/Scripts/System/Actor/GuardPerception.cs` | sensor collider를 살아 있는 ICombatTarget 후보로 변환·중복 제거 |
| `Assets/Scripts/System/Actor/GuardRuntimeState.cs` | Guard별 현재 타겟과 타겟 invariant |
| `Assets/Data/Struct/MoveRequest.cs` | 고정/동적 목적지와 stopping distance |
| `Assets/Scripts/System/Action/GuardAction.cs` | 장기 순찰, 초당 욕구, perception 후보 확인, 재계획 요청 |
| `Assets/Scripts/System/Action/AttackAction.cs` | 공격 주기, 거리·생존 검사, 반복 타격 |
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | GuardAction 실행·중단 tuning |
| `Assets/Data/ScriptableObject/Script/AttackActionCost.cs` | 공격 범위 등 AttackAction tuning |
| `Assets/Scripts/Actor/Enemy.cs` 또는 기존 적 구현 | ICombatTarget 최소 구현; 전체 적 AI는 제외 |

### 변경 파일 후보

| 파일 | 변경 목적 |
|---|---|
| `Assets/Scripts/Interface/IAction.cs` | action 결과 노출과 lifecycle 명확화 |
| `Assets/Scripts/System/Action/DefaultAction.cs` | 완료·재계획·실패 종료 경로 분리, Init/Start 분리 |
| `Assets/Scripts/Actor/WorkerNPC.cs` | NPCType 보관, queue 단독 소유, 전체 취소·반환·재계획 |
| `Assets/Scripts/Manager/NPCManager.cs` | WorkerNPC에 실제 NPCType 전달 및 selector 검증 |
| `Assets/Scripts/System/Actor/NPCComponent.cs` | sensor, GuardPerception과 Guard별 runtime state 연결점 제공 |
| `Assets/Scripts/System/Actor/NPCStat.cs` | 공격 속도와 승인된 공격 수치 invariant |
| `Assets/Scripts/Interface/IStatView.cs` | 공격 속도 read-only surface |
| `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs` | 공격 속도 기본 데이터와 생성 연결 |
| `Assets/Data/Struct/ActionContext.cs` | MoveRequest 또는 Guard/Attack에 필요한 최소 명시적 context |
| `Assets/Scripts/System/Action/MoveAction.cs` | 동적 목적지와 stopping distance, 목적지 소실 재계획 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | 복사된 Farmer 로직 제거, target→need→guard 우선순위 |
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | null-safe action rent/return 보조만 허용; public 요청 API 추가 금지 |
| `Assets/Scripts/System/Lib/ActionPool.cs` | Guard/Attack 생성과 null-safe 반환 |
| `Assets/Scripts/Enum/BuildingType.cs` | Guard 경계 구역 식별자 |
| `Assets/Scripts/Enum/ActionType.cs` | 기존 사용자 추가 Guard/Attack 보존; 중복 추가 금지 |
| `Assets/Scenes/SampleScene.unity` | Guard selector, cost asset, GuardPost, test target와 Layer 배선 |
| Guard prefab 또는 `NPCGirl.prefab` | combat sensor child, Trigger collider, perception과 per-NPC reference 연결 |

### 구현 후 문서

- 현재 구조에 맞게 `PublicMD/ProjectStructure.md`의 Guard 흐름과 파일 목록을 갱신한다.
- 실제 구현과 검증 결과를 `PublicMD/PROGRESS.md`에 기록한다.
- `PublicMD/Code_Evaluation_Result.md`는 독립 Codex 리뷰 agent만 수정한다.

## 6. 의존 방향

```text
NPCManager
  → WorkerNPC.Init(NPCType, NPCStat, Selector)

WorkerNPC (queue/action lifecycle owner)
  → BaseNPCActionSelector.RequestNewActionQueue(existing API)
  → IAction result

GuardActionSelector (decision/queue construction)
  → NPCStat read-only values
  → NPCComponent.GuardRuntimeState
  → NPCComponent.GuardPerception typed candidates
  → DestinationDB
  → ActionPool

GuardAction (execution)
  → NPCComponent movement
  → NPCStat need mutation
  → GuardPerception.HasCandidate read
  → GuardActionCost

ProximitySensor2D (generic physics detection)
  → Collider2D trigger callbacks
  → per-instance LayerMask

GuardPerception (Guard domain adapter)
  → ProximitySensor2D enter/exit notification
  → ICombatTarget candidate cache

GuardActionSelector target branch
  → GuardPerception candidates
  → GuardRuntimeState target write

MoveAction (generic execution)
  → MoveRequest
  → NPCComponent movement

AttackAction (execution)
  → NPCStat attack speed
  → GuardRuntimeState target read/clear
  → ICombatTarget damage API
  → AttackActionCost
```

금지하는 의존:

- `NPCStat → GuardAction/Selector/Enemy/Unity target`
- `GuardAction → GuardActionSelector`
- `GuardAction/AttackAction → WorkerNPC queue`
- `WorkerNPC → GuardAction concrete type`
- `MoveAction → GuardAction 또는 Enemy concrete type`
- `ProximitySensor2D → Guard/Enemy/ICombatTarget domain type`
- `GuardPerception → WorkerNPC queue 또는 selector 호출`
- selector field에 Guard별 현재 타겟 저장
- ScriptableObject에 현재 타겟이나 timer 저장

## 7. 세부 상태 전이

| 현재 상태 | 조건 | action 결과 | runtime target | 다음 selector 결과 |
|---|---|---|---|---|
| Guard | GuardPerception 후보 없음, 욕구 임계 미만 | Running | 없음 | 요청 없음 |
| Guard | 욕구 임계 도달 | ReplanRequested | 없음 | Move→Need 또는 Idle fallback |
| Guard | GuardPerception에 유효 후보 존재 | ReplanRequested | 아직 없음 | selector가 후보 선택·저장 후 Move→Attack 또는 Attack |
| MoveToTarget | 타겟 접근 성공 | Completed | 유지 | queue의 Attack 진행 |
| MoveToTarget | 타겟 소실 | ReplanRequested | clear | Guard 또는 need |
| Attack | 타겟 생존·범위 안 | Running | 유지 | 요청 없음 |
| Attack | 타겟 생존·범위 밖 | ReplanRequested | 유지 | Move→Attack |
| Attack | 타겟 사망/파괴 | ReplanRequested | clear | Guard 또는 need |
| Need | 정상 완료 | Completed | 없음 | queue 종료 후 Guard 재선택 |

## 8. 검증 계획

### 정적·빌드 검증

- `dotnet build Assembly-CSharp.csproj --no-restore`
- `ActionPool.Create`에 Guard와 Attack case 존재 확인
- `WorkerNPC`의 `NPCType.Farmer` hardcode 제거 확인
- `GuardActionSelector`에 FarmingActionCost와 Farmer 오류 문구가 남지 않았는지 확인
- Guard/Attack action이 selector 또는 WorkerNPC concrete type을 참조하지 않는지 검색
- ProximitySensor2D가 Guard, Enemy, ICombatTarget을 참조하지 않는지 검색
- GuardAction에 `Physics2D`, `OnTrigger`, `GetComponent<ICombatTarget>` 호출이 없는지 검색
- GuardPerception이 sensor notification을 enable/disable lifecycle에 맞춰 구독·해제하는지 확인
- `UnityEngine.Random` 직접 호출이 새로 생기지 않았는지 검색
- pooled action의 Clear가 target/timer/순찰점을 초기화하는지 확인

### Play Mode 시나리오

| ID | 초기 조건 | 기대 결과 |
|---|---|---|
| G-PT-01 | 욕구 낮음, 적 없음 | Guard가 경계 범위 안에서 30초 이상 동일 GuardAction으로 순찰 |
| G-PT-02 | Guard 임계치 직전 | 초당 증가 후 임계 도달 시 need queue로 전환 |
| G-PT-03 | 욕구 해결 완료 | queue 종료 후 GuardAction으로 복귀 |
| G-PT-04 | Guard 중 Enemy Layer의 ICombatTarget 진입 | sensor→perception 후보 생성, runner 전환 지점에 pending queue 폐기 후 Move→Attack |
| G-PT-05 | 적이 이미 공격 범위 안 | Move 없이 AttackAction 시작 |
| G-PT-06 | 적이 이동함 | 동적 위치 추적 후 공격 범위에서 Attack 시작 |
| G-PT-07 | 공격 중 적이 멀어짐 | target 유지, Replan, Move→Attack 재구성 |
| G-PT-08 | 공격 중 적 사망 | target clear, Replan, Guard 복귀 |
| G-PT-09 | 두 Guard와 두 적 | Guard별 타겟·timer·queue가 서로 오염되지 않음 |
| G-PT-10 | GuardPost/target/action cost 누락 | null exception·빈 queue spin 없이 진단과 Idle fallback |
| G-PT-11 | action 중 NPC disable/despawn | current/pending action 반환, target과 timer 정리 |
| G-PT-12 | Farmer 회귀 | 기존 이동·농사·식사·음료·수면 queue가 정상 동작 |
| G-PT-13 | sensor LayerMask 밖의 collider 진입 | sensor와 GuardPerception 후보에 추가되지 않음 |
| G-PT-14 | Enemy Layer지만 ICombatTarget 없는 collider 진입 | raw sensor 후보일 수 있으나 Guard 후보·재계획은 발생하지 않음 |
| G-PT-15 | 적 하나의 복수 collider 진입·순차 이탈 | Guard 후보 하나 유지, 마지막 collider 이탈 시에만 perception 후보 제거 |
| G-PT-16 | sensor 내부 적 오브젝트 파괴 | exit callback 유무와 관계없이 무효 후보 정리 |
| G-PT-17 | 서로 다른 LayerMask의 sensor 두 개 | 동일 ProximitySensor2D 코드가 각 설정에 맞는 후보만 제공 |
| G-PT-18 | 적이 sensor 안에 있는 상태로 GuardPerception 재활성화 | 현재 sensor snapshot과 동기화되어 후보를 놓치지 않음 |

### 시간 기반 검증

- 서로 다른 frame rate에서 Guard 욕구 10초 증가량을 비교한다.
- 공격 속도 0.5, 1.0, 2.0 attacks/sec에서 10초간 타격 횟수를 비교한다.
- Pause가 존재한다면 pause 중 Guard 욕구와 공격 timer가 증가하지 않는지 확인한다.

현재 프로젝트에 테스트 asmdef가 없으므로, 이 작업에서 전체 asmdef 마이그레이션을 함께 수행하지 않는다. 자동 테스트 기반이 별도로 마련되지 않았다면 명령행 빌드와 Play Mode 수동 검증 결과를 `PROGRESS.md`에 기록한다.

## 9. 위험과 대응

| 위험 | 결과 | 대응 |
|---|---|---|
| selector가 현재 타겟을 field로 보관 | 여러 Guard가 타겟을 공유 | GuardRuntimeState를 per-NPC로 생성 |
| NPCStat에 Unity target 저장 | stat 책임 오염, 파괴된 객체 처리 어려움 | target을 전용 runtime state에 저장 |
| action이 selector를 직접 호출 | 순환 의존과 queue 이중 변경 | ReplanRequested 결과를 runner가 처리 |
| queue 대기 action이 Init 시 Start | 실행 전 timer/상태 변경 | Init/Start 분리, runner만 Start |
| replan 때 pending action 미반환 | pool 누수와 이미 시작된 action 잔존 | runner의 단일 CancelAndReturnQueue 경로 |
| 고정 Vector3로 적 이동 | 도착 시 타겟과 거리 불일치 | generic dynamic MoveRequest 사용 |
| target 없는 상태에서 매 frame 재요청 | CPU 낭비와 로그 폭주 | long-running Guard 또는 Idle backoff |
| Guard와 Farmer가 같은 need tuning 사용 | 욕구 억제 차이가 사라짐 | Guard 전용 asset instance와 threshold |
| GuardAction이 직접 Physics/할당 | 책임 혼재와 다수 Guard 성능 저하 | ProximitySensor2D Trigger cache + GuardPerception typed cache |
| 범용 sensor가 ICombatTarget을 직접 판정 | 센서 재사용 불가 | sensor는 collider/Layer만, domain adapter가 capability 해석 |
| `OnTriggerStay2D`에서 반복 component 탐색 | 매 physics tick 비용과 중복 처리 | Enter/Exit notification에서 한 번 해석하고 cache 유지 |
| 적의 복수 collider를 각각 적으로 취급 | 중복 타겟·잘못된 Exit 처리 | target root 기준 중복 제거와 collider reference count |
| 파괴된 적이 Exit를 보내지 않음 | stale candidate와 null reference | perception 조회/상태 변경 시 Unity object validity 정리 |
| target 사망과 pool Clear가 타겟을 잘못 제거 | 다른 action이 같은 상태를 읽지 못함 | target clear 책임을 사망 확인 경로에만 둠 |
| 공격 속도 0 또는 지나친 값 | 무한 대기 또는 한 frame 과다 타격 | stat clamp와 action validation |

## 10. 명시적 제외 범위

이번 계획에는 다음을 포함하지 않는다.

- 적 wave 생성과 난이도 증가
- 적 AI의 이동·공격 정책
- 경비대 진형, 집단 전술과 타겟 공유
- 더 위험한 적이 나타났을 때 기존 살아 있는 타겟 교체
- 원거리 공격, 투사체, 광역 공격과 스킬
- 방어력, 회피, 치명타와 상태 이상
- 주민 부상, 사망, 치료와 게임오버
- 경계 교대 근무와 시간표
- NavMesh/pathfinding 도입
- 전투·경계 애니메이션, VFX와 SFX
- 전투 중 모든 생활 action을 외부 감지기로 강제 중단하는 기능
- 임의 C# Type을 Inspector에서 지정하는 reflection 기반 범용 DetectionFilter 프레임워크
- EnemyManager 전체 목록을 Guard마다 순회하는 감지 방식
- Spatial Hash, Quadtree, ECS 기반 대규모 감지 최적화
- Guard 구현과 무관한 Farmer/DestinationDecider 전면 리팩터링
- 새 selector 공용 요청 API

## 11. 결정 필요 항목

아래 항목은 구현 전에 사용자가 승인해야 한다. 권장안은 구현 가능한 최소 수직 슬라이스 기준이다.

| ID | 질문 | 권장안 |
|---|---|---|
| GQ-001 | 공격 속도의 단위는 무엇인가? | 초당 공격 횟수(attacks/sec) |
| GQ-002 | 첫 공격은 즉시인가, 한 interval 후인가? | 한 interval 후 첫 타격 |
| GQ-003 | 공격력은 어디에 속하는가? | NPC 고유 수치라면 NPCStat; 무기 도입 전까지 NPCStat 사용 |
| GQ-004 | 여러 적 중 누구를 선택하는가? | Guard 현재 위치에서 가장 가까운 살아 있는 적 |
| GQ-005 | Guard 경계 목적지 명칭은 무엇인가? | `BuildingType.GuardPost` |
| GQ-006 | Attack 중 욕구 임계 도달 시 중단하는가? | 중단하지 않고 현재 타겟이 무효가 될 때까지 공격 |
| GQ-007 | 타겟이 공격 범위를 벗어나면 어떻게 하는가? | 타겟 유지 후 Replan하여 Move→Attack |
| GQ-008 | 경계 순찰점 생성은 어떤 방식인가? | 전역 Random 없이 주입 난수 또는 결정적 지점 순환 |
| GQ-009 | Guard 욕구 중단 임계치와 초당 증가량은 얼마인가? | 수치 확정 전 asset placeholder로 두되 Farmer보다 높은 임계치·낮은 증가량 유지 |
| GQ-010 | combat sensor LayerMask, Trigger 반경과 공격 반경은 얼마인가? | LayerMask는 sensor instance, Trigger 반경은 CircleCollider2D/prefab, 공격 반경은 Attack 전용 asset에서 설정 |

## 12. 구현 승인 기준

사용자가 이 계획과 GQ-001~010의 결정을 승인하기 전에는 구현 파일을 수정하지 않는다.

승인 후 구현자는 다음 문장을 작업 범위로 사용한다.

> 기존 `RequestNewActionQueue` public API를 유지하면서 action result 기반 재계획을 추가한다. 범용 `ProximitySensor2D`가 LayerMask 기반 Trigger 후보를 제공하고, per-NPC `GuardPerception`이 살아 있는 `ICombatTarget` 후보만 캐시하며, GuardAction은 후보 존재 시 재계획만 요청한다. GuardActionSelector가 실제 타겟을 선택해 `GuardRuntimeState`에 저장하고 `Guard 장기 순찰 → queue 전체 중단 → 동적 Move → Attack 장기 반복 → 타겟 사망 후 Guard 복귀`를 Phase A~D 순서로 구현한다. Guard의 욕구는 초당 데이터 기반으로 증가하며 Farmer보다 높은 임계치까지 억제한다.
