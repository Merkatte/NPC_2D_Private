# Code Evaluation Result

## Purpose

`Guard_Action_Implementation_Plan.md` Phase A–D 구현을 대상으로 NPC/worker 책임 경계, action lifecycle, 전투 타겟 유효성, pooling, Unity 직렬화 안전성과 회귀 위험을 읽기 전용으로 감사했다.

## Review Snapshot

- Date: 2026-08-19
- Scope:
  - 요청에 명시된 신규·변경 C# 파일 전체
  - 직접 의존 표면: `BaseNPCActionSelector`, `DataManager`, `DestinationDB`, `DestinationDecider`, `WorkerPool`, `NPCDecision`
  - `Assets/Scenes/SampleScene.unity`
  - `Assets/Prefab/NPCGirl.prefab`
  - `ProjectSettings/TagManager.asset`, `ProjectSettings/Physics2DSettings.asset`
  - `Assembly-CSharp.csproj`
  - `PublicMD/ARCHITECTURE.md`, `ProjectStructure.md`, `CodeConvention.md`, 이전 `Code_Evaluation_Result.md`
- Excluded:
  - 이번 Guard 변경과 무관한 `Game_Plan.md`, `PLAN.md` 기존 변경
  - untracked `Assets/_Recovery/0 (2).unity`
- Verification:
  - `git status`, `git diff`, `git diff --check` 수행
  - `git diff --check`: 오류 없음
  - 신규 파일의 `Assembly-CSharp.csproj` compile entry 전부 확인
  - action lifecycle, 타겟 유효성, Unity YAML과 정적 의존성 검색 수행
  - `Assets/BehaviorGraph/CustomActionNode`는 현재 존재하지 않음
  - sandbox가 read-only이므로 `dotnet build`는 재실행하지 않았다. 제공된 “0 warnings / 0 errors” 결과는 독립 재검증하지 못했다.
  - Unity Editor 및 G-PT-01~18 Play Mode 시나리오는 실행하지 못했다.

## Executive Summary

Guard 수직 슬라이스의 핵심 책임 배치는 양호하다. 타겟 선택은 selector, 물리 감지는 범용 sensor, collider 해석은 perception, 타겟 소유는 per-NPC runtime state, 실행은 action, queue lifecycle은 `WorkerNPC`에 배치되어 문서화된 의존 방향을 따른다. `MoveAction`도 전투 타입 대신 `IMoveTarget`/`MoveRequest`에 의존한다.

그러나 현재 저장소 상태는 Guard 기능을 실행할 수 없다. 씬의 Guard selector, cost assets, GuardPost, sensor/prefab, Enemy layer 배선이 모두 없고 `NPCManager`의 모든 NPC 타입이 Farmer selector를 참조한다. 이는 승인된 범위 제외 사항이지만 기능 완료·Play Mode 검증을 막는 High 통합 blocker다.

코드 측에서는 `Failed`와 `ReplanRequested`를 runner가 동일하게 즉시 재시도하여 영구 실패가 매 프레임 action 대여·로그·할당을 반복할 수 있다. 비활성화된 타겟이 유효한 것으로 남는 문제, 공격력 0에서 영구 공격하는 문제, Eat/Drink 상호작용 실패를 성공으로 처리하는 문제도 확인됐다.

최종 판정: **Changes requested — 구조적 방향은 승인 가능하지만 현재 상태는 통합 또는 기능 완료로 승인할 수 없다.**

## Priority Assessment

1. Architecture and responsibility placement: **확인된 책임 경계 위반 없음.**
2. Correctness, lifecycle, cancellation, regression: **H-02, M-01, M-02, M-05, M-06, M-07 확인.**
3. Unity scene, prefab, component, serialized-reference safety: **H-01, M-03, M-04 확인.**
4. Convention, maintainability, dead code, magic values: **L-01~L-04 확인.**

## Improvements Since Previous Review

- `WorkerNPC`의 `NPCType.Farmer` hardcode가 제거되고 실제 `NPCType`이 전달된다.
- `IAction`에 명시적 `ActionResult`가 추가됐다.
- action `Init()`과 `Start()`가 분리되어 대기 action이 미리 시작되지 않는다.
- current/pending action 정리와 pool 반환이 `WorkerNPC`의 단일 경로로 통합됐다.
- Farmer 및 Guard queue 조립이 중간 실패 시 이미 대여한 action을 반환한다.
- pooled NPC disable/re-init 시 action queue와 Guard target이 정리된다.
- Guard selector에서 Farmer work/cost 의존성이 제거됐다.
- sensor/perception/selector/runtime target 책임이 분리됐다.
- `MoveAction`이 전투 concrete type 없이 동적 목적지를 지원한다.
- `BuildingType`과 `ActionType` 신규 값이 enum 끝에 추가되어 기존 직렬화 index를 보존한다.
- 이전 보고서의 `NPCManager` registry 미등록, `NPCStat.ChangeMoveSpeed` clamp, 자동 테스트 부재는 아직 남아 있다.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — 현재 Unity 구성으로는 Guard 기능을 실행할 수 없다

- Severity: High
- Category: Unity composition / serialized-reference safety
- Location:
  - `Assets/Scenes/SampleScene.unity:1034-1035`
  - `Assets/Scenes/SampleScene.unity:1210-1213`
  - `Assets/Scenes/SampleScene.unity:1273-1289`
  - `Assets/Prefab/NPCGirl.prefab:11-15`, `61-65`
  - `Assets/Data/ScriptableObject/DefaultStatContext.asset:13-24`
  - `ProjectSettings/TagManager.asset:7-39`
- Evidence:
  - `NPCManager._selectors`의 Farmer, Guard, Cook 세 슬롯이 모두 동일한 `FarmerActionSelector` fileID `825924306`을 참조한다.
  - `DataManager._costInfos`에는 기존 cost 하나만 있고 Guard/Attack asset instance는 존재하지 않는다.
  - `DestinationDB`에는 Pub/Inn/Well/Farm만 있고 `BuildingType.GuardPost` 값 5가 없다.
  - `NPCGirl.prefab`에는 `WorkerNPC`, `NPCComponent`, `SpriteRenderer`만 있고 combat sensor, `Collider2D`, `ProximitySensor2D`, `GuardPerception` 및 `_guardPerception` 참조가 없다.
  - Enemy layer가 없고 Physics2D matrix는 기본 전체 충돌 상태다.
  - `DefaultStatContext.asset`에 `_attackPower`와 `_attackSpeed` 직렬화 값이 없다.
- Description: 코드 정의는 존재하지만 scene/prefab/config composition이 하나도 연결되지 않았다. 현재 Guard 생성은 Guard selector가 아니라 Farmer selector를 실행한다.
- Recommended fix: Unity Editor에서 Guard selector GameObject와 manager 슬롯, Guard/Attack cost asset, GuardPost destination, sensor child와 trigger collider, perception 참조, Enemy layer/collision matrix 및 공격 stat을 연결한다. 저장된 YAML을 검토한 뒤 G-PT-01~18을 실행한다.
- Impact if unfixed: Guard patrol, perception, target acquisition 및 attack loop가 실행되지 않는다. 현재 build 성공 여부와 무관하게 기능 검증과 release 승인이 불가능하다.
- Note: 구현 범위에서 의도적으로 제외된 작업임은 확인했다. 다만 기능 준비 상태 기준에서는 여전히 High blocker다.

#### H-02 — `Failed`가 즉시 동일 계획으로 재시도되어 영구적인 per-frame failure loop를 만든다

- Severity: High
- Category: Action lifecycle / failure handling
- Location:
  - `Assets/Scripts/Actor/WorkerNPC.cs:61-64`, `88-102`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs:113-140`
  - `Assets/Scripts/System/Action/AttackAction.cs:27-42`
- Evidence:
  - `WorkerNPC`는 `ReplanRequested`와 `Failed`를 동일하게 `CancelAndReturnQueue()` 후 즉시 `AdvanceQueue()`로 처리한다.
  - selector가 빈 queue를 반환하면 다음 `Update()`에서 다시 같은 selector를 호출한다.
  - `AttackAction`은 attack speed가 0 이하이면 `Failed`가 되지만 타겟은 유지된다. 다음 selector 호출은 같은 타겟에 다시 Attack을 구성한다.
  - combat action 대여 실패 시 `BuildCombatQueue`는 빈 queue를 반환하지만 `TryBuildCombatQueue`는 `true`를 반환한다.
- Description: 결과 enum은 실패와 정상 재계획을 구분하지만 runner 정책이 그 차이를 제거한다. 영구 설정 오류나 action 생성 실패는 Idle/backoff로 전환되지 않는다.
- Recommended fix:
  - `Failed`에 별도 안전 정책을 둔다. 예: 최소 1회 Idle backoff, 일정 시간 재시도 지연, 해당 intent 일시 차단.
  - `TryBuildCombatQueue`는 생성된 queue가 비어 있으면 `false`를 반환하고 명시적인 Idle fallback을 구성한다.
  - 빈 queue 자체를 정상 실패 표현으로 사용하지 않도록 selector의 `TryBuild...` 계약을 일관되게 만든다.
- Impact if unfixed: 잘못된 attack stat, pool factory 문제 또는 지속적인 시작 실패 시 매 프레임 로그, queue/List 할당과 pool churn이 발생하며 NPC가 복구하지 못한다.

### Medium

#### M-01 — 비활성화된 전투 타겟이 계속 유효하며 무효 handle도 명시적으로 정리되지 않는다

- Severity: Medium
- Category: Target lifecycle / pooled-object safety
- Location:
  - `Assets/Scripts/System/Actor/CombatTargetHandle.cs:39-41`
  - `Assets/Scripts/System/Actor/ProximitySensor2D.cs:58-69`
  - `Assets/Scripts/System/Action/MoveAction.cs:57-60`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs:65-75`
  - `Assets/Scripts/Actor/Enemy.cs:13-28`
- Evidence:
  - `CombatTargetHandle.IsValidPair`는 `owner`가 파괴되지 않았고 `target.IsAlive`이면 유효하다고 판단한다. `owner.gameObject.activeInHierarchy`는 확인하지 않는다.
  - 반대로 `ProximitySensor2D.Prune`은 inactive collider를 무효로 취급한다.
  - `Enemy`는 `OnDisable`에서 생존 상태를 변경하지 않는다.
  - 동적 `MoveAction`이 타겟 위치 조회에 실패해도 Guard runtime target은 지우지 않는다.
- Description: sensor/perception과 selected-target handle의 활성 상태 정의가 일치하지 않는다. SetActive(false)로 despawn/pooling된 살아 있는 적은 perception에서 사라지지만 선택된 handle에서는 계속 유효하다.
- Recommended fix: 전투 타겟의 “사용 가능” 의미를 하나의 계약으로 정의하고 `activeInHierarchy` 또는 명시적인 availability를 포함한다. selector는 `HasValidTarget == false`인 기존 handle을 새 후보 탐색 전에 명시적으로 정리한다.
- Impact if unfixed: Guard가 비활성·pooled 적을 계속 추적하거나 공격하고 stale Unity 참조가 NPC runtime state에 남는다.

#### M-02 — Eat/Drink의 실제 상호작용 실패가 `Completed`로 기록된다

- Severity: Medium
- Category: Action result correctness
- Location:
  - `Assets/Scripts/System/Action/EatAction.cs:45-52`
  - `Assets/Scripts/System/Action/DrinkAction.cs:46-53`
- Evidence: `TryInteraction(...)`이 `false`를 반환해 stat effect가 적용되지 않아도 두 action 모두 무조건 `Complete()`를 호출한다.
- Description: Start 단계의 provider 누락은 `Fail()`로 개선됐지만 실행 중 자원 소진·요청 거절은 여전히 성공으로 보고된다.
- Recommended fix: `TryInteraction` 실패를 `Failed` 또는 정책상 `ReplanRequested`로 반환하고, `Completed`는 effect가 실제 적용된 경우에만 사용한다.
- Impact if unfixed: 행동 기록과 실제 자원 소비가 불일치하고, 주민은 욕구를 해결하지 못한 채 성공한 것처럼 queue를 진행한다.

#### M-03 — 신규 C# 파일 14개 중 13개에 Unity `.meta`가 없다

- Severity: Medium
- Category: Unity asset identity / source control
- Location: 신규 Guard 관련 `.cs` 파일 전체. `GuardActionSelector.cs.meta`만 존재한다.
- Evidence: `Test-Path` 검사에서 `AttackActionCost`, `GuardActionCost`, `MoveRequest`, `Enemy`, `ActionResult`, 두 interface, 두 action, `CombatTargetHandle`, `GuardPerception`, `GuardRuntimeState`, `ProximitySensor2D`의 `.meta`가 모두 누락됐다.
- Description: Unity가 각 로컬 환경에서 새 GUID를 생성하게 되며 이후 scene/prefab 연결의 GUID 안정성이 보장되지 않는다.
- Recommended fix: 수동 wiring 전에 Unity에서 asset import를 완료하고 생성된 `.meta`를 모든 신규 스크립트와 함께 source control에 포함한다.
- Impact if unfixed: 다른 checkout에서 script GUID가 달라지거나 향후 직렬화 참조가 손실될 수 있다.

#### M-04 — 수동 Guard wiring을 검증할 component/config guard가 부족하다

- Severity: Medium
- Category: Serialized-reference validation
- Location:
  - `Assets/Scripts/System/Actor/GuardPerception.cs:11`, `25-30`
  - `Assets/Scripts/System/Actor/ProximitySensor2D.cs:10-13`
  - `Assets/Scripts/Manager/DataManager.cs:16-23`
- Evidence:
  - `GuardPerception`은 `_sensor`가 없으면 진단 없이 동작을 중단한다.
  - `ProximitySensor2D`는 Collider 존재만 강제하며 `isTrigger`, non-empty LayerMask 또는 필요한 Rigidbody2D 구성을 검증하지 않는다.
  - `DataManager.Awake`는 `_costInfos`, null entry, null `actionCost`, duplicate `ActionType`을 검증하지 않고 순회 및 `Add`한다.
- Description: 이번 기능은 다수의 수동 Inspector 설정을 요구하지만 오류가 silent no-op 또는 초기화 예외로 나타난다.
- Recommended fix: `Awake`/`OnValidate`에서 필수 참조, trigger 설정, LayerMask, cost null/duplicate를 구체적인 GameObject/ActionType 정보와 함께 검증한다.
- Impact if unfixed: wiring 실수 시 감지가 조용히 실패하거나 `DataManager` 전체 초기화가 중단되어 원인 추적이 어렵다.

#### M-05 — 공격력 0인 Guard는 살아 있는 타겟을 영구히 공격한다

- Severity: Medium
- Category: Combat invariant
- Location:
  - `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs:20-21`
  - `Assets/Scripts/System/Actor/NPCStat.cs:24-25`
  - `Assets/Scripts/System/Action/AttackAction.cs:27-43`, `90-97`
  - `Assets/Scripts/Actor/Enemy.cs:21-28`
- Evidence:
  - `NPCStat`은 attack power를 0까지 허용한다.
  - `AttackAction.Start`는 attack speed만 양수인지 검사하고 attack power는 검사하지 않는다.
  - `Enemy.ApplyDamage`는 0 이하 피해를 무시한다.
  - `AttackAction`은 타겟이 살아 있고 범위 안이면 스스로 완료하지 않는다.
- Description: attack power 0이 유효한 상태인지 실패 상태인지 정책이 없다.
- Recommended fix: combat-capable NPC의 attack power를 양수 invariant로 검증하거나, 0 공격력을 의도적으로 허용한다면 selector/action이 공격 불가 결과와 fallback을 명시적으로 처리한다.
- Impact if unfixed: 잘못 설정된 Guard가 동일 타겟을 영구 점유하고 순찰이나 다른 행동으로 복귀하지 못한다.

#### M-06 — `NPCManager` registry에 생성한 worker를 추가하지 않는다

- Severity: Medium
- Category: Manager responsibility / lifecycle
- Location: `Assets/Scripts/Manager/NPCManager.cs:28-35`
- Evidence: 타입별 list를 생성하지만 `newWorker`를 `_workers[npcType]`에 추가하지 않는다. release 시 제거 경로도 없다.
- Description: 이전 리뷰의 registry finding이 그대로 남아 있다.
- Recommended fix: 성공적으로 초기화된 worker만 registry에 추가하고 release/despawn 시 제거하는 대칭 경로를 제공한다.
- Impact if unfixed: 타입별 조회, release, 저장 또는 디버그 기능이 실제 생성 NPC를 찾지 못한다.

#### M-07 — 이동 속도 증가가 불가능한 기존 clamp 버그가 남아 있다

- Severity: Medium
- Category: Stat correctness / regression risk
- Location: `Assets/Scripts/System/Actor/NPCStat.cs:89-92`
- Evidence: `_moveSpeed = Mathf.Clamp(_moveSpeed + val, 0, _moveSpeed)`가 현재 속도를 자기 자신의 최대값으로 사용한다.
- Description: 양수 delta는 항상 기존 값으로 clamp된다.
- Recommended fix: 명시적인 max speed를 사용하거나 최대값 정책이 없다면 0 하한만 적용한다.
- Impact if unfixed: 향후 Guard 이동속도 buff/debuff, 추적 시간 예측 및 실제 이동 결과가 불일치한다.

#### M-08 — action 결과·pooling·combat loop를 검증하는 자동 테스트가 없다

- Severity: Medium
- Category: Verification / regression safety
- Location: project-wide
- Evidence:
  - `.asmdef` 파일이 없고 `[Test]`, `[UnityTest]`, `Assert` 사용처가 없다.
  - G-PT-01~18은 wiring 미완료로 실행되지 않았다.
  - 이번 검토에서는 read-only 제한으로 build도 재실행하지 못했다.
- Description: queue 취소, target 사망/비활성화, 복수 collider, hit cap 및 pool reuse처럼 회귀 위험이 높은 상태 전이가 수동 검증에만 의존한다.
- Recommended fix: 최소한 action lifecycle 및 순수 runtime state에 Edit Mode 테스트를 추가하고, wiring 완료 후 G-PT-01~18을 Play Mode 테스트 또는 재현 가능한 체크리스트로 실행한다.
- Impact if unfixed: 컴파일 성공으로 검출되지 않는 lifecycle·타이밍·Unity null 회귀가 다음 변경에 재발할 가능성이 높다.

### Low

#### L-01 — 신규 tuning asset이 public mutable field를 노출하고 접근 여유값이 magic number다

- Severity: Low
- Category: Code convention / tuning ownership
- Location:
  - `Assets/Data/ScriptableObject/Script/GuardActionCost.cs:8-24`
  - `Assets/Data/ScriptableObject/Script/AttackActionCost.cs:8`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs:123`
- Evidence:
  - 신규 cost 값이 `[SerializeField] private` + read-only property가 아니라 public field다.
  - attack stopping distance가 `AttackRange * 0.9f`로 selector에 하드코딩됐다.
- Description: 기존 data style과 일부 일치하지만 신규 코드에 적용할 `CodeConvention.md`의 field style 및 data-driven tuning 원칙에는 맞지 않는다.
- Recommended fix: serialized private field와 read-only property를 사용하고, `0.9`가 balance 값이면 `AttackActionCost`의 명명된 접근 여유값으로 이동한다.
- Impact if unfixed: 외부 코드가 tuning asset을 runtime에 수정할 수 있고 접근 거리 정책을 찾거나 조정하기 어렵다.

#### L-02 — hit-cap clamp 조건이 제공된 구현 설명과 정확히 일치하지 않는다

- Severity: Low
- Category: Contract/documentation accuracy
- Location: `Assets/Scripts/System/Action/AttackAction.cs:94-109`
- Evidence: clamp 조건은 단순히 `hits >= MaxHitsPerTick`이다. 다섯 번째 타격으로 타겟이 죽으면 `targetDied == true`인 동시에 clamp도 실행된다.
- Description: “hit cap으로 중단했을 때만 clamp하고 death에서는 clamp하지 않는다”는 설명이 코드에 명시적으로 반영되지 않았다.
- Recommended fix: 의도가 엄격하다면 `hits >= MaxHitsPerTick && !targetDied && !outOfRange`처럼 중단 이유를 조건에 포함하거나 설명을 실제 동작에 맞춘다.
- Impact if unfixed: 현재는 즉시 pool 반환 시 timer가 Clear되어 실질 영향이 작지만, lifecycle 변경 시 설명과 구현의 차이가 회귀 원인이 된다.

#### L-03 — `ProjectStructure.md`의 상단 구조 설명이 현재 코드와 모순된다

- Severity: Low
- Category: Documentation drift
- Location:
  - `PublicMD/ProjectStructure.md:5-83`
  - `PublicMD/ProjectStructure.md:388-423`
- Evidence: 상단은 selector가 비어 있고 action이 대부분 `NotImplementedException`이며 `NPCComponent`가 empty라고 설명하지만 하단에는 현재 Guard 구현을 추가 설명한다.
- Description: 최신 섹션을 append했지만 문서의 주 구조 설명은 2026-08-01 skeleton 상태에 머물러 있다.
- Recommended fix: 상단 file tree, runtime flow와 class role을 현재 구현 기준으로 갱신하고 과거 skeleton 설명은 history로 분리한다.
- Impact if unfixed: 다음 구현자가 서로 충돌하는 책임 설명 중 잘못된 기준을 따를 수 있다.

#### L-04 — 기존 action duration과 정상 경로 로그가 여전히 코드에 박혀 있다

- Severity: Low
- Category: Magic values / maintainability
- Location:
  - `EatAction`, `DrinkAction`, `SleepAction`, `FarmingAction`, `IdleAction`
  - `Assets/Scripts/System/Action/FarmingAction.cs:51`
- Evidence: 1f, 2f, 3f 실행 시간이 concrete action field에 남아 있고 Farming 정상 완료마다 `Debug.Log`를 출력한다.
- Description: 이전 리뷰의 tuning 및 console-noise cleanup 항목이 유지되고 있다.
- Recommended fix: duration을 action별 tuning source로 이동하고 정상 반복 로그는 debug instrumentation 뒤로 이동하거나 제거한다.
- Impact if unfixed: balance 조정이 코드 변경을 요구하고 다수 NPC 실행 시 콘솔 노이즈가 증가한다.

## Findings By File

- `WorkerNPC.cs`: H-02. queue 단독 ownership과 disable 정리는 적절하지만 `Failed` 정책이 필요하다.
- `GuardActionSelector.cs`: H-02, L-01. 전투→욕구→순찰 책임 배치는 적절하다.
- `AttackAction.cs`: H-02, M-05, L-02. 반복 타격과 post-hit 생존 재검사는 적절하다.
- `CombatTargetHandle.cs`: M-01. destroyed-object 방어는 좋지만 inactive-object 의미가 빠져 있다.
- `MoveAction.cs`: M-01. generic dynamic target 설계는 좋지만 invalid target의 domain cleanup seam이 필요하다.
- `EatAction.cs`, `DrinkAction.cs`: M-02.
- `GuardPerception.cs`, `ProximitySensor2D.cs`: M-01, M-04. 감지와 domain 변환 분리는 적절하다.
- `NPCManager.cs`: M-06.
- `NPCStat.cs`: M-05, M-07.
- `GuardActionCost.cs`, `AttackActionCost.cs`: L-01.
- `SampleScene.unity`, `NPCGirl.prefab`, project settings: H-01.
- 신규 `.meta`: M-03.
- `ProjectStructure.md`: L-03.

## Cross-Cutting Findings

- 명시적 action 결과는 도입됐지만 failure recovery policy는 아직 완성되지 않았다.
- Unity target validity가 destroyed, dead, inactive 상태를 하나의 일관된 정의로 다루지 않는다.
- 수동 composition이 핵심인 기능에 editor/runtime validation이 충분하지 않다.
- Guard 코드의 책임 경계는 양호하나 현재 scene composition과 자동 검증이 이를 실행 가능한 기능으로 만들지 못한다.
- 기존 manager registry, move-speed clamp, action duration 부채가 직접 수정된 의존 표면에 남아 있다.

## Positive Notes

- `WorkerNPC`가 concrete Guard action을 알지 않고 queue lifecycle만 소유한다.
- selector가 타겟 선택과 plan 구성을 소유하며 action을 직접 실행하지 않는다.
- `GuardAction`은 Physics query나 component lookup을 수행하지 않는다.
- `ProximitySensor2D`는 전투 domain type을 의존하지 않는다.
- collider reverse mapping과 per-target collider set으로 복수 collider exit가 올바르게 모델링됐다.
- `CombatTargetHandle`의 Owner pair는 interface를 통한 Unity destroyed-null 함정을 방지한다.
- Guard별 `GuardRuntimeState`가 `NPCComponent` instance에 생성되어 selector 간 target 공유가 없다.
- `MoveRequest`가 movement와 combat의 concrete dependency를 차단한다.
- queue 조립 rollback과 `ActionPool` null 처리 방향이 개선됐다.
- Guard need 변화는 per-tick `StatEffect` allocation 없이 처리된다.
- attack timer, patrol index 및 action context가 pool 반환 시 초기화된다.
- enum 값이 끝에 추가되어 기존 Unity 직렬화 값을 보존한다.
- 신규 C# 파일의 `Assembly-CSharp.csproj` entry는 모두 존재한다.

## Recommended Next Actions

1. 신규 스크립트 `.meta`를 생성·보존하고 Unity Guard wiring을 완료한다.
2. `Failed`와 빈 queue에 Idle/backoff 정책을 추가해 per-frame retry loop를 제거한다.
3. inactive target과 stale handle을 포함한 target validity/clear 규칙을 통일한다.
4. attack power invariant와 Eat/Drink interaction failure 결과를 명확히 한다.
5. Guard sensor, selector, DataManager cost 구성을 `Awake`/`OnValidate`에서 검증한다.
6. G-PT-01~18 및 attack frequency/pool reuse 검증을 실행한다.
7. 기존 `NPCManager` registry와 `ChangeMoveSpeed` 버그를 수정 대상으로 분리한다.
8. `ProjectStructure.md` 상단 구조와 tuning field style을 현재 코드에 맞춘다.

## Final Verdict

**Changes requested.**

Guard 기능의 코드 아키텍처와 의존 방향은 전반적으로 적절하며 이전 구현보다 명확하게 개선됐다. 다만 현재 저장소는 Unity wiring 부재로 기능을 실행할 수 없고, persistent action failure가 매 프레임 재시도되는 High lifecycle 문제가 있다. scene/prefab 구성, failure backoff, target validity 및 핵심 Play Mode 검증이 완료되기 전에는 Phase A–D 전체 완료로 판정하지 않는다.