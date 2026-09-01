# Farming Definition and Catalog

## 기능 목적

crop별 생산 규칙과 presentation reference, 선택 가능한 catalog와 정적 유효성 검증을 소유한다.

## 책임 경계

- `FarmProductionDefinition`은 crop ID·표시명, progress 변화량, output item·yield와 visual controller·stage 배열을 정의한다.
- `CropCatalog`는 definition 목록, 중복 crop ID와 output item cross-reference를 검증한다.
- runtime progress·phase와 현재 표시 stage는 공유 asset에 기록하지 않는다.

## 현재 데이터 흐름

```text
CropCatalog.TryValidate(ItemDataContext)
  -> definition 생산 값 검증
  -> visual controller와 0..1 stage 배열 검증
  -> crop ID 중복과 output item 존재 검증

FarmWorkSite -> 선택된 definition의 생산 값 소비
FarmCropPresenter -> 같은 definition의 presentation 값 소비
```

stage threshold는 첫 값 0, 마지막 값 1이고 엄격한 오름차순이어야 한다. Carrot과 Potato는 각각 `0 / 0.3333 / 0.6667 / 1`의 네 stage를 사용한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/FarmProductionDefinition.cs` | crop별 공유 생산 규칙과 presentation definition |
| `Assets/Data/ScriptableObject/Script/CropCatalog.cs` | 선택 가능한 definition 목록과 구조·중복·item cross-reference 검증 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 생산 요구치·yield·output | `FarmProductionDefinition.cs`, crop definition asset |
| stage 수·threshold·sprite | `FarmProductionDefinition.cs`, [Crop Presentation](Crop_Presentation.md), crop definition asset |
| crop 추가·catalog | `CropCatalog.cs`, definition asset, `CropCatalog.asset`, [Inventory and Items](../Inventory_and_Items.md) |

## 불변 규칙

- production과 presentation reference는 하나의 crop definition을 source of truth로 사용한다.
- first threshold는 0, last threshold는 1이며 모든 stage sprite가 존재해야 한다.
- catalog는 구조 검증과 ItemDataContext output item 검증을 함께 수행한다.
- 공유 definition은 runtime에서 mutation하지 않는다.

## 에셋과 검증

- `FarmProductionDefinition_Carrot.asset`, `FarmProductionDefinition_Potato.asset`: 네 stage와 공통 crop controller를 참조한다.
- `CropCatalog.asset`: Carrot과 Potato definition을 등록한다.
- `TestFarmProductionWindow`가 catalog 초기화 경계에서 `TryValidate`를 한 번 호출한다.

## 알려진 제약과 TBD

- crop 해금·계절·재고에 따른 catalog filtering은 미결정이다.
- 수치가 대량화되기 전까지 ScriptableObject가 생산 값과 Unity reference의 authoritative definition이다.

## 관련 문서

- [Farming index](README.md)
- [Runtime and Transactions](Runtime_and_Transactions.md)
- [Crop Presentation](Crop_Presentation.md)

## 문서 갱신 조건

definition schema, stage 검증, catalog 목록과 output item cross-reference가 바뀌면 갱신한다.
