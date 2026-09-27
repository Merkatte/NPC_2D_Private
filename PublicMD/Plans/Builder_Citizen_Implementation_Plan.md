# 건축가 시민 1차 구현 계획

승인: 사용자의 `Implement the plan.` 지시. 구현 방식은 `$orchestrate-unity-work`이며 NPC 기능의 계획·승인·구현 단계를 분리한다.
현재 상태: 코드·망치 이미지에 이어 Unity CLI로 실제 배선 적용(2026-09-26 사용자 재개 요청). 자동 플레이·화면 검증은 제외하고 구조·참조 검사 및 독립 리뷰 결과를 PROGRESS에 기록한다.

## Approved scope and acceptance

- 기존 NPCGirl 몸체에 나무 손잡이·금속 머리의 대형 망치를 든 전용 Builder 직업을 추가한다.
- 시청에 농부·경비병과 함께 세 번째 모집 카드를 둔다. 초기 모집 가능, 100골드, 건축가의 독립 쿨다운 60초이며 기존 낙하·기립 연출을 재사용한다.
- 도달 가능한 마을 전체를 배회하며 기존 navigation의 물·건물 차단 규칙을 따른다. 최대 5초 이동 후 1초 쉬고 재판단한다.
- 배회 중 허기·갈증·피로는 각각 초당 0.3 증가한다. 기존 생활 판단과 Eat/Drink/Sleep 실행을 사용하고 욕구 해소 뒤 배회로 복귀한다.
- 초기 stat은 Farmer의 DefaultStatContext를 독립 BuilderStatContext 에셋으로 복제한다. 기존 직업의 수치나 공유 에셋은 바꾸지 않는다.
- 새 enum 값은 끝에 추가한다. 기존 농부·경비병과 pool 재사용 시 도구·행동·대상이 섞이지 않아야 한다.

## 책임과 구현 순서

1. `BuilderActionSelector`가 생활 판단 및 배회 queue를 구성한다. `DestinationDecider`의 직업 중립 판단을 재사용한다.
2. `WanderAction`이 경로 추종·이동 제한시간·휴식·욕구 변화·종료/반환 정리를 소유한다. `WanderActionCost`는 공유 수치만 보관한다.
3. `TilemapNavigation`이 주입된 난수로 도달 가능한 위치를 제한된 횟수 내 조회한다. 기존 `INavigationService`와 경로 추종 계약은 유지한다.
4. `NPCComponent`가 직업별 도구 sprite와 초기화를 소유하고 `WorkerNPC`는 초기화 시 직업만 전달한다.
5. 시청의 기존 모집 transaction과 NPCManager의 selector/stat entry를 연결한다. UI는 설정된 카드 목록을 사용한다.
6. `$create-project-sprites`로 만든 망치 후보를 확인하고, Unity Editor API로 import·SO·prefab·FarmerTest를 조립한다.

주 소유 문서: [Builder](../Systems/Builder.md). 공통 책임은 Navigation, NPC Presentation, Town Hall, Spawning and Pooling 문서를 따른다.

## 적용과 검증

Unity에서 저장을 마친 Edit Mode에 `Tools > NPC > Builder > Setup`을 실행한다. 이는 지정된 망치 importer, Builder stat/cost, NPCGirl·TownHall·TownHallPopup prefab과 FarmerTest만 변경한다. 열린 dirty scene/asset 또는 Prefab Stage가 있으면 중단하며 임의로 저장하지 않는다. 자동 import 시에는 실행되지 않는다.

Unity를 닫은 상태에서는 원본 프로젝트에 CLI `-batchmode -quit -projectPath <project> -executeMethod BuilderCitizenSetup.Setup -logFile <log>`을 사용할 수 있다. 열린 프로젝트에 두 번째 Editor를 띄우지 않는다.

2026-09-26 실제 적용은 열린 FarmerTest의 idle/clean 상태를 확인한 뒤 공식 Unity CLI의 `command --project-path <project> eval_file <script> ... --json`으로 `BuilderCitizenSetup.Setup()`을 호출했다. 씬과 NPCGirl·TownHall·TownHallPopup, 망치 importer 및 두 SO 에셋에 저장했다. `.harness-runs/builder-cli-wiring-20260926/`에 원본 백업과 적용·참조 확인 증거를 보관한다.

필수 확인:

- 런타임·Editor 컴파일 및 Unity import 오류, 실제 serialized 참조와 누락 스크립트.
- 기존 FarmerScene.Structure v1, 독립 후보 리뷰와 Harness.ReviewEvidence 검증. 후보 수정 예산 2회.

사람의 확인 항목(자동 실행·스크린샷 검증 제외, 미실행만으로 구현 완료를 막지 않음):

- 세 직업 모집·골드 부족·연타·독립 쿨다운·낙하·기립.
- 건축가의 물/건물 우회, 휴식, 생활시설 방문과 배회 복귀.
- 망치 손잡이 위치·크기·좌우 이동, Farmer/Builder/Guard 반복 pool 재사용.

컴파일·구조 검사 통과만으로 위 플레이 항목을 통과 처리하지 않는다. 이전 Unity 적용 보류는 사용자의 CLI 배선 적용 요청으로 해제됐으며, 플레이·화면 확인은 미검증으로 남긴다.

## 제외 범위와 후속 건축 시스템

이번에는 실제 건축·수리·공사장 예약·배치 UI·교육·전직·전투·동적 navigation 갱신을 구현하지 않는다.

2026-09-27 후속 대화에서 지정 부지 클릭, 시설 선택, 즉시 비용 차감, 공사 취소·환불, 신청 순서·작업량 합산과 생활 중단 규칙을 확정했다. 이전 우측 하단 건축 버튼 중심 흐름은 대체한다. 최신 확정 규칙과 남은 TBD의 단일 기준은 [건물 건설 컨텍스트](Building_Construction_Context.md)다. 현재 문서의 건축가 시민 1차 구현 범위와 완료 상태를 확대하지 않는다.

무직 주민 모집과 교육 후 도구 지급도 별도 후속 범위다.
