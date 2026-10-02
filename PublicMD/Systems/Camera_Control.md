# Camera Control

## 책임과 범위

현재 테스트 씬의 카메라 입력은 `Assets/TestOnly/TestCameraArrowMove.cs`가 소유한다. 방향키 평면 이동과 마우스 휠 직교 줌을 제공하는 임시 조작 컴포넌트이며, NPC·경제·건설 runtime은 이 타입에 의존하지 않는다. 정식 카메라 시스템이나 원근 투영 제어는 범위 밖이다.

## 변경 유형별 최소 확인 범위

- 이동·줌 입력: `TestCameraArrowMove.cs`, `PublicMD/CodeConvention.md`.
- UI 위 휠 입력 차단: `Assets/Scripts/UI/PointerClickRouter.cs`의 현재 위치 GraphicRaycaster 검사 패턴과 [UI](UI.md).
- 배선·초기값: `Assets/Scenes/BuildingTest.unity`, `Assets/Scenes/FarmerTest.unity`의 Main Camera와 TestCameraArrowMove 컴포넌트.

## 조작과 설정

방향키는 기존 XY 이동과 이동 경계를 유지한다. 휠 위는 확대, 휠 아래는 축소한다. 직교 카메라이므로 Transform Z가 아니라 Camera.orthographicSize를 변경하며, 값이 작을수록 가까이 보이고 클수록 넓은 영역을 본다.

BuildingTest와 FarmerTest의 `Main Camera > Test Camera Arrow Move` Inspector에서 다음 값을 조정한다.

| 필드 | 기본값 | 의미 |
|---|---|---|
| World Camera | 같은 오브젝트의 Camera | 직교 카메라 명시 참조 |
| Min Orthographic Size | 2 | 최대 확대 시 가장 작은 화면 범위 |
| Max Orthographic Size | 20 | 최대 축소 시 가장 넓은 화면 범위 |
| Zoom Speed | 1 | 휠 한 칸당 Size 변화량, 0이면 줌 중지 |

기존 시작 Size 5, Transform Z −10과 XY 이동 경계를 보존한다. Awake/OnValidate에서 최소 Size는 0.01 이상, 최대 Size는 최소 이상, 감도는 0 이상으로 보정한다. Awake에서 현재 Size를 범위 안으로 제한한다. 필수 카메라가 없거나 원근 투영이면 한 번 오류를 기록하고 줌만 중단하며 방향키 이동은 유지한다.

설치된 Input System의 정규화된 휠 입력을 사용한다. Windows에서 KeepPlatformSpecificInputRange일 때만 120으로 나누고, 누적·소수 입력 크기를 보존한다. 휠 입력에는 deltaTime을 곱하지 않는다.

현재 화면의 uGUI GraphicRaycaster hit 위에서는 줌 입력을 무시하여 팝업의 목록 스크롤과 카메라 줌이 동시에 실행되지 않도록 한다. 카메라 조작은 domain 데이터나 팝업의 스크롤 상태를 변경하지 않는다.

컴파일·직렬화 검토와 실제 Unity에서의 휠·팝업 스크롤 조작 검증은 구분한다. 실제 화면과 입력 검증은 사람이 확인한다.
