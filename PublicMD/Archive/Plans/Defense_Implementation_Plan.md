# 북쪽 성벽 방어 1차 — 승인 계획

2026-10-09 사용자가 대화에서 계획을 승인하고 구현을 요청했다. 상태: 구현 완료. strict Unity 컴파일·기존 비-Play 검사·참조 검사와 독립 리뷰 Approve를 확인했다. 실제 플레이·화면은 미검증이며 결과와 한계는 PublicMD/Status/PROGRESS.md를 따른다.

## 적용과 제외

BuildingTest를 기반으로 Assets/Scenes/DefenseTest.unity를 만든다. 기존 테스트 씬은 보존하며 방어 전용 prefab variants, controller와 설정을 사용한다. 메딕/치료 콘텐츠/자동 회복, 유닛 직접 명령, 원정 콘텐츠, 보스, 승리 조건은 제외한다. 코드는 기존 역할과 lifecycle을 확장하고 별도 AI 실행기를 만들지 않는다.

## 승인 규칙

- 북쪽에 구간별 HP를 가진 고정 성벽과 그 북쪽 적 생성 띠를 둔다. 양 끝 우회는 지형으로 막는다. 파괴 구간만 적이 통행할 수 있다. 아군은 지정된 자동 출입 지점을 통과한다. 성문 개폐 조작은 없다.
- 성벽은 대체 불가이며 잔해 청소 없이 같은 자리에서 재건한다. 재건 완료 시 통로 안에 유닛이 있으면 비워질 때까지 폐쇄를 보류한다.
- 검병은 기존 Guard enum을 유지한다. 근처 적 우선, 없으면 성벽 내부의 적, 없으면 북쪽 원래 경계 위치로 복귀한다.
- Archer는 enum 마지막에 추가한다. 궁병은 원래 성벽 자리에서 사격하고 자신의 구간이 붕괴하면 남쪽 지상 anchor에서 계속 정지 사격한다. 성벽 복구 시 원래 자리로 자동 복귀한다. 공격 목적 추격/회피는 없다.
- 검병과 궁병의 전투 대응은 태업/욕구/첫 귀가보다 우선한다. 안전해지면 기존 생활 규칙으로 돌아간다. 이동/식사/수면/집 체류 도중에도 긴급 재판단한다.
- 성벽 밖 적은 근처 검병 우선, 없으면 성벽을 공격한다. 접근 중 열린 틈이 생기면 도달 가능한 가까운 틈으로 간다. 이미 성벽 타격을 시작했으면 다른 틈 때문에 목표를 바꾸지 않는다. 검병 교전 우선은 유지한다.
- 내부 적은 공격 가능한 가장 가까운 건물 또는 유닛을 공격한다. 시청에 별도 우선순위를 부여하지 않는다.
- 근접 피해는 타격 시점에 한 번, 화살은 발사 후 도착 시 유효한 대상에 한 번 적용한다. 소실한 대상을 다른 대상으로 자동 전환하지 않는다.
- 아군 HP0은 Downed이며 untargetable이다. object, stat, housing, cargo와 배치 identity를 보존하며 queue를 정리하고 실행을 멈춘다. disable/despawn을 다운 처리로 사용하지 않는다. 기존 HP 소유자는 NPCStat이다. 자동 회복은 없다.
- 적 HP0은 제거한다. 시청 HP0은 한 번 GameOver를 확정하고 진행을 멈춘다. DefenseTest에서는 초소의 TemporaryGameOverReporter를 연결하지 않는다.
- 열린 성벽이 있거나 내부 적이 남아 있으면 경계 상태다. 모두 복구되고 내부 적도 없어야 평상시다. 비전투 주민은 경계 상태에서 자신의 감지 범위 안에 적이 보이면 도주하고, 주변에 적이 없어지면 selector를 통해 작업/생활을 다시 선택한다. 진행도와 cargo는 보존, lease는 반환 후 재예약한다.
- 도주 진입 시 기존 스타일의 짧은 자동 말풍선을 표시한다. 대사 선택/표시는 action/NPCComponent가 소유하지 않는다.
- 일반 건물은 손상 시 수리, HP0이면 기능 해제와 잔해 표시, 잔해 청소 후 Empty plot으로 복귀한다. 새 건설은 기존 비용/선택 절차다. 주택/창고/provider/공사 예약을 해당 owner를 통해 해제한다. 창고 용량 상실로 공용 재고를 임의 삭제하지 않는다.
- 업그레이드 중 파괴는 공사/예약을 종료하고 잔해로 전환한다. 파괴는 취소가 아니며 자동 환불하지 않는다.
- Builder 우선순위: 도주 > 기존 태업/긴급 생활 > 공격받는 성벽 수리 > 성벽 재건 > 건물 수리 > 잔해 청소 > 기존 건설/업그레이드. 같은 순위는 발생 순서다. 높은 우선 작업/도주로 중단해도 진척을 보존한다.
- 상주 시설/시청은 피해 대상, 빈 부지/신규 공사 부지/방문 상단은 제외다. 업그레이드 중 기존 건물은 포함한다.
- 궁병 모집 전에 빈 배치 슬롯을 예약한다. 슬롯이 차면 모집을 거부하고 이유를 표시한다. 파괴된 빈 구간에도 배정 가능하고 지상 anchor에서 시작한다. 다운된 궁병의 자리도 보존한다.

## 웨이브와 이벤트

- production spawn 경로를 만든다. 모든 예정 spawn 완료 AND alive enemy 0일 때만 웨이브 종료. 시작/종료 후 정비, 정비 중 다음 습격 버튼으로 조기 시작 가능.
- 이벤트는 ID/등장 조건/유효 시간/문구/선택지/결과 처리 연결을 공유 정의로 가진다. 웨이브 manager에 개별 콘텐츠를 하드코딩하지 않는다.
- 정비 중 알림만 있으면 시간 진행. 클릭해 선택 팝업을 열면 게임 전체 정지. 선택/닫기 시 재개. 미선택 닫기는 남은 시간 동안 재열기 가능. 시간 만료 또는 다음 웨이브 시작 시 알림 종료.
- pause owner 하나가 이유/lease를 관리한다. 이벤트 종료가 GameOver 정지를 해제하지 않는다. UI 입력은 정지 중에도 작동해야 한다. 확인/넘기기 샘플만 포함하며 실제 원정/보상은 제외한다.

## 수치와 데이터

기존 CSV는 보존한다. 새 전투/성벽/복구/웨이브/이벤트는 ScriptableObject 정의로 편집하며 runtime HP/타이머/점유를 공유 asset에 저장하지 않는다.

| 항목 | 임시 기본값 |
|---|---|
| 검병/적 | 기존 Guard/Enemy stat 복사 |
| 궁병 | HP80, damage10, range6, interval1.5초 |
| 성벽/일반 건물/시청 | HP500/200/1000 |
| 수리/성벽 재건/잔해 제거 | 20HP/초, 15초, 8초 |
| 복구 비용 | 기본 빈 비용 목록(0), 설정 가능 |
| 웨이브 | 적5/8/12, spawn interval1초, rest60초, 마지막 편성 반복 |
| 이벤트 유효 시간 | 30초 |

밸런스 최종 확정, 보상, 자동 난이도 증가, 승리 조건은 제외다. 테스트 초기 구성은 기존 BuildingTest 자원/시설/건축가를 보존하고 검병2/궁병2를 추가한다. 성벽은 전체 지도 폭에 걸친 균등 구간이며 최소 4개의 궁병 자리를 확보한다.

## 책임과 연결 계약

- DefenseBattlefield: 등록/내외부/성벽/슬롯/후보 조회. selector: role target policy와 queue. WorkerNPC: 공통 실행/취소만.
- NPCStat: 주민 HP. 별도 downed 상태/adapter는 생명주기와 presentation을 연결한다. building/wall durability는 scene runtime owner 하나만 사용한다.
- Navigation: dynamic obstacles, faction/access와 revision. NPCPathFollower: revision/이동 target에 따른 경로 갱신. 기존 Direct 추적으로 성벽을 통과하지 않는다.
- Maintenance provider/registry: repair/rebuild/clear의 lease/진척/transaction. action은 주입된 provider의 이동/작업/반환만 한다. action에서 manager/search/다음 행동 선택 금지.
- UI는 domain의 조회/명령만 소비한다. pause/gameover/wave/event current state는 각각의 scene owner가 소유한다.
- 공통 CSV/enum의 기존 값/참조/GUID를 보존한다. 새 NPCType.Archer=5. 방어 미연결 기존 씬의 행동을 보존한다.

## 에셋과 조립

기존 NPC body/guard-sword와 기존 따뜻한 정면 목재·석조 건물 스타일을 재사용한다. 신규 bow/arrow, wall intact/damaged/destroyed, building rubble. 검 베기/활 당김·발사/쓰러짐 표현과 이동 중 무기를 연결한다. NPCComponent는 표현만, 피해 timing은 action 실행이 소유한다.

각 wall root는 visual/target/blocker/top/ground/work/attack anchors를 유지한다. DefenseTest에 북쪽 지면, guard anchors, friendly passage, spawn 띠와 camera bounds를 추가한다. 기존 클릭 collider와 피격 layer를 혼합하지 않는다. 새 Assets/Prefab/Defense, Assets/Data/ScriptableObject/Defense, Assets/Animation/Defense를 사용한다.

## 절차와 검증

코드/graphics 병렬, assembly 준비 병행. 코드/art 준비+컴파일 후 import/조립/저장을 순차 실행한다. helpers는 코드 담당 작성, assembly 담당 실행. root는 공통 문서/기록/실제 diff/검사 확인 담당이다.

Unity 컴파일, 기존 건설/자원/경로/메시지 비-Play 검사, 참조/enum/범위/diff 검사와 독립 읽기 전용 C# 리뷰를 필수로 한다. 새 검증 전용 코드/runner/harness와 자동 Play/게임 화면 검증은 하지 않는다. 런타임과 화면은 사람 QA로 NOT_VERIFIED를 남긴다.

사람 QA: 접근중/타격중 breach 분기, 검병 내부지원·복귀, 궁병 붕괴후 사격·복귀, 도주·예약·cargo 보존, 다운 untargetability, 시설 파괴·청소·재건·용량/입주 해제, wave spawn pending, 이벤트 시간정지·만료, 재건 통로 점유, 시청 GameOver 한번.
