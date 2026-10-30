# UI Framework

입력 의도를 domain source와 UI 표시 수명 사이에서 전달하는 공통 기반이다. 구체 게임 규칙과 거래 전용 드래그 구현은 소유하지 않는다.

| 변경 대상 | leaf |
|---|---|
| popup·hover registry, 표시 전환, service 계약 | [Routing](Routing.md) |
| world hover·click 감지, UI 가림과 source 조회 | [Pointer Input](Pointer_Input.md) |
| hover 수명, 농장 게이지·NPC 말풍선 표시 | [Hover](Hover.md) |
| popup 바인딩·열기·닫기, Canvas 맞춤, 공용 비용 행 | [Popup](Popup.md) |

Pointer Input → Routing → Popup/Hover로 표시를 전달한다. source는 표시할 정보와 현재 클릭 가능 여부를 제공하고 view는 source의 수명에 맞춰 정리한다. concrete view의 게임 요청은 각 domain이 처리한다.

[공통 시각 규칙](../Presentation.md)과 [구체 표시](../Views/README.md)는 필요한 경우에만 추가로 확인한다. 공통 계약이나 하위 역할이 바뀌면 이 인덱스를 갱신한다.
