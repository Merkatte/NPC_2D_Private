# NPC Dissatisfaction

## 목적과 책임

Farmer·Builder·Guard·Archer의 원인별 불만 누적·회복과 태업 판정을 소유한다. Enemy는 원인 등록·시간 갱신 대상에서 제외한다. 주거 연결 씬의 자동 무주택 원인과 입주 효과는 Housing이 소유한다.

Defense Downed의 `SetSuspended`는 상태/원인을 초기화하지 않고 시간 Tick만 정지한다. WorkerNPC의 실행 중단이 이를 호출한다. 실제 disable의 Unbind와 새 Init은 기존 reset 계약을 유지한다. 치료·회복 API는 추가하지 않았다.

| 주 소유 파일 | 책임 |
|---|---|
| `Assets/Data/Struct/DissatisfactionSettings.cs` | 네 설정값과 유효성 |
| `Assets/Scripts/Enum/DissatisfactionCause.cs` | 원인 ID: None=0(등록 불가), Homeless=1 |
| `Assets/Scripts/System/Actor/DissatisfactionState.cs` | 원인별 활성·누적량, 회복, 제한된 합산과 태업 |
| `Assets/Scripts/Actor/NPCDissatisfaction.cs` | 주입된 주민 상태의 시간 갱신·원인 등록·해제 수명 |

NPCStat/IStatView/DefaultStatContext는 [NPC Runtime](NPC_Runtime.md), Guard definition은 [Guard](Combat/Guard.md)가 소유한다.

## 설정과 계산

SO의 `_dissatisfactionSettings` 안에 `_increasePerSecond`, `_decreasePerSecond`, `_maximum`, `_strikeThreshold`를 둔다. 임시 기본값은 1/초, 1/초, 100, 60이며 최종 밸런스는 TBD다. 정의는 매번 새 runtime stat으로 설정을 복사한다. 기존 생성자는 기본 설정과 현재값 0/원인 없음/태업 아님으로 호환된다.

속도는 유한한 0 이상, 상한은 양수, 기준은 `0 < 기준 <= 상한`이어야 한다. OnValidate와 definition factory는 설정을 보정하고 직접 runtime 생성자는 잘못된 설정을 거부한다. runtime에서 SO를 변경하거나 설정을 교체하지 않는다.

활성 원인은 각각 증가 속도로 쌓이고 해소된 원인은 감소한다. 반복 등록은 중복 항목을 만들지 않고 회복 중 재발하면 잔량에서 증가한다. Tick은 감소 후 총 상한의 남은 여유를 활성 원인에 균등 배분한다. 상한을 넘는 누적량을 숨겨 저장하지 않는다. 합계가 기준 이상이면 태업, 미만이면 복귀 가능하다. 현재 원인은 Homeless 하나지만 합산 구조는 향후 enum에 추가되는 원인을 지원한다.

## 연결과 수명

WorkerNPC의 `_dissatisfaction`은 같은 NPCGirl 루트 component다. Init의 Initialize(role,state)와 disable의 Unbind만 수행하며 불만 정책을 갖지 않는다. NPCDissatisfaction은 시설 검색·queue 선택 없이 기본 실행 순서 -100에서 Time.deltaTime을 갱신한다. 일시정지(deltaTime=0), 미초기화·Enemy·비활성 중에는 누적하지 않는다. Unbind/OnDisable은 원인·잔량·상태 참조를 지워 pool 재사용에 넘기지 않는다.

외부 domain은 활성 component의 `TrySetCauseActive(DissatisfactionCause.Homeless, true/false)`를 호출한다. 실패 시 상태는 변경되지 않는다. 읽기는 IStatView의 CurrentDissatisfaction·MaximumDissatisfaction·IsOnStrike, 원인별 값은 state.GetContribution을 사용한다.

selector는 업무 provider·건설 예약·전투 target 조회 전에 태업을 확인한다. 공통 생활/배회 queue는 선택된 생활 행동, 해결 불가 긴급 욕구의 timed Idle, 나머지 Wander 순서다. Guard는 target을 지운다. ActionContext.RequiresWorkAvailability는 업무와 업무 이동만 제한한다. 공통 시작/Tick 검사에서 효과 이전에 ReplanRequested를 내며 기존 Stop/Clear가 경로·도구·예약을 정리한다. 확정된 cargo·생산물·공사 진행률은 보존한다. 생활/배회 한 구간은 회복해도 끝낸 뒤 다음 선택에서 복귀한다. Enemy에는 work 제한을 설정하지 않는다.

메시지는 지정 대사 > 태업 > 일반 생각이며 [NPC Messages](NPC_Messages.md)가 소유한다. selector·action 연결은 [Selector and Queue](NPC_Decision_and_Actions/Selector_and_Queue.md), [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md), 해당 role leaf를 따른다.

## 최소 확인 범위·검증

계산 변경은 위 네 파일과 NPCStat/IStatView, 초기값은 DefaultStatContext/GuardStatDefinition 및 해당 SO, 수명은 WorkerNPC/NPCGirl과 [Spawning](Spawning_and_Pooling.md)를 읽는다. 구조·API·설정·수명·원인 ID 변경 시 이 문서를 갱신한다.

현재 검사·배선·리뷰 결과는 [PROGRESS](../Status/PROGRESS.md)와 `.harness-runs/dissatisfaction-20261003/scene-work.md`를 따른다. 자동 Play/화면 검증은 하지 않으며 원인 등록·회복·업무 중단·pool 재사용·Hover는 사람의 확인 항목이다.

## 주거 원인과 회복 보너스

주거 연결 씬에서는 ResidentHousingState가 입주 여부에 따라 Homeless 원인을 등록/해소한다. NPCDissatisfaction.Initialize의 선택적 residence는 집의 회복 보너스를 읽는다. DissatisfactionState.Tick의 recoveryBonusPerSecond는 기존 감소 속도에 더하며 비활성 원인에만 적용한다. 외출 중에도 입주 보너스를 유지하고 활성 무주택 증가를 상쇄하지 않는다. 미연결 기존 씬에는 자동 원인을 추가하지 않는다. 상세는 [Housing](Housing/README.md)를 따른다.
