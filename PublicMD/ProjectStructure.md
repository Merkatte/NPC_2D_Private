# Project Structure

> 문서 기준일: 2026-10-30
> 이 문서는 전체 구조 지도와 기능 문서 라우팅만 소유한다. 구체 클래스 흐름과 Unity 배선은 `PublicMD/Systems`의 해당 문서가 소유한다.

## 1. 구조 한눈에 보기

```text
NPCManager / WorkerPool          생성과 조립
  -> WorkerNPC                  action queue 실행
     -> BaseNPCActionSelector   다음 queue 구성
        -> DestinationDecider   semantic decision
        -> DestinationDB        목적지와 interaction capability
        -> ActionPool           action 대여와 반환
     -> IAction                 선택된 행동 실행
     -> NPCStat                 actor별 runtime 상태
     -> NPCComponent            이동·presentation adapter와 운반 cargo 소유
```

현재 프로젝트에는 `WorkerAI`, `WorkerActionPlan`, `WorkerActionContext`, Behavior Graph 기반 실행기가 없다. 과거 구조를 전제로 새 코드를 배치하지 않는다.

## 2. 기능 문서 지도

| 대기능 | 읽는 경우 |
|---|---|
| [NPC](Systems/NPC/README.md) | NPC 개체의 공통 수명·판단·실행·표현 |
| [Settlement](Systems/Settlement/README.md) | 정착지 건설·건축가 작업·주거·시청 모집 |
| [Economy](Systems/Economy/README.md) | 농사·아이템·공유 자원·물류·상단 거래 |
| [Combat](Systems/Combat/README.md) | 공통 감지·공격·직군 판단과 성벽 방어 |
| [World](Systems/World/README.md) | 경로 탐색과 목적지·provider 공통 계약 |
| [UI](Systems/UI/README.md) | 입력·표시 수명·구체 view·시각 규칙·지역화·카메라 조작 |

기능 폴더에서는 각 README의 선택표를 따라 필요한 하위 기능을 반복 선택하여 leaf에 도달한다. 구체 작업의 진입점은 다음 표를 사용한다. Codex 작업 조정·검증 경계는 gameplay 기능 밖의 [Harness Architecture](HARNESS_ARCHITECTURE.md)를 따른다.

## 3. 작업별 읽기 라우팅

| 작업 | 필수 기능 문서 | 조건부 추가 문서 |
|---|---|---|
| queue 처리·공통 stat | [NPC Runtime](Systems/NPC/Runtime/Lifecycle_and_Stats.md) | [Action Runtime](Systems/NPC/Decision_and_Actions/Action_Runtime.md), [Spawning](Systems/NPC/Runtime/Spawning_and_Pooling.md) |
| 불만 원인·누적·태업 | [NPC Dissatisfaction](Systems/NPC/Runtime/Dissatisfaction.md) | [NPC Runtime](Systems/NPC/Runtime/Lifecycle_and_Stats.md), [Selector and Queue](Systems/NPC/Decision_and_Actions/Selector_and_Queue.md), [Action Runtime](Systems/NPC/Decision_and_Actions/Action_Runtime.md), [NPC Messages](Systems/NPC/Presentation/Messages.md) |
| utility·위험·look-ahead | [Decision Policy](Systems/NPC/Decision_and_Actions/Decision_Policy.md) | [Interaction](Systems/World/Interaction_and_Destinations.md), role 문서 |
| selector·새 action | [Selector and Queue](Systems/NPC/Decision_and_Actions/Selector_and_Queue.md), [Action Runtime](Systems/NPC/Decision_and_Actions/Action_Runtime.md) | concrete action leaf |
| 이동 실행·동적 target·경로 추종 | [Movement](Systems/NPC/Decision_and_Actions/Movement.md) | [Navigation](Systems/World/Navigation.md)(경로 조회 변경), [Presentation](Systems/NPC/Presentation/Adapter.md), [Combat Targeting](Systems/Combat/Targeting_and_Perception.md) |
| 노드 그리드·A*·힙·지형 비용·지역 출입구 | [Navigation](Systems/World/Navigation.md) | [Movement](Systems/NPC/Decision_and_Actions/Movement.md)(추종 연결), [Selector and Queue](Systems/NPC/Decision_and_Actions/Selector_and_Queue.md)(요청 변경), [Combat/Guard](Systems/Combat/Guard.md)(순찰 연결) |
| 건설·부지·완공 | [Construction](Systems/Settlement/Construction/README.md) | [Builder](Systems/Settlement/Construction/Builder.md), [Inventory and Items](Systems/Economy/Inventory_and_Items/README.md), [Interaction](Systems/World/Interaction_and_Destinations.md), [Navigation](Systems/World/Navigation.md) |
| Farmer·농장·씨앗 | [Farming](Systems/Economy/Farming/README.md) | [Decision Policy](Systems/NPC/Decision_and_Actions/Decision_Policy.md), [Inventory](Systems/Economy/Inventory_and_Items/README.md), [UI](Systems/UI/README.md) |
| 밭 클릭·씨앗 소비·심기 | [Farming Runtime](Systems/Economy/Farming/Runtime_and_Transactions.md) | [Farming Definition and Catalog](Systems/Economy/Farming/Definition_and_Catalog.md), [Inventory](Systems/Economy/Inventory_and_Items/README.md), [UI](Systems/UI/README.md)(source 바인딩) |
| 수확물 운반·창고 입고 | [Inventory and Items](Systems/Economy/Inventory_and_Items/README.md) | [Farming Runtime](Systems/Economy/Farming/Runtime_and_Transactions.md), [Selector and Queue](Systems/NPC/Decision_and_Actions/Selector_and_Queue.md), [Interaction](Systems/World/Interaction_and_Destinations.md) |
| Guard 순찰·판단 | [Combat/Guard](Systems/Combat/Guard.md) | [Targeting](Systems/Combat/Targeting_and_Perception.md), [Attack](Systems/Combat/Attack_Runtime.md), [Decision Policy](Systems/NPC/Decision_and_Actions/Decision_Policy.md) |
| Enemy 판단·stat | [Combat/Enemy](Systems/Combat/Enemy.md) | [Targeting](Systems/Combat/Targeting_and_Perception.md), [Attack](Systems/Combat/Attack_Runtime.md), [Spawning](Systems/NPC/Runtime/Spawning_and_Pooling.md) |
| 감지·target lifecycle | [Combat/Targeting and Perception](Systems/Combat/Targeting_and_Perception.md) | [Guard](Systems/Combat/Guard.md) 또는 [Enemy](Systems/Combat/Enemy.md) |
| 공격·사거리·damage | [Combat/Attack Runtime](Systems/Combat/Attack_Runtime.md) | 공격 role 문서 |
| 북쪽 성벽 방어·궁병·도주·시설 피해·복구 | [Defense](Systems/Combat/Defense/README.md) | [Combat](Systems/Combat/README.md), [Navigation](Systems/World/Navigation.md), [Builder](Systems/Settlement/Construction/Builder.md), [Construction](Systems/Settlement/Construction/README.md), [Town Hall](Systems/Settlement/Town_Hall.md) |
| 습격 웨이브·이벤트·게임 정지 | [Defense Progression](Systems/Combat/Defense/Progression.md) | Defense, [Spawning and Pooling](Systems/NPC/Runtime/Spawning_and_Pooling.md), [UI](Systems/UI/README.md) |
| destination·provider | [Interaction and Destinations](Systems/World/Interaction_and_Destinations.md) | [Decision Policy](Systems/NPC/Decision_and_Actions/Decision_Policy.md), 소비 domain |
| item·warehouse·CSV | [Inventory and Items](Systems/Economy/Inventory_and_Items/README.md) | 생산 또는 interaction 문서 |
| 골드 잔액·획득·지출 | [Player Gold](Systems/Economy/Inventory_and_Items/Shared_Resources.md) | [Merchant Caravan](Systems/Economy/Merchant_Caravan.md), [Town Hall](Systems/Settlement/Town_Hall.md)(현재 gameplay 소비·획득 지점) |
| 상단 방문·타이머·거래 | [Merchant Caravan](Systems/Economy/Merchant_Caravan.md) | [Player Gold](Systems/Economy/Inventory_and_Items/Shared_Resources.md), [Inventory and Items](Systems/Economy/Inventory_and_Items/README.md), [UI](Systems/UI/README.md) |
| 시청 모집·정착지원금·예약 스폰 | [Town Hall](Systems/Settlement/Town_Hall.md) | [Player Gold](Systems/Economy/Inventory_and_Items/Shared_Resources.md), [Spawning and Pooling](Systems/NPC/Runtime/Spawning_and_Pooling.md), [UI](Systems/UI/README.md) |
| animation·Flip·도구 | [NPC Presentation](Systems/NPC/Presentation/Adapter.md) | 이를 호출하는 action 문서 |
| 주민 생각·지정 대사 | [NPC Messages](Systems/NPC/Presentation/Messages.md) | [NPC Runtime](Systems/NPC/Runtime/Lifecycle_and_Stats.md), [UI](Systems/UI/README.md), [Localization](Systems/UI/Localization.md) |
| popup·hover·gauge·click 감지 | [UI](Systems/UI/README.md) | 데이터를 제공하는 domain 문서 |
| 문구·언어·LocalizeKey·Text/TMP 표시 | [Localization](Systems/UI/Localization.md) | [UI](Systems/UI/README.md), singleton 접근 변경 시 ARCHITECTURE |
| role·prefab·pool | [Spawning and Pooling](Systems/NPC/Runtime/Spawning_and_Pooling.md) | 생성되는 role 문서 |
| Codex Skill·작업 위임·Unity 자동 검증 | [Harness Architecture](HARNESS_ARCHITECTURE.md) | 변경 대상 gameplay 기능 문서, 필요 시 ARCHITECTURE |

상위 게임 규칙을 설계하거나 변경하면 먼저 `Game_Plan.md`와 `SPEC.md`를 읽는다. 여러 기능의 책임이나 의존 방향을 바꾸면 `ARCHITECTURE.md`, C#을 수정하면 `CodeConvention.md`를 추가로 읽는다.

## 4. 최상위 폴더 책임

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Actor` | scene에 존재하는 actor root, facility component와 scene 경로 서비스 |
| `Assets/Scripts/Manager` | scene 조립, registry 진입점과 scene 단위 단일 접근점 runtime 상태 |
| `Assets/Scripts/System/Actor` | actor runtime state, selector, Unity adapter |
| `Assets/Scripts/System/Navigation` | 순수 노드 그리드, A*와 내부 힙 |
| `Assets/Scripts/System/Action` | `IAction` 실행 구현 |
| `Assets/Scripts/System/Farming` | 농경지 runtime 상태와 작물 표현 |
| `Assets/Scripts/System/Inventory` | 시설과 NPC 운반 inventory runtime 상태 |
| `Assets/Scripts/System/Lib` | 둘 이상의 흐름이 공유하는 계산·pool·registry 보조 |
| `Assets/Scripts/System/Mapper` | 외부 row를 runtime value로 변환 |
| `Assets/Scripts/UI` | UI facade, routing과 concrete view lifecycle |
| `Assets/Scripts/Interface` | 여러 영역이 소비하는 안정적인 최소 계약 |
| `Assets/Data/Struct` | 경계 사이를 전달하는 작은 request/result/value |
| `Assets/Data/ScriptableObject/Script` | 공유 definition, cost와 tuning 타입 |
| `Assets/TestOnly` | production이 의존하지 않는 수동 검증 도구 |
| `Assets/Art/Generated/Buildings` | `TownHall`, `Inn`, `Restaurant`, `Warehouse`, `GuardPost`, `MerchantCaravan`, `Farm`별 건물·밭 이미지와 `House`의 1~4단계 주택 스프라이트 시트 |
| `Assets/Art/Generated/Crops` | `Carrot`, `Potato`별 성장 단계와 아이콘, 당근 리깅 이미지는 `Carrot/Rigged` |
| `Assets/Art/Generated/NPC` | 사람 NPC 이미지 |
| `Assets/Art/Generated/Defense` | 성벽 상태, 건물 잔해, 궁병 활과 화살 이미지 |
| `Assets/Art/Generated/UI`, `Assets/Art/Generated/Tiles` | UI 패널과 타일 이미지·Tile 에셋 |
| `Assets/Editor/NpcHarness` | 제한된 원자적 Unity 편집 Tool, Job 실행기, 검증기, UI Toolkit 창과 Batch 진입점 |
| `Tools/NpcHarness` | 자연어 해석 없이 명시적인 `verify`, `run-adapter`, `self-test`를 제공하는 cross-platform 결정적 runner |

`Assets/_Recovery`는 Unity 복구 산출물이며 runtime 구조의 일부가 아니다. 현재 `.asmdef`가 없으므로 production과 TestOnly C#은 기본 `Assembly-CSharp`에 함께 컴파일된다.

## 5. 의존 방향

```text
Manager / scene composition
  -> actor runtime + selector
     -> policy / registry / pool
        -> interface + value object
     -> action
        -> injected context + capability
UI input -> IUIService <- domain IHoverInfoSource
```

- manager는 action 세부 실행이나 role utility를 소유하지 않는다.
- selector는 무엇을 할지와 queue 구성을, action은 선택된 행동의 실행을 소유한다.
- provider는 domain transaction을 실행하고 다음 행동을 선택하지 않는다.
- scene별 mutable 상태는 scene component, 공유 definition과 tuning은 ScriptableObject가 소유한다.
- UI와 domain은 concrete type 대신 작은 service/source 계약으로 연결한다.

## 6. 새 기능 배치

| 추가 항목 | 시작 위치 |
|---|---|
| 새 action | `System/Action` 구현, `ActionType`, `ActionPool`, 사용하는 selector |
| 새 role | selector, 필요한 stat/definition, `NPCManager` creation entry |
| 새 destination | `BuildingType`, scene `DestinationDB` row, 공통 provider registration |
| 새 facility runtime state | 해당 domain의 scene component |
| 새 item 운반·입고 흐름 | `ICarriedInventory` 소비 provider, `InteractionRequest.Cargo`, 실행 action, [Inventory and Items](Systems/Economy/Inventory_and_Items/README.md) |
| 새 경로 지형·지역·출입구 | [Navigation](Systems/World/Navigation.md)의 profile과 scene 조립; 이동 실행 변경은 [Movement](Systems/NPC/Decision_and_Actions/Movement.md) |
| 새 gameplay 난수 | `IRandomSource`를 주입받는 domain 계산 |
| 새 popup·hover | category enum, base view 구현, `UIManager` registry |
| 새 prefab 형태 | `NPCPrefabType`, prefab catalog, pool/spawn 조립 |
| 새 crop | `FarmProductionDefinition` asset, `CropCatalog` 등록, 결과 item CSV row, [Farming](Systems/Economy/Farming/README.md) |
| 새 골드 획득·지출 지점 | `ResourceManager.TryExchange`/`TrySpend` 호출자, [Player Gold](Systems/Economy/Inventory_and_Items/Shared_Resources.md) |
| 새 골드 소비 domain(업그레이드 등) | 그 domain 전용의 새 작은 provider가 `ResourceManager`를 직접 참조. 기존 provider(예: `MerchantTradeSite`, `TownHallRecruitment`)를 거치지 않는다 |
| 새 world click 대상 | `IClickPopupSource` 구현, `Clickable` 레이어 collider, [UI](Systems/UI/README.md) |

구체 절차와 불변 규칙은 표의 대상 기능 문서를 따른다.

## 7. 기능 문서 분할 규칙

계층 배치·분할 기준은 [Systems 문서 안내](Systems/README.md#계층-분할-기준)가 소유한다. `Systems` 루트에는 안내 README와 대기능 폴더만 두고, 각 기능 폴더 아래에는 하위 기능 폴더 또는 leaf를 둔다. 하위 기능은 같은 규칙으로 재귀 분할하며 고정 깊이를 두지 않는다.

한 leaf의 독립 세부 기능이 4개 이상이면 폴더형 인덱스와 하위 문서로 분할한다. 상위 책임을 묶는 폴더에는 이 최소 개수를 적용하지 않는다. 각 README는 바로 아래 기능의 선택과 관계를 안내하고, leaf는 상세 흐름·주 소유 파일·최소 확인 범위를 소유한다.

모든 production C#은 하나의 leaf에서만 주 소유한다. 다른 문서는 링크로 연결한다.

## 8. 문서 갱신 규칙

- 파일 추가·이동·삭제: 주 소유 기능 문서의 파일 표를 갱신한다.
- 책임·흐름·Unity 배선 변경: 해당 leaf 문서를 갱신한다.
- 한 leaf의 독립 세부 기능이 4개에 도달: 기능 인덱스와 하위 문서로 분할한다. 각 하위 기능에도 같은 기준을 적용한다.
- 기능 폴더 추가·문서 이동·분할: 부모 README의 선택표와 활성 문서·지침의 링크, 이 문서의 작업별 진입 경로를 함께 갱신한다.
- 여러 시스템의 의존 방향 변경: `ARCHITECTURE.md`도 갱신한다.
- 보편적 C# 작성 규칙 변경: `CodeConvention.md`도 갱신한다.
- 문서와 코드가 다르면 코드를 현재 사실로 보고 같은 작업에서 문서를 교정한다.
