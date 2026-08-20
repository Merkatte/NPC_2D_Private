# PROGRESS

## Current Reset Snapshot

2026-08-01: The project has been reset to a new lightweight NPC/action skeleton. The old `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph, animation, combat, recruitment, and UI implementation described by older IMP records is not present in the current `Assets/Scripts` tree.

Current direction: keep `WorkerNPC` as a narrow actor root and tick bridge. Do not turn it into the one large script that every NPC-related caller must depend on. Stats belong in `NPCStat`, Unity object references belong in `NPCComponent` or narrower component holders, behavior choice belongs in selectors, and behavior execution belongs in actions or an action runner.

Current active script structure:

- `Assets/Scripts/Actor/WorkerNPC.cs`
- `Assets/Scripts/Manager/NPCManager.cs`
- `Assets/Scripts/Interface/IAction.cs`
- `Assets/Scripts/Enum/ActionType.cs`
- `Assets/Scripts/Enum/NPCType.cs`
- `Assets/Scripts/System/Actor/NPCComponent.cs`
- `Assets/Scripts/System/Actor/NPCStat.cs`
- `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs`
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
- `Assets/Scripts/System/Action/MoveAction.cs`
- `Assets/Scripts/System/Action/EatAction.cs`
- `Assets/Scripts/System/Action/FarmingAction.cs`
- `Assets/Scripts/System/Action/SleepAction.cs`
- `Assets/Scripts/System/Lib/ActionPool.cs`
- `Assets/Scripts/System/Lib/DestinationDB.cs`

Verification on 2026-08-01:

- `rg --files Assets/Scripts` confirms the reset script set above.
- `Assets/BehaviorGraph` is absent.
- `Assembly-CSharp.csproj` contains compile entries for the current 15 scripts.
- `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors.

Docs updated on 2026-08-01:

- `PublicMD/ProjectStructure.md` now describes the reset skeleton.
- `PublicMD/CodeConvention.md` now uses the new `WorkerNPC`/`NPCComponent`/`NPCStat`/`IAction` responsibility boundaries.
- `PublicMD/Code_Evaluation_Result.md` now contains a current code review of the reset skeleton.

Immediate next actions:

1. Decide the `IAction` completion contract before implementing action queues.
2. Add narrow access surfaces or an explicit action context if actions need `NPCStat`, `Transform`, animation, or destination access.
3. Replace action `NotImplementedException` stubs with safe lifecycle behavior before wiring runtime execution.
4. Fix `NPCStat` move speed initialization and clamp logic.
5. Keep `WorkerNPC` narrow; do not make it the dependency bucket for every NPC concern.

## Current Status
IMP-026 completed on 2026-08-20 (코드 구현 완료 / Play Mode 검증은 씬 재배선 대기): `PublicMD/Code_Evaluation_Result.md`(Codex, 2026-08-20)의 stat/cost 책임 경계 리뷰를 Opus Plan Mode로 계획하고(계획 파일 `purring-waddling-rainbow.md`, Codex 2회 계획 리뷰 반영) 승인된 범위로 구현했다. 목표는 `GuardActionCost`/`AttackActionCost`(공유 ScriptableObject)에 섞여 있던 "행동 비용/판정 기준"과 "개체별 성장 능력치"를 분리하고, `NPCStat`/`IStatView`가 모든 NPC에게 전투 필드를 강제하던 문제(M-02)와 `NPCManager`가 selector를 enum index로만 고르고 stat은 npcType과 무관하게 생성하던 문제(M-04)를 함께 고치는 것이었다. 사용자 결정: 직업은 생성 후 고정(런타임 전직 없음), `AttackRange`+`GuardRadius` 모두 per-NPC stat, 이번 범위는 stat 분리까지(detection range 동기화·레벨업 progression은 제외), NPC 생성 경로를 NPCType keyed 등록 구조로 교체.

`ICombatStatView`(`AttackPower`/`AttackSpeed`/`AttackRange`, `IStatView` 비상속 — `AttackAction`이 health/needs를 전혀 안 쓰므로)와 이를 상속하는 `IGuardStatView`(`GuardRadius` 추가)를 신설하고, `GuardStat : NPCStat, IGuardStatView`가 이 능력치들을 소유하는 단일 runtime 객체로 구현했다(런타임에 Guard 한 명당 `NPCStat`/`CombatStat`/`GuardStat`을 따로 만드는 게 아니라 `GuardStat` 하나가 모든 interface reference를 만족). 범위 판정(`sqrMagnitude` 비교)은 stat이 아니라 신규 static utility `CombatRange.IsInRange(from, to, range)`로 분리해 stat이 Unity 위치 판정을 소유하지 않게 했다. `GuardAction`/`AttackAction`은 stat을 `Start()`에서 1회만 `as IGuardStatView`/`as ICombatStatView`로 캐스트해 필드에 캐싱하고 `Clear()`에서 null로 되돌린다(Tick마다 재캐스트하지 않음 — 1차 계획 리뷰 반영). `GuardActionSelector`는 여러 NPC가 공유하는 scene instance이므로 캐스트 결과를 필드가 아니라 `RequestNewActionQueue` 안의 local parameter로만 하위 builder 메서드(`TryBuildCombatQueue`/`BuildCombatQueue`/`BuildGuardQueue`)에 전달한다.

`NPCStat`/`IStatView`에서 `GetAttackPower`/`GetAttackSpeed`를 제거하고(→`GuardStat`), 호출부 0건이었던 `Current*Percentage` 3개·4-인자 ctor·버그 있는(미사용) `ChangeMoveSpeed`·`ActionContext.HasStat`·`DefaultActionCost.MovePerThirst/Hunger/Fatigue`(전부 grep으로 호출부 0건 확인 후 제거)도 함께 정리했다. `GuardActionCost`에서 `GuardRadius`를 제거하고(→`GuardStat`), selector에 매직넘버 `0.9f`로 남아 있던 attack stopping-distance 비율을 `AttackStoppingDistanceRatio`라는 named tuning 값으로 새로 옮겼다(2차 계획 리뷰 반영 — 개체별 성장치가 아니라 공유 행동 정책이므로 tuning에 남김).

`M-04`(selector-stat 역할 불일치) 수정을 위해 `BaseNPCActionSelector.CanUseStat(NPCStat)`(기본 `stat != null`, `GuardActionSelector`는 `stat is IGuardStatView`로 override) 계약을 신설하고, `NPCManager`를 `List<BaseNPCActionSelector> _selectors`(enum index 접근) 대신 `List<NPCCreationEntry> _creationEntries`(`NPCType`+`Selector`+`NPCStatDefinition`을 한 row로 묶는 `[System.Serializable]` 내부 클래스, `DataManager.CostInfo`와 동일한 배치 패턴)로 재작성했다. `Awake()`가 null entry/누락 selector·stat definition/중복 `NPCType`을 검증하고, `CreateNPC()`는 `_workerPool` 누락 → stat 생성 실패 또는 `CanUseStat` 불일치 → `WorkerPool.GetWorker()` null까지 실패 경로를 전부 갖춘 뒤 성공 시에만 `_workers[npcType]`에 등록한다(기존에 `_workers` 등록 자체가 누락돼 있던 버그를 creation 경로를 만지는 김에 함께 수정 — 2차 계획 리뷰 반영). stat 생성은 `NPCStatDefinition`(abstract `CreateRuntimeStat()`) 추상화로 옮겨 `DefaultStatContext`(기반 타입만 `ScriptableObject`→`NPCStatDefinition`으로 변경, GUID/asset 인스턴스 보존)와 신규 `GuardStatDefinition`이 각각 구현한다. `DataManager.GetStat()`/`IDataManager.GetStat()`(유일 호출부였던 `NPCManager`가 대체됨)과 `DataManager._defaultStatContext` 필드를 제거했다.

`AttackActionCost`는 유일한 실사용 필드(`AttackRange`)가 `GuardStat`으로 이동하며 빈 껍데기만 남아 완전히 삭제했다(`.cs`/`.cs.meta`/`.asset`/`.asset.meta` 4개 파일). 삭제 순서는 크래시 방지를 위해 고정했다: 코드 의존 제거 → `GuardTest.unity`의 `DataManager._costInfos`에서 해당 entry 제거(자산을 먼저 지우면 `DataManager.Awake()`의 `item.actionCost.MyType` 접근이 dangling reference로 NRE를 던짐) → 4개 파일 삭제 → `Assembly-CSharp.csproj` 정리. `SampleScene.unity`는 애초에 `AttackActionCost`/`GuardActionCost`를 등록하지 않은 상태였어서(구형 씬, `GuardTest.unity`가 실제 작업 씬) 별도 조치가 필요 없었다.

**의도적 제외 범위**: detection range → `CircleCollider2D` 동기화(Codex M-03, `GuardStat`에 `DetectionRange` 필드를 미리 추가하지 않음), 레벨업/progression 시스템(`GuardStat`의 4개 capability 필드는 이번 slice에서 생성 시점에 결정되는 **immutable** 값이다 — 개체별 초기 차이는 `GuardStatDefinition` 에셋으로 표현 가능하지만 런타임 성장은 아직 불가능하며, 지금 쓰이지 않을 `ApplyGrowth()` 등을 미리 만들지 않았다), `FarmStat` 신설(코드 어디에도 농부 숙련도 데이터가 없어 빈 서브클래스를 만들지 않음, Farmer/Cook은 `DefaultStatContext`로 plain `NPCStat` 생성), `WorkerPool`의 NPCType별 분리, `GuardActionCost`/`FarmingActionCost`의 `Tuning`/`Policy` 개명(Codex L-01, cosmetic).

IMP-025 completed on 2026-08-19 (코드 구현 완료 / Play Mode 검증은 씬 배선 대기): `PublicMD/Guard_Action_Implementation_Plan.md`(rev 3, Codex 2회 계획 리뷰 반영)의 Phase A~D를 한 번의 승인된 패스로 구현했다. 목표는 `WorkerNPC → BaseNPCActionSelector.RequestNewActionQueue(...) → Queue<IAction>` 구조를 유지하면서, Guard가 경계 구역에서 장기 `GuardAction`으로 순찰하다가 Guard 전용 욕구 임계치에 도달하거나 유효한 전투 대상이 감지되면 남은 queue 전체를 반환하고 재계획해 `Move(동적 타겟) → 장기 AttackAction → 타겟 사망 → Guard 복귀` loop를 완성하는 것이었다.

Phase A(action 결과 계약과 queue lifecycle): `ActionResult`(Running/Completed/ReplanRequested/Failed) 추가, `IAction.CheckComplete()`를 `Result` property로 대체. `DefaultAction.Init()`이 더 이상 `Start()`를 호출하지 않고, `Complete()`/`RequestReplan()`/`Fail(reason)` 세 종료 경로로 분리했다(`Fail`은 원인을 한 번 로그로 남긴다). `WorkerNPC`가 queue lifecycle을 단독 소유하도록 재작성: `CancelAndReturnQueue()`가 현재 action과 남은 queue를 각각 정확히 한 번 반환하고, `AdvanceQueue()`는 프레임당 최대 1회만 `RequestNewActionQueue`를 호출한다. `NPCType`을 저장해 `NPCType.Farmer` 하드코딩을 제거했고, 풀 재사용을 고려해 `_isInitialized` 가드와 `NPCComponent.ResetRuntimeState()`(Phase A에서는 빈 구현)를 `Init()`/`OnDisable()` 양쪽에 추가했다. `MoveAction`/`EatAction`/`DrinkAction`/`SleepAction`/`FarmingAction`/`IdleAction`을 새 계약으로 마이그레이션하면서, base.Start() 실패 후 하위 override가 `Complete()`를 덮어써 결과를 오염시키던 잠재 버그(EatAction/DrinkAction)와 컴포넌트 유실 시 `Stop()`만 호출해 영원히 멈추던 버그(FarmingAction/MoveAction)를 함께 고쳤다. `ActionPool.GetAction`/`ReturnAction`을 null-safe하게 만들고, `FarmerActionSelector`의 queue 조립을 대여 실패 시 전체 롤백하는 트랜잭션 방식으로 바꿨다(결정 로직 자체는 변경 없음).

Phase B(비전투 Guard 순찰과 욕구 억제): `GuardActionCost`(경계 반경/순찰 도착 거리/순찰점 개수, 초당 욕구 증가량, Guard 전용 중단 임계치 0..1) 신설. `GuardAction`은 결정적 순찰점 순환(반경 위 N개 점 인덱스 순회, `UnityEngine.Random` 미사용)으로 이동하고 매 Tick 욕구를 직접 증가시키며(`StatEffect` 신규 할당 없음), 정상 순찰 중에는 `Complete()`를 반환하지 않는다. `BuildingType.GuardPost`를 enum 끝에 추가(기존 직렬화 값 보존). `GuardActionSelector`를 Farmer 복사본에서 전면 재작성해 비전투 분기만 구현: need 임계 초과 시 기존 job-neutral `DestinationDecider.Decide(workCost: null)`로 `Move → Eat/Drink/Sleep`, 아니면 `Move → Guard`/`Guard`, 설정 누락 시 `Idle` backoff.

Phase C(감지·타겟 인프라와 동적 이동): `ICombatTarget`/`IMoveTarget` 최소 계약, `CombatTargetHandle`(Target+Owner 쌍으로 유효성 판정하는 plain C# handle, `IMoveTarget` 구현 — Unity 파괴 객체 null 판정 문제를 Owner truthiness로 해결), `ProximitySensor2D`(LayerMask 기반 범용 Trigger2D 감지기, `OnTriggerEnter2D`/`Exit2D`만 사용, 파괴된 collider 정리는 명시적 `Prune()`에서만 exit 알림과 함께 수행, `OnDisable()`에서 보유 collider 전체 exit 알림 후 정리), `GuardPerception`(`Dictionary<Collider2D, TargetEntry>` 역방향 매핑으로 한 적의 복수 collider를 하나의 후보로 중복 제거, 자체 `Update()`에서 sensor+자신의 `Prune()` 호출, `CopyCandidatesTo` 버퍼 메서드로 selector에 후보 노출), `GuardRuntimeState`(NPC별 `CombatTargetHandle` 하나를 직접 소유 — perception이 후보를 잃어도 selector가 명시적으로 지우기 전까지 타겟 유지), `MoveRequest`(고정 `Vector3` 또는 `IMoveTarget` + stopping distance), `Enemy`(최소 `ICombatTarget` 테스트 표적, AI 없음)를 신설했다. `NPCComponent`가 `GuardPerception` 참조와 per-NPC `GuardRuntimeState`를 노출하고 `ResetRuntimeState()`가 타겟을 지운다. `ActionContext`에 `MoveRequest? moveRequest`를 생성자 맨 끝에 추가(기존 호출부 보존). `MoveAction`이 `MoveRequest`를 지원하도록 확장하고, 항상 0.1f로 고정되어 있던 stopping distance 버그를 제거했으며, 동적 타겟이 사라지면 재계획을 요청한다.

Phase D(장기 AttackAction과 전체 loop 연결): `AttackActionCost`(공격 반경) 신설. `AttackAction`은 매 Tick `GuardRuntimeState`에서 타겟을 다시 읽고(캐시하지 않음), `interval = 1/AttackSpeed`로 타이머를 누적해 `MaxHitsPerTick` 한도 내에서 반복 타격한다. 타격 전 유효성·거리 재검사 → 피해 적용 → 타격 직후 생존 재검사 순서를 지켜 같은 Tick에서 죽은 대상에 중복 피해를 주지 않으며, hit 상한에 걸려 루프가 끝난 경우에만 잔여 timer를 `interval * MaxHitsPerTick`으로 clamp한다(사망/사거리 이탈로 끝난 경우는 clamp하지 않음). `Clear()`는 timer만 초기화하고 타겟은 지우지 않는다. `NPCStat`/`IStatView`/`DefaultStatContext`에 공격력·공격 속도(attacks/sec)를 기존 생성자 끝에 default 파라미터로 추가했다(유일한 10-인자 호출부인 `DefaultStatContext.CreateStat()`도 갱신). `GuardAction`이 매 Tick `GuardPerception.HasCandidate`를 욕구 임계 검사보다 먼저 확인해 적 감지가 욕구보다 우선하도록 했다(타겟 선택·저장은 하지 않음). `GuardActionSelector`가 재계획마다 전투 분기를 최우선으로 시도: `GuardRuntimeState`에 유효 타겟이 있으면 그대로, 없으면 `GuardPerception` 후보 중 가장 가까운 살아있는 적을 선택해 `SetTarget` 후, 공격 범위 밖이면 `Move(dynamic MoveRequest, stopping distance = AttackRange×0.9) → Attack`, 안이면 `Attack`만 queue에 넣는다.

**씬 배선 (미완료, 의도된 범위 제외)**: 이번 패스는 사용자 승인에 따라 C# 코드와 ScriptableObject 클래스 정의만 구현했다. `SampleScene.unity`/`NPCGirl.prefab`의 GameObject·collider·Layer·asset 인스턴스 연결은 다루지 않았으며, 사용자가 Unity 에디터에서 직접 수행해야 한다. 남은 배선 항목은 아래 "다음 배선 작업" 참고. 이 배선이 끝나기 전까지 Guard 계획 §8의 Play Mode 시나리오(G-PT-01~18)는 실행할 수 없다.

### 다음 배선 작업 (사용자, Unity 에디터)

1. `GuardActionSelector` GameObject 생성 + `dataManager`/`actionPool`/`_destinationDB`/Guard 전용 `NPCDecisionTuning`(Farmer보다 높은 `CriticalNeedThreshold` 권장) 연결, `NPCManager._selectors[1]`을 이 selector로 교체(현재 3칸 모두 Farmer selector를 가리킴).
2. `GuardActionCost.asset`, `AttackActionCost.asset` 생성 후 `DataManager._costInfos`에 등록. `GuardActionCost`의 초당 욕구 증가량은 `FarmingActionCost.asset`의 `FarmingActionPerHunger/Thirst/Fatigue`(각 10) 대비 `증가량 × 3초 < 10`을 만족해야 한다(기본값 0.3은 이 조건을 만족).
3. `DestinationDB`에 `GuardPost` 엔트리 추가.
4. `NPCGirl.prefab`(또는 Guard 전용 prefab)에 combat sensor 자식 GameObject + `CircleCollider2D(IsTrigger)` + `ProximitySensor2D`(LayerMask=Enemy) + `GuardPerception` 구성, `NPCComponent._guardPerception` 연결.
5. Enemy Layer 생성, `Enemy` 표적 배치(Collider2D + 한쪽에 Rigidbody2D), Physics 2D Layer Collision Matrix 제한, sensor `_detectionMask` 설정.
6. `DefaultStatContext`(또는 Guard 전용 stat context)에 공격력·공격 속도 값 입력.

IMP-024 completed on 2026-08-14 (코드 구현 완료 / 런타임 검증 blocked): `PublicMD/DestinationDecider_Refactor_Plan.md`(Codex 작성)를 검토해 Phase A~D를 구현했다(Phase E 정식 문서 개정과 자동 테스트 도입은 사용자와 합의해 범위 제외, 아래 후속 과제로 기록). 목표는 판단(`DestinationDecider`)과 실행(`Pub`/`DrinkAction`/`EatAction`)이 서로 다른 결과를 예측·적용하던 문제(카테고리만 보고 랜덤 아이템 지급, 퍼센트 vs 절대값 need 예측 불일치, 안전 작업 0회에도 강제 1회 작업)를 없애는 것이었다.

Phase 0(선행 버그): `FarmingActionCost.asset`의 `FarmingAcitonPerThirst` 오타로 인해 런타임 갈증 작업비용이 항상 0이었던 버그를 수정(`FormerlySerializedAs`로 마이그레이션 병행). `DataManager`의 비용 딕셔너리 구성을 `Start()`→`Awake()`로 옮겨 `FarmerActionSelector.Start()`와의 초기화 순서 경쟁을 제거. `FarmingAction.Clear()` 부재(재사용 시 진행시간 미초기화)와 캐스팅 실패 시 큐가 멈추는 문제를 수정.

Phase A(아이템 선택-실행 일치): `IInteractionProvider`를 `AppendOptions(type, buffer)`(조회) + `TryInteraction(request)`(지정 실행) 계약으로 재정의. `Pub`은 `UnityEngine.Random`을 완전히 제거하고 요청받은 `ItemId`를 그대로 실행하며, 아이템을 못 찾으면 스탯 변화 없이 실패한다. `ActionContext`에 `InteractRequest? Request`를 추가하고, `DefaultAction.Clear()`에서 `actionContext`를 초기화해 풀 재사용 시 이전 요청이 새는 것을 막았다. `EatAction`/`DrinkAction`은 이제 지정된 아이템만 실행한다.

Phase B: `NPCDecisionTuning` ScriptableObject 신설(`DangerThreshold`/`CriticalNeedThreshold`는 0~1 정규화 값, `MinimumWorkBatch` 기본 2, `MaximumWorkBatch`, `SwitchMargin`, 각종 utility weight). `OnValidate()`로 필드 간 관계(Danger≤Critical, Min≤Max, 가중치 음수 불가)를 강제한다.

Phase C: `DestinationDB`에 중복 제거된 `RegisteredKeys` 캐시와 `EnsureInitialized()` 지연 초기화를 추가해 `Awake` 순서 의존 NRE와 `_destionations == null` 크래시를 제거. `NPCDecision`을 다단계 배열에서 "한 결정 = 한 목적지" 단일 구조체로 축소(`NPCDecisionStep`/`EstimatedWorkCount`/`NextRequiredIntent`/`PrimaryIntent` 삭제, 사용처 0건 확인 후 제거)하고 `Idle()` 정적 팩토리를 추가. `ActionType.Idle`/`NPCIntent.Idle`을 enum 끝에 추가하고 `IdleAction`+`ActionPool` 배선을 신설해, 빈 결정이 나와도 `WorkerNPC`가 매 프레임 재판단 스핀에 빠지지 않게 했다. `DestinationDecider`는 전면 재작성: need를 퍼센트가 아닌 원본값+max로 예측, 등록된 모든 provider에서 구체 아이템 옵션을 수집, 안전 작업 횟수를 나눗셈이 아닌 반복 시뮬레이션으로 계산(`Mathf.Max(1,workCount)` 제거), `MinimumWorkBatch` 미만이면 Work 후보 자체를 배제, critical 상태는 판단 시작 시점에 critical이었던 need 집합 기준 3단계 정책(1순위 엄격 무악화+개선 / 2순위 최댓값 감소 / 3순위 risk 합 감소)으로 선택해 부작용 있는 아이템 때문에 영구 Idle에 빠지지 않게 했고, utility·거리·ItemId·ActionType·BuildingType 순의 완전 결정적 타이브레이크를 적용했다.

Phase D: `FarmerActionSelector`는 단일 `NPCDecision`으로부터 `ActionContext` 하나를 구성한다(Work는 `FarmingActionCost`, Eat/Drink는 provider+`InteractRequest`, Sleep은 둘 다 없음 — `SleepAction`이 자체 전량 회복 계산을 갖고 있으므로). `_decider`/`_decisionTuning`/`stat` 중 하나라도 없으면 빈 큐 대신 명시적 `Idle` 결정으로 낮춘다(빈 큐 반환 시 매 프레임 재요청 스핀 재발 방지). `WorkerNPC.cs`, `Assets/Scenes/SampleScene.unity`는 계획대로 수정하지 않았다.

**런타임 Blocker (기능 미완료 사유)**: (1) `InteractableManager`가 `SampleScene.unity`에 배치돼 있지 않아 `BaseInteractable.Init`이 호출되지 않고 모든 provider의 아이템 딕셔너리가 `null`이다. (2) `FarmerActionSelector._decisionTuning`이 새로 만든 `NPCDecisionTuning.asset`에 연결돼 있지 않다. 두 가지 모두 Unity 에디터에서 사용자가 직접 연결해야 하며, 연결 전까지 드링크/이트/작업 배치 흐름이 Play Mode에서 실제로 동작하지 않는다.

**미실행 수동 스모크 테스트**: `Assets/TestOnly/DecisionSmokeItemData.csv`(체력/갈증 트레이드오프 아이템 2종 추가)를 `ItemDataContext._itemTextData`에 임시 연결해 (a) 요청한 아이템이 실제 실행되는지, (b) 체력에 따라 `SpringWater`/`StrongLiquor` 선택이 뒤집히는지, (c) `MinimumWorkBatch` 미만에서 강제 1회 작업이 없는지, (d) 작업 배치 N회 전부 실행되는지, (e) critical 상태에서 부작용 아이템이 있어도 Idle에 고착되지 않는지, (f) 보급·작업 모두 불가할 때 매 프레임 재판단 스핀이 없는지 검증 예정.

IMP-023 completed on 2026-08-11: `DefaultStatContext` ScriptableObject를 현재 `NPCStat` 정보의 기본값 source로 구현했다. 에셋은 이름, 현재/최대 체력, 이동 속도, 피로/허기/갈증 현재값과 최대값만 소유하며 runtime state를 저장하지 않고 `CreateStat()`로 새 `NPCStat` 인스턴스를 생성한다. `NPCStat`에는 need 현재/최대값을 받는 생성자 overload를 추가하고 입력값을 각 max 기준으로 clamp한다. `NPCManager`와 `DataManager.GetStat()`은 연결된 `DefaultStatContext`가 있으면 에셋 기반 stat을 사용하고, 없으면 기존 임시 하드코딩 fallback을 유지한다. 빌드 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기, run id `20260811-175130-135`).

IMP-022 completed on 2026-08-02: `DestinationDecider`/`FarmerActionSelector`의 판단 계층과 큐 조립 계층을 분리했다. `DestinationDecider`(순수 C#, `System/Lib`, 직업 공용)는 `Decide(NPCStat, NPCType, Vector3) -> NPCDecision`을 반환하며, 순서 있는 `NPCDecisionStep[]`으로 "이동→보급→이동→작업" 체인을 표현한다. 보급 후보(Drink/Eat/Sleep)는 순열 전체 열거 대신 탐욕적 한계이득 체인으로 하나씩 선택하고, critical 임계값(95%) 초과 시 즉시 해당 need로 단락한다. `FarmerActionSelector`는 `NPCDecision.Steps`를 순회하며 `Queue<IAction>`을 조립하기만 하고 계산식을 갖지 않는다(grep으로 확인). `ActionType.Drink`/`DrinkAction`(EatAction과 동일한 스텁 스타일) 추가, `ActionPool`에 배선. `NPCStat`의 `Current*Percentage`에 0 나눗셈 방어(`GetPercentage`) 추가. `DestinationDB.DestinationInfo`에 `[System.Serializable]` 추가(이전에는 Inspector에 노출되지 않아 런타임 딕셔너리가 항상 비어 있었음). 설계 근거는 `PublicMD/NPC_Decision_System_Plan.md`에 별도 기록. 빌드 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기).
IMP-021 completed on 2026-07-22: ScriptableObject asset instances were moved out of `Assets/Scripts` into `Assets/Data` while keeping their `.meta` files with the assets so Unity GUID references remain stable. ScriptableObject class definitions remain in their owning script folders.
IMP-020 completed on 2026-07-03: popup infrastructure now supports lazy Instantiate, inactive caching, LRU trimming, and a PopupManager-compatible component name. Command-line C# build passed with 0 warnings and 0 errors.
주민 모집 UI 뼈대 스크립트를 구현했다(IMP-019). 공유 UI 인프라(`IPopup`, `UIPopupType`, `UIManager`) 3종과 모집 전용 UI 뼈대(`RecruitmentPopup`, `RecruitmentCandidateSlot`) 2종을 신규 생성했다. `UIManager`는 LIFO 팝업 스택 + ESC 처리(Input System)를 담당하며 도메인 로직을 소유하지 않는다. `RecruitmentPopup`은 도메인 API 4종(`Candidates`, `CanRecruit`, `TryRecruit`, `IResidentCandidateView`)만 사용하며 닫기는 항상 `UIManager.ClosePopup`을 경유해 스택 동기화를 보장한다. 빌드 오류 0, 경고 0.

## Completed Tasks
| Task ID | Date | Summary | Evidence | Related REQs |
|---|---|---|---|---|
| IMP-026 | 2026-08-20 | Stat/Cost 책임 분리: `ICombatStatView`/`IGuardStatView`/`GuardStat` 신설, `IStatView`/`NPCStat`에서 전투 필드 제거, `GuardActionCost`에서 `GuardRadius` 제거하고 `AttackStoppingDistanceRatio` tuning 추가, `AttackActionCost` 완전 삭제, `NPCManager`를 `NPCCreationEntry`(selector+stat definition 결합, `CanUseStat` 검증) 기반으로 재작성, `NPCStatDefinition`/`GuardStatDefinition` 팩토리 도입. 상세는 위 Current Status 참고. | 아래 검증 행 참조. | 사용자 요청: `Code_Evaluation_Result.md` stat/cost 경계 리뷰 반영 |
| IMP-026 | 2026-08-20 | Command-line C# build | `dotnet build Assembly-CSharp.csproj --no-restore` — 신규 6개 `.cs` 추가, `AttackActionCost.cs` 제거 후 오류 0, 경고 0. |  |
| IMP-026 | 2026-08-20 | 정적 grep 검증 | `Assets` 전체 `AttackActionCost` 참조 0건, `Assets/Scripts` 전체 `GetAttackPower`/`GetAttackSpeed` 0건, `GuardActionCost.cs`에 `GuardRadius` 0건, `Assets/Scripts/System/Action`의 `as IGuardStatView`/`as ICombatStatView` 캐스트가 `GuardAction.Start()`/`AttackAction.Start()`에만 존재(Tick 안에는 0건). `git diff --check` — 이번 세션에서 수정한 파일에 whitespace 오류 없음(`NPCGirl.prefab`의 기존 trailing whitespace는 이번 변경과 무관한 이전 상태). |  |
| IMP-026 | 2026-08-20 | 씬 편집(크래시 방지 목적, 신규 배선 아님) | `GuardTest.unity`의 `DataManager._costInfos`에서 삭제되는 `AttackActionCost.asset`을 가리키던 entry 제거(자산 삭제 전 dangling reference로 인한 `Awake()` NRE 방지). `SampleScene.unity`는 애초에 `AttackActionCost`/`GuardActionCost`를 등록하지 않아 조치 불필요. |  |
| IMP-026 | 2026-08-20 | Codex review agent | **미실행**. skill이 요구하는 launch script(`start_codex_review_agent.ps1`)가 `.codex\agent-runs\`에 디렉터리/프롬프트 파일을 생성하고 `.codex\skills\...`를 존재 확인하는데, 이는 `CLAUDE.md`의 "Claude는 어떤 이유로도 `.codex` 내용을 읽거나 참조하지 말고 수정·삭제·생성하지 말라"는 명시적 override 지시와 충돌해 실행하지 않았다. 사용자에게 직접 실행할 수 있는 명령을 안내함. |  |
| IMP-026 | 2026-08-20 | 런타임 Blocker (의도된 범위 제외, 버그 아님) | `NPCManager._selectors`(List\<BaseNPCActionSelector\>) → `_creationEntries`(List\<NPCCreationEntry\>)로 필드 타입이 바뀌어 기존 씬 직렬화 데이터를 Unity가 이어받지 못한다(크래시 없이 에러 로그 후 NPC 생성 실패). `GuardTest.unity`/`SampleScene.unity`의 `NPCManager._creationEntries` 재배선과 신규 `GuardStatDefinition.asset` 생성이 사용자 작업으로 남아 있다. |  |
| IMP-025 | 2026-08-19 | `Guard_Action_Implementation_Plan.md` rev 3 Phase A~D 구현. ActionResult 계약, WorkerNPC queue lifecycle 단독 소유, 장기 GuardAction(순찰+욕구 억제), ProximitySensor2D+GuardPerception+GuardRuntimeState+CombatTargetHandle 감지·타겟 인프라, 장기 AttackAction, GuardActionSelector 전투/욕구/순찰 우선순위 통합. 상세는 위 Current Status 참고. | 아래 검증 행 참조. | 사용자 요청: Guard 장기 행동·전투 전환 구현 |
| IMP-025 | 2026-08-19 | Command-line C# build (Phase A~D 각각 독립 검증) | `dotnet build Assembly-CSharp.csproj --no-restore` — Phase A/B/C/D 각 종료 시점 모두 오류 0, 경고 0. 신규 파일은 매 Phase마다 `Assembly-CSharp.csproj`에 수동 `<Compile Include>` 추가 후 확인(Unity 미실행, IMP-022/024와 동일 절차). |  |
| IMP-025 | 2026-08-19 | 정적 grep 검증 | `ActionPool.Create`에 Guard/Attack case 존재, `WorkerNPC`에 `NPCType.Farmer` 하드코딩 0건, `GuardActionSelector`에 FarmingActionCost/Farmer 문구 0건, `GuardAction`에 Physics2D/OnTrigger/GetComponent 0건, `ProximitySensor2D`에 Guard/Enemy/ICombatTarget 실제 참조 0건(doc-comment 1건만), `MoveAction`에 ICombatTarget/Enemy 0건, `Assets/Scripts` 전체 `UnityEngine.Random` 신규 호출 0건. |  |
| IMP-025 | 2026-08-19 | Codex review agent | 에이전트 런치 성공(비동기, PID 62428, run id `20260819-014129-214`). 결과는 `PublicMD/Code_Evaluation_Result.md`에 기록됨(아직 미수신 — 수신된 리뷰 결과는 이 표에 기록하지 않음). |  |
| IMP-025 | 2026-08-19 | 런타임 Blocker (의도된 범위 제외, 버그 아님) | 사용자 승인에 따라 이번 구현은 C# 코드/ScriptableObject 클래스 정의만 포함하고 `SampleScene.unity`/`NPCGirl.prefab` YAML은 의도적으로 수정하지 않았다. GuardActionSelector 씬 배선, GuardActionCost/AttackActionCost asset 인스턴스, GuardPost destination, combat sensor prefab 구성, Enemy Layer가 모두 미완료라 Play Mode 시나리오(G-PT-01~18)는 사용자 배선 후 실행 가능하다. |  |
| IMP-024 | 2026-08-14 | `DestinationDecider_Refactor_Plan.md` Phase A~D 구현. `IInteractionProvider`를 옵션 조회+지정 실행 계약으로 재정의하고 `Pub`에서 `UnityEngine.Random` 제거, `DestinationDecider` 전면 재작성(원본값 예측, 반복 시뮬레이션 안전 작업 횟수, 3단계 critical 정책, 결정적 타이브레이크), `NPCDecision` 단일 스텝화, `IdleAction` 신설, `FarmerActionSelector` 단일 결정→큐 변환. 상세는 위 Current Status 참고. | 아래 검증 행 참조. | 사용자 요청: Codex DestinationDecider 리팩터링 계획 검토·구현 |
| IMP-024 | 2026-08-14 | Command-line C# build | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. (신규 파일 `IdleAction.cs`가 Unity 미실행 상태에서 `Assembly-CSharp.csproj`에 즉시 반영되지 않아 최초 빌드가 CS0246으로 실패 — 수동 1줄 패치로 재확인. Unity가 다음 애셋 리프레시에서 정확한 버전으로 덮어쓸 것.) |  |
| IMP-024 | 2026-08-14 | `UnityEngine.Random` 제거 확인 (grep) | `Assets/Scripts` 전체에서 `UnityEngine.Random`/`Random.Range` 0건. | |
| IMP-024 | 2026-08-14 | 런타임 Blocker (미해결) | `InteractableManager`가 `SampleScene.unity`에 없음(참조 0건) → provider 아이템 딕셔너리가 항상 null. `FarmerActionSelector._decisionTuning`이 `NPCDecisionTuning.asset`에 미연결. 계획에 따라 씬 배선은 이번 구현 범위에서 의도적으로 제외했고, 사용자가 Unity 에디터에서 직접 연결해야 한다. | |
| IMP-024 | 2026-08-14 | Codex review agent | 에이전트 런치 성공(비동기, PID 50108). 결과는 `PublicMD/Code_Evaluation_Result.md`에 기록됨(아직 미수신 — 수신된 리뷰 결과는 이 표에 기록하지 않음). |  |
| IMP-023 | 2026-08-11 | `DefaultStatContext` ScriptableObject 구현 및 `NPCManager`/`DataManager.GetStat()` 기본 stat 생성 경로 연결. `NPCStat`은 health/move speed/need 현재값과 max를 생성자에서 clamp해 runtime 인스턴스로 보관한다. | `dotnet build Assembly-CSharp.csproj --no-restore` 오류 0, 경고 0. Codex review agent launched asynchronously, run id `20260811-175130-135`. | REQ-NF-002, REQ-NF-005 |
| IMP-022 | 2026-08-02 | `DestinationDecider`/`NPCDecision`/`NPCIntent` 신설, `FarmerActionSelector` 재작성. 판단(거리·need 예측·작업 가능 횟수·점수)은 decider가, 큐 조립(Move 선행 + `ActionPool` 조회)은 selector가 전담하도록 경계를 확정. 탐욕적 한계이득 체인으로 "나온 김에" 보급을 몰아서 처리하는 판단 도입(순열 열거 대비 평가 6회로 축소). `ActionType.Drink`+`DrinkAction` 스텁 추가, `ActionPool` Awake/Create/ReturnAction 배선(ReturnAction switch는 딕셔너리 조회 1줄로 정리). `NPCStat` percentage 0 나눗셈 방어. `DestinationDB.DestinationInfo`에 `[System.Serializable]` 추가(직렬화 버그 수정). 런타임 `using NUnit.*` 2건 제거. | 아래 검증 행 참조. | 사용자 요청: DestinationDecider/FarmerActionSelector 책임 분리 |
| IMP-022 | 2026-08-02 | Command-line C# build | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. (Assembly-CSharp.csproj가 새 파일 2개를 아직 반영하지 못해 최초 빌드가 CS0246으로 실패 — Unity 백그라운드 재생성 대기 후 수동 2줄 패치로 재확인. Unity가 다음 애셋 리프레시에서 정확한 버전으로 덮어쓸 것.) |  |
| IMP-022 | 2026-08-02 | 경계 확인 (grep) | `FarmerActionSelector.cs`에 `Distance`/`Percentage`/`Threshold`/`Score`/`DestinationDB` 조회 0건. `DestinationDecider.cs`에 `IAction`/`Queue`/`ActionPool` 참조는 doc-comment 1건뿐, 실제 코드 참조 0건. |  |
| IMP-022 | 2026-08-02 | 판단 로직 데스크 체크 | 검증 시나리오 1~6을 수식에 손대입: (1) 저 need·근거리 작업장 → Work×8, (2) 목마름 80%·이동+작업 후 87% 예상 → DrinkThenWork(작업 2→9회 증가), (3) 급수지 근거리·목마름 30%·여유 충분 → Work만(한계이득 -10.6 < MinChainGain), (4) 피로 85% → SleepThenWork(danger penalty 324로 압도적 우위), (5)/(6) 체인 반복 시 재평가 지점(pos/state) 갱신 구조상 다중 보급·큰 작업 횟수 증가 케이스 모두 알고리즘적으로 성립 확인. 가정한 이동 거리는 시나리오 서술("가까움"/"멀음")에 맞춰 임의 배정. | 손계산 결과, 대화 로그에 기록 |  |
| IMP-022 | 2026-08-02 | Codex review agent | 에이전트 런치 성공(비동기, PID 46140). 결과는 `PublicMD/Code_Evaluation_Result.md`에 기록됨. |  |
| IMP-021 | 2026-07-22 | ScriptableObject asset instances reorganized under `Assets/Data`: worker action result tuning moved to `Assets/Data/Worker`, recruitment prototype candidate assets moved to `Assets/Data/Recruitment/Prototype`. Script definitions stayed in `Assets/Scripts`. | `.asset.meta` files moved with each asset; asset GUIDs preserved by file move. Static path check confirmed the assets exist in the new folders. | Project asset organization |
| IMP-020 | 2026-07-03 | PopupManager-compatible lazy popup cache implemented. `UIManager` now instantiates popup templates on demand, keeps closed popup instances inactive, preserves LIFO close behavior, and destroys least-recently-used inactive popups when the cache limit is exceeded. Added `PopupManager` subclass for scene naming compatibility and added owner injection to `IPopup`. | `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors. | UI popup infrastructure |
| IMP-019 | 2026-06-30 | 주민 모집 UI 뼈대 스크립트 구현: `IPopup`, `UIPopupType`, `UIManager`(LIFO 팝업 스택, ESC Input System 처리, ClosePopup 단방향 동기화), `RecruitmentPopup`(IPopup 구현, 도메인 API 4종만 사용, OnCloseButtonClicked→UIManager 경유), `RecruitmentCandidateSlot`(TMP/Image/Button 필드 뼈대, 콜백 위임). 씬/프리팹 배선 미수행. | `dotnet build` 오류 0, 경고 0. worker 내부 참조 grep 0건. 닫기 단방향 흐름 확인. Codex 리뷰 에이전트 런치(비동기). | REQ-F-042~047, Game_Plan 2.2/2.13 |
| IMP-018 | 2026-06-30 | Codex 평가 결과 적용: Guard 씬 엔트리 오배선 수정(SelectorType 2 → GuardActionSelector fileID 937587168), DestinationProvider null 배열 초기화, WorkerAIManager·MoveAction 정상 경로 Debug.Log 제거(설정 누락 LogWarning 유지), WorkerStats readonly 인스턴스 필드 → const 통합. | `dotnet build Assembly-CSharp.csproj --no-restore` 오류 0, 경고 0. Guard 엔트리 YAML grep 확인. Common 폴더 Debug.Log grep → 0건. Codex 리뷰 에이전트 런치(비동기). | Codex Finding 1, 5, 6, 8 |
| IMP-001 | 2026-06-17 | Moved Work/Eat/Drink/Rest duration and stat deltas into WorkerActionResultStatData and injected entries through WorkerActionSet. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | WorkerAI must remain execution-only; action result stats must be data-owned. |
| IMP-002 | 2026-06-17 | Updated WorkerAIManager to inject selector-side WorkerActionSet and connected the SampleScene manager to the Default selector template. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | WorkerAIManager must not require WorkerActionSet on WorkerAI prefabs. |
| IMP-004 | 2026-06-17 | Added wheat carry reward, warehouse destination, and DepositWheat action selected when carried wheat is full. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; serialized references verified by search. | Work must produce wheat reward; WorkerAI must remain execution-only; deposit must be a separate action. |
| IMP-005 | 2026-06-19 | Added the initial IAnim contract, animation context/type, MoveAnim hop loop, and WorkAnim squash loop with horizontal flip support. | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors. | DOTween animations must be isolated behind reusable animation implementations. |
| IMP-006 | 2026-06-19 | Added shared animation lookup, per-worker playback control, child visual-root ownership, and Move/Work action playback requests. | Command-line C# build and static prefab/reference checks passed. | WorkerAI must remain animation-agnostic; shared animation definitions must not share per-worker Tween state. |
| IMP-007 | 2026-06-20 | Added the initial CookActionSelector skeleton without coupling it to Farmer's WorkerActionSet. | Command-line C# build passed; selector contract implementation verified statically. | Cook requires a selector boundary before Cook-specific actions and action ownership are introduced. |
| IMP-008 | 2026-06-22 | Added warehouse inventory system: ItemType enum, IInventory contract, WarehouseInventory component, safe carry→warehouse transfer in DepositWheatAction, selector→action IInventory injection pattern. WorkerActionContext unchanged (Worker-only). | `dotnet build Assembly-CSharp.csproj --no-restore` succeeded with 0 warnings and 0 errors; scene wiring verified by YAML search (fileID 1903621705 consistent across WarehouseObject components, WarehouseInventory MonoBehaviour, and selector `_warehouse` reference). | REQ-F-022, REQ-F-023, REQ-NF-003. |
| IMP-009 | 2026-06-22 | Reorganized actor scripts into `AI/Friendly/Common`, `Cook`, and `Farmer`, with an `AI/Enemy` root reserved for hostile actors. | Command-line C# build passed with 0 warnings/errors; no duplicate GUIDs or stale old actor paths found; scene/prefab script GUID references remained unchanged. | Architecture responsibility and dependency placement; no gameplay requirement changed. |
| IMP-010 | 2026-06-22 | Added Guard role skeleton: `GuardActionSelector` (clean-start, no Farmer ActionSet coupling), `WallPerimeter` facility marker, `WorkerSelectorType.Guard` enum value. | `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors. Codex review skipped per user request (token limit). | Guard selector boundary must exist before Guard actions and decision logic are designed; WallPerimeter identifies the guard post location. |
| IMP-011 | 2026-06-23 | Unified action set; added `AttackAction` with `IDamageable`/`IAttackPower` seams. `WorkerActionSet._registeredActions` serialized array replaces hardcoded `Awake` pool list; Attack factory case, injection overload, and ResetAction extension added. `ActionType.Attack` added. | `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors. Codex review skipped per user request (token limit). | GuardActionSet would have duplicated ~120-line pooling core; unifying via serialized pool list eliminates drift risk while keeping role-specific action configuration in the inspector. |
| IMP-012 | 2026-06-23 | Critical 수정: 풀 등록을 직렬화 배열(`_registeredActions`) 대신 `_resultStatData` 엔트리에서 파생으로 전환. `IActionSet<TKey>` 죽은 인터페이스 삭제. `TryRentAction` 중복 공개 메서드를 private으로 통합. `WorkerActionResultStatData.ActionTypes` 접근자 추가. | `dotnet build Assembly-CSharp.csproj --no-restore` passed with 0 warnings and 0 errors. Codex review skipped per user request (token limit). | `_registeredActions` 배열 직렬화 누락으로 씬 실행 시 풀 0개 등록 → Farmer 생산 루프 전체 정지(Critical). 데이터 파생 등록으로 단일 진실원 확보; 씬/프리팹 수정 불필요. |
| IMP-017 | 2026-06-28 | 주민 모집 최소 골격: `ResidentCandidateKind`, `CandidateStatPreview`/`CandidateStatLine`, `IResidentCandidateView`, `ResidentCandidateDefinition`(SO), `IRecruitmentCostPolicy`, `AlwaysAffordableCostPolicy`, `IResidentSpawner`, `RecruitmentResult`, `RecruitmentManager`. 기존 파일 미수정. | `dotnet build Assembly-CSharp.csproj --no-restore` 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기). | REQ-F-042~047, REQ-D-013. |
| IMP-016 | 2026-06-27 | PatrolAction.Tick()에서 WorkerMover.TickMove() Failed 분기 수정: Success일 때만 idle 전환, Failed이면 Animation.Stop 후 ActionState.Failed 반환. | `dotnet build Assembly-CSharp.csproj --no-restore` 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기). | 이동 설정 오류가 순찰 성공으로 숨겨지지 않도록. |
| IMP-015 | 2026-06-27 | DefaultWorkerActionResultStatData.asset에 Attack(ActionType=7) 엔트리 추가: duration=1, statDelta={0,0,0}, wheatDelta=0. DepositWheat(6) 엔트리 다음에 삽입. C# 코드 변경 없음. | `dotnet build Assembly-CSharp.csproj --no-restore` 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기). | WorkerActionSet.TryCreateAction(Attack)이 TryGetResultStatEntry 성공 경로를 타도록. |
| IMP-014 | 2026-06-24 | Guard 순찰+욕구 처리 구현: `PatrolAction`(앵커 반경 내 무작위 이동·idle·매 tick 스캔, 적 발견 시 즉시 Success), `EnemyScanner`(Seek·Patrol 공용 Physics 스캐너로 추출), `PatrolParams`(주입 묶음 struct), `WorkerNeedsPolicy`(static 공용 욕구 임계 판정). `ActionType.Patrol` 추가. `SeekAction`을 `EnemyScanner`로 DRY 리팩토링. `WorkerActionSet`에 Patrol 풀·오버로드·ResetAction 확장. `FarmerActionSelector.TryGetNeededActionType`을 `WorkerNeedsPolicy` 위임으로 교체. `GuardActionSelector`를 전투>Critical>Prepare>순찰 우선순위로 확장, `_patrolAnchor`/`_patrolRadius`/`_patrolIdleMin`·`Max`/`_destinationProvider` 추가. | `dotnet build Assembly-CSharp.csproj` 오류 0 경고 0. Codex 리뷰 에이전트 실행 성공(실행 중). | REQ-F-038. |
| IMP-013 | 2026-06-23 | Guard 전투 기반 구현: `Health`(MonoBehaviour, IDamageable, 방어/회피/OnDied), `AttackPower`(IAttackPower), `Enemy`(테스트 표적), `SeekAction`(Physics2D ContactFilter 질의, CombatTargetHolder 주입), `CombatTargetHolder`(selector↔action 핸드오프). `IDamageable`에 `IsAlive` 추가. `ActionType.Seek` 추가. `WorkerActionSet`에 Seek 풀·주입 오버로드·ResetAction 확장. `GuardActionSelector` 핵심 로직 구현(Seek→Move→Attack 전환, attackPower 해석). `WorkerAI`에 `Health.OnDied` 구독·사망 처리 추가. | `dotnet build Assembly-CSharp.csproj` 오류 0 경고 0 확인. Codex 리뷰 에이전트 실행 실패(auto-mode 권한 차단). | REQ-F-003, REQ-F-027~033. |

## In Progress
| Task ID | Started | Current Step | Remaining Work |
|---|---|---|---|

## Files Changed
| Path | Change Summary | Reason |
|---|---|---|
| Assets/Scripts/Interface/ICombatStatView.cs | 신규. `AttackPower`/`AttackSpeed`/`AttackRange` read-only property. `IStatView` 비상속. | `AttackAction`이 health/needs를 안 쓰는데 상속하면 강제로 노출되므로 독립 인터페이스로 분리. |
| Assets/Scripts/Interface/IGuardStatView.cs | 신규. `ICombatStatView` 상속, `GuardRadius` 추가. | Guard 전용 patrol 반경을 combat 계약과 분리. |
| Assets/Scripts/System/Actor/GuardStat.cs | 신규. `NPCStat, IGuardStatView`. 4개 capability 필드는 생성 시 결정되는 immutable 값. | Guard 한 명당 stat 객체 하나로 combat/patrol capability까지 소유하기 위해(M-01/M-02 수정). |
| Assets/Scripts/System/Lib/CombatRange.cs | 신규. `IsInRange(from, to, range)` static utility. | 범위 판정을 stat에서 분리해 stat이 Unity 위치 판정을 소유하지 않게 하기 위해. |
| Assets/Data/ScriptableObject/Script/NPCStatDefinition.cs | 신규. `abstract NPCStat CreateRuntimeStat()`. | NPCType별 stat 생성 팩토리 계약(M-04 수정의 기반). |
| Assets/Data/ScriptableObject/Script/GuardStatDefinition.cs | 신규. `NPCStatDefinition` 구현, `GuardStat` 생성. | Guard 전용 능력치를 코드 변경 없이 asset에서 조정하기 위해. |
| Assets/Scripts/Interface/IStatView.cs | `GetAttackPower`/`GetAttackSpeed`(→`ICombatStatView`)와 호출부 0건인 `Current*Percentage` 3개 제거. | Farmer 등 비전투 role에 전투 필드를 강제하던 M-02 수정. |
| Assets/Scripts/System/Actor/NPCStat.cs | 공격 필드/ctor 파라미터 제거(10-인자 ctor로 복귀). 호출부 0건인 4-인자 ctor·버그 있는 `ChangeMoveSpeed`·`Current*Percentage` 제거. | M-02 수정 및 확인된 dead code 정리. |
| Assets/Data/Struct/ActionContext.cs | 호출부 0건인 `HasStat` 제거. `Stat` 타입은 `NPCStat` 그대로 유지. | dead code 정리(생성자 시그니처는 불변으로 유지해 9개 호출부 보존). |
| Assets/Data/ScriptableObject/Script/DefaultActionCost.cs | 호출부 0건인 `MovePerThirst/Hunger/Fatigue` 제거, 미사용 `using Unity.Properties;` 제거. | dead code 정리. |
| Assets/Data/ScriptableObject/Script/GuardActionCost.cs | `GuardRadius` 제거(→`GuardStat`). `AttackStoppingDistanceRatio`(0.9 기본값) tuning 신설. | M-01 수정, selector에 있던 매직넘버를 named tuning으로 승격(2차 계획 리뷰). |
| Assets/Scripts/System/Actor/BaseNPCActionSelector.cs | `CanUseStat(NPCStat)` virtual 계약 추가(기본 `stat != null`). | selector-stat 조합을 NPC 생성 시점에 검증할 수 있게(M-04 수정, 2차 계획 리뷰). |
| Assets/Scripts/System/Action/GuardAction.cs | `Start()`에서 `stat as IGuardStatView`를 1회 캐스트해 필드 캐싱, `Clear()`에서 null화. `GuardRadius`를 `cost` 대신 `guardStat`에서 읽음. | Tick마다 재캐스트하지 않기 위해(1차 계획 리뷰), GuardRadius stat 이전 반영. |
| Assets/Scripts/System/Action/AttackAction.cs | `AttackActionCost` 참조 전부 제거. `Start()`에서 `stat as ICombatStatView` 1회 캐스트, `CombatRange.IsInRange` 사용. | AttackActionCost 삭제에 대응, stat 캐스트 캐싱 패턴 통일. |
| Assets/Scripts/System/Actor/GuardActionSelector.cs | `_attackActionCostInfo` 제거. `CanUseStat` override. `RequestNewActionQueue` 진입부에서 `stat as IGuardStatView` 1회 캐스트 후 local parameter로 하위 메서드에 전달(필드 캐싱 금지). | selector는 여러 NPC가 공유하는 scene instance이므로 per-NPC 캐스트 결과를 필드에 저장하면 안 됨(2차 계획 리뷰). |
| Assets/Scripts/Manager/NPCManager.cs | `_selectors`(List\<BaseNPCActionSelector\>) → `_creationEntries`(List\<NPCCreationEntry\>, selector+stat definition 결합)로 재작성. `Awake()` 검증 추가. `CreateNPC()` 실패 경로 완비, `_workers` 등록 누락 버그 수정. | M-04 근본 수정(selector-stat 오조합을 생성 시점에 구조적으로 차단), 기존 버그 수정. |
| Assets/Scripts/Manager/DataManager.cs | `_defaultStatContext` 필드와 `GetStat()` 제거. | stat 생성이 `NPCManager`의 per-entry `NPCStatDefinition`으로 이동. |
| Assets/Scripts/Interface/IDataManager.cs | `GetStat()` 제거. | 유일 호출부였던 `NPCManager`가 대체됨. |
| Assets/Data/ScriptableObject/Script/DefaultStatContext.cs | 기반 타입을 `ScriptableObject`→`NPCStatDefinition`으로 변경(GUID/asset 인스턴스 보존). `_attackPower`/`_attackSpeed` 제거. `CreateStat()`을 `CreateRuntimeStat()` override로 교체. | NPCType keyed 생성 구조에 편입, M-02 수정. |
| Assets/Data/ScriptableObject/Script/AttackActionCost.cs, .cs.meta, Assets/Data/ScriptableObject/AttackActionCost.asset, .asset.meta | 삭제(4개 파일). | 유일한 실사용 필드(`AttackRange`)가 `GuardStat`으로 이동해 빈 껍데기만 남음. |
| Assets/Scenes/GuardTest.unity | `DataManager._costInfos`에서 `AttackActionCost`를 가리키던 entry 제거. | 자산 삭제 전 dangling reference로 인한 `DataManager.Awake()` NRE 방지(새 기능 배선이 아니라 크래시 방지 목적으로 예외적으로 직접 편집). |
| Assembly-CSharp.csproj | 신규 6개 `<Compile Include>` 추가, `AttackActionCost.cs` 항목 제거. | Unity 미실행 상태에서 `dotnet build`가 정확한 파일셋을 인식하도록. |
| Assets/Scripts/Enum/ActionResult.cs | 신규. `Running`/`Completed`/`ReplanRequested`/`Failed`. | 장기 action이 재계획과 실패를 구분해 결과를 보고하기 위해. |
| Assets/Scripts/Interface/IAction.cs | `CheckComplete()` 제거, `ActionResult Result { get; }` 추가. | WorkerNPC가 완료/재계획/실패를 구분해 queue를 처리하도록. |
| Assets/Scripts/System/Action/DefaultAction.cs | `Init()`에서 `Start()` 호출 제거. `Complete()`/`RequestReplan()`/`Fail(reason)` 종료 경로 분리. | selector가 queue를 구성하는 시점에 대기 action이 미리 시작되지 않도록, 실패 원인을 진단 가능하게. |
| Assets/Scripts/Actor/WorkerNPC.cs | queue lifecycle 단독 소유로 재작성(`AdvanceQueue`/`CancelAndReturnQueue`), `NPCType` 저장(Farmer 하드코딩 제거), `_isInitialized` 풀 재사용 가드. | Guard의 장기 action 재계획·전체 queue 취소 요구사항을 지원하고 pool 재사용 시 stale 상태를 막기 위해. |
| Assets/Scripts/Manager/NPCManager.cs | 실제 `NPCType`을 `WorkerNPC.Init`에 전달, selector 인덱스 사전 검증. | Farmer 하드코딩 제거에 대응, worker를 pool에서 꺼내기 전에 설정 오류를 잡기 위해. |
| Assets/Scripts/System/Actor/NPCComponent.cs | `GuardPerception` 참조와 per-NPC `GuardRuntimeState` 추가, `ResetRuntimeState()`(Phase A 빈 구현 → Phase C에서 타겟 clear로 채움). | Guard 감지·전투 상태의 연결점이자 pool 재사용 시 정리 지점을 제공하기 위해. |
| Assets/Scripts/System/Action/MoveAction.cs | `MoveRequest`(고정/동적) 지원, 하드코딩된 stopping distance 제거, 동적 타겟 소실 시 재계획. | Guard의 이동 대상(적)이 움직이는 상황을 전투 도메인 참조 없이 지원하기 위해. |
| Assets/Scripts/System/Action/EatAction.cs, DrinkAction.cs | `IsFinished` 마이그레이션, provider 부재 시 `Complete()`→`Fail(...)`로 수정(기존에는 base.Start() 실패 후에도 Complete()가 결과를 덮어쓸 수 있었음). | 새 ActionResult 계약 적용 및 잠재 버그 수정. |
| Assets/Scripts/System/Action/SleepAction.cs, IdleAction.cs | `IsFinished` 마이그레이션. | 새 ActionResult 계약 적용. |
| Assets/Scripts/System/Action/FarmingAction.cs | `IsFinished` 마이그레이션, 컴포넌트 유실 시 `Stop()`→`Fail(...)`(기존에는 영원히 멈춘 채 남았음), cost 누락 시 `Fail(...)`. | 새 ActionResult 계약 적용 및 정지 상태로 남는 버그 수정. |
| Assets/Scripts/System/Lib/ActionPool.cs | `GetAction`/`ReturnAction` null-safe, `Create` default case 진단, Guard/Attack case 추가. | 팩토리 실패 시 예외 대신 안전한 실패, 신규 action 타입 등록. |
| Assets/Scripts/System/Actor/FarmerActionSelector.cs | queue 조립을 대여 실패 시 전체 롤백하는 트랜잭션 방식으로 변경(결정 로직은 불변). | 부분 조립된 queue가 pool에서 누수되지 않도록. |
| Assets/Scripts/Enum/BuildingType.cs | `GuardPost`를 enum 끝에 추가. | 기존 직렬화 값 보존하며 Guard 경계 목적지 식별자 확보. |
| Assets/Data/ScriptableObject/Script/GuardActionCost.cs | 신규. 경계 반경/순찰 도착 거리/순찰점 개수, 초당 욕구 증가량, Guard 전용 중단 임계치(0..1). | 순찰·욕구 억제 수치를 코드 변경 없이 asset에서 조정하기 위해. |
| Assets/Scripts/System/Action/GuardAction.cs | 신규. 결정적 순찰점 순환, 매 Tick 직접 욕구 증가, GuardPerception 후보 감지 시 재계획. | Guard의 장기 경계 행동을 selector 호출이나 Physics 참조 없이 구현하기 위해. |
| Assets/Scripts/System/Actor/GuardActionSelector.cs | Farmer 복사본에서 전면 재작성. 전투→욕구→순찰 우선순위, 트랜잭션 queue 조립. | 복사된 Farmer 로직(FarmingActionCost 의존 등)을 제거하고 Guard 고유 의사결정을 구현하기 위해. |
| Assets/Scripts/Interface/ICombatTarget.cs, IMoveTarget.cs | 신규. 최소 전투/이동 대상 계약. | Guard 전투와 MoveAction이 서로 다른 최소 계약만 의존하도록 분리. |
| Assets/Scripts/System/Actor/CombatTargetHandle.cs | 신규. `IMoveTarget` 구현 plain C# handle, Owner 기반 유효성 판정. | Unity 파괴 객체의 interface 참조 null 판정 문제를 해결하기 위해. |
| Assets/Scripts/System/Actor/ProximitySensor2D.cs | 신규. LayerMask 기반 범용 Trigger2D 감지기. | Guard/Enemy 도메인과 무관한 재사용 가능한 감지 계층을 확보하기 위해. |
| Assets/Scripts/System/Actor/GuardPerception.cs | 신규. sensor collider를 살아있는 ICombatTarget 후보로 변환·중복 제거하는 Guard adapter. | 복수 collider를 가진 적을 하나의 후보로 취급하고 sensor를 domain-agnostic하게 유지하기 위해. |
| Assets/Scripts/System/Actor/GuardRuntimeState.cs | 신규. NPC별 `CombatTargetHandle` 소유. | 선택된 타겟이 perception 범위 이탈과 무관하게 유지되도록(사거리 이탈 시 타겟 유지 요구사항). |
| Assets/Data/Struct/MoveRequest.cs | 신규. 고정 `Vector3` 또는 `IMoveTarget` + stopping distance. | MoveAction이 전투 타입을 직접 참조하지 않고 동적 목적지를 지원하도록. |
| Assets/Data/Struct/ActionContext.cs | `MoveRequest? moveRequest`를 생성자 맨 끝에 추가. | 기존 positional 호출부를 보존하면서 동적 이동 요청을 전달하기 위해. |
| Assets/Scripts/Actor/Enemy.cs | 신규. 최소 `ICombatTarget` 테스트 표적. | 사용자가 Play Mode에서 Guard 전투를 검증할 표적을 제공하기 위해. |
| Assets/Scripts/System/Actor/NPCStat.cs | 공격력·공격 속도 필드와 `GetAttackPower`/`GetAttackSpeed` 추가(생성자 끝에 default 파라미터로 append). | AttackAction이 공격 주기·피해량을 계산하기 위해. |
| Assets/Scripts/Interface/IStatView.cs | `GetAttackPower`/`GetAttackSpeed` 추가. | read-only stat 소비자가 공격 수치를 조회할 수 있도록. |
| Assets/Data/ScriptableObject/Script/DefaultStatContext.cs | 공격력·공격 속도 직렬화 필드 추가, `CreateStat()` 호출부 갱신. | 공격 수치를 코드 변경 없이 asset에서 조정하기 위해. |
| Assets/Data/ScriptableObject/Script/AttackActionCost.cs | 신규. 공격 반경. | AttackAction의 사거리 판정 수치를 asset에서 조정하기 위해. |
| Assets/Scripts/System/Action/AttackAction.cs | 신규. 장기 반복 타격, 타격 직후 사망 재검사, hit 상한 기반 timer clamp. | 하나의 action이 타겟 사망까지 공격을 관리하고 프레임 폭주를 방지하기 위해. |
| Assembly-CSharp.csproj | Phase A~D 신규 `.cs` 파일 `<Compile Include>` 수동 추가. | Unity 미실행 상태에서 `dotnet build`가 신규 파일을 인식하도록(Unity가 다음 리프레시에서 정확한 버전으로 덮어씀). |
| PublicMD/ProjectStructure.md | "Guard Combat Vertical Slice" 섹션 추가. | Guard 관련 신규 파일과 책임 배치를 문서화하기 위해. |
| Assets/Data/ScriptableObject/Script/DefaultStatContext.cs | 빈 ScriptableObject를 NPC 기본 stat 정의 에셋으로 구현. `CreateStat()`로 새 `NPCStat` runtime 인스턴스를 생성. | 기본 NPC stat 수치를 코드 변경 없이 Inspector 에셋에서 조정하기 위해. |
| Assets/Scripts/System/Actor/NPCStat.cs | need 현재/최대값까지 받는 생성자 overload 추가 및 health/move speed/need clamp 적용. 기존 4인자 생성자는 유지. | `DefaultStatContext`에서 현재 `NPCStat`이 가진 모든 stat 정보를 안전하게 주입하기 위해. |
| Assets/Scripts/Manager/NPCManager.cs | `_defaultStatContext` serialized reference 추가. 연결되어 있으면 에셋 기반 stat 생성, 없으면 기존 fallback 유지. | NPC 생성 시 하드코딩된 기본 stat 대신 ScriptableObject 기본값을 사용할 수 있게 하기 위해. |
| Assets/Scripts/Manager/DataManager.cs | `GetStat()`을 `_statInfo.CreateStat()` 기반으로 구현하고 fallback stat을 유지. | 기존 `IDataManager`/`DefaultStatContext` 연결 의도를 완성하고 빌드 오류를 제거하기 위해. |
| Assets/Scripts/Recruitment/ResidentCandidateKind.cs | 신규. 일반/네임드 주민 구분 enum. | 도메인 enum은 도메인 폴더에 배치(CodeConvention). |
| Assets/Scripts/Recruitment/CandidateStatPreview.cs | 신규. `CandidateStatLine`(label+value) + `CandidateStatPreview`(IReadOnlyList 노출). | 스탯 표시 데이터는 balance 수치 없이 디자이너 정의 쌍으로만 구성. |
| Assets/Scripts/Recruitment/IResidentCandidateView.cs | 신규. 후보 읽기 전용 뷰 계약(DisplayName, Kind, StatPreview, RecruitCost, Portrait). | UI는 이 인터페이스만 의존; 뮤테이션 경로 없음. |
| Assets/Scripts/Systems/Recruitment/ResidentCandidateDefinition.cs | 신규. `ScriptableObject, IResidentCandidateView`. `[CreateAssetMenu("Settlement/Resident Candidate")]`. | 인스펙터 편집 가능 후보 에셋; 기존 WorkerActionResultStatData 패턴 준수. |
| Assets/Scripts/Recruitment/IRecruitmentCostPolicy.cs | 신규. 골드/지갑 seam: `CanAfford(cost)`, `TryPay(cost)`. | 실제 경제 시스템 연결 전 교체 가능한 경계. |
| Assets/Scripts/Recruitment/AlwaysAffordableCostPolicy.cs | 신규. 임시 항상-통과 정책. | 골드 시스템 구현 전 컴파일·런타임 안정성 확보. |
| Assets/Scripts/Recruitment/IResidentSpawner.cs | 신규. 주민 생성 seam: `TrySpawnResident(candidate)`. | candidate→WorkerAI 매핑은 비결정 구간; TODO로 경계만 확보. |
| Assets/Scripts/Recruitment/RecruitmentResult.cs | 신규. 모집 명령 결과 enum(Success, InvalidCandidate, CannotAfford, SpawnFailed). | TryRecruit 실패 이유 구분; 나중에 UI 피드백에 사용. |
| Assets/Scripts/Recruitment/RecruitmentManager.cs | 신규. MonoBehaviour. `Candidates`(IReadOnlyList<IResidentCandidateView>), `CanRecruit`, `TryRecruit`, `Init`(주입 경로), `[ContextMenu("Log Candidates")]`. | 모집 결정·비용 검증·생성 위임을 한 곳에 소유; WorkerAI와 완전 분리. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatData.cs | Added ScriptableObject result stat database. | Own action cost/reward data outside WorkerAI and action logic. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatEntry.cs | Added serializable action result entry type. | Expose action duration and stat delta as shared data. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerStatDelta.cs | Added serializable stat delta type. | Let actions apply stat changes without knowing action-specific values. |
| Assets/Data/Worker/DefaultWorkerActionResultStatData.asset | Added default Work/Eat/Drink/Rest durations and stat deltas matching previous behavior; IMP-015에서 Attack(7) 엔트리(duration=1, statDelta=0, wheatDelta=0) 추가. | Preserve existing gameplay values while making them configurable; WorkerActionSet가 AttackAction 생성 시 TryGetResultStatEntry 성공 경로 확보. |
| Assets/Data/Recruitment/Prototype/*.asset | Moved prototype recruitment candidate ScriptableObject assets out of `Assets/Scripts`. | Keep editable data instances in the shared data area while leaving script definitions under the owning system folder. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Added result stat data reference, lookup API, and data-backed action creation. | Centralize action construction and data injection. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/*.cs | Replaced hardcoded durations and stat values with injected result stat entries. | Keep actions responsible for execution flow only. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs | Added timer-based action that deposits all carried wheat on completion. | Keep warehouse deposit behavior in an IAction implementation. |
| Assets/Scripts/Enum/ItemType.cs | Added ItemType enum (Wheat). | Identify item types without IItem interface overhead; same pattern as ActionType. |
| Assets/Scripts/Interface/IInventory.cs | Added IInventory contract (TryAdd/TryRemove/GetQuantity/TotalCount/Capacity). | Define a single inventory role contract shared by facility components and actions. |
| Assets/Scripts/Facility/WarehouseInventory.cs | Added WarehouseInventory MonoBehaviour implementing IInventory with total-capacity and per-ItemType quantity tracking. | Provide runtime inventory ownership for the warehouse facility as a composable component. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerCarryStorage.cs | Replaced DepositAllWheat with RemoveWheat(int); partial removal now supported. | Enable safe partial transfer where only the accepted quantity is removed from carry. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/DepositWheatAction.cs | Rewrote to use injected IInventory (SetTargetInventory/ClearTargetInventory); transfer removes only accepted quantity; returns Failed if accepted==0. | Fix resource loss bug; align with selector→action injection pattern identical to MoveAction destination. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Added TryGetAction(ActionType, IInventory, out) and TryRentAction(ActionType, IInventory, out) overloads; ResetAction clears DepositWheatAction inventory. | Mirror MoveAction destination injection pattern for DepositWheatAction. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Added _warehouse serialized field; WarehouseHasSpace() guard; TryCreateDepositPlan() that injects IInventory into rented DepositWheatAction. | Keep IInventory out of WorkerActionContext; selector is the decision layer that owns external facility references. |
| Assets/Scenes/SampleScene.unity | Added WarehouseInventory component (fileID 1903621705) to WarehouseObject; wired _warehouse on selector template. | Connect scene facility to the selector so IInventory can be injected at plan time. |
| Assembly-CSharp.csproj | Added ItemType.cs, IInventory.cs, WarehouseInventory.cs compile entries. | Make new scripts visible to dotnet build before Unity regeneration. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs | Added generic stat delta application and removed action-specific stat mutation methods. | Keep WorkerStats focused on stat ownership and clamping. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerCarryStorage.cs | Added carried wheat state, capacity clamp, add, and deposit operations. | Separate carried item state from hunger/thirst/fatigue stats. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerActionContext.cs | Exposes WorkerCarryStorage to actions and selectors. | Let actions mutate carried wheat without WorkerAI knowing the reward system. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Reads Work result data through WorkerActionSet before creating Work plans. | Let selector depend on shared data instead of action implementation. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerDefaultActionSelector.cs | Selects DepositWheat after critical needs and before prepare-threshold needs when carried wheat is full. | Prevent endless work while still honoring critical survival needs. |
| Assets/Scenes/SampleScene.unity | Connected the default result stat data asset to the existing WorkerActionSet. | Ensure scene action creation can resolve stat entries. |
| Assets/Scenes/SampleScene.unity | Added WarehousePoint/WarehouseObject and mapped ActionType.DepositWheat in DestinationProvider. | Give DepositWheat a scene destination. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Removed worker-prefab WorkerActionSet lookup and resolved WorkerActionSet from selector instances. | Keep WorkerActionSet owned by selector/action construction, not WorkerAI. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Added initial carry storage configuration for spawned workers. | Configure carried wheat capacity without WorkerAI owning item state. |
| Assets/Scenes/SampleScene.unity | Connected WorkerAIManager to the Default selector template. | Allow selector creation after WorkerActionSet resolution. |
| Assets/Scripts/Enum/ActionType.cs | Added DepositWheat action type. | Allow action set and destination provider to identify the deposit behavior. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatEntry.cs | Added wheat delta to action result data entries. | Keep work reward values in data instead of action code. |
| Assets/Scripts/Animation/AnimType.cs | Added Move and Work animation identifiers. | Support enum-keyed animation lookup without coupling to action implementations. |
| Assets/Scripts/Animation/AnimContext.cs | Added visual Transform and FlipX execution parameters. | Give animations only the runtime data required to create their tweens. |
| Assets/Scripts/Animation/IAnim.cs | Added the shared DOTween animation creation contract. | Allow animation implementations to be registered and invoked through one role. |
| Assets/Scripts/Animation/Anims/MoveAnim.cs | Added a looping local hop with stretch, squash, cleanup, and FlipX preservation. | Provide movement feedback without modifying gameplay movement logic. |
| Assets/Scripts/Animation/Anims/WorkAnim.cs | Added a looping squash pulse with cleanup and FlipX preservation. | Provide reusable visual feedback for work actions. |
| Assets/Scripts/Animation/IAnimSet.cs | Added the animation lookup contract. | Let playback depend on a shared registry role. |
| Assets/Scripts/Animation/AnimSet.cs | Registered one reusable Move and Work animation definition. | Share stateless definitions across all workers without pooling. |
| Assets/Scripts/Animation/IAnimPlayer.cs | Added per-actor playback, facing, and stop operations. | Expose animation capability to actions without exposing DOTween lifecycle details. |
| Assets/Scripts/Animation/ActorAnimationController.cs | Added per-worker active Tween and facing ownership. | Prevent workers from sharing mutable playback state. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerActionContext.cs | Exposed `IAnimPlayer` to actions. | Keep WorkerAI unaware of concrete animation lookup and playback. |
| Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs | Starts Move animation with destination-derived facing and stops it with action lifecycle. | Keep movement animation owned by movement behavior. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/Actions/WorkAction.cs | Starts and stops Work animation with action lifecycle. | Preserve current facing while showing work feedback. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerAIManager.cs | Creates one shared AnimSet and one ActorAnimationController per spawned worker. | Establish correct shared-definition and per-actor-state ownership. |
| Assets/Prefab/FarmerAI.prefab | Moved SpriteRenderer to a child `VisualRoot`. | Isolate DOTween local visual changes from gameplay-root movement. |
| PublicMD/ProjectStructure.md | Documented animation roles, ownership, paths, and dependency flow. | Keep architecture guidance aligned with implementation. |
| PublicMD/CodeConvention.md | Added animation boundaries and corrected Farmer-domain paths. | Prevent animation lifecycle from drifting into WorkerAI or shared definitions. |
| Assets/Scripts/Actors/AI/Friendly/Cook/CookActionSelector.cs | Added a no-plan Cook selector implementing the existing worker selector contract. | Establish the Cook decision boundary without inventing Cook actions or reusing Farmer action ownership. |
| PublicMD/ProjectStructure.md | Documented the Cook folder and initial selector responsibility. | Keep actor-domain structure aligned with implementation. |
| Assets/Scripts/Actors/AI/Friendly/Common/** | Moved reusable AI lifecycle, plan, movement/recovery actions, context capabilities, and shared action-result data while preserving `.meta` files. | Prevent reusable friendly AI code from being owned by the Farmer role. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/** | Kept Farmer composition, production selector/action set, combat selector stub, and Work/Deposit actions under the Farmer role. | Keep job-specific production ownership out of Common. |
| Assets/Scripts/Actors/AI/Friendly/Cook/** | Moved the Cook selector into the friendly Cook role. | Align Cook with the Friendly actor hierarchy. |
| Assets/Scripts/Actors/AI/Enemy/ | Added the hostile AI root. | Separate future enemy behavior from friendly actor code. |
| Assembly-CSharp.csproj | Updated moved script compile paths. | Preserve command-line build verification before Unity regenerates the project file. |
| PublicMD/ProjectStructure.md, PublicMD/CodeConvention.md | Updated folder ownership and placement rules. | Keep future implementation aligned with the new hierarchy. |
| Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs | Added Guard role skeleton selector with `WallPerimeter` dependency and no-plan TrySelectAction body. | Establish Guard decision boundary before Guard actions are designed; mirrors Cook clean-start pattern. |
| Assets/Scripts/Facility/WallPerimeter.cs | Added minimal MonoBehaviour marker identifying the guard post work location via optional `_workPoint` or self transform. | Provide the facility reference point for the Guard selector without introducing facility logic prematurely. |
| Assets/Scripts/Actors/AI/Friendly/Common/WorkerSelectorType.cs | Added `Guard` enum value. | Enable `WorkerAIManager` selector entry wiring for the Guard role. |
| Assembly-CSharp.csproj | Added compile entries for `WallPerimeter.cs` and `GuardActionSelector.cs`. | Ensure `dotnet build` can verify Guard scripts before Unity regeneration. |
| Assets/Scripts/Enum/ActionType.cs | Added `Attack` enum value. | Allow action set and pool to identify the attack behavior. |
| Assets/Scripts/Interface/IDamageable.cs | Added `TakeDamage(int amount)` contract. | Provide the minimum attack-target seam for `AttackAction` without coupling to a concrete health/enemy system. |
| Assets/Scripts/Interface/IAttackPower.cs | Added `GetAttackPower()` contract. | Separate attack-power calculation (Stat + equipment) from attack execution; concrete implementation supplied by user later. |
| Assets/Scripts/Actors/AI/Friendly/Common/Actions/AttackAction.cs | Added timer-based attack action implementing `IAction`; injects `IDamageable` target and `IAttackPower`; null-guards both in Start; applies damage and StatDelta on completion. | Keeps attack execution logic in an `IAction` implementation; external seams allow Stat+equipment calculation and enemy health to be added independently. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Replaced hardcoded `Awake` pool list with `[SerializeField] ActionType[] _registeredActions` loop; added Attack factory case; added `TryGetAction`/`TryRentAction` overloads for `IDamageable`+`IAttackPower`; extended `ResetAction` to clear Attack injections. | Unify Farmer and Guard action pools in one component; role-specific pool is now an inspector configuration instead of a subclass. |
| Assets/Scripts/Actors/AI/Friendly/Common/Data/WorkerActionResultStatData.cs | Added `ActionTypes` IEnumerable<ActionType> property. | 데이터 파생 등록을 위해 외부에서 엔트리 타입 목록을 순회할 수 있도록 최소 접근자 추가. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | `_registeredActions` 제거, `Awake` 데이터 파생 등록으로 재작성, `RegisterPool` idempotent 처리, `TryRentAction` 3종 private 전환, `IActionSet<ActionType>` 구현 선언 제거. | Critical 수정: 씬/프리팹 수정 없이 Farmer 풀 자동 복구; 중복 공개 API·단일 진실원 확보. |
| Assets/Scripts/Interface/IActionSet.cs | 삭제. | 타입으로 소비되지 않는 명목 인터페이스 — 불필요 코드 제거(원칙 1). |
| Assets/Scripts/Interface/IDamageable.cs | `bool IsAlive { get; }` 추가(additive). | Physics 질의 후 살아있는 대상만 선택하기 위한 최소 계약 확장. |
| Assets/Scripts/Enum/ActionType.cs | `Seek` 추가. | SeekAction 풀 등록·식별에 필요. |
| Assets/Scripts/Combat/Health.cs | 신규. `Health : MonoBehaviour, IDamageable`. HP/MaxHP·방어·회피 직렬화; `TakeDamage`(회피→0, `max(1,raw-def)` 적용); `Die()`→`OnDied` 이벤트; 중복 사망 방지; `Init` 주입 경로. | Worker·적·건물·작물 공용 IDamageable 진입점. |
| Assets/Scripts/Combat/AttackPower.cs | 신규. `AttackPower : MonoBehaviour, IAttackPower`. 직렬화 `_attackPower`, `GetAttackPower()`. | 가드·적 공용 공격력 제공. |
| Assets/Scripts/Actors/AI/Enemy/Enemy.cs | 신규. 테스트 표적 식별 MonoBehaviour. `[RequireComponent(typeof(Health))]`. | 이동/반격 AI 없는 정적 테스트 적. |
| Assets/Scripts/Actors/AI/Friendly/Common/Actions/SeekAction.cs | 신규. `IAction`. `Physics2D.OverlapCircle`(ContactFilter2D) 스캔, 최근접 살아있는 IDamageable을 holder에 기록 후 `Success`. | 공격 명령 없는 순수 탐지 액션. |
| Assets/Scripts/Actors/AI/Friendly/Common/Combat/CombatTargetHolder.cs | 신규. plain C# 핸드오프 홀더. `Target`, `TargetTransform`, `HasLiveTarget`, `SetTarget`, `Clear`. | 외부(적) 정보가 WorkerActionContext로 새지 않도록 selector↔action 경계에서 전달. |
| Assets/Scripts/Actors/AI/Friendly/Farmer/WorkerActionSet.cs | Seek 풀 명시 등록, Seek 주입 오버로드, TryCreateAction Seek 케이스, ResetAction Seek 정리 추가. | GuardActionSelector가 Seek 렌트/반환 가능하도록 공용 풀 확장. |
| Assets/Scripts/Actors/AI/Friendly/Guard/GuardActionSelector.cs | 핵심 결정 로직 구현: `IWorkerActionSelectorSetup` 추가, `CombatTargetHolder` 소유, `_attackPower` 해석(`GetComponentInParent`), `TrySelectAction`(HasLiveTarget→Attack/Move+Attack, 없으면 Seek). | Guard가 Seek→이동→공격 자연 전환하도록. |
| Assets/Scripts/Actors/AI/Friendly/Common/WorkerAI.cs | `_health` 필드, `Init` 시 `TryGetComponent`+`OnDied` 구독, `OnDisable` 해제, `OnWorkerDied`(plan Cancel→enabled=false) 추가. | WorkerAI는 IDamageable 미구현, Health에 위임; 사망 시 plan 정리·AI 정지. |
| Assembly-CSharp.csproj | `Health.cs`, `AttackPower.cs`, `Enemy.cs` 컴파일 항목 추가. | 신규 폴더 파일이 dotnet build에 포함되도록(Unity 재임포트 전). |
| Assets/Scenes/SampleScene.unity | Guard 셀렉터 엔트리 `_selectorSource` fileID 교체: 568873992(FarmerActionSelector) → 937587168(GuardActionSelector). | Codex Finding 1: Guard 선택 시 Farmer 행동이 실행되는 오배선 수정. |
| Assets/Scripts/Provider/DestinationProvider.cs | `_destinationInfos` 필드 `= Array.Empty<DestinationInfo>()` 초기화 추가. | Codex Finding 5: 미설정 Provider의 Try... 메서드 NPE → 정상 false 반환. |
| Assets/Scripts/Actors/AI/Friendly/Common/WorkerAIManager.cs | 정상 경로 `Debug.Log` 5개 제거(스폰 흐름 스캐폴딩 로그). `Debug.Log("No WorkerPrefab")` → `Debug.LogWarning`, `Debug.Log("Worker Init Fail")` → `Debug.LogWarning` 상향. | Codex Finding 6: 다중 스폰 시 콘솔 노이즈 제거, 설정 누락 경고는 유지. |
| Assets/Scripts/Actors/AI/Friendly/Common/Actions/MoveAction.cs | `Debug.Log("Worker Moving Start")` 제거. | Codex Finding 6: 이동 시작 정상 경로 로그 제거. |
| Assets/Scripts/Actors/AI/Friendly/Common/Context/WorkerStats.cs | `private readonly float MIN_*_VAL / MAX_*_VAL` 6개 → `private const float MinStatVal = 0f; MaxStatVal = 100f;` 2개로 통합. setter Mathf.Clamp 참조 갱신. | Codex Finding 8: 인스턴스 readonly 필드 → const 통합(Field Style 규칙). |
| Assets/Scripts/UI/IPopup.cs | 신규. `namespace UI`. 팝업 공통 계약: `IsOpen`, `Open()`, `Close()`. | 모든 팝업이 UIManager를 통해 라우팅되도록 단일 계약 확보. |
| Assets/Scripts/UI/UIPopupType.cs | 신규. `namespace UI`. `Recruitment` 값 포함 팝업 타입 enum. | UIManager Inspector 배선과 코드 라우팅에 사용. |
| Assets/Scripts/UI/UIManager.cs | 신규. `namespace UI`. LIFO 팝업 스택, `OpenPopup`, `CloseTopPopup`, `ClosePopup`, ESC 처리(Input System), `UIPopupEntry` 직렬화 엔트리 포함. | 씬 단위 UI 라우터; 도메인 로직 미소유. |
| Assets/Scripts/Systems/Recruitment/UI/RecruitmentPopup.cs | 신규. `namespace Recruitment`. `IPopup` 구현. `RecruitmentManager` 참조, 슬롯 instantiate/rebuild, `OnCloseButtonClicked`→UIManager.ClosePopup 경유, `Init` 코드 주입 경로. | 모집 UI 진입점; 도메인 API 4종만 사용. |
| Assets/Scripts/Systems/Recruitment/UI/RecruitmentCandidateSlot.cs | 신규. `namespace Recruitment`. `TMP_Text`(name/kind/cost) + `Image`(portrait) + `Button` + `Transform`(stat placeholder) 뼈대. `Bind(candidate, callback)`, 버튼 리스너 코드 연결, OnDestroy 리스너 정리. | 후보 카드 뼈대; WorkerAI/selector 미접근. |
| Assembly-CSharp.csproj | 위 5개 .cs `<Compile Include>` 엔트리 추가. | Unity 재임포트 전 `dotnet build` 인식. |

## Implementation Notes
IMP-019: UI 인프라는 `namespace UI`(Assets/Scripts/UI)에, 모집 전용 UI는 `namespace Recruitment`(Assets/Scripts/Systems/Recruitment/UI)에 배치해 기존 Recruitment 네임스페이스 규약 유지. `UIManager`의 ESC 처리는 `activeInputHandler: 1`(Input System 전용) 환경에서 레거시 `Input.GetKeyDown` 대신 `Keyboard.current.escapeKey.wasPressedThisFrame` 사용. `UIPopupEntry` 직렬화 엔트리 패턴은 `WorkerSelectorEntry`(WorkerAIManager.cs:190) 패턴을 그대로 차용. non-top 팝업 닫기는 스택 재구성으로 처리하며 LogWarning을 남긴다(정책상 비정상 경로). 슬롯 오브젝트 풀링은 스켈레톤 단계에서 미적용(rebuild 방식). `_contentRoot` 미지정 시 `gameObject` 폴백으로 팝업 컴포넌트 자체를 활성/비활성한다. `_portraitImage.enabled = candidate.Portrait;`는 UnityEngine.Object implicit bool 변환 활용.

IMP-017: `Assets/Scripts/Recruitment/` 신규 도메인 폴더로 분리해 기존 worker 행동 시스템에 의존하지 않는다. `ResidentCandidateDefinition`은 `ScriptableObject`이므로 Unity truthiness(`if (def)`)로 null 체크한다. 평 C# 인터페이스(`IRecruitmentCostPolicy`, `IResidentSpawner`)는 `== null`/`is null`로 체크한다. `_candidates`는 `List<ResidentCandidateDefinition>`로 관리하며 `IReadOnlyList<IResidentCandidateView>`로 노출된다(`IReadOnlyList<out T>` 공변성 활용). `TryRecruit`에서 `TryPay` 성공 후 spawn 실패 시 골드가 차감된 상태로 남는 한계를 코드에 TODO로 명시했다 — 실제 지갑 구현 시 `Refund` 경로가 필요하다. `IResidentSpawner` 어댑터(WorkerAIManager 연결)는 candidate→WorkerInitialStats 매핑이 미결정이므로 TODO로 경계만 확보했다. `[ContextMenu("Log Candidates")]`로 인스펙터에서 후보 목록을 즉시 확인할 수 있다. AD-012(후보 갱신, 비용 공식, 네임드 중복)는 미결정이므로 이 슬라이스에서 구현하지 않았다.

IMP-016: `PatrolAction.Tick()`의 moveState 분기를 3-way로 변경. `Running`→Running 반환(불변), `Success`→Animation.Stop+idle 전환(불변), `Failed`→Animation.Stop+Failed 반환(신규). 기존 `// 이동 완료 또는 실패 시 idle 단계로 전환` 주석이 오해를 유발했으므로 `// 이동 완료(Success) 시에만 idle 단계로 전환`으로 수정. Cancel·EnemyScanner·idle 로직은 변경 없음.

IMP-015: C# 코드 변경 없이 asset YAML만 수정. `_actionType: 7` 엔트리를 DepositWheat(6) 다음에 추가해 `WorkerActionSet.TryCreateAction(ActionType.Attack)`이 `TryGetResultStatEntry` 성공 경로를 타도록 함. duration은 튜닝 전용 초기값 1초; 전투 데미지·효과는 AttackAction 주입 seam(`IDamageable`, `IAttackPower`)을 통해 별도 조정.

IMP-018: Finding 1 씬 YAML 수정은 `_initialSelectorType: 0`(Default) 상태에서 진행해 런타임 영향 없이 안전. Finding 2(Guard 템플릿 완성)는 Enemy 레이어·프리팹 컴포넌트 부착이 필요한 Unity Editor 작업이라 코드로 처리 불가, 수동 배선 패스로 보류. Finding 3(모집 트랜잭션)은 `AlwaysAffordableCostPolicy`가 no-op이므로 현재 실제 버그 없음, 지갑 구현 연계 시점에 reserve/commit/rollback 설계. Finding 4(EnemyScanner GetComponent)는 10-collider 상한·프로토타입 적 수 소수·Guard 비활성 조건에서 조숙한 최적화, Patrol throttle 과제로 보류. Finding 7(stale enum)은 `ActionType.Sleep` index 3 제거 시 `DefaultWorkerActionResultStatData.asset` raw int 직렬화 회귀 발생 위험, 직렬화 의존 감사 후 처리.

IMP-001: `WorkerAI` was intentionally left unchanged and does not reference `WorkerActionResultStatData`.

IMP-002: CSV/provider abstraction was not added yet; `WorkerActionResultStatData` is the current single source of truth and can be replaced later behind the same lookup role if needed.

IMP-003: `WorkerActionSet` remains selector-side. `WorkerAI` and worker prefabs should not own action pools.

IMP-004: `WorkerAI` remains unchanged for wheat and warehouse behavior. The selector only decides that full carried wheat should trigger `DepositWheat`; `WorkAction` and `DepositWheatAction` perform the state changes through `WorkerCarryStorage`.

IMP-005: `IAnim` implementations create and return DOTween tweens but do not own the active tween lifecycle. `AnimContext.Transform` must be a child visual Transform so MoveAnim local-position changes do not compete with WorkerMover on the actor root. FlipX is applied by preserving scale magnitude and changing only the local X scale sign.

IMP-006: `AnimSet` is shared and stores only stateless definitions. `ActorAnimationController` is created per worker and exclusively owns the mutable active Tween and facing. `MoveAction` and `WorkAction` request animation through `WorkerActionContext.Animation`; `WorkerAI` remains unchanged and animation-agnostic.

IMP-007: `CookActionSelector` intentionally does not implement `IWorkerActionSelectorSetup` because that setup contract injects Farmer's `WorkerActionSet`. It will remain a no-plan selector until Cook-specific actions and their ownership model are defined.

IMP-008: `WorkerActionContext` was intentionally left unchanged. `IInventory` is not a Worker capability — it is an external facility reference. The selector→action injection pattern (identical to `MoveAction` destination) was used instead. `WarehouseInventory` holds a total-capacity limit (not per-ItemType) as the initial choice (user-confirmed). Carry+warehouse both full results in `DepositWheatAction.Failed` with no resource loss; idle/wait policy for that state is out of scope for this slice. `Assembly-CSharp.csproj` was manually updated with new files because Unity has not yet reimported them; Unity regeneration will produce the authoritative csproj. Codex review agent launch was blocked by auto-mode sandbox; user must invoke it manually if desired.

IMP-009: Common contains reusable friendly-AI execution and capabilities, not global game services. Farmer retains `WorkerActionSet`, `WorkerAIManager`, Farmer selectors, `WorkAction`, and `DepositWheatAction` because those currently compose or execute Farmer production. `WorkerCombatActionSelector` remains under Farmer until its action-set dependency is generalized. Moving files preserved their existing `.meta` GUIDs, so serialized script references do not require rewiring.

IMP-010: `GuardActionSelector` intentionally does not implement `IWorkerActionSelectorSetup` because Guard does not yet have its own action set. This mirrors the Cook boundary decision. `WallPerimeter` is a minimal marker; all guard-post logic will be added in later slices when the Guard decision policy is defined. Codex review agent was not launched per user request (token limit).

IMP-013: `Health`는 `MonoBehaviour, IDamageable`로 Worker·적·건물·작물 어디에나 부착 가능하다. `WorkerAI`는 IDamageable을 구현하지 않고 `Health.OnDied`만 구독해 plan을 정리하고 `enabled=false`로 AI 틱을 중지한다. `OnDisable`에서 구독을 해제하므로 씬 언로드·disable 시 이중 처리가 없다. `SeekAction`은 `Physics2D.OverlapCircle(ContactFilter2D)`(Unity 6 non-deprecated)를 사용해 Enemy 레이어를 스캔하며, 전역 적 리스트·매니저 없이 엔진이 공간을 관리한다. `CombatTargetHolder`는 `GuardActionSelector`가 소유하고 SeekAction에 주입·회수한다 — 외부 타깃 정보가 `WorkerActionContext`로 새지 않는 메모리 경계 규칙 준수. 방어력·회피는 현재 `Health` 직렬화 필드로 단순화하고, 향후 ScriptableObject·전략 분리는 두 번째 사용처 확인 후 추출한다. Seek는 매 Tick Physics 질의가 발생하는 구조이므로 적이 없을 때 throttle이 필요하면 별도 작업으로 분리한다. Codex 리뷰 에이전트 실행이 auto-mode 권한 정책으로 차단됐다.

IMP-012: `_registeredActions` 배열을 제거하고 `_resultStatData.ActionTypes`에서 직접 풀을 파생시켰다. 이로써 씬/프리팹 수정 없이 Farmer Critical 회귀가 코드 수준에서 완전 제거된다. Move는 stat 엔트리가 없는 공용 액션이므로 `Awake`에서 명시 등록한다. `RegisterPool`을 idempotent로 만들어 데이터에 실수로 Move가 포함돼도 이중 풀 생성을 방지한다. `IActionSet<TKey>` 삭제: 해당 인터페이스는 `WorkerActionSet`만 구현하며 어디에서도 타입으로 소비되지 않는 죽은 명목 인터페이스였다. Codex가 제안한 High/Medium/Low 항목 중 구조를 깨거나 조숙한 SOLID에 해당하는 것은 수정하지 않았으며, 각 항목의 제외 이유를 계획 문서에 명시했다.

IMP-011: `GuardActionSet` class was not created; instead `WorkerActionSet` was extended so Farmer and Guard share identical pooling logic differentiated only by the serialized `_registeredActions` array. This avoids ~120-line duplication and eliminates drift risk. `WorkerActionSet` stays in its current Farmer folder; relocation to `Common/` would require Unity to regenerate script references in the Farmer prefab and is deferred. `IDamageable` and `IAttackPower` are minimal seams — no concrete enemy or damage-calculation implementation is introduced. `AttackAction` follows the `DepositWheatAction` injection pattern: injections are set before the action starts and cleared on return. Codex review agent was not launched per user request (token limit).

## Blockers
| ID | Blocking Task | Problem | Required Decision |
|---|---|---|---|

## Verification Performed
| Task ID | Check | Result | Notes |
|---|---|---|---|
| IMP-023 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` completed with 0 warnings and 0 errors. |
| IMP-023 | Codex review agent | Launched | Asynchronous run started, run id `20260811-175130-135`; result will be written by the launcher to `PublicMD/Code_Evaluation_Result.md`. |
| IMP-020 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` completed with 0 warnings and 0 errors. |
| IMP-001 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-001 | Static reference check | Passed | Action hardcoded stat values were removed; WorkerAI has no result stat data reference. |
| IMP-002 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-002 | Serialized reference search | Passed | SampleScene manager has Default selector entry; selector template has WorkerActionSet with result stat data. |
| IMP-004 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-004 | Serialized reference search | Passed | DepositWheat action type, result data entry, destination mapping, and initial carry storage were found. |
| IMP-005 | Command-line C# build | Passed | DOTween reference resolved; 0 warnings and 0 errors. |
| IMP-005 | Static implementation check | Passed | MoveAnim and WorkAnim use local Transform properties, preserve FlipX through the X scale sign, and reset modified values when killed. |
| IMP-006 | Command-line C# build | Passed | New animation lookup/controller and action integration compile with DOTween references. |
| IMP-006 | Static prefab/reference check | Passed | FarmerAI SpriteRenderer is on child VisualRoot; WorkerAI has no animation dependency; actions use IAnimPlayer through context. |
| IMP-007 | Command-line C# build | Passed | CookActionSelector compiles against the existing generic selector contract. |
| IMP-007 | Static implementation check | Passed | Selector returns a null plan and does not depend on Farmer's WorkerActionSet. |
| IMP-008 | Command-line C# build | Passed | 0 warnings, 0 errors. |
| IMP-008 | Scene wiring search | Passed | `_warehouse` on selector template (fileID 568873992) references fileID 1903621705; fileID 1903621705 is in WarehouseObject component list and has WarehouseInventory script GUID a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6. |
| IMP-008 | Static logic review | Partial | Partial transfer preserves carry, but later Codex review found that carry-full + warehouse-full falls through to Work instead of entering a defined wait/blocked state. |
| IMP-009 | Command-line C# build | Passed | 0 warnings, 0 errors after updating moved compile paths. |
| IMP-009 | Structure and GUID audit | Passed | Old `Actors/Farmer` and `Actors/Cook` paths are absent; no duplicate asset GUIDs; existing scene/prefab script GUID references are unchanged. |
| IMP-010 | Command-line C# build | Passed | 0 warnings, 0 errors. GuardActionSelector and WallPerimeter compile cleanly. |
| IMP-010 | Static implementation check | Passed | GuardActionSelector implements IActionSelector contract; does not couple to Farmer WorkerActionSet; WorkerSelectorType.Guard added without breaking Default/Combat. |
| IMP-011 | Command-line C# build | Passed | 0 warnings, 0 errors. AttackAction, IDamageable, IAttackPower, updated WorkerActionSet, and ActionType.Attack all compile cleanly. |
| IMP-011 | Static implementation check | Passed | AttackAction implements IAction contract; null-guards target and attackPower in Start; applies TakeDamage and StatDelta on completion; WorkerActionSet._registeredActions loop replaces hardcoded Awake; Attack factory case, injection overload, and ResetAction extension are present. |
| IMP-012 | Command-line C# build | Passed | 0 warnings, 0 errors. Data-derived Awake, private TryRentAction, IActionSet deletion all compile cleanly. |
| IMP-012 | Static implementation check | Passed | WorkerActionSet.Awake always registers Move then iterates _resultStatData.ActionTypes; RegisterPool is idempotent; TryRentAction overloads are private; IActionSet.cs is deleted with no remaining references. |
| IMP-013 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj` — 오류 0, 경고 0. Physics2D.OverlapCircle(ContactFilter2D) deprecated 경고 없음 확인. |
| IMP-013 | Static implementation check | Passed | Health TakeDamage: 회피→return, `max(1,raw-def)` 하한, HP≤0→Die()→OnDied 단 1회 발행. SeekAction: holder null→Failed, 최근접 살아있는 IDamageable 선택, Cancel 안전. GuardActionSelector: HasLiveTarget 분기, attackPower GetComponentInParent, Seek/Move/Attack 각 실패 시 반환. WorkerAI: OnDied→plan Cancel→enabled=false, OnDisable에서 구독 해제. |
| IMP-013 | Codex review agent | Failed | auto-mode 권한 정책으로 launch 차단. 사용자가 직접 실행 가능. |
| IMP-017 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |
| IMP-017 | Static implementation check | Passed | `IReadOnlyList<IResidentCandidateView>` 공변 노출 확인. `CanRecruit` null 체크 + membership 체크 + `CanAfford` 흐름 확인. `TryRecruit` preflight→pay→spawn→remove 원자성(spawn 실패 시 후보 유지) 확인. `AlwaysAffordableCostPolicy` 항상 통과 확인. `[ContextMenu]` 디버그 메서드 존재 확인. |
| IMP-017 | Codex review agent | Launched | 에이전트 런치 성공(비동기, PID 34764). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |
| IMP-016 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |
| IMP-016 | Diff 확인 | Passed | moveState==Failed → Animation.Stop+return Failed; moveState==Success → idle 전환. Failed가 idle로 넘어가지 않음 확인. |
| IMP-016 | Codex review agent | Launched | 에이전트 런치 성공(비동기). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |
| IMP-015 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |
| IMP-015 | Asset 엔트리 확인 | Passed | _actionType: 7, _duration: 1, _statDelta: {0,0,0}, _wheatDelta: 0 확인. DepositWheat(6) 다음 위치 삽입. |
| IMP-015 | Codex review agent | Launched | 에이전트 런치 성공(비동기). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |
| IMP-014 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj` — 오류 0, 경고 0. |
| IMP-014 | Static implementation check | Passed | PatrolAction.Start: wander 지점 산출·Mover.StartMove·Move 애니. Tick: 매 tick EnemyScanner 스캔→발견 시 holder.SetTarget+Mover.Stop+Success; 이동 중 TickMove; 도착 후 Random idle 타이머; idle 완료 시 Success. Cancel: Mover.Stop+애니 정지+ClearPatrolParams. EnemyScanner: ContactFilter2D 스캔·최근접 IDamageable·IsAlive 필터. GuardActionSelector: 4단계 우선순위, GetPatrolAnchor 캡처, TryCreatePatrolPlan PatrolParams 조립. FarmerActionSelector: WorkerNeedsPolicy 위임(동작 불변). |
| IMP-014 | Codex review agent | Launched | 에이전트 실행 성공(비동기). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |
| IMP-018 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |
| IMP-018 | Guard 씬 엔트리 YAML grep | Passed | `_selectorSource: {fileID: 937587168}` 확인. Default 엔트리(568873992)는 변경 없음. |
| IMP-018 | Debug.Log grep | Passed | Common AI 폴더 내 `Debug.Log` 0건. LogWarning(설정 누락 경고) 유지 확인. |
| IMP-018 | Codex review agent | Launched | 에이전트 런치(비동기). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |
| IMP-019 | Command-line C# build | Passed | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |
| IMP-019 | Worker 내부 미접근 grep | Passed | Recruitment UI 폴더 내 WorkerAI·selector·WorkerAction 참조 — 주석 1건(의도적 경고 문구)만 확인. 런타임 의존성 없음. |
| IMP-019 | ESC → CloseTopPopup 흐름 | Passed | UIManager.Update()에서 Keyboard.current.escapeKey.wasPressedThisFrame → CloseTopPopup() 단독 호출 확인. |
| IMP-019 | 닫기 단방향 흐름 | Passed | RecruitmentPopup.OnCloseButtonClicked → _uiManager.ClosePopup(this); Close()는 UIManager를 되호출하지 않음. 멱등 확인. |
| IMP-019 | Codex review agent | Launched | 에이전트 런치(비동기). 결과는 PublicMD/Code_Evaluation_Result.md에 기록됨. |

## Next Actions

### IMP-024 이후 남은 작업 (기능 완료를 위한 필수 blocker)
1. **`InteractableManager`를 `SampleScene.unity`에 배치**하고 `_dataManager`, `_interactables`(Pub 오브젝트 + Well 오브젝트에 붙은 `Pub` 컴포넌트)를 연결. 현재 씬에 이 매니저가 없어 `BaseInteractable.Init`이 호출되지 않고 아이템 옵션이 하나도 생성되지 않는다.
2. **`FarmerActionSelector._decisionTuning`에 `Assets/Data/ScriptableObject/NPCDecisionTuning.asset` 연결**.
3. 위 두 가지 연결 후 `PublicMD/robust-wiggling-corbato.md`(세션 plan 파일) 검증 섹션의 수동 스모크 테스트를 수행하고 결과를 PROGRESS.md에 추가로 기록할 것.

### IMP-024 후속 과제 (범위 제외, 별도 작업으로 필요)
- **자동 Edit Mode 테스트 부재**: 프로젝트에 `.asmdef`가 0개라 테스트 어셈블리를 추가할 수 없었다(테스트 asmdef는 `Assembly-CSharp`을 참조 불가). `DestinationDecider`의 utility/critical 선택 로직은 회귀 위험이 높으므로, asmdef 마이그레이션과 함께 Edit Mode 테스트 도입을 별도 작업으로 진행해야 한다.
- **`NPC_Decision_System_Plan.md` 정식 개정**: "나온 김에" 탐욕적 체인 절이 이번에 폐기된 설계와 맞지 않는다. 문서를 새 단일 결정 구조에 맞게 다시 쓸 것.
- `NPCStat.ChangeMoveSpeed`의 자기 상한 clamp 버그, `Pub.cs` 관련 기존 이슈들은 이번 범위에 포함되지 않았다.

### IMP-022 이후 남은 작업 (Unity Editor 배선 필요)
0. **`DefaultStatContext` asset 생성 및 연결**: Project 창에서 `Scriptable Objects/DefaultStatContext` 메뉴로 기본 stat asset을 만들고, `NPCManager._defaultStatContext`와 `DataManager._statInfo`에 연결해야 한다. 연결 전에는 기존 hardcoded fallback stat이 사용된다.
1. **`DestinationDB` GameObject가 씬에 아예 없음**: `SampleScene.unity`를 grep한 결과 `DestinationDB` 참조가 0건. GameObject를 만들고 `DestinationDB` 컴포넌트를 부착한 뒤, `farmerWorkingPlace`/`sleepPlace`/`drinkPlace`/`eatPlace` 4개 키에 대응하는 목적지 Transform을 Inspector에서 채워야 한다. `[System.Serializable]`이 이번에 추가되었으므로 이 리스트가 처음으로 Inspector에 노출된다.
2. **`FarmerActionSelector`에 새로 생긴 `_destinationDB` 슬롯 연결**: 씬의 `FarmerActionSelector` 오브젝트(라인 543 부근)에서 위 `DestinationDB`를 드래그해 연결해야 한다.
3. **need 증가/최대치 시스템**: `NPCStat`의 `_fatigueMax/_hungerMax/_thirstMax`가 생성자에서 설정되지 않아 0이다. 방어 로직만 있는 지금은 세 percentage가 항상 0%이고, `DestinationDecider`는 런타임에서 항상 Work만 선택한다. 사용자가 이 시스템을 별도로 구현할 예정(계획 확정 사항). 완료되면 `DestinationDecider`의 `const` 튜닝 값들을 데이터 에셋으로 이관하는 작업이 뒤따른다 (`PublicMD/NPC_Decision_System_Plan.md` 참고).
4. **큐 소비 측 연결**: `WorkerNPC`는 여전히 `Temp_CreateNewActionMove()`로 랜덤 이동만 반복한다. `FarmerActionSelector.RequestNewActionQueue`를 호출하고 큐를 순차 실행할 runner가 아직 없다.
5. **Eat/Sleep/Farming/Drink action 구현**: 4개 모두 `NotImplementedException` 스텁이다. 큐가 실제로 실행되기 전에 반드시 구현되어야 한다.

### Unity Editor 직렬화 연결 (Play Mode 전 필수, Guard 시스템 — 리셋 이전 문서, 최신 구조와 불일치할 수 있음)
1. **Enemy 레이어 생성**: Project Settings → Tags & Layers → 새 레이어 "Enemy" 추가.
2. **적 테스트 GameObject 만들기**: `Enemy` + `Health` + `Collider2D`(Circle 등) 컴포넌트, 레이어 = Enemy.
3. **가드 prefab에 `Health` + `AttackPower` 부착**, HP/공격력 수치 설정.
4. **Guard selector 템플릿**: `GuardActionSelector` + `WorkerActionSet` 구성.
   - `_scanRadius`(예 6), `_enemyMask`(Enemy 레이어), `_engageRange`(예 1.2)
   - `_patrolRadius`(예 2~3), `_patrolIdleMin`(예 0.5), `_patrolIdleMax`(예 1.5)
   - `_patrolAnchor`: 비워두면 첫 선택 시 스폰 위치 캡처 (또는 명시 Transform 지정)
   - `_destinationProvider`: 가드용 Eat/Drink 목적지 포함 여부 확인
   - `WorkerActionResultStatData` 에셋에 **Attack 엔트리** 존재 확인. ✓ (IMP-015 완료)
5. **`WorkerAIManager`**: `_selectorEntries`에 `{Guard, template}` 추가, 테스트 시 `_initialSelectorType = Guard`.

### Play Mode 검증 항목
- 적 없음 → 가드가 앵커 반경 내를 조금씩 이동하고 가끔 멈춤(얼어붙지 않음).
- 순찰 중 scan 반경 안에 적 등장 → 즉시 이동→공격 → 처치 → 순찰 복귀.
- 순찰 중 Hunger/Thirst 임계 초과 → 음식/물로 이동·회복 → 순찰 복귀.
- 가드 HP 0 → AI 정지(plan Cancel, Tween 정리).
- Farmer도 기존 Eat/Drink/Rest/Work 우선순위 동일하게 동작(WorkerNeedsPolicy 추출 회귀 없음).

### 이후 과제
- **모집 UI 수동 배선 (IMP-019 이후, Unity Editor 작업)**
  - 씬에 `UIManager` GameObject 배치. `_popupEntries`에 `{Recruitment, RecruitmentPopup}` 연결.
  - `RecruitmentPopup` GameObject에 `RecruitmentManager`, `UIManager`, `_slotPrefab`, `_slotParent`, `_contentRoot` Inspector 연결.
  - `RecruitmentCandidateSlot` 프리팹 제작: `_nameText`, `_kindText`, `_costText`(TMP_Text), `_portraitImage`(Image), `_recruitButton`(Button), `_statLineParent`(Transform) 연결.
  - 닫기 버튼 `Button.onClick` → `RecruitmentPopup.OnCloseButtonClicked` 연결.
  - (`_recruitButton`은 `Bind` 호출 시 코드로 리스너 설정되므로 Inspector 추가 배선 불필요.)
- **모집 시스템 연결 지점 (IMP-017 이후)**
  - `IResidentSpawner` 어댑터 구현: candidate→WorkerInitialStats 매핑 정의 후 `WorkerAIManager.SpawnWorker`와 연결.
  - 실제 골드/지갑 시스템 구현 시 `IRecruitmentCostPolicy` 교체 + `Refund` 경로 추가.
  - 후보 목록 UI 배선 완료 후 `RecruitmentManager.Candidates`(`IReadOnlyList<IResidentCandidateView>`)에 직접 바인딩 검증.
  - AD-012 결정 후 후보 갱신 주기·비용 공식·네임드 중복 정책을 `RecruitmentManager`에 추가.
  - `Settlement/Resident Candidate` 메뉴로 `ResidentCandidateDefinition` 에셋 생성 및 `RecruitmentManager._candidateDefinitions`에 할당해 인스펙터 검증.
- **Guard 시스템**
  - `WorkerAIManager` spawn 시 `Health.Init(maxHp, def, dodge)` 주입(스탯 파생 연동, AD-002).
  - Patrol throttle: 매 프레임 Physics 질의 성능 영향 시 n tick 간격 스캔 도입.
  - 욕구 회복 후 patrol anchor가 의도와 다를 경우 anchor re-capture 정책 검토.
  - 성곽(castle wall) 파괴 가능 오브젝트 / 마을 침입 트리거는 별도 슬라이스로 분리.
