# Hover Lifecycle and Views

## 목적과 책임 경계

hover source 수명과 농장 게이지·NPC 메시지 표시를 소유한다. 문구 선택은 NPC Messages, 번역은 Localization이 담당한다.

## 현재 실행 흐름

UIManager → HoverBase.TryShow/refresh → FarmGaugeHover 또는 NPCMessageHover.ApplyInfo. source가 사라지면 닫고 비활성화 때 참조와 표시 연출을 정리한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/HoverInfo.cs` | anchor/progress 및 선택적인 LocalizeKey·추적 Transform 정보 값 |
| `Assets/Scripts/Enum/HoverType.cs` | hover registry category key |
| `Assets/Scripts/Interface/IHoverInfoSource.cs` | domain object가 hover 정보와 Unity owner를 제공하는 계약 |
| `Assets/Scripts/UI/FarmGaugeHover.cs` | farm progress를 fill과 screen anchor로 표현하는 hover view |
| `Assets/Scripts/UI/NPCMessageHover.cs` | LocalizeText 문구 표시와 머리 위 anchor의 LateUpdate 화면 위치 추적 |
| `Assets/Scripts/UI/HoverBase.cs` | source 소유권, refresh, show/hide 공통 hover lifecycle |

## 변경 유형별 최소 확인 범위

- 공통 hover: HoverBase.cs, HoverType.cs, IHoverInfoSource.cs, HoverInfo.cs.
- 농장 gauge: FarmGaugeHover.cs, Assets/Prefab/UI/FarmGauge.prefab, [Farming](../../Economy/Farming/README.md).
- 말풍선: NPCMessageHover.cs, [NPC Messages](../../NPC/Presentation/Messages.md), [Localization](../Localization.md).

## 불변 규칙

- hover owner가 파괴되면 view는 다음 refresh에서 스스로 닫힌다.
- view는 전달된 상태만 표시하며 문구나 게임 규칙을 선택하지 않는다.

## Unity 배선과 검증

FarmGaugeHover에는 fill image와 camera를 연결한다. NPC 메시지의 상세 배선과 기존 검증 범위는 아래 문구 표시 연계를 따른다.

## 문구 표시 연계

꼬리 없는 독립 말풍선 패널은 `Assets/Art/Generated/UI/ui-speech-panel-white-9slice.png`다.
128×128 RGBA, 불투명 흰색 내부·검은색 3px 테두리·둥근 모서리·투명 외곽을 사용한다.
Single/FullRect, PPU 100, Bilinear, 비압축, mipmap off, 사방 Border 32px를 `.meta`에 저장했다.
UI `Image.Type = Sliced`, `Fill Center = true`로 사용한다. 일반 Simple 이미지 확대는 9-slicing이 아니다.
원본 pixel 기준 최소 가로/세로 64 이상에서 고정 corner 영역을 온전히 유지한다.
캐릭터 연결부·꼬리·텍스트·씬/prefab 배선은 포함하지 않는다. 가로/세로 9-slice 수치 검사는 통과했으며
Unity import/Canvas 실표시는 미검증이다. 원본 SVG·늘림 미리보기·설정 증거는 `.harness-runs/speech-panel-20260920/`에 있다.

`LocalizeText`의 CSV key 기반 Text/TMP 표시와 persistent `LocalizeManager` 접근은
[Localization](../Localization.md)이 주 소유한다. UIManager의 popup/hover registry에는 등록하지 않는다.
첫 적용은 독립 LocalizeTest 씬이며 기존 UI 문구는 이번 작업에서 이관하지 않았다.

주민 생각·지정 대사는 [NPC Messages](../../NPC/Presentation/Messages.md)의 source가 `HoverType.NPCMessage`로 제공한다.
`NPCMessageHover`는 문구를 선택하지 않고 `LocalizeText.SetKey`, 매 프레임 anchor 위치 추적과 표시 연출을 담당한다.
기존 HoverInfo 생성 호출은 그대로 유효하며, 선택적인 MessageKey/TrackingAnchor를 메시지 view가 소비한다.
FarmerTest의 `Canvas/HoveringUI/NPCMessageHover/MessageText`에 말풍선 한 개를 추가하고 UIManager에 등록했다.
기존 농장 게이지·PopupUI 위치와 순서는 유지한다. Router는 기존 serialized layer mask에 Friendly를 runtime filter로 합친다.
NPCGirl source/anchor/Catalog와 별도 루트 LocalizeManager 배선은 [NPC Messages](../../NPC/Presentation/Messages.md)를 따른다.
사용자가 기존 말풍선 표시를 정상 확인했다. 한글·9-slice·화면 경계 전체 시나리오의 독립 검증과 신규 연출의 시각 확인은 별도다.

### NPC 말풍선 표시 연출

- 기존 Legacy Text 말풍선은 DOTween `DOText`로 빈 문자열부터 대사를 표시한다. duration은 문자열 길이 ×
  기본 글자당 0.025초이며 `Ease.Linear`, `SetUpdate(true)`를 사용한다. Inspector의 Typing / Character Interval로
  조절하며 0이면 즉시 표시한다. 현재 CSV의 완성형 한글은 글자 단위로 나타난다.
  LocalizeText의 첫 Start 이후 LateUpdate에서 문구를 읽고 tween을 시작한다. Rich Text 설정은 Text의 값을 따른다.
  새 key 또는 다른 anchor에서 재시작하며 같은 source의 refresh에는 이어서 표시한다. 퇴장 시 Pause하고
  완전히 닫히기 전 같은 대상 재진입 시 Play한다. 비활성화·대사 교체 시 Kill하고 원문을 복구한다.
  현재 배선처럼 LocalizeText와 Text가 같은 오브젝트에 있어야 한다. TMP는 기존 즉시 표시를 유지한다.
  별도 문자열 캐시·투명 뒷부분은 사용하지 않으므로 정렬·줄바꿈은 현재까지 표시된 문자열을 기준으로 한다.
- `NPCMessageHover`의 DOTween 하나가 0~1 표시 진행도를 움직인다. 기본 진입은 0.2초/OutCubic,
  퇴장은 0.15초/InCubic이며 `SetUpdate(true)`로 게임 시간 배율과 독립적이다.
- 진행도에 따라 원래 크기의 65%에서 100%로 커지며 18 Canvas UI 단위 아래에서 anchor로 올라온다.
  CanvasGroup alpha도 함께 변한다. 퇴장은 반대 방향이다. Motion의 시간·거리·시작 크기는 Inspector에서 조정한다.
- LateUpdate가 현재 anchor·카메라 위치에 연출 오프셋과 크기를 합친 뒤 화면 경계를 보정한다.
  위치 tween이 anchor 추적을 덮어쓰지 않는다. 기존 scene 참조와 Hierarchy를 그대로 사용한다.
- `HoverBase.HideCurrent`는 source를 즉시 해제하고 `BeginHide`를 호출한다. 기본 hover는 즉시 닫히며,
  NPC 말풍선만 퇴장 완료 뒤 `CompleteHide`로 비활성화한다. 닫히는 동안 문구를 재조회하지 않는다.
- UIManager는 닫히는 view 참조를 유지한다. 같은 말풍선 재진입은 남은 진행도에서 열리고,
  다른 category 표시·HideAll·초기 registry 정리는 `HideImmediately`로 이전 view를 즉시 닫는다.
  다른 NPC anchor로 바뀌면 새 진입 연출을 시작한다. 같은 source 재확인은 tween을 재시작하지 않는다.
- NPCMessageHover 비활성화 시 자신이 소유한 tween을 완료 콜백 없이 Kill하고 진행도·anchor·문구 캐시를 정리한다.
  anchor가 파괴되거나 비활성화되면 즉시 닫는다. HoverBase도 비활성화 시 source를 해제한다.

## 알려진 제약과 TBD

실제 화면·입력·Play 확인은 사람의 QA 항목이며 이 문서 재배치에서는 실행하지 않았다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
