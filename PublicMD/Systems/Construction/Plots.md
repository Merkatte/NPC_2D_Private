# Construction Plots

## 책임과 흐름

BuildingPlot은 씬 부지와 상태 변경 진입점, ConstructionState는 해당 공사의 runtime 상태, registry는 부지 후보와 신청 순서를 소유한다. selector가 공사를 고르고 예약하며 provider는 작업 요청을 검증한다. 검증 상태는 PROGRESS를 따른다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| Assets/Scripts/Actor/BuildingPlot.cs | 건설/취소/예약/작업/완공 재시도와 읽기 전용 표시 상태 |
| Assets/Scripts/Manager/BuildingPlotRegistry.cs | 명시적으로 연결한 부지 후보와 신청 순번 |
| Assets/Scripts/System/Construction/ConstructionState.cs | 정의/지불 사본/진행도/시작 이력/예약/완공 실패/업그레이드 구분 |
| Assets/Scripts/System/Construction/ConstructionReservation.cs | 공사별 예약 식별과 반환 수명; provider가 아님 |
| Assets/Scripts/Enum/BuildingPlotState.cs | Empty/UnderConstruction/Completed/Rubble |
| Assets/Scripts/Enum/ConstructionReservationStatus.cs | 예약의 활성/종료 사유 구분 |

## 최소 확인 범위

상태/지불/환불은 Plot+State와 ResourceManager, 예약은 Reservation과 InteractionRequest 및 Builder, 조립 실패는 [Facilities](Facilities.md), 팝업은 [UI](UI.md), 공사 이미지·애니메이션은 [Presentation](Presentation.md)를 읽는다. Factory는 비용/진행도를 변경하지 않는다. Plot은 표시 오브젝트를 직접 제어하지 않고 StateChanged를 발행한다.

## 불변 규칙

- 외부는 State를 수정하지 않고 Plot API를 호출한다. 상태 검사+재진입 방지가 중복 지불/환불/완공을 차단한다.
- 실제 지불한 비용 사본으로 환불한다. 첫 양수 작업 전 전액, 이후 floor(paid×남은 진행률). 용량 부족으로 환불을 버리지 않는다.
- 이동 중 예약도 정원을 점유한다. 매 작업 요청에서 부지/공사/자리 유효성을 검증한다. 과거 예약은 같은 부지의 새 공사에 영향을 주지 않는다.
- 예약 인계 전 실패는 selector, 이후 모든 종료는 BuildAction의 책임이다. 반복 반환은 안전하다.
- 100% 시설 생성 실패는 공사 상태/진행도 보존, 예약 무효화/추가 작업 금지, 명시적 무비용 재시도다.
- 빈부지부터 footprint와 외곽 작업 위치를 고정한다. 취소/완공으로 navigation topology를 바꾸지 않는다.

## 배선·검증·제약

씬에서 registry와 각 plot의 data/resources/factory, 고정 entrance/workPositions/workArea를 명시 연결한다. 기본 허용 목록은 비워 모든 정의를 허용한다. 부지당 두 작업점을 두고 정의의 정원이 실제 작업점 수를 넘으면 시작을 거부한다. 부지6개와 초기 조건은 BuildingTest/TestOnly 설정이다.

BuildingTest의 오른쪽 테스트 지면에 3×2로 배치한다. 부지 입구는 x36.5/47.5/58.5와 y−10.5/−0.5 조합이며 원래 지형은 유지한다. footprint와 외곽 작업 영역은 각각 9×6, 9×2다. 기존 click mask에 포함된 layer7을 사용한다. TestOnly 카메라는 방향키로 이 구역까지 이동할 수 있다.

예약 경쟁/과거 예약/반복 Dispose, 이동중 취소·버림 환불, 2인 합산, 생성 실패/재시도가 비-Play 위험 검사다. 실제 이동·망치·욕구 복귀는 사람 확인이며 자동 Play는 제외한다. 수리/잔해 처리는 별도 Defense owner가 담당한다. 상태/API/직렬화가 바뀌면 갱신한다.

## 주택 업그레이드

TryStartUpgrade는 Completed 상태의 기존 주택에 대해 다음 단계 정의를 조회하고 비용 선불, 새 공사 ID/FIFO 순번, 기존 예약/BuildAction 흐름을 사용한다. IsUpgrade 공사 동안 CompletedFacility와 입주/기존 효과는 유지한다. 완료는 Factory.TryUpgrade로 기존 객체의 단계·효과·sprite를 적용한다. 실패는 100%와 기존 주택을 보존하고 TryRetryCompletion으로 재시도한다. 취소는 환불 성공 후 예약을 종료하고 Completed로 돌아간다.

IsAllowed와 Factory.CanStartNewConstruction은 빈 부지의 주택 건설을 카탈로그 1단계로 제한한다. 업그레이드 때 Plot의 Construction 클릭 source는 비활성화되고 HousePopupSource가 기존 집 창을 연다. ConstructionVisual은 기존 주택을 숨기지 않고 공사 레이어만 추가하며 취소의 Completed 복귀에서 빈 부지 표시를 켜지 않는다.

## Defense 시설 파괴

`_initialFacility`는 scene-native 완공 시설을 Start에서 adopt하는 선택적 참조다. OnFacilityDestroyed(expectedFacility)는 같은 시설인지 검증하고 진행 중 공사/업그레이드 예약을 환불 없이 끝낸 뒤 Rubble 상태를 발행한다. ClearDestroyedFacility만 시설 참조를 제거하고 Empty로 되돌린다. Rubble은 Construction popup/신축을 허용하지 않는다. 잔해 progress·HP·기능 중단은 [Defense durability](../Defense/Durability_and_Maintenance.md)가 소유한다.
