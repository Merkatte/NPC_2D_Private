# Construction UI

## 책임과 흐름

기존 PointerClickRouter → IClickPopupSource → UIManager → PopBase 경로로 클릭한 부지의 건설 화면을 연다. UI는 선택·표시·요청 전달만 담당하고 지불/환불/예약 검증은 BuildingPlot API가 수행한다. 에셋 적용과 검증 상태는 PROGRESS를 따른다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/UI/ConstructionPopup.cs | 선택된 부지의 목록/비용/진행/취소 UI와 구독 수명 |
| Assets/Scripts/UI/ConstructionChoiceRow.cs | 한 건물 정의의 선택 표시 |

공용 WarehousePopup/ResourceQuantityRow는 Inventory and Items, 공통 PopupType/PopBase/입력은 UI.md가 주 소유한다. BuildingPlot의 source 구현은 Plots leaf 소유다.

## 최소 확인 범위와 불변 규칙

- binding/클릭 변경은 위 타입과 IClickPopupSource/PopBase/UIManager, 대상 collider/layer를 확인한다.
- 선택만으로 자원을 변경하지 않는다. 확정·취소 때 domain API가 현재 상태를 다시 검증한다.
- 열기/재바인딩에서 이전 source와 선택/구독을 정리하고 최초 조회한다. 닫기/disable에서 모든 구독을 해제한다.
- ResourceManager.ResourcesChanged와 부지 변경 알림으로 갱신하며 자원 수량을 매 frame polling하지 않는다.
- 완공 시 plot의 공사 클릭은 끝나며 FarmSeedSource 등 시설 클릭과 경쟁하지 않는다.
- 완공 실패는 상태만 표시하고 기술 진단/명시적 재시도는 TestOnly가 제공한다.

## 배선·표현·검증

ConstructionPopup prefab은 비활성으로 Canvas/PopupUI에 배치하고 registry에 등록한다. 기존 UI 패널/버튼 스타일을 재사용하며 비용/보유량/예약수/진행도/예상환불을 표시한다. 지불 후 material-pile, 첫 작업 이후 scaffolding을 표시한다. 신규 PNG는 그래픽 작업자, import/scene 배선은 조립 작업자 소유다. source 수명/연속클릭/이벤트 구독과 참조를 검사하며 게임 화면 검증은 사람이 수행한다. 표시/API/프리팹 변경 시 갱신한다.
