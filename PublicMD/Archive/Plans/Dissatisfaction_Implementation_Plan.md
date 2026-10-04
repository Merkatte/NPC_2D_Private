# 불만도·태업 1차 구현 계획

승인: 2026-10-03, 사용자가 Plan Mode의 계획 승인 후 구현을 요청했다.
구현 완료: 2026-10-03. 컴파일·기존 검사·독립 리뷰를 통과했다. 실제 플레이/화면은 미검증이며 현재 결과는 `../../Status/PROGRESS.md`를 따른다.

## 승인 범위

- Farmer, Builder, Guard의 불만도·태업을 구현한다. Enemy는 제외한다.
- 원인별 누적/회복, 개별 유닛의 증가 속도·감소 속도·상한·태업 기준을 지원한다.
- 태업은 농사·수확·운반·건설·순찰·전투 및 업무 이동을 중단한다. 생활 행동과 배회는 허용한다.
- 기존 Hover 말풍선에 일반 태업 문구를 표시한다.
- 주택, 입주, 귀가, 무주택 자동 판정/대사, 쾌적도 효과, 건물 레벨/업그레이드/스프라이트, 랜덤 유닛 생성은 후속이다. 이번에는 기존 주민에게 불만 원인을 자동 등록하지 않는다.

## 규칙과 초기 설정

- 조정 가능한 임시 시작값: 증가 1/초, 감소 1/초, 총 상한 100, 태업 기준 60. 최종 밸런스는 미확정이다.
- 각 활성 원인은 증가 속도로 누적하고 해소된 원인은 감소 속도로 회복한다. 반복 등록은 중복 적용하지 않으며 회복 중 재발하면 남은 값에서 증가한다.
- 총 불만도는 원인별 누적량의 합이다. 감소를 먼저 반영하고 총 상한까지 남은 증가 여유를 활성 원인에 균등 배분한다. 상한 초과분을 숨겨서 저장하지 않는다.
- 총량 >= 기준이면 태업, 총량 < 기준이면 복귀 가능하다. 별도 복귀 임계값은 추가하지 않는다.
- 초기값은 0/원인 없음이다. 게임 시간으로 갱신하고 일시정지·미초기화·비활성 중에는 진행하지 않는다.
- 속도 >= 0, 상한 > 0, 0 < 기준 <= 상한을 Inspector와 runtime 경계에서 검증한다.
- SO 초기 설정을 각 runtime stat에 복사하고 공유 SO를 runtime에서 변경하지 않는다.

## 책임과 계약

- `DissatisfactionSettings`: 네 설정값을 전달하는 값 타입.
- `DissatisfactionState`: 원인별 활성/누적량, 회복, 합산, 태업 판정. `NPCStat`이 인스턴스별로 소유한다.
- `NPCDissatisfaction`: 주입된 상태의 시간 갱신과 `TrySetCauseActive(cause, active)` 진입점. 시설/주택 검색이나 행동 선택은 하지 않는다.
- `WorkerNPC`: Init/disable 시 연결·정리만 담당하며 불만 계산·업무 선택은 추가하지 않는다.
- `IStatView`: 현재 불만도·상한·태업 여부의 읽기 계약. 기존 생성자/검사 대역의 호환성은 유지한다.
- 각 selector는 태업을 업무보다 먼저 판정한다. Guard는 전투 target을 정리하고 신규 전투를 선택하지 않는다.
- `DestinationDecider`는 태업 중 role activity를 look-ahead에서도 제외한다. 기존 생활 utility와 critical-need 판단을 재사용한다.
- 공통 selector 보조 메서드로 생활/배회 queue를 만든다. 선택된 생활 행동을 수행하고, 해결 불가한 critical need는 timed Idle, 나머지는 Wander다.
- `ActionContext`에 업무 가능 상태를 요구하는지 명시한다. 일하러 가는 Move와 생활 Move를 구분한다. Enemy Attack은 업무 제한을 설정하지 않는다.
- 공통 action 보조 로직을 시작/Tick에서 재사용해 업무 효과 이전에 ReplanRequested를 반환한다. 기존 Stop/Clear로 예약·도구·이동 상태를 정리하고 미완료 action을 성공 처리하지 않는다.
- 확정된 생산물·cargo·공사 진행률은 보존하고 건설 lease는 반환한다.
- 회복 뒤 다음 행동 선택에서 업무로 복귀한다. 생활 행동과 기존 Wander 한 구간(최대 이동 5초+휴식 1초)은 끝낸다.
- WanderAction/Cost를 재사용하고 현재 욕구 비용을 유지한다. 배회의 주 소유 문서를 Needs Actions로 옮기고 Builder는 링크한다.
- 메시지: 외부 지정 대사 > 태업 문구 > 평소 생각. 기존 4초 유지·연속 중복 방지·전용 난수·Hover 표시를 유지한다. 초기 문구는 '지금은 일할 기분이 아니야.', '불만부터 해결해 줘!', '당분간 일은 안 할래.'다. CSV/기존 생성기로 안정적 LocalizeKey를 추가한다. 새 HUD/자동 표시 없음.

## 배선과 역할

- NPCGirl에 불만도 component와 WorkerNPC lifecycle reference를 연결한다. DefaultStatContext, BuilderStatContext, GuardStatDefinition에 초기 설정을 명시한다.
- BuildingTest/FarmerTest의 주민 selector에 기존 navigation, Wander cost, 명시적 난수원을 연결한다. 지형/건물 배치는 보존한다.
- GuardTest의 기존 Direct 이동은 유지한다. 경로망이 없어 태업 배회가 불가능하면 생활 행동 또는 timed Idle로 안전하게 처리한다.
- code: 별도 author-unity-code 작업자, C#/신규 script meta/CSV/주 소유 기능 문서.
- assembly: 별도 assemble-unity-objects 작업자, 승인된 prefab/scene/SO. 코드 및 컴파일 준비 전에는 조사만 한다.
- root: 계획·범위·공통 문서·작업 기록·실제 diff와 검증 증거·최종 보고.
- 독립 reviewing-unity-candidate 작업자는 현재 검사 성공 후 read-only 리뷰한다. 작성자와 겸임하지 않는다.
- 코드 컨벤션/의존 방향/소유권을 준수하고 production C#은 하나의 주 소유 leaf만 가진다. 검증 도구 전용 코드를 작성하지 않는다.

## 검사와 수용 기준

- runtime/Editor 컴파일, 관련 기존 비-Play 검사(메시지/지역화와 영향받는 기존 검사), diff --check, 참조/금지 의존성/직렬화 값·GUID 확인.
- 기존 테스트의 assertion은 유지한다. API 호환성 이관만 허용하고 새 테스트/runner/eval helper/validator는 만들지 않는다.
- 검토 시나리오: 원인 없음/누적/해소/재발/중복/다중 원인/상한/기준 경계/주민별 독립성; 업무 중단과 cargo·진행률 보존/예약 반환; 생활 이동·배회·회복·경비·Enemy; init/disable/pool 재사용; 말풍선/지정 대사/기존 동작 회귀.
- 자동 Play Mode 및 게임 화면 검증은 제외한다. 실제 runtime/시각 확인을 하지 않은 항목은 NOT_VERIFIED로 인계한다.
- 현재 성공한 compile/check 증거와 tracked/untracked 실제 diff를 독립 리뷰어에게 제공하고 컨벤션·소유권·의존 방향·수명 검토의 실제 승인 결과를 기다린다.
- 하나의 작업 기록과 PROGRESS에 실제 결과를 기록한다. 주택 연동 전 자연 발생 원인이 없다는 한계를 명시한다.
