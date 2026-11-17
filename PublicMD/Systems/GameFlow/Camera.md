# Sequence Camera

## 책임과 주 소유 코드

`Assets/Scripts/System/Sequence/SequenceCameraDirector.cs`가 이 기능을 소유한다. 목표 좌표·크기·시간·easing으로 unscaled DOTween을 실행한다. 시작 position/rotation/orthographic size를 보관하고 종료·취소 시 복원한다. 수동 입력은 점유 동안 양보한다.

## 최소 확인 범위와 흐름

연출 변경 시 해당 코드와 [Sequences](Sequences.md)의 단계·바인딩, 실제 씬 연결을 확인한다. 실행 시작/완료/실패/취소는 SequenceAction 계약을 따른다. 시간 정지 token은 진행기가 소유한다.

## 검증·제약·갱신

실제 컴파일·참조 검토는 PROGRESS를 따른다. 화면과 Play는 사람 확인 항목이다. API, serialized 설정, scene 참조 변경 시 이 문서를 갱신한다.
