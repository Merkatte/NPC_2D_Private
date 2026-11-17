# Sequence Actors

## 책임과 주 소유 코드

`Assets/Scripts/System/Sequence/SequenceActorDirector.cs`가 이 기능을 소유한다. 외부 scene 배우의 Transform과 Animator를 조작한다. root·Animator·anchor는 바인딩에서 받으며 actor 자체에 전용 행동 script를 붙이지 않는다. 기존 Visual 경로의 걷기 애니메이션을 unscaled로 재사용하고 연출 종료 시 원래 상태를 복구한다.

## 최소 확인 범위와 흐름

연출 변경 시 해당 코드와 [Sequences](Sequences.md)의 단계·바인딩, 실제 씬 연결을 확인한다. 실행 시작/완료/실패/취소는 SequenceAction 계약을 따른다. 시간 정지 token은 진행기가 소유한다.

## 검증·제약·갱신

실제 컴파일·참조 검토는 PROGRESS를 따른다. 화면과 Play는 사람 확인 항목이다. API, serialized 설정, scene 참조 변경 시 이 문서를 갱신한다.
