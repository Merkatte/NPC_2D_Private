# NPC

NPC 개체의 공통 수명, 행동 판단·실행과 표현을 연결한다. 직업별 생산·건설·전투 규칙은 해당 domain이 소유한다.

| 변경 대상 | 하위 기능 |
|---|---|
| 생성·반환, queue 소비, 공통 stat, 불만도 | [Runtime](Runtime/README.md) |
| utility, selector, action 계약, 이동·생활 실행 | [Decision and Actions](Decision_and_Actions/README.md) |
| 이동·도구·운반물 표현, 생각·지정 대사 | [Presentation](Presentation/README.md) |

생성 조립 → 개체 초기화 → selector의 queue 구성 → runtime의 action 실행 → adapter 표현 순서로 연결한다. 표현과 메시지는 현재 상태를 소비하며 다음 업무를 선택하지 않는다.

직업별 작업은 [Economy](../Economy/README.md), [Settlement](../Settlement/README.md), [Combat](../Combat/README.md), 경로·시설 조회는 [World](../World/README.md)를 따른다. 하위 책임이나 연결이 바뀌면 이 인덱스를 갱신한다.
