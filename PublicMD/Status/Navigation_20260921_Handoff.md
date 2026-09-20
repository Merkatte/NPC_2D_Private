# Grid Navigation 작업 인계 — 2026-09-21

## 현재 상태

노드 그리드 A*와 indexed binary min heap, Farmer/Guard 시설 이동, FarmerTest 배선을 구현했다.
같은 농장/초소 지역 안의 seeded random 이동은 직접 이동한다. 전투 추적은 Direct다.
흙 1/잔디 3/혼합 2 비용은 경로 선호만 바꾸며 물/건물은 통행 불가다.
Play 중 타일·건물 변경 갱신과 주민 회피는 이번 범위 밖이다.

코드 소유권: [Navigation](../Systems/Navigation.md), [Movement](../Systems/NPC_Decision_and_Actions/Movement.md).
[구현 계획](../Plans/Grid_Navigation_Implementation_Plan.md),
[파일별 변경 보고](Verification/GridNavigation20260921/implementation-report.md),
[검증 보관본](Verification/GridNavigation20260921/README.md)을 함께 확인한다.

## 맥북에서 이어가기

1. 기존 clone은 `git pull --ff-only origin main`, 새 clone은 원격 저장소의 main을 받는다.
2. Unity Hub에서 프로젝트 루트를 열고 `ProjectSettings/ProjectVersion.txt`에 지정된
   Unity **6000.3.9f1**을 사용한다. Packages manifest/lock과 Assets의 meta가 함께 저장돼 있다.
3. 첫 import가 끝나면 `Assets/Scenes/FarmerTest.unity`를 연다.
4. Play에서 시설 사이 경로와 농장/초소 내부 이동을 확인한다.
   `Tools > NPC > Navigation > Run Checks`는 순수 탐색과 현재 씬 경로 질의를 실행한다.
   `TestOnly!!/NavigationProbe`에서 지정 지점 경로와 탐색 횟수도 확인할 수 있다.
5. 다음 코드 작업은 루트 AGENTS.md와 ProjectStructure의 관련 leaf routing부터 확인한다.
   로컬 SDK·실행 증거 경로의 Windows 절대 경로는 Mac에서 그대로 실행하지 않는다.
   하네스 사용법은 `Tools/NpcHarness/README.md`를 따른다.

Library/Temp/IDE 캐시와 미저장 Editor 메모리는 Git 전송 대상이 아니다.
저장된 코드·씬·에셋·프로젝트 설정·Packages·작업 규칙/문서는 포함된다.
이 작업은 Windows에서 검증됐으며 macOS import/Play는 새 환경에서 확인해야 한다.

## 검증 결과와 한계

Runtime/Editor C# 빌드 및 사용자 Unity 자동 컴파일: 오류 없음.
핵심 검사 42,931 assertion, tracked 순수 검사 1,446개, 이동 host 21개와 저장 맵 30개 경로 통과.
FarmerScene.Structure v1은 33개 구조 검사 통과, 독립 리뷰 Approve,
Harness.ReviewEvidence v1 Pass/exit 0, 검증 후 코드 수정 0/2회다.
사용자는 실제 Play에서 정상 이동을 확인했다. 풀 재사용·실패 분기는 host 검사와 코드 추적 근거이며
모든 분기를 native Play에서 별도 계측한 것으로 보고하지 않는다.

검증 이후 이 인계 문서와 PROGRESS, 보관본만 추가·갱신했다. gameplay 코드는 바뀌지 않았다.
