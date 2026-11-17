# Game Time

## 목적과 책임

`GameTimeController`는 씬 단위 정지 token과 원래 시간 배율을 소유한다. 시간 배율의 production 쓰기는 이 컴포넌트로 한정한다. 각 소비자는 자기 token만 반환하고 마지막 token이 없어야 기존 배율을 복원한다.

## 주 소유 코드와 최소 확인 범위

- `Assets/Scripts/System/Time/GameTimeController.cs`: AcquirePause/ReleasePause, 읽기 전용 IsPaused와 Changed, disable 복구.
- 방어 소비자는 [Defense Progression](../Combat/Defense/Progression.md)의 DefenseGameSession API를 유지하며 이 컴포넌트를 참조한다.

## 흐름과 불변 규칙

첫 정지 요청에서 Time.timeScale을 보관하고 0으로 설정한다. 각 token은 개별 수명을 가진다. 대화가 끝나도 다른 요청과 GameOver의 정지가 남는다. disable에서는 소유 요청을 정리하고 원래 배율을 복원한다. UI와 카메라 연출은 독립 시간으로 실행한다.

## 배선과 검증

DefenseTest와 TerrainTest의 방어 세션에 공통 관리자 참조를 연결한다. 실제 연결·컴파일·리뷰 결과는 PROGRESS를 따른다. 중복 정지/재개·씬 종료는 사람 Play 확인 항목이다. 새 시간 소비자 또는 공개 API·수명 변경 시 이 문서를 갱신한다.
