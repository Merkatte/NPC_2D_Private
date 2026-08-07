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
IMP-022 completed on 2026-08-02: `DestinationDecider`/`FarmerActionSelector`의 판단 계층과 큐 조립 계층을 분리했다. `DestinationDecider`(순수 C#, `System/Lib`, 직업 공용)는 `Decide(NPCStat, NPCType, Vector3) -> NPCDecision`을 반환하며, 순서 있는 `NPCDecisionStep[]`으로 "이동→보급→이동→작업" 체인을 표현한다. 보급 후보(Drink/Eat/Sleep)는 순열 전체 열거 대신 탐욕적 한계이득 체인으로 하나씩 선택하고, critical 임계값(95%) 초과 시 즉시 해당 need로 단락한다. `FarmerActionSelector`는 `NPCDecision.Steps`를 순회하며 `Queue<IAction>`을 조립하기만 하고 계산식을 갖지 않는다(grep으로 확인). `ActionType.Drink`/`DrinkAction`(EatAction과 동일한 스텁 스타일) 추가, `ActionPool`에 배선. `NPCStat`의 `Current*Percentage`에 0 나눗셈 방어(`GetPercentage`) 추가. `DestinationDB.DestinationInfo`에 `[System.Serializable]` 추가(이전에는 Inspector에 노출되지 않아 런타임 딕셔너리가 항상 비어 있었음). 설계 근거는 `PublicMD/NPC_Decision_System_Plan.md`에 별도 기록. 빌드 오류 0, 경고 0. Codex 리뷰 에이전트 런치(비동기).
IMP-021 completed on 2026-07-22: ScriptableObject asset instances were moved out of `Assets/Scripts` into `Assets/Data` while keeping their `.meta` files with the assets so Unity GUID references remain stable. ScriptableObject class definitions remain in their owning script folders.
IMP-020 completed on 2026-07-03: popup infrastructure now supports lazy Instantiate, inactive caching, LRU trimming, and a PopupManager-compatible component name. Command-line C# build passed with 0 warnings and 0 errors.
주민 모집 UI 뼈대 스크립트를 구현했다(IMP-019). 공유 UI 인프라(`IPopup`, `UIPopupType`, `UIManager`) 3종과 모집 전용 UI 뼈대(`RecruitmentPopup`, `RecruitmentCandidateSlot`) 2종을 신규 생성했다. `UIManager`는 LIFO 팝업 스택 + ESC 처리(Input System)를 담당하며 도메인 로직을 소유하지 않는다. `RecruitmentPopup`은 도메인 API 4종(`Candidates`, `CanRecruit`, `TryRecruit`, `IResidentCandidateView`)만 사용하며 닫기는 항상 `UIManager.ClosePopup`을 경유해 스택 동기화를 보장한다. 빌드 오류 0, 경고 0.

## Completed Tasks
| Task ID | Date | Summary | Evidence | Related REQs |
|---|---|---|---|---|
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

### IMP-022 이후 남은 작업 (Unity Editor 배선 필요)
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
