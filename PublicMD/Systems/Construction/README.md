# Construction

지정 부지의 건설 정의, 공사 상태와 예약, 완공 시설 조립, 플레이어 조작을 연결한다. 승인 규칙은 [건설 컨텍스트](../../Plans/Building_Construction_Context.md), 실행 범위는 [승인 계획](../../Plans/Building_Construction_Implementation_Plan.md)을 따른다. 적용 및 검증 상태는 PROGRESS가 소유한다.

| 변경 | 주 소유 leaf |
|---|---|
| CSV·불변 건물/작업 정의·에셋 연결 | [Definitions](Definitions.md) |
| 부지 상태·비용/환불·예약·작업 요청 | [Plots](Plots.md) |
| 완공 프리팹 생성·주입·등록·실패 정리 | [Facilities](Facilities.md) |
| 부지 클릭·건설 목록·진행/취소 표시 | [UI](UI.md) |
| 자재·골조·가림막 단계 표시와 애니메이션 | [Presentation](Presentation.md) |

전체 흐름: DataManager 정의 조회 → 클릭한 BuildingPlot의 지불/공사 생성 → Builder selector의 FIFO 예약 → BuildAction 이동/작업 → Plot의 완공 요청 → Factory 조립/등록. 공용 재고는 [Inventory and Items](../Inventory_and_Items.md), 건축가 실행은 [Builder](../Builder.md), 다중 시설 조회와 provider 공통 계약은 [Interaction and Destinations](../Interaction_and_Destinations.md)가 주 소유한다.

한 production C#은 한 leaf만 주 소유한다. BuildAction/BuildActionCost는 Builder, IInteractionReservation/InteractionRequest는 공통 interaction 문서에 등록한다. 이 인덱스는 모든 코드를 먼저 읽으라는 목록이 아니다.
