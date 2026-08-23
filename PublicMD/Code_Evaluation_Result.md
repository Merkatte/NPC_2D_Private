# Code Evaluation Result

## Purpose

`ItemDataContext`의 소유·주입 방식과 `DataManager`, `InteractableManager`, `BaseInteractionProvider`, `Pub` 사이의 dependency boundary를 검토한다. 이번 검토는 read-only production-code audit이며 구현 코드는 수정하지 않았다.

## Review Snapshot

- Date: 2026-08-22
- Scope:
  - `BaseInteractionProvider`, `Pub`, `InteractableManager`
  - `DataManager`, `IDataManager`, `ItemDataContext`, `CSVParser`
  - `DestinationDB`와 두 gameplay scene의 serialized wiring
  - 프로젝트 전체 C#의 `TextAsset` 사용처
- Sources:
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ProjectStructure.md`
  - 현재 production C#과 `SampleScene.unity`, `GuardTest.unity`
- Verification:
  - `dotnet build Assembly-CSharp.csproj --no-restore`: 0 warnings, 0 errors
  - 프로젝트 전체 `TextAsset`, `ItemDataContext`, manager/provider 참조 검색
  - 두 scene의 Pub/manager/DataManager YAML 확인

## Executive Summary

사용자의 문제 제기는 타당하다. 공유 global content를 사용하는 scene component마다 같은 asset reference를 반복 배선하면 instance 수와 scene 수가 늘수록 누락 가능성과 편집 비용이 증가한다. 현재 체크인된 scene이 실제로 그 위험을 보여준다. 두 scene의 Pub에는 새 `_itemDataContext` 값이 저장되어 있지 않으며, 삭제된 field의 값만 `DataManager` YAML에 남아 있다.

다만 “모든 TextAsset을 manager가 직접 소유하고 provider가 manager를 조회한다”가 자동으로 정답은 아니다. 현재 raw `TextAsset`은 이미 `ItemDataContext` 하나가 중앙 소유하고 parsing/cache까지 담당한다. 반복되는 것은 raw data 자체가 아니라 공유 context reference다. 이를 없애기 위해 provider가 `DataManager.instance`를 조회하거나 `InteractableManager`가 Pub/Farm/Cook별 데이터를 분기하면 service-locator 또는 God Manager 문제가 생긴다.

권장 방향은 두 단계다.

1. raw source와 parsing은 typed ScriptableObject context가 계속 소유한다.
2. 프로젝트 규모상 중앙 배선이 필요하면 typed context들을 하나의 read-only `GameDataCatalog`에 모아 scene bootstrap/DataManager가 한 번 참조하고, `InteractableManager`는 동일한 immutable initialization context를 모든 provider에 전달한다.

manager는 concrete provider type을 검사하지 않고 data를 parse하지 않아야 한다. provider도 global singleton을 직접 조회하지 않는다. 이 경계를 지키면 중앙 관리 편의성과 provider 독립성을 함께 얻을 수 있다.

## Improvements Since Previous Review

- Eat/Drink/Farming이 하나의 `IInteractionProvider` 실행 protocol로 통합되었다.
- `InteractableManager`가 `(GameObject, ActionType)` registry를 소유하고 `DestinationDB`는 routing만 담당한다.
- item dictionary가 범용 base에서 제거되어 Pub domain으로 이동했다.
- raw CSV parsing과 cache는 `ItemDataContext`에 남아 runtime facility state와 분리되어 있다.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — 체크인된 두 scene에서 Pub item dependency가 배선되지 않았다

- Severity: High
- Location:
  - `Assets/Scripts/Actor/Pub.cs:6-21`
  - `Assets/Scenes/SampleScene.unity:710-721, 1414-1425`
  - `Assets/Scenes/GuardTest.unity:902-913, 1808-1819`
- Evidence:
  - Pub의 `TryInitializeCore`는 `_itemDataContext`가 없으면 false를 반환한다.
  - 두 scene의 Pub component 네 개에는 `_itemDataContext` serialized key가 없다.
  - 기존 값은 제거된 `DataManager._itemDataContext` key로 scene YAML에 남아 있다.
- Impact:
  - `InteractableManager` 초기화에서 두 Pub가 non-operational이 된다.
  - Eat/Drink provider lookup과 option 열거가 실패해 NPC 공급 행동이 사라질 수 있다.
- Recommendation:
  - 단기적으로는 모든 Pub 또는 Pub prefab에 같은 `ItemDataContext.asset`을 배선하고 scene을 저장한다.
  - 중앙 catalog 설계를 채택한다면 이 배선을 provider별로 반복하지 않고 migration한다.

### Medium

#### M-01 — per-instance shared-context 배선은 정확하지만 장기 편집 비용이 크다

- Severity: Medium
- Location: `Assets/Scripts/Actor/Pub.cs:6`
- Evidence:
  - 모든 Pub instance가 동일한 `ItemDataContext.asset`을 별도로 참조해야 한다.
  - 현재 scene 두 개만으로도 Pub 네 개의 배선 지점이 생긴다.
- Description:
  - 이 구조는 dependency가 명시적이고 테스트하기 쉬워 코드 품질상 잘못은 아니다.
  - 그러나 모든 facility가 global shared catalogue를 같은 방식으로 반복 참조하면 scene/prefab 유지 비용이 누적된다.
- Recommendation:
  - 같은 시설 instance가 많지 않다면 prefab에 한 번 배선하는 방식을 우선한다.
  - 여러 global dataset과 facility가 빠르게 늘어날 예정이라면 typed `GameDataCatalog`와 composition-root injection을 도입한다.

#### M-02 — 중앙화가 domain-aware manager 또는 service locator로 변하면 경계가 악화된다

- Severity: Medium
- Location:
  - `Assets/Scripts/Manager/InteractableManager.cs`
  - `Assets/Scripts/Actor/BaseInteractionProvider.cs`
- Risk:
  - manager가 `if (provider is Pub)`, `if (provider is FarmWorkSite)`처럼 분기하며 서로 다른 data를 주입할 수 있다.
  - provider가 `DataManager.instance` 또는 string key로 필요한 data를 직접 검색할 수 있다.
- Recommendation:
  - manager는 concrete type을 몰라야 한다.
  - 중앙화 시 모든 provider에 동일한 read-only initialization context/catalog를 전달하되, 그 catalog에는 immutable content definition만 둔다.
  - runtime state, manager/service, scene object를 catalog에 넣지 않는다.

#### M-03 — 초기화에 실패한 provider도 registry에 등록된다

- Severity: Medium
- Location: `Assets/Scripts/Manager/InteractableManager.cs:60-64`
- Evidence:
  - `TryInitialize`가 false여도 `RegisterProvider(provider)`가 실행된다.
- Impact:
  - 일반 lookup은 `CanInteract`에서 거부되어 안전하지만, 같은 owner/action의 뒤쪽 정상 provider가 duplicate 정책 때문에 등록되지 않을 수 있다.
- Recommendation:
  - 초기화 실패 provider를 registry에 넣지 않거나, duplicate 선택 정책에서 operational provider를 우선한다.

### Low

#### L-01 — 제거된 serialized field가 scene YAML에 남아 있다

- Severity: Low
- Location:
  - `SampleScene.unity:245, 1037`
  - `GuardTest.unity:342, 1424`
- Evidence:
  - 현재 C#에 없는 `InteractableManager._dataManager`와 `DataManager._itemDataContext` key가 남아 있다.
- Recommendation:
  - 올바른 새 배선을 완료한 뒤 Unity Editor에서 scene을 저장해 stale field를 정리한다. 기존 사용자 scene 변경과 충돌한다면 YAML을 수동 삭제하지 않는다.

## Findings By File

- `ItemDataContext.cs`: 현재 프로젝트의 유일한 raw `TextAsset` owner다. parse/cache 책임도 응집되어 있어 적절하다.
- `Pub.cs`: typed dependency가 명시적이라는 장점이 있지만 M-01과 H-01이 적용된다.
- `InteractableManager.cs`: 범용 registry 경계는 적절하다. 중앙 데이터 주입을 추가하더라도 concrete type 분기를 넣지 않아야 하며 M-03을 수정해야 한다.
- `DataManager.cs`: 현재 action cost registry만 소유한다. global content catalog를 도입한다면 역할 이름과 계약을 명확히 다시 정의해야 한다.
- `CSVParser.cs`: raw `TextAsset`을 받는 순수 parser이며 scene wiring 문제와 무관하다.
- `DestinationDB.cs`: provider data를 알지 않고 manager에 lookup을 위임하므로 현재 책임 경계가 적절하다.
- `SampleScene.unity`, `GuardTest.unity`: H-01과 L-01이 적용된다.

## Cross-Cutting Findings

- 중앙 소유와 중앙 접근은 다르다. 하나의 `ItemDataContext.asset`을 여러 Pub가 참조해도 데이터 source는 이미 하나다.
- 반복 Inspector reference는 prefab으로 상당 부분 해결할 수 있다. 모든 dependency를 global manager로 옮길 이유는 아니다.
- string path 또는 일반 string key lookup은 Unity object reference의 GUID 추적, rename 안전성, build dependency 추적을 잃게 한다. Addressables를 실제 도입할 때만 typed address/key와 load lifecycle을 함께 설계하는 편이 낫다.
- global data catalog는 순수 immutable content에 한정하면 실용적인 composition 도구가 될 수 있다. runtime service까지 넣으면 service locator가 된다.

## Positive Notes

- production C#에서 `TextAsset` field는 `ItemDataContext` 하나뿐이다.
- Pub는 raw CSV나 parser를 모르고 parsed item table만 소비한다.
- 같은 ScriptableObject asset을 여러 component가 참조해도 runtime data copy가 생기지 않는다.
- InteractableManager와 DestinationDB는 현재 concrete Pub/Farm type을 분기하지 않는다.
- command-line build가 경고 0, 오류 0으로 통과한다.

## Recommended Next Actions

1. 현재 Pub 공급 기능을 복구하기 위해 scene/prefab wiring 방식을 먼저 확정한다.
2. Pub 수가 적다면 shared `ItemDataContext.asset`을 Pub prefab에 한 번 배선한다.
3. global datasets가 계속 늘어날 것이 확실하다면 `GameDataCatalog` + composition-root initialization 설계를 별도 Plan으로 작성한다.
4. 어떤 방식을 택하더라도 Pub의 `DataManager.instance` 직접 조회와 string path loading은 사용하지 않는다.
5. `InteractableManager`가 초기화 실패 provider를 cache하지 않도록 수정한다.
6. scene 저장 후 stale serialized key 제거와 Eat/Drink Play Mode 동작을 검증한다.
