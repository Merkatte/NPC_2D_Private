# Startup Tutorial

## 목적과 소유 코드

- `Assets/Scripts/Tutorial/DefenseTutorialStartup.cs`: 자동 시작 toggle, 초기 정지 확보와 SequenceRunner 시작, 향후 이어하기용 SkipForContinue.
- `Assets/Editor/SceneWork/DefenseTutorialSetup.cs`: 승인된 씬·UI·배우·정의 연결용 Editor 조립 도구. 코드 역할이 작성하고 연결 역할이 실행한다.

## 흐름과 최소 확인 범위

초기 Awake에서 정지를 확보하고 Start에서 공통 진행기로 인계한다. 초기 roster/시설 조립과 게임 진행을 구별한다. 시청 도입→질문→시설 소개 또는 거절→접수원 인사·귀환→카메라 원복→재개 순서는 정의 에셋에서 편집한다. 순서·문구·대상 변경은 [Sequences](Sequences.md), UI는 [Dialogue](../UI/Dialogue.md)를 따른다.

## 배선과 불변 규칙

DefenseTest가 첫 사례다. Tutorial host와 UI host는 상시 활성이고 비활성 DefenseUI에 종속되지 않는다. 접수원은 기존 은발 sprite와 NPCGirl_Move controller를 재사용하며 custom MonoBehaviour나 주민 AI를 붙이지 않는다. 이미지 scale은 애니메이션이 소유한 Visual 아래에서 조절한다. 별도 문·대기 anchor를 사용한다.

기본 소개는 시청/밭/창고/식당/여관/초소/북쪽 성벽/상단 방문 장소다. 상단 강제 등장과 현재 없는 주택은 제외한다. 저장 시스템은 없으므로 기본 씬 재실행마다 재생한다. 이어하기의 영속 판별은 TBD이며 시작 호출을 생략할 수 있다.

## 검증과 변경

실제 compile·배선·리뷰는 PROGRESS에 기록한다. 사람은 수락/거절, 정지 유지, 귀환/원복, 데이터 편집과 한글/화면 배치를 확인한다. 자동 Play/화면 검증은 실행하지 않는다. 시작 정책/배선/API 변경 시 이 문서를 갱신한다.

## 편집 위치

- `Assets/Data/ScriptableObject/Tutorial/DefenseTutorial.asset`: 시작 ID와 단계/분기, 표시 방식, 연출 시간/확대/easing, 자동 넘김 대기.
- `Assets/Data/CSV/LocalizeData.csv`: 키1201~1215의 초기 안내 문구. 기존 생성기로 LocalizeKey를 동기화한다.
- DefenseTest `Systems/TutorialSystems`의 SequenceBindings: 배우와 11개 target 연결. 다른 씬에서는 이 바인딩을 교체한다.
- `Assets/Prefab/UI/SequenceDialogue.prefab`: 말풍선과 하단창 스타일·Text·선택지 템플릿. 필수 참조를 유지한다.

기존 NPC 생각 hover는 정식 대화로 시작하지 않는다. 신규 정식 대화는 공통 SequenceRunner를 통해 실행한다.
