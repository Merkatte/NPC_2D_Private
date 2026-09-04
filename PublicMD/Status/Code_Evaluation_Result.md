# Code Evaluation Result

## Purpose

`GoldManager` 1단계 구현을 대상으로 책임 배치, transaction 정확성, Unity 직렬화 배선, 코드 규약과 문서 정합성을 감사했다. 상인 캐러밴과 주민 모집은 명시적인 후속 범위로 취급했으며, 범용 resource abstraction, `IGoldService`, singleton, 변경 event 부재는 finding으로 판단하지 않았다.

## Review Snapshot

- Date: 2026-09-05
- Scope:
  - `Assets/Scripts/Manager/GoldManager.cs` 및 `.meta`
  - `Assets/TestOnly/TestGoldWindow.cs` 및 `.meta`
  - `PublicMD/Systems/Player_Gold.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/Systems/README.md`
  - `PublicMD/Status/PROGRESS.md`
  - 직접 비교 표면: `WarehouseInventory`, 기존 TestOnly window, `FarmerTest.unity`, `GuardTest.unity`, Manager 폴더
- Standards:
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `PublicMD/Game_Plan.md`
  - `PublicMD/SPEC.md`
  - reviewing-npc-work-code audit methodology
- Verification:
  - `git status`, 변경 diff와 `git diff --check` 확인
  - API·static·logging·TestOnly 역참조·stale recruitment reference 검색
  - 모든 scene/prefab에서 신규 script GUID 및 component 참조 검색
  - 신규 문서 링크와 `.meta` GUID 확인
  - 읽기 전용 제약으로 build 및 Unity Editor/Play Mode는 재실행하지 않음

## Executive Summary

`GoldManager`의 production 코드는 작고 응집되어 있으며 transaction 구현도 정확하다. 잔액은 단일 private field에 유지되고, `TrySpend`는 실패 시 상태를 변경하지 않으며, `Add`는 overflow 대신 포화한다. static 접근, 불필요한 interface, UI 의존성, production-to-TestOnly 역참조는 없다.

다만 현재 모든 scene과 prefab에 `GoldManager` 및 `TestGoldWindow`가 전혀 배선되지 않았다. 따라서 저장소 상태 그대로는 Gold 시스템 인스턴스와 수동 검증 창이 런타임에 존재하지 않는다. production 코드 자체의 correctness finding은 없지만 이 integration gap 때문에 완료 상태로 승인할 수 없다.

최종 판정은 **Changes requested**이다.

## Improvements Since Previous Review

- 이전 보고서는 NPC 화물 presentation 변경을 다뤘으며 이번 Gold 변경과 직접 비교 가능한 항목은 없다.
- 이전 보고서의 `CarryVisualPresenter` findings는 이번 focused review 범위 밖이며 해소 여부를 재판정하지 않았다.
- 새 기능은 별도 Systems 문서, 상위 routing, 책임 경계와 TBD를 함께 추가해 문서 소유권이 명확하다.

## Priority Coverage

| Priority | Result |
|---|---|
| 1. Architecture and responsibility placement | None found. Gold의 scene 단위 단일 소유권과 명시적 참조 방향은 현재 구조에 부합한다. |
| 2. Correctness, lifecycle, cancellation, regression | None found in production code. `Awake` 초기화, 포화 덧셈과 all-or-nothing 지출이 정적으로 정확하다. cancellation 대상은 없다. |
| 3. Unity scene, prefab, serialized-reference safety | M-01 발견. 신규 component와 검증 창이 어떤 scene/prefab에도 배선되지 않았다. |
| 4. Convention, maintainability, dead code, magic values | L-01~L-03 발견. edit-time validation, 문서 사실성, 수동 probe의 상태 의존성이 남아 있다. Dead production code나 익명 gameplay magic value는 발견되지 않았다. |

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

#### M-01 — Gold runtime과 검증 창이 어떤 Unity scene에도 배선되지 않았다

- Severity: Medium
- Category: Unity integration / Serialized-reference safety
- Location:
  - `Assets/Scenes/FarmerTest.unity`
  - `Assets/Scenes/GuardTest.unity`
  - `PublicMD/Systems/Player_Gold.md:63-75`
  - `PublicMD/Status/PROGRESS.md:71`
- Evidence:
  - `GoldManager.cs.meta` GUID는 `583c72025a4048bbbd6ec3d016525f1f`다.
  - `TestGoldWindow.cs.meta` GUID는 `0e4f959f6abc483babe6c42d4a2a648e`다.
  - 모든 `.unity`와 `.prefab` 검색에서 두 GUID, `Assembly-CSharp::GoldManager`, `Assembly-CSharp::TestGoldWindow` 참조가 0건이다.
  - `FarmerTest.unity:475-481`의 `Systems > Manager` 자식 목록에는 기존 manager들만 있고 GoldManager 자식이 없다.
  - `FarmerTest.unity:628-650`의 `TestOnly!!`에는 `TestNPCSpawnWindow`만 부착되어 있다.
  - owning Systems 문서는 `FarmerTest.unity`에 두 component를 배선하도록 명시한다.
- Description:
  - 신규 API는 컴파일 대상이지만 scene에 인스턴스가 없어 런타임 Gold 상태가 생성되지 않는다.
  - 현재 유일한 호출자인 `TestGoldWindow`도 부착되지 않아 문서화된 관찰·검증 흐름을 실행할 수 없다.
  - 기존 gameplay consumer가 의도적으로 없는 것은 허용되지만, scene runtime owner와 이번 단계의 유일한 probe까지 모두 부재한 것은 integration 미완료다.
- Recommended fix:
  - `FarmerTest.unity`의 `Systems > Manager` 아래에 `GoldManager` component를 가진 GameObject를 추가하고 시작 골드를 명시적으로 직렬화한다.
  - `TestOnly!!`에 `TestGoldWindow`를 추가하고 `_goldManager`를 해당 component에 연결한다.
  - YAML의 두 script GUID와 `_goldManager` fileID가 정확히 연결되는지 확인한다.
  - 이후 최초 진입, 두 번째 Play Mode 진입, 획득, 성공 지출, 부족 지출, Console 무경고를 검증한다.
- Impact if unfixed:
  - 현재 저장소를 열어 실행해도 Gold 시스템과 테스트 창은 존재하지 않는다.
  - `Awake` 초기화와 transaction 동작, 상태 reset 및 Inspector 입력을 실제 Unity lifecycle에서 검증할 수 없다.
  - 후속 merchant/recruitment 작업이 아직 존재하지 않는 scene service를 전제로 시작할 위험이 있다.

### Low

#### L-01 — `_initialGold`에 프로젝트 규약이 요구하는 `OnValidate` 보정이 없다

- Severity: Low
- Category: Code convention / Serialized value validation
- Location:
  - `Assets/Scripts/Manager/GoldManager.cs:16`
  - `Assets/Scripts/Manager/GoldManager.cs:24-27`
  - `PublicMD/CodeConvention.md` §7
- Evidence:
  - `_initialGold`에는 `[Min(0)]`만 있고 `OnValidate()`가 없다.
  - runtime 잔액은 `Awake()`의 `Mathf.Max(0, _initialGold)`로 안전하게 보정된다.
  - 프로젝트 규약은 serialized 수치를 `OnValidate()`에서 보정하고 runtime 입력도 별도로 검증하도록 명시한다.
- Description:
  - Inspector UI 입력은 `Min`으로 제한되지만 YAML 직접 편집, migration 또는 script 기반 값 변경으로 음수가 저장될 수 있다.
  - runtime 잔액은 안전하므로 gameplay correctness 버그는 아니지만 serialized source와 실제 실행 값이 달라질 수 있다.
- Recommended fix:
  - `OnValidate()`에서 `_initialGold = Mathf.Max(0, _initialGold)`로 serialized 값을 보정한다.
  - `Awake()`의 runtime 보정은 방어적 검증으로 유지한다.
- Impact if unfixed:
  - scene YAML에는 음수 시작값이 남아 있지만 실행 시에는 0이 되는 구성 drift가 발생할 수 있다.
  - Inspector와 runtime 결과를 비교하는 수동 검증이 혼동될 수 있다.

#### L-02 — 변경 문서에 저장소 사실과 맞지 않는 설명이 있다

- Severity: Low
- Category: Documentation drift / Audit traceability
- Location:
  - `Assets/Scripts/Manager/GoldManager.cs:11-12`
  - `PublicMD/Status/PROGRESS.md:57`
  - `PublicMD/Status/PROGRESS.md:67-71`
  - `PublicMD/ARCHITECTURE.md` §8
- Evidence:
  - 코드와 PROGRESS는 `DataManager.instance`가 `ARCHITECTURE.md`에 구조 부채로 기록됐다고 설명한다.
  - 실제 `ARCHITECTURE.md`의 현재 구조 부채 목록에는 `DataManager` 또는 static singleton 항목이 없다.
  - `Inventory_and_Items.md:96`은 오히려 `DataManager.instance`의 수명 정책이 단순하다고 기록한다.
  - PROGRESS의 변경 파일 목록은 실제 신규 `.meta` 두 개와 `PROGRESS.md` 자체를 누락한다.
  - PROGRESS는 Unity에서 `.meta`를 생성해야 한다고 적었지만 두 `.meta` 파일은 이미 존재하며 고유 GUID를 가진다.
- Description:
  - static singleton을 사용하지 않은 설계 자체는 적절하지만 이를 정당화하는 문서 인용이 현재 저장소 사실과 맞지 않는다.
  - 실제 changeset 및 meta 상태도 완료 기록과 불일치한다.
- Recommended fix:
  - “ARCHITECTURE에 기록된 부채”라는 표현을 제거하고 현재 저장소의 majority wiring pattern을 근거로 남긴다. 별도 합의로 실제 architecture debt가 맞다면 그때 공통 문서를 갱신한다.
  - PROGRESS 변경 파일 목록에 두 `.meta`와 `PROGRESS.md`를 포함한다.
  - `.meta 생성`이 아니라 Unity import 후 GUID와 component 인식을 검증해야 한다고 고친다.
- Impact if unfixed:
  - 후속 reviewer가 존재하지 않는 architecture 결정을 전제로 판단할 수 있다.
  - 신규 asset의 실제 변경·검증 범위가 완료 기록에서 부정확하게 남는다.

#### L-03 — 성공·실패 검증 버튼의 의미가 현재 잔액에 따라 바뀐다

- Severity: Low
- Category: Test harness reliability
- Location:
  - `Assets/TestOnly/TestGoldWindow.cs:7-9`
  - `Assets/TestOnly/TestGoldWindow.cs:42-55`
  - `PublicMD/Systems/Player_Gold.md:71-72`
  - `PublicMD/Status/PROGRESS.md:61,71`
- Evidence:
  - “Affordable” 지출은 고정값 10, “Unaffordable” 지출은 고정값 1000이다.
  - `_initialGold`는 임의의 0 이상 값이며 Add 버튼으로 잔액을 계속 늘릴 수 있다.
  - 잔액이 10 미만이면 affordable 버튼도 실패하고, 잔액이 1000 이상이면 unaffordable 버튼도 성공한다.
  - 문서는 세 버튼이 성공 지출과 부족 지출 경로를 모두 노출한다고 설명한다.
- Description:
  - 버튼은 transaction API 자체를 호출하지만 이름과 문서가 약속하는 branch를 독립적으로 보장하지 않는다.
  - 조작 순서나 Inspector 시작값에 따라 reviewer가 의도한 경로를 실행하지 않은 채 검증했다고 판단할 수 있다.
- Recommended fix:
  - 검증 절차에 요구되는 시작 잔액과 버튼 순서를 명시하거나, 현재 잔액을 기준으로 성공·실패 조건을 확실히 만드는 probe를 제공한다.
  - 고정 1000 버튼을 유지한다면 “Unaffordable”이 아니라 실제 의미인 “Try Spend 1000”으로 기술하고 사전조건을 문서화한다.
- Impact if unfixed:
  - 정상 성공 또는 잔액 부족 branch가 누락된 Play Mode 검증이 PASS로 기록될 수 있다.
  - 초기 골드 tuning 변경 후 probe 설명이 조용히 부정확해진다.

## Findings By File

- `Assets/Scripts/Manager/GoldManager.cs`
  - 잔액 소유와 transaction 책임이 잘 응집되어 있다.
  - static/global lookup, UI 의존성, logging과 미래 abstraction이 없다.
  - L-01의 edit-time serialized value 보정과 L-02의 부정확한 architecture 주석만 수정 대상이다.
- `Assets/TestOnly/TestGoldWindow.cs`
  - 기존 TestOnly `OnGUI`/`GUI.Window` 패턴을 따르며 production에서 역참조되지 않는다.
  - fallback search는 `Awake` 한 번에만 수행된다.
  - L-03 때문에 버튼 이름이 보장하는 branch는 현재 잔액에 종속된다.
- `Assets/Scenes/FarmerTest.unity`
  - 기존 `Systems > Manager` 및 `TestOnly!!` hierarchy는 확인됐다.
  - M-01의 신규 Gold component와 serialized reference가 없다.
- `Assets/Scenes/GuardTest.unity`
  - 의도적으로 미배선 상태이며 현재 Gold GUID 참조도 없다.
- `PublicMD/Systems/Player_Gold.md`
  - 9개 필수 절과 책임·불변식·TBD·최소 확인 범위를 모두 갖춘다.
  - 현재 scene 미배선 상태를 정확히 구분한다.
  - L-03의 검증 사전조건은 명확하지 않다.
- `PublicMD/ProjectStructure.md`
  - 새 기능 routing과 신규 호출자 위치를 등록했다.
  - Gold의 scene 단위 상태를 Manager 책임에 포함한 것은 현재 작은 범위에서 수용 가능하다.
- `PublicMD/Systems/README.md`
  - 신규 leaf 등록이 정확하다.
- `PublicMD/Status/PROGRESS.md`
  - 현재 상태 최상단에 새 기록을 추가하고 과거 기록을 수정하지 않았다.
  - L-02와 L-03의 검증·변경 범위 설명을 교정할 필요가 있다.
- 신규 `.meta`
  - 두 파일 모두 GUID가 존재하고 저장소 내 중복 또는 scene 참조는 발견되지 않았다.

## Cross-Cutting Findings

- Gold mutation은 `GoldManager` 내부로 한정되며 `_currentGold` 외부 직접 참조가 없다.
- production C#에서 `TestGoldWindow` 또는 `Assets/TestOnly` 역참조는 없다.
- `GoldManager`에는 `Debug`, static field 또는 singleton instance가 없다.
- `DataManager.instance`의 실제 `Assets` 소비자는 0건이다.
- `WarehouseInventory.TryAdd`와 마찬가지로 `long` 합산을 사용하지만, Gold는 의도대로 포화하고 Warehouse는 실패를 반환한다. 두 API의 서로 다른 실패 계약은 문서화되어 있다.
- merchant, price, recruitment, UI와 save/load 책임이 GoldManager에 섞이지 않았다.
- event/callback 부재는 현재 consumer 범위에서 architecture defect가 아니다.
- 사용자의 별도 미추적 이미지 `Assets/Art/Generated/Tiles/farm-dirt-corner-tileset-01.png`는 감사 범위에서 제외했다.

## Positive Notes

- `CurrentGold`는 읽기 전용이며 모든 mutation이 명명된 transaction API를 통과한다.
- `TrySpend`는 잘못된 금액과 잔액 부족에서 상태를 변경하지 않는다.
- `Add`는 양수 `int`와 현재 non-negative 잔액의 합을 `long`으로 계산해 overflow를 방지한다.
- 정상 gameplay 실패를 production log로 오염시키지 않는다.
- 신규 production type 하나와 TestOnly type 하나가 각각 파일명과 일치한다.
- 모든 test 수치는 명명된 constant로 분리되어 있다.
- 신규 Systems 문서의 상대 링크가 모두 실제 파일로 resolve된다.
- `git diff --check`는 통과했다.
- 현재 `Assembly-CSharp.csproj`에는 신규 두 C# 파일의 Compile Include가 존재한다.
- `obj/Debug/Assembly-CSharp.dll`은 두 신규 source보다 늦은 timestamp를 가지므로 보고된 build 실행 정황과 일치한다.

## Verification Limits

- sandbox와 사용자 지시에 따라 어떤 파일도 수정하지 않았다.
- build는 output artifact를 변경할 수 있어 `dotnet build`를 독립적으로 재실행하지 않았다. 따라서 보고된 “0 warnings / 0 errors” 콘솔 결과 자체는 재현하지 않았다.
- Unity Editor/Play Mode를 실행하지 않았다.
- M-01 때문에 현 저장소 상태에서는 Gold lifecycle, 버튼 동작, Console 출력, Inspector clamp와 두 번째 Play Mode 진입을 검증할 수 없다.
- Unity import를 실행하지 않았으므로 신규 `.meta` GUID의 실제 MonoScript import 결과는 확인하지 못했다.
- 이전 Carry presentation findings와 Gold 외 기존 시스템은 이번 focused review에서 재검증하지 않았다.

## Recommended Next Actions

1. M-01의 `FarmerTest.unity` GoldManager 및 TestGoldWindow 배선을 완료한다.
2. Unity에서 import 후 두 MonoScript가 정상 인식되고 missing script가 없는지 확인한다.
3. L-01의 `OnValidate` 보정을 추가한다.
4. L-03의 성공·실패 probe 사전조건을 고정하거나 문서와 버튼 이름을 실제 동작에 맞춘다.
5. L-02의 architecture debt, 변경 파일과 `.meta` 검증 설명을 현재 저장소 사실에 맞춘다.
6. Play Mode에서 시작 잔액, Add, 성공·실패 TrySpend, overflow 경계, 두 번째 진입 reset과 Console 무경고를 검증한다.

## Final Verdict

**Changes requested.**

GoldManager의 architecture와 production transaction 코드는 승인 가능한 수준이며 priority 1과 2에서는 issue를 발견하지 못했다. 그러나 scene runtime owner와 유일한 검증 도구가 모두 미배선 상태이므로 현재 changeset만으로는 기능이 실행되거나 검증될 수 없다. M-01을 해소하고 Unity Play Mode 검증을 마친 뒤 완료 처리하는 것이 안전하다.