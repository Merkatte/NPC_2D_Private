# Project Structure

> 문서 기준일: 2026-09-06
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

| 기능 영역 | 문서 | 읽는 경우 |
|---|---|---|
| NPC 공통 runtime | [NPC Runtime](Systems/NPC_Runtime.md) | queue 소비, 공통 stat, disable/reset |
| 판단과 action | [NPC Decision and Actions](Systems/NPC_Decision_and_Actions/README.md) | utility, selector, action lifecycle, 이동, 생활 action |
| 농사 | [Farming](Systems/Farming/README.md) | 농장 progress, 수확 transaction, 생산 definition, 씨앗 선택·작물 표현, Farming/Harvest action |
| 전투 | [Combat](Systems/Combat/README.md) | 감지, target, 공격, Guard, Enemy |
| 상호작용과 목적지 | [Interaction and Destinations](Systems/Interaction_and_Destinations.md) | provider, destination, 건물 action |
| 아이템과 inventory | [Inventory and Items](Systems/Inventory_and_Items.md) | CSV, item data, 창고와 NPC 봇짐, 운반·입고 transaction, cost registry |
| 플레이어 골드 | [Player Gold](Systems/Player_Gold.md) | 전역 골드 잔액, 획득·지출 transaction |
| 상단(Merchant Caravan) | [Merchant Caravan](Systems/Merchant_Caravan.md) | 방문 phase·타이머, 클릭 가능 판정, 거래 transaction |
| 시청(Town Hall) | [Town Hall](Systems/Town_Hall.md) | 모집 쿨다운·phase, 정착지원금 transaction, 예약 기반 NPC 스폰, 낙하 연출 |
| NPC 표현 | [NPC Presentation](Systems/NPC_Presentation.md) | 이동 animation, Flip, 건물·도구 표현 |
| UI | [UI](Systems/UI.md) | popup, hover, gauge, pointer routing |
| 생성과 pooling | [Spawning and Pooling](Systems/Spawning_and_Pooling.md) | role 생성, prefab catalog, worker pool |

판단·action, 농사, 전투는 독립 세부 기능이 4개 이상이므로 폴더 `README.md`가 필요한 leaf 문서를 다시 선택한다. 상위 README에는 전체 파일 목록이 없다.

## 3. 작업별 읽기 라우팅

| 작업 | 필수 기능 문서 | 조건부 추가 문서 |
|---|---|---|
| queue 처리·공통 stat | NPC Runtime | Action Runtime, Spawning |
| utility·위험·look-ahead | Decision Policy | Interaction, role 문서 |
| selector·새 action | Selector and Queue, Action Runtime | concrete action leaf |
| 이동·동적 target | Movement | Presentation, Combat Targeting |
| Farmer·농장·씨앗 | Farming | Decision Policy, Inventory, UI |
| 수확물 운반·창고 입고 | Inventory and Items | Farming Runtime, Selector and Queue, Interaction |
| Guard 순찰·판단 | Combat/Guard | Targeting, Attack, Decision Policy |
| Enemy 판단·stat | Combat/Enemy | Targeting, Attack, Spawning |
| 감지·target lifecycle | Combat/Targeting and Perception | Guard 또는 Enemy |
| 공격·사거리·damage | Combat/Attack Runtime | 공격 role 문서 |
| destination·provider | Interaction and Destinations | Decision Policy, 소비 domain |
| item·warehouse·CSV | Inventory and Items | 생산 또는 interaction 문서 |
| 골드 잔액·획득·지출 | Player Gold | Merchant Caravan, Town Hall(현재 gameplay 소비·획득 지점) |
| 상단 방문·타이머·거래 | Merchant Caravan | Player Gold, Inventory and Items, UI |
| 시청 모집·정착지원금·예약 스폰 | Town Hall | Player Gold, Spawning and Pooling, UI |
| animation·Flip·도구 | NPC Presentation | 이를 호출하는 action 문서 |
| popup·hover·gauge·click 감지 | UI | 데이터를 제공하는 domain 문서 |
| role·prefab·pool | Spawning and Pooling | 생성되는 role 문서 |

상위 게임 규칙을 설계하거나 변경하면 먼저 `Game_Plan.md`와 `SPEC.md`를 읽는다. 여러 기능의 책임이나 의존 방향을 바꾸면 `ARCHITECTURE.md`, C#을 수정하면 `CodeConvention.md`를 추가로 읽는다.

## 4. 최상위 폴더 책임

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Actor` | scene에 존재하는 actor root와 facility component |
| `Assets/Scripts/Manager` | scene 조립, registry 진입점과 scene 단위 단일 접근점 runtime 상태 |
| `Assets/Scripts/System/Actor` | actor runtime state, selector, Unity adapter |
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
| 새 item 운반·입고 흐름 | `ICarriedInventory` 소비 provider, `InteractionRequest.Cargo`, 실행 action, [Inventory and Items](Systems/Inventory_and_Items.md) |
| 새 gameplay 난수 | `IRandomSource`를 주입받는 domain 계산 |
| 새 popup·hover | category enum, base view 구현, `UIManager` registry |
| 새 prefab 형태 | `NPCPrefabType`, prefab catalog, pool/spawn 조립 |
| 새 crop | `FarmProductionDefinition` asset, `CropCatalog` 등록, 결과 item CSV row, [Farming](Systems/Farming/README.md) |
| 새 골드 획득·지출 지점 | `GoldManager.Add`/`TrySpend` 호출자, [Player Gold](Systems/Player_Gold.md) |
| 새 골드 소비 domain(업그레이드 등) | 그 domain 전용의 새 작은 provider가 `GoldManager`를 직접 참조. 기존 provider(예: `MerchantTradeSite`, `TownHallRecruitment`)를 거치지 않는다 |
| 새 world click 대상 | `IClickPopupSource` 구현, `Clickable` 레이어 collider, [UI](Systems/UI.md) |

구체 절차와 불변 규칙은 표의 대상 기능 문서를 따른다.

## 7. 기능 문서 분할 규칙

자체 실행 흐름, 별도 변경 진입점, 독립 변경 가능성을 모두 가진 세부 기능이 한 문서에 4개 이상이면 폴더형 인덱스로 분할한다. 파일 수만으로 나누지 않는다.

```text
Systems/Feature/
  README.md       선택표, 전체 흐름, leaf 간 의존성
  SubFeature.md   상세 흐름, 주 소유 파일, 최소 확인 범위
```

모든 production C#은 하나의 leaf 또는 단일 기능 문서에서만 주 소유한다. 다른 문서는 링크로 연결한다.

## 8. 문서 갱신 규칙

- 파일 추가·이동·삭제: 주 소유 기능 문서의 파일 표를 갱신한다.
- 책임·흐름·Unity 배선 변경: 해당 leaf 문서를 갱신한다.
- 세부 기능이 4개에 도달: 기능 인덱스와 leaf 분할을 같은 변경에서 수행한다.
- 여러 시스템의 의존 방향 변경: `ARCHITECTURE.md`도 갱신한다.
- 보편적 C# 작성 규칙 변경: `CodeConvention.md`도 갱신한다.
- 문서와 코드가 다르면 코드를 현재 사실로 보고 같은 작업에서 문서를 교정한다.
