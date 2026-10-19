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

ConstructionPopup prefab을 Canvas/PopupUI에 배치하고 registry에 등록한다. 현재 저장된 프리팹 root는 active=1이다. 기존 UI 패널/버튼 스타일을 재사용하며 비용/보유량/예약수/진행도/예상환불을 표시한다. 지불 후 material-pile, 첫 작업 이후 scaffolding을 표시한다. 신규 PNG는 그래픽 작업자, import/scene 배선은 조립 작업자 소유다. source 수명/연속클릭/이벤트 구독과 참조를 검사하며 게임 화면 검증은 사람이 수행한다. 표시/API/프리팹 변경 시 갱신한다.

2026-10-01 팝업 표현 정리: 700×620 루트는 유지하고 상단 스타일 제목 명패·종이 본문·우상단 X를 배치한다. 목록은 260×342, 선택 행은 260×62와 간격8로 현재 5개 시설을 표시한다. 세부 정보·결과·확정/공사 취소 버튼은 서로 분리하며 원래 serialized Button/source 연결을 유지한다. 공통 닫기 기준은 [UI](../UI.md#팝업-공통-표현)를 따른다.

2026-10-08 주택 추가: 목록은 기존 다섯 시설과 주택 1단계를 표시한다. `_choiceScrollRect`의 마스크/ContentSizeFitter로 여섯 번째 및 이후 행을 스크롤한다. 선택 source의 IsAllowed가 신규 건설용 단계만 필터링한다. 주택 2~4단계는 [Housing UI](../Housing/UI.md)의 업그레이드 경로로만 진입한다.

## 건설 비용 명세서

기존 팝업 오른쪽에는 `명세서` 제목의 프레임을 배치한다. 별도 팝업이나 열기 경로는 만들지 않는다. 빈 부지에서 시설을 선택하면 명세서 안에 건물명과 `보유 / 필요` 안내를 표시하고, 골드·목재·석재 아이콘 옆에 실제 보유량과 정의의 필요량을 표시한다. 부족한 항목은 붉은색, 충족한 항목은 기존 본문의 짙은 갈색을 사용한다. 수량은 기존 ResourceManager.ResourcesChanged와 선택 변경 경로에서 갱신하며 비용 지불·건설 가능 여부는 기존 domain 규칙을 따른다.

`ConstructionPopup._costRows`의 private serializable `CostRowBinding`은 `_itemId`, `_root`, `_icon`, `_quantity`를 가진다. 아이콘은 프리팹 Image.sprite에 연결하고 `_costNormalColor`/`_costShortageColor`로 수량 색을 지정한다. Item ID는 프리팹에만 저장하며 코드에 골드·목재·석재 ID를 하드코딩하지 않는다. 바인딩은 한 번 검사하고 잘못된 항목을 진단한다. 선택 비용 중 하나라도 유효한 아이콘 행이 없으면 모든 아이콘 행을 숨기고 전체 비용을 기존 텍스트 목록으로 표시해 누락·겹침을 피한다.

`_costScrollRect`는 재료 목록의 세로 스크롤을 담당한다. 각 재료는 개별 배경을 가진 카드로 표시하고 viewport의 RectMask2D가 목록을 잘라낸다. 가로 스크롤은 끄고 세로 이동은 Clamped로 제한하며 세로 스크롤바를 연결한다. 건물 선택·재바인딩·닫기·비활성화 시 이동 속도를 멈추고 맨 위로 초기화한다. 보유량 갱신에서는 표시 중인 행을 껐다 켜지 않아 스크롤 위치를 유지한다.

선택 전·닫기·비활성화·공사 진행 중과 텍스트 fallback에서는 ScrollView와 행을 숨기거나 초기화한다. 제목과 상세 텍스트는 ScrollView의 형제이므로 목록을 숨겨도 안내를 표시할 수 있다. 진행률·작업 인원·취소 시 환불 안내는 기존 상세 텍스트를 유지한다. 선택만으로 자원을 차감하지 않으며 확정·취소 API와 구독 수명은 유지한다.

골드는 `ui-gold-coin_0`, 목재는 `ui-item-wood_0`, 석재는 `ui-item-stone_0`의 실제 Sprite subasset을 사용한다. 원본 이미지와 importer 설정은 유지한다. 기존270×342 오른쪽 영역 안에 제목·안내와 재료 스크롤 영역을 나눈다. 실제 Unity import·화면·클릭 검증은 별도다.

## WarmPixel 표시용 프리팹

2026-10-18 `Assets/Prefab/UI/WarmPixelConstructionPopup.prefab`을 별도 표시용으로 추가했다. 기본 크기는 1440×920이며 Canvas 아래에 배치한다. 왼쪽 `BuildingGallery`는 창고·식당·숙소·초소·농경지·주택 1단계를 2열×3행으로 보여준다. `GalleryScrollView/Viewport/Content`에는 ScrollRect·RectMask2D·GridLayoutGroup을 사용하며 셀 크기는 360×214, 간격은 가로18·세로16이다. 현재 Content 높이는 여섯 카드에 맞춘 674이며 항목 추가 시 높이도 조정해야 한다. 카테고리와 필터는 추가하지 않았다.

헤더는 `WarmPixelMerchantPopup`·`WarmPixelTownHallPopup`의 규격을 따른다. 높이82, 아이콘40×40, 제목30pt, 제목 옆 세로 구분선2×38, 문맥 라벨 `건물 선택`28pt, 닫기32×32(상단24·우측34 여백), 하단 구분선 높이3·좌우12 여백이다. 본문은 상단108·하단30·좌우36 여백을 두고 갤러리와 상세 사이에 세로 구분선을 배치한다.

오른쪽 `WarehouseDetail`에는 선택된 창고의 큰 이미지·이름·설명, 필요/보유 수량을 나란히 표시하는 `ConstructionCost`, 하단 `StartConstructionButton`을 둔다. 필요량100골드·20목재·10석재는 현재 BuildingCost CSV의 창고 비용이며, 보유량250·80·40은 샘플로 표시하고 예시임을 명시한다. 창고 선택 상태와 상세는 고정 표시다.

건물 이미지는 `BuildingDataContext`의 실제 Sprite subasset을 재사용하고, 주택은 `house-tiers`의 1단계 Sprite를 사용한다. 창·버튼·닫기는 기존 WarmPixel 부품, 카드 배경은 WarmPixelTownHall의 선택/초상 패널을 사용한다. 이미지와 importer는 변경하지 않는다. 문구·숫자·아이콘은 각각 별도 uGUI 요소로 편집할 수 있다.

카드의 `BuildingImage`는 332×160 영역 안에서 비율을 유지하며 가로 중앙·하단 기준으로 표시한다. anchorMin/Max는 (0.5,1), pivot은 (0.5,0), anchoredPosition은 (0,-170)으로 두어 여섯 건물의 바닥선과 이름 위 여백을 맞춘다. 오른쪽 `SelectedBuildingImage`도 pivot (0.5,0)·anchoredPosition (0,-300)을 사용하며 기존 가로 stretch와 높이300을 유지한다. uGUI Image의 비율 유지 정렬은 RectTransform pivot을 따르므로 상단 왼쪽 pivot을 사용하지 않는다. 카드 자체의 위치·크기는 GridLayoutGroup이 소유하며, Unity 저장 파일의 해당 driven 값이 0인 것과 실제 레이아웃 크기는 구분한다.

기존 `ConstructionPopup.prefab`, 씬 배치, UIManager 등록과 건설 코드는 교체하지 않는다. 새 프리팹에는 `ConstructionPopup`·`ConstructionChoiceRow` 컴포넌트나 버튼 callback이 없으며, 카드 선택·닫기·건설·실제 재고 조회는 연결하지 않았다. 최종 한글 표시·화면 크기별 배치와 게임 화면은 사람 확인 항목이며 자동 Play·화면 검증은 수행하지 않는다.

표시용 프리팹 루트는 `Background`, `Header`, `Body`를 구분한다. `Body/WarehouseDetail`은 건물 그림·이름·설명을 담은 `Overview`, 기존 `ConstructionCost`, 건설 버튼을 담은 `Footer`로 나눈다. 갤러리·스크롤·비용 행의 기존 내부 구조와 화면 배치는 유지한다. 공통 계층 원칙은 [UI](../UI.md#ui-프리팹-계층-원칙)를 따른다.

`Header/ConstructionIcon`은 `Assets/Art/Generated/UI/WarmPixel/construction-header-icon.png`의 전용 망치 도트 Sprite를 사용한다. 기존 헤더40×40 배치와 Image 참조 ID는 유지하며 원래 world 망치 그림은 변경하지 않는다. 공통 규격은 [헤더 전용 도트 아이콘](../UI.md#헤더-전용-도트-아이콘)을 따른다.
