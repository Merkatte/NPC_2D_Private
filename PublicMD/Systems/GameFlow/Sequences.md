# Editable Sequences

## 목적과 책임

대사·선택·카메라·배우·외부 행동을 단계 ID로 연결한다. 특정 씬 이름이나 튜토리얼 내용을 runtime 코드에 넣지 않는다. 다음 ID가 없으면 종료하고 외부 행동은 완료 통지를 기다린다.

## 주 소유 코드

| 파일 | 책임 |
|---|---|
| Assets/Data/ScriptableObject/Script/SequenceDefinition.cs | entry와 단계 정의 및 ID 연결 검사 |
| Assets/Data/Struct/SequenceStep.cs | 단계 종류, 대사/선택/연출 값 |
| Assets/Scripts/System/Sequence/SequenceRunner.cs | 순차 진행·입력·정지 수명·종료/실패/취소 |
| Assets/Scripts/System/Sequence/SequenceBindings.cs | scene actor/target/action ID 연결 |
| Assets/Scripts/System/Sequence/SequenceAction.cs | 비동기 행동 시작·완료·취소 계약 |

## 흐름과 최소 확인 범위

순서/분기를 바꾸면 Definition/Step/Runner와 대상 SO, 대상/연출은 Bindings와 해당 Director 및 씬을 확인한다. 문구는 LocalizeData.csv와 [Localization](../UI/Localization.md), 표시/입력은 [Dialogue UI](../UI/Dialogue.md)를 따른다.

TryStart는 정의·참조를 확인하고 카메라를 보관한 뒤 시간/입력 token을 확보한다. 단계 진행은 동일 token을 유지하며 여러 대화가 동시에 실행되지 않는다. 키/클릭은 입력 프레임과 단계 진입 프레임을 구분한다. 선택은 응답을 기다린다. 취소/실패는 자기 연출·표시·token을 정리한다. 완료와 실패는 별개 결과다.

## 배선·제약·검증

공유 SO에는 scene 참조와 현재 진행을 저장하지 않는다. Bindings에는 actor root/Animator/말풍선 anchor와 target Transform 또는 좌표를 지정한다. 새 장면에서는 바인딩만 교체한다. 런타임 hot reload와 전용 node editor는 없다. 현재 결과와 로그는 PROGRESS, 실제 연출/입력은 사람 확인 항목이다. 코드/API/serialized field 변경 때 이 문서를 갱신한다.

연출 구현 주 소유는 [Camera](Camera.md), [Actors](Actors.md)다.
