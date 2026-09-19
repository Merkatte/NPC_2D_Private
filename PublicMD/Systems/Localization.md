# Localization

## 목적과 책임 경계

CSV 문구의 안정적인 key/ID 정의, 언어별 조회, Text/TMP 표시를 소유한다.
NPC 판단·대화 진행·말풍선·타이핑·게임 규칙은 포함하지 않는다.

## 현재 흐름

```text
LocalizeData.csv import -> LocalizeCsvPostprocessor -> LocalizeKeyGenerator
  -> LocalizeCsvParser + LocalizeKeySource -> LocalizeKey.cs (변경된 경우만 쓰기)
빌드 -> LocalizeBuildValidator -> CSV / 소스 / 컴파일된 Enum 일치 확인

LocalizeText.Start / OnEnable(두 번째부터) / SetKey
  -> LocalizeManager.Instance.GetText(key)
    -> LocalizeData.TryGetText(key, "ko", out text)
      -> 최초 1회 CSV parsing + Enum 검사 -> 언어별 Dictionary
  -> Text.text 또는 TMP_Text.text
```

## 주 소유 스크립트

| 경로 | 책임 |
|---|---|
| `Assets/Scripts/Enum/LocalizeKey.cs` | CSV로 생성하는 명시적 ID enum |
| `Assets/Scripts/System/Localization/LocalizeCsvParser.cs` | 순수 CSV 구조/데이터 검증, 읽기 전용 Table/Row |
| `Assets/Scripts/System/Localization/LocalizeKeyValidation.cs` | CSV와 컴파일된 Enum의 정확한 일치 검사 |
| `Assets/Data/ScriptableObject/Script/LocalizeData.cs` | TextAsset 참조와 비직렬화 언어별 조회 캐시 |
| `Assets/Scripts/Manager/LocalizeManager.cs` | ko 고정 조회 진입점, persistent singleton lifecycle, missing-key 진단 |
| `Assets/Scripts/UI/LocalizeText.cs` | 선택 키를 단일 Text/TMP 대상에 표시 |

지원 Editor 파일은 `Assets/Editor/Localization` 아래에 있다:
`LocalizeKeySource.cs`(결정적 소스 생성/이력 보호), `LocalizeKeyGenerator.cs`(파일 읽기/쓰기/메뉴),
`LocalizeCsvPostprocessor.cs`(CSV import 감지/캐시 무효화), `LocalizeBuildValidator.cs`(빌드 차단),
`LocalizeTestBuild.cs`(검증 씬 전용 빌드), `LocalizeTests.cs`(순수 자동 검사),
`LocalizeEditorTests.cs`(실제 데이터 정합성/캐시 검사 메뉴).
`Assets/TestOnly/TestLocalizeControls.cs`는 수동 UI/lifecycle 관찰 도구다. Production은 이를 참조하지 않는다.

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 문구/키 추가 | CSV, LocalizeKeySource, LocalizeKeyGenerator, 생성 Enum |
| CSV 문법/검증 | LocalizeCsvParser, LocalizeKeyValidation, LocalizeTests |
| 데이터/캐시 | LocalizeData.cs/.asset, LocalizeCsvPostprocessor, LocalizeEditorTests |
| Manager lifecycle | LocalizeManager, LocalizeText, LocalizeTest 씬 |
| 표시/폰트 | LocalizeText, LocalizeTest 씬, TMP Settings/동적 Font Asset, TestLocalizeControls |
| 빌드 | LocalizeBuildValidator, LocalizeKeyGenerator, LocalizeTestBuild |

## 불변 규칙과 실패

- 원본은 `Assets/Data/CSV/LocalizeData.csv`, 생성 소스는 `Assets/Scripts/Enum/LocalizeKey.cs`다.
- UTF-8 BOM, RFC-style quoted field의 쉼표/줄바꿈/`""`를 보존한다. 빈 중간 행도 구조 오류다.
- Id는 양의 Int32, Key는 대소문자 구분 C# identifier이며 키워드·타입명·`value__`는 금지한다.
- `ko`는 필수, 언어 열 이름/키/ID 중복 및 빈 한국어 문구는 전체 적재 실패다.
- ID 순으로 결정적으로 생성한다. 키 삭제/이름 변경/ID 변경은 오류다. 사용하지 않는 행도 보존한다.
- 이전 생성 소스를 이력으로 읽으므로 직전 import의 컴파일이 끝나지 않아도 추가된 키를 보호한다.
  손상/삭제된 생성 소스는 VCS에서 복원한다. 수동으로 CSV와 Enum을 동시에 바꾸는 migration은 지원하지 않는다.
- 빌드 검사에서는 코드를 생성하지 않는다. 불일치 시 재생성·컴파일 후 빌드하도록 차단한다.
- LocalizeData 캐시는 공유 정의의 파생 lookup이며 actor/scene 상태가 아니다. 실패 결과도 한 번만 진단한다.
  Editor에서 import/OnValidate로 무효화하고 SubsystemRegistration 세대 값으로 다음 Play의 캐시를 다시 만든다.
  Play 중 이미 구성한 캐시는 유지한다. 실행 중 CSV hot reload는 계약에 없다.
- Manager는 별도 루트 오브젝트에 배치한다. 중복 인스턴스의 **전체 GameObject**를 제거한다.
  기존 인스턴스와 SO가 계속 사용되므로 씬마다 서로 다른 LocalizeData를 제공하는 구성은 피한다.
- LocalizeText만 표시 adapter로서 싱글턴 접근을 사용한다. action/selector/provider에 접근 범위를 넓히지 않는다.
- Text/TMP 참조는 정확히 하나만 필요하다. 최초 Start 이전 SetKey/Refresh는 조회를 지연한다.
  Manager는 UI 첫 Start 전에 Awake를 완료해야 한다. 뒤늦은 Manager 생성은 명시적 Refresh가 필요하다.
- TryGetText 실패는 false/빈 문자열, GetText 실패는 `[Missing:키]`이며 키별 오류 로그를 억제한다.

## Unity 배선과 사용법

`Assets/Data/ScriptableObject/LocalizeData.asset`의 `_csv`는 원본 CSV,
`Assets/Scenes/LocalizeTest.unity`의 전용 루트 Manager `_data`는 이 SO를 참조한다.
Canvas 아래 Legacy Text/TMP Text의 LocalizeText는 각각 자신의 Text/TMP만 참조하고 같은 키 1001로 시작한다.
CanvasScaler 기준은 1280×720이다. 수동 도구는 IMGUI 버튼으로 키 순환, TMP 활성 전환,
runtime Text 복제, Manager 중복 제거와 임시 scene 전환/해제를 실행한다.
scene 전환 probe는 현재 원본 씬을 unload하지 않는다. 전체 씬 교체/반복 Play 검증은 별도 필요하다.

Text는 기존 프로젝트 UI와 같은 Unity 기본 폰트, TMP는 설치된 uGUI 패키지에 포함된
`Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset`을 직접 참조한다.
동적 atlas와 원본 LiberationSans.ttf 참조를 유지한다. 필요한 TMP Settings/스타일/줄바꿈 자료/셰이더만
동봉하며 font license는 `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`에 있다.
**이 기본 TMP 폰트는 샘플 한국어 글리프를 포함하지 않는다.** 한국어 데이터 조회 성공과 시각 표시 성공은 다르다.
외부 폰트 사용은 사용자 후속 요청으로 제외되었다.

Editor 메뉴:

- `Tools/Localization/Regenerate LocalizeKey`
- `Tools/Localization/Run Edit Mode Checks` (순수 검사 + 실제 CSV 정합성 + SO 캐시)
- `Tools/Localization/Build LocalizeTest Player` (검증 씬만, Windows x64 Development)

Player 경로는 `.harness-runs/localization-player/LocalizeTest.exe`이며 EditorBuildSettings를 바꾸지 않는다.
현재 검증 증거는 `.harness-runs/localization-20260920/` 및 [PROGRESS](../Status/PROGRESS.md)를 참조한다.

## 제약과 TBD

- 현재 언어 ko 고정, 언어 변경/선택 UI 미구현. 추가 언어 열은 같은 SO에 적재 가능하다.
- 기본 폰트 사용 조건에서 한국어 글리프의 완전한 표시 보장은 제외된다.
- Unity import/load, Editor 검사, Play/Player 실행과 Domain Reload off 실제 반복 진입은 `NOT_VERIFIED`다.
- 전체 기존 UI 전환, NPC 문구 선택 정책, 타이핑/대화 진행, 키 migration은 후속 범위다.

## 관련 문서와 갱신 조건

[승인 계획](../Plans/LocalizeSystem_Implementation_Plan.md), [UI](UI.md), [Architecture](../ARCHITECTURE.md).
API·CSV 규칙·캐시 수명·singleton 범위·씬/폰트 배선이 바뀌면 이 문서를 함께 갱신한다.
