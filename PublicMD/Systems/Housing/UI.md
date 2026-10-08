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
