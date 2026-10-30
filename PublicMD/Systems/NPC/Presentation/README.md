# NPC Presentation

NPC의 게임 상태를 이동·애니메이션·도구·운반물과 생각·대사로 전달한다. 업무 선택과 UI view의 화면 배치는 소유하지 않는다.

| 변경 대상 | leaf |
|---|---|
| 이동 adapter, 방향·animation·도구·봇짐 표현과 reset | [Adapter](Adapter.md) |
| 생각 선택, 지정 대사 토큰, 메시지 수명과 HoverInfo 제공 | [Messages](Messages.md) |

action/runtime이 adapter에 표현을 요청하고, 메시지 source는 현재 행동·욕구를 읽어 문구 key를 제공한다. 최종 말풍선 위치·등장·타이핑은 [UI Hover](../../UI/Framework/Hover.md), 번역 조회는 [Localization](../../UI/Localization.md)이 소유한다.

표현 요청과 상태 소비 경계 또는 하위 기능이 바뀌면 이 인덱스를 갱신한다.
