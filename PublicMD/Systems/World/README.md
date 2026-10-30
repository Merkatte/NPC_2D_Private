# World

NPC와 시설이 공유하는 공간 질의와 목적지·상호작용 계약을 안내한다. 어느 시설을 선택할지는 판단 기능, 선택된 시설의 transaction은 각 domain이 소유한다.

| 변경 대상 | leaf |
|---|---|
| 지형 그리드·A*·지역 출입구·장애물·통행 revision | [Navigation](Navigation.md) |
| 목적지·provider 등록/조회, option·request·result, 실내 action 공통 계약 | [Interaction and Destinations](Interaction_and_Destinations.md) |

selector가 등록된 시설 후보와 capability를 읽어 행동을 선택하고, 이동 action이 navigation으로 경로를 구한 뒤 주입된 provider를 실행한다. 경로 탐색은 시설 transaction이나 다음 목적지를 선택하지 않는다.

이동 실행은 [Movement](../NPC/Decision_and_Actions/Movement.md), 시설 조립은 [Construction](../Settlement/Construction/README.md)을 따른다. 두 질의 체계의 책임·연결이 바뀌면 이 인덱스를 갱신한다.
