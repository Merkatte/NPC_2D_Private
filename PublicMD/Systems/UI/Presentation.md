# UI Presentation

## 목적과 책임 경계

공통 팝업 스타일, prefab 계층, WarmPixel 이미지와 헤더 아이콘 규격을 소유한다. UI lifecycle은 [Framework](Framework/README.md)가 담당한다.

## 주 소유 스크립트

없음. 이미지·prefab 표현 규칙을 소유하며 표시 컴포넌트의 소유권은 각 leaf를 따른다.

## 변경 유형별 최소 확인 범위

- 공통 프레임·버튼: Assets/Art/Generated/UI/WarmPixel과 대상 popup prefab.
- 계층·헤더: 아래 규격과 해당 domain UI 문서.
- Canvas 크기 맞춤: [Popup](Framework/Popup.md).

## 팝업 공통 표현

기존 목재 계열 팝업은 상단(MerchantPopup)의 목재 외곽·제목 명패·종이 내부 패널·녹색 확정/적갈색 취소 버튼을 기준으로 재사용한다. 기능별 내용 구조와 루트 크기는 유지한다. 닫기 X는 실제 배경 우측 상단 안쪽에 두며 anchor/pivot은 (1,1), 오른쪽·위 여백은 버튼 한 변의 1/4이다. 현재 Merchant 100/25, TownHall 72/18, Construction·Warehouse 56/14 UI 단위다. Merchant 배경은 루트보다 위로 확장되어 있으므로 실제 배경 상단을 기준으로 위치를 계산한다. WarmPixel로 갱신한 씨앗 선택 창은 종이 프레임 안쪽 여백에 맞춰 닫기 44×44, 오른쪽·위 여백 24를 사용한다.

건설·창고의 제목판과 BodyBackground는 기존 UI 이미지를 사용하는 별도 자식 Image이며 raycast를 받지 않는다. 기존 닫기 Button과 클릭 연결을 유지하고 이전 닫기 라벨을 제목판으로 옮겼다. 본문과 목록은 목재 프레임 안쪽에 배치한다. 현재 건설 5개 행과 창고 9개 항목이 들어가는 범위를 기준으로 하며 항목 수 증가 시 목록 높이·스크롤 정책을 다시 확인해야 한다.

씬의 팝업 크기 override를 유지한다. 프리팹의 저장값·참조 검토는 Unity에서의 실제 표시 및 클릭 검증과 구분하며, 화면 QA는 사람의 확인 항목이다.

## UI 프리팹 계층 원칙

- 팝업 조립 루트와 실제 표시 패널을 구분한다. 여러 패널을 조합하는 루트에 같은 창 배경을 중복으로 깔지 않는다.
- 실제 패널은 역할에 따라 `Background`, `Header`, `Body`, `Footer`로 구분한다. 배경 Image는 `Background`, 제목·탭·닫기·헤더 구분선은 `Header`, 주요 내용은 `Body`, 하단 실행 버튼은 `Footer`에 둔다. 사용하지 않는 영역을 이름만 맞추기 위해 만들지는 않는다.
- `Body` 아래도 건물 개요·현재 효과·거주민·효과 비교·비용처럼 내용별로 묶는다. 비용/효과의 각 행은 해당 행 아래에 아이콘·이름·수량·구분선을 둔다. 서로 다른 영역의 모든 오브젝트를 한 계층에 나열하지 않는다.
- 작은 재사용 슬롯·아이콘·행은 그 자체가 하나의 의미 있는 묶음이므로 불필요한 Header/Body/Footer 래퍼를 강제하지 않는다. 기존 ScrollRect·viewport·grid·layout과 nested prefab은 해당 영역 내부 구조를 유지한다.
- 계층 정리만 하는 경우 기존 표시 위치·크기·폰트·sorting·callback·serialized 참조와 애니메이션 좌표를 보존한다. 부모 변경으로 좌표계가 달라지지 않게 하고, 변경한 실제 hierarchy 경로는 주 소유 문서에도 반영한다.

## WarmPixel UI 부품

`Assets/Art/Generated/UI/WarmPixel`은 2026-10-16 사용자가 선택한 따뜻한 픽셀 스타일의 공통 UI 이미지 9개를 보관한다. 창·판매 내역 패널·주요/보조/사각 버튼·수량 입력칸·닫기 아이콘·세로 스크롤바 배경과 손잡이로 구성된다. 문구·숫자·상품 아이콘은 이미지에 합치지 않았다.

Unity에서 Sprite Single, PPU 100, Point, Clamp, mipmap off, sRGB, 비압축으로 가져왔다. pivot은 중앙이다. Border는 Left/Bottom/Right/Top 순서로 창 12/12/12/12, 명세 패널·사각 버튼·입력칸 8/8/8/8, 주요·보조 버튼 10/8/10/8, 스크롤 배경·손잡이 2/4/2/4이며 닫기 아이콘은 0이다. 닫기는 Simple, 나머지는 사용할 Image에서 Sliced로 지정한다. 기존 importer 도구가 Mesh Type 변경을 지원하지 않아 현재 저장값은 Tight이며 권장 Full Rect 적용과 실제 Canvas 늘림 표시는 미검증이다.

`Assets/Prefab/UI/WarmPixelMerchantPopup.prefab`은 이 부품을 사용하는 별도 표시용 거래창이다. 상품 행은 `WarmPixelMerchantItemRow.prefab`, 판매 명세 행은 `WarmPixelMerchantSaleLine.prefab`의 nested instance로 배치한다. 왼쪽 목록은 기본 uGUI ScrollRect·RectMask2D·VerticalLayoutGroup·ContentSizeFitter와 세로 Scrollbar를 사용하며 오른쪽 명세와 판매 버튼은 목록 밖에 고정한다. 자세한 구조와 샘플값은 [Merchant Caravan](../Economy/Merchant_Caravan.md)의 WarmPixel 프리팹 절을 따른다.

2026-10-20 DefenseTest의 신규 WarmPixel 팝업에는 실제 controller와 UIManager 등록을 연결했다. 각 기능의 현재 계약은 주 소유 문서를 따른다. 옛 prefab 파일과 다른 씬은 보존한다. 원본과 부품 미리보기·import 권장값은 `output/UI/WarmPixel`에 있다.

## 헤더 전용 도트 아이콘

신규 WarmPixel 팝업의 제목 옆에는 world 건물·도구 그림을 축소한 이미지 대신 UI 전용 64×64 RGBA 도트 아이콘을 사용한다. 공통 저장 위치는 `Assets/Art/Generated/UI/WarmPixel`이며 Single/FullRect·PPU100·Point·Clamp·mipmap off·sRGB·비압축·중앙 pivot·border0으로 가져온다. 각 헤더 Image는 기존40×40 크기·Simple·preserveAspect·흰색·raycast off 설정을 유지한다.

| UI | PNG | 기존 연결 위치 |
|---|---|---|
| 주택 | `house-header-icon.png` | `MainPanel/Header/HouseIcon` |
| 건설 | `construction-header-icon.png` | `Header/ConstructionIcon` |
| 시청 | `town-hall-header-icon.png` | `Header/CityIcon` |
| 상단 | `merchant-header-icon.png` | `Header/GoldIcon` |

상단의 `GoldIcon`은 기존 오브젝트 이름이며 표시 그림은 거래용 돈주머니·동전이다. 교체는 이 헤더 Image의 Sprite 참조에 한정한다. 원래 건물·도구·골드 코인 파일과 재료 행·직군 도구·골드 HUD의 참조는 유지한다. 생성 PNG와 정확한 프롬프트는 `output/UI/HeaderIcons20261020`에 보관하며 주택 아이콘은 기존 제작본을 재사용한다.

## 관련 문서와 갱신 조건

[UI](README.md). 공통 스타일·계층·import 설정이 바뀌면 갱신한다. 기존 미검증 사항은 각 절에 보존하며 이 문서 이동으로 화면 검증을 대신하지 않는다.
