# 군사훈련소

DefenseTest의 군사훈련소는 현재 직업이 검병 또는 궁병인 주민을 대상으로 한다. [건설](../../Settlement/Construction/README.md)이 착공·비용·부지·완공을, [직업 성장](../../NPC/Runtime/Profession_Growth.md)이 개인 숙련도와 실전 성장 상한을 소유한다.

## 시설과 병사

플레이어가 한 명을 선택하면 시설이 자리를 예약한다. 이동 중에는 숙련도를 지급하지 않는다. 도착한 뒤에는 실제 게임 시간에 비례해 30초 동안 최대 20을 지급하며 현재 직업 Lv.2까지의 잔여량으로 제한한다. 회차 종료·상한 도달·수동 중단 시 자리를 해제하고 기존 병사 판단을 재개한다. 습격 자체는 배정 해제 조건에 포함되지 않는다.

## 조작

빈 시설의 팝업에서 후보·직업·레벨을 확인하고 배정한다. 점유 중에는 훈련생, 이동 또는 훈련 상태, 남은 시간과 중단 버튼을 표시한다. 상한에 도달한 후보는 제한 이유를 표시한다.

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 |
|---|---|
| 배정·중단·시간 지급 | 시설, 훈련 행동, 병사 selector, 개인 직업 성장 |
| 습격·궁병 하강·쓰러짐 | [방어 반응](Battlefield_and_Response.md), 병사 행동과 시설 예약 |
| 후보 조회·입력 | MilitaryTrainingPopup, 시설 클릭 source, DefenseTest 팝업 등록 |
| 한 채 제한·비용·재건설 | [부지](../../Settlement/Construction/Plots.md), [완공 시설](../../Settlement/Construction/Facilities.md), 건설 정의와 비용 CSV |

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| MilitaryTrainingFacility.cs | 훈련소 점유, 후보 검증, 실제 훈련 시간, 훈련 중단·해제 |
| MilitaryTrainingSettings.cs | 공유 회차 시간, 숙련도, 훈련 상한 |
| MilitaryTrainingCandidate.cs | 배정 후보와 현재 직업·레벨·주민 수명 식별 |
| MilitaryTrainingAction.cs | 도착 후 훈련 실행과 action 반환 시 정리 |
| MilitaryTrainingPopup.cs | 클릭한 시설의 후보·상태 표시, 배정·중단 입력 |

후보는 전장에 등록된 해당 주민의 번호로 구분한다. 팝업은 명단이 바뀌어도 같은 병사와 수명에 대한 선택을 유지하고, 선택한 병사가 사라진 뒤에는 플레이어가 다시 선택해야 한다.

훈련 시간 만료와 훈련에 의한 상한 도달은 해당 병사의 표시 번호를 담아 [공통 알림 HUD](../../UI/Views/Notifications.md)에 알린다. 수동 중단·도착 실패·쓰러짐·시설 파괴는 완료 알림을 발행하지 않는다.

TrainingCompletionNotificationBinding.cs는 전장의 TrainingCompleted를 구독해 병사 번호를 지역화된 훈련 완료 문구로 전달하는 UI 연결부다. 이 파일의 주 소유 문서는 이 leaf다.
