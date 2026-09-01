# Seed System Implementation Plan

> 상태: Seed Phase 1 completed, Seed Phase 2 implementation complete / verification pending, Phase 3 이후 승인 대기
> 작성일: 2026-08-29
> 상위 roadmap: `PublicMD/PLAN.md`
> 요구사항: `PublicMD/SPEC.md`의 `SRC-024`, `REQ-F-052`~`REQ-F-060`, `REQ-D-016`
> 현재 구현 지도: `PublicMD/Systems/Farming/README.md`

## 1. 목표와 플레이 경험

플레이어가 농경지에 심을 씨앗을 선택하면 농부는 선택 내용을 알 필요 없이 기존처럼 농경지에서 일한다. 농경지는 선택된 정의를 기준으로 성장 요구치, 수확 요구치, 결과물과 수확량을 계산하고, 작물 표현과 UI는 같은 runtime 상태를 읽는다.

이 slice가 완성되면 다음 흐름이 한 Play Mode session에서 이어져야 한다.

```text
빈 농경지 선택
  -> 씨앗 선택 popup
  -> 유효한 씨앗 확정
  -> 농부 작업으로 progress 증가
  -> 성장 단계 sprite와 animation 변경
  -> 수확 가능 상태
  -> 정의된 item과 수량을 inventory에 저장
  -> 수확 소멸 animation
  -> 다음 씨앗을 선택할 수 있는 상태
```

## 2. 확정된 책임 경계

| 책임 | 소유자 |
|---|---|
| 어떤 씨앗이 선택됐는지와 현재 생산 cycle | `FarmWorkSite` runtime state |
| 씨앗별 결과 item, 요구치, 수확량과 presentation reference | crop별 ScriptableObject definition |
| 선택 가능한 crop definition 목록과 중복 검증 | catalog ScriptableObject |
| 농사 작업 1회 실행과 NPC 비용 | 기존 `FarmingAction` |
| 어떤 업무를 할지 판단 | 기존 Farmer selector와 decision 계층 |
| 성장 단계 계산과 작물 visual state 적용 | FarmWorkSite가 제공한 read model을 소비하는 crop presentation component |
| 선택 popup과 입력 lifecycle | UI 계층과 farm interaction adapter |
| 결과 item 수량 저장 | 기존 `IInventory` 구현 |

NPC와 Farmer selector에는 selected seed, crop sprite, yield table을 저장하지 않는다. `FarmingAction`은 현재처럼 provider에 작업을 요청하고, 결과물의 종류와 양은 `FarmWorkSite`가 결정한다.

## 3. 데이터 형식 결정

### 첫 구현의 기준

첫 구현은 ScriptableObject를 authoritative crop definition으로 사용한다.

이유:

- 기존 `FarmProductionDefinition`이 이미 output item, progress, yield를 소유한다.
- sprite, animation clip 또는 controller 같은 Unity object reference는 CSV보다 ScriptableObject 연결이 안전하다.
- crop 수가 적은 첫 vertical slice에서는 Inspector validation과 asset reference가 대량 표 편집보다 중요하다.

CSV는 Phase 1 범위에 추가하지 않는다. crop 수가 늘어 숫자 일괄 편집이 실제 병목이 되면 numeric balance만 CSV로 이동하고, Unity asset reference는 ScriptableObject에 남긴다. 두 형식을 동시에 source of truth로 만들지 않는다.

### 정의에 필요한 정보

| 범주 | 필수 정보 |
|---|---|
| 식별 | 변경되지 않는 crop ID, 표시 이름 |
| 생산 | 결과 item ID, 성장 요구치, 수확 요구치, 최소·최대 수확량 |
| 단계 | stage 수, 각 stage 진입 threshold |
| 표현 | stage별 sprite, 성장 transition, 성장 중 idle, 수확 소멸 animation reference |
| 검증 | ID 중복, 누락 output item, 음수·0 요구치, 잘못된 yield 범위, 오름차순이 아닌 threshold, 누락 sprite |

기존 `FarmProductionDefinition`을 crop별 definition으로 확장하고, 선택 가능한 definition 목록을 제공하는 catalog를 별도로 둔다. runtime progress와 phase는 definition에 쓰지 않는다.

## 4. 시작 전 Decision Gates

아래 항목은 구현자가 임의로 확정하지 않는다. 각 Phase를 시작하기 전에 사용자의 결정을 SPEC에 반영한다.

| Gate | 결정할 내용 | 권장 기본안 | 상태 |
|---|---|---|---|
| SG-001 | 씨앗을 언제 변경할 수 있는가? | 빈 농경지에서만 선택, 성장·수확 중 변경 금지 | **확정(2026-08-29)**: 권장 기본안대로. `SPEC.md` 12절 Decision Log Q-030 참고. |
| SG-002 | 실제 seed item을 inventory에서 소비하는가? | 첫 vertical slice는 선택만 하고 소비는 후속 단계로 보류 | **확정(2026-08-29)**: 권장 기본안대로. `SPEC.md` 12절 Decision Log Q-031 참고. |
| SG-003 | 첫 구현 crop 종류와 output item ID | 기존 item data에서 검증 가능한 2종부터 시작 | **확정(2026-08-29)**: Carrot(cropId 1, item 4), Potato(cropId 2, item 5), 둘 다 Food category로 `ItemData.csv`에 신규 추가. `SPEC.md` 12절 Decision Log Q-032 참고. |
| SG-004 | 첫 crop의 성장 stage 수 | 데이터는 가변 배열, 첫 art는 4단계 | **확정(2026-09-01)**: Carrot·Potato 모두 `0 / 0.3333 / 0.6667 / 1` 네 stage. `SPEC.md` Decision Log Q-033 참고. |
| SG-005 | popup 진입 입력 | 기존 hover collider를 재사용하되 click은 별도 interaction adapter가 처리 | 미확정 — Seed Phase 3 시작 전 결정 |
| SG-006 | 수확 완료 뒤 다음 cycle | 소멸 animation 완료 후 빈 상태로 전환하고 다시 선택 요구 | **확정(2026-09-01)**: 최종 수확 transaction 직후 current crop을 비우고 선택을 허용한다. 소멸 중 선택된 새 visual은 기존 소멸 완료 뒤 표시한다. `SPEC.md` Decision Log Q-035 참고. |

## 5. Seed Phase 1 — 기본 기능과 데이터

### 목표

선택된 crop definition이 실제 성장 요구치, 수확 요구치, 결과 item과 yield를 결정하고 코드 수정 없이 definition asset으로 조정되게 한다.

### 작업 순서

1. SG-001~SG-003을 확정하고 SPEC의 open question을 결정 기록으로 바꾼다.
2. 기존 `FarmProductionDefinition`을 crop별 definition으로 확장한다.
3. crop ID와 표시 이름, 결과 item, 성장·수확 요구치, yield 범위를 검증한다.
4. 선택 가능한 definition을 보유하는 catalog ScriptableObject를 추가한다.
5. `FarmWorkSite`에 current definition과 빈/선택 대기 상태를 추가한다.
6. 선택된 definition이 없으면 Farming interaction을 사용할 수 없게 한다.
7. 유효한 선택 API는 성공할 때만 current definition과 cycle state를 변경한다.
8. 성장·수확 work는 current definition의 요구치를 사용한다.
9. 수확 transaction은 current definition의 output item과 yield를 inventory에 전체 입고한 뒤에만 cycle을 전진시킨다.
10. TestOnly 도구에서 crop 선택, 강제 work, 결과 item·수량을 검증할 수 있게 한다.

### 예상 변경 영역

- Farming domain의 definition, catalog, `FarmWorkSite`
- 기존 farming TestOnly window
- 필요 시 item lookup validation 경계
- Farming·Inventory Systems 문서와 progress 기록

Farmer selector, `WorkerNPC`, `NPCStat`, decision utility는 기본적으로 변경하지 않는다.

### 완료 조건

- 서로 다른 두 definition을 선택하면 서로 다른 output item이 inventory에 들어간다.
- 같은 work 횟수에서 요구치가 다른 crop의 진행 속도 또는 완료 시점이 다르다.
- yield 범위가 다른 crop은 고정 seed에서 정의된 범위의 재현 가능한 수량을 만든다.
- current definition이 없거나 invalid하면 progress와 inventory가 변하지 않고 원인이 식별된다.
- 수확 입고가 실패하면 crop progress와 phase가 소모되지 않는다.
- ScriptableObject 값 변경만으로 요구치와 yield가 바뀐다.
- compile, deterministic test, transaction test와 Play Mode 수동 검증이 통과한다.

### 구현 기록 (2026-08-29)

작업 순서 1~10을 구현했다. `FarmProductionDefinition`에 `_cropId`/`_displayName`을 추가하고, `CropCatalog` ScriptableObject를 신설했다. `FarmWorkSite`는 `_startingDefinition`(구 `_definition`, `FormerlySerializedAs`로 scene 참조 보존)과 런타임 `_currentDefinition`을 분리하고 `TrySelectCrop`/`CanSelectCrop`/`HasCrop`을 추가했다. `CanInteractCore`를 override해 crop 없음을 게이트했고, `TryInitializeCore`에서는 definition 검사를 제거해 `BaseInteractionProvider._isOperational` latch로 인해 씨앗 선택이 영구히 막히는 문제를 피했다. `ApplyHarvestingWork`는 거부된 yield를 `_pendingYield`로 보존해 재시도 시 재사용한다. `TestFarmProductionWindow`에 catalog 기반 crop 선택 버튼과 crop별 창고 수량 표시를 추가했다. `ItemData.csv`에 Carrot(4)·Potato(5)를 추가하고, 기존 `FarmProductionDefinition.asset`을 `FarmProductionDefinition_Carrot.asset`으로 rename(GUID 보존)한 뒤 새 필드로 채우고, `FarmProductionDefinition_Potato.asset`과 `CropCatalog.asset`을 신규 작성했다.

`Assembly-CSharp.csproj` compile 통과(0 경고/0 오류, `dotnet build`). `WarehouseInventory`가 용량 제한이 없어 입고 거부 경로는 코드 검토로만 확인했고 완료 조건 5는 `NOT VERIFIED`로 남겼다. `TestFarmProductionWindow`는 `FarmerTest` 농장 GameObject에 배치되어 `_farmWorkSite`/`_warehouse`/`_cropCatalog`/`_itemDataContext`가 연결됐다. 2026-09-01 사용자가 Carrot/Potato 선택·진행·입고와 선택 거부 Play Mode 시나리오 통과를 확인했다.

**Codex 리뷰 후속 수정(2026-08-29)**: 독립 Codex 리뷰(`Status/Code_Evaluation_Result.md`)가 H-01(신규 파일 Git 미추적), M-01(output item id 미검증), M-02(`CropCatalog.TryValidate` 미호출), M-03(부분 수락 시 비원자적 transaction), L-01(상태 문구 모순)을 지적했다. M-04(yield가 cycle 총량이 아니라 작업당)는 사용자가 작업당이 의도한 설계임을 확정해 결함에서 제외했다. 나머지는 모두 수정했다: `ItemDataContext.TryGetItemInfo` 추가, `CropCatalog.TryValidate`에 `ItemDataContext` cross-reference 검증 추가 및 `TestFarmProductionWindow`의 실제 초기화 경계에 연결, `FarmWorkSite.ApplyHarvestingWork`가 부분 수락 시 남은 수량만 재요청하도록 수정, Seed Phase 1 관련 신규 파일을 명시적으로 stage(커밋은 아직 하지 않음), 상태 문구를 `completed`에서 `implementation complete / verification pending`으로 정정.

## 6. Seed Phase 2 — 작물 실제 표현

### 목표

현재 crop과 progress가 stage sprite, 성장 transition, 성장 중 idle과 수확 소멸 animation으로 표현되게 한다.

### 작업 순서

1. SG-004와 SG-006을 확정한다.
2. 첫 crop별 stage sprite 세트를 제작한다. 데이터는 stage 수를 가변으로 지원한다.
3. `FarmWorkSite`의 current definition, normalized progress, phase를 읽는 presentation component를 추가한다.
4. progress가 stage threshold를 처음 통과할 때 해당 stage sprite로 성장 transition을 실행한다.
5. stage가 유지되는 동안 짧고 반복 가능한 idle animation을 실행한다.
6. 수확 cycle이 완전히 끝났을 때 disappear animation을 한 번 실행한다.
7. disappear가 끝나기 전에는 다음 crop visual을 표시하지 않는다.
8. disable, re-enable, definition 교체와 pool/scene reload 경로에서 visual state를 현재 runtime state로 복원한다.

### 아트 산출물 기준

- transparent background의 crop-only sprite
- 동일 pivot, pixels-per-unit와 canvas size
- stage 간 실루엣과 높이가 명확히 달라야 함
- 첫 vertical slice 권장 4단계: 새싹, 어린 작물, 성숙 중, 수확 가능
- idle은 가벼운 흔들림·호흡 정도로 progress를 오해시키지 않아야 함
- disappear는 수확 완료를 명확히 보여주되 gameplay transaction을 지연시키지 않음

### 완료 조건

- stage threshold 전후에서 sprite와 animation 상태가 정확히 한 번 전환된다.
- progress가 변하지 않는 동안 idle이 안정적으로 loop한다.
- 수확 완료 시 disappear가 한 번 재생되고 빈 상태가 남는다.
- crop definition을 바꾸면 해당 crop의 sprite와 animation reference를 사용한다.
- presentation이 inventory, yield 계산 또는 NPC 판단을 변경하지 않는다.
- scene 재진입 또는 component 재활성화 후 runtime state와 visual이 일치한다.

### 구현 기록 (2026-09-01)

`FarmProductionDefinition`에 visual controller와 가변 `CropVisualStage` 배열을 추가하고 Carrot·Potato asset을 `0 / 0.3333 / 0.6667 / 1` 네 stage로 배선했다. `FarmWorkSite`는 성공한 상태 변경 뒤 `StateChanged`를 발행하며 최종 수확 성공 시 current crop을 즉시 비운다. `FarmCropPresenter`는 이 이벤트를 구독해 stage command를 queue하고 별도 visual seed로 `CropVisualAnimator` 배열을 shuffle한다. 각 visual은 0.08초 간격으로 disappear→sprite 교체→appear를 실행하고 최종 수확에서는 disappear 후 숨는다. disable/re-enable과 소멸 중 새 crop 선택은 현재 runtime state로 안전하게 동기화한다.

명령행 compile과 script·asset GUID 정적 검사는 통과했다. 약 10개 `CropVisualAnimator`의 scene 위치와 `FarmCropPresenter` Inspector 배열은 사용자 수동 배선 범위이므로 Play Mode visual 완료 조건은 `NOT VERIFIED`다.

## 7. Seed Phase 3 — 선택 UI와 상호작용

### 목표

기존 성장 gauge를 유지하면서 플레이어가 농경지에서 현재 crop과 후보 정보를 확인하고 유효한 씨앗을 선택할 수 있게 한다.

### 작업 순서

1. SG-005와 미결정 popup 정책을 확정한다.
2. 기존 farm hover의 progress 표시가 새 current definition과 stage를 읽도록 확장한다.
3. crop 표시 이름, 결과물, 성장·수확 요구치와 yield 범위를 보여주는 seed selection popup을 만든다.
4. popup은 catalog의 유효 definition만 표시한다.
5. world click을 farm selection intent로 변환하는 interaction adapter를 추가한다.
6. UI는 concrete `FarmWorkSite`를 scene search하지 않고 명시적 source/command 계약을 사용한다.
7. 선택 확정은 farm의 유효 selection API를 호출하고 성공한 경우에만 popup 상태와 표시를 갱신한다.
8. 선택 불가 상태, invalid definition, catalog 누락과 중복 ID에 대한 사용자 feedback을 제공한다.
9. popup open/close, 다른 farm 선택, source 파괴·비활성화와 연속 클릭을 검증한다.

### 완료 조건

- farm을 유효한 입력으로 선택하면 해당 farm의 seed popup이 열린다.
- popup 후보와 표시 수치가 catalog/definition과 일치한다.
- 한 번의 확정 입력이 정확히 한 번의 selection mutation을 만든다.
- 선택 불가 상태에서 기존 crop과 progress가 보존되고 이유가 표시된다.
- 여러 farm을 번갈아 선택해도 source와 popup 정보가 섞이지 않는다.
- 기존 성장 gauge가 regress하지 않고 새 stage·progress와 일치한다.
- UI를 닫거나 source가 사라져도 stale reference와 열린 popup이 남지 않는다.

## 8. 전체 회귀와 최종 완료 조건

다음 시나리오를 순서대로 통과해야 전체 Seed slice를 완료로 기록한다.

1. 빈 farm에서 crop A 선택 → 성장 → 수확 → A output 저장 → disappear → 빈 상태.
2. 같은 farm에서 crop B 선택 → A와 다른 요구치로 성장 → B output 저장.
3. inventory 수락 실패 → output·progress·phase 보존 → 이후 정상 수락에서 한 번만 완료.
4. 잘못된 definition/catalog → 예외나 무한 retry 없이 선택·작업 거부와 원인 표시.
5. 성장 중 popup 입력 → SG-001에서 확정한 규칙에 따라 안전하게 거부 또는 처리.
6. farm 두 개가 서로 다른 crop과 progress·visual·popup source를 독립 유지.
7. scene reload 또는 object disable/enable 후 runtime과 visual/UI 상태 일치.

각 Seed Phase는 별도 plan approval, 구현, compile/Play Mode 검증과 `PublicMD/Status/PROGRESS.md` 기록을 거친다. 이전 Phase의 완료 조건을 통과하기 전 다음 Phase의 production 구현을 시작하지 않는다.

## 9. 이번 계획의 제외 범위

- NPC가 씨앗 종류를 판단하거나 선호하는 AI
- 작물 품질, 병충해, 물·비료, 계절과 날씨
- 연구·해금·특수 작물
- seed item 구매, 운반과 실제 inventory 소비(SG-002 결정 전)
- 다단 가공과 요리 recipe
- 저장/불러오기 migration
- CSV와 ScriptableObject를 동시에 authoritative source로 운영하는 구조
