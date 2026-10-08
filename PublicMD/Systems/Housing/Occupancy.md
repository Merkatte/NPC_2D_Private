# 입주와 주택 수명

NPCManager는 커밋된 주민의 등록/해제와 읽기 전용 Residents 목록을 소유한다. HousingManager는 scene의 집 목록과 입주 조정을 소유하며 NPCManager와 navigation을 명시적으로 받는다. House는 단계·정원·입주자를, ResidentHousingState는 한 주민의 배정/실내 체류/첫 귀가 상태를 소유한다.

| 주 소유 코드 | 책임 |
|---|---|
| `Assets/Scripts/Actor/House.cs` | 검증된 단계 적용, 정원·입주자와 주거 provider |
| `Assets/Scripts/Manager/HousingManager.cs` | 주택 등록과 주민 배정 조정 |
| `Assets/Scripts/System/Actor/HousingAssignmentPolicy.cs` | 불만도/등록 순서 정렬과 도달 가능한 최근접 집 선택 |
| `Assets/Scripts/System/Actor/ResidentHousingState.cs` | 주민별 주택/체류 상태와 효과 읽기 |

House는 TryConfigure에서 canonical 단계, manager와 entrance를 확인하고 활성화 시 등록, 비활성화 시 해제한다. 등록 해제는 기존 주민을 무주택으로 돌린 뒤 빈 집 배정을 다시 시도한다. 무주택자는 현재 총불만도 내림차순, 동률 주민 등록 순서로 처리한다. 여유 정원이 있고 navigation 경로가 성공하는 집 중 출입구 제곱거리가 최소인 집에 이동 전에 입주를 확정한다. 동거리 집은 등록 순서를 유지한다. 기존 입주자의 자동 이사는 없다.

NPCManager는 예약 커밋에서만 ResidentHousingState를 만들고 selector 호출 전 등록한다. 예약 취소에는 주민 등록이나 방 배정이 없다. worker disable은 등록 목록·주택 점유를 해제하고 pooled runtime 연결을 정리한다. 첫 배정은 첫 HomeStay 도착을 요구하므로 바로 업무로 전환하지 않는다. 집의 Changed와 NPCManager의 ResidentsChanged는 확정 상태를 알리고 구독자 예외를 격리한다.

TryApplyTier는 같은 catalog의 바로 다음 단계와 비감소 정원을 검증한다. 실패는 상태·알림을 변경하지 않는다. 완공 시설이 sprite를 먼저 준비/설정하고 domain 단계 적용 실패 시 그림을 되돌리므로 Changed 시점에는 새 그림과 새 단계가 모두 적용되어 있다. 업그레이드 중 기존 객체·입주·효과는 유지된다. House는 BuildingPlot/UI나 WorkerNPC 메서드를 호출하지 않는다.

무주택 원인은 입주 여부가 결정하며 여관 체류로 해소되지 않는다. 해소된 원인에 대한 집의 불만 회복 보너스는 외출해도 유지한다. 실제 체류 효과는 [Life](Life.md), 건설 업그레이드와 화면은 Construction/UI 소유다.

변경 유형별 최소 확인 범위: 입주/정원은 위 네 코드와 NPCManager, 생성/해제는 Spawning_and_Pooling 및 WorkerNPC, 경로 선택은 Navigation을 읽는다. 등록·배정·원인·공개 계약 변경 시 이 문서를 갱신한다.
