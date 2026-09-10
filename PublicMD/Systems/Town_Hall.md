# Town Hall

## 기능 목적과 책임 경계

시청이 일정 쿨다운마다 이주 희망자 1명을 모집하고, 플레이어가 정착지원금(골드)을 지불하면 NPC가 하늘에서 떨어지는 연출과 함께 스폰되는 흐름을 설명한다. 골드 로드맵(골드 → 상단 → 모집)의 3단계다.

이 문서가 소유하지 않는 것: `GoldManager`(잔액 자체는 [Player Gold](Player_Gold.md) 소유), 클릭 감지 입력 adapter(`PointerClickRouter`/`IClickPopupSource`는 [UI](UI.md) 소유), worker pool과 NPC 생성 진입점 자체(`WorkerPool`/`NPCManager.CreateNPC`는 [Spawning and Pooling](Spawning_and_Pooling.md) 소유 — 이 문서는 그 위에 추가된 예약 API(`TryReserveWorker`/`CommitReservation`/`CancelReservation`)의 소비자일 뿐이다).

**이번 버전은 의도적으로 최소 구조다.** 모집 직업은 직렬화된 단일 값(`_recruitNpcType`, 기본 Farmer)이고 스탯은 role별 고정 `NPCStatDefinition`을 그대로 쓴다 — 후보 명부(roster), `IRandomSource` 기반 직업 추첨, 스탯 랜덤화는 전부 후속 작업으로 보류됐다(§알려진 제약과 TBD). 영구적으로 없애기로 한 게 아니라, 게임 루프(생산 → 판매 → 골드 → 인구 → 생산)를 먼저 닫기 위한 범위 축소다.

핵심 분리: `TownHallVisual`(클릭 표면 + 월드 아이콘)과 `TownHallRecruitment`(모집 도메인)는 한 GameObject 위에 같이 있지만 서로 다른 책임을 진다 — `TownHallVisual`은 `TownHallRecruitment`가 `SetPhase(RecruitPhase)`로 알려주는 상태만 그대로 저장·표시할 뿐, 쿨다운이나 골드를 전혀 모른다. `TownHallPopup`은 `TownHallRecruitment`를 폴링해 표시를 갱신하고, "모집하기" 버튼을 누르면 `TryDispatchCandidate()`만 호출한다.

## 현재 실행 흐름

```text
TownHallRecruitment (씬에 상시 활성 — 별도 스케줄러 없음, 아래 "불변 규칙" 참고)
  Awake(): 참조·tuning 값 검증 -> StartInitialCooldown() (phase=Recruiting, 쿨다운 값 채움)
  Update(): phase==Recruiting && !_pendingReservation.IsValid 인 동안만 쿨다운 감소
    -> 0 이하가 되면 phase=CandidateReady, TownHallVisual.SetPhase(CandidateReady)

클릭(항상 성립 — 상단과 달리 게이트 없음):
PointerClickRouter -> TownHallVisual.TryGetClickPopup() -> 항상 true(구성이 정상인 한)
  -> IUIService.TryShow(PopupType.TownHall) -> TownHallPopup 오픈

팝업 표시(폴링, OnOpened 이후 매 프레임):
TownHallPopup.Update() -> _recruitment.Phase 읽어 패널 토글
  Recruiting: 남은 시간(정수 초, 값 바뀔 때만 텍스트 갱신)
  CandidateReady: 직업명 + 정착지원금 + "모집하기" 버튼

"모집하기" 클릭 -> TownHallRecruitment.TryDispatchCandidate():
  1. phase==CandidateReady && !_pendingReservation.IsValid 검증 -> 아니면 NotReady
  2. dropStart = landingPosition + Vector3.up * _dropHeight
     NPCManager.TryReserveWorker(SelectRecruitType(), dropStart, out reservation)
       -> creation entry 조회·stat 생성·CanUseStat·풀 대여·worker.BeginSpawnPresentation()이 전부 끝난다
       -> 실패면 SpawnUnavailable (골드 무변경)
  3. GoldManager.TrySpend(_settlementCost)
       -> 실패면 NPCManager.CancelReservation(reservation) 후 NotEnoughGold (골드 무변경)
  4. _pendingReservation=reservation, phase=Recruiting, _cooldownRemaining=_cooldownDuration(즉시 채움),
     TownHallVisual.SetPhase(Recruiting)
  5. 낙하 코루틴 시작(dropStart -> landingPosition). Update()가 _pendingReservation.IsValid인 동안
     쿨다운을 절대 안 줄이므로, 실제 카운트다운은 착지 이후부터 시작되는 것과 동일한 효과를 낸다.
  6. 착지 -> CommitPendingReservation() -> NPCManager.CommitReservation(reservation)
       -> worker.CompleteSpawnPresentation() -> worker.Init(...) -> _workers 등록
       -> 성공해야만 _pendingReservation=default(실패 시 그대로 남겨 상태를 잃지 않음)
  성공 시 팝업은 스스로 닫는다(낙하 연출을 가리지 않기 위해)
```

`NPCManager`의 예약 API(`TryReserveWorker`/`CommitReservation`/`CancelReservation`)는 [Spawning and Pooling](Spawning_and_Pooling.md)이 주 소유한다. 반환된 `WorkerNPC`는 `Init` 전이라 `Update()`가 no-op이지만, `WorkerPool.OnGetWorker`가 대여 즉시 `SetActive(true)`를 하므로 `Collider2D`는 이미 활성 상태다 — 낙하 중 `CombatPerception`류 감지에 걸리지 않도록 `WorkerNPC.BeginSpawnPresentation()`이 Rigidbody2D simulation과 gameplay Collider2D를 꺼 두고, `CompleteSpawnPresentation()`이 커밋 직전(또는 취소 시 반환 직전)에 이전 상태로 복구한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Actor/TownHallRecruitment.cs` | 쿨다운 타이머, 후보 상태, 정착지원금 트랜잭션, 낙하 연출 코루틴의 단일 소유자 |
| `Assets/Scripts/System/Actor/TownHallVisual.cs` | 클릭 표면(`IClickPopupSource`, 항상 true) + 모집중/완료 아이콘 2개 토글 |
| `Assets/Scripts/UI/TownHallPopup.cs` | 모집 상태 표시(`PopBase`)와 "모집하기" 버튼 |
| `Assets/Scripts/Enum/RecruitPhase.cs` | 모집 상태(Recruiting/CandidateReady, 2개뿐) |
| `Assets/Scripts/Enum/RecruitResult.cs` | `TryDispatchCandidate()` 결과(Success/NotReady/NotEnoughGold/SpawnUnavailable) — `TradeResult` 선례처럼 `Try...`가 enum을 직접 반환 |
| `Assets/Data/Struct/WorkerReservation.cs` | 예약된 `WorkerNPC`, 예약 시 생성한 `NPCStat`, 기존 `NPCCreationEntry.Selector` 참조를 함께 보관하는 값 타입 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 쿨다운·지원금·낙하 tuning | `TownHallRecruitment.cs` |
| 트랜잭션 순서·실패 분기 | `TownHallRecruitment.TryDispatchCandidate`/`CommitPendingReservation`, [Player Gold](Player_Gold.md) |
| 예약 스폰 API 자체 | [Spawning and Pooling](Spawning_and_Pooling.md)의 `NPCManager.TryReserveWorker`/`CommitReservation`/`CancelReservation`, `WorkerNPC.BeginSpawnPresentation`/`CompleteSpawnPresentation` |
| 클릭 가능 판정·아이콘 | `TownHallVisual.cs`, [UI](UI.md)의 `PointerClickRouter`/`IClickPopupSource` |
| 팝업 표시·버튼 | `TownHallPopup.cs`, [UI](UI.md) |
| 후보 직업 결정 로직 | `TownHallRecruitment.SelectRecruitType()` (현재는 고정값 반환 — 후속 확장 지점) |

## 불변 규칙

- `TownHallRecruitment`는 항상 활성인 GameObject 위에 산다. `MerchantArrivalScheduler`처럼 타이머를 별도 GameObject로 분리하지 않는다 — 그 분리는 캐러밴이 방문 사이에 비활성이라 필요했던 것이고, 시청은 그 이유가 없다.
- 눈에 보이는 phase는 `Recruiting`/`CandidateReady` 2개뿐이다. "낙하 중"이라는 3번째 phase를 만들지 않고, `_pendingReservation.IsValid`로 표현한다.
- 골드는 되돌릴 수 없는 마지막 단계다: worker 예약(`TryReserveWorker`, `CancelReservation`으로 완전히 되돌릴 수 있음)을 먼저 하고, 그다음에만 `GoldManager.TrySpend`를 부른다. `GoldManager.Add`는 "획득" 전용 API라 환불에 쓰지 않는다.
- `_cooldownRemaining`은 골드 지출이 성공한 그 순간(낙하 시작 전) 이미 `_cooldownDuration`으로 채워진다. 착지(커밋 성공) 시 다시 초기화하지 않는다 — `Update()`가 `_pendingReservation.IsValid`인 동안 감소를 멈추는 것만으로 "착지 이후부터 카운트다운"이 성립한다.
- `CommitPendingReservation()`은 `NPCManager.CommitReservation`이 성공했을 때만 `_pendingReservation`을 지운다. 실패 시 필드를 그대로 남겨 골드와 예약을 모두 잃는 상태를 피한다. 정상 경로에서 이 커밋은 실패하지 않는다는 것이 invariant다.
- 낙하 중 `TownHallRecruitment`가 비활성화되면(씬 언로드 등) 예약을 반납하지 않고 착지 지점으로 즉시 이동시킨 뒤 그대로 커밋한다 — 골드가 이미 나갔으므로 반납은 플레이어가 대가 없이 돈을 잃는 결과가 된다.
- `TownHallVisual`은 `IUIService`를 참조하지 않는다. 시청은 항상 클릭 가능해서(요구사항) 상단처럼 "상태가 바뀌었으니 열린 팝업을 강제로 닫아야 하는" 경우가 없다. 대신 `TownHallPopup`이 전달 성공 시 스스로 닫는다.
- `TownHallPopup`-`TownHallRecruitment`는 1:1 전용 배선이라 interface로 감싸지 않는다(`MerchantPopup`-`MerchantTradeSite` 선례와 동일).
- 후보 직업 추첨을 위한 `IRandomSource` 주입은 이번 버전에서 하지 않는다. `SelectRecruitType()`이 유일한 확장 지점이며, 별도 abstraction이나 후보 데이터 구조를 미리 만들지 않는다.
- TestOnly 검증을 위한 production API(강제 phase 설정, worker despawn, stat getter)를 추가하지 않는다 — `TestTownHallRecruitProbe`는 실제 API만으로 구성 가능한 실패 경로만 자동 검증한다.

## Unity 배선과 검증 도구

**2026-09-11 기준**, `Assets/Prefab/InGame/TownHall.prefab`(루트에 `BoxCollider2D`+`TownHallVisual`+`TownHallRecruitment`, layer 9, 아이콘 자식 2개)과 `Assets/Prefab/UI/TownHallPopup.prefab`은 완성됐지만 `FarmerTest.unity`에 씬 한정 참조(`_npcManager`/`_goldManager`, `UIManager._popups` 등)가 배선되지 않았다. 프리팹 자체는 씬과 무관하게 독립적으로 완성되므로 배선만 남았다.

씬 배선 체크리스트:

1. `TownHall` 인스턴스(이미 `FarmerTest.unity`에 배치돼 있음): `TownHallRecruitment._npcManager`/`_goldManager`/`_visual` 연결, `_recruitNpcType`이 `Farmer`인지 확인(기본값), 쿨다운·정착지원금·낙하 tuning 조정. `_npcManager`/`_goldManager`는 씬 오브젝트 참조라 프리팹에 저장되지 않고 씬의 `TownHall` `PrefabInstance` 블록에 `m_Modifications` 항목으로 저장된다.
2. `RecruitingIcon`/`CandidateReadyIcon` 자식에 스프라이트 배정(미배정이어도 동작에는 지장 없음, 화면에만 안 보임).
3. Canvas 아래에 `TownHallPopup.prefab` 배치(비활성 상태로 시작) → `TownHallPopup._recruitment` ← 씬의 `TownHall`의 `TownHallRecruitment`. `UIManager._popups`에 등록해야 팝업이 실제로 열린다(`PopupType.TownHall` 기존 값 사용).
4. **공통 선행 조건**: `PointerClickRouter`를 씬에 배치하고 Layer 9에 이름(예: `Clickable`)을 부여해 `_clickableMask`와 짝을 맞춘다. `MerchantCaravan.prefab`도 이미 Layer 9를 쓰고 있어 상단 팝업과 공유되는 미완 배선이다.
5. `EventSystem`/`InputSystemUIInputModule`/Canvas의 `GraphicRaycaster`는 `FarmerTest.unity`에 이미 있음 — 확인만.

검증 도구: `Assets/TestOnly/TestTownHallRecruitProbe.cs`(IMGUI PASS/FAIL). `TownHallRecruitment`의 쿨다운·낙하는 실제 시간이 걸리는 상태라, 구성 없이 즉시 검증 가능한 실패 경로(초기 `Recruiting` 상태에서 `NotReady`, 미등록 직업에서 `SpawnUnavailable`, `CandidateReady` 상태에서 골드 0원일 때 `NotEnoughGold`, 미구성 인스턴스에서 `NotReady`)만 자동 검증한다. 성공 경로(실제 NPC 커밋)는 씬에 되돌릴 수 없는 변화를 남기므로 의도적으로 자동 프로브에서 제외했다 — Play Mode 수동 검증 대상이다.

## 알려진 제약과 TBD

- **후보 직업 추첨과 스탯 랜덤화는 이번 범위에서 완전히 보류됐다.** 현재는 직렬화된 단일 `_recruitNpcType`(기본 Farmer)과 role별 고정 `NPCStatDefinition.CreateRuntimeStat()`을 그대로 쓴다. 후속 확장(복수 직업 후보 목록, `IRandomSource` 기반 후보 직업 추첨, NPC 스탯 랜덤화, 후보별 스탯 미리보기, 스탯과 직업을 반영한 정착지원금 계산)을 위한 roster·randomizer·별도 ScriptableObject는 지금 만들지 않는다. 구현 시 `TownHallRecruitment.SelectRecruitType()`과 `NPCManager.TryReserveWorker()` 내부의 stat 생성 지점만 교체하면 되도록 책임 경계를 유지해 뒀다. `IRandomSource`/`SeededRandomSource` 타입 자체는 그대로 있고, 이번 기능만 참조하지 않는다.
- 마을 업그레이드, NPC 해고·despawn은 다루지 않는다. `NPCManager`에는 여전히 production despawn API가 없다.
- 골드 잔액 상시 표시 UI는 없다.
- 낙하 착지 이펙트(먼지·사운드)는 파티클·오디오 시스템이 없어 다루지 않는다.
- 드래그 앤 드롭 팝업 작업 때와 마찬가지로, 이 저장소 작업에서는 실제 Play Mode 검증(쿨다운 카운트다운, 낙하 연출, 착지 후 NPC가 일하러 가는지, 연타 방어, `TownHall` 비활성화 시 즉시 착지·커밋)을 수행하지 못했다 — Unity 에디터에서 사용자가 확인해야 하는 TBD.

## 관련 문서

- [Player Gold](Player_Gold.md) — `GoldManager.TrySpend`, 정착지원금 지출 지점
- [Spawning and Pooling](Spawning_and_Pooling.md) — `NPCManager`의 예약 기반 2단계 스폰 API(`TryReserveWorker`/`CommitReservation`/`CancelReservation`), `WorkerNPC.BeginSpawnPresentation`/`CompleteSpawnPresentation`
- [UI](UI.md) — `PointerClickRouter`, `IClickPopupSource`, `PopBase`/`TownHallPopup`
- [Merchant Caravan](Merchant_Caravan.md) — 골드 로드맵의 이전 단계, 1:1 전용 provider·팝업 배선의 선례

## 문서 갱신 조건

트랜잭션 순서, 낙하 연출, phase 정의, 후보 직업 결정 방식(현재는 고정값), Unity 배선이 바뀌면 갱신한다.
