# 방어 웨이브와 이벤트

## 기능 목적과 소유 경계

방어 씬의 정비/습격, production 적 생성, 정비 이벤트, 일시정지와 GameOver 상태를 소유한다.
이 구현은 [승인 계획](../../Archive/Plans/Defense_Implementation_Plan.md)의 웨이브·이벤트 slice다.
전투 대상/등록은 Battlefield, 주민 HP는 NPCStat, 적의 행동 선택은 주입된 selector가 소유한다.
보상, 원정, 승리 조건, 자동 난이도 증가는 구현하지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Manager/DefenseGameSession.cs` | 이유별 pause lease, 전역 시간 배율 복구, 한 번의 GameOver 확정 |
| `Assets/Scripts/Manager/DefenseWaveController.cs` | 정비/습격 runtime 상태, production 적 생성과 예정/생존 수 동시 완료 판정 |
| `Assets/Scripts/Manager/DefenseEventController.cs` | 이벤트 조건 평가·유효 시간·선택·pause lease 및 결과 callback |
| `Assets/Scripts/UI/DefenseHUD.cs` | 웨이브/적/정비 시간·알림·GameOver 표시와 조기 시작/열기 명령 |
| `Assets/Scripts/UI/DefenseEventPopup.cs` | 이벤트 문구·선택지 표시, 선택/닫기 명령 |
| `Assets/Data/ScriptableObject/Script/DefenseWaveDefinition.cs` | 편성별 적 수와 생성 간격 |
| `Assets/Data/ScriptableObject/Script/DefenseWaveCatalog.cs` | 순서 있는 편성 목록, 마지막 편성 반복, 공통 정비 시간 |
| `Assets/Data/ScriptableObject/Script/DefenseEventDefinition.cs` | ID·웨이브 필터·정비 경과 조건·유효 시간·문구·선택지 |
| `Assets/Data/ScriptableObject/Script/DefenseEventCatalog.cs` | 중복 ID를 거부하는 순서 있는 이벤트 정의 목록 |
| `Assets/Data/Struct/DefenseEventChoice.cs` | 선택 표시 문구와 결과 연결 ID 값 |
| `Assets/Scripts/Manager/DefenseInitialRoster.cs` | production NPCManager 예약/커밋으로 초기 검병·궁병 추가 |
| `Assets/Editor/DefenseSetup.cs` | 승인된 DefenseTest와 전용 설정·프리팹·UI를 생성하는 조립 도구; assembly 역할만 실행 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 확인할 범위 |
|---|---|
| 편성/정비 | WaveDefinition, WaveCatalog, WaveController와 방어 웨이브 설정 에셋 |
| 적 생성 | WaveController, WorkerNPC.Init, NPCStatDefinition.CreateRuntimeStat, DefenseActor.BindBattlefield/Initialize와 적 prefab |
| 이벤트 추가/조건 | EventDefinition, EventCatalog, EventChoice, EventController와 이벤트 에셋 |
| pause/GameOver | GameSession, EventController, DefenseHUD와 시청 피해 owner의 EndGame 호출 |
| 화면 배선 | DefenseHUD/DefenseEventPopup의 Configure·serialized field, DefenseTest Canvas/방어 controller 참조 |

## 현재 실행 흐름

- WaveController.Start가 참조/편성을 검사하고 첫 정비를 시작한다. 정비는 catalog의 설정 시간이며 샘플은 60초다. 초기 정비의 `WaveNumber`는 0이고 다음 습격은 1이다.
- 정비 시간이 끝나거나 `TryStartNextWave`가 수락되면 다음 편성을 선택한다. 목록 이후에는 마지막 편성을 반복한다. 생성 시작은 다음 Update이고, 이후 scaled 시간 간격으로 최대 한 명씩 생성한다.
- 생성은 주입된 WorkerNPC prefab과 NPCStatDefinition/scene selector를 사용한다. stat 생존·selector 호환성을 검사한 뒤 복제하고 DefenseActor.BindBattlefield, WorkerNPC.Init 순으로 초기화한다. WorkerNPC가 공통 초기화에서 같은 NPCStat을 DefenseActor에 전달한다.
- 초기화/등록 실패는 해당 복제만 비활성화·Destroy하고 실패 사유를 한 번 기록한다. 예정 수를 차감하거나 정비로 넘어가지 않는다. HUD는 생성 오류로 진행이 중단되었음을 표시하고 세부 구성 사유는 로그로 확인한다.
- 모든 예정 생성이 성공했고 Battlefield.AliveEnemyCount가 0이어야 다음 정비를 시작한다. WaveController는 TestOnly spawner나 NPCManager 주민 roster를 참조하지 않는다.
- 적 spawn 위치는 연결된 Transform 목록을 순환한다. gameplay 전역 난수를 소비하지 않는다.

## 이벤트와 결과 연결

EventDefinition은 안정적인 ID, 등장 가능한 다음 웨이브 번호의 최소/최대(최대 0은 제한 없음), 정비 경과 시간, 유효 시간, 알림·제목·본문, 선택별 label/resultId를 가진다. EventCatalog 순서로 조건을 확인한다. 동시에 하나만 제시하고 같은 이벤트 ID는 정비 한 차례에 한 번만 제시한다. 새 정비에서는 다시 조건 평가가 가능하다.

유효 시간은 알림을 제시한 정비 경과 시점을 기준으로 한다. 알림만 있으면 시간이 계속 간다. `TryOpenPopup`은 GameSession에서 pause lease를 받는다. 닫기는 lease만 반환해 알림/남은 시간을 보존하고, 선택은 현재 이벤트를 먼저 해제한 뒤 결과를 한 번 전달한다. 유효 시간 만료, 다음 웨이브, 구성 실패 또는 GameOver는 현재 이벤트를 종료한다.

`DefenseEventController._choiceResolved`는 Inspector에서 연결하는 `UnityEvent<string, string>`이며 인자는 eventId/resultId다. C# 소비자는 `ChoiceResolved`에 구독할 수 있다. 후속 콘텐츠는 이 경계에 해당 domain handler를 연결하며 view나 WaveController에 콘텐츠 규칙을 넣지 않는다. 샘플 확인/넘기기는 결과 ID만 확정하고 보상/원정은 수행하지 않는다. 범용 조건/효과 해석기는 없다.

## Pause와 UI lifecycle

GameSession 하나가 이유 문자열을 가진 정수 lease를 발급한다. 최초 정지 시 기존 Time.timeScale을 저장하고, 마지막 lease가 반환되고 GameOver가 아닐 때만 복구한다. EndGame은 idempotent이며 이벤트가 lease를 반환해도 GameOver 정지는 유지된다. scene owner 비활성화/해제 시에는 다음 씬에 전역 정지가 남지 않도록 배율을 복구한다.

웨이브/이벤트 timer는 scaled 시간만 사용한다. Update에서는 session pause와 Time.timeScale도 확인하므로 같은 프레임의 UI 입력 이후 진행하지 않는다. 기존 actor/provider gameplay loop의 정지 준수는 해당 owner가 맡는다. UI 버튼 입력과 event subscription에는 scaled timer를 쓰지 않는다.

HUD와 Popup은 상시 활성 host에 붙이고 실제 숨기는 popup/game-over panel은 별도의 자식으로 연결한다. Popup component 자체를 panel에 붙이면 알림을 다시 받아 열 수 없으므로 금지한다. OnEnable/OnDisable은 구독과 버튼 listener를 대칭적으로 관리한다. Popup host 비활성화는 열린 popup lease를 반환한다. 두 view는 domain에 명령만 전달하고 직접 시간 배율·HP·보상·생성 수를 변경하지 않는다.

배선은 기존 Canvas/EventSystem/GraphicRaycaster와 Legacy Text/Button을 재사용한다. 이벤트 panel과 GameOver panel은 전체 화면 raycast 차단 배경을 포함해 뒤의 world/다른 운영 UI 입력을 막는다. sample 두 선택에 맞는 버튼/label 배열을 연결한다. 더 많은 선택을 추가하면 배열도 함께 확장한다. 버튼 부족은 한 번 진단하고 popup을 닫아 숨겨진 선택을 확정하지 않는다.

## 전용 씬 조립

DefenseSetup은 기존 BuildingTest에서 새 DefenseTest만 복사한다. 기존 초기 자원·시설·건축가를 보존하고 DefenseInitialRoster가 검병 2명·궁병 2명을 production 예약/커밋으로 추가한다. 신규 적은 WaveController가 별도로 생성한다. 기존 TestOnly 적 생성과 초소의 TemporaryGameOverReporter는 방어 씬에서 비활성화한다.

방어 주민/적은 기존 프리팹을 독립 복사한 전용 prefab이다. 기존 root·Visual/tool/cargo 하위 구조를 보존하면서 BodyPose 부모를 추가하고 기존 root animation clip의 Visual 경로만 전용 override clip에서 remap한다. 원본 prefab/controller/clip은 변경하지 않는다. 시설 prefab은 전용 inherited variant와 복사한 BuildingDataContext/HousingDataContext를 사용한다. 기존 배치 시설은 새 씬 안에서만 unpack해 영구 BuildingPlot 아래로 옮기고 기존 navigation geometry와 초기 facility 참조를 보존한다. 구체 durability/maintenance 책임은 해당 Defense leaf가 소유한다.

씬 조립은 원래 열린 scene이 dirty이면 거부한다. 기존 DefenseTest가 있으면 임의 교체하지 않는다. 성공 시 소유한 새 에셋과 DefenseTest만 저장하고 새 씬을 활성으로 남긴다. 실패 시 새 씬의 미저장 메모리만 닫고 이전 scene setup을 복구하며 원래 씬은 저장하지 않는다. 조립 중 생성된 일부 새 파일은 실패 후 조사 대상으로 남기며 자동 삭제하지 않는다.

## 검증 범위와 미검증

컴파일·직렬화 참조·기존 비-Play 검사는 현재 작업 기록의 실제 결과를 따른다. 코드만으로 prefab/Canvas 배선 성공을 주장하지 않는다.
예정 spawn이 남은 상태에서 생존 수 0, 마지막 적 제거, popup 닫기/재열기와 다른 lease 공존, GameOver 중 popup 종료, scene 종료 후 시간 배율 복구는 사람 Play QA 항목이다. 자동 Play와 게임 화면 검증은 수행하지 않는다.
