# Grid Navigation 구현 계획

사용자가 승인한 범위: 노드 그리드 A*와 indexed binary min heap을 사용한다.
흙 1/잔디 3/혼합 2, 물과 건물 통행 불가, 8방향과 corner-cut 금지다.
농장/초소 내부 seeded random 이동은 직선이고 지역 밖 시설 이동에만 A*를 사용한다.
전투 Direct 추적, provider 난수와 transaction, NPC 이동 adapter는 유지한다.
씬 snapshot은 Play 시작 시 생성하며 동적 지형 갱신/NPC 회피/JPS/NavMesh는 제외한다.

기준 commit: `71f3ff51bc66f8fb5d71fa022c2ebec55b63347e`, 시작 시 미커밋 변경 없음.
새 production C# 9개, 기존 C# 6개, TestOnly 2개, profile asset 및 FarmerTest 배선이 대상이다.
파일별 책임은 Navigation/Movement 문서, 실제 추가/삭제량은 실행 기록의 변경 목록에 남긴다.
코드 컨벤션, 명시적인 의존 전달, 하나의 production 파일/주 소유 문서 규칙을 준수한다.

루트가 통합하고 핵심 순수 알고리즘만 author-unity-code worker에 위임한다.
검증은 컴파일, Dijkstra/힙/수명 검사, 참조/범위 diff, FarmerScene.Structure v1,
실제 Play Mode 확인, 독립 reviewing-unity-candidate 리뷰 및 Harness.ReviewEvidence v1 순서다.
검증용 새 gate profile은 만들지 않는다. 최종 후보 고정 후 수정 예산은 총 2회다.
검증 미완료는 완료로 보고하지 않는다. 원본 Editor와 미저장 작업은 보존한다.

실행 증거: `.harness-runs/grid-navigation-20260921/`.
