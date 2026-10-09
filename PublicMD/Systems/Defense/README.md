# Defense

DefenseTest의 성벽 방어 확장은 기존 NPC queue·stat·navigation·시설 transaction 위에 선택적으로 연결한다. 기존 BuildingTest와 GuardTest는 기존 selector와 자산을 유지한다.

| 변경 | 먼저 읽을 leaf |
|---|---|
| 검객·궁수·적의 대상 선택, 전투 interrupt, Downed, 도주, 궁수 자리 | [Battlefield and Response](Battlefield_and_Response.md) |
| 성벽·시설 HP, 통로 개폐, 수리·재건·잔해 제거와 건축가 예약 | [Durability and Maintenance](Durability_and_Maintenance.md) |
| 공격·쓰러짐 자세, 자동 도주 말풍선 | [Presentation](Presentation.md) |
| 웨이브, 사건, pause/game-over, HUD와 전용 씬 조립 | [Progression](Progression.md) |

흐름은 Battlefield 등록 → selector의 DefenseResponsePolicy 긴급 판단 → 기존 WorkerNPC queue 실행 → health owner 변경 → Downed/벽 통로/시설 잔해 변경이다. 벽 topology는 [Navigation](../Navigation.md)의 revision을 통해 진행 중 경로에 반영한다. 공유 부지·신축·주택 수명은 [Construction](../Construction/README.md)과 [Housing](../Housing/README.md), HP adapter는 [Combat](../Combat/README.md)가 소유한다.

밸런스·완료 조건은 [승인 계획](../../Archive/Plans/Defense_Implementation_Plan.md), 검사 결과와 미검증 사항은 [PROGRESS](../../Status/PROGRESS.md)를 따른다. 자동 Play Mode나 화면 검증은 실행하지 않는다.
