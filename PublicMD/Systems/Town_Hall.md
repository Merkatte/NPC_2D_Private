# Town Hall

## 기능 목적과 책임 경계

하나의 TownHallRecruitment가 직군별 후보, 쿨다운, 예약과 낙하 코루틴을 독립적으로 소유한다. Farmer는 60초/100골드, Guard는 90초/100골드이며 두 후보 모두 초기 준비 상태다. 골드 잔액은 [Player Gold](Player_Gold.md), worker 예약 API는 [Spawning and Pooling](Spawning_and_Pooling.md)이 소유한다.

TownHallVisual은 클릭 표면과 월드 아이콘만 소유한다. TownHallPopup은 탭 선택과 모집 의도를 전달하고 TownHallRecruitCard는 읽기 전용 상태를 표시한다. UI가 타이머나 골드를 직접 변경하지 않는다.

## 현재 실행 흐름

1. Awake가 직군별 설정을 검증하고 CandidateReady, 남은 시간 0으로 runtime 상태를 생성한다. 중복/잘못된 설정은 한 번 오류를 출력하고 모집을 거부한다.
2. TryDispatchCandidate(NPCType)가 해당 직군의 준비 상태와 활성 상태를 확인한다.
3. NPCManager.TryReserveWorker → GoldManager.TrySpend 순으로 처리한다. 예약 실패는 골드·후보 무변경, 골드 부족은 예약 취소 후 후보 보존이다.
4. 성공한 직군만 Recruiting으로 전환하고 전체 쿨다운 값을 채운다. 낙하 및 기립 연출 중에는 해당 직군의 IsDispatching이 타이머를 막는다. 다른 직군은 계속 모집·카운트다운할 수 있다.
5. 기존 SpawnLanding clip의 visual 하강량을 제외한 나머지만 root에 적용한다. 기립까지 기다린 뒤 CommitReservation을 호출한다. 성공 후에만 해당 직군의 카운트다운을 시작한다.
6. 컴포넌트 또는 GameObject 비활성화 시 각 코루틴을 명시적으로 중지하고 남은 유료 예약은 착지점에서 커밋한다. 커밋 실패는 예약 취소·지원금 환급·후보 복구로 처리한다. 씬 teardown 중에는 worker를 다시 초기화하지 않는다.
7. 하나라도 CandidateReady이면 준비 완료 월드 아이콘, 모두 대기 중이면 모집 중 아이콘이다.

TryGetRecruitment(NPCType, out RecruitmentStatus)는 값 복사만 반환한다. 요청되지 않은 직군이나 미구성 상태는 false다. 기존 무인자 모집 API와 강제 상태 변경 API는 없다.

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Actor/TownHallRecruitment.cs` | 직군별 설정·상태·타이머·예약·지원금 transaction·낙하 lifecycle |
| `Assets/Data/Struct/RecruitmentStatus.cs` | UI가 읽는 직군·phase·남은 시간·비용·낙하 상태 값 |
| `Assets/Scripts/System/Actor/TownHallVisual.cs` | 클릭 표면과 집계 phase 아이콘 |
| `Assets/Scripts/UI/TownHallPopup.cs` | 탭 선택, 상태 조회와 모집 의도 전달 |
| `Assets/Scripts/UI/TownHallRecruitCard.cs` | 직군별 이미지·이름·비용·상태·시간·버튼 표시 |
| `Assets/Scripts/Enum/RecruitPhase.cs` | Recruiting/CandidateReady, 기존 직렬화 값 유지 |
| `Assets/Scripts/Enum/RecruitResult.cs` | Success/NotReady/NotEnoughGold/SpawnUnavailable |

WorkerReservation의 계약과 소유는 [Spawning and Pooling](Spawning_and_Pooling.md)을 따른다.

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 범위 |
|---|---|
| 후보·쿨다운·직군 설정 | TownHallRecruitment.cs, TownHall.prefab |
| 예약·지출·비활성화 | TownHallRecruitment.cs, NPCManager 예약 API, Player Gold, Spawning and Pooling |
| 탭·카드·버튼 | TownHallPopup.cs, TownHallRecruitCard.cs, TownHallPopup.prefab, UI |
| 시청 클릭·월드 아이콘 | TownHallVisual.cs, PointerClickRouter, FarmerTest.unity |

## 불변 규칙

- 하나의 시청 컴포넌트가 모든 직군을 소유하며 runtime 상태와 Coroutine을 직군 간 공유하지 않는다.
- 예약 → 지출 → 낙하 → 커밋 순서를 지킨다. 성공한 직군만 쿨다운을 재설정하며 타 직군의 시간을 멈추지 않는다.
- 낙하 중 연타와 비활성 상태의 모집은 NotReady다. 중복 커밋은 NPCManager의 기존 예약 집합이 거부한다.
- 초기 모집은 즉시 가능하다. 팝업 재개방·탭 전환·모집 성공은 모집 도메인을 초기화하지 않는다.
- 모집 성공 후 팝업은 유지된다. 기본 탭은 모집이며 마을 상태 탭에는 정확히 `준비중입니다`를 표시한다.
- 타이머는 Time.deltaTime을 사용하며, 팝업은 초 단위 표시가 바뀔 때만 시간 문자열을 갱신한다.

## Unity 배선과 검증

FarmerTest의 기존 TownHall 인스턴스는 NPCManager/GoldManager를 prefab override로 참조한다. TownHall.prefab의 단일 Farmer 값을 `_recruitments` 목록의 Farmer 행으로 이관하고 Guard 행을 추가했다. FarmerTest NPCManager에는 Farmer와 Guard 생성 entry가 있고 worker pool을 공유한다.

TownHallPopup.prefab은 기존 목재 외곽·제목·닫기 버튼을 유지하며 상인 탭과 양피지 스타일을 재사용한다. 두 카드에는 기존 farmer-hoe와 guard-sword sprite subasset을 지정한다. scene의 `_recruitment`와 UIManager popup 등록은 유지한다. PointerClickRouter와 시청 클릭 collider의 layer 9 mask 연결은 이미 존재하며 레이어 이름 자체는 필수 조건이 아니다.

기존 TestTownHallRecruitProbe를 직군 지정 API로 이관했다. 현재 Farmer가 Recruiting일 때의 거부, 미등록 role 예약 실패, 준비 상태의 골드 부족, 미구성 인스턴스 거부를 확인한다. 만들 수 없는 상태는 SKIP이며 초기 대기 상태를 가정하지 않는다.

2026-09-19 작업의 실제 Play Mode(동시 모집, 60/90초 독립 카운트다운, 낙하 중 disable, 팝업 조작)는 사용자 지시로 생략하여 NOT_VERIFIED다. 컴파일·직렬화 확인은 runtime 통과를 대신하지 않는다.

## 알려진 제약과 TBD

- 후보 직업 추첨, 스탯 랜덤화, 명부·스탯 미리보기는 보류한다. 직군은 명시적 설정이고 기존 role stat definition을 재사용한다.
- 마을 상태는 placeholder다. 업그레이드, 해고·despawn, 저장/불러오기는 추가하지 않는다.
- 낙하 먼지·사운드는 이번 범위가 아니다.

## 관련 문서

- [Player Gold](Player_Gold.md)
- [Spawning and Pooling](Spawning_and_Pooling.md)
- [UI](UI.md)
- [Guard](Combat/Guard.md)

## 문서 갱신 조건

직군 설정·예약 transaction·낙하 lifecycle·UI 탭·Unity 배선이 바뀌면 갱신한다.
