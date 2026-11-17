# 주민 직업 성장 정보창

## 목적과 책임 경계

선택한 주민의 현재 직업과 직업별 성장 상태를 읽기 전용으로 표시한다. 성장 계산·지급·상한 변경은 NPC 성장 runtime이 소유한다. 정보창은 게임 시간을 정지하거나 업무·생각 말풍선 상태를 바꾸지 않는다.

## 현재 실행 흐름

`PointerClickRouter → NPCProfessionPopupSource(IClickPopupSource) → UIManager → NPCProfessionPopup` 순서로 바인딩한다. source는 연결된 `WorkerNPC`의 현재 직업과 `ProfessionGrowth.TryGetProgress` 결과, `LifetimeVersion`만 전달한다. 성장 설정이 없는 주민이나 비활성·해제된 주민은 클릭을 거부한다.

팝업은 바인딩 당시 주민 수명 번호를 보관한다. 열린 동안 매 프레임 source 생존성과 수명 번호를 확인하고, 직업·레벨·상한·숙련도·필요량·상한 도달 여부가 바뀐 경우만 표시를 다시 쓴다. 동일 프리팹이 풀에서 재사용되어도 다른 주민으로 자동 전환하지 않고 `RequestClose`로 닫는다. 닫기 시작과 비활성화 때 source를 해제하며, 닫기 애니메이션 중 재바인딩은 공통 PopBase 수명을 따른다.

표시는 현재 직업, `Lv.현재 / 상한`, `숙련도 보유량 / 다음 레벨 필요량`, 진행 막대로 구성한다. 상한에서는 막대를 가득 채우고 `성장 상한 도달` 문구를 함께 표시한다. 상한 감소로 이미 상한보다 높은 주민도 실제 레벨·보유량을 그대로 보여준다. 직업 변경·업그레이드 명령은 제공하지 않는다.

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Actor/NPCProfessionPopupSource.cs` | 주민 클릭 가능 여부와 현재 성장 snapshot·수명 번호 조회 |
| `Assets/Scripts/UI/NPCProfessionPopup.cs` | 성장 표시, 변경 갱신과 선택 주민 수명에 맞춘 닫기 |

## 변경 유형별 최소 확인 범위

- 조회·풀 재사용: 위 source/view, `WorkerNPC.Profession`, `ProfessionGrowth`, `LifetimeVersion`과 [NPC Runtime](../../NPC/Runtime/Lifecycle_and_Stats.md).
- 성장 값 의미: NPC 성장 runtime의 `NPCProfessionGrowth`, `ProfessionProgress`.
- 문구: [Localization](../Localization.md)의 CSV 1301~1308, LocalizeKey와 LocalizeText.
- 열기·닫기: [Popup](../Framework/Popup.md), [Routing](../Framework/Routing.md).
- 클릭·대화 입력 차단: [Pointer Input](../Framework/Pointer_Input.md), [Dialogue](../Dialogue.md).

## Unity 배선 계약

`DefenseNPC`의 collider가 있는 동일 GameObject에 source를 배치하고 `_worker`를 명시적으로 연결한다. 기존 Friendly 레이어·말풍선 source를 보존하며 DefenseTest click mask가 Friendly를 포함해야 한다. 기존 `GameplayInputGate`와 UIManager가 대화 중 입력·일반 팝업을 차단한다.

`NPCProfessionPopup`은 `PopupType.NPCProfession = 7`로 UIManager에 등록하고 초기 비활성 상태로 배치한다. `_titleText`, `_professionText`, `_levelText`, `_experienceText`, `_capText`에는 각자 Text 또는 TMP 하나가 연결된 `LocalizeText`를 사용한다. `_capText`는 독립된 문구 GameObject여야 한다. `_progressFill`에는 가로 Filled Image, `_closeButton`에는 닫기 Button을 연결한다. 공통 `Background/Header/Body` 계층과 WarmPixel 부품은 [Presentation](../Presentation.md)을 따른다. 코드가 동적 문구 key와 숫자 인수를 설정한다.

## 검증과 제약

코드·문구 정합성 검사와 직렬화 참조 검토는 실제 화면·클릭·풀 재사용 플레이 검증과 구분한다. 자동 Play Mode와 화면 검증은 실행하지 않으며, 성장 갱신·상한 문구·주민 해제 시 닫기·기존 말풍선·대화 입력 차단의 실제 표시는 사람 QA 항목이다.

## 문서 갱신 조건

source/view 조회, 표시 필드, 수명 처리나 배선 계약이 바뀌면 이 문서를 갱신한다.
