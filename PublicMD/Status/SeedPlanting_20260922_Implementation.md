# 씨앗 심기 구현 기록 — 2026-09-22

빈 밭 클릭 → 보유 씨앗 선택 → 심기 확정 → 농부 작업으로 연결했다. 밭 한 생산 주기당 씨앗 1개를 사용하며, 확정 후에는 progress 0이어도 교체·취소할 수 없다. 수확 완료 뒤 다시 선택한다. 확정 전 선택·취소·닫기와 실패는 재고·밭 상태를 변경하지 않는다.

FarmerTest는 빈 밭으로 시작하고 창고의 당근·감자 씨앗 각각 5개를 유지한다. 씨앗 구매·운반·자동 보충은 구현하지 않았으므로 소진 후 추가 심기는 거부된다.

## 코드 변경 목록

기준 커밋은 `02097c0`이다. production C# 신규 2개·수정 9개(+329/-57), TestOnly 신규 1개·수정 1개(+196/-2), 합계 13개 파일 +525/-59줄이다. 줄 수는 순증가가 아니라 Git diff의 추가/삭제 수이며 신규 파일은 전체 줄 수를 센다.

| 파일 | 구분 | 추가/삭제 | 변경 목적 |
|---|---|---:|---|
| [FarmSeedSource.cs](../../Assets/Scripts/Actor/FarmSeedSource.cs) | 신규 | +87/-0 | 밭별 click source, 후보 조회, catalog 검증과 심기 요청 |
| [SeedPlantResult.cs](../../Assets/Scripts/Enum/SeedPlantResult.cs) | 신규 | +10/-0 | 심기 성공·실패 결과 |
| [FarmWorkSite.cs](../../Assets/Scripts/System/Farming/FarmWorkSite.cs) | 수정 | +34/-21 | 무료 선택 API 제거, 빈 밭 조건과 씨앗 차감 후 작물 확정 |
| [WarehouseInventory.cs](../../Assets/Scripts/System/Inventory/WarehouseInventory.cs) | 수정 | +13/-4 | 단일 item 전량 차감, batch 판매 유지 |
| [FarmProductionDefinition.cs](../../Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs) | 수정 | +3/-1 | SeedItemId definition |
| [CropCatalog.cs](../../Assets/Data/ScriptableObject/Script/CropCatalog.cs) | 수정 | +26/-0 | seed ID 조회, 중복·item category 검증 |
| [IUIService.cs](../../Assets/Scripts/Interface/IUIService.cs) | 수정 | +1/-0 | popup source 전달 overload |
| [UIManager.cs](../../Assets/Scripts/UI/UIManager.cs) | 수정 | +6/-0 | source 바인딩 성공 후 popup 열기 |
| [PopBase.cs](../../Assets/Scripts/UI/PopBase.cs) | 수정 | +3/-0 | source 바인딩 hook, 기존 popup 호환 |
| [PointerClickRouter.cs](../../Assets/Scripts/UI/PointerClickRouter.cs) | 수정 | +39/-6 | 겹친 collider의 source 전달, UI 클릭 관통 방지 |
| [SeedSelectionPopup.cs](../../Assets/Scripts/UI/SeedSelectionPopup.cs) | 수정 | +107/-25 | 대상별 목록·선택·심기 버튼·실패 표시·수명 정리 |
| [TestFarmProductionWindow.cs](../../Assets/TestOnly/TestFarmProductionWindow.cs) | 수정 | +4/-2 | 테스트 선택도 실제 씨앗 소비 경로 사용 |
| [SeedPlantingTests.cs](../../Assets/TestOnly/Editor/SeedPlantingTests.cs) | 신규 | +192/-0 | 임시 객체로 production transaction 39개 검사 |

NPC runner, selector, Farming/Harvest action과 A* 코드는 변경하지 않았다. UIManager와 router는 농장 구체 타입을 참조하지 않으며, SeedSelectionPopup이 명시적으로 전달된 FarmSeedSource를 사용한다. 별도 전역 manager나 범용 transaction framework는 추가하지 않았다.

## 에셋 변경

| 에셋 | 변경 |
|---|---|
| FarmerTest.unity | 기존 Soil에 FarmSeedSource 추가·연결, 시작 작물 null override, 두 생산 테스트 창 source 연결 |
| SeedSelectionPopup.prefab | 고정 창고·item context 참조 제거, 기존 취소·닫기 버튼 참조 연결 |
| FarmProductionDefinition_Carrot.asset | SeedItemId 6 |
| FarmProductionDefinition_Potato.asset | SeedItemId 7 |

신규 C# 3개에 meta를 추가했다. 기존 Hierarchy/collider 블록 132개를 보존했고 기존 GUID/fileID, 레이어, 원본 Soil prefab은 유지했다. 역할별 위치는 `InGameObjects/Map/Soil`, `Canvas/PopupUI/SeedSelectionPopup`, 기존 `TestOnly!!`다.

## 검증 결과와 한계

- Runtime·Editor 소스 컴파일: 오류 0, 경고 0. 사용자 자동 컴파일 완료 확인과 새 Unity DLL 시각도 확인했다.
- production transaction 검사: host 39개 Pass, 실제 Unity 메뉴 `Tools/NPC/Farming/Run Seed Checks`에서도 39개 Pass 로그 확인.
- popup/router 검사: host 12개 Pass. 대상 전환, 중복 입력, 취소·닫기, stale 재고, 닫기 tween 중 재개방, 파괴된 source, UI 관통과 collider 겹침을 검사했다. UI·물리·트윈 대체 환경이므로 native 화면 검사와 구분한다.
- 사용자 Play 확인: 빈 밭 클릭, 씨앗 선택, 심기, 재고 1개 감소, 농부 작업, 취소·닫기 재고 보존 정상.
- FarmerScene.Structure **v1 Pass, 33 checks, exit 0**. 새 DLL 반영 후 loaded-clean-scene에서 빈 밭·catalog·참조를 확인했고 현재 씬/source 해시와 일치했다.
- 변경 외부 GUID 참조 133개, Markdown 로컬 링크, diff 공백 검사 통과. 구 DLL로 먼저 반환된 Gate Pass는 현재 후보 증거에서 제외했다.
- 두 작물 전체 시각 cycle과 모든 입력 예외를 native Play에서 계측한 것은 아니다. 기존 Phase 2의 전체 시각 회귀를 자동 완료 처리하지 않는다.

기능 구현 후보 수정 예산은 **0/2회 사용**했다. host adapter의 누락 API와 package GUID 검색 경로 보완은 production 후보 수정과 구분했다.

## 독립 리뷰와 증거

`implement-npc-feature`의 지정 launcher로 별도 읽기 전용 Codex 리뷰를 비동기 요청했다. PID 38980, 요청 파일 prefix는 `.codex/agent-runs/20260922-230345-272-*`다. 요청 시 native 메뉴·Play 결과는 대기 상태였고, 이후 성공 증거를 이 문서와 PROGRESS에 기록했다. 리뷰 결과는 읽지 않았으며 판정을 추정하지 않는다. 리뷰 출력은 launcher가 `Code_Evaluation_Result.md`에 기록한다.

로컬 실행 증거:

- `.harness-runs/seed-planting-20260922/`: baseline, Runtime/Editor 컴파일 로그, host 검사 코드·로그, `native-seed-checks.log`, `wiring-checks.json`, `verification-status.json`, `change-inventory.json`.
- `.harness-runs/seed-planting-20260922-current/`: 현재 Editor의 `gate-results/farmer-scene-structure.json`과 dependency hash 증거.

`.harness-runs`는 Git 제외 경로다. 이 문서는 인계용 결과 요약이며, 다른 checkout의 현재 검증 증거를 대신하지 않는다. 공유 가능한 재검사 진입점은 tracked `SeedPlantingTests.cs`와 기존 구조 Gate다.
