# Code Evaluation Result

## Purpose

승인된 씨앗 심기 변경을 아키텍처, 정확성·수명, Unity 직렬화 안전성, 코드 규약 순서로 검토했다. **리뷰만 수행했으며 파일을 수정하지 않았다.**

## Review Snapshot

- **Date:** 2026-09-22
- **Scope:** 제공된 변경 파일 31개 전체와 직접 영향을 받는 의존 코드·에셋.
- **Standards:** `reviewing-npc-work-code` 스킬과 audit workflow, `ProjectStructure.md`, `CodeConvention.md`, `ARCHITECTURE.md`, 관련 Systems leaf 및 승인된 씨앗 규칙.
- **Baseline:** 기존 dirty 문서의 승인된 규칙을 요구사항으로 인정했다.
- **Evidence:** 현재 Git diff·신규 파일·YAML·메타데이터, `.harness-runs/seed-planting-20260922/`, 최신 `seed-planting-20260922-current/`, Unity Editor 로그.
- **추가 상태:** 검토 중 인계 문서와 검증 기록이 갱신되었다. native 메뉴 성공은 로그로 확인했고, 기본 Play 정상 동작은 인계 문서에 기록된 사용자 확인으로 구분했다.

## Executive Summary

**차단 수준의 코드 결함은 발견하지 못했다.** 심기 요청, 농장 transaction, 재고 저장소와 UI의 책임이 분리되어 있다. 씨앗 차감 후 작물 상태를 확정하기까지 외부 callback이나 추가 실패 분기가 없으며, 확정 이후 이벤트를 발행한다.

확정된 규칙을 여전히 미결정으로 설명하는 **Low 문서 불일치 1건**이 있다.

| 검토 우선순위 | 결과 |
|---|---|
| 1. Architecture / responsibility | 코드 책임 배치·의존 방향에서 문제 발견 없음 |
| 2. Correctness / lifecycle / cancellation / regression | 검토 범위에서 확정 결함 발견 없음 |
| 3. Unity scene / prefab / serialized references | 변경 배선과 정적 참조에서 문제 발견 없음 |
| 4. Convention / maintainability / dead code / magic values | Low 문서 불일치 1건. 변경 코드에서 별도 지적 없음 |

## Improvements Since Previous Review

기존 보고서는 LocalizeSystem 검토이므로 이번 후보와 직접 비교할 수 없다. 이전 finding의 해결 여부는 재검증하지 않았다.

씨앗 기능의 변경 전후를 비교하면 다음이 개선되었다.

- 무료 `TrySelectCrop` 경로를 제거하고, 테스트 창까지 실제 씨앗 소비 경로로 통일했다.
- 진행도 0에서도 확정된 작물을 교체할 수 없도록 제한했다.
- 팝업이 클릭한 밭의 source를 받아 대상과 재고를 조회한다.
- UI raycast 차단과 겹친 collider 탐색을 추가했다.
- 재고 부족, 반복 확정, 재활성화, 두 밭의 마지막 씨앗 경쟁을 검사한다.

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

None found.

### Low

#### L-01 — 아키텍처 문서에 폐기된 씨앗 규칙의 미결정 상태가 남아 있다

- **Severity:** Low — 비차단 문서 정리 항목.
- **Category:** Maintainability / Documentation consistency.
- **Location:** `PublicMD/ARCHITECTURE.md:175`, `현재 구조 부채`.
- **Evidence:** “씨앗 선택을 포함한 농장 생산 대상 변경 규칙이 미결정이다”라고 기록되어 있다. 반면 `SPEC.md`의 2026-09-22 확정 규칙과 `FarmWorkSite.cs:37`, `:82–92`는 빈 밭에서만 심기, 씨앗 1개 소비, 확정 후 교체 금지를 명시한다.
- **Description:** 기존 문구가 이번 구현 이후에도 현재 구조 부채로 남아 승인된 규칙과 모순된다. 같은 아키텍처 문서의 54행에는 새 transaction 책임도 이미 반영되어 있다.
- **Recommended fix:** 해당 항목을 제거하거나 실제로 남은 미결정 범위만 기술하고 Farming 소유 문서로 연결한다.
- **Impact if unfixed:** 후속 구현자가 확정된 규칙을 다시 설계하거나 이전의 교체 허용 동작을 복원할 수 있다. 현재 실행 동작에는 영향이 없다.

## Findings By File

| 검토 파일 | 결과 |
|---|---|
| `FarmSeedSource.cs`, `SeedPlantResult.cs` 및 각 `.meta` | 명시적 밭·창고·catalog·item 참조, 대상 수명 확인, 후보 검증과 결과 구분이 적절하다. 신규 GUID 중복 없음 |
| `FarmWorkSite.cs` | 빈 밭 검증 → 전량 차감 → 작물 상태 확정 → 이벤트 순서를 유지한다. 무료 public 선택 API 제거 확인 |
| `WarehouseInventory.cs` | 단일 항목 차감은 부족·잘못된 수량에서 무변경 실패한다. 기존 batch 판매 transaction 유지 |
| `CropCatalog.cs`, `FarmProductionDefinition.cs` | 명시적 seed ID 매핑, 중복·누락·Seed category 검사. 공유 정의에 runtime 재고나 진행도를 저장하지 않음 |
| Carrot·Potato definition assets | seed ID 6/7이 CSV와 일치하며 기존 생산·표현 설정 유지 |
| `IUIService.cs`, `UIManager.cs`, `PopBase.cs` | 공통 계층은 농장 구체 타입을 참조하지 않는다. 바인딩 성공 후 열고 기존 source 없는 popup 경로 유지 |
| `SeedSelectionPopup.cs` | 선택과 확정 분리, 확정 전 선택 ID 해제, 실패 표시, source 재바인딩, listener 등록·해제 확인 |
| `PointerClickRouter.cs` | press 시점 UI raycast, Unity backing 참조, 비활성 source 제외와 겹친 collider 탐색 확인 |
| `SeedSelectionPopup.prefab` | 심기·취소·닫기 버튼 참조가 존재하며 persistent callback 중복 없음. 고정 창고 참조 제거 |
| `FarmerTest.unity` | Soil 인스턴스의 빈 시작 override, FarmSeedSource 의존성, 두 테스트 창 연결 확인. 기존 Hierarchy·collider 유지 |
| `TestFarmProductionWindow.cs` | 실제 source를 통한 유료 심기로 전환. 기존 수확·입고 probe 경로 유지 |
| `SeedPlantingTests.cs` 및 `.meta` | 임시 fixture 정리, 두 crop의 반복 cycle, 중복 확정, 공유 재고, 비활성·파괴 대상, catalog 오류 검사 확인 |
| `ARCHITECTURE.md` | L-01 |
| `PLAN.md`, 활성 Seed 계획, `ProjectStructure.md`, `SPEC.md`, `PROGRESS.md` | 승인 범위·라우팅·현재 구현 및 검증 상태 검토 |
| Farming `README.md`, `Definition_and_Catalog.md`, `Runtime_and_Transactions.md`, `Inventory_and_Items.md`, `UI.md` | 변경 파일의 소유권, transaction 흐름과 배선 설명이 구현과 일치 |

직접 의존성으로 `BaseInteractionProvider`, `SeededRandomSource`, `ItemDataContext`, inventory·interaction 계약, `FarmCropPresenter`, `ItemSlotView`, `BackBg`, 기존 Merchant·TownHall click source와 관련 에셋도 확인했다.

## Cross-Cutting Findings

- `WorkerNPC`, selector, action에 씨앗 선택·차감 책임이 추가되지 않았다.
- `FarmSeedSource`는 후보와 요청 진입점을, `FarmWorkSite`는 심기 확정을, `WarehouseInventory`는 수량 변경을 소유한다.
- `IInventory`에 출고 권한을 추가하지 않았다. production 단일 차감 호출은 농장 transaction에 있다.
- 전용 팝업의 `FarmSeedSource` 참조는 UI 문서의 전용 view–domain 연결 규칙과 일치한다.
- `_startingDefinition`은 기존 배치 초기화 경로로 유지되며, FarmerTest 인스턴스에서만 null로 override한다. 승인 범위와 일치한다.
- 기존의 무관한 구조 부채나 변경되지 않은 스타일을 이번 후보의 신규 결함으로 확대하지 않았다.

## Positive Notes

- 차감과 작물 적용 사이에 재진입 가능한 callback이 없다.
- `StateChanged` 구독자는 이미 확정된 재고·농장 상태를 관측한다.
- 선택·취소·닫기는 재고를 변경하지 않는다.
- 확정 시 현재 재고와 밭 상태를 다시 검사한다.
- 재활성화가 농장 상태나 씨앗 재고를 초기화하지 않는다.
- source 바인딩 실패가 기존 팝업의 대상이나 stack을 먼저 변경하지 않는다.
- 실제 Unity 실행과 대체 host 검증의 차이를 인계 자료에 명시했다.

## Verification

| 항목 | 결과 |
|---|---|
| Git status·전체 변경 diff·신규 파일 | 확인 |
| `git diff --check` | PASS. CRLF 정규화 안내만 발생 |
| 신규 C# 공백·충돌 표식·메타 GUID 중복 | 문제 없음 |
| 기존 host 바이너리 재실행 | domain **39개**, popup/router **12개 PASS** |
| 저장된 Runtime·Editor 빌드 로그 | 각각 **0 errors / 0 warnings** 확인 |
| 최신 FarmerScene.Structure v1 | **33 checks PASS** 기록 확인 |
| 최신 Gate 의존 파일 해시 | **848개 모두 현재 파일과 일치**. PackageCache의 40개 경로를 해석해 비교 |
| 배선 검사 메모리 재실행 | 기존 Hierarchy·collider **132개 블록 보존**, 외부 GUID **133개**, source·버튼 참조 PASS |
| 변경 문서 로컬 링크 검사 | **134개 PASS** |
| native 메뉴 검사 | Unity Editor 로그에서 `Seed planting checks passed: 39` 확인 |
| 기본 Play | 갱신된 인계 문서에 사용자 정상 확인이 기록됨. 리뷰어 직접 관찰은 아님 |

구 DLL로 실행된 최초 구조 Gate는 현재 후보의 증거에서 제외했다.

## Verification Limits

- read-only 지시에 따라 `dotnet build`와 Unity 재컴파일을 실행하지 않았다. 빌드는 출력·중간 파일을 생성한다.
- host 재실행은 기존 바이너리를 사용했다. 현재 소스를 새로 컴파일한 독립 검증은 아니다.
- host의 Unity·UI·물리·트윈 대체 구현은 실제 입력 순서, 애니메이션 또는 렌더링을 증명하지 않는다.
- 구조 Gate는 저장된 참조·배선 검증이며 심기 동작 전체의 실행 검증이 아니다.
- native 메뉴 성공은 확인했지만, 모든 UI 예외 경로와 두 crop의 전체 성장·수확·소멸 cycle을 직접 실행하지 않았다.
- 검토 중 추가된 기본 Play 확인은 repository 인계 기록에 근거한다. 전체 Seed Phase 2·4 회귀 완료로 확대하지 않는다.

## Recommended Next Actions

1. L-01의 아키텍처 문구를 현재 확정 규칙에 맞춘다.
2. 남은 native 회귀에서 두 crop의 전체 cycle, 소멸 중 재심기, 팝업 재개방·대상 상실, 기존 Merchant·TownHall 입력을 확인한다.
3. 전체 Seed slice 완료 여부는 활성 계획의 미완료 시각·회귀 조건과 별도로 판단한다.

## Final Verdict

**Approve with nits.**

검토 범위에서 아키텍처·정확성·직렬화의 차단 결함은 발견하지 못했다. Low 문서 불일치 1건이 남는다. 이번 판정은 승인된 심기 후보에 대한 코드 리뷰이며, 전체 Seed slice의 시각·회귀 검증 완료를 의미하지 않는다.