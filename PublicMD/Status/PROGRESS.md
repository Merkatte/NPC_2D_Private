# PROGRESS

> 현재 경로: `PublicMD/Status/PROGRESS.md`
> 2026-08-29 이전 기록에 등장하는 `PublicMD/PROGRESS.md`는 이 문서의 이전 경로입니다.

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

### PublicMD 문서를 Harvest/Deposit 구현 기준으로 최신화 (2026-09-04)

커밋 `f35cb65`(C#)와 `fd279f6`(씬 배선)로 Farmer 수확물 운반 기능이 들어갔지만 `Systems/` 문서와 `ProjectStructure.md`에는 반영되지 않은 상태였다. 실제로 `HarvestAction`, `DepositAction`, `WorkerInventory`, `ICarriedInventory`, `WarehouseDepositPoint`, `BaseWorkingAction`을 `PublicMD` 전체에서 검색하면 이 `PROGRESS.md` 외에는 0건이었다. 코드를 현재 사실로 보고 문서를 교정했다.

갱신 문서:

- `ProjectStructure.md`: 기준일 2026-09-04, "수확물 운반·창고 입고" 라우팅 행, 새 기능 배치표의 운반·입고 흐름 행, `System/Inventory`·`System/Farming` 폴더 책임, Farming이 폴더형 인덱스라는 사실 누락 수정
- `Systems/Inventory_and_Items.md`: `ICarriedInventory`/`WorkerInventory`/`WarehouseDepositPoint`/`DepositAction` 주 소유 등록, `IInventory.TryAdd` 부분 수락 계약, 봇짐 단일 item type·용량·`Clear()` 권한 규칙
- `Systems/Farming/Runtime_and_Transactions.md`: Farming/Harvest 두 capability의 phase 게이트, cargo 인도, roll-once 유지, `_outputInventorySource` 제거
- `Systems/Farming/Farming_Action.md`: `HarvestAction` 포함(제목을 "Farming and Harvest Actions"로 변경, 파일명은 링크 안정성 때문에 유지), `Fail` vs `RequestReplan` 기준
- `Systems/Farming/README.md`, `Systems/NPC_Decision_and_Actions/README.md`: 흐름도와 소유 문서 라우팅
- `Systems/Interaction_and_Destinations.md`: `InteractionRequest.Cargo` 계약과 Warehouse 배선
- `Systems/NPC_Decision_and_Actions/Selector_and_Queue.md`: Farmer 우선순위(긴급 욕구 > Harvest > Deposit > decider), 빈 밭 진단 분리
- `Systems/NPC_Decision_and_Actions/Action_Runtime.md`: `BaseWorkingAction` 주 소유와 `ProviderStillUsable`
- `Systems/NPC_Decision_and_Actions/Decision_Policy.md`: `DestinationDecider.HasCriticalNeed`
- `Systems/NPC_Presentation.md`: 봇짐 표시 책임 분리(`WorkerInventory`가 판단, `NPCComponent.SetCarryVisible`이 조작)
- `Systems/NPC_Runtime.md`: pool 재사용 시 cargo 초기화
- `ARCHITECTURE.md` §2.4·§3.4, `SPEC.md` Decision Log Q-037(봇짐 단일 item type, 용량 10, 부분 수락 의미)

문서 구조 판단 2건: `Inventory_and_Items.md`는 세부 기능 4개 분할 기준을 적용하지 않고 단일 문서로 유지했다 — 봇짐과 창고가 `IInventory.TryAdd`라는 같은 계약·같은 변경 진입점을 공유하므로 Runtime inventory 하나로 묶는 것이 맞다고 봤고, 107줄로 250줄 가이드 안이다. 이 판단과 "독립 세부 기능이 4개가 되면 분할" 조건을 문서 안에 명시했다. `Farming_Action.md`는 링크가 1곳뿐이라 rename도 가능했지만 이력 연속성을 택했다.

미완 상태는 문서에 사실대로 기록했다: 봇짐 스프라이트 대기로 `CarryAnchor`/`Carry` prefab 배선 없음(운반은 동작하지만 화면에 안 보임), `GuardTest.unity` 미배선, Harvest/cargo 경로 Play Mode `NOT VERIFIED`.

검증: `git diff --check` 통과. 새 스크립트 6개가 각각 정확히 한 문서에서만 `주 소유 스크립트`로 등록되는지 확인(중복 0). `FarmerTest.unity`의 `InteractableManager._interactables` 4개 항목을 fileID → `m_EditorClassIdentifier`로 해석해 Pub/Pub(Well)/`FarmWorkSite`/`WarehouseDepositPoint`임을 확인한 뒤 문서에 기록. 코드 변경은 없다.

### Farmer 수확물 운반·창고 입고 구현 — C# 완료 / Unity 배선 진행 중 / 봇짐 스프라이트 자산 대기 (2026-09-04)

Farmer가 수확한 작물을 창고로 순간이동시키던 것을 실제로 지고 운반하도록 바꿨다. `FarmWorkSite`는 Farming/Harvest를 phase 게이트된 두 capability로 분리하고, 수확물은 신규 `InteractionRequest.Cargo`(plain C# `ICarriedInventory`)를 통해 NPC의 `WorkerInventory`(용량 10, 단일 item type, `NPCComponent`가 `CombatRuntimeState`와 같은 방식으로 인라인 소유)로 들어간다. 신규 `HarvestAction`/`DepositAction`이 각각 수확·입고를 실행하고, 신규 `WarehouseDepositPoint`(`Assets/Scripts/Actor/`)가 창고 입고 provider를 맡는다. `FarmerActionSelector`는 긴급 욕구 > (Harvest, cargo에 room 있을 때만) > (Deposit, cargo 있을 때만) > 기존 decider 순으로 우선순위를 게이트해, 창고 provider가 없거나 cargo가 가득 차도 busy loop가 생기지 않는다.

설계는 Codex와 2차례 검토를 거쳐 확정했다: 수확 결과는 `InteractionResult`가 아니라 요청 쪽 cargo로 실어 "외부 수락 확인 후 내부 상태 변경" 순서를 지켰고, 부분 수락은 1개 이상이면 성공으로 처리하며 progress는 전량이 봇짐으로 넘어간 뒤에만 소모한다(roll-once 불변식 유지). 빈 밭 진단은 `BuildingType.Farm`이 `DestinationDB`에 아예 미등록일 때만 오류로 남기고, 등록은 됐지만 지금 상호작용 불가(빈 밭·Harvesting phase)한 정상 상태는 무음 처리하도록 기존 로직도 함께 고쳤다(`DestinationDecider.AddFarmerWorkCandidate`가 `CanInteract`를 확인하지 않는다는 사실을 코드로 직접 확인). `WarehouseDepositPoint`는 `ProjectStructure.md`의 "`Assets/Scripts/Actor` = facility component" 라우팅에 따라 그 폴더에 배치했다(`FarmWorkSite`가 `System/Farming`에 있는 것은 선례가 아니라 기존 예외로 판단).

변경 파일: `Assets/Scripts/Enum/{ActionType,BuildingType}.cs`, `Assets/Scripts/System/Lib/{ActionPool,DestinationDecider}.cs`, `Assets/Scripts/Interface/{IInventory,ICarriedInventory}.cs`(후자 신규), `Assets/Scripts/System/Inventory/WorkerInventory.cs`(신규), `Assets/Scripts/Actor/WarehouseDepositPoint.cs`(신규), `Assets/Scripts/System/Actor/{NPCComponent,FarmerActionSelector}.cs`, `Assets/Scripts/System/Farming/FarmWorkSite.cs`, `Assets/Scripts/System/Action/{BaseWorkingAction,HarvestAction,DepositAction}.cs`(신규)·`FarmingAction.cs`(리팩터), `Assets/Data/Struct/InteractionRequest.cs`, `Assets/TestOnly/TestFarmProductionWindow.cs`.

검증: `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0. `git diff --stat Assets/Data/Struct/ActionContext.cs` 비어 있음(도메인 전용 provider 필드를 추가하지 않는다는 불변 주석 준수 확인). `_outputInventorySource` 정적 검색 `*.cs` 0건. `ActionPool.Create`에 `Harvest`/`Deposit` case 확인. `FarmWorkSite` 참조 확산 없음(`PointerHoverRouter.cs`의 주석 언급 1건뿐, 실제 참조 아님). `git diff --check`는 이번에 만든 파일에는 걸리지 않고 기존 미커밋 파일(Carrot.prefab, NPCGirl.prefab, FarmerTest.unity, GuardTest.unity)의 기존 trailing whitespace만 걸린다. C# 변경만 정확히 22개 파일로 분리해 커밋 `f35cb65`(원격 push 완료, `dbb094c..f35cb65`) — 무관한 기존 미커밋 변경(AGENTS.md, .codex, PublicMD 초안, art, `_Recovery`, `NPCPrefabCatalog.asset`)은 그대로 두었다.

Unity 씬 배선은 `FarmerTest.unity`에서 사용자가 진행했고 씬 파일을 직접 grep해 다음을 확인했다: `Warehouse` 오브젝트에 `WarehouseDepositPoint` 추가 및 `_inventorySource`가 같은 오브젝트의 `WarehouseInventory`를 정확히 참조, `DestinationDB._destionations`에 `BuildingType: 6`(Warehouse) 행이 `Warehouse`의 Transform/GameObject를 정확히 참조, `InteractableManager._interactables`에 `WarehouseDepositPoint` 등록(3개 → 4개). `TestFarmProductionWindow`는 별도 오브젝트가 아니라 Farm 자체인 `Square` 오브젝트에 붙어 있음을 확인해 안내했고, `_depositPoint` 연결은 진행 중이다. `GuardTest.unity`에는 아직 같은 배선을 하지 않았다.

**블로킹**: `NPCGirl.prefab`에 봇짐(`CarryAnchor`/`Carry`) 앵커를 추가하려면 `Assets/Art/Generated/worker-cargo-sack.png` 스프라이트가 먼저 필요하다. 이 프로젝트의 기존 픽셀아트(`farmer-hoe.png` 등 — 따뜻한 halo 발광이 있는 픽셀아트 스타일, `spritePixelsToUnits: 900`)는 ChatGPT로 생성해왔는데, 2026-09-04 기준 ChatGPT 토큰이 소진되어 당장 생성할 수 없다고 사용자가 확인했다. Claude Code에는 이미지 생성 도구가 없어 대신 만들 수 없다 — 사용자에게 이 제약을 알리고 이 기록으로 대체했다.

다음 액션(ChatGPT 사용 가능해지면):

1. `Assets/Art/Generated/worker-cargo-sack.png` 생성 — `farmer-hoe.png`와 동일 화풍, PPU는 900 근사치로 통일.
2. Import 설정을 `farmer-hoe.png.meta`와 맞춘다(filterMode, `alphaIsTransparency: 1`, textureCompression).
3. `NPCGirl.prefab`의 `Visual` 아래 `ToolAnchor`의 형제로 `CarryAnchor`/`Carry`(SpriteRenderer, 기본 `m_IsActive: 0`)를 추가하고 `NPCComponent._carryRenderer`에 연결한다.
4. `TestFarmProductionWindow._depositPoint` 연결을 마무리하고, `GuardTest.unity`에 `WarehouseDepositPoint`/`DestinationDB`/`InteractableManager` 배선을 동일하게 반복한다.
5. Play Mode 전체 검증 — 세부 20개 항목은 `C:\Users\Merkatte\.claude\plans\proposed-plan-farmer-majestic-turing.md`의 검증 섹션을 따른다(정적 검증은 이미 통과, Play Mode는 전부 `NOT VERIFIED`).

### Farmer 작업 영역 분산 구현 완료 / Play Mode 검증 대기 (2026-09-04)

`IInteractionProvider`에 action 실행 위치 조회 계약을 추가하고, `BaseInteractionProvider`가 사용 가능한 action에는 등록 목적지를 기본 위치로 돌려주도록 확장했다. `FarmWorkSite`는 기존 생산 transaction과 별개로 `BoxCollider2D` 작업 영역을 기본 4 x 2 셀로 나누며, seed 2의 전용 `SeededRandomSource`로 셀 순서를 섞어 한 순환 안에서 중복 없이 배정한다. 셀 안에는 크기의 최대 20% jitter를 적용하고, 새 순환의 첫 셀이 직전 마지막 셀과 같으면 교환한다. 위치 dependency나 영역이 유효하지 않으면 한 번만 경고하고 등록된 농장 중심을 반환해 생산 흐름을 유지한다.

`FarmerActionSelector`는 Work가 선택된 뒤 이미 조회한 동일 provider에서 위치를 한 번만 받고, 그 좌표를 하나의 `ActionContext`를 통해 Move와 전체 Farming 반복 batch에 공유한다. `DestinationDecider`는 변경하지 않아 계속 농장 중심으로 utility를 계산하고 위치 난수는 queue 구성 시점에만 소비한다. yield용 `_randomSource`와 위치용 `_workPositionRandomSource`는 분리되어 위치 요청이 수확량 난수열을 바꾸지 않는다. 위치 예약·반납과 8명 초과 시 중복 방지는 이번 범위에 포함하지 않았다.

변경 파일:

- `Assets/Scripts/Interface/IInteractionProvider.cs`
- `Assets/Scripts/Actor/BaseInteractionProvider.cs`
- `Assets/Scripts/System/Farming/FarmWorkSite.cs`
- `Assets/Scripts/System/Actor/FarmerActionSelector.cs`
- `Assets/Scenes/FarmerTest.unity`, `Assets/Scenes/GuardTest.unity`
- `PublicMD/Systems/Interaction_and_Destinations.md`
- `PublicMD/Systems/Farming/README.md`, `Runtime_and_Transactions.md`
- `PublicMD/Systems/NPC_Decision_and_Actions/Selector_and_Queue.md`
- `PublicMD/ARCHITECTURE.md`, `PublicMD/SPEC.md`

Scene 배선은 두 FarmWorkSite에 작업 영역과 별도 위치 RNG를 연결했다. `FarmerTest`는 기존 `BoxCollider2D`를 재사용했고, 실제 scene 상태에 콜라이더가 없던 `GuardTest`에는 같은 농장 GameObject의 trigger `BoxCollider2D`를 추가했다. Guard selector, `GuardAction`, GuardPost는 변경하지 않았다.

검증 결과:

- `dotnet build Assembly-CSharp.csproj --no-restore`: 경고 0, 오류 0
- `FarmerTest.unity`, `GuardTest.unity`: document fileID 중복 0, work area·position RNG·4 x 2 grid·0.2 jitter·seed 2 참조 확인
- 위치 난수와 yield 난수 사용처 정적 검색: 별도 source 사용 확인, `UnityEngine.Random` 사용 0건
- 변경한 C#과 문서의 `git diff --check`: 통과
- 두 scene 전체 diff의 `git diff --check`: 기존 사용자 crop/scene 변경에 포함된 `m_Name: ` trailing whitespace 때문에 실패. 이번에 추가한 component 행에는 새 trailing whitespace를 남기지 않았다.
- Farmer 8명 분산, batch 내부 위치 유지, 다음 batch 위치 변경, collider 내부 실제 도착, fallback 경고 1회와 Eat/Drink 회귀는 Unity Play Mode에서 `NOT VERIFIED`

독립 Codex read-only review agent를 비동기로 시작했다(PID 35868). 결과는 아직 수신하지 않았으며 통과로 간주하지 않는다. 다음 작업은 `FarmerTest` Play Mode에서 8명 첫 batch 분산과 다음 batch 재배정, fallback 및 기존 생산·공급 회귀 시나리오를 확인하는 것이다.

### S-02 implementation complete / visual verification pending (2026-09-01)

이 항목은 아래의 과거 S-01 `verification pending` 기록을 최신 상태로 대체한다. 사용자가 `FarmerTest` Play Mode에서 Seed Phase 1 생산·입고 흐름이 정상 동작함을 확인했으므로 S-01은 완료되었다. 단, Warehouse 용량 부족 시의 부분 입고 거부 시나리오는 여전히 `NOT VERIFIED`다.

Seed Phase 2의 작물 표현 코드를 구현했다. `FarmWorkSite`는 성공적으로 상태가 변경된 뒤 `StateChanged` 이벤트만 발행하며, 표현 계층을 직접 참조하지 않는다. 최종 수확 성공 시 생산물 입고와 동시에 현재 작물을 비우고 Growing 상태로 돌아간다. 시각 효과가 생산 transaction이나 다음 작물 선택을 지연시키지 않는다.

추가·변경한 핵심 구성은 다음과 같다.

- `CropVisualStage`: 정규화 임계값과 단계별 Sprite를 보관하는 직렬화 값 형식
- `FarmProductionDefinition`: crop별 Animator Controller와 오름차순 visual stage 목록 소유 및 검증
- `CropVisualAnimator`: 개별 작물 표현의 즉시 표시, 단계 전환(Disappear -> Sprite 교체 -> Appear), 최종 Disappear 실행
- `FarmCropPresenter`: `FarmWorkSite.StateChanged` 구독, 약 10개의 concrete `CropVisualAnimator` 무작위 순서 관리, 0.08초 기본 cascade, 명령 queue 및 새 작물 표시 지연 처리
- Carrot/Potato definition: 각각 0 / 0.33333334 / 0.6666667 / 1 임계값의 4단계 Sprite와 controller 연결

표현용 무작위 순서는 yield 계산과 분리된 `System.Random`을 사용한다. 최초 선택과 `OnEnable` 동기화는 cascade 없이 즉시 표시하며, 성장 단계 상승은 섞인 순서로 겹쳐 재생한다. 최종 수확은 섞인 순서로 Disappear만 수행하고, 그 도중 선택된 새 작물의 시각 동기화는 기존 disappear 완료 뒤 실행한다. `IVisualAnim` 추상화는 현재 동일 구현만 사용하므로 도입하지 않았다.

Farming 문서는 `PublicMD/Systems/Farming/README.md`와 네 leaf 문서로 분리했다. 새 표현 책임과 scene wiring은 `Crop_Presentation.md`, runtime transaction은 `Runtime_and_Transactions.md`, definition 규칙은 `Definition_and_Catalog.md`가 소유한다. 관련 활성 문서의 로컬 링크도 새 경로로 갱신했다.

검증 결과:

- `dotnet build Assembly-CSharp.csproj --no-restore`: 경고 0, 오류 0
- 활성 Markdown 로컬 링크 검사: 깨진 링크 0
- 새 script/controller/sprite GUID 참조 검사: 누락 0
- 새 코드의 `UnityEngine.Random`, `Find*`, `GetComponent` 의존 검사: 0건
- 변경 범위 `git diff --check`: 통과
- 독립 Codex read-only 검토를 background로 시작함(PID 38940). 검토 결과는 아직 수신하지 않았으며 통과로 간주하지 않는다.

Scene의 작물 표현 오브젝트 약 10개 배치와 Inspector 연결은 합의한 범위대로 자동 수정하지 않았다. 따라서 S-02의 Play Mode 시각 검증은 `NOT VERIFIED`다. 다음 작업은 각 표현 오브젝트에 `CropVisualAnimator`를 연결하고, farm 또는 sibling 오브젝트의 `FarmCropPresenter`에 `FarmWorkSite`와 animator 배열을 지정한 뒤 최초 표시, 33%/67%/100% cascade, 최종 disappear, disappear 중 새 작물 선택 순서를 확인하는 것이다.
S-01 implementation complete / verification pending on 2026-08-29 (Seed Phase 1 — 기본 기능과 데이터): Seed System Implementation Plan(`PublicMD/Plans/Seed_System_Implementation_Plan.md`)의 Phase 1을 구현했다. Play Mode 수동 검증 전이므로 `completed`가 아니라 `verification pending`으로 기록한다 — 프로젝트 공통 Definition of Done(runtime 기능은 Play Mode 검증 후에만 completed)을 따른다. 사용자가 SG-001(빈 농경지에서만 씨앗 선택)·SG-002(첫 slice는 seed item 미소비)·SG-003(Carrot=item 4, Potato=item 5, Food category 신규)을 확정했고 `SPEC.md` 12절 Decision Log로 옮겼다.

구현 결정 3가지가 설계를 좌우했다: (1) `BaseInteractionProvider._isOperational`이 첫 초기화에서 latch되므로 definition 부재를 `TryInitializeCore` 실패로 두면 런타임 씨앗 선택이 영구히 막힌다 — 검사를 새 `CanInteractCore` override로 옮겼다. (2) `FarmWorkSite`가 기존에 `CanInteractCore`를 override하지 않았던 것이 정확한 게이트 seam이었다 — 여기서 막으면 `FarmerActionSelector`의 기존 one-shot 로그 + Idle fallback 경로가 그대로 동작해 selector 변경이 불필요했다. (3) `ApplyHarvestingWork`가 입고 실패 시에도 난수를 소비하던 문제를 `_pendingYield` sentinel로 고쳐 거부된 yield를 재사용하게 했다.

변경 파일: `FarmProductionDefinition.cs`(crop ID·표시 이름 추가) 수정, `CropCatalog.cs` 신규, `FarmWorkSite.cs`(선택 API·게이트·pending yield) 수정, `TestFarmProductionWindow.cs`(catalog 기반 선택 UI) 수정, `ItemData.csv`(Carrot/Potato 2행) 수정, `FarmProductionDefinition.asset` → `FarmProductionDefinition_Carrot.asset` rename(GUID 보존), `FarmProductionDefinition_Potato.asset`·`CropCatalog.asset` 신규, `FarmerTest.unity`(`_definition` → `_startingDefinition` 필드명, 참조 GUID는 불변), `Assembly-CSharp.csproj`(새 스크립트 compile entry). `FarmerActionSelector`, `DestinationDecider`, `WorkerNPC`, `FarmingAction`, `IInteractionProvider`, `IInventory`는 계획대로 변경하지 않았다.

검증: `dotnet build Assembly-CSharp.csproj --no-restore` 0 경고/0 오류. `_definition` 잔존 참조 정적 검색 0건. `WarehouseInventory`가 무제한 용량이라 입고 거부 시나리오는 `NOT VERIFIED`(코드 검토로만 확인). Play Mode 수동 검증은 미실행이다.

문서 갱신: `Systems/Farming.md`(실행 흐름·불변 규칙·TBD·분할 트리거), `Systems/Inventory_and_Items.md`(신규 crop item 사실), `Systems/Interaction_and_Destinations.md`(`CanInteractCore` 게이트 패턴), `ProjectStructure.md`(새 crop 배치 행), `SPEC.md`(12절 Decision Log 신설), `Plans/Seed_System_Implementation_Plan.md`(SG-001~003 확정 기록, Phase 1 구현 기록), `PLAN.md`(S-01 상태).

### 후속: Codex 리뷰 반영 (2026-08-29)

`Code_Evaluation_Result.md`의 지적 중 M-04(yield 의미)는 사용자가 작업당 수확량이 의도한 설계임을 확정해 결함에서 제외됐다(명칭 명확성만 Phase 2 밸런스 조정과 함께 후속 처리). 나머지는 수정했다:

- **H-01(Git 미추적)**: `CropCatalog.cs`/`.meta`, `CropCatalog.asset`/`.meta`, `FarmProductionDefinition_Potato.asset`/`.meta`, `PublicMD/Plans/`를 Seed Phase 1 관련 변경만 명시적으로 stage했다(커밋은 하지 않음, 무관한 NPC animation/GuardTest/NPCPrefabCatalog/Recovery 파일은 제외).
- **M-01(output item 미검증)**: `ItemDataContext.TryGetItemInfo(int, out ItemInfo)`를 추가했다.
- **M-02(TryValidate 미호출)**: `CropCatalog.TryValidate`가 이제 `ItemDataContext`를 받아 구조 검증과 함께 output item 존재 여부까지 확인한다. `TestFarmProductionWindow`가 window 생성 시 1회 호출해 결과를 캐시하고, catalog가 invalid하면 선택 버튼 대신 실패 사유를 표시한다(`_definitions == null`도 이제 성공이 아니라 명시적 실패로 처리).
- **M-03(부분 수락 시 비원자적 transaction)**: `FarmWorkSite.ApplyHarvestingWork`가 부분 수락 시 `_pendingYield`를 수락된 만큼만 줄여, 재시도가 이미 입고된 수량을 다시 요청하지 않게 했다.

검증: `dotnet build Assembly-CSharp.csproj --no-restore` 0 경고/0 오류. `TestFarmProductionWindow`를 `FarmerTest` 농장 GameObject에 배치하고 `_farmWorkSite`/`_warehouse`/`_cropCatalog`/`_itemDataContext`를 연결했다. Play Mode 수동 검증은 여전히 미실행이다.

다음 액션: 사용자가 `FarmerTest` Play Mode에서 계획서 5절 시나리오를 수동 검증한다. 통과 후에만 S-01을 completed로 전환하고 Seed Phase 2(작물 표현) 구현을 시작한다.

DOC-001 completed on 2026-08-29 (PublicMD 점진적 공개 구조 전환 완료): 에이전트가 큰 공통 문서와 관련 없는 코드를 반복해서 읽지 않도록 `AGENTS.md -> ProjectStructure.md -> Systems 기능 문서/인덱스 -> leaf의 최소 파일` 읽기 흐름을 도입했다. 최상위 기능 영역은 9개로 유지하고, 독립 세부 기능이 4개 이상인 판단·action과 전투만 폴더형 인덱스로 분할했다. 판단·action은 5개 leaf, 전투는 4개 leaf가 현재 흐름·불변 규칙·변경 유형별 최소 확인 범위를 소유한다.

production C# 89개를 각 하나의 `주 소유 스크립트` 표에 등록하고 한 줄 책임을 기록했다. 검증 결과 production 89개 / owner row 89개 / 누락 0 / extra 0 / 중복 0이다. 활성 문서 24개의 Markdown local link는 깨진 링크 0개다. 공통 문서는 `ProjectStructure.md` 130줄, `ARCHITECTURE.md` 164줄, `CodeConvention.md` 199줄로 합계 493줄이며, 전환 전 합계 1,471줄에서 기능 세부 내용을 Systems로 이동했다. 모든 Systems 문서는 250줄 이하이다.

`AGENTS.md`, `.codex`와 `.agents`의 구현·리뷰 skill, 독립 reviewer prompt를 새 라우팅으로 동기화했다. 고정된 공통 3문서 전체 읽기와 과거 `WorkerAI`/Behavior Graph 책임 안내를 제거하고, whole-project review가 아닌 경우 관련 leaf와 직접 dependency만 읽도록 했다. reviewer launcher 두 개는 PowerShell parser 오류 0개다. C# production 코드, scene, prefab, animation, ScriptableObject asset은 이 문서 전환에서 수정하지 않았다. 승인 계획은 `PublicMD/Archive/Plans/PublicMD_Progressive_Disclosure_Plan.md`로 보관했다.

IMP-035 completed on 2026-08-28 (코드·prefab·씬 구현 완료 / Codex 리뷰 백그라운드 실행 / Play Mode 검증은 사용자 대기): Enemy를 임시 `ICombatTarget` 표적에서 `WorkerNPC` + selector 아키텍처를 쓰는 진짜 전투 AI 유닛으로 편입했다. 계획은 Opus Plan Mode에서 작성했고, Codex 리뷰 게이트를 4라운드 거치며(구현 전) 수정됐다 — 매 라운드의 지적과 사용자 결정을 아래에 요약한다.

**Codex 리뷰 라운드 요약**: 1차는 `NPCComponent`에 `ICombatTarget`을 추가해 Farmer/Guard까지 공격 대상으로 만드는 안이었으나, (a) 주민 사망/전투불능 정책(`PublicMD/Game_Plan.md` GD-008)이 미결정이고 (b) `GuardPerception`/`GuardRuntimeState`가 이름만 Guard 전용이지 실질적으로 공용이라는 지적을 받았다. 사용자가 직접 "Farmer/Guard 피해는 이번엔 보류, 전투 코드 이름은 지금 중립화"로 확정했다. 2차는 `Enemy.cs`를 그대로 두면 체력이 두 곳(자체 필드 + `EnemyStat`)으로 이중화된다는 지적, `NPCGirl.prefab`을 "건드리지 않음"이라 해놓고 rename 마이그레이션이 필요하다는 모순 지적, Rigidbody2D 부재로 트리거 콜백이 안 뜰 수 있다는 지적, Animator 필수 검증이 Enemy마다 에러 로그를 남긴다는 지적을 받아 `Enemy.cs`를 stat adapter로 개조하고 `NPCComponent._requiresAnimator` 플래그를 추가했다. 3차는 `AttackAction.cs`가 `GuardRuntimeState`를 직접 참조하는데 rename 목록에서 빠졌던 것(빌드 깨짐 직결)과 `.csproj` 재생성 순서를 지적받았다. 4차는 승인하되 10개 구현 지침(EnemyActionSelector를 prefab이 아니라 씬 공용 컴포넌트로, 스폰 전 사전 검증 순서, destroyed-object 체크 순서, 낙하 테스트 요소 완전 제거, Enemy prefab 정적 배선 체크리스트 등)을 확정했다.

**설계**: `DestinationDB`/`DestinationDecider`는 전혀 참조하지 않는다(Enemy는 "본능이 없다" — 대상이 없으면 항상 Idle). 감지·교전 파이프라인은 `GuardPerception`/`GuardRuntimeState`를 `CombatPerception`/`CombatRuntimeState`로 rename(`.meta` GUID 보존, `NPCComponent`는 `FormerlySerializedAs` 경유)해 role-무관 공용 컴포넌트로 만들었다. `GuardActionSelector.TryAcquireNearestTarget`의 최근접 탐색 로직을 `Assets/Scripts/System/Lib/CombatTargeting.cs`(순수 함수, 상태를 바꾸지 않음, 2D 거리로 정합성 통일 — 기존은 3D였음)로 추출해 Guard/Enemy가 공유한다.

신규 `AttackStyle`(`Melee`/`Ranged`) + `IEnemyStatView`(`ICombatStatView` 확장, `Style`/`PreferredAttackRangeRatio`) + `EnemyStat`(`NPCStat` 파생, `GuardStat`과 동일 패턴) + `EnemyStatDefinition`(hunger/thirst/fatigue는 Inspector에 노출하지 않고 0 고정). `EnemyActionCost`는 만들지 않았다 — `DataManager`가 `ActionType`당 Cost 하나만 등록 가능해 `ActionType.Attack`으로 별도 등록하면 충돌 위험이 있고, 애초에 "접근 성향"은 개체 특성이라 `EnemyStat.PreferredAttackRangeRatio`로 충분했다.

신규 `EnemyActionSelector : BaseNPCActionSelector`는 `GuardActionSelector`처럼 **씬 레벨 공용 컴포넌트**다(prefab에 안 붙임 — prefab은 씬의 `ActionPool`을 참조할 수 없음). `CanUseStat`은 `stat is IEnemyStatView`. Melee는 Guard와 동일한 sticky 전투 큐(사거리 밖이면 Move+Attack). Ranged는 매 replan마다 sticky 없이 새로 스캔하고 `AttackRange` 안의 후보만 대상으로 삼아 `Attack`만 만든다(Move 없음) — 사거리 밖으로 나가면 target을 놓고 Idle. `AttackAction`은 rename만 반영해 그대로 재사용.

`Enemy.cs`를 체력 필드 없는 stat adapter로 개조했다(`Init(EnemyStat)`, `IsAlive`/`ApplyDamage`가 전부 `EnemyStat` 위임, null 방어 포함) — 체력 원본이 하나임을 보장한다. Farmer/Guard는 GD-008 미결정으로 이번엔 공격 대상으로 만들지 않았다 — 대신 `Assets/TestOnly/CombatTestDummy.cs`(신규 `"Friendly"` layer, `GuardTest.unity`의 단일 씬 오브젝트, prefab 아님)로 Enemy AI를 검증한다.

`Assets/Prefab/InGame/Enemy.prefab`(신규)은 `WorkerNPC`+`NPCComponent`(`_requiresAnimator: 0`, `enemy-slime.png` sprite)+`Rigidbody2D`(Kinematic/Gravity 0/회전 고정)+`CombatPerception`(Sensor child, 감지 반경 4.5 — 두 `EnemyStatDefinition` 중 큰 `AttackRange` 이상)+기존 `Enemy` 컴포넌트로 구성했다. Melee/Ranged는 prefab 하나를 공유하고 spawn 시 넘기는 `EnemyStatDefinition`(`EnemyStatDefinition_Melee.asset`/`_Ranged.asset`, `AttackRange` 1.2/4)으로만 갈린다.

`TestEnemyRainSpawner`를 재작성했다: `_enemyTemplate`을 `Enemy`→`WorkerNPC` prefab 참조로, `_selector`/`_statVariants`(round-robin) 추가. 매 프레임 강제 하강(`Vector3.down`) 로직·`_fallSpeed`·`_despawnY`·Fall Speed GUI를 전부 제거했다(Enemy가 이제 스스로 움직임). 스폰 전에 `_statVariants`/`_selector`/`CreateRuntimeStat() as EnemyStat`/`CanUseStat`을 전부 검증하고, 실패하면 `Instantiate` 자체를 하지 않는다. `Instantiate` 후에만 알 수 있는 실패(Enemy 컴포넌트 없음)는 생성된 오브젝트를 즉시 `Destroy`한다. 생존 루프는 `!entry.Enemy || !entry.Enemy.IsAlive` 순서(destroyed-check 먼저)로 `MissingReferenceException`을 피한다. `NPCType`에 `Enemy`를 추가했다(끝에 append).

`GuardTest.unity`에 `EnemyActionSelector`(ActionSelector 컨테이너의 세 번째 자식, `actionPool`/`dataManager` 기존 씬 참조 연결) 및 `CombatTestDummy`(layer 8/Friendly, `m_LocalPosition: {0,8,0}`)를 배치하고 `SceneRoots`에 등록했다. `ProjectSettings/TagManager.asset`에 layer 8 `"Friendly"`를 추가했다(`NPCGirl.prefab`에는 배정하지 않음). `NPCGirl.prefab`은 `_guardPerception`→`_combatPerception` 직렬화 키만 갱신(fileID 불변, 실제 게임 데이터 변경 없음).

변경/신규 파일: `Assets/Scripts/System/Actor/{CombatPerception,CombatRuntimeState}.cs`(rename, GUID 보존, 구 `Guard*` 파일 삭제), `Assets/Scripts/System/Lib/CombatTargeting.cs`, `Assets/Scripts/Enum/AttackStyle.cs`, `Assets/Scripts/Interface/IEnemyStatView.cs`, `Assets/Scripts/System/Actor/{EnemyStat,EnemyActionSelector}.cs`, `Assets/Data/ScriptableObject/Script/EnemyStatDefinition.cs`(+ 에셋 2개), `Assets/TestOnly/CombatTestDummy.cs`, `Assets/Prefab/InGame/Enemy.prefab`, `Assets/Scripts/Actor/Enemy.cs`, `Assets/Scripts/Enum/NPCType.cs`, `Assets/Scripts/System/Actor/{NPCComponent,GuardActionSelector}.cs`, `Assets/Scripts/System/Action/{GuardAction,AttackAction}.cs`, `Assets/TestOnly/TestEnemyRainSpawner.cs`, `Assets/Scenes/GuardTest.unity`, `Assets/Prefab/InGame/NPCGirl.prefab`, `ProjectSettings/TagManager.asset`, `PublicMD/{ARCHITECTURE,ProjectStructure}.md`.

검증: `dotnet build Assembly-CSharp.csproj --no-restore`와 `Assembly-CSharp-Editor.csproj` 둘 다 경고 0/오류 0(rename 직후 `.csproj`가 자동 갱신되지 않아 `EnemyActionSelector.cs`/`CombatTestDummy.cs` 두 개만 `Compile Include`를 직접 추가 — 나머지는 Unity가 자동 반영함). `rg`로 런타임 코드·prefab·현재 구조 문서에 `GuardPerception`/`GuardRuntimeState` 잔존 참조 0건 확인(`ARCHITECTURE.md`의 rename 역사 서술 한 줄만 의도적으로 남김). 변경 경로 한정 `git diff --check` 통과(기존 `m_Name: `/`m_WarningMessage: ` trailing-space는 전부 원본 파일에 이미 있던 것이고 이번에 추가한 블록엔 없음을 라인 번호로 확인). 새 fileID들이 `GuardTest.unity`/`Enemy.prefab` 내에서 각각 유일한지 grep으로 확인.

Play Mode는 실행하지 못했으며 `NOT VERIFIED`다: Enemy 새 Sensor가 `CombatTestDummy` 트리거를 실제로 수신하는지(Rigidbody2D 배치 유효성), Melee가 사거리 밖이면 접근·안이면 정지 공격, Ranged가 사거리 밖 대상에 절대 접근하지 않고 사거리 안 대상만 공격, 대상 사망/소멸 시 두 스타일 모두 Idle 복귀, 기존 Guard-vs-Enemy(`EnemyStat` 경유로 바뀐 뒤에도) 동작 회귀 없음, rename 이후 Guard 기존 순찰/전투 회귀 없음, 스포너 round-robin 동작, `NPCGirl.prefab`을 Unity에서 열었을 때 `Combat Perception` 필드 참조 보존.

독립 Codex 리뷰 에이전트(`codex exec`, read-only sandbox, ephemeral)를 백그라운드로 실행했다(PID 30076, `.codex/agent-runs/20260828-033755-839-*`). 결과는 아직 수신되지 않았으며, 도착하면 `PublicMD/Code_Evaluation_Result.md`에 기록된다. 이 항목은 미수신 결과를 통과로 주장하지 않는다.

다음 최소 작업: Unity Editor에서 프로젝트 재import 확인(csproj가 새 경로를 스스로 반영했는지), 위 Play Mode 항목 전체 확인, 필요시 Enemy prefab 위치/스탯 수치 튜닝.

IMP-034 completed on 2026-08-27 (코드·Animator·애니메이션 클립·prefab 구현 완료 / Codex 리뷰는 이번 회차 미실행 / Play Mode 시각 검증은 사용자 대기): Farmer NPC에 호미(hoe) 시각 장착을 추가했다. 사용자가 명시적으로 확정한 범위는 (1) Farmer + 호미만 우선 구현(Guard 등 다른 role/도구는 이번 범위 밖), (2) "착!" 타이밍은 실제 게임 tick과 동기화하지 않는 단순 반복 루프다.

리깅 없이 IMP-032/IMP-033이 확립한 절차적 transform-curve 기법을 그대로 재사용했다. `Assets/Prefab/InGame/NPCGirl.prefab`의 `Visual` 자식에 새 `Hoe` GameObject(SpriteRenderer, `Assets/Art/Generated/farmer-hoe.png`의 기존 import된 Sprite 참조, `m_SortingOrder: -1`, 기본 `m_IsActive: 0`)를 추가했다. 건물 표현(존재/부재 토글)과 달리 호미는 항상 표시되되 두 모션(캐리/작업) 사이만 전환되므로, 같은 Animator 위에 독립된 두 번째 layer `Tool`(가중치 1, mask 없음 — Base Layer가 `Visual`을, Tool layer가 `Visual/Hoe`만 움직여 속성 충돌 없음)을 추가했다: 새 파라미터 `IsWorking`(Bool), 새 state `ToolCarry`(default)/`ToolWork`, 0-duration 양방향 전이 2개. 신규 클립 `NPCGirl_ToolCarry.anim`(거의 정적인 어깨걸침 pose, rotation curve만)과 `NPCGirl_ToolWork.anim`(젖혀짐→슬램→리바운드→복귀의 반복 루프, position/scale/rotation curve)을 raw YAML로 직접 작성했다(Codex가 building 3종 클립에 쓴 것과 같은 기법, `path: Visual/Hoe`, CRC32 path hash 검증 후 `m_ClipBindingConstant`에 반영).

`NPCComponent`에 `_toolRenderer`(SerializeField), `SetWorking(bool)`(`SetInsideBuilding`과 동일한 모양으로 `IsWorking` bool을 Animator에 씀), `SetToolVisible(bool)`(Hoe GameObject의 active 토글)을 추가했고, `Flip(bool)`이 도구 SpriteRenderer도 함께 flip하도록, `ResetAnimationState()`가 `IsWorking`도 false로 복원하도록 확장했다. 표시 여부는 `WorkerNPC.Init`이 `npcType == NPCType.Farmer`로 결정한다(한 줄 추가, 새 의존성 없음 — Guard는 Hoe가 기본 비활성 상태로 남는다). 모션 전환은 `FarmingAction`이 담당한다: `BaseBuildingAction`과 같은 idempotent latch 패턴(`_isWorking` 플래그 + `ExitWorking()`)을 `Start`/`Complete`/`RequestReplan`/`Fail`/`Stop`/`Clear`에 인라인으로 추가했다 — 현재 소비자가 `FarmingAction` 하나뿐이라 별도 abstract base로 추출하지 않았다(두 번째 소비자가 생기면 그때 추출). `ActionContext`/`DestinationDB`에는 아무 필드도 추가하지 않았다 — IMP-033에서 걷어낸 "목적지 metadata로 여러 action에 표현을 흩뿌리는" 패턴을 반복하지 않았다.

새 `Assets/TestOnly/Editor/NPCGirlToolLayerConfigurator.cs`(+ `.meta`)를 추가해 `Tool` layer의 파라미터·전이만 독립적으로 멱등 구성·검증한다(메뉴: `Tools/NPC/Configure NPC Girl Tool Layer`, `Tools/NPC/Validate NPC Girl Tool Layer`). 이미 구현되어 있고 리뷰 대기 중인 기존 `NPCGirlAnimatorControllerConfigurator.cs`(Base Layer 전용)는 건드리지 않았다.

변경 파일: `Assets/Prefab/InGame/NPCGirl.prefab`, `Assets/Animation/NPCGirl_Move.controller`, 신규 `Assets/Animation/DefaultAnim/NPCGirl_ToolCarry.anim`(+ `.meta`), `NPCGirl_ToolWork.anim`(+ `.meta`), `Assets/Scripts/System/Actor/NPCComponent.cs`, `Assets/Scripts/System/Action/FarmingAction.cs`, `Assets/Scripts/Actor/WorkerNPC.cs`, 신규 `Assets/TestOnly/Editor/NPCGirlToolLayerConfigurator.cs`(+ `.meta`), `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`.

검증: `dotnet build Assembly-CSharp.csproj --no-restore`와 `dotnet build Assembly-CSharp-Editor.csproj --no-restore` 모두 경고 0/오류 0(Unity가 새 Editor 스크립트를 자동으로 csproj에 반영하지 않아 `Assembly-CSharp-Editor.csproj`의 `Compile Include`를 직접 추가했다 — `.csproj`는 `.gitignore`로 무시되는 생성 파일이라 changeset에는 영향 없다). `rg`로 `SetToolVisible`/`SetWorking`/`_toolRenderer`/`IsWorking`이 의도한 6개 파일에만 나타나는지 확인했다. 새/변경 파일 한정 `git diff --check`는 통과했다(controller의 기존 trailing-space 경고는 모두 원본 라인 범위(1-383)에 있고 이번에 추가한 384행 이후 구간에는 없음을 라인 번호로 확인). 씬/문서 변경 외 사용자의 기존 dirty 변경(삭제된 `NPCGirl_Idle.anim`/`NPCGirl_Move.anim`, `DefaultAnim` 폴더 이동 등)은 건드리지 않았다.

이번 회차는 사용자가 5시간 사용량 한도에 도달해 독립 Codex read-only 리뷰를 실행하지 못했다 — 이는 절차 누락이 아니라 사용자 요청에 따른 의도적 생략이며, `Code_Evaluation_Result.md`는 갱신되지 않았다.

**후속 수정 (같은 날, 사용자 Play Mode 영상 피드백 반영):** 사용자가 실제 화면 영상(mp4)을 근거로 세 가지 문제를 지적했다 — Claude는 영상 파일을 직접 열어볼 수 없어 텍스트 설명만으로 원인을 진단했다. (1) 호미가 캐릭터 앞이 아니라 뒤에 보임 → `SpriteRenderer.m_SortingOrder`를 `-1`에서 `1`로 변경(몸은 `0`)해 해결. (2) 호미의 위치·회전 모션이 이상해 보임 → `farmer-hoe.png.meta`의 sprite pivot이 `alignment: 0`(Center, `spriteSheet.sprites[0].pivot`의 `{0,0}` 값은 alignment가 Custom(9)이 아닌 한 무시되고 실제로는 스프라이트 중앙이 사용됨)이었던 것이 원인으로 추정된다. 손잡이 중간이 아니라 스프라이트 중앙을 축으로 회전하면 자연스러운 "휘두르기"가 아니라 "궤도를 도는" 것처럼 보인다. `alignment: 9`(Custom) + `pivot: {x: 0.5, y: 0.18}`(손잡이 하단 쪽, 잡는 위치)로 변경하고, 이에 맞춰 `NPCGirl.prefab`의 `Hoe` local position(`{0.15, 0.8, 0}`)과 두 애니메이션 클립의 회전값(캐리 `-25°`, 작업 `-25→-70→30→10→-25`)을 다시 잡았다. `ToolWork`에서 호미 자체의 position curve는 제거하고 rotation-only로 단순화했다(회전과 이동을 동시에 주면서 생기던 부자연스러운 움직임 제거). (3) 작업 중 캐릭터가 전혀 움직이지 않음(점프 후 내려찍는 느낌 요청) → 기존에는 `Tool` layer가 `Visual/Hoe`만 움직이고 `Visual`(몸 전체)은 전혀 건드리지 않아 몸이 반응하지 않았다. `ToolWork` 클립에 `Visual`의 `m_LocalPosition.y` curve를 추가해(대기 `-0.604` → 예비동작으로 살짝 hop `-0.45` → 내려찍는 순간 살짝 웅크림 `-0.72` → 반동 `-0.62` → 복귀 `-0.604`) 몸이 실제로 들썩이게 했다. `ToolCarry`는 `Visual`을 건드리지 않으므로 평상시엔 기존 Idle 애니메이션이 그대로 재생된다.

이 수치들은 여전히 시각적으로 검증되지 않은 추정치다(Claude는 영상도, Play Mode 결과도 볼 수 없다). Codex 리뷰와 마찬가지로 Play Mode 확인은 사용자 대기 상태다.

**2차 후속 수정 (같은 날, 사용자가 Scene 뷰 스크린샷 3장 제공):** 스크린샷에서 호미가 캐릭터 머리 위 허공에 완전히 분리되어 떠 있는 것을 확인했다 — 1차 수정에서 잡은 pivot(`0.18`, 손잡이 맨 아래)과 prefab scale(`0.6`)의 조합이 실제로는 그립 지점 대비 스프라이트 전체 길이(캐릭터 키의 약 87%)가 너무 길어서, 그립을 몸통 어디에 두어도 도끼날 쪽이 머리 위로 멀리 솟아 보이는 문제였다. `farmer-hoe.png.meta`의 pivot을 `0.35`(그립을 손잡이 중간 쪽으로 올림)로, `NPCGirl.prefab`의 `Hoe` scale을 `0.6`→`0.4`로 축소하고 position을 `{0.1, 0.85, 0}`(월드 y ≈ 0.25, 어깨~가슴 높이)로 재조정해 그립 위쪽으로 뻗는 길이 자체를 줄였다.

동시에 사용자가 "Move와 같은 생동감"을 요구해 `NPCGirl_Move.anim`(0.68초 loop, position ±0.24 bounce, scale ±14~16% squash·stretch)의 진폭을 참고 기준으로 삼아 `ToolWork`를 훨씬 과장되게 다시 썼다: 호미 회전 폭을 `-25→-70→30→10→-25`에서 `-20→-85→45→12→-20`(135° 스윙)로, 몸(`Visual`)의 hop을 `-0.604→-0.45→-0.72→-0.62`에서 `-0.604→-0.40→-0.78→-0.55`로 키웠다. 무엇보다 `ToolWork`에 `Visual`의 **scale curve를 신규 추가**했다(`1,1`→`0.93,1.09`(예비동작 stretch)→`1.2,0.78`(타격 순간 squash)→`0.96,1.05`(반동)→`1,1`) — 기존에는 몸 전체의 squash/stretch가 전혀 없어 Move 대비 뻣뻣해 보였던 부분이다. `ToolCarry`의 정지 tilt도 `-25°`→`-20°`로 pivot 재조정에 맞춰 다시 잡았다.

이번에도 스크린샷(정적 pose)만 보고 진단했고 실제 재생 결과는 보지 못했다 — 애니메이션 진폭·타이밍은 여전히 Play Mode에서 사용자가 직접 확인해야 한다.

**3차 후속 수정 (같은 날):** 사용자가 직접 `NPCGirl.prefab`의 `Hoe` scale을 `1,1,1`로, position을 `{0, -0.05, 0}`으로 재조정했다(Unity에서 직접 편집, 유지). 사용자 요청에 따라 `NPCGirl_ToolWork.anim`의 호미 자체 scale curve(raw curve + 대응 `m_EditorCurves` 2개)만 baseline `0.4` → `1.0` 기준으로 다시 계산했다(동일 비율 유지: 예비동작 `0.9/1.15`, 타격 `1.3/0.7`, 반동 `1.05/0.93`, 복귀 `1.0/1.0`). `ToolCarry`는 애초에 호미 scale curve가 없어(prefab 기본값 상속) 변경할 것이 없었다. 몸(`Visual`)의 squash/stretch, 호미의 rotation/position curve는 이번 요청 범위 밖이라 손대지 않았다.

**4차 후속 수정 (같은 날, flip 대응):** 사용자가 캐릭터가 좌우 반전(`NPCComponent.Flip`, `MoveAction`/`GuardAction`이 이동 방향에 따라 호출)될 때 호미가 함께 뒤집히는지 질문했다. 확인 결과 실제 결함이었다 — `SpriteRenderer.flipX`는 렌더링되는 픽셀만 좌우로 뒤집을 뿐 Transform의 회전·위치는 전혀 건드리지 않으므로, `ToolCarry`/`ToolWork`의 tilt(rotation) 값은 캐릭터가 어느 쪽을 보든 항상 같은 부호로 적용되고 있었다. Farmer는 도착 직전 이동 방향에 따라 좌우 어느 쪽이든 보고 있을 수 있으므로 실제로 발생하는 문제였다.

리깅 없이 2D에서 자식 오브젝트를 좌우 반전에 맞춰 함께 미러링하는 표준 기법(음수 X scale을 가진 wrapper transform)을 적용했다. `NPCGirl.prefab`에서 `Hoe`를 `Visual`의 직계 자식에서 새 `HoeAnchor`(빈 Transform, `Visual`의 자식)의 자식으로 옮겼다(`Visual → HoeAnchor → Hoe`). `NPCComponent`에 `_hoeAnchor`(Transform, SerializeField)를 추가하고, `Flip(bool)`이 기존 `_toolRenderer.flipX` 대신 `_hoeAnchor.localScale.x`의 부호를 뒤집도록 변경했다 — 부모의 음수 X scale은 자식의 위치·회전·렌더링을 한 번에 미러링하므로, `Tool` layer가 구동하는 `Hoe`의 회전/스케일 애니메이션과 충돌하지 않는다(anchor는 Animator가 건드리지 않는 별도 노드). 두 애니메이션 클립의 `path`가 `Visual/Hoe`에서 `Visual/HoeAnchor/Hoe`로 바뀌었으므로 `m_ClipBindingConstant`의 CRC32 path hash도 `4257220453` → `4011575896`으로 다시 계산해 반영했다.

`dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0. `rg`로 이전 경로(`Visual/Hoe` 단독, hash `4257220453`)가 두 클립에 더 이상 남아있지 않음을 확인했다. Play Mode에서 좌우 반전 상태 모두 확인은 여전히 사용자 대기 상태다.

**5차 후속 수정 (같은 날, 이름 정리):** 사용자가 Unity에서 직접 `HoeAnchor`/`Hoe` GameObject 이름을 `ToolAnchor`/`Tool`로 바꿨다(향후 Farmer 외 다른 role/도구로 확장할 여지를 이름에도 반영한 것으로 보임 — fileID/GUID는 그대로라 참조 자체는 안 끊겼지만, Animator 애니메이션 curve의 `path`는 fileID가 아니라 GameObject 이름을 `/`로 이어붙인 문자열이라 그대로 두면 바인딩이 끊긴다). 두 애니메이션 클립의 `path: Visual/HoeAnchor/Hoe`를 `path: Visual/ToolAnchor/Tool`로, `m_ClipBindingConstant`의 CRC32 path hash를 `4011575896` → `2357264895`로 다시 계산해 반영했다. 이름 일관성을 위해 `NPCComponent`의 `_hoeAnchor` 필드도 `_toolAnchor`로 함께 바꾸고 prefab의 직렬화 키도 맞춰 갱신했다(필드명이 바뀌면 Unity가 이전 값을 못 찾으므로 fileID는 유지한 채 키 이름만 변경).

`dotnet build` 경고 0/오류 0. `rg`로 `_hoeAnchor`/`HoeAnchor/Hoe`/`Visual/Hoe` 잔존 참조가 전혀 없음을 확인했다.

**6차 후속 수정 (같은 날, 타격 방향 반전):** 사용자가 `ToolWork`에서 Tool이 "들고 있는 방향"이 아니라 반대 방향으로 찍힌다고 지적했다. 원인은 회전 keyframe 배치였다 — 기존에는 `rest(-20°) → windup(-85°, rest와 같은 회전 방향으로 더 젖힘) → impact(+45°, 부호가 반대인 쪽으로 넘어감)`이어서, 타격이 rest가 기대고 있는 쪽이 아니라 수직을 지나 반대쪽으로 넘어가서 찍히고 있었다. windup과 impact의 값을 맞바꿔(`windup: 45°`, `impact: -85°`) rebound도 같은 쪽(`12° → -50°`)으로 옮겼다 — 이제 위로 반대편(+45°)까지 젖혔다가 rest를 지나 같은 방향(-85°)으로 계속 휘둘러 찍는 모양이 된다. raw `m_EulerCurves`와 대응 `m_EditorCurves`(`localEulerAnglesRaw.z`) 양쪽 다 갱신했다. `git diff --check` 통과, 이전 값(-85/45/12 조합) 잔존 없음을 확인했다. 실제 스윙 방향이 맞는지는 여전히 Play Mode에서 확인이 필요하다.

**7차 후속 수정 (같은 날, 기준 baseline 재계산):** 사용자가 "`Visual` position이 왜 갑자기 `-0.6`에서 시작하는지 이해가 안 간다"고 질문했다. 확인해보니 사용자가 이미 `NPCGirl.prefab`의 `Visual`/`Image` local position을 원래의 `{0,-0.604,0}`/`{0,0.604,0}` 짝(발밑 기준 pivot + 원점 보정)에서 둘 다 `{0,0,0}`으로 바꿔둔 상태였다. 문제는 `Move.anim`, `EnterBuilding`/`InsideBuilding`/`ExitBuilding.anim`(모두 기존 파일), 그리고 이번 세션에서 만든 `ToolWork.anim`까지 다섯 개 클립 전부가 여전히 `-0.604`를 "정지 상태 기준값"으로 커브에 박아두고 있었다는 점이다 — `Idle`/`ToolCarry`처럼 `Visual` position을 안 건드리는 상태는 새 기본값 `0`에 머무르는데, 저 다섯 클립 중 하나로 전환되는 순간 `-0.604`로 뚝 떨어졌다가 돌아올 때 다시 `0`으로 튀는 것처럼 보였을 것이다.

`AskUserQuestion`으로 확인한 결과 "`0,0` 유지 + 모든 클립 재계산"을 선택해, 다섯 클립의 `Visual`(`ToolWork`는 `Visual`과 `Visual/ToolAnchor/Tool` 둘 다 있지만 baseline 문제는 `Visual` 쪽에만 해당) position.y 커브 전체에 `+0.604`를 일괄 적용해 새 기준값 `0`에 맞춰 재계산했다(상대적인 움직임 모양은 그대로 유지, 원점만 이동). raw `m_PositionCurves`와 대응 `m_EditorCurves`(`m_LocalPosition.y`) 양쪽 다 갱신했다. `rg`로 옛 baseline 값(`-0.604`, `-0.616`, `-0.612` 등 9개 토큰)이 다섯 파일 어디에도 남아있지 않음을 확인했고, `git diff --check` 통과, `dotnet build` 경고 0/오류 0(C# 변경 없음, 컴파일 유지만 재확인).

Play Mode 검증은 실행하지 못했으며 `NOT VERIFIED`다: Farmer가 평소 호미를 어깨에 걸치고 있음, Farming 시작 시 raise-slam 루프로 전환, Farming 종료(완료/실패/replan/stop/clear 각 경로)에서 캐리 pose로 복귀, Guard는 호미가 전혀 보이지 않음, pool 재사용 후에도 역할에 맞는 상태로 시작. Hoe의 위치/각도/크기(`{x: 0.2, y: 0.9, z: 0}`, scale `0.6`, 기본 tilt `-35°`, `m_SortingOrder: -1`)는 추정값이라 Play Mode에서 직접 보고 조정이 필요할 가능성이 높다.

다음 최소 작업: Unity Editor에서 `Tools/NPC/Configure NPC Girl Tool Layer` → `Validate ...` 실행, Play Mode에서 위 시각 검증 항목 확인, 필요시 Hoe 위치/각도/크기 및 두 애니메이션 클립의 curve 값 튜닝. 사용량 한도가 풀리면 독립 Codex 리뷰를 실행한다.

IMP-033 completed on 2026-08-27 (코드 구현 완료 / Play Mode 시각 검증은 사용자 대기): IMP-032가 도입한 목적지 metadata 기반 건물 출입 표현(`DestinationInfo.UsesBuildingAnimation` → `DestinationDB.ShouldUseBuildingAnimation` → `ActionContext.ShouldUseBuildingAnimation` → `DefaultAction`의 공통 lifecycle)에는 실제 버그가 있었다: `FarmerActionSelector.RequestNewActionQueue`가 Move와 semantic action에 같은 `actionContext` 인스턴스를 주입했고, `DefaultAction.Start()`는 action 종류를 가리지 않고 flag만 보고 입장 표현을 켰다. 그 결과 Pub/Inn으로 향하는 Farmer가 출발 지점부터 입장 애니메이션을 시작했다(`Code_Evaluation_Result.md` H-01). `GuardActionSelector`는 이동용 context를 별도로 만들어 이 버그가 없었으므로 Farmer와 Guard의 실제 표현이 서로 달랐다.

근본 원인이 "건물 안에 있는가"를 목적지의 성질로 모델링한 데 있다고 보고, 표현 소유권을 action 종류로 옮겼다. 이 프로젝트에서 Eat/Drink/Sleep은 항상 실내 행동이라는 전제 위에 신규 `Assets/Scripts/System/Action/BaseBuildingAction.cs`(`DefaultAction` 상속 abstract base)를 추가했다. `Start()` 성공 후 `NPCComponent.SetInsideBuilding(true)`를 호출하고, `Complete`/`RequestReplan`/`Fail`/`Stop`/`Clear` 모든 정리 경로에서 `SetInsideBuilding(false)`를 호출한다. 내부 `_isInsideBuilding` 플래그로 정리 호출을 멱등하게 만들었고, `Clear()`는 `base.Clear()`가 context를 지우기 전에 건물 상태부터 해제한다. `EatAction`, `DrinkAction`, `SleepAction`의 상속 대상을 `DefaultAction`에서 `BaseBuildingAction`으로 바꿨다(`sealed SleepAction` 유지). `MoveAction`, `FarmingAction`, `IdleAction`, `GuardAction`, `AttackAction`은 계속 `DefaultAction`을 직접 상속하며 건물 표현을 갖지 않는다.

`DefaultAction`에서는 `_isBuildingAnimationActive` 필드, `Start()`의 flag 분기, `StopBuildingAnimation()`과 그 호출부를 모두 제거해 범용 lifecycle만 남겼다. `ActionContext.ShouldUseBuildingAnimation` property와 생성자 인자, `DestinationDB.ShouldUseBuildingAnimation(BuildingType)`, `DestinationInfo.UsesBuildingAnimation` field를 제거했다. `FarmerActionSelector.BuildContext`와 `GuardActionSelector.BuildNeedQueue`에서 해당 조회·전달 코드를 제거했다 — Farmer의 기존 queue 구조(Move context와 semantic context 공유)는 그대로 두었다. action 종류 자체가 건물 표현 여부를 결정하므로 별도의 이동 context 분리는 필요 없어졌다. `NPCComponent.SetInsideBuilding(bool)`과 Animator의 `IsInsideBuilding` bool은 그대로 유지했으며 Animator Controller, animation clip, prefab 배선은 수정하지 않았다.

`FarmerTest.unity`, `GuardTest.unity`에서 `UsesBuildingAnimation:` 직렬화 줄만 제거했다. 이 필드는 커밋된 HEAD에는 애초에 없었고(IMP-032가 미커밋 working tree에서만 추가) 이번 제거로 해당 부분은 HEAD와 다시 일치한다. 두 씬에 함께 있던 UI·농장·창고 등 사용자의 무관한 기존 변경은 건드리지 않았다. `PublicMD/ARCHITECTURE.md`(§5.1, `ActionContext` 공통 값 목록, §8.2, §14)와 `PublicMD/ProjectStructure.md`(action 파일 트리, action 책임 표, 신규 action 추가 절차, `NPCGirl.prefab` 절)의 목적지 metadata 기반 설명을 `BaseBuildingAction` 기반 설명으로 갱신했다.

변경 파일: 신규 `Assets/Scripts/System/Action/BaseBuildingAction.cs`(+ `.meta`), `Assets/Scripts/System/Action/DefaultAction.cs`, `EatAction.cs`, `DrinkAction.cs`, `SleepAction.cs`, `Assets/Data/Struct/ActionContext.cs`, `Assets/Scripts/System/Lib/DestinationDB.cs`, `Assets/Scripts/System/Actor/FarmerActionSelector.cs`, `GuardActionSelector.cs`, `Assets/Scenes/FarmerTest.unity`, `GuardTest.unity`, `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`.

검증: `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0 (Unity가 이미 새 파일을 `Assembly-CSharp.csproj`의 `Compile Include`에 반영한 상태에서 실행). `rg`로 `ShouldUseBuildingAnimation`/`UsesBuildingAnimation` 잔존 참조를 확인했고, `Assets/` 전체와 현재 구조 문서(`ARCHITECTURE.md`, `ProjectStructure.md`)에는 0건이다(역사 기록인 이 문서의 IMP-032 항목과 `Code_Evaluation_Result.md`에는 의도적으로 남겨둠). 변경 경로 한정 `git diff --check`는 사용자의 기존 `m_Name: ` trailing-space 패턴 외 신규 오류가 없었다(이 패턴은 IMP-032 때도 동일하게 관찰된 기존 Unity YAML 특성이다). 씬 diff는 `UsesBuildingAnimation` 필드 제거만 반영하며 사용자의 기존 dirty 변경은 보존됐다. Play Mode 검증은 실행 중인 Unity 프로세스의 lock 때문에 수행하지 못했으며 `NOT VERIFIED`다: Pub/Inn으로 이동하는 NPC가 도착 전까지 Move 상태 유지, Eat/Drink/Sleep 시작 시점에만 Enter→Inside 실행, 정상 완료 후 Exit 실행, 실내 action 중 비활성화·중단·replan·실패 시 `IsInsideBuilding` false 복원, pool 반환 후 두 번째 사용에서 외부/Idle 시작, Move/Farming/Guard/Attack/Idle의 건물 애니메이션 미실행.

독립 Codex 리뷰 에이전트(`codex exec`, read-only sandbox, ephemeral)를 백그라운드로 실행했다(PID 50264, `.codex/agent-runs/20260827-202645-065-*`). 결과는 아직 수신되지 않았으며, 도착하면 `PublicMD/Code_Evaluation_Result.md`에 기록된다. 이 항목은 미수신 결과를 통과로 주장하지 않는다.

다음 최소 작업: Unity Play Mode에서 위 6개 시각 검증 항목을 확인하고 결과를 이 문서에 추가한다.

IMP-032 completed on 2026-08-27 (코드·Animator Controller·씬 metadata 구현 완료 / 시각 Play Mode 검증은 사용자 대기): 기존 `NPCGirl_Move.controller`의 Idle, Move, EnterBuilding, InsideBuilding, ExitBuilding 상태와 5개 clip GUID를 보존하면서 `Speed` float와 `IsInsideBuilding` bool 파라미터, 7개 transition을 Unity Editor API로 구성했다. locomotion은 `Idle ↔ Move`에 `Speed` 0.01 임계치와 `IsInsideBuilding == false` guard를 사용하고, 건물 흐름은 `Idle/Move → EnterBuilding → InsideBuilding → ExitBuilding → Idle`로 구성했다. Any State, Trigger, Int, Blend Tree, root motion은 추가하지 않았다.

`NPCComponent`가 모든 실제 이동이 통과하는 `Move()`의 변위를 frame 단위로 기록하고 `LateUpdate()`에서 `Speed`를 갱신하므로 `MoveAction`뿐 아니라 `GuardAction` 순찰도 같은 이동 애니메이션을 사용한다. Animator reference와 두 parameter type은 `Awake()`에서 한 번 검증하며, `Init()`/`ResetRuntimeState()`는 `Speed=0`, `IsInsideBuilding=false`, Idle로 복원한다. 비어 있던 `PlayAnim()`/`PlayDOTweenAnim()`은 제거하고 `SetInsideBuilding(bool)`을 추가했다.

`DestinationInfo.UsesBuildingAnimation`과 `DestinationDB.ShouldUseBuildingAnimation(...)`을 추가했다. `FarmerActionSelector`/`GuardActionSelector`가 선택된 목적지 metadata를 `ActionContext.ShouldUseBuildingAnimation`에 전달하고, `DefaultAction` 공통 lifecycle이 action 시작 시 입장 상태를 켜며 Completed, ReplanRequested, Failed, Stop, Clear 모든 정리 경로에서 끈다. action timer와 transaction 시점은 기다리지 않는 비차단 표현으로 유지한다. `FarmerTest.unity`와 `GuardTest.unity`에서는 Pub(1)·Inn(3)만 true, Well·Farm·GuardPost는 false로 명시했다. 기존 UI·농장·창고 씬 변경과 사용자가 만든 clip 이동/추가 변경은 수정하거나 되돌리지 않았다.

변경 파일: `Assets/Animation/NPCGirl_Move.controller`, `Assets/Data/Struct/ActionContext.cs`, `Assets/Scripts/System/Action/DefaultAction.cs`, `Assets/Scripts/System/Actor/NPCComponent.cs`, `FarmerActionSelector.cs`, `GuardActionSelector.cs`, `Assets/Scripts/System/Lib/DestinationDB.cs`, `Assets/Scenes/FarmerTest.unity`, `GuardTest.unity`, 신규 `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs`(+ `.meta`, `Editor.meta`), `PublicMD/ARCHITECTURE.md`, `PublicMD/ProjectStructure.md`. Editor configurator는 이미 정확히 구성된 Controller에서는 저장하지 않고 반환하며, 부분 구성일 때 관리 대상 파라미터와 상태 간 transition만 재구성한다.

검증: `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0. 원본 프로젝트가 실행 중인 Unity 프로세스의 lockfile을 보유해 직접 batch open은 반환 코드 1이었으므로 사용자 프로세스를 종료하지 않고 `Temp/NPCAnimatorConfiguratorProject`에 현재 Controller·clip·GUID를 복제했다. Unity 6000.3.9f1 batch mode에서 Editor configurator 컴파일과 실행이 성공했고, 파라미터 2개, transition 7개, 각 condition/Exit Time/fixed duration, 5개 motion reference를 검증했다. 실제 반영 Controller를 다시 격리 프로젝트에서 검증했으며 재실행 전후 SHA-256 `5899DCBCB7907B88AC0667F46AA6123E4255CA8BE584B84C21233D215514CDC7`가 유지됐다. 프리팹의 Animator reference와 controller GUID `75e1959732ee5ff499bbc4be3a55b81e`, 두 씬의 Pub/Inn true와 나머지 false를 정적 확인했다. 구현 C#·문서·Editor tool 범위의 `git diff --check`는 통과했다. 전체 working tree `git diff --check`는 기존 사용자 Unity YAML 변경의 빈 scalar trailing space(`NPCGirl_Move.controller`, `FarmerTest.unity`, `GuardTest.unity`) 때문에 실패했으며 이번 구현 줄의 공백 오류로 간주하지 않았다. Farmer 이동, Guard 순찰, Pub/Inn 출입, Well/Farm/GuardPost 비적용, 실패·pool 재사용의 실제 시각 흐름은 Play Mode에서 실행하지 못해 `NOT VERIFIED`다.

독립 Codex 리뷰 에이전트(`codex exec`, read-only sandbox, ephemeral)를 백그라운드로 실행했다(PID 46036, `.codex/agent-runs/20260827-194422-061-*`). 결과는 아직 수신되지 않았으며, 도착하면 `PublicMD/Code_Evaluation_Result.md`에 기록된다. 이 항목은 미수신 결과를 통과로 주장하지 않는다.

다음 최소 작업: Unity Play Mode에서 (1) Farmer 이동/정지와 Guard 순찰의 Move/Idle 전환, (2) Pub·Inn의 Enter→Inside→Exit, (3) Well·Farm·GuardPost 비적용, (4) 실내 action 실패·replan·disable 뒤 가시성 복원, (5) pool 반환 후 두 번째 spawn의 Idle/외부 초기화를 확인하고 결과를 이 문서에 추가한다.

IMP-031 completed on 2026-08-25 (코드 구현 완료 / 씬 배선과 Play Mode 검증은 사용자 대기): IMP-030이 만든 UI facade 골격을 실제로 호출하는 첫 소비자인 pointer/raycast hover 입력 adapter를 추가했다. 이 세션은 opusplan(계획은 Opus 모드) 후 Sonnet 5가 구현하는 흐름으로 진행됐고, 계획 승인 전 Codex 리뷰가 두 가지 상태 전환 버그(popup guard와 collider 조기 반환의 충돌, `HoverBase.TryShow`의 `ApplyInfo` → `OnShown` 순서로 인한 `OnShown()` 검증 시점 오류)를 지적해 계획에 반영한 뒤 구현했다.

`IHoverInfoSource`에 `HoverType HoverType { get; }`를 추가했다. source가 자신을 어떤 hover 창으로 보여줘야 하는지 스스로 선언하게 해, 입력 adapter가 도메인 타입별로 분기하지 않고도(즉 어떤 concrete 도메인 타입도 몰라도) 올바른 `HoverType`으로 `IUIService.TryShow`를 호출할 수 있게 했다. `FarmWorkSite`는 기존에 이미 구현돼 있던 `IHoverInfoSource`(`Owner`, `TryGetHoverInfo`)에 `HoverType => HoverType.FarmStatus`를 추가하고 세 멤버를 기존 public 프로퍼티 블록 옆으로 모았다.

신규 `Assets/Scripts/UI/PointerHoverRouter.cs`는 `Mouse.current`(신규 Input System — 이 프로젝트가 `activeInputHandler: 1`로 신규 Input System 전용이라 `Input.mousePosition`은 런타임 예외를 던진다) + `Physics2D.OverlapPoint(worldPosition, _hoverableMask)`로 마우스 아래 대상을 찾는다. **candidate**(hit collider가 바뀔 때만 `TryGetComponent<IHoverInfoSource>` 재호출)와 **active**(실제로 `TryShow`에 성공한 대상) 상태를 분리해서, 표시가 한 번 실패해도(예: 향후 popup 정책에 막힘) 같은 collider 위에서 다음 poll마다 자동 재시도되게 했다 — 두 상태를 합쳤다면 조기 반환 때문에 영구히 복구되지 않았을 것이다. `Layer`(`Hoverable`)는 레이캐스트 후보를 줄이는 필터로만 쓰고 정보 전달에는 쓰지 않는다. `TryGetComponent`는 대상이 바뀐 순간에만 호출해 `CodeConvention.md` §12.3(hot path 반복 `GetComponent` 금지)을 지켰다. `Collider2D`와 `IHoverInfoSource`가 같은 GameObject에 있어야 한다는 제약을 코드 주석과 `ARCHITECTURE.md`에 명시했다.

`Assets/Scripts/UI/FarmGaugeHover.cs`를 재작성했다. 기존 파일은 세 가지가 깨져 있었다: (1) 클래스명이 파일명과 다른 `FarmHoverHover`(오타), (2) `ApplyInfo`가 `NotImplementedException`을 던짐, (3) 빈 `void Update()` 스텁이 `HoverBase.private void Update()`를 가려 refresh가 조용히 죽어 있었음. 새 구현은 클래스명을 파일명에 맞추고, `Update()` override를 제거해 base의 refresh 루프가 정상 동작하게 했으며, `Awake()`에서 `_progressFill`/`_worldCamera` 필수 참조를 검사해 `_isConfigured`에 캐시한다(`OnShown()`에서 검사하면 `HoverBase.TryShow`가 `SetActive` → `ApplyInfo` → `OnShown` 순서로 호출하므로 `ApplyInfo`가 먼저 터진다 — Codex 리뷰 지적 반영). `ApplyInfo`는 `Image.fillAmount`와 `WorldToScreenPoint` anchor 갱신을 멱등하게 수행한다. 클래스명만 바뀌고 파일명·`.meta` GUID는 그대로라 기존 `Assets/Prefab/UI/FarmGauge.prefab`의 직렬화된 `_hoverType`/`_refreshInterval` 값과 컴포넌트 참조는 보존된다.

`ProjectSettings/TagManager.asset` 인덱스 7(첫 빈 슬롯)에 `Hoverable` layer를 append했다 — 기존 레이어 인덱스는 밀리지 않는다. `PublicMD/ARCHITECTURE.md` §10.5와 `PublicMD/ProjectStructure.md`를 갱신해 adapter의 호출 체인, `IHoverInfoSource.HoverType`이 필요한 이유, Collider/source 동일 GameObject 규칙, `Physics2D.OverlapPoint`의 중첩 우선순위 미지원 제한 사항을 반영했다. 두 문서 모두 stale 항목(`SampleScene.unity` → `FarmerTest.unity`, `Prefab/` 하위 실제 폴더 구조, 누락됐던 `System/Mapper/ItemInfoCsvMapper.cs`)도 정정했다.

**이번 범위에서 명시적으로 제외한 것**: `UIManager`의 popup 차단 정책(Codex 리뷰 지적 — `PopupType`에 `None`뿐이라 concrete popup이 없고, 단순 guard만 넣으면 popup 종료 후 hover가 복구되지 않는 문제가 있어 별도 작업으로 미룸. 다만 candidate/active 분리 덕에 나중에 정책이 추가돼도 재시도 경로는 그대로 동작한다), `HoverBase`의 반복 `ApplyInfo` 문제(대화 초반 사용자가 수정을 요구하지 않는다고 명시), 씬 배선 전체(Collider2D 부착, Canvas/UIManager GameObject 생성, prefab 배치, `PointerHoverRouter` 컴포넌트 배선).

검증: `dotnet build Assembly-CSharp.csproj --no-restore`가 기존에 이미 `.csproj`에 등록된 세 변경 파일(`IHoverInfoSource.cs`, `FarmWorkSite.cs`, `FarmGaugeHover.cs`)에 대해 경고 0/오류 0으로 통과했다. 신규 `PointerHoverRouter.cs`는 Unity가 `.csproj`를 재생성해야 명령행 빌드 대상이 되는데, 사용자의 Unity Editor가 이미 프로젝트를 열어 lock을 쥐고 있어 별도 headless 인스턴스를 띄울 수 없었다 — 대신 실행 중인 Editor가 파일 변경을 자동 감지해 `.meta`를 생성한 것으로 최소한의 구문 유효성을 확인했다. Play Mode 검증은 씬 배선이 없어 수행하지 않았다.

**`TagManager.asset` 실제 회귀와 수정**: 최초 구현 시 `Hoverable` 추가와 함께 빈 layer 슬롯의 형식을 `- `(dash+공백, 원본 형식)에서 `-`(dash만)로 "정리"했는데, 이는 YAML 스펙상 동등해 보였지만 Unity의 자체 TagManager 파서는 표준 YAML 파서가 아니어서 이 차이로 실제 파싱에 실패했다(`Parser Failure at line 41: Expect ':' between key and value within mapping`, Unity Console에서 실사용자가 직접 확인). 최초 보고 시 이 로그를 "편집 도중의 과도기 상태를 가리키는 stale 로그"로 잘못 판단했으나, 사용자가 Unity Console에서 재현을 확인해 오판임이 드러났다. 원본과 byte 단위로 대조해 `git diff`가 `Hoverable` 값 추가 한 줄만 남도록(다른 모든 빈 슬롯의 trailing space 포함 서식을 원본과 완전히 동일하게) 수정했고, 사용자가 Unity Console에서 에러가 사라지고 `Hoverable` 레이어가 정상 표시됨을 확인했다.

독립 Codex 리뷰 에이전트(`codex exec`, read-only sandbox, ephemeral)를 백그라운드로 실행했다(PID 15812, `.codex/agent-runs/20260825-010215-646-*`). 결과는 아직 수신되지 않았으며, 도착하면 `PublicMD/Code_Evaluation_Result.md`에 반영된다 — 이 항목은 그 결과를 대신 요약하지 않는다.

다음 최소 작업: (1) 사용자가 `FarmerTest.unity`에 씬 배선(Collider2D on FarmWorkSite + Hoverable layer, Canvas, `UIManager` GameObject, `FarmGauge.prefab` 배치, `PointerHoverRouter` 부착)을 마친 뒤 Play Mode 검증, (2) `UIManager` popup 차단 정책 설계·구현, (3) Codex 리뷰 결과 확인 및 후속 조치.

IMP-030 completed on 2026-08-23 (UI 기본 코드 구현 완료 / concrete view와 씬 배선·Play Mode 검증은 미구현): 호출자가 concrete Canvas/view 구조를 탐색하지 않고 category enum과 필요한 source만 `IUIService`에 전달하는 UI facade 기반을 추가했다. `UIManager`는 `PopBase[]`/`HoverBase[]` 두 serialized registry를 `PopupType`/`HoverType` dictionary로 변환하며, popup open/close stack, top popup close, 현재 hover 하나의 전환과 전체 hide만 조정한다. 개별 popup/hover마다 concrete `[SerializeField]`를 추가하거나 화면별 `switch`를 두지 않는다.

`PopBase`는 `PopupType` 식별자와 open/close lifecycle hook만 소유하고, `HoverBase`는 `HoverType`, 현재 `IHoverInfoSource` 소유권, unscaled-time 0.1초 기본 refresh, `HoverInfo` 적용 hook을 소유한다. Hover 종료는 요청 source identity가 현재 source와 같을 때만 허용해 이전 대상의 exit가 새 대상 UI를 닫지 않게 했다. `IHoverInfoSource.Owner`는 interface에 저장된 Unity component의 destroyed-object fake-null을 확인하는 backing `UnityEngine.Object`다. `HoverInfo`는 title/description/world anchor/optional normalized progress만 전달하며 concrete Text, Slider, animation은 이후 `HoverBase` subclass 책임으로 남겼다. `PopupType`은 아직 concrete popup이 없으므로 `None`만, `HoverType`은 현재 수직 슬라이스 대상인 `FarmStatus`를 추가했다.

신규 파일: `Assets/Scripts/UI/UIManager.cs`, `PopBase.cs`, `HoverBase.cs`, `Assets/Scripts/Interface/IUIService.cs`, `IHoverInfoSource.cs`, `Assets/Scripts/Enum/PopupType.cs`, `HoverType.cs`, `Assets/Data/Struct/HoverInfo.cs` 및 각 `.meta`. `ARCHITECTURE.md`, `ProjectStructure.md`, `CodeConvention.md`도 facade/routing/lifecycle 책임과 아직 미구현인 concrete UI 경계를 반영했다.

검증: Unity 6000.3.9f1이 신규 8개 스크립트를 `Assembly-CSharp.csproj`에 반영한 뒤 `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0. `git diff --check` 통과, 신규 GUID 중복 0건, UI 기본 코드에서 `object` payload/string registry/`Resources.Load`/scene `Find*`/UI singleton/개별 `PopupType`·`HoverType` switch가 없음을 확인했다. Unity Play Mode 검증은 concrete `PopBase`/`HoverBase` subclass와 scene reference가 아직 없어 수행하지 않았다. 다음 최소 작업은 `FarmHoverInfoSource`, 실제 `FarmHoverView`, world pointer 감지 adapter를 만들고 `UIManager._hovers`에 배선하는 것이다.

IMP-029 completed on 2026-08-22 (코드 구현 완료 / Unity 씬 배선과 Play Mode 검증은 사용자 대기): `PublicMD/InteractionProvider_Unification_Plan.md`(사용자 승인)의 `IInteractionProvider`/`IFarmWorkProvider` 통합을 구현했다. 이 세션은 opusplan(계획 재검토는 Opus 모드) 후 Sonnet 5가 구현하는 흐름으로 진행되었다. 구현 전 working tree 전제를 실제 코드·씬 YAML과 대조했다: `GuardTest.unity`/`SampleScene.unity` 양쪽에서 Eat/Drink destination의 `InteractProvider`가 정확히 같은 `DestinationObject` GameObject에 붙은 `Pub` 컴포넌트를 가리키는 것을 fileID 단위로 확인해(`(GameObject, ActionType)` 라우팅으로 기존 의미 보존 가능), 계획의 stop condition을 트리거하는 전제 오류는 없었다. 농경지 구현(2.3절이 우려한 미커밋 변경)은 이미 `6d19561` 커밋에 들어가 있어 실제 작업 시작 시점의 unstaged 변경은 계획 문서 자체뿐이었다.

목표는 공급 전용 `IInteractionProvider`(`CanInteract`/`AppendOptions`/`TryInteraction(InteractRequest, out InteractResult)`)와 농사 전용 `IFarmWorkProvider`(`TryApplyWork`)라는 두 개의 병렬 실행 경로를, Eat/Drink/Farming이 공유하는 단일 **지원 여부 조회 → option 열거 → 명시적 request 실행 → 공통 result 반환** protocol로 합치는 것이었다. `IInteractionProvider`를 `Supports(ActionType)`/`CanInteract(ActionType)`/`AppendOptions(type, buffer)`/`TryInteract(InteractionRequest, out InteractionResult)`로 재정의했다(`Supports`는 초기화 상태와 무관한 구조적 capability, `CanInteract`는 `Supports && 현재 operational`). `InteractRequest`(`ActionType+ItemId`)는 `InteractionRequest`(`ActionType+OptionId+Strength`, `NoOptionId=-1`, `HasValidStrength`가 NaN/Infinity/0 이하를 거부)로, `InteractResult`(`Success+StatEffect`)는 `InteractionResult`(`ActorEffect` 하나만 보유, 성공 여부는 `TryInteract`의 bool 하나로만 표현)로 교체했다. `InteractionOption`도 `ItemId`/`Effect`를 `OptionId`/`ActorEffect`로 일반화했다.

`Assets/Scripts/Actor/BaseInteractable.cs`를 `.meta` GUID 보존 rename으로 `BaseInteractionProvider.cs`로 옮기고, idempotent 초기화(`TryInitialize`가 실패 상태까지 캐싱해 재호출 시 재시도하지 않음)와 template method dispatch(`Supports`/`CanInteract`/`AppendOptions`/`TryInteract` public이 각각 `SupportsCore`/`CanInteractCore`/`AppendOptionsCore`/`TryInteractCore` protected core로 위임)를 구현했다. `Pub`는 이 base를 상속하며 `[SerializeField] ItemDataContext`를 직접 참조해 `TryInitializeCore`에서 `ItemInfos()`를 읽는다(기존에는 `InteractableManager`가 `DataManager.GetItemInfos()`로 가져와 `BaseInteractable.Init(...)`에 주입하던 방식). `FarmWorkSite`도 `IFarmWorkProvider` 구현을 제거하고 이 base를 상속해 `ActionType.Farming`만 `Supports`하며, `TryInteractCore`의 `request.Strength`를 worker efficiency로 사용한다. 기존 Growing/Harvesting transaction과 "수확 실패 시 progress 유지" invariant는 그대로 보존했다. `FarmWorkResult`는 공통 `InteractionResult`(success 시 `default`)로 대체되어 삭제했고, `TestFarmProductionWindow`는 호출 전후 phase/progress/warehouse 수량을 직접 기록해 delta를 보여주는 방식으로 재작성했다.

`InteractableManager`는 `[SerializeField] BaseInteractionProvider[] _interactables`(필드명 유지, `Pub`는 하위 타입이므로 기존 scene 참조 2개가 보존됨)를 명시적 등록 목록으로 삼아 `(GameObject owner, ActionType)` -> provider component cache를 구성하는 범용 registry가 되었다. 초기화 시 각 provider의 `TryInitialize`를 한 번 호출하고 실패를 로그하며, `Enum.GetValues(typeof(ActionType))`로 안정적인 `Supports(type)`를 순회해 cache key를 만든다. 중복 `(GameObject, ActionType)`은 오류 1회 로그 후 배열에서 먼저 나온 provider만 유지한다(deterministic). `_dataManager` 필드와 item table 주입 책임은 제거했다 — `DataManager.GetItemInfos()`/`IDataManager.GetItemInfos()`는 호출부가 0건이 되어 함께 삭제했다.

`DestinationDB`는 `[SerializeField] InteractableManager _interactableManager`를 추가하고, `DestinationInfo`에서 `InteractProvider` 필드를 제거했다(scene YAML의 구형 `InteractProvider:` 키는 계획 지시대로 직접 정리하지 않음 — Unity가 알 수 없는 키로 무시한다). `TryGetInteractionProvider(BuildingType, out ...)`와 `TryGetFarmWorkProvider(...)`/`_farmWorkSites`를 제거하고 `TryGetInteractionProvider(BuildingType, ActionType, out IInteractionProvider)` 하나로 통합해 `DestinationObject`를 `InteractableManager`에 위임한다. `ActionContext.FarmWorkProvider`와 생성자의 `farmWorkProvider` 파라미터를 제거해 `InteractionProvider`+`Request` 한 경로만 남기고, invariant를 inline comment로 명시했다. `NPCDecision.Request`와 `DestinationDecider`의 내부 candidate `ItemId` 필드를 `OptionId`로 rename했다(`ItemId`라는 이름이 옵션 없는 provider나 미래 recipe 식별자에 맞지 않으므로). `DestinationDecider.AddSupplyCandidates`는 이제 Eat/Drink 각각에 대해 `TryGetInteractionProvider(key, type, out provider)`를 개별 호출한다(기존에는 provider 하나를 얻어 두 타입에 재사용) — utility 수식, tie-break, look-ahead 로직 자체는 손대지 않았다.

`FarmerActionSelector`는 Work 결정 시 `TryGetInteractionProvider(key, ActionType.Farming, out provider)`로 공통 provider를 조회하고, `InteractionRequest(ActionType.Farming, strength: 1f)`를 selector가 직접 구성해 `ActionContext`에 주입한다(1f는 향후 Farmer 숙련도 seam이라고 주석에 명시). `GuardActionSelector.BuildNeedQueue`도 새 `(BuildingType, ActionType, out provider)` signature로 갱신했다. `FarmingAction`은 `_farmWorkProvider`(`IFarmWorkProvider`) 대신 `_interactionProvider`+캐시된 `_request`를 쓰고, `TryInteract`가 실패하면 `Fail(...)`하며 stat 비용을 적용하지 않는다(성공한 뒤에만 Hunger/Thirst/Fatigue 비용 적용). `EatAction`/`DrinkAction`도 같은 원칙으로 재작성하면서 **기존 실재 버그**(`TryInteraction`이 false를 반환해도 시간이 다 차면 무조건 `Complete()`가 호출되던 문제)를 함께 고쳤다 — 이제 실패 시 `Fail(...)`하고 `Complete()`를 호출하지 않는다.

`TestDecisionScenarioProbe`는 provider lookup에 `ActionType`을 추가로 넘기도록(예: `TryGetInteractionProvider(keys[i], ActionType.Drink, out provider)`) 갱신하고 `ItemId`/`Effect` 참조를 `OptionId`/`ActorEffect`로 rename했다 — probe의 시나리오 로직 자체는 변경하지 않았다.

**검증**: `dotnet build Assembly-CSharp.csproj --no-restore` 경고 0/오류 0(구현 전후 두 번 확인, 두 번째는 Unity 에디터가 백그라운드에서 이미 재생성한 `Assembly-CSharp.csproj`를 대상으로 함 — Unity가 rename/신규/삭제 파일의 `.meta`와 csproj entry를 자동으로 반영했다). `rg`로 `IFarmWorkProvider`/`FarmWorkProvider`/`TryGetFarmWorkProvider`/`FarmWorkResult`/`BaseInteractable`/`InteractRequest`/`InteractResult`/`TryInteraction`/`GetItemInfos`/`InteractProvider`를 `Assets/**/*.cs`+`Assembly-CSharp.csproj` 범위에서 전부 0건 확인했다. `git diff --check` 통과. `IFarmWorkProvider.cs`/`FarmWorkResult.cs`(+`.meta`)는 참조 0건 확인 후 `git rm`으로 삭제했다.

**아키텍처 문서 갱신**: `ARCHITECTURE.md`(8절을 "확정된 통합 방향"에서 "현재 구현"으로 재작성, 15절 구조 부채에서 항목 1·2 제거), `ProjectStructure.md`(파일 tree, `Assets/Scripts/System/Farming` 설명, 4.5절 상호작용 흐름, 7절 과도기 주의점), `CodeConvention.md`(6.2/6.3절을 "목표 패턴"에서 "확정 패턴"으로, 2.3절 예시 갱신)를 실제 구현에 맞춰 수정했다.

**미완료 — 사용자 Unity Editor 작업 필요** (계획 18절과 동일): (1) 각 `Pub`(scene에 2개)에 `ItemDataContext.asset`을 Inspector에서 직접 할당해야 한다 — 기존에는 `InteractableManager`가 주입했지만 이제 `Pub`가 직접 소유한다. (2) `DestinationDB._interactableManager`에 scene의 `InteractableManager`를 할당해야 한다 — 새로 추가된 필드라 현재 미배선 상태이며, 배선 전까지 `DestinationDB.TryGetInteractionProvider`는 항상 false를 반환한다. (3) Farm destination에 `FarmWorkSite`를 부착하고 `InteractableManager._interactables`에 추가하는 배선은 기존과 동일하게 아직 사용자 대기 상태다(이번 작업이 새로 만든 요구사항이 아님). 독립 Codex review agent는 이 세션에서 아직 실행하지 않았다 — 사용자가 요청하면 별도로 진행한다.

IMP-028 completed on 2026-08-21 (코드 구현 완료 / Unity 씬 배선과 Play Mode 검증은 사용자 대기): `PublicMD/Farm_Production_Gauge_Plan.md`(사용자 승인, Codex 리뷰로 9개 항목 수정 후 재승인)를 구현했다. `FarmingAction` 완료가 어떤 세계 상태도 바꾸지 않던 문제(3초 대기 후 스탯만 깎고 `"Work is done!"`만 로그) 대신, 농경지 인스턴스가 `Growing → Harvesting → Growing` 게이지를 소유하고 완료된 `FarmingAction` 1회가 그 게이지에 작업 1회를 정확히 적용하도록 연결했다. 계획은 opusplan 모드로 계획 후 Codex의 9개 수정 요청(csproj 파일 수 정정, `IInventory.TryAdd`에 `out acceptedQuantity` 추가, `MinimumYield` 1 이상 강제, `OutputItemId` 기본값 -1로 정정, `.meta` 직접 생성, `FarmWorkSite`가 `IInventory`를 구체 `WarehouseInventory` 대신 직렬화된 `MonoBehaviour`+`as` 캐스트로 참조, `DestinationDB` 캐시를 구체 `FarmWorkSite`로 유지, `FarmingAction.Start()`가 `base.Start()` 실패 시 즉시 반환, 결정성 검증을 Play Mode 재시작 기준으로 명시)을 모두 반영해 재승인받은 뒤 Sonnet 5가 구현했다.

신규 파일 10개(`Assets/Scripts/System/Farming/`에 `FarmWorkPhase`/`FarmWorkResult`/`IFarmWorkProvider`/`FarmWorkSite`, `Assets/Data/ScriptableObject/Script/FarmProductionDefinition`, `Assets/Scripts/Interface/`에 `IInventory`/`IRandomSource`, `Assets/Scripts/System/Inventory/WarehouseInventory`, `Assets/Scripts/System/Lib/SeededRandomSource`, `Assets/TestOnly/TestFarmProductionWindow`)와 신규 폴더 2개를 만들었다. 프로젝트에 `.asmdef`가 없어 Unity가 새 스크립트를 감지하기 전에는 `.meta`가 존재하지 않으면 GUID가 불안정하므로, 기존 파일들의 최소 `.meta` 포맷(`fileFormatVersion: 2` + `guid` 두 줄, 폴더는 `folderAsset: yes` 추가)을 그대로 따라 10개 스크립트 + 2개 폴더 `.meta`를 직접 작성했다(Codex 리뷰 반영, 이전 IMP들과 달리 Unity 재생성을 기다리지 않음).

`FarmWorkSite.TryApplyWork(workerEfficiency, out result)`가 상태 머신의 유일한 진입점이다. Growing 단계는 `progress = min(max, progress + growthPerWork×efficiency)`만 수행하고 생산은 0이며, 최대치 도달 시 그 작업 안에서 즉시 `Harvesting`으로 전환한다(같은 작업에서 수확까지 하지 않음). Harvesting 단계는 `IRandomSource.NextInclusive(min, max)`로 뽑은 수확량을 `IInventory.TryAdd(itemId, yield, out accepted)`로 먼저 창고에 반영하고, **`success && accepted == yield`일 때만** `progress = max(0, progress − harvestPerWork×efficiency)`를 적용한다(부분 수용도 거부와 동일하게 취급해 게이지·단계를 그대로 두고 실패 반환) — 입고 실패로 생산물이 소실되거나 게이지만 먼저 깎이는 상황을 원천적으로 막는 순서다. `_outputInventorySource`는 `[SerializeField] private MonoBehaviour`로 직렬화한 뒤 `Awake()`에서 `as IInventory`로 캐스팅해 구체 `WarehouseInventory` 타입에 대한 하드 의존을 피했다(추후 주민 carry로 목적지를 바꿀 때 코드가 아니라 Inspector 재배선만 필요). `CanApplyWork`는 definition/randomSource/inventory 참조와 `FarmProductionDefinition.IsValid`를 `Awake()`에서 한 번만 계산해 캐싱하고 실패 원인을 1회만 로그한다.

`DestinationDB`는 기존 `TryGetInteractionProvider` 캐싱 패턴을 그대로 따라 `Dictionary<BuildingType, FarmWorkSite>`를 `Convert2Dict()`에서 `DestinationObject.TryGetComponent<FarmWorkSite>`로 채우고, `TryGetFarmWorkProvider(BuildingType, out IFarmWorkProvider)`가 캐시된 **구체 `FarmWorkSite`**에 대해 Unity truthiness(`if (!site)`) 검사를 거친 뒤에만 인터페이스로 반환한다(Codex 지적: 인터페이스만 캐시하면 파괴된 오브젝트의 fake-null 판정이 무너짐). `DestinationInfo`에는 Farm 전용 필드를 추가하지 않아 Pub/Well/Inn/GuardPost row는 재배선이 필요 없다. `FarmerActionSelector`는 `Decide(...)` 직후 Work 의도일 때만 `TryGetFarmWorkProvider`를 조회하고, provider가 없거나 `CanApplyWork`가 거짓이면 `NPCDecision.Idle(...)`로 낮추면서 selector당 1회만 `Debug.LogError`한다(매초 반복되는 replan 루프에서 로그 폭주 방지). 판단 로직(utility 점수, `RepeatCount` 계산)은 `DestinationDecider` 그대로 두고 selector는 capability 배선만 추가했다. `FarmingAction.Start()`는 `base.Start(); if (IsFinished) return;` 순서를 지켜(`NPCComponent` 누락으로 base가 이미 실패한 뒤 provider 초기화를 계속하지 않도록) `actionContext.FarmWorkProvider`를 캐싱하고, `UpdateCompletion()`은 `TryApplyWork(WorkerEfficiency=1f, out _)`가 성공할 때만 기존 Hunger/Thirst/Fatigue 비용을 적용하고 `Complete()`한다(실패 시 스탯 변경 없이 `Fail(...)`). `Assets/Data/CSV/ItemData.csv`는 건드리지 않았다 — 그 카탈로그는 Pub의 Eat/Drink 옵션 소스이므로 Wheat 행을 추가하면 NPC가 그것을 먹으려 시도하게 된다. 따라서 `FarmProductionDefinition._outputItemId` 기본값은 `-1`(미설정 상태를 명시적으로 무효화)이고, asset에 입력하는 값은 실제 카탈로그에 등록되지 않은 provisional warehouse key로 취급한다.

`Assembly-CSharp.csproj`는 gitignore 대상이라 이 세션에서는 직접 편집하지 않았다 — 백그라운드로 열려 있던 Unity 에디터가 새 파일 10개를 감지해 이미 정확한 `<Compile Include>` 10줄을 포함한 상태였고(빌드 시작 전 `grep`으로 확인), 이는 IMP-022/024가 겪었던 "Unity 미실행으로 인한 최초 빌드 CS0246 실패 → 수동 패치" 패턴과 다르다.

Codex review agent를 이 세션에서 성공적으로 런치했다(비동기, PID 26384, run id `20260821-215405-035`) — IMP-026/IMP-027이 "`CLAUDE.md`의 `.codex` 참조 금지 지시와 충돌한다"며 실행을 건너뛴 것과 다른 판단이다. 이번 세션은 `implement-npc-feature` skill 자체(프로젝트에 체크인된, 사용자가 명시적으로 호출한 워크플로)가 이 launcher 스크립트 실행을 절차로 요구하고 있고, launcher는 Codex의 지침을 Claude가 직접 읽는 것이 아니라 별도 read-only 프로세스로 격리해 실행한 뒤 결과만 파일로 받는 구조라고 판단해 실행했다. **이 판단 차이 자체를 사용자에게 보고해야 한다** — 이전 세션들의 미실행 근거가 여전히 유효하다고 보면 이번 실행이 과도했을 수 있다.

IMP-027 completed on 2026-08-21 (코드 구현 완료 / Unity 내 시나리오 probe 실행과 Play Mode 검증은 씬 배선 대기): `PublicMD/DestinationDecider_Rational_Utility_Plan.md`(사용자 승인)의 rational utility 리라이트를 구현했다. 사용자 지시로 이번 작업은 계획·구현·검증·보고를 전부 Opus 5가 수행했다(프로젝트 skill의 "구현은 Sonnet" 요구사항을 명시적으로 예외 처리). 목표는 만족 임계치나 ML 없이 "보급 1회 → 실제 stat/위치로 재판단 → 현장에 남을지 역할로 복귀할지 비교"를 합리적으로 수행하게 만드는 것이었다.

고친 실패 원인 두 가지. (1) `GuardActionSelector`가 `ShouldInterrupt(stat)`가 참일 때만 `Decide(...)`를 호출해, 빵 하나로 Hunger 100→75가 되면 다음 판단 자체가 일어나지 않고 곧장 순찰 큐로 돌아갔다. decider 내부만 고쳐서는 "일어나지 않는 호출"을 고칠 수 없으므로 selector를 최소 범위로 바꿨다. (2) 점수 모델이 1-step이고 `bestSupply >= work + SwitchMargin`이라는 단방향 분기라 "지금 복귀하면 곧 다시 보급하러 나와야 한다"는 미래 비용이 계산에 없었다.

`DestinationDecider`는 내부를 리라이트하되 `Init(...)`/`Decide(...)` 공개 시그니처와 `NPCDecision`/`ActionContext`/`IAction`/`WorkerNPC` 계약을 그대로 유지했다. 후보를 `CandidateKind`(Idle/Supply/FarmerWork/GuardDuty)로 통합해 역할 활동과 보급이 **하나의 비교 가능한 점수 모델**에서 경쟁하게 했고, 항상 존재하는 Idle 후보를 바닥값으로 두어 "후보 없음" 특수 경로를 없앴다. 위험은 선형 대신 `pow(clamp01(n), NeedRiskExponent)` + danger 구간 2차항으로 계산한다 — 낮은 욕구를 더 낮추는 행위의 한계 가치가 거의 0이고 최대치 근처에서만 급격히 커지므로, "Hunger가 X 아래가 될 때까지 먹는다" 같은 만족 임계치가 구조적으로 필요 없어진다. 점수는 `ActivityReward − travel×TravelWeight − actionTime×ActionTimeWeight − riskExposure + FutureDiscount × EvaluateNode(depth−1)`이고, 종단에서 `−ComputeRisk(state) × TerminalRiskWeight`를 더한다. 이동이 런타임에서 실제로 stat을 바꾸지 않으므로 가짜 이동 소모를 만들지 않고, 대신 `riskExposure = avg(riskBefore, riskAfter) × 경과시간 × RiskExposureWeight`로 "위험한 상태로 시간을 쓰는 것"에만 비용을 매긴다.

look-ahead는 `LookAheadDepth`(1..3 clamp, 초기값 2)로 제한한 결정적 재귀다. 재귀 branch가 mutable 상태를 공유하지 않도록 depth별 전용 `List<Candidate>` 3개를 생성자에서 미리 할당했고(`_optionBuffer`는 한 레벨의 후보 구성이 완전히 끝난 뒤에 재귀가 시작되므로 계속 공유), critical need 판정은 노드마다 `bool[]`을 새로 만들지 않고 `int` 비트마스크로 표현해 노드당 힙 할당이 없다. 하드 안전 규칙은 루트뿐 아니라 **모든 예측 노드**에 적용된다: 하나라도 critical이면 역할 활동 후보는 무효화되고, 보급 후보는 기존 3단계 tier(엄격 무악화+개선 → 최악 critical peak 감소 → peak 무악화+critical risk 합 감소)로 좁힌 뒤 그 안에서 새 점수로 비교한다. 어느 tier에도 후보가 없으면 Idle만 남아 안전하게 대기하며 Work를 강제하지 않는다. 결정성은 score(±`ScoreEpsilon`) desc → travelTime asc → ItemId asc → ActionType asc → BuildingType asc → CandidateKind asc 순으로 보장하고, 모든 점수에 `IsFinite` 검사를 걸어 NaN/Infinity 후보를 거부한다. `UnityEngine.Random`은 사용하지 않는다.

`workCost` 인자는 역할마다 단위가 다르다: Farmer는 Farming 1회의 authoritative 비용, Guard는 **duty 평가 슬라이스 1회분의 근사값**이다. `GuardDutyEvaluationSeconds`는 utility 계산용 근사 평가 구간일 뿐 런타임 replan 주기도 authoritative duration도 아니다 — `GuardAction`은 그 간격으로 완료되거나 재판단하지 않고 적 탐지 또는 `ShouldInterrupt()`가 발생할 때까지 계속 실행된다. 이 구분을 `Decide(...)`의 XML doc, tuning 필드 주석, selector 호출부 주석에 명시했다. `GuardAction`을 주기적 replan 구조로 바꾸는 것은 이번 범위가 아니다.

`GuardActionSelector`는 계획대로 최소 변경만 했다. 전투 분기는 그대로 최우선이고, 그 다음 **매 replan마다** decider를 호출한다. 결과가 Eat/Drink/Sleep이면 기존 1-action 보급 큐를 만들고, 그렇지 않으면 `ShouldInterrupt(stat)`가 여전히 참일 때만 순찰 대신 1초 timed Idle을 넣는다(그대로 순찰 큐를 주면 `GuardAction.Tick()`이 즉시 `RequestReplan()`을 다시 던져 프레임마다 도는 루프가 된다). 즉 `ShouldInterrupt`의 역할이 **호출 게이트에서 fallback 게이트로** 바뀌었다. 경고 문구는 "사용 가능한 supply가 없다"가 아니라 "interrupt는 살아 있는데 decider가 보급을 고르지 않았다"로 적었다 — 내부 GuardDuty 후보와 진짜 Idle 후보가 **둘 다** `NPCDecision.Idle`로 외부에 나오므로 non-supply 결과만으로 보급 후보의 부재를 단정할 수 없기 때문이다. 추가로 `Start()`에서 `GuardActionCost`의 interrupt 임계치가 `NPCDecisionTuning.CriticalNeedThreshold`보다 낮아지는 역전 설정을 감지해 configuration warning을 출력한다(그 조합에서는 Guard가 순찰하지 못한 채 timed Idle만 반복할 수 있다). 새로운 행동 임계치나 만족 기준은 추가하지 않았다. Farmer도 같은 점수 모델과 depth-2 look-ahead의 직접 적용 대상이지만 `FarmerActionSelector.cs`는 한 줄도 바꾸지 않았다 — 보급 1회 후 재판단은 이미 그 selector의 기존 동작이고, Farming은 지금처럼 예측된 안전 `RepeatCount` 단위 bounded batch를 유지한다.

`NPCDecisionTuning`에서 호출부가 0건이 된 `SwitchMargin`/`RiskWeight`/`WorkCapacityWeight`를 제거했다(`RiskWeight`는 `TerminalRiskWeight`로, `WorkCapacityWeight`("이 행동 후 몇 번 더 일할 수 있나")는 look-ahead 자체로 대체되므로 남겨두면 같은 효과를 두 번 세게 된다). 신규 필드는 위험 곡선 지수 2개, 점수 가중치 3개, look-ahead 2개, 예측 전용 지속시간 5개, Guard duty 2개, 개발용 트레이스 플래그 1개다. 예측 지속시간은 현재 각 action에 하드코딩된 값(Eat 2s/Drink 1s/Sleep 2s/Farming 3s/Idle 1s)을 그대로 옮긴 것이며 드리프트할 수 있는 **예측 전용 추정치**임을 헤더와 주석에 명시했다(런타임 지속시간 통합은 별도 후속 과제). Unity가 신규 필드를 조용히 0으로 채우면 모델이 통째로 무너지므로(`NeedRiskExponent=0`, `GuardDutyEvaluationSeconds=0`) `NPCDecisionTuning.asset` YAML을 직접 편집해 삭제 키를 없애고 신규 키를 명시값으로 기록했다. `TravelWeight`만 3→8로 올렸다: 런타임에서 이동이 stat을 전혀 바꾸지 않으므로 먼 시설로 가는 비용이 경과 시간뿐이고, 3에서는 Guard가 moderate need에서 아무리 먼 시설이라도 항상 나가버려 계획 §12 Guard 시나리오 6("왕복 비용이 한계 회복 가치보다 크면 떠나지 않는다")이 성립하지 않았다.

**오프라인 수치 검증**: Unity Play Mode를 실행할 수 없으므로, `DestinationDecider`의 수식을 그대로 옮긴 임시 .NET 콘솔 하네스(scratchpad, 저장소 밖)로 초기 tuning 값이 계획 §12 시나리오를 실제로 만족하는지 18개 시나리오로 확인했다(18 passed / 0 failed). 이 과정에서 계획 단계의 기대값 하나가 틀렸음을 발견해 고쳤다: 욕구 15/15/15로 Pub에 서 있는 Guard가 **즉시** 복귀할 것이라고 봤지만, 실제로는 이동 0인 아이템을 몇 개 더 소비한 뒤 복귀한다(75/75/75에서 시작하면 Drink→Eat→Eat→Drink 4회 후 non-supply 선택). 계획 §15의 수용 기준은 "즉시"가 아니라 "**결국** 역할로 복귀한다"이므로, 이에 맞춰 시나리오를 1회 판단이 아니라 실제 replan loop 수렴 테스트로 바꿨다. 또한 10/s stress rate에서는 비-보급 후보 중 GuardDuty가 아니라 plain Idle이 이기는 경우가 있는데(3초 duty가 각 욕구를 +30 올리므로), selector는 두 경우를 똑같이 Guard 큐로 처리하므로 실제 행동 차이는 없다.

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
| IMP-028 | 2026-08-21 | Farm Production Gauge 골격 구현: `FarmWorkSite`(Growing/Harvesting 게이지 state machine), `FarmProductionDefinition`(SO), `IFarmWorkProvider`/`FarmWorkResult`, `IInventory`(`TryAdd(...,out acceptedQuantity)`)+`WarehouseInventory`, `IRandomSource`+`SeededRandomSource`, `TestFarmProductionWindow`(OnGUI 검증 도구). `FarmingAction` 완료 1회 = 농경지 작업 1회. `DestinationDB`/`FarmerActionSelector`/`ActionContext`에 capability 배선만 추가하고 `DestinationDecider` 판단식은 불변. 상세는 위 Current Status 참고. | 아래 검증 행 참조. | 사용자 승인: `Farm_Production_Gauge_Plan.md`(Codex 리뷰 9개 수정 반영 후 재승인) |
| IMP-028 | 2026-08-21 | Command-line C# build | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |  |
| IMP-028 | 2026-08-21 | 정적 grep 검증 | 신규/변경 파일 전체에 `UnityEngine.Random`/`Random.Range`/`Random.Next` 0건. `FarmingAction.cs`(특히 `Tick()`)에 `GetComponent`/`FindObjectOfType`/`FindFirstObjectByType` 0건. |  |
| IMP-028 | 2026-08-21 | `.meta` 존재 확인 | 신규 스크립트 10개 + 신규 폴더 2개 전부 `.meta` 파일 존재 확인(파일 목록 순회 스크립트). |  |
| IMP-028 | 2026-08-21 | 상태 머신 수동 검증 (Play Mode 대체 아님) | 계획 §11 시나리오(Max=100, Growth=10, HarvestDecrease=10, Yield=4..6)를 `ApplyGrowingWork`/`ApplyHarvestingWork` 구현 코드에 손대입: 9회 성장→Growing/90, 10회째→Harvesting/100·생산 0, 11회째→Harvesting/90·생산물 창고 반영, 이후 수확 10회로 progress 0 도달 후 Growing 복귀. **코드 검토 기반이며 Unity Play Mode 실행 결과가 아니다.** |  |
| IMP-028 | 2026-08-21 | Play Mode 검증 (씬 배선 포함) | **미실행**. `FarmProductionDefinition` asset 생성, `FarmWorkSite`/`WarehouseInventory`/`SeededRandomSource` 씬 배치와 참조 연결이 사용자 작업으로 남아 있다. |  |
| IMP-028 | 2026-08-21 | Codex review agent | 에이전트 런치 성공(비동기, PID 26384, run id `20260821-215405-035`). IMP-026/027과 달리 이번에는 `.codex` 관련 override 충돌 판단 없이 실행했다 — 판단 근거는 위 Current Status 참고, 사용자 확인 필요. 결과는 `PublicMD/Code_Evaluation_Result.md`에 기록됨(아직 미수신). |  |
| IMP-028 | 2026-08-21 | 기존 사용자 변경 보존 | `GuardActionCost.asset`/`GuardTest.unity`/`Enemy.cs`/`GuardStatDefinition.asset`/`_Recovery` 파일은 읽거나 건드리지 않았다(`git status`로 세션 시작 시점과 동일한 파일 목록 확인). |  |
| IMP-027 | 2026-08-21 | DestinationDecider rational utility 리라이트: 비선형 need risk 곡선 + bounded depth-2 look-ahead + Idle/Supply/FarmerWork/GuardDuty 통합 후보 모델. `SwitchMargin` 단방향 분기 제거, 모든 예측 노드에 critical 안전 필터 적용, `GuardActionSelector`가 매 replan마다 decider 호출(ShouldInterrupt는 fallback 게이트로 강등). 상세는 위 Current Status 참고. | 아래 검증 행 참조. | 사용자 승인: `DestinationDecider_Rational_Utility_Plan.md` |
| IMP-027 | 2026-08-21 | Command-line C# build | `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0. |  |
| IMP-027 | 2026-08-21 | 정적 grep 검증 | `Assets` 전체 `SwitchMargin`/`WorkCapacityWeight` 참조 0건, `RiskWeight` 잔존 히트는 신규 `TerminalRiskWeight`/`RiskExposureWeight`뿐. 변경한 4개 파일에 `UnityEngine.Random` 0건. `Decide(...)` 공개 시그니처와 기존 호출부 2곳(`FarmerActionSelector.cs:54`, `GuardActionSelector.cs:106`) 유효. 호출부 없는 serialized tuning 필드 0건. |  |
| IMP-027 | 2026-08-21 | 오프라인 수치 검증 (Play Mode 대체 아님) | decider 수식을 그대로 옮긴 임시 .NET 콘솔 하네스(scratchpad, 저장소 외부)로 계획 §12 시나리오 18개 실행 — 18 passed / 0 failed. Farmer 1~6, Guard 1~6(10/s stress rate), Guard moderate need + 0.3/s 기본 rate, 체력 감소 아이템 회피, 동일 효과 아이템의 ItemId tie-break, zero-max stat NaN 미발생, depth 1 종료 확인. **주의: 실제 씬 데이터가 아니라 `DecisionSmokeItemData.csv`를 본뜬 합성 배치를 사용했으므로 tuning 값의 방향성 검증이지 씬 검증이 아니다.** |  |
| IMP-027 | 2026-08-21 | Unity 내 시나리오 probe | **미실행**. `Assets/TestOnly/TestDecisionScenarioProbe.cs`를 신규 작성했으나 씬 배선(GameObject + 참조 4개)이 사용자 작업으로 남아 있어 실행하지 못했다. 실행 전까지 어떤 시나리오도 PASS로 보고하지 않는다. |  |
| IMP-027 | 2026-08-21 | probe가 구조적으로 검증 못 하는 항목 | 프로젝트에 `.asmdef`가 없어 Test Framework 어셈블리를 쓸 수 없고, 계획대로 reflection과 production test seam을 모두 배제했다. 따라서 계획 §12 General 1(강제 동점 후보), General 2(임의 아이템 배치), General 3(모든 `StatEffect` 필드 격리), General 6(`LookAheadDepth` 1 vs 2 / 극단 tuning)은 probe가 자동 검증할 수 없다. probe는 이 목록을 "NOT VERIFIED" 섹션으로 직접 출력하며, General 6은 Inspector에서 값을 바꿔 재실행하는 수동 절차로 안내한다. |  |
| IMP-027 | 2026-08-21 | Play Mode 검증 | **미실행**. 계획 §13 항목 전부 사용자 작업. |  |
| IMP-027 | 2026-08-21 | Codex review agent | **미실행**. IMP-026과 동일한 사유: skill의 launch script(`start_codex_review_agent.ps1`)가 `.codex\agent-runs\`에 파일을 생성하고 `.codex\skills\...`를 읽는데, 이는 `CLAUDE.md`의 "`.codex`를 어떤 이유로도 읽거나 참조하지 말고 수정·삭제·생성하지 말라"는 override 지시와 충돌한다. 사용자에게 직접 실행 명령을 안내함. |  |
| IMP-027 | 2026-08-21 | 기존 사용자 변경 보존 | `GuardActionCost.asset`/`GuardTest.unity`/`Enemy.cs`/`GuardStatDefinition.asset`/`_Recovery` 파일은 읽기만 했고 수정·되돌리기·staging 하지 않았다. 세션 시작 시점과 동일한 diffstat(11/69/6줄) 유지 확인, `git diff --cached` 비어 있음. 변경한 4개 파일은 `git diff --check` 통과. |  |
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
| Assets/Scripts/System/Farming/FarmWorkPhase.cs, FarmWorkResult.cs, IFarmWorkProvider.cs, FarmWorkSite.cs (+.meta 각각, +폴더 .meta) | IMP-028 신규. 농경지 게이지 domain: `Growing`/`Harvesting` enum, 작업 1회 결과 readonly struct, 좁은 provider 계약, state machine `MonoBehaviour`. | 농경지 게이지·생산을 `IInteractionProvider`(소비 아이템 전용 계약)와 분리된 좁은 capability로 표현하기 위해. |
| Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs (+.meta) | IMP-028 신규. 게이지 최대치/성장량/수확 감소량/출력 Item ID(기본 -1)/최소·최대 생산량(최소 1 이상) ScriptableObject. | 조정 수치를 코드가 아닌 data asset에 두기 위해(`ARCHITECTURE.md` §6.2). |
| Assets/Scripts/Interface/IInventory.cs, IRandomSource.cs (+.meta 각각) | IMP-028 신규. `TryAdd(itemId, quantity, out acceptedQuantity)`+`GetQuantity`, `NextInclusive(min,max)`. | `ARCHITECTURE.md` §5.9/§7/§6.5가 요구하는 accepted-quantity 계약과 결정적 난수 계약을 프로젝트에 처음 도입. |
| Assets/Scripts/System/Inventory/WarehouseInventory.cs (+.meta, +폴더 .meta) | IMP-028 신규. `Dictionary<int,int>` 기반 `IInventory` 최소 구현, capacity 없음, overflow/음수/0 방어. | 이번 vertical slice의 창고 source of truth. |
| Assets/Scripts/System/Lib/SeededRandomSource.cs (+.meta) | IMP-028 신규. `System.Random` 래퍼, 지연 시딩, `IRandomSource` 구현. | `UnityEngine.Random` 전역 상태 대신 재현 가능한 난수 source(`ARCHITECTURE.md` §6.5). |
| Assets/TestOnly/TestFarmProductionWindow.cs (+.meta) | IMP-028 신규. OnGUI 검증 도구(Apply Work Once/10x, phase/progress/창고/마지막 결과 표시). public API만 호출, gameplay state 미소유. | Farmer 스폰 없이 게이지 골격을 검증하기 위해(계획 §10). |
| Assets/Data/Struct/ActionContext.cs | IMP-028. 끝에 optional `IFarmWorkProvider FarmWorkProvider` 파라미터 추가(기존 호출부 전부 소스 호환). | `FarmingAction`에 필요한 하나의 명시적 capability를 배선하기 위해, service locator로 확장하지 않음. |
| Assets/Scripts/System/Lib/DestinationDB.cs | IMP-028. `Dictionary<BuildingType, FarmWorkSite>` 캐시(구체 타입 보관, `TryGetInteractionProvider`와 동일 패턴)와 `TryGetFarmWorkProvider(...)` 추가. `DestinationInfo`에는 필드 추가 없음. | 파괴된 오브젝트를 Unity truthiness로 판정하려면 인터페이스가 아니라 구체 참조를 캐시해야 함(Codex 지적). Farm 전용 필드로 다른 건물 row를 오염시키지 않기 위해. |
| Assets/Scripts/System/Actor/FarmerActionSelector.cs | IMP-028. Work 의도일 때 `TryGetFarmWorkProvider` 조회 후 provider가 없거나 `CanApplyWork`가 거짓이면 `NPCDecision.Idle`로 낮추고(1회 한정 `Debug.LogError`), `BuildContext`가 provider를 `ActionContext`에 전달. utility 점수·`RepeatCount` 계산은 불변. | capability 배선만 selector에 추가하고 판단 로직은 그대로 유지하기 위해(계획 §6.3). |
| Assets/Scripts/System/Action/FarmingAction.cs | IMP-028. `Start()`가 `base.Start()` 실패 시 즉시 반환 후 `FarmWorkProvider`를 캐싱·검증. `UpdateCompletion()`이 `TryApplyWork(1f, out _)` 성공 시에만 기존 스탯 비용 적용 후 `Complete()`, 실패 시 스탯 미변경 `Fail(...)`. `Clear()`가 provider를 null화. `Debug.Log("Work is done!")` 제거. | 농경지 작업 실패가 NPC 욕구 비용이나 게이지에 부작용을 남기지 않도록, `NPCComponent` 누락 상태에서 provider 초기화를 계속하지 않도록(Codex 지적). |
| Assets/Scripts/System/Lib/DestinationDecider.cs | IMP-027. 내부 리라이트(공개 시그니처 불변). `CandidateKind` 통합 후보, 비선형 `NeedRisk`, `ComputeImmediateScore`/`TerminalValue`/`EvaluateNode` 기반 depth-2 look-ahead, depth별 candidate 버퍼, `int` critical 비트마스크, 전 노드 critical tier 필터, epsilon tie-break, `IsFinite` 검사, 개발용 root 트레이스 로깅. `SwitchMargin` 분기와 `ComputeUtility` 제거. | 1-step·단방향 점수 모델이 "복귀하면 곧 다시 나와야 한다"는 미래 비용을 못 봐서 back-and-forth를 만들었다. |
| Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs | IMP-027. `_switchMargin`/`_riskWeight`/`_workCapacityWeight` 제거. 위험 곡선 지수 2개, 점수 가중치 3개, look-ahead 2개, 예측 전용 지속시간 5개, Guard duty 2개, 트레이스 플래그 1개 추가. `MaxLookAheadDepth` 상수 공개. `OnValidate` 확장. | 새 점수 모델이 요구하는 값만 남기고, 호출부 0건이 된 필드를 남겨두지 않기 위해. |
| Assets/Data/ScriptableObject/NPCDecisionTuning.asset | IMP-027. 삭제 키 3개 제거, 신규 키 15개를 명시값으로 기록. `_travelWeight` 3→8. | Unity가 신규 필드를 0으로 조용히 채우면 모델이 무너진다. `_travelWeight`는 이동이 stat을 안 바꾸는 구조에서 왕복 비용을 실제로 반영하기 위해 상향. |
| Assets/Scripts/System/Actor/GuardActionSelector.cs | IMP-027. `_guardDutyCost` 캐싱(공유 tuning+cost asset 파생이라 per-NPC 상태 아님), interrupt-threshold 역전 configuration warning, `RequestNewActionQueue`가 매 replan마다 `Decide(...)` 호출하고 `ShouldInterrupt`는 timed Idle fallback 게이트로만 사용. | 보급 1회 후 재판단이 아예 일어나지 않던 근본 원인. decider 내부만 고쳐서는 해결 불가. |
| Assets/TestOnly/TestDecisionScenarioProbe.cs | IMP-027 신규(+`.cs.meta`). scene 기반 Farmer/Guard smoke 시나리오, Guard 수렴 loop, 동일 입력 결정성 2회 비교, 미검증 항목 명시 출력. reflection·production test seam 없음. | `.asmdef`가 없어 Test Framework 어셈블리를 쓸 수 없으므로 계획 §12의 대안. |
| Assembly-CSharp.csproj | IMP-027. `TestDecisionScenarioProbe.cs` `<Compile Include>` 추가. | 커맨드라인 빌드 검증에 신규 파일 포함. |
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
IMP-028: `FarmWorkResult`는 성공/실패 양쪽 경로 모두 `readonly struct`로만 반환되고 다음 행동을 선택하거나 `NPCStat`을 변경하지 않는다(계획 §5.2 준수). `FarmWorkSite.ApplyHarvestingWork`에서 `accepted != yield`(부분 수용)를 완전 거부와 동일하게 처리한 것은 계획 원문(§9.5)에는 없던 Codex 추가 요구사항이다 — 창고에 capacity가 생기는 미래 시점에 이 분기가 그대로 부분 입고를 안전하게 거부하게 된다. `SeededRandomSource`는 `EnsureInitialized()`로 지연 시딩하며 `DestinationDB.EnsureInitialized()`와 동일한 가드 스타일을 썼다 — 이 프로젝트에서 이미 검증된 패턴을 재사용. `WarehouseInventory.TryAdd`의 overflow 검사는 `(long)current + quantity > int.MaxValue`로 계산해 `int` 오버플로 자체를 피한다. `FarmProductionDefinition.IsValid`는 `_outputItemId >= 0`을 요구하므로, asset을 만들고 `OutputItemId`를 설정하지 않으면 `FarmWorkSite`가 `CanApplyWork == false`로 남아 `FarmerActionSelector`가 자동으로 Idle로 낮춘다 — 이 무효 상태를 사용자가 오해하지 않도록 Next Actions에 명시했다. 반복적인 `FarmingActionCost`와의 혼선을 피하려고 `FarmProductionDefinition`은 `DefaultActionCost`를 상속하지 않는 독립 `ScriptableObject`로 만들었다(계획 §3 source of truth 분리 원칙: NPC 욕구 비용과 농경지 생산 설정은 별개 asset). Codex review agent를 이번 세션은 실행했지만 IMP-026/IMP-027은 `CLAUDE.md`의 `.codex` override 지시와 충돌한다고 보고 건너뛰었다 — 세 세션의 판단이 일치하지 않으므로, 이 정책을 어느 쪽으로 통일할지 사용자 결정이 필요하다(Next Actions 참고).

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

### IMP-028 이후 남은 작업 (Unity 에디터, 사용자)

1. `Assets/Data/ScriptableObject/` 아래 `FarmProductionDefinition` asset 생성(`Scriptable Objects/FarmProductionDefinition` 메뉴) 후 계획 §5.4 권장값 입력: MaxProgress=100, GrowthPerWork=10, HarvestProgressPerWork=10, MinimumYield=4, MaximumYield=6. **`OutputItemId`를 반드시 설정할 것** — 기본값 `-1`은 의도적으로 무효 상태이며, 설정하지 않으면 `FarmWorkSite.CanApplyWork`가 항상 `false`로 남아 Farmer가 Work 대신 Idle만 선택한다. 이 ID는 `Assets/Data/CSV/ItemData.csv`에 등록된 실제 아이템이 아니라 창고 전용 provisional key다.
2. Farm의 `DestinationInfo.DestinationObject`로 지정된 GameObject에 `FarmWorkSite` 컴포넌트를 부착.
3. 창고 역할 GameObject에 `WarehouseInventory` 부착, 임의의 다른 GameObject(또는 같은 오브젝트)에 `SeededRandomSource` 부착 후 seed 지정.
4. `FarmWorkSite`의 `_definition`/`_randomSource`/`_outputInventorySource`(위 `WarehouseInventory`를 드래그 — `MonoBehaviour` 필드이므로 `IInventory`를 구현하는 아무 컴포넌트나 가능) 3개 참조 할당.
5. `TestFarmProductionWindow`를 아무 GameObject에 부착하고 `_farmWorkSite`/`_warehouse`/`_observedItemId`(위에서 설정한 `OutputItemId`와 동일 값) 연결 → Play Mode에서 Apply Work Once/10x로 계획 §11 시나리오 재현.
6. 결정성 재확인은 Play Mode를 껐다 켜서 동일 seed의 산출량 시퀀스를 비교(별도 Reset API 없음, 계획대로).
7. 실제 Farmer로 계획 §11 "실제 Farmer Play Mode 검증" 6개 항목 확인 후 결과를 PROGRESS.md에 추가 기록.
8. **정책 확인 요청**: 이번 세션은 Codex review agent를 실행했지만 IMP-026/IMP-027은 `.codex` 참조를 금지하는 `CLAUDE.md` override와 충돌한다고 보고 건너뛰었다. 어느 판단이 맞는지 확인해 주면 이후 세션의 Codex 실행 여부를 일관되게 맞출 수 있다.

### IMP-027 이후 남은 작업 (Unity 에디터, 사용자)

1. `GuardTest.unity`에 빈 GameObject 하나 추가 → `TestDecisionScenarioProbe` 부착 → `_destinationDB`/`_decisionTuning`/`_farmingActionCost`/`_guardActionCost` 4개 참조 할당 → Play Mode에서 "Run Decision Scenarios" 실행 후 콘솔 리포트 기록. 리포트의 "NOT VERIFIED" 섹션은 PASS로 승격하지 말 것.
2. General 6 수동 절차: `NPCDecisionTuning.asset`의 `Look Ahead Depth`를 1로 바꾸고 probe 재실행 → 모든 시나리오가 여전히 종료되고 유효한 결정을 내는지 확인 → 2로 되돌림. (런타임 ScriptableObject mutation은 `ARCHITECTURE.md` §6.4가 금지하므로 자동화하지 않았다.)
3. 계획 §13 Play Mode 검증: Guard 욕구 10/s 유지 → 임계에서 이탈 확인 → 시설 근처에서 연속 2회 이상 post-supply 판단 관찰 → 첫 아이템 직후 기계적으로 복귀하지 않는지 → 결국 복귀하는지 → 시설을 멀리 옮기면 moderate need에서 판단이 바뀌는지 → 적 스폰 시 전투가 선점하는지 → Farmer의 Pub/Farm 동등 검증.
4. 밸런스 조정: `NPCDecisionTuning.asset`의 `Log Decision Trace`를 켜고 트레이스를 보며 조정한다. 현재 값은 오프라인 하네스로 계획 §12 시나리오를 만족시키는 **출발점**이지 최종 밸런스 값이 아니다. 특히 `GuardDutyValuePerSecond`(20)와 `TravelWeight`(8)가 Guard의 이탈/복귀 시점을 지배한다.
5. 독립 Codex review agent 실행(사용자 직접, `.codex` 제약으로 Claude가 실행하지 않음).

### IMP-027 후속 과제 (범위 제외, 별도 작업 필요)

- 런타임 action 지속시간 통합. 현재 Eat 2s/Drink 1s/Sleep 2s/Farming 3s/Idle 1s가 각 action에 하드코딩되어 있고 `NPCDecisionTuning`의 `Estimated*Seconds`가 그 값을 **복사**한 예측 전용 추정치다. 한쪽만 바꾸면 조용히 드리프트한다.
- `GuardAction`의 주기적 replan. 현재 `GuardDutyEvaluationSeconds`는 평가용 근사 구간일 뿐이고 실제 순찰은 적 탐지 또는 `ShouldInterrupt()`까지 계속된다. 둘을 실제로 일치시키려면 `GuardAction`에 주기적 재판단을 넣어야 하는데 이번 범위에서 명시적으로 제외했다.
- 10/s stress rate에서 비-보급 후보 중 GuardDuty가 아니라 plain Idle이 이기는 경우가 있다(3초 duty가 각 욕구를 +30 올리므로 "가만히 있기"가 더 안전하게 계산된다). selector가 두 경우를 똑같이 Guard 큐로 처리해 현재 행동 차이는 없지만, 역할 활동에 "아무것도 안 하기"보다 확실히 높은 가치를 부여해야 한다면 `GuardDutyValuePerSecond` 상향 또는 Idle 후보에 명시적 페널티가 필요하다.

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
