# Defense Presentation

## 목적과 현재 흐름

Defense 전용 actor prefab에 붙는 표현 adapter다. 전투 선택과 피해 시간은 [Battlefield and Response](Battlefield_and_Response.md)가 소유하고 표현은 해당 요청을 자세/도구/말풍선으로 옮긴다.

DefenseCombatPresentation은 Init의 역할 sprite, 공격 시작/종료의 weapon 회전/scale, Downed body 회전을 적용한다. 공격 시작은 weapon을 표시하고 StopAttack은 자세만 복원하므로 공격이 끝나도 이동·경비 중 검/활을 계속 든다. ShowDowned가 weapon을 숨기고 실제 disable/pool reset은 기존 NPCComponent.ResetRuntimeState가 숨긴다. ResetPose는 재초기화 때 기본 자세로 돌린 뒤 SetRole이 새 역할 무기를 표시한다. runtime hit는 animation event에 의존하지 않는다. DefenseFleeSpeech는 Actor.FleeStarted에서 설정 문구를 자동으로 열고 scaled 시간 만료/Downed/disable에 닫는다. hover 입력 없이 보이며 메시지를 selector·action·NPCComponent에 넣지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/System/Actor/DefenseCombatPresentation.cs` | 역할 무기 sprite와 공격/Downed 자세 표현 |
| `Assets/Scripts/Actor/DefenseFleeSpeech.cs` | 자동 도주 메시지의 구독·표시·수명·방향 유지 |

## 변경 유형별 최소 확인 범위

- 공격/Downed: 두 actor prefab, DefenseCombatPresentation, DefenseAttackAction, DefenseActor.
- 말풍선: DefenseFleeSpeech, DefenseCombatSettings의 문구/시간, world canvas/panel/Text 연결.
- 이동·도구·Animator 충돌: [NPC Presentation](../NPC_Presentation.md), Defense controller/clip override와 BodyPose/ToolAnchor 경로.

## Unity 배선과 불변 규칙

Defense prefab은 기존 Animator가 제어하는 Visual 위에 BodyPose를 두고 override clip 경로를 맞춘다. `_body`는 BodyPose, `_weapon`은 ToolAnchor, `_weaponRenderer`는 기존 tool renderer다. 같은 transform 회전을 Animator와 동시에 소유하지 않는다. NPCComponent 역할 도구 표에 Guard sword/Archer bow를 연결한다. 공격 action은 NPCComponent로 방향을 요청하고 무기 표시/공격 자세는 DefenseCombatPresentation에 맡긴다. RequestReplan/Stop/pooled Clear는 무기를 숨기지 않는다.

말풍선은 기존 흰색 9-slice/폰트를 사용하는 전용 world canvas panel이다. `_panel`은 비활성 표시 child이며 스크립트는 활성 actor에 둔다. 표시 중 world rotation을 고정하고 부모의 음수 X scale 부호를 상쇄해 facing을 바꾸어도 글자가 뒤집히지 않게 한다. 실제 가독성은 사람 확인 항목이다. UI는 gameplay state를 변경하지 않는다.

## 검증·제약·갱신

표현은 runtime 완료 조건을 결정하지 않는다. 컴파일과 serialized 연결 검사만으로 공격 자세·사거리·한글·화살·말풍선을 시각 통과로 보고하지 않는다. 이 항목은 사람 확인이며 자동 Play/스크린샷 검증은 제외한다. [NPC Messages](../NPC_Messages.md)의 hover 생각 표시는 기존대로 유지한다. 표현 API·transform ownership·말풍선 수명/배선이 변경되면 갱신한다.
