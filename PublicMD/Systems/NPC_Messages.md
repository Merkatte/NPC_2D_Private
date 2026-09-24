# NPC 생각과 지정 대사

## 목적과 책임

주민의 현재 행동·욕구를 읽어 짧은 생각의 `LocalizeKey`를 제공한다. 외부 지정 대사가 있으면
생각보다 우선한다. 메시지는 gameplay 상태, action queue, 행동 selector와 gameplay 난수를 변경하지 않는다.
코드·문구와 NPCGirl/FarmerTest의 YAML 배선을 적용했고 사용자가 말풍선 표시를 정상 확인했다.
전체 Play Mode 회귀와 새 DOTween 연출의 시각 검증은 별도다.

## 현재 코드 흐름

```text
PointerHoverRouter -> IHoverInfoSource.TryGetHoverInfo
  -> NPCMessageSource.TryGetMessage
    -> WorkerNPC.TryGetCurrentState(out IStatView, out ActionType?)
    -> NPCMessageState.Synchronize(stat identity)
    -> NPCThoughtSelector.CollectCandidates / TrySelect
    -> NPCMessageState: 지정 대사 우선, 유효한 생각 캐시와 실시간 만료
  -> HoverInfo(MessageKey, TrackingAnchor)
  -> NPCMessageHover -> LocalizeText
```

`WorkerNPC` 없는 대사 전용 NPC도 지원한다. 이때 랜덤 생각은 없고 지정 대사만 조회한다.
Worker가 있으면 초기화된 활성 주민만 메시지를 제공한다. worker의 공개 조회는 stat view와
현재 `ActionType?`만 반환하며 action 객체와 queue는 노출하지 않는다.

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Actor/NPCMessageSource.cs` | 주민별 메시지 진입점, dependency·stat identity·disable 처리, HoverInfo 제공 |
| `Assets/Scripts/System/Actor/NPCMessageState.cs` | 지정 대사 토큰, 직전 생각, 유지 시간과 캐시의 개체별 상태 |
| `Assets/Scripts/System/Actor/NPCThoughtSelector.cs` | 전달받은 행동·욕구·정의의 후보 구성과 난수 선택 |
| `Assets/Data/ScriptableObject/Script/NPCThoughtCatalog.cs` | 행동·욕구 문구 목록, threshold, 기본 문구와 유지 시간 정의 |

`NPCMessageHover`는 [UI](UI.md), 상태 조회는 [NPC Runtime](NPC_Runtime.md),
CSV/enum과 `LocalizeText`는 [Localization](Localization.md)이 주 소유한다.

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 확인 |
|---|---|
| 후보 조건·난수 | Catalog, NPCThoughtSelector, IStatView, ActionType, NPCMessageTests |
| 지정 대사·시간·풀 재사용 | NPCMessageState, NPCMessageSource, WorkerNPC 조회·Init·OnDisable, NPCMessageTests |
| 문구 | LocalizeData.csv, LocalizeKey 생성기, Catalog |
| 표시·배선 | NPCMessageSource serialized field, UI의 NPCMessageHover/PointerHoverRouter, NPCGirl/FarmerTest |

## 선택·수명 규칙

- 행동은 Farming, Harvest, Deposit, Guard, Attack, Eat, Drink, Sleep, Idle에 각각 두 문구를 제공한다.
  `Move`에서는 이동 목적을 추측하지 않으며, 등록된 Move 행도 후보에 넣지 않는다.
- 배고픔·갈증·피로는 현재값/최댓값이 기본 0.6 이상이면 후보를 추가한다. threshold는 need row별로 편집한다.
  행동 선택 임계값에는 영향을 주지 않는다. 최댓값이 0 이하이거나 유효하지 않은 수치는 후보에서 제외한다.
- 후보 키를 중복 제거하고 동일 확률로 선택한다. 다른 후보가 있으면 직전 생각은 제외한다.
  후보가 없으면 기본 문구 하나를 사용한다. 유지 중 새 후보가 늘어나는 것만으로 재추첨하지 않는다.
- 기본 실시간 4초 유지. 기존 키를 지지하는 조건이 모두 사라지면 다음 조회에 재선택한다.
  Hover 이탈만으로 캐시를 지우지 않으며, 조회가 없으면 시간 경과만으로 추첨하지 않는다.
- NPC의 전용 `IRandomSource`를 serialized reference로 연결한다. gameplay 난수와 공유하지 않는다.
- `TrySetFixedMessage(LocalizeKey, out uint)`는 새 토큰을 발급하고 이전 지정 대사를 교체한다.
  `TryClearFixedMessage(uint)`는 현재 토큰만 수락한다. 해제 후 다음 조회에서 현재 후보로 돌아간다.
  Set/Clear는 UI를 자동으로 열지 않으며 큐나 우선순위 스택은 없다.
- 비활성화와 runtime stat identity 교체는 지정 대사·생각·timer를 지운다.
  토큰 발급 번호는 reset 후 재사용하지 않는다. uint 발급 한계에 도달하면 새 Set을 거부한다.
- `NPCComponent`, action, 행동 selector, `NPCStat`에는 메시지 책임을 추가하지 않는다.

## Unity 배선

초기 코드 전용 제한 이후 사용자가 YAML 배선을 명시적으로 승인했다. 기존 Hierarchy를 유지해
`Assets/Data/ScriptableObject/NPCThoughtCatalog.asset`, NPCGirl 프리팹과 FarmerTest에 배선을 적용했다.

- NPCGirl 루트의 `NPCMessageSource`는 같은 루트 WorkerNPC와 전용 `SeededRandomSource`(seed 73021),
  Catalog, `Visual/MessageAnchor`를 참조한다. anchor의 local y는 0.85다.
  기존 Friendly 레이어와 물리는 유지한다. Farmer/Guard 모두 같은 프리팹을 사용한다.
- FarmerTest: `Canvas/HoveringUI/NPCMessageHover/MessageText`를 추가하고 hover 한 개를 UIManager에 등록했다.
  Router는 기존 Hoverable mask에 Friendly를 합치며 기본 우선 category는 NPCMessage다.
- 기존 `ui-speech-panel-white-9slice.png`의 Sliced Image와 기본 폰트 uGUI Text/LocalizeText를 쓴다.
  패널은 360×76, bottom-center pivot, Text 20px/중앙 정렬/단일 행이며 시작 시 비활성이다.
  graphic raycast와 CanvasGroup interaction을 차단하고 Main Camera·기존 Canvas를 참조한다.
- 기존 LocalizeData를 참조하는 독립 루트 LocalizeManager를 Systems 다음 root로 배치했다.
  `LocalizeManager.Awake`의 dedicated-root/DontDestroyOnLoad 계약을 지키기 위한 배치이며 다른 Manager를 이동하지 않았다.
  자동 표시·아이콘은 없다. 기존 Legacy Text 말풍선의 타이핑은 [UI](UI.md)의 NPCMessageHover가 소유한다.
- 진입 시 위로 올라오며 커지고 퇴장 시 반대로 움직이는 DOTween 연출은 [UI](UI.md)의
  NPCMessageHover가 소유한다. 메시지 선택·유지 시간·지정 대사 계약은 그대로다.

## 검증 도구와 제약

`Assets/Editor/NPCMessageTests.cs`는 실제 selector/state를 주입 시간·난수로 검사하는 순수 자동 검사다.
`Assets/TestOnly/TestNPCMessageControls.cs`는 Inspector에 지정한 source의 Set/Replace/Stale Clear/Current Clear를
조작한다. source 또는 표시 UI를 자동 생성하지 않는다. 씬에는 아직 배치하지 않았다.

명령행 컴파일·순수 검사는 Unity lifecycle, collider 겹침, 카메라 이동·줌, Canvas 경계,
한글 glyph, 9-slice 렌더, Farmer/Guard 모집·행동 회귀의 실행 증거가 아니다.
`FarmerScene.Structure v1`도 메시지 동작 자체를 검증하지 않는다. 현재 판정은
[PROGRESS](../Status/PROGRESS.md)와 `.harness-runs/npc-message-wiring-20260920/verification-summary.json`을 확인한다.
이전 코드 후보의 증거는 `.harness-runs/npc-message-20260920/`에 보존했다.

## 관련 문서와 갱신 조건

[UI](UI.md), [NPC Runtime](NPC_Runtime.md), [Localization](Localization.md),
[Spawning and Pooling](Spawning_and_Pooling.md). 선택 조건, API, 초기화·pooling 수명,
전용 난수, 배선 또는 실행 검증 상태가 바뀌면 이 문서를 갱신한다.
