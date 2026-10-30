# 주거 생활

| 주 소유 코드 | 책임 |
|---|---|
| `Assets/Scripts/System/Action/HomeStayAction.cs` | 선택된 집까지 이동하고 계속 머물며 시간 비례 효과 적용 |

BaseNPCActionSelector의 선택적 ResidentHousingState overload가 최초 귀가와 업무 부재 시 귀가를 선택한다. 기존 3인수 경로는 유지한다. 각 role의 HasAvailableWork는 기존 provider/작업 가용성을 읽기만 하며 예약·작업 위치 난수·전투 target 확정 없이 판단한다. selector의 주기 hook은 집 체류 중 업무 가용성과 기존 욕구 판단을 재확인한다. WorkerNPC는 간격과 hook 결과에 따른 기존 queue 취소/재요청만 수행한다.

HomeStay 하나가 주입된 House provider와 NPCPathFollower로 귀가 이동 및 체류를 실행한다. 별도 Move 뒤에 실내 action을 반복하는 방식이 아니므로 변화가 없는 주기에는 실내 표시를 유지한다. 첫 도착에서 첫 귀가를 완료한다. 경로 실패·주택 해제·배정 변경은 기존 ReplanRequested/Stop/Clear로 경로와 실내 표시를 정리한다. action은 manager나 selector를 검색하지 않는다. ActionContext의 단일 provider 계약은 변경하지 않았다.

집에 머무는 동안 기본 허기·갈증 증가와 피로 회복에 단계 옵션의 회복 보너스를 합산한다. 외출 중에는 체류 효과를 적용하지 않는다. 주택 소유는 NPCDissatisfaction을 통해 해소된 원인의 회복 보너스만 제공하며 활성 원인의 증가를 상쇄하지 않는다.

집에서의 필요 판단은 DestinationDecider.DecideNeeds를 사용하며 role 업무와 Sleep 후보를 루트와 모든 look-ahead 단계에서 제외한다. Eat/Drink가 선택되면 밖으로 나가 기존 시설 행동을 실행한다. 업무가 생기면 기존 role 판단으로 돌아가며 그 판단에서 허기/갈증 해결 또는 업무 피로의 여관 회복을 선택할 수 있다. 피로가 높다는 이유만으로 집에서 업무 재판단을 막지 않는다. 업무 선택 경로의 여관 Sleep은 주거 설정이 연결된 주민에 한해 초당 회복으로 피로 0까지 실행하며 utility 예상 시간도 현재 피로/동일 회복 속도를 사용한다. 주거 미연결 기존 씬은 종전 2초 후 완전 회복과 기존 예상 시간을 유지한다.

변경 유형별 최소 확인 범위: 체류 실행은 HomeStayAction/ResidentHousingState와 Movement, 판단은 Selector_and_Queue/Decision_Policy 및 role selector, 여관은 Needs_Actions, queue는 NPC_Runtime, 불만 보너스는 NPC_Dissatisfaction을 읽는다. 자동 Play/화면 검증은 제외하며 입주·외출·업무 복귀·반복 pooling은 사람 QA다.
