# Housing UI

## 책임과 최소 확인 범위

`Assets/Scripts/Actor/HousePopupSource.cs`는 같은 오브젝트의 주택 collider 클릭을 기존 IClickPopupSource 경로에 연결한다. Factory가 새 주택의 House와 BuildingPlot을 명시 주입한다. House는 UI나 공사 객체를 참조하지 않는다.

`Assets/Scripts/UI/HousePopup.cs`는 선택 주택의 단계·정원·거주민·현재 효과·다음 단계 전체 비교·비용/보유량·공사 진행·취소/완공 재시도를 표시한다. `Assets/Scripts/UI/HouseInfoRow.cs`는 하나의 가변 목록 행과 부족 수량 색을 표시한다. 이 세 production C#의 주 소유 문서는 이 leaf다.

변경 시 위 세 파일, House/HousingDataContext 읽기 API, BuildingPlot의 업그레이드 API, 공통 PopBase/IClickPopupSource를 먼저 확인한다. 공사 transaction은 [Construction Plots](../Construction/Plots.md), 주택 생활/입주 상태는 Housing domain 문서가 소유한다.

## 표시와 수명

- 거주민·옵션·재료를 임의 개수 행으로 생성하고 재사용한다. 전체 내용은 RectMask2D/ScrollRect/자동 높이 Content 안에 배치한다.
- 옵션 비교는 효과별 추가·유지·값 변경·제거를 모두 표시한다. 단계 옵션은 자동 상속으로 추정하지 않는다.
- 비용은 다음 buildingId 정의의 모든 itemId를 읽고 보유/필요와 부족 색을 표시한다. UI에서 아이템 종류나 단계별 비용을 하드코딩하지 않는다.
- House.Changed, Plot.StateChanged, ResourcesChanged를 구독한다. 대상 재바인딩·닫기·disable에서 전부 해제하며 소유 대상 비활성/파괴는 닫기로 처리한다. 매 frame 재고/목록을 polling하지 않는다.
- 업그레이드·취소·재시도 버튼은 Plot의 검증된 API만 호출한다. 완료 오류의 기술 문자열은 화면에 노출하지 않는다.
- 문구는 기존 ConstructionPopup의 한국어 Legacy Text 스타일을 따른다. 별도 지역화 전환은 이번 범위가 아니다.

## 조립

`Assets/Editor/HousingSetup.cs`는 assembly 역할이 실행하는 한정 authoring helper다. `Tools/NPC/Housing/Setup BuildingTest` 메뉴/`HousingSetup.Setup`은 기존 주택 sprite를 재사용해 House/HousePopup/HouseInfoRow prefab과 HousingDataContext, BuildingDataContext의 주택 연결, BuildingTest의 주거 참조만 조립한다. 기존 ConstructionPopup 목록에는 세로 스크롤을 추가한다. 저장되지 않은 씬·Play·컴파일·Prefab Mode에서는 거부하고 변경한 대상만 저장한다.

House 루트 layer7 trigger collider는 9×6/offset(0,4.5), 입구(0,0), Visual(0,1.5)/scale1.9/order0을 사용한다. 공사 중 기존 House 객체와 source는 유지하며 Plot source는 업그레이드 중 건설 팝업을 반환하지 않는다. PopupType.House=6으로 기존 UIManager registry에 추가한다.

실제 조립/컴파일/리뷰 결과는 작업 기록과 PROGRESS를 따른다. 화면·스크롤 조작·반복 열기/닫기·재료 부족·완공/취소는 사람 QA이며 자동 Play/화면 검증은 실행하지 않는다.

## WarmPixel 독립 표시용 주택창

`Assets/Scripts/UI/WarmPixelHousePopup.cs`의 주 소유 문서는 이 leaf다. 이 컴포넌트는 새 `Assets/Prefab/UI/WarmPixelHousePopup.prefab`의 표시 전용 열기·접기·닫기와 배치 전환만 소유한다. 기존 `HousePopup`·`HousePopupSource`, scene의 UIManager 등록과 주택 transaction은 교체하지 않는다. 실제 주택·자원 데이터 바인딩과 `업그레이드 시작`의 공사 요청은 연결하지 않는다.

기본 창은 1440×920이며 기존 WarmPixel 건설창의 헤더82·아이콘40·제목30·문맥28·닫기32 규격을 따른다. 처음에는 현재 2단계 주택 그림·역할·정원·효과·거주민이 전체 폭을 사용한다. `업그레이드`를 누르면 좌상단과 전체 외곽은 유지하면서 본창 폭이 1440에서 940으로 줄고, 뒤에 있던 오른쪽 500 영역의 비교 패널이 나타난다. 다음 단계 그림·효과 비교·필요/보유 비용 표·시작 버튼은 이 패널에 둔다. 텍스트와 아이콘의 scale은 줄이지 않고, 지정한 RectTransform의 위치·영역만 보간한다.

`WarmPixelHousePopup`은 Unity UI와 DOTween만 사용하며 House·BuildingPlot·ResourceManager를 조회하거나 변경하지 않는다. `_transitionDuration` 기본0.3초의 unscaled 전환을 사용하고, 연속 입력은 현재 진행도에서 반전한다. disable에서 자신이 소유한 tween과 listener를 정리하며, 다시 활성화하면 접힌 상태로 시작한다. 닫힌 비교 패널과 전환 중인 패널은 입력을 받지 않는다. 메인 닫기는 이 독립 view root를 비활성화한다.

표시 내용은 시안용 2→3단계 예시다. 정원2→3, 불만 회복 보너스0.25/초 유지, 현재 집 체류 피로 회복5/초를 표시한다. 비용200골드·40목재·20석재와 보유450·80·36은 각각 현재 정의의 비용과 예시 보유량이며, 두 거주민 카드도 실제 입주 목록에 연결하지 않았다. 비용은 기존 건설창처럼 `필요` 다음 `보유` 순서로 표시한다.

헤더 전용 `Assets/Art/Generated/UI/WarmPixel/house-header-icon.png`는 64×64 RGBA 도트 이미지다. Sprite Single·FullRect·PPU100·Point·Clamp·mipmap off·sRGB·비압축·중앙 pivot·border0을 사용하며, Simple Image의40×40 영역에 표시한다. 건물 그림은 실제 house-tiers의 2·3단계 Sprite, 거주민과 재료는 기존 에셋을 재사용한다. 후속 수정은 이 프리팹과 컴포넌트의 serialized 참조·배치 목록을 함께 확인한다.

주택창 조립 루트에는 창 Image를 두지 않는다. 본창과 업그레이드 패널의 `Background` 두 개가 각자 배경을 소유하며, 뒤에 제3의 전체 창 배경을 깔지 않는다. 두 패널은 각각 `Header`, `Body`, `Footer`로 구분한다. 본창 Body는 `HouseOverview`, `CurrentDetails`, `Residents`, 업그레이드 Body는 `NextTierOverview`, `EffectsComparison`, `Costs`로 묶는다. 효과와 비용은 행마다 해당 라벨·수량·구분선을 자식으로 갖는다. 런타임이 참조하는 기존 RectTransform·버튼과 네 배치 항목의 좌표는 계층 정리 후에도 보존한다. 공통 기준은 [UI 프리팹 계층 원칙](../UI.md#ui-프리팹-계층-원칙)을 따른다.
