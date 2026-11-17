# Dialogue Presentation and Input

## 목적과 책임

공통 SequenceRunner가 선택한 대사를 말풍선 또는 하단창으로 표시한다. 화자의 선택·대사 분기·게임 규칙은 view가 소유하지 않는다.

## 주 소유 코드와 최소 확인 범위

| 파일 | 책임 |
|---|---|
| Assets/Scripts/UI/DialogueView.cs | 대화 session 표시, 화자/본문, 말풍선/하단창 전환, 선택 입력 전달 |
| Assets/Scripts/UI/GameplayInputGate.cs | 이유별 입력 점유와 이전 UI 입력 상태 복원, 해제 프레임 누출 차단 |
| Assets/Scripts/UI/MessageTyping.cs | 기존 hover와 대화가 공유하는 글자 출력 |
| Assets/Scripts/UI/MessageBubblePosition.cs | 기존 hover와 대화가 공유하는 머리 위 화면 위치 보정 |

표시/입력 변경은 이 코드와 [Sequences](../GameFlow/Sequences.md), 기존 hover는 [Hover](Framework/Hover.md)를 읽는다. CSV와 key 생성은 [Localization](Localization.md)를 따른다.

## 흐름과 불변 규칙

대화 host는 상시 활성이다. 진행기가 session을 열고 단계별 view를 선택한다. Bubble은 actor anchor, BottomPanel은 화면 하단을 사용한다. 글자 출력은 unscaled이며 입력하면 완성, 출력 후에는 다음 줄로 진행한다. 선택지는 자동 진행하지 않는다. 카메라 이동 중에는 대사 표시만 숨긴다.

GameplayInputGate는 UIManager/일반 CanvasGroup과 연결하여 기존 hover·팝업을 정리하고 일반 입력을 막는다. PointerClickRouter, PointerHoverRouter, UIManager와 수동 camera 입력이 gate를 소비한다. 대화 UI는 차단 그룹 밖에 둔다. 마지막 점유 반환 프레임에도 차단을 유지한다.

## 배선과 검증

기존 white 9-slice와 uGUI Text 폰트를 재사용한다. 하단창에는 이름과 본문을 표시하며 초상화는 없다. 접수원 source sprite 변경과 gameplay 데이터 mutation을 하지 않는다. 실제 연결/검사/독립 리뷰는 PROGRESS에 기록한다. 한글 glyph·긴 대사·선택·화면 경계는 사람 확인 항목이다. 변경 시 관련 UI/Sequence 문서 링크와 주 소유 파일을 갱신한다.
