# LocalizeSystem 1차 구현 계획

> 승인 근거: 2026-09-20 사용자가 전달한 계획의 구현 요청.
> 후속 변경: Unity 에셋은 YAML로 조립, 외부 Nanum Gothic 다운로드 대신 Unity 기본 폰트 사용.
> 현재 상태: 구현 후보 작성, 실행 검증 범위는 `Status/PROGRESS.md` 참조.

## 목표와 범위

CSV 편집 → 명시적 ID를 가진 LocalizeKey 자동 생성 → LocalizeData 조회 → LocalizeText 표시.
전용 LocalizeTest 씬에서 uGUI Text와 TMP를 비교한다. Manager는 씬 전환 후 유지되는 싱글턴,
데이터와 언어별 조회 Dictionary는 하나의 SO가 소유한다. 현재 언어는 `ko`로 고정한다.
NPC 생각 판단, 대화 순서, 말풍선, 타이핑, 기존 UI 전체 전환, 언어 변경 UI/API는 제외한다.

## CSV와 자동 생성

원본: `Assets/Data/CSV/LocalizeData.csv`, 열: `Id,Key,ko`, 이후 언어 열 추가 가능.

- Id는 중복 없는 양의 Int32, Key는 대소문자를 구분하는 C# enum member 이름이다.
- `LocalizeKey`는 명시적 정수 ID를 사용한다. 행 재정렬과 문구 변경은 생성 코드에 영향을 주지 않는다.
- 기존 키 삭제·이름 변경·ID 변경은 거부하고 마지막 생성본을 보존한다. 미사용 키도 행을 유지한다.
- UTF-8 BOM, 따옴표 내 쉼표·줄바꿈·이중 따옴표를 지원한다. 잘못된 구조·중복·빈 한국어는 행과 원인으로 보고한다.
- 기존 아이템 `CSVParser`는 변경하지 않는다.
- 생성기는 Editor에만 존재하며 CSV import 후 지연 호출한다. 검증 후 변경된 출력만 쓴다.
- 수동 재생성 메뉴 및 빌드 전 CSV/생성 소스/컴파일된 Enum 일치 검사를 제공한다.
- 생성 소스 자체가 ID 이력이므로 삭제 시 새로 만들지 않고 버전 관리에서 복원하도록 안내한다.

## 런타임 계약

| 구성 | 계약과 책임 |
|---|---|
| LocalizeData | TextAsset 참조, 최초 조회 시 언어별 Dictionary 구성, `TryGetText(key, languageCode, out text)` |
| LocalizeManager | 읽기 전용 Instance/CurrentLanguage, `TryGetText`, `GetText`, 중복 제거와 DontDestroyOnLoad |
| LocalizeText | 정확히 하나의 Text/TMP 대상, Inspector 키, `SetKey`, `Refresh` |

SO 캐시는 비직렬화하고 외부 컬렉션을 노출하지 않는다. 조회 전 CSV와 Enum을 검사해 부분 적재를 막는다.
Editor CSV 변경과 새 Play 세션에서 무효화한다. 실행 중 CSV 편집의 즉시 화면 반영은 제외한다.
Manager 정적 참조는 SubsystemRegistration과 소유 인스턴스 OnDestroy에서 정리한다.
TryGetText 실패는 false/빈 문자열, GetText 실패는 `[Missing:키]`와 키별 중복 억제 오류 로그다.
LocalizeText는 첫 Start, 이후 OnEnable, SetKey에서 표시하며 최초 OnEnable에서는 조회하지 않는다.
Start 이전 SetKey는 저장하고 Start에서 적용한다. 매 프레임 조회·씬 검색을 사용하지 않는다.

## 배치와 조립

- 파서/정합성 검사: `Scripts/System/Localization`, Enum: `Scripts/Enum`.
- Manager: `Scripts/Manager`, SO: `Data/ScriptableObject`, view: `Scripts/UI`.
- 생성기/빌드 검사/자동 검사: `Editor/Localization`.
- 수동 실행 도구: `TestOnly/TestLocalizeControls.cs`, 씬: `Scenes/LocalizeTest.unity`.
- SO·씬 참조는 사용자 지시에 따라 YAML로 작성한다. 기존 씬과 Build Settings는 유지한다.
- Text는 프로젝트의 기존 기본 폰트 참조, TMP는 설치된 Unity uGUI 패키지의 Liberation Sans 동적
  Font Asset 및 필요한 TMP Resources/Shader를 사용한다. 배포본 라이선스를 함께 보존한다.
- **기본 TMP 폰트에 한국어 글리프가 없는 제약을 허용한 구성**이다. 문자열 조회와 실제 글리프 표시는
  별도 검증한다. 한국어 지원 폰트 도입은 사용자가 다시 선택할 후속 작업이다.
- 검증 Player 빌드는 LocalizeTest 씬 목록을 명시한다. 시작 씬/EditorBuildSettings는 변경하지 않는다.

## 검증과 완료 조건

| 항목 | 기준 |
|---|---|
| 파서 | 한국어/쉼표/따옴표/여러 줄 보존, 오류는 부분 반환 없이 행과 원인 보고 |
| Enum | 키 추가 자동 생성, 기존 ID 유지, 삭제/변경 거부, 문구 변경/재정렬 시 출력 동일 |
| 캐시 | 반복 조회 시 동일 캐시, 새 Play 세션/CSV 재임포트 후 재구성 |
| UI | 첫 표시, 키 변경, 재활성화, 실행 중 생성 시 Text/TMP 문자열 일치 |
| Singleton | 중복 인스턴스 제거, 씬 이동 후 동일 인스턴스, Domain Reload off 반복 진입 |
| 빌드 | Unity 컴파일/검증 Player 빌드, CSV·Enum 불일치 사전 차단 |

명령행 순수 C# 검사, Editor 검사, Play Mode, Player와 실제 글리프 표시를 구분해서 기록한다.
실행하지 않은 항목은 `NOT_VERIFIED`다. 현재 구현 진입점/MCP 연결이 없어 Unity 실행 검증은 미완료다.
검증 결과를 확인한 뒤 implement-npc-feature 스킬의 독립 읽기 전용 리뷰를 비동기로 요청하고
PROGRESS에 요청 상태를 기록한다. 수신하지 않은 리뷰 결과를 통과로 기록하지 않는다.
