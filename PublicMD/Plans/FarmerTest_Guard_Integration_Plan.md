# FarmerTest 경비·모집·방어 통합

2026-09-19 사용자 승인 계획. 원본 프로젝트에서 순차 구현하며 자동 commit/push하지 않는다.

## 확정 범위

- GuardPost의 BoxCollider2D 로컬 사각형과 전용 SeededRandomSource로 반복 순찰한다. 전투·식사·수면은 영역 밖 이동을 허용한다. 범용 길찾기는 제외한다.
- GuardPost가 최대 체력 100(직렬화 tuning)과 현재 체력을 소유하고 CombatTarget에 IHealthState로 주입한다. Friendly 피격 collider와 순찰 collider를 분리한다. 사망 시 visual을 숨기고 대상에서 제외하며 루트는 보존한다. TemporaryGameOverReporter가 GameOver를 한 번 출력한다.
- 하나의 TownHallRecruitment가 Farmer(60초/100골드), Guard(90초/100골드)의 후보·타이머·예약·낙하를 독립 소유한다. 초기 두 후보 모두 준비. 예약→지출→낙하→커밋 후 해당 직군만 카운트다운한다.
- TryDispatchCandidate(NPCType), TryGetRecruitment(NPCType, out RecruitmentStatus). 기존 NPCManager 예약 API와 enum 값은 유지한다.
- 시청 팝업은 마을 상태/모집 탭, 기본 모집 탭, 두 카드, 성공 후 팝업 유지. 상태 탭의 문구는 정확히 `준비중입니다`.
- FarmerTest 농사·거래를 유지하고 Guard/수동 Enemy 스포너/초소를 배선한다. GuardTest 순찰만 최소 이관한다. 씬 이름·시작 설정은 변경하지 않는다.
- 코드·기능 확인 후 마지막에 초소 이미지 1개 생성·검수·지연 적용. 기존 농기구/검 이미지로 모집 카드를 구성한다.
- 정식 웨이브, 주민 피격·사망, 게임오버 화면·진행 정지·재시작은 제외한다. 초소 GameOver는 시청 파괴 최종 패배 규칙을 대체하지 않는 임시 통합 규칙이다.

## 실행과 검증

Phase 1 순찰 → 2 피격 → 3 모집 → 4 UI → 5 배선/기능 → 6 이미지 → 7 최종 검증/독립 리뷰.
Phase별 보정 최대 2회. 반복 원인 또는 범위 확장은 중단·보고한다.
기존 FarmerScene.Structure v1은 농사 배선 회귀만 증명한다. 새 전용 하네스 프로필은 만들지 않는다.
컴파일/import, 변경 참조, diff --check, 기존 프로브와 실제 Play Mode, 독립 read-only 리뷰와 review-evidence를 사용한다.
실행하지 않은 검사는 NOT_VERIFIED로 남긴다. 열린 Editor의 미저장 작업을 버리거나 Editor를 임의 종료하지 않는다.

### 사용자 후속 지시

이번 작업은 새 오브젝트·팝업 계층도 YAML로 조립하도록 명시적으로 허용됐다.
일회성 Editor 조립 스크립트 대신 `.harness-runs/farmer-guard-20260919/assemble.py`를 사용한다.
Play Mode는 사용자가 생략하도록 지시했으므로 완료 gate에서 제외하고 기능 시나리오는 NOT_VERIFIED로 남긴다.
기존 interactive gate bridge는 호출 가능하나 새 에셋 import에는 열린 Editor의 Refresh가 필요하다.

## 스크립트 변경 기록

| 파일 | 생성·수정 이유 | 소유 책임 / 관련 문서 | 검증 결과 |
|---|---|---|---|
| `GuardPost.cs` | 초소 순찰 위치와 체력 추가 | Combat/Guard | C# compile 통과, Play Mode NOT_VERIFIED |
| `TemporaryGameOverReporter.cs` | 사망 로그를 시설 책임에서 분리 | Combat/Guard | C# compile 통과, 정확히 1회 runtime NOT_VERIFIED |
| `GuardAction.cs` | 같은 provider에서 반복 위치 조회·과이동 방지 | Combat/Guard | C# compile 통과, 풀 재사용 runtime NOT_VERIFIED |
| `GuardActionSelector.cs` | GuardStat 호환성·provider/첫 점 주입·불가 시 Idle | Combat/Guard | C# compile 통과 |
| `GuardStat.cs`, `GuardStatDefinition.cs` | 사용하지 않는 GuardRadius 제거 | Combat/Guard | 참조 검색 및 compile 통과 |
| `IGuardStatView.cs` | radius 제거 후 중복 계약 삭제, ICombatStatView 재사용 | Combat/Guard | 잔여 C# 참조 없음 |
| `GuardActionCost.cs` | 고정 4점 순찰 tuning 제거 | Combat/Guard | compile 통과 |
| `DestinationDecider.cs` | 사용할 수 없는 초소를 경비 후보에서 제외 | Decision Policy | compile 통과 |
| `TownHallRecruitment.cs` | 직군별 독립 모집과 disable 시 코루틴 정리 | Town Hall | compile 통과, transaction runtime NOT_VERIFIED |
| `RecruitmentStatus.cs` | UI 읽기 전용 직군 상태 제공 | Town Hall | compile 통과 |
| `TownHallPopup.cs` | 두 탭·모집 후 유지·의도 전달 | Town Hall | compile 통과, 실제 UI NOT_VERIFIED |
| `TownHallRecruitCard.cs` | 카드 표시·버튼 구독 lifecycle | Town Hall | compile 통과 |
| `TestTownHallRecruitProbe.cs` | 직군 지정 API와 초기 준비 상태에 맞게 이관 | TestOnly, Town Hall | compile 통과, 실행 생략 |
| `.harness-runs/farmer-guard-20260919/assemble.py` | 허용된 YAML 조립: 씬 오브젝트·팝업 재구성·설정 이관 | root-owned 일회성 조립 산출물 | 저장값·내부 참조 확인, Refresh 후 농사 배선 33 checks 통과 |

## 진행

- 기준 커밋: `1ce9e3ce6f3c216d8630c3aa0b42a24eed1ec5c4`, 시작 시 dirty 파일 없음.
- Unity Editor 실행 중, MCP 연결 인스턴스 없음. 사용자 허용에 따라 YAML로 조립했다. 기존 HarnessInteractiveGateBridge를 통한 저장 씬 검사 요청은 작동한다.
- Phase 1–5: 코드·배선 작성 및 C# 컴파일 완료. 네 scene/prefab 파일 내부 참조 확인 완료. Play Mode는 후속 지시로 생략했다.
- Phase 6: built-in imagegen으로 초소 1개 생성, RGBA 1254×1254, 외곽 alpha 0 및 여백 확인. 원본 픽셀을 보존하고 `guard-post-front.png`를 두 씬의 초소 sprite에 지연 적용했다. 이미지 import 설정은 정면 건물 참조의 PPU 150/Bilinear를 사용하며 단일 sprite다.
- Phase 7: preliminary FarmerScene.Structure는 새 스크립트 미import 상태에서 missing-scripts/providers.registration 2건 Fail. 사용자 Refresh 뒤 같은 33 checks가 Pass했다(`farmer-guard-imported`). 코드 우회 수정 없이 import로 해소됐다. 후보 보정 사용 0/2.
- 최종 고정 후보의 snapshot, FarmerScene.Structure, 독립 review 및 accept-review 결과는 `.harness-runs/farmer-guard-20260919/`에 기록한다. 이 구조 검사는 Guard 전투·모집 gameplay 통과를 의미하지 않는다.
