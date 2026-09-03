# Code Evaluation Result

## Purpose

FarmCropPresenter crop presentation slice를 대상으로 architecture·책임 경계, runtime lifecycle, Unity 직렬화 배선, changeset 완결성과 문서 일치 여부를 읽기 전용으로 감사했다.

## Review Snapshot

- Date: 2026-09-01
- Scope:
  - 요청에 명시된 Farming C#·ScriptableObject·scene·문서 변경
  - `FarmWorkSite`, `FarmCropPresenter`, `CropVisualAnimator`, `CropVisualStage`
  - Carrot/Potato definition과 직접 참조하는 controller·sprite·crop prefab
  - 직접 dependency surface인 `BaseInteractionProvider`, `IInventory`, `WarehouseInventory`, `ItemDataContext`, `TestFarmProductionWindow`
- Standards:
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ARCHITECTURE.md`
  - Farming의 네 leaf 문서
  - Inventory·Interaction의 직접 관련 계약
- Repository checks:
  - `git status`, tracked·cached diff, target `git diff --check`
  - script·asset GUID resolution과 scene/prefab reference 검색
  - 금지 pattern·직접 사용처·Markdown link 검색
- Excluded unrelated worktree changes:
  - `GuardTest.unity`, NPC animation, agent skill, NPC prefab catalog, Recovery assets 등
- Verification:
  - 대상 tracked diff의 `git diff --check`는 통과했다.
  - 선택 문서의 local Markdown link는 모두 존재한다.
  - Carrot/Potato controller·stage GUID는 현재 local workspace에서 각각 정확히 한 `.meta`로 해석된다.
  - 사용자 보고상 `dotnet build`는 0 warnings / 0 errors다. 읽기 전용 조건상 빌드는 독립 재실행하지 않았다.
  - crop visual scene 배선과 Play Mode 동작은 검증되지 않았다.

## Executive Summary

`FarmWorkSite`가 상태만 소유하고 `FarmCropPresenter`가 read-only event subscriber로 동작하는 기본 책임 배치는 적절하다. stage command queue, 별도 visual random stream, disable 시 구독·coroutine 정리와 re-enable 즉시 동기화도 코드상 일관된다. 최종 수확 전에 필요한 definition 값을 지역 변수에 보존한 뒤 current crop을 즉시 비우는 변경도 안전하다.

그러나 현재 S-02는 실행 가능한 상태로 통합되지 않았다. `FarmCropPresenter`와 `CropVisualAnimator` script GUID는 어떤 scene이나 prefab에도 참조되지 않으며, `FarmerTest.unity` 변경도 Phase 1 test window 배선만 포함한다. 따라서 현재 빌드에서는 `StateChanged`를 소비하는 component가 없고 crop presentation 기능이 실행되지 않는다.

또한 새 production C#·meta, Farming leaf 문서와 definition이 참조하는 stage sprite 다수가 Git 미추적 상태이며 cached diff는 비어 있다. tracked 변경만 전달하면 `CropVisualStage` type이 누락되어 컴파일이 깨지고 새 문서 routing과 sprite reference도 불완전해진다.

Architecture 측면에서는 `FarmProductionDefinition.IsValid`가 production 규칙과 presentation 유효성을 하나로 묶어 `FarmWorkSite`가 visual controller·sprite 누락 때문에 crop 선택과 farming interaction을 거부하게 됐다. 이는 presentation 실패가 gameplay transaction에 영향을 주지 않아야 한다는 문서화된 경계와 충돌한다.

최종 판정은 **Changes requested — 구조적 기반은 양호하지만 S-02 runtime integration 및 changeset 완결성이 미완료**다.

## Improvements Since Previous Review

- `ItemDataContext.TryGetItemInfo`와 `CropCatalog.TryValidate(ItemDataContext)`가 output item cross-reference를 검사한다.
- `TestFarmProductionWindow`가 catalog validation을 한 번 실행하며 `FarmerTest`에 실제 배선됐다.
- 부분 수락 시 `_pendingYield`에서 실제 수락량을 차감하여 전체 yield 중복 재요청 위험을 줄였다.
- Phase 1 신규 production 파일은 현재 Git 추적 대상이다.
- Carrot/Potato 선택·진행·입고 Play Mode 검증 결과가 문서에 기록됐고 S-01 상태가 `completed`로 정리됐다.
- Farming 문서가 네 leaf로 분리됐으며 각 production C#의 주 소유 문서가 명확하다.
- crop presentation이 NPC selector, action과 inventory transaction에 직접 의존하지 않는다.

## Priority Assessment

### 1. Architecture and Responsibility Placement

M-01이 확인됐다. Shared definition에 production과 presentation 값을 함께 두는 것은 허용 가능하지만, gameplay 실행 유효성까지 presentation 데이터에 종속시킨 것은 책임 경계를 넘는다.

### 2. Correctness, Lifecycle, Cancellation, and Regression Risks

확인된 독립 결함은 없다.

정적 코드 기준으로 다음 경로는 일관된다.

- 성공한 상태 변경 뒤에만 `StateChanged`가 발행된다.
- 최종 수확은 definition 값을 보존한 뒤 current crop을 즉시 제거한다.
- 여러 threshold를 넘으면 각 stage command를 순서대로 한 번씩 queue한다.
- harvest 중에는 마지막 visual stage를 유지한다.
- `OnDisable`에서 subscription과 presenter-owned coroutine·queue를 정리한다.
- `OnEnable`에서 현재 runtime state로 즉시 재동기화한다.
- visual RNG는 `System.Random`을 사용해 production random stream과 분리된다.

다만 실제 Animator timing, 빠른 다중 threshold 진행, 수확 중 새 crop 선택과 disable/re-enable은 Play Mode에서 검증되지 않았다.

### 3. Unity Scene, Prefab, Component, and Serialized-Reference Safety

H-01과 H-02가 확인됐다. Local GUID 자체는 해석되지만 신규 component의 runtime 배선과 changeset 포함 상태가 모두 미완료다.

### 4. Code Convention, Maintainability, Dead Code, and Magic Values

별도 finding은 없다.

- Inspector dependency는 serialized private field다.
- cascade 값은 named constant와 serialized tuning으로 표현된다.
- Animator state hash는 static readonly로 캐시된다.
- scene search, repeated component lookup, `UnityEngine.Random`, per-frame LINQ가 없다.
- null·duplicate visual은 cache 구축 시 한 번 진단한다.
- component가 미배선되어 현재 runtime에서 도달하지 못하는 문제는 H-01에서 다룬다.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — Crop presentation component가 어떤 scene이나 prefab에도 배선되지 않았다

- Severity: High
- Category: Unity integration / Serialized-reference safety
- Location:
  - `Assets/Scripts/System/Farming/FarmCropPresenter.cs`
  - `Assets/Scripts/System/Farming/CropVisualAnimator.cs`
  - `Assets/Scripts/System/Farming/FarmCropPresenter.cs.meta:2`
  - `Assets/Scripts/System/Farming/CropVisualAnimator.cs.meta:2`
  - `Assets/Scenes/FarmerTest.unity:1267-1285`
  - `PublicMD/PLAN.md:45`
  - `PublicMD/Plans/Seed_System_Implementation_Plan.md:153-166`
- Evidence:
  - `FarmCropPresenter` GUID `c1b9efbd29444a108e630c65debb3702`는 자체 `.meta` 외에 repository 내 참조가 없다.
  - `CropVisualAnimator` GUID `e43ab01182bd4c4098d39cb81d425b1f`도 자체 `.meta` 외에 참조가 없다.
  - 모든 `.unity`·`.prefab` 대상 검색 결과 두 GUID의 serialized reference는 0건이다.
  - `FarmCropPresenter`·`CropVisualAnimator`를 runtime에서 동적으로 추가하는 코드도 없다.
  - `FarmerTest.unity`의 변경은 `TestFarmProductionWindow`와 Phase 1 dependency 배선만 추가한다.
  - 계획 문서는 약 10개 visual과 presenter 배선이 아직 수동 작업이라고 명시한다.
- Description:
  - `FarmWorkSite.StateChanged`는 정상 발행되지만 이를 소비하는 runtime instance가 없다.
  - 따라서 sprite stage, cascade, idle, 수확 disappear와 lifecycle resync 중 어떤 기능도 현재 scene에서 실행되지 않는다.
  - 이는 단순히 Play Mode 검증이 빠진 상태가 아니라 runtime integration 자체가 빠진 상태다.
- Recommended fix:
  - 실제 farming scene 또는 crop visual prefab에 `FarmCropPresenter`와 각 `CropVisualAnimator`를 추가한다.
  - `_workSite`, `_visuals`, 각 `_animator`·`_spriteRenderer`를 명시적으로 배선한다.
  - scene/prefab YAML을 changeset에 포함하고 S-02 상태를 그전까지 `implementation/integration pending`으로 기록한다.
  - 배선 후 threshold 전환, 최종 수확, 즉시 새 crop 선택, disable/re-enable을 Play Mode에서 검증한다.
- Impact if unfixed:
  - S-02의 사용자 가시 기능이 전혀 실행되지 않는다.
  - `implementation complete / verification pending` 상태가 실제 repository 상태를 과대평가한다.
  - S-02 완료 조건을 검증할 수 없으며 후속 S-03 작업이 비활성 presentation 기반 위에서 진행된다.

#### H-02 — 필수 신규 코드·sprite·문서가 Git 미추적 상태라 tracked changeset만으로는 빌드와 asset routing이 깨진다

- Severity: High
- Category: Changeset completeness / Unity asset delivery
- Location:
  - `Assets/Data/Struct/CropVisualStage.cs`와 `.meta`
  - `Assets/Scripts/System/Farming/CropVisualAnimator.cs`와 `.meta`
  - `Assets/Scripts/System/Farming/FarmCropPresenter.cs`와 `.meta`
  - `Assets/Art/Generated/Rigged/Carrot_Progress1.png`~`Progress3.png`와 `.meta`
  - `Assets/Art/Generated/crop-potato-single-stage-01.png`~`04.png`와 `.meta`
  - `PublicMD/Systems/Farming/`
  - `Assets/Data/ScriptableObject/FarmProductionDefinition_Carrot.asset:24-32`
  - `Assets/Data/ScriptableObject/FarmProductionDefinition_Potato.asset:24-32`
  - `PublicMD/Plans/Seed_System_Implementation_Plan.md:125`
- Evidence:
  - `git status --untracked-files=all`에서 위 신규 production code, meta, referenced sprite와 Farming leaf 문서가 `??`로 표시된다.
  - `git diff --cached --name-status`는 비어 있다.
  - tracked `FarmProductionDefinition.cs`는 untracked `CropVisualStage` type을 직접 참조한다.
  - tracked Carrot/Potato assets는 미추적 sprite GUID 7개를 참조한다.
  - tracked `ProjectStructure.md`는 새 `Systems/Farming/README.md`로 routing하고 기존 `Systems/Farming.md`는 삭제 상태지만 새 폴더는 미추적이다.
  - Seed plan은 Phase 1 관련 신규 파일이 stage됐다고 기록하지만 현재 index에는 staged entry가 없다.
- Description:
  - 현재 dirty workspace에서는 파일이 존재하므로 csproj와 GUID 검색이 성공한다.
  - tracked diff나 commit 기준으로 전달하면 `CropVisualStage` 정의가 없어 C# compile이 실패하고 crop sprite reference와 Farming 문서 link가 누락된다.
  - 사용되지 않는 추가 carrot sprite도 미추적 상태이므로 실제 필요 asset과 작업 중 산출물을 구분해야 한다.
- Recommended fix:
  - S-02에 실제 필요한 C#·meta, 참조되는 sprite·meta, Farming leaf 문서와 scene/prefab integration만 명시적으로 stage한다.
  - 사용하지 않는 `crop-carrot-single-stage-*` 산출물은 changeset 필요성을 별도로 판단한다.
  - clean checkout 또는 staged-only 기준으로 compile, GUID resolution, Markdown links와 scene reference를 다시 확인한다.
  - Seed plan의 “staged” 기록은 실제 index 상태와 일치시키거나 staging 전에는 제거한다.
- Impact if unfixed:
  - 다른 환경이나 clean checkout에서 build가 실패한다.
  - crop definition에 Missing sprite가 발생한다.
  - `ProjectStructure`가 존재하지 않는 Farming 문서를 가리킨다.
  - local workspace 검증 결과를 재현할 수 없다.

### Medium

#### M-01 — Farming gameplay 유효성이 presentation asset 유효성에 종속됐다

- Severity: Medium
- Category: Architecture / Responsibility and dependency direction
- Location:
  - `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs:35-38`
  - `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs:81-105`
  - `Assets/Scripts/System/Farming/FarmWorkSite.cs:64-68`
  - `Assets/Scripts/System/Farming/FarmWorkSite.cs:89-90`
  - `PublicMD/Systems/Farming/README.md`
  - `PublicMD/Systems/Farming/Crop_Presentation.md`
  - `PublicMD/Plans/Seed_System_Implementation_Plan.md:159`
- Evidence:
  - `FarmProductionDefinition.IsValid`는 production 값뿐 아니라 `HasValidPresentation()`도 요구한다.
  - `HasValidPresentation()`은 controller, 최소 두 stage, sprite, threshold 배열이 모두 유효해야 성공한다.
  - `FarmWorkSite.TrySelectCrop`과 `CanInteractCore`는 이 통합 `IsValid`를 사용한다.
  - 따라서 controller 또는 sprite 하나가 누락되면 crop selection이 거부되고 이미 선택된 farm도 interaction 불가가 된다.
  - Farming 문서는 presentation 부재나 비활성화가 gameplay transaction에 영향을 주지 않아야 한다고 규정한다.
- Description:
  - 하나의 ScriptableObject에 production과 presentation reference를 보관하는 것은 source-of-truth 측면에서 타당하다.
  - 그러나 transaction owner가 presentation의 완전성을 실행 전제에 포함하면서 presentation failure가 production을 중단시킨다.
  - 현재 Carrot/Potato asset은 local workspace에서 유효하므로 즉시 재현되는 데이터 결함은 아니지만, 향후 art reference 손실·headless test·presentation 없는 crop 정의가 gameplay 장애로 확대된다.
- Recommended fix:
  - definition validation을 `IsProductionValid`와 `HasValidPresentation`처럼 책임별로 분리한다.
  - `FarmWorkSite`는 production validation만 사용한다.
  - `CropCatalog`의 전체 content validation 또는 `FarmCropPresenter` 초기화 경계에서는 production과 presentation을 모두 검증한다.
  - invalid presentation은 presenter만 안전하게 비활성화하고 farming transaction은 유지한다.
- Impact if unfixed:
  - art asset 누락이나 Animator 변경이 농사 생산을 중단시킨다.
  - gameplay와 presentation 사이의 dependency direction이 흐려진다.
  - presentation 없는 자동 test definition을 만들기 어렵고 이후 crop 추가 시 실패 범위가 불필요하게 커진다.

### Low

None found.

## Findings By File

- `FarmWorkSite.cs`
  - runtime crop·phase·progress·yield transaction 소유 위치는 적절하다.
  - 성공 변경 뒤 event 발행과 최종 수확 즉시 clear가 안전하다.
  - M-01의 통합 `IsValid` 소비가 적용된다.
- `FarmCropPresenter.cs`
  - event subscription, command queue, shuffle와 farm 단위 visual orchestration 책임이 응집돼 있다.
  - disable/re-enable 정리와 즉시 resync 경로가 존재한다.
  - H-01과 H-02 때문에 현재 runtime에서 사용되지 않는다.
- `CropVisualAnimator.cs`
  - 단일 visual의 Animator/SpriteRenderer adapter 역할에 한정돼 있다.
  - 상태 존재 여부와 필수 reference를 검사하고 반복 오류를 latch한다.
  - H-01과 H-02가 적용된다.
- `CropVisualStage.cs`
  - 작은 serializable value로 배치가 적절하다.
  - H-02가 적용된다.
- `FarmProductionDefinition.cs`
  - 공유 production·presentation reference를 asset에 저장하는 위치는 적절하다.
  - M-01처럼 두 validation 의미를 하나의 `IsValid`로 합친 것은 수정이 필요하다.
- `CropCatalog.cs`
  - duplicate crop과 output item cross-reference를 검사하며 이전 dead validation 문제가 개선됐다.
- Carrot/Potato definition assets
  - 두 asset 모두 `0 / 0.33333334 / 0.6666667 / 1` 네 stage를 가진다.
  - 모든 local GUID는 정확히 한 `.meta`로 해석된다.
  - H-02의 미추적 sprite delivery 위험이 적용된다.
- `FarmerTest.unity`
  - Phase 1 `TestFarmProductionWindow` dependency는 직렬화돼 있다.
  - H-01처럼 crop visual component와 배열 배선은 없다.
- Farming documentation
  - leaf 소유권과 변경 유형별 최소 범위가 명확하다.
  - H-01의 integration 상태와 H-02의 staging 기록 불일치를 교정해야 한다.

## Cross-Cutting Findings

- `FarmWorkSite -> StateChanged -> FarmCropPresenter` 방향은 domain runtime이 concrete presentation을 참조하지 않는 올바른 의존 방향이다.
- production과 presentation이 동일 crop definition을 source of truth로 사용하는 선택은 합리적이지만 validation 소비자는 책임별로 분리해야 한다.
- local asset GUID 완전성과 deliverable changeset 완전성은 별개다. 현재는 전자는 통과하지만 후자는 실패한다.
- component가 scene/prefab에 존재하지 않으면 C# compile 성공만으로 presentation slice 구현 완료를 판단할 수 없다.
- 이번 변경은 Farming 내부 presentation slice이므로 새로운 cross-system runtime owner나 공통 interface 추가는 필요하지 않다.

## Positive Notes

- NPC, selector와 `FarmingAction`에 crop stage·sprite 지식이 추가되지 않았다.
- `FarmWorkSite`는 concrete presenter를 참조하지 않는다.
- final harvest 시 log와 progress 계산에 필요한 definition 값을 clear 전에 보존한다.
- visual random은 production `IRandomSource`와 완전히 분리돼 yield 결정성을 오염시키지 않는다.
- stage가 여러 개 상승하면 누락 없이 순차 command를 만든다.
- harvesting 중 progress 감소는 visual stage를 역행시키지 않는다.
- duplicate/null visual은 cache 구축 시 한 번만 진단하고 제외한다.
- Animator controller에는 `Appear`, `Disappear`, `Leaf Sway`가 존재하며 appear/disappear clip은 non-looping이다.
- Carrot stage sprite는 동일한 3-bone 이름·topology를 유지한다.
- cascade interval `0.08f`는 named default와 serialized tuning으로 관리된다.
- 대상 코드에는 scene-wide `Find*`, repeated `GetComponent`, `UnityEngine.Random`, per-frame allocation loop가 없다.
- Farming의 네 production 책임이 각각 하나의 leaf에서 주 소유된다.

## Verification Limits

- 읽기 전용 조건 때문에 `dotnet build`를 재실행하지 않았다. 0 warnings / 0 errors는 사용자 제공 결과다.
- Unity Editor import, console warning과 Play Mode를 실행하지 않았다.
- stage threshold 전후의 정확한 animation 횟수, Animator transition timing과 idle loop는 정적으로만 확인했다.
- 수확 중 새 crop 선택, 빠른 다중 threshold, disable/re-enable과 scene reload는 Play Mode 미검증이다.
- Potato의 leaf sway는 rig 부재로 정적으로 보일 수 있으며 실제 화면 결과를 확인하지 않았다.
- global `git diff --check`에는 범위 밖 `GuardTest.unity`의 trailing whitespace가 존재한다. 요청된 대상 tracked diff는 별도로 검사해 통과했다.
- production scene이 명확히 지정되지 않았으므로 모든 scene/prefab을 GUID 검색해 신규 component 참조가 0건임을 확인했다.

## Recommended Next Actions

1. production validation과 presentation validation을 분리해 `FarmWorkSite`가 art reference에 종속되지 않게 한다.
2. 필요한 신규 C#·meta, 실제 참조 sprite·meta와 Farming leaf 문서를 명시적으로 stage한다.
3. farming scene 또는 owning prefab에 `FarmCropPresenter`와 `CropVisualAnimator` 배열을 배치·배선한다.
4. staged-only 또는 clean checkout 기준으로 compile, GUID, Markdown link와 serialized reference 검사를 다시 실행한다.
5. Play Mode에서 네 stage, 빠른 threshold 횡단, 최종 disappear 1회, 소멸 중 새 crop 선택, disable/re-enable와 Carrot/Potato 교체를 검증한다.
6. 배선 전에는 S-02를 `implementation/integration pending`으로 기록하고, 검증 통과 후에만 완료 처리한다.

## Final Verdict

**Changes requested.**

Architecture의 기본 방향과 lifecycle 코드는 양호하지만, crop presentation이 runtime에 전혀 배선되지 않았고 필수 신규 파일이 changeset에 포함되지 않았다. 또한 gameplay validation과 presentation validation의 결합을 해소해야 한다. H-01·H-02를 해결하고 M-01을 교정 또는 명시적으로 설계 승인한 뒤 Play Mode 검증을 통과하기 전에는 S-02를 완료로 닫거나 S-03 production 구현을 시작하면 안 된다.

---

## Documentation Coverage Audit — 2026-09-02

### Purpose

현재 프로젝트 소유 C# 스크립트가 `ProjectStructure.md`의 구조 지도와 그 문서가 연결하는 `PublicMD/Systems` leaf 문서에 빠짐없이 라우팅되는지 확인했다. 이번 부록은 위 Farming 변경 리뷰를 대체하지 않는다.

### Review Snapshot

- Scope: `Assets/Scripts`, `Assets/Data`, `Assets/TestOnly`의 C# 및 `PublicMD/ProjectStructure.md`, 모든 `PublicMD/Systems` 문서
- Excluded: `Assets/Plugins/Demigiant/DOTween`의 third-party module 10개와 `Assets/_Recovery`
- Verification: production C# 95개와 각 leaf의 `주 소유 스크립트` 표를 경로 기준 대조, TestOnly 7개의 기능 문서 언급 확인, `dotnet build Assembly-CSharp.csproj --no-restore`
- Build: 0 warnings, 0 errors

### Executive Summary

production C# 95개 중 94개는 정확히 하나의 Systems leaf 또는 단일 기능 문서에 주 소유가 등록되어 있다. 중복 소유와 문서에만 남은 삭제 파일은 없다. 유일한 누락은 `Assets/Data/Class/CONST.cs`이며, 해당 폴더도 `ProjectStructure.md`의 최상위 폴더 책임 표에 없다. 이 파일의 `DefineString.Define.COSNT_ASDF` 상수는 프로젝트 내 사용처가 없어 현재는 문서화 누락인 동시에 dead placeholder 후보로 판단된다.

TestOnly C# 7개는 `ProjectStructure.md`에서 폴더 책임이 정의되어 있고 모두 관련 Systems 문서에 파일명 또는 전체 경로로 언급된다. 따라서 production 주 소유 표의 누락으로 계산하지 않았다.

### Findings By Severity

#### Critical

None found.

#### High

None found.

#### Medium

None found.

#### Low

##### L-01 — `CONST.cs`가 구조 지도와 Systems 주 소유 문서에서 누락되어 있다

- File: `Assets/Data/Class/CONST.cs`
- Evidence: production C# 95개 중 이 파일만 어떤 `주 소유 스크립트` 표에도 없고, `Assets/Data/Class`도 `ProjectStructure.md`의 최상위 폴더 책임 표에 없다.
- Additional evidence: `DefineString`과 `COSNT_ASDF`는 선언 파일 외 사용처가 없으며 파일 내용은 `"ASDF"` placeholder 상수 하나뿐이다.
- Recommendation: 실제 용도가 없다면 파일과 `.meta`를 삭제한다. 유지할 이유가 있다면 명확한 이름과 책임으로 교정하고 적절한 기존 폴더로 이동한 뒤 하나의 Systems 문서에 주 소유를 등록한다. 현재 상태 그대로 `ProjectStructure.md`에 새 공통 폴더를 추가하는 것은 권장하지 않는다.

### Cross-Cutting Findings

- production 문서 소유율은 94/95이며, 등록된 94개에는 중복 소유가 없다.
- Systems 문서에 등록됐지만 실제 파일이 없는 stale path는 없다.
- DOTween module은 외부 plugin code이므로 프로젝트의 기능 문서 소유 대상에서 제외했다.

### Positive Notes

- production C#의 one-leaf ownership 규칙이 `CONST.cs` 한 개를 제외하고 지켜지고 있다.
- TestOnly 도구도 관련 기능 문서의 배선·검증 항목에서 추적되고 있다.

### Recommended Next Actions

1. `CONST.cs`가 불필요한 placeholder인지 확인한 뒤 삭제를 우선 검토한다.
2. 유지한다면 파일명·namespace·상수명을 실제 책임에 맞게 바꾸고 기존 owning system으로 이동·등록한다.
