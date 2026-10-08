# Housing

주택의 공유 정의, 입주, 생활 효과와 주택 창을 나누어 소유한다. 2026-10-08 승인 구현을 BuildingTest에 연결했으며 검사와 남은 사람 QA는 [PROGRESS](../../Status/PROGRESS.md)를 따른다.

| 변경 대상 | 주 소유 문서 | 함께 확인 |
|---|---|---|
| 단계/복수 옵션/생활 tuning CSV | [Definitions](Definitions.md) | Construction/Definitions |
| 주택 정원/입주/퇴거/배정 순서 | [Occupancy](Occupancy.md) | Spawning_and_Pooling, NPC_Runtime |
| 집 체류/여관 회복/불만 효과 | [Life](Life.md) | NPC_Decision_and_Actions/Needs_Actions, NPC_Dissatisfaction |
| 거주민/효과/업그레이드 표시 | [UI](UI.md) | UI, Construction/UI |

NPCManager 주민 목록 → HousingManager 배정 조정 → HousingAssignmentPolicy 순서/집 선택 → House와 ResidentHousingState 입주 확정 → selector 행동 선택 → action 실행.

공사·비용·예약·환불은 [Construction](../Construction/README.md)이 계속 소유한다. UI는 source와 domain 요청을 사용하며 스스로 재료/단계를 변경하지 않는다. 구체 파일 목록은 leaf에서만 관리한다.
