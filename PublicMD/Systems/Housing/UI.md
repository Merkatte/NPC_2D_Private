# Housing UI

## 책임과 최소 확인 범위

| 주 소유 스크립트 | 책임 |
|---|---|
| `Assets/Scripts/Actor/HousePopupSource.cs` | 같은 객체의 주택 collider 클릭을 IClickPopupSource에 연결 |
| `Assets/Scripts/UI/HousePopup.cs` | 선택 주택의 조회·구독·입력 요청·동적 목록 |
| `Assets/Scripts/UI/WarmPixelHousePopup.cs` | 본창 너비와 비교 패널의 열기·접기 전환 |
| `Assets/Scripts/UI/HouseEffectRowView.cs` | 현재 효과 또는 다음 단계 비교 행 표시 |
| `Assets/Scripts/UI/HouseResidentView.cs` | 주민 이름과 초상 표시 |
| `Assets/Scripts/UI/HouseInfoRow.cs` | 보존된 옛 주택창의 목록 행 |

변경 시 관련 UI 파일과 WarmPixelHousePopup.prefab, House/HousingDataContext 읽기 API, BuildingPlot 업그레이드 API, PopBase/IClickPopupSource를 확인한다. 공유 ResourceCostRowView·PopupCanvasFit은 [UI](../UI.md), 공사 transaction은 [Construction Plots](../Construction/Plots.md)가 소유한다. Factory가 HousePopupSource에 House와 BuildingPlot을 주입하며 House는 UI나 공사 객체를 참조하지 않는다.

## 표시와 수명

- HousePopup이 단계·정원·입주·현재 효과·다음 단계 비교·비용·공사 상태를 실제 데이터에서 읽는다. 주민은 실제 Worker 이름과 공용 초상으로 표시하며 빈 목록 안내를 지원한다.
- 효과 옵션은 그대로 표시한다. 집 피로 회복 보너스와 생활 기본값을 합한 `집 체류 시 피로 회복`은 별도 현재 정보 행이다. 비교는 효과별 추가·유지·변경·제거를 표시하며 단계 상속을 추정하지 않는다.
- 효과·비교·주민·재료 행은 비활성 template에서 생성해 재사용한다. 각 목록은 마스크와 ScrollRect를 사용한다. 재료는 정의의 모든 itemId를 읽으며 미등록 아이콘도 이름·필요·보유량은 표시한다. 환불은 반환/보유량으로 표시하고 부족 색을 적용하지 않는다.
- House.Changed, Plot.StateChanged, ResourcesChanged를 구독한다. 재바인딩·닫기·disable에서 해제하며 대상 비활성·파괴는 RequestClose로 닫는다. 재고와 목록을 매 프레임 조회하지 않는다.
- 비용이 부족해도 비교를 열 수 있고 실제 시작 버튼만 제한한다. 시작·취소·완공 재시도는 Plot의 기존 API만 호출한다. 공사 중에는 진행률·작업 인원·예상 환불·취소·필요 시 재시도를 보여주며 완료/취소 시 비교 패널을 접는다. 실패 이유와 현재 창은 유지한다.
- 최고 단계에서는 다음 단계와 업그레이드 버튼을 숨긴다. 재바인딩·재열기에는 접힌 상태로 초기화한다.

## WarmPixel 배치와 연결

DefenseTest의 PopupUI에는 비활성 WarmPixelHousePopup instance 하나를 PopupType.House=6으로 등록한다. Defense 전용 HousingDataContext·BuildingDataContext를 scene override로 연결한다. 옛 HousePopup prefab은 보존하며 다른 씬의 옛 배치 호환 모드는 제공하지 않는다.

기본 외곽은 1440×920이다. 현재 정보가 전체 폭을 사용하다가 업그레이드를 누르면 본창 폭이 940으로 줄고 오른쪽 500 영역에 비교 패널이 나타난다. 텍스트·아이콘 배율은 바꾸지 않고 지정된 RectTransform의 위치·영역만 보간한다. WarmPixelHousePopup.SetDrawerOpen·ResetDrawer는 배치만 소유하며 버튼 listener·닫기·데이터 조회는 HousePopup이 소유한다. 전환은 기본 0.3초 unscaled이며 연속 입력은 현재 진행도에서 반전한다. 닫힌 패널과 전환 중인 패널은 입력을 받지 않는다. 작은 화면에서 전체 창의 비율 축소는 PopupCanvasFit이 담당한다.

조립 루트에는 창 Image를 두지 않는다. 본창과 업그레이드 패널 각각의 Background가 배경을 소유하며 Header/Body/Footer로 구분한다. 본창 Body는 HouseOverview/CurrentDetails/Residents, 비교 Body는 NextTierOverview/EffectsComparison/Costs로 묶는다. 비활성 Templates는 동적 목록의 원본만 보관한다. [공통 계층 원칙](../UI.md#ui-프리팹-계층-원칙)을 따른다.

헤더는 Assets/Art/Generated/UI/WarmPixel/house-header-icon.png의 64×64 도트 Sprite를 40×40에 표시한다. 실제 건물 그림은 BuildingDataContext의 단계별 Sprite, 주민·재료는 기존 에셋을 재사용한다. 이미지/importer 변경은 없다.

## 기존 조립 도구와 검증

Assets/Editor/HousingSetup.cs는 옛 BuildingTest/HousePopup 배선을 위한 도구다. 신규 DefenseTest UI 전환에는 실행하지 않는다. 기존 House root의 collider·입구·Visual과 공사 중 source 유지 규칙은 그대로다. 정적 참조·컴파일·독립 리뷰 결과는 작업 기록과 PROGRESS를 따른다. 실제 화면·스크롤·반복 열기·공사 흐름은 사람 QA이며 자동 Play/화면 검증은 실행하지 않는다.
