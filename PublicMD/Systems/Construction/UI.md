# Construction UI

## 책임과 최소 확인 범위

기존 PointerClickRouter → IClickPopupSource → UIManager → PopBase 경로로 선택 부지를 연다. UI는 조회·선택·요청 전달을 담당하며 지불·환불·예약 검증은 BuildingPlot API가 수행한다.

| 주 소유 파일 | 책임 |
|---|---|
| Assets/Scripts/UI/ConstructionPopup.cs | 부지 목록·선택 상세·비용·진행·취소와 구독 수명 |
| Assets/Scripts/UI/ConstructionChoiceRow.cs | 건물 카드의 이미지·이름·선택 표시와 선택 의도 |

변경 시 위 코드와 WarmPixelConstructionPopup.prefab, BuildingDataContext 읽기 API, BuildingPlot/IClickPopupSource/PopBase/UIManager를 먼저 확인한다. 공용 비용 행과 화면 맞춤은 [UI](../UI.md), 부지 상태/transaction은 [Plots](Plots.md)가 소유한다.

## 표시와 상태

- 부지 Definitions와 IsAllowed에서 가변 목록을 구성하고 정의 순서의 첫 유효 시설을 기본 선택한다. 목록이 없으면 빈 안내를 표시한다. 주택 고단계는 신규 건설 목록에 넣지 않는다.
- 선택한 정의의 이미지·이름·비용·제공 용량을 읽는다. 역할 설명은 UI의 명시적 설정이다. 선택은 자원을 변경하지 않으며 확정만 TryStartConstruction을 호출한다.
- ResourceCostRowView는 모든 비용 항목의 이름·아이콘·필요/보유량과 부족 색을 표시한다. 아이콘이 없어도 이름과 수량은 유지한다. 고정 세 재료 binding과 전체 텍스트 fallback은 사용하지 않는다.
- 공사 중에는 진행률·작업 인원·예상 환불·취소를 표시한다. 환불 행은 반환/보유량으로 표시하며 부족 색을 적용하지 않는다. 완공 실패는 일반 안내를 유지한다.
- 완공·폐허 등 source가 더 이상 건설 팝업을 제공하지 않으면 닫는다. 취소 후 빈 부지에서는 선택 목록으로 돌아간다.
- ResourcesChanged와 부지 StateChanged를 구독하며 매 프레임 재고를 조회하지 않는다. 재고 갱신에서는 선택과 스크롤을 유지하고 대상 전환·닫기·disable에서는 구독/선택을 정리한다. 건물 선택은 비용 스크롤을 맨 위로 돌린다.

## WarmPixel 배선

DefenseTest는 WarmPixelConstructionPopup.prefab instance를 PopupUI 아래 비활성 상태로 등록하고 Defense 전용 BuildingDataContext를 연결한다. 기존 ConstructionPopup prefab과 다른 씬은 보존한다. 새 표시 구조를 위한 별도 옛 배치 호환 모드는 없다.

1440×920 루트는 Background/Header/Body로 나눈다. 왼쪽 BuildingGallery의 ScrollRect·RectMask2D·2열 GridLayoutGroup과 ContentSizeFitter가 실제 카드 수에 맞춰 세로 목록을 구성한다. 카드 크기는 360×214, 간격은 18×16이다. 오른쪽 WarehouseDetail은 Overview/ConstructionCost/Footer로 나누며 선택 내용과 시작/취소를 연결한다. 비활성 Templates에 카드와 비용 행 원본을 보관한다. 비용도 독립 ScrollRect로 표시한다.

건물 그림은 기존 BuildingDataContext Sprite를 재사용한다. 카드 BuildingImage는 332×160 영역에서 비율을 유지하고 바닥선을 맞춘다. 헤더는 기존 WarmPixel 높이82·아이콘40·제목30·닫기32 규격과 construction-header-icon.png 도트 아이콘을 유지한다. 이미지/importer 변경은 없다. 작은 화면에서는 PopupCanvasFit이 루트만 비율 축소하며 내부 배치를 재배열하지 않는다.

2026-10-18에는 고정 창고/여섯 카드와 예시 보유량을 가진 시안이었다. 현재 목록·선택·상세·실제 비용·버튼은 controller에 연결되며 예시 내용은 사용하지 않는다. 옛 목재 프리팹용 조립 도구는 이번 전환에 실행하지 않는다.

컴파일·직렬화 참조·독립 리뷰 결과는 PROGRESS와 작업 기록을 따른다. 실제 클릭·스크롤·공사·작은 화면 확인은 사람 QA이며 자동 Play/화면 검증은 실행하지 않는다.
