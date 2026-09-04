# Code Evaluation Result

## Purpose

NPCGirl 화물 바구니 등장·퇴장 애니메이션 변경을 대상으로 architecture, lifecycle/pooling, Animator 상태 제어, prefab·asset 직렬화, 코드 규칙을 감사했다. 리뷰만 수행했으며 파일은 수정하지 않았다.

## Review Snapshot

- Date: 2026-09-04
- Scope:
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs`
  - `Assets/Scripts/System/Actor/NPCComponent.cs`
  - `Assets/Prefab/InGame/NPCGirl.prefab`
  - `Assets/Animation/Carry/*`
  - `Assets/TestOnly/Editor/NPCGirlCarryVisualConfigurator.cs`
  - `PublicMD/Systems/NPC_Presentation.md`
  - `PublicMD/Systems/Inventory_and_Items.md`
  - `PublicMD/Status/PROGRESS.md`
- Direct dependency surfaces:
  - `WorkerInventory`, `WorkerNPC`, `WorkerPool`
  - `HarvestAction`, `DepositAction`, `WarehouseDepositPoint`
  - 기존 NPCGirl main controller/animation binding
  - `Enemy.prefab`의 optional presentation 배선
- Standards:
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/Systems/NPC_Presentation.md`
  - `PublicMD/Systems/Inventory_and_Items.md`
  - `PublicMD/Systems/NPC_Runtime.md`
  - `PublicMD/Systems/Spawning_and_Pooling.md`

## Executive Summary

책임 배치는 적절하다. 화물 보유 여부는 계속 `WorkerInventory`가 결정하고, `NPCComponent`는 의미 기반 presentation adapter로 남으며, 신규 `CarryVisualPresenter`가 전용 Animator 재생만 소유한다. `HarvestAction`과 `DepositAction`에는 Animator 의존성이 추가되지 않았고 gameplay transaction 계약도 변경되지 않았다.

Prefab과 animation asset의 정적 배선도 전반적으로 건전하다. `CarryAnchor -> CarryMotion -> Basket` 계층, controller·presenter 참조, sprite fileID, clip GUID, local fileID가 일치한다. 메인 Animator와 화물 Animator가 같은 Transform 속성을 제어하지 않으며 `CarryMotion`의 serialized scale `(0,0,1)`도 초기 숨김 상태를 제공한다.

다만 `ResetImmediate()`가 `Hidden`의 short-name hash를 `Animator.Play`에 전달한다. Unity의 API 계약은 state hash를 만들 때 부모 layer 이름을 포함하도록 요구하므로, 현재 구현은 즉시 Hidden 복구를 보장하지 못한다. 현재 구조에서 가장 중요한 수정 필요 항목이다.

최종 판정은 **Changes requested**이다.

## Improvements Since Previous Review

- 이전 보고서에서 integration gap으로 남았던 화물 visual과 prefab 배선이 완료됐다.
- `_carryRenderer`가 제거되고 `_carryPresenter`로 책임이 명확하게 이전됐다.
- prefab에 `_cargoCapacity: 10`과 `_carryPresenter`가 명시적으로 직렬화됐다.
- 화물 motion이 locomotion/tool animation과 별도 Animator 및 별도 Transform에 배치됐다.
- 초기 basket 노출 위험이 `CarryMotion`의 serialized hidden scale로 해소됐다.
- 이전 보고서의 Farming validation, cost 누락 반복, Deposit retry 정책 등은 이번 focused review에서 재검증하지 않았으며 이 변경으로 해결된 것으로 간주하지 않는다.

## Priority Coverage

| Priority | Result |
|---|---|
| 1. Architecture and responsibility placement | None found. 문서화된 presentation 경계와 dependency direction을 준수한다. |
| 2. Correctness, lifecycle, cancellation, regression | M-01 발견. `ResetImmediate`의 state hash가 Unity API 계약과 맞지 않는다. |
| 3. Unity scene, prefab, serialized-reference safety | 현재 YAML 참조 파손은 없음. 구성 drift 대응 문제는 L-01에 기록했다. |
| 4. Convention, maintainability, dead code, magic values | 별도 dead field나 부적절한 gameplay magic value는 발견되지 않았다. |

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

#### M-01 — `ResetImmediate()`가 layer를 포함하지 않은 state hash를 사용한다

- Severity: Medium
- Category: Correctness / Animator lifecycle / Pool reuse
- Location:
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:13`
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:28`
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:67`
  - `Assets/Animation/Carry/NPCGirl_Carry.controller:20`
- Evidence:
  - `HiddenStateHash`는 `Animator.StringToHash("Hidden")`로 생성된다.
  - controller의 layer 이름은 `Base Layer`다.
  - `ResetImmediate()`는 이 short-name hash를 `Animator.Play(..., 0, 0f)`에 전달한다.
  - Unity는 `Animator.Play`의 state 이름 또는 hash 생성 문자열에 부모 layer 이름을 포함하도록 명시한다. 즉 이 상태는 `Base Layer.Hidden`으로 식별해야 한다. [Unity Animator.Play documentation](https://docs.unity3d.com/ScriptReference/Animator.Play.html)
  - 같은 저장소의 `CropVisualAnimator`는 `Base Layer.Appear`, `Base Layer.Disappear`, `Base Layer.Leaf Sway`의 full-path hash를 `HasState`와 `Play`에 사용한다.
- Description:
  - 현재 `HiddenStateHash`는 `Animator.Play`가 요구하는 full state path를 나타내지 않는다.
  - 따라서 `ResetImmediate()`의 핵심 계약인 “현재 animation/transition과 무관하게 즉시 Hidden 상태로 이동”이 Unity API 계약상 보장되지 않는다.
  - `Awake()`의 `HasState` 검사도 동일한 hash를 사용하므로 올바른 controller를 잘못된 구성으로 진단할 가능성이 있다.
- Recommended fix:
  - hash를 `Animator.StringToHash("Base Layer.Hidden")`으로 생성하고 `HasState`와 `Play`에서 동일하게 사용한다.
  - Play Mode 검증에서 `ResetImmediate()` 직후 `GetCurrentAnimatorStateInfo(0).fullPathHash`가 해당 hash인지 확인한다.
  - visible, Show 진행 중, Hide 진행 중 각각에서 active re-init과 disable/re-enable을 검증한다.
- Impact if unfixed:
  - active object의 re-Init 또는 reset에서 이전 Visible/Show/Hide pose가 즉시 제거되지 않을 수 있다.
  - 상태를 찾지 못했다는 Animator Console 오류가 발생할 수 있다.
  - 일반 pool disable/re-enable은 prefab의 `m_KeepAnimatorStateOnDisable: 0`과 default Hidden 상태로 일부 완화되지만, `ResetImmediate()` 자체의 문서화된 보장은 충족되지 않는다.

### Low

#### L-01 — Animator 구성 실패를 진단한 뒤에도 presenter가 계속 잘못된 controller를 구동한다

- Severity: Low
- Category: Configuration safety / Maintainability
- Location:
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:20-31`
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:38-50`
  - `Assets/Scripts/System/Actor/CarryVisualPresenter.cs:57-68`
- Evidence:
  - `Awake()`는 Animator 참조와 Hidden state만 검사하며 필수 bool parameter `HasCargo`는 검사하지 않는다.
  - `ReportConfigurationFailure()`는 로그 latch만 설정하고 configured/disabled 상태를 기록하지 않는다.
  - 구성 실패 후에도 `SetVisible()`은 `Animator.SetBool`을, `ResetImmediate()`는 `SetBool`, `Play`, `Update`를 계속 호출한다.
  - 현재 controller YAML에는 올바른 `HasCargo` bool parameter가 있으므로 현재 prefab에서 활성화된 결함은 아니다.
- Description:
  - 향후 controller 누락, 잘못된 controller 재배선 또는 parameter 이름/type 변경이 발생하면 초기 진단 뒤 안전하게 presentation을 중단하지 못한다.
  - 이는 필수 dependency 오류를 한 번 진단하고 안전하게 비활성화하거나 실패하라는 `CodeConvention.md`의 Unity dependency 규칙보다 약하다.
- Recommended fix:
  - 초기화 시 Animator, runtime controller, `Base Layer.Hidden`, `HasCargo` bool parameter를 한 번 검증하고 명시적인 configured 상태를 저장한다.
  - validation 실패 시 animation 호출을 중단하고 basket을 안전한 hidden 상태로 유지하거나 presenter를 비활성화한다.
  - editor validator는 현재 asset 정합성 검증용으로 유지하되 runtime 안전 정책을 대신하도록 의존하지 않는다.
- Impact if unfixed:
  - 이후 prefab/controller drift가 발생하면 basket animation이 조용히 멈추거나 Animator parameter/state 오류가 발생할 수 있다.
  - gameplay는 계속 동작하지만 presentation 오류의 원인과 실제 안전 상태가 불명확해진다.

## Findings By File

- `Assets/Scripts/System/Actor/CarryVisualPresenter.cs`
  - presentation-only 책임과 idempotent state guard는 적절하다.
  - M-01의 state hash와 L-01의 fail-open 구성 처리가 수정 대상이다.
- `Assets/Scripts/System/Actor/NPCComponent.cs`
  - inventory callback을 presenter로 전달하는 경계가 적절하다.
  - `_cargo.Clear()` 뒤 `ResetImmediate()`를 호출하는 순서는 의도와 일치한다.
  - Enemy처럼 presenter가 없는 role도 null-safe하게 유지된다.
- `Assets/Prefab/InGame/NPCGirl.prefab`
  - `_carryPresenter`, presenter의 `_animator`, controller, sprite가 모두 유효한 fileID/GUID로 연결된다.
  - `Visual -> CarryAnchor -> CarryMotion -> Basket` 부모·자식 참조가 양방향으로 일치한다.
  - `CarryMotion`의 기본 local scale은 `(0,0,1)`이다.
- `Assets/Animation/Carry/*`
  - controller는 `HasCargo` bool 하나, 4개 state와 의도한 4개 transition을 가진다.
  - 모든 clip은 Animator root 기준 `CarryMotion`만 바인딩한다.
  - `CarryMotion` CRC32 `273173623`과 각 clip의 binding constant가 일치한다.
- `Assets/TestOnly/Editor/NPCGirlCarryVisualConfigurator.cs`
  - parameter, default state, 필수 transition과 motion 존재 여부를 검사한다.
  - 실제 실행 결과와 prefab/clip binding 검증은 아직 확인되지 않았다.
- `PublicMD/Systems/NPC_Presentation.md`
  - 현재 책임, 흐름, prefab 계층 및 known edge case를 구현과 일치하게 설명한다.
  - M-01이 해결되기 전에는 `ResetImmediate()`가 즉시 Hidden을 보장한다는 서술을 검증 완료 사실로 볼 수 없다.
- `PublicMD/Systems/Inventory_and_Items.md`
  - `_carryRenderer`에서 `_carryPresenter`로 변경된 배선을 정확히 반영한다.

## Cross-Cutting Findings

- `WorkerNPC`는 queue lifecycle 전용 책임을 유지한다.
- dependency 방향은 `WorkerInventory -> callback -> NPCComponent -> CarryVisualPresenter -> Animator`로 제한된다.
- `HarvestAction`, `DepositAction`, `WarehouseDepositPoint`에는 Animator나 visual hierarchy 의존성이 없다.
- gameplay transaction과 presentation 완료 시간이 결합되지 않았다.
- `_carryRenderer` 잔존 참조는 `Assets`에서 0건이다.
- animation timing과 curve 값은 animation asset에 응집되어 있으며 gameplay code의 magic duration으로 중복되지 않았다.

## Positive Notes

- 별도 Animator와 별도 bound Transform을 사용한 선택은 property ownership 충돌을 피한다.
- main NPC animation은 `Visual` 및 `Visual/ToolAnchor/Tool`만, carry animation은 `CarryMotion`만 바인딩한다.
- `SetVisible(bool)`의 equality guard는 반복 수확 callback이 Show를 재생하는 것을 방지한다.
- `WorkerInventory`의 add/transfer/clear callback 계약은 변경되지 않았다.
- Basket의 기존 SpriteRenderer fileID와 sprite GUID가 보존됐다.
- prefab, controller 및 4개 clip에서 duplicate YAML anchor와 unresolved local fileID 참조가 발견되지 않았다.
- `.meta` GUID와 controller/prefab의 cross-asset 참조가 일치한다.
- `git diff --check`는 현재 변경에서 통과했다.
- 신규 production type과 editor validator가 각각 Unity-generated csproj compile item 및 최근 build artifact에 포함된 것을 확인했다.

## Verification Limits

- read-only 지시에 따라 `dotnet build`를 다시 실행하지 않았다. 기존 csproj에는 신규 두 C# 파일의 compile item이 있고 최근 DLL artifact에도 두 type이 포함되어 있으나, 보고된 “0 warnings / 0 errors” 출력 자체는 이번 리뷰에서 독립적으로 재현하지 않았다.
- Unity Editor/Play Mode를 실행하지 않았으므로 Show/Hide 실제 재생, visual offset, transition timing, pool 두 번째 대여와 Console 무오류 상태는 검증하지 못했다.
- 신규 및 기존 세 Animator validator 메뉴를 실행하지 않았다.
- `Animator.Update(0f)`가 `WorkerNPC.OnDisable` 경로에서 호출될 때의 실제 동작은 Play Mode 검증이 필요하다.
- `Code_Evaluation_Result.md`와 basket PNG/meta의 변경 시점·작성 주체는 현재 git snapshot만으로 구분할 수 없어 사용자 제공 범위대로 기존 변경으로 취급했다.
- 기존 보고서의 Farming 관련 findings는 이번 focused review 범위 밖이며 해소 여부를 재판정하지 않았다.

## Recommended Next Actions

1. M-01의 Hidden hash를 `Base Layer.Hidden` full-path hash로 수정한다.
2. L-01의 runtime controller/parameter validation과 safe disabled 상태를 추가한다.
3. Unity Editor에서 신규 carry validator와 기존 main/tool validator를 실행한다.
4. Play Mode에서 최초 수확, 반복 수확, 부분·전량 입고, Show/Hide 도중 reset, active re-Init, disable/re-enable, pool 두 번째 대여를 검증한다.
5. Console에 state/parameter/missing-binding 오류가 없음을 확인한 뒤 완료 처리한다.

## Final Verdict

**Changes requested.**

Architecture와 serialized wiring은 승인 가능한 수준이며 gameplay 책임 오염이나 참조 파손은 발견되지 않았다. 그러나 `ResetImmediate()`의 state hash가 Unity의 full-path 규칙을 따르지 않아 핵심 reset 계약을 보장하지 못한다. M-01을 수정하고 Unity Play Mode에서 즉시 Hidden 복구와 실제 Show/Hide 재생을 확인하기 전에는 완료로 닫지 않는 것이 안전하다.