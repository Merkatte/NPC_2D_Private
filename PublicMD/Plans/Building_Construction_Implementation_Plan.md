# 지정 부지 건설·공용 자원 — 승인 구현 계획

> 2026-09-27: Plan Mode에서 설계·추가 선택을 확정하고 사용자가 `Implement the plan.`으로 구현을 승인했다.
> 확정 규칙의 원문은 [Building_Construction_Context](Building_Construction_Context.md) 1~8절이다. 이 계획은 해당 규칙을 대체하거나 축소하지 않는다. 실제 완료·검증 상태는 PROGRESS에 기록한다.

## 1. 목표와 작업자 필수 계약

빈 지정 부지 클릭 → 시설 선택·비용 확인 → 즉시 지불 → 건축가의 출발 전 예약·이동·작업 → 시설 생성·기존 기능 연결을 구현한다. 첫 대상은 식당(Pub), 숙소, 창고, 초소, 농경지이며 시청은 기존 하나를 유지한다. 모든 부지에서 모든 다섯 시설을 허용하며 선택적 허용 ID 목록도 제공한다.

모든 코드·조립 작업자와 독립 리뷰어는 컨텍스트 8절과 이 문서를 필수 입력으로 읽는다. 임의의 구조 재선택은 금지한다. 구조 충돌이나 누락은 루트에 보고한다. 기존 CodeConvention/Systems와 승인 설계를 함께 적용하고 변경된 현재 사실은 주 소유 문서에서 교정한다.

- BuildingPlot은 지속되는 씬 부지와 상태 변경 진입점, ConstructionState는 부지별 일반 C# 상태다. 상태는 Empty/UnderConstruction/Completed뿐이다.
- BuildingDefinition은 CSV 파싱 결과인 불변 C# 정의다. 입력 컬렉션을 복사하고 내부 가변 참조를 노출하지 않는다.
- selector는 선택·예약·queue 구성, BuildAction은 선택한 공사의 이동·작업·정리, WorkerNPC는 기존 queue lifecycle만 담당한다.
- 실제 부지가 IInteractionProvider다. 예약 객체는 provider가 아니며 ActionContext에 건설 전용 provider/state 필드를 추가하지 않는다.
- BuildingFactory는 시설 생성·주입·초기화·등록을, 부지는 비용·진행도·환불을 소유한다.
- ResourceManager는 공개 API와 알림, plain ResourceInventory는 실제 수량을 소유한다. 골드 별도 저장소와 창고별 재고는 제거한다.
- 수리, 철거 실행, 업그레이드, 특수자원, 목재/석재 획득 gameplay, Addressables, save/load, 후속 범용 프레임워크는 제외한다.

## 2. 데이터와 자원

### 승인 데이터

| 데이터 | 계약 |
|---|---|
| BuildingData.csv | buildingId,buildingType,displayName,requiredWork,maxWorkers,providedCapacity |
| BuildingCost.csv | buildingId,itemId,quantity; 같은 buildingId와 기존 itemId로 결합 |
| BuilderWork.csv | workPerSecond,hungerPerSecond,thirstPerSecond,fatiguePerSecond; 1개 설정 행 |
| ItemData.csv | 기존 열 뒤에 usesStorage,showInWarehouse bool 열 추가 |
| 자원 ID | 기존 1~7 유지, Gold=8, Wood=9, Stone=10 |
| 건물 ID | Warehouse=1, Restaurant/Pub=2, Inn=3, GuardPost=4, Farm=5 |
| 모든 건물 첫 값 | 골드100·목재20·석재10, requiredWork100, maxWorkers2 |
| 제공 용량 | Warehouse500, 나머지0 |
| 건축가 | 작업 중 10/초; 이동·작업 중 허기·갈증·피로 각각0.3/초 |

건물 종류는 기존 BuildingType 이름으로 파싱하고 기존 enum 수치는 보존한다. 새 enum 항목은 끝에 추가한다. BuildingDataContext SO는 CSV 및 ID별 프리팹·아이콘 연결을 보관하고 수치를 수동 복제하지 않는다. IDataManager/DataManager는 TryGetBuildingDefinition과 읽기 전용 목록 조회를 위임한다. 파일/행/ID 오류, 중복, 비양수/비유한 수치, 존재하지 않는 아이템, 에셋 누락을 전체 검증한 뒤 일괄 공개한다. 오류 상태의 부분 목록 공개는 금지한다. 기존 모든 데이터의 적재 정책 전환은 범위 밖이다.

BuildActionCost는 기존 DefaultActionCost 전달 경로를 사용하되 CSV의 불변 작업 설정을 소비한다. 긴급 욕구 계산은 기존 판단 설정의 공통 메서드로 공유하고 selector/action에 임계값이나 공식을 복제하지 않는다.

### ResourceManager

합의 API: GetQuantity, TryDeposit(itemId,quantity,out acceptedQuantity), TrySpend(cost), TryRefund(amounts). 추가 승인 API: TryExchange(debits,credits), DeferNotifications() 동기 범위. 내부 inventory dictionary는 공개하지 않는다. IInventory의 명시적 TryAdd 구현은 TryDeposit으로 연결한다.

- Spend/Exchange는 전량 사전 검증 후 전량 반영한다. 판매의 차감과 골드 지급은 Exchange 한 번이다.
- 모집·심기·입고·건설은 자기 도메인 상태/cargo까지 확정한 뒤 ResourcesChanged를 한 번 발행한다. 중첩 알림 보류는 가장 바깥 Dispose에서 처리하고 yield/frame을 넘기지 않는다.
- 구독자 예외가 이미 성공한 거래를 실패처럼 보이게 하지 않으며 재진입 알림은 현재 발행 이후 처리한다. 변경 없는 성공/실패는 이벤트를 내지 않는다.
- 기본 용량0 + 창고별 제공량. 인스턴스를 키로 등록·갱신·해제하며 반복 호출은 중복 증감을 만들지 않는다. 일시 disable은 용량 유지, 실제 제거/파괴는 해제한다.
- Gold는 usesStorage=false/showInWarehouse=false. 일반 물품은 true/true이며 개당 용량1. 슬롯/99개 제한은 없다.
- 입고는 부분 수락하고 잔량은 cargo에 남긴다. 공간 없음은 정상 availability=false이며 농부는 cargo 유지·Idle 후 재판단한다.
- 환불은 용량을 초과해도 전량 반환한다. 초과 재고 보존, 공간 회복 전 일반 입고 금지.
- int overflow는 사전 검사하여 무변경 실패한다. 포화/유실은 금지하며 취소 공사 또는 미환급 기록을 보존한다. 빈 Spend/Refund는 변경 없는 성공, 잘못된 ID나 비양수 명세 항목은 실패. 환불 계산의 0 항목은 제외한다.
- GoldManager/WarehouseInventory의 모든 호출자·TestOnly·검사·Unity 참조를 이관한 후 구 구현을 제거한다. 기존 골드0/씨앗 각5 시작값은 기존 씬에서 유지하고 TestOnly로 이전한다.

## 3. 부지·예약·action

BuildingPlot은 건설 시작·취소·예약·명시적 완공 재시도 API를 제공하며, 작업량은 기존 TryInteract 경로로 받는다. 외부는 ConstructionState를 수정하지 않는다. 상태 검사와 처리 중 표시로 재진입/중복 지불/환불/완공을 차단한다.

- 건설 성공 후 실제 지불 비용 사본과 신청 순서를 보관한다. registry는 부지 후보/신청 순번만 제공하고 selector가 먼저 신청된 빈자리부터 예약한다.
- ConstructionReservation은 부지·공사·예약·고정 작업 위치를 식별한다. IInteractionReservation : IDisposable의 IsValid를 제공하며 Optional InteractionRequest.Reservation으로 동일 참조를 전달한다.
- 부지는 매 요청에 발급 주체·현재 공사·자리 유효성을 검증한다. 인원이 꽉 찬 공사도 기존 유효 예약자는 작업할 수 있다.
- 최초 양수 작업량 반영 때 HasWorkStarted=true. 작업량은 deltaTime 기반 합산이고 요구량에서 clamp한다.
- 작업 전 취소는 전액, 이후는 자원별 floor(actualPaid * remainingProgress). 취소 후 같은 부지의 새 공사에 과거 예약이 영향을 주지 않는다.
- BuildAction은 NPCPathFollower로 이동하고 도착 후 작업 표현을 켠다. 긴급 욕구/취소/경로 실패는 예약 정리와 재판단. 일반 MoveAction에 Builder 분기를 넣지 않는다.
- 예약에는 완료를 구분하는 종료 정보를 보존한다. 다른 건축가가 완공한 경우도 정상 종료 가능해야 한다. InteractionResult에 건설 전용 payload를 추가하지 않는다.
- 인계 전 실패는 selector, 이후 완료·중단·실패·Clear는 BuildAction이 정리한다. Dispose/Stop/Clear 반복과 부분 초기화는 안전해야 한다.

## 4. Factory·시설 선택·UI

Factory는 비활성 생성 → 의존성 주입 → 명시적 초기화 → 성공 확인 → 목적지/provider/용량 등록 → 성공 결과 순서다. GuardPost Awake와 FarmSeedSource의 주입 전 실패 latch를 피한다. 프리팹의 실제 입구 anchor를 부지 입구에 맞추고 실제 provider(식당은 입구 자식)를 연결한다. Inn의 기존 위치 기반 Sleep에는 불필요한 provider를 추가하지 않는다.

Farm의 작업 영역과 Guard 순찰 영역은 Plot 외곽 영역으로 주입한다. 새 Farm은 빈 밭이고 위치/수확 난수원을 분리한다. prefab별 참조는 명시적 조립 계약으로 캐시하고 hot-path component 검색은 금지한다.

생성 실패 시 해당 시도의 객체와 등록만 정리한다. 공사 정보/100% 진행률/UnderConstruction 유지, 예약 무효화와 추가 작업 차단, 자동 반복 재시도 금지. Plot의 명시적 API와 TestOnly 조작으로 비용·작업 없는 재시도를 수행한다. 성공 전 Completed 금지.

DestinationDB는 종류별 다중 시설, InteractableManager는 동적 등록/해제를 지원한다. NPCDecision에 선택 시설 identity/provider를 유지하여 selector가 종류만으로 재조회하지 않는다. Farmer Harvest/Deposit, Guard duty fallback, Builder needs 모두 이관한다. 이용 가능한 후보를 등록된 실제 입구/상호작용 anchor의 거리 제곱으로 한 번 순회(O(N))해 선택하고 동률은 등록 순서다. Farm/Guard의 영역 내 작업/순찰 난수 위치는 시설 선택 후에만 한 번 추출하며, 후보 비교나 look-ahead에서 gameplay 난수를 소비하지 않는다. 경로 길이를 후보 비교에 사용하지 않는다. 선택 목적지 이동은 기존 경로 실패 대기·재판단을 유지한다.

기존 IClickPopupSource→UIManager→PopBase에 건설 popup과 공용 창고 popup을 연결한다. 건설 popup은 목록/보유량/비용/선택확정, 진행/예약인원/취소/예상환불, 완공 실패 상태를 표시한다. 기술 진단/재시도는 TestOnly. 완료 부지의 공사 click source는 종료해 FarmSeedSource 등과 경쟁하지 않는다. 창고 popup은 공용 재고/총용량 표시, Gold 목록 제외. UI는 open/bind 시 구독+최초조회, close/disable 시 해제하며 자원 polling 금지.

## 5. 에셋·씬

Assets/Scenes/BuildingTest.unity를 FarmerTest 구성에 기반해 신설한다. 기존 계층 InGameObjects/Map, Systems/Manager·Data·ActionSelector·Navigation, Canvas/PopupUI·HoveringUI, Grid, TestOnly!!를 유지한다.

- 부지6개, 각 고정 작업점2개, 초기 창고1개(500), 건축가2명, 시청1개와 기존 생활/모집 구성.
- TestOnly 시작 재고: Gold1000, Wood200, Stone200, 씨앗6/7 각5. production API로 지급/생성한다.
- 부지 footprint는 빈 상태부터 blocked. 외곽 FacilityWorkArea와 NavigationEntrance도 초기 snapshot부터 등록한다. footprint/지역/blocked tile 중첩 금지, 입구의 외부 연결 확인. 완공/취소 시 topology 변경 없음.
- 새 Farm은 외곽 작업/내부 작물 표현. 기존 FarmerTest/GuardTest에는 부지를 추가하지 않고 필요한 자원·목적지 호환 참조만 이관한다.
- Restaurant/Inn/Warehouse/Soil/TownHall/NPCGirl 및 기존 UI 모양 재사용. GuardPost는 현재 scene 객체를 독립 prefab으로 만든다.
- 신규 그래픽은 Construction/material-pile.png(768x512), Construction/scaffolding.png(1536x1024)의 2종. 기존 정면 건물 목재/석재/윤곽/팔레트, 투명 배경, 글자 없음, 정적 sprite. PPU150, bottom-center pivot, Single, Bilinear, mipmap off, alpha transparency. 나머지 import는 reference를 따른다. code와 독립 병행, assembly가 import한다.

## 6. 역할·일정·검증

오케스트레이터 ordinary common checks 경로. reviewPolicy=RequiredByRequestOrSkill. candidate remediationBudget=2. 신규 harness profile 금지. 원본 프로젝트에서 범위 제한 변경, unrelated dirty Packages와 기존 문서 변경 보존. Unity import/compile/save 순차. 자동 Play Mode/게임 화면 확인 및 이를 위한 검사 코드는 금지한다.

1. Root: baseline/assignment/연결계약과 문서, 실제 diff/증거 확인.
2. Code worker($author-unity-code): 데이터·자원/기존 소비자 → 부지/예약/BuildAction → Factory/다중시설/UI/TestOnly와 필요한 명시적 Editor 조립 helper. 코드만 작성, 에셋 mutation helper 실행 금지.
3. Graphics worker($create-project-sprites): 두 이미지와 importSpec. 코드와 병행, scene/meta 금지.
4. Assembly worker($assemble-unity-objects): read-only 준비 병행, code/art 준비와 컴파일 후 승인된 scene/prefab/SO/CSV/import 배선. C# helper는 code worker에게 요청한다.
5. 별도 검증 코드 담당: 기존 구 타입/구조 참조 검사와 비-Play regression을 이관. 구현 작성자가 accepting gate/인수 기준을 약화시키지 못하게 분리.
6. Independent reviewer($reviewing-unity-candidate): 구현에 참여하지 않은 read-only agent. 성공한 현재 common checks/실제diff/관련규칙만 제공, 작성자의 자기평가 제외. 실제 결과 수신 후 판단.

모든 worker/reviewer는 fork_turns=none이며 컨텍스트8절/승인계획/관련leaf/정확한 허용파일/금지영역/증거요구를 명시한다. 변경 경로와 before hash를 대조한다. 코드·이미지·조립 역할을 합치거나 루트가 대신 구현하지 않는다.

필수 검증: 신규 파일을 실제 포함한 runtime/Editor compile; diff/scope; 기존 enum/GUID 보존; 삭제 타입 잔존참조; source/provider/scene 참조; CSV 검증; 독립 컨벤션/책임/의존방향 리뷰. 기존 구조 gate는 구 단일 창고 가정을 승인 구조로 별도 이관하며 과거 Pass를 재사용하지 않는다.

비-Play 의미있는 회귀: 다중 지출/교환 원자성, 부분입고 총량, 초과환급, 이벤트 재진입과 도메인 commit 관측, 정의/비용 사본 불변성, 이동중 전액/진행후버림환불, 2인합산/정원/과거예약/반복정리/pool재사용, 실패완공100%보존/무비용재시도/한번등록, 다중시설 identity/거리동률, 기존 판매/모집/심기.

컴파일/필수검사/현재 후보의 독립 Approve 이후에만 구현 완료를 기록한다. 사람의 Play/화면 확인(공사 표현·생활중단복귀·완공시설 이용)은 미검증으로 짧게 인계한다. 관련 Systems·상위구조 문서·PROGRESS를 실제 결과에 맞게 갱신한다.

## 구현 중 확정한 연결 사항

- 재료 더미 생성 결과는 1536×1024이며 픽셀을 재가공하지 않고 PPU300으로 적용한다. 승인된 768×512@150과 월드 크기는 같다. 비계는 1536×1024@150을 유지한다.
- 새 click source는 기존 FarmSeedSource가 있는 Soil 오브젝트의 실제 layer를 사용하며 ProjectSettings의 layer 이름을 변경하지 않는다. 최종 저장 구성은 layer7(Hoverable)이고 기존 PointerClickRouter의 mask에 포함된다.
- 기존 prefab/scene의 씨앗 source와 작업 영역을 보존하며, 새 Farm에는 별도 scene 참조 없이 Factory가 작업 영역과 공유 자원을 주입한다.
- 기존 지도에는 여섯 부지의 추가 공간이 없어 새 BuildingTest에만 기존 지면 타일 660칸을 오른쪽으로 추가하고 고정 3×2 부지를 배치한다. 원래 타일과 FarmerTest/GuardTest 지형은 유지하며 TestOnly 카메라 이동 범위만 새 공간에 맞춘다. Soil/GuardPost의 건설용 입구 anchor는 외곽 작업 영역과 내부 표현이 겹치지 않도록 맞춘다.
- 현재 검증 및 완료 여부는 PROGRESS를 따른다. 작성자의 컴파일 성공이나 이미지 전달만으로 전체 후보를 수용하지 않는다.
