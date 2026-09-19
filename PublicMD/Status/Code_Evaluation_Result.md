# Code Evaluation Result

## Purpose

LocalizeSystem 1차 구현을 책임 경계, 의존 방향, 런타임 정확성, Unity 직렬화 안전성 및 코드 규약 기준으로 검토했다. **리뷰만 수행했으며 파일을 수정하지 않았다.**

## Review Snapshot

- **Date:** 2026-09-20
- **Scope:** 제공된 로컬라이제이션 변경 70개 경로. C# 14개, CSV·Enum·SO, LocalizeTest 씬, TMP 리소스와 관련 문서.
- **Excluded:** 기존 Guard·모집 변경과 무관한 미커밋 작업.
- **Standards:** `reviewing-npc-work-code` 스킬, `ProjectStructure.md`, `CodeConvention.md`, `ARCHITECTURE.md`, `Systems/Localization.md`, `Systems/UI.md`, 승인된 구현 계획.
- **Evidence:** 현재 소스·YAML·`.meta`, Git 상태와 문서 diff, `.harness-runs/localization-20260920`의 검사 코드·로그·기준 해시.

신규 파일은 대부분 untracked 상태이므로 tracked diff에만 의존하지 않고 파일 전체를 읽었다.

## Executive Summary

**정적 검토에서 차단 수준의 결함은 발견하지 못했다.** 파서, Editor 생성기, SO 캐시, persistent Manager와 표시 어댑터의 책임이 분리되어 있으며 문서화된 의존 방향과 일치한다.

낮은 심각도의 naming 규약 위반 1건이 있다. Unity import, 실제 캐시 lifecycle, Play Mode와 Player 검증은 여전히 미완료이므로 이번 결과를 기능 전체의 실행 검증 통과로 해석해서는 안 된다.

| 검토 우선순위 | 결과 |
|---|---|
| 1. Architecture / responsibility | 문제 발견 없음 |
| 2. Correctness / lifecycle / cancellation / regression | 검토 범위에서 확정 결함 발견 없음. Unity 실행 검증은 미완료 |
| 3. Scene / component / serialized references | 정적 검사에서 문제 발견 없음. 실제 import·rendering은 미검증 |
| 4. Convention / maintainability / dead code / magic values | Low 1건. 그 외 별도 finding 없음 |

## Improvements Since Previous Review

기존 보고서는 NPC Harness를 대상으로 하므로 이번 로컬라이제이션 검토와 직접 비교할 수 없다. 기존 Harness finding의 해결 여부는 재검증하지 않았으며, 해결된 것으로 판정하지 않는다.

## Findings By Severity

### Critical

None found.

### High

None found.

### Medium

None found.

### Low

#### L-01 — private static readonly 필드의 이름이 프로젝트 규약과 다르다

- **Severity:** Low — 비차단 정리 항목.
- **Category:** Code convention / Naming.
- **Location:** `Assets/Scripts/System/Localization/LocalizeCsvParser.cs:38`, 사용 지점 `:49`.
- **Evidence:** `private static readonly HashSet<string> ReservedNames`로 선언되어 있다. `PublicMD/CodeConvention.md:22`는 private field에 `_camelCase`를 요구하며 static readonly 예외를 두지 않는다.
- **Description:** 새 코드의 필드 이름이 프로젝트의 명시적 규약과 일치하지 않는다. 동작 결함은 아니다.
- **Recommended fix:** 선언과 사용 지점의 이름을 `_reservedNames`로 맞춘다.
- **Impact if unfixed:** 기능 영향은 없지만 새 코드의 naming 일관성이 낮아진다.

## Findings By File

| 파일·그룹 | 검토 결과 |
|---|---|
| `LocalizeCsvParser.cs` | BOM, quoted comma/newline/escaped quote, 필수 열, 중복 ID·Key·언어, 빈 한국어와 구조 오류를 검사한다. 실패 시 Table을 공개하지 않는다. L-01 외 문제 없음 |
| `LocalizeKeySource.cs`, `LocalizeKeyGenerator.cs`, `LocalizeKeyValidation.cs` | 이전 생성 소스를 ID 이력으로 사용한다. 기존 identity 변경 거부, ID 정렬, 변경 시에만 쓰기, CSV·소스·컴파일된 Enum 비교가 분리되어 있다 |
| `LocalizeCsvPostprocessor.cs`, `LocalizeBuildValidator.cs` | import 후 지연 생성과 빌드 차단의 책임이 분리되어 있다. 빌드 검사 자체는 코드를 생성하지 않는다 |
| `LocalizeData.cs` | 공유 정의에서 파생된 비직렬화 lookup만 소유한다. 검증 후 완성된 dictionary를 게시하고, 실패 반복과 세션별 캐시 재사용을 제어한다 |
| `LocalizeManager.cs` | 문구 조회·중복 제거·persistent lifecycle만 소유한다. 전용 루트 검사와 소유 인스턴스의 정적 참조 정리가 있다 |
| `LocalizeText.cs` | Text/TMP 중 정확히 하나를 요구한다. 최초 조회를 Start까지 지연하고 이후 재활성화·SetKey에서 갱신한다 |
| 테스트·빌드 도구 | 순수 검사, Unity 의존 검사, 수동 probe와 Player 빌드가 구분되어 있다. 수동 persistence probe는 원본 씬 전체 unload를 검증하지 않는다 |
| CSV·Enum·SO·씬·TMP 에셋 | 현재 키 1001–1003, 데이터 연결, 표시 대상, 폰트·머티리얼 참조가 정적 검사와 일치한다 |
| 관련 문서 | 책임, singleton 접근 범위, 캐시 계약, 폰트 제약과 미검증 항목을 명시한다 |

## Cross-Cutting Findings

- production의 `LocalizeManager` 접근은 `LocalizeText`에 한정되어 있다. action·selector·provider로의 의존 확장은 검색에서 발견하지 못했다.
- SO 캐시는 actor·scene별 mutable gameplay 상태를 포함하지 않는다.
- production 로컬라이제이션 코드에서 매 프레임 조회, 반복 scene search 또는 Editor API 의존을 발견하지 못했다.
- 늦게 생성된 Manager의 자동 연결, 실행 중 CSV hot reload, 키 migration은 현재 계약 밖으로 문서화되어 있다. 이를 이번 구현의 누락 결함으로 처리하지 않았다.
- 기본 TMP 폰트의 한국어 글리프 부족은 승인 범위에 기록된 제약이다. 문자열 조회 성공과 한국어 화면 표시 성공은 별도로 판단해야 한다.

## Positive Notes

- 명시적 Enum ID와 이전 생성 소스 검증으로 행 재정렬 및 연속 import 중 identity 변경을 방어한다.
- CSV 오류 시 부분 적재를 공개하지 않는다.
- 빌드 시 CSV, 생성 소스와 컴파일된 Enum을 모두 확인한다.
- lookup 컬렉션을 외부에 mutable 상태로 노출하지 않는다.
- 첫 OnEnable과 Manager Awake 사이의 순서 문제를 피하도록 최초 UI 조회를 지연한다.
- 사용자 기존 변경과 로컬라이제이션 후보를 분리했으며, 검사되지 않은 Unity 실행 항목을 PASS로 기록하지 않았다.

## Verification

| 항목 | 이번 리뷰 결과 |
|---|---|
| Git 상태·범위 내 문서 diff | 확인 |
| 범위 내 문서 `git diff --check` | PASS |
| 신규 C#의 trailing whitespace·충돌 표식 | 발견 없음 |
| 기존 순수 테스트 바이너리 재실행 | Unity 내장 Mono에서 **38개 + 실제 CSV/생성 소스 일치 PASS** |
| 직렬화 검사 재실행 | 결과 파일 쓰기를 제거한 메모리 실행으로 **84개 PASS** |
| 기존 파일 보존 | 제공된 기준 해시와 비교해 **1,316개 일치** |
| 범위 내 `.meta` | 누락 및 Assets 내 GUID 충돌 발견 없음 |
| TMP 원본 폰트 문자 검사 | 샘플의 한국어 고유 문자 **19개 미포함** 재확인 |
| Runtime / Editor 컴파일 | 기존 로그 각각 **0 errors / 0 warnings** 확인. 새 빌드는 실행하지 않음 |

## Verification Limits

- read-only 지시를 준수하기 위해 `dotnet build`를 재실행하지 않았다. 빌드는 출력·중간 파일을 생성한다.
- 순수 테스트는 기존 바이너리를 재사용했다. 현재 소스를 새로 컴파일한 독립 검증은 아니다.
- 테스트 EXE 직접 실행은 예외 출력 실패로 종료되었으나, 설치된 Unity의 Mono로 실행했을 때 통과했다.
- 순수 테스트 host의 기존 빌드 로그에는 MSB3276 경고 1건이 있다. Runtime / Editor 컴파일의 무경고 결과와 구분한다.
- Unity import/load, 실제 Editor 캐시 검사, UI lifecycle, 전체 씬 교체, Domain Reload off 반복 Play, Player 빌드·실행·렌더링은 **NOT_VERIFIED**다.
- 84개 검사는 YAML 참조와 지정된 구조 조건을 확인한다. Unity importer의 수용, callback 순서 또는 실제 화면 표시를 증명하지 않는다.
- `TestLocalizeControls.CheckPersistence()`는 임시 씬을 만들고 제거하지만 원본 씬을 unload하지 않는다. 전체 씬 교체 검증을 대신하지 않는다.

## Recommended Next Actions

1. Unity에서 import·컴파일 후 `Run Edit Mode Checks`를 실행한다.
2. 첫 표시, Start 전 SetKey, 재활성화, runtime 생성, 중복 Manager 제거와 **원본 씬 전체 교체**를 검증한다.
3. Domain Reload off 반복 진입과 다음 세션의 캐시 재구성을 확인한다.
4. CSV 추가·재정렬·identity 변경 거부, 반복 import, 불일치 빌드 차단을 실제 Editor에서 확인한다.
5. LocalizeTest Player를 빌드·실행하고 문자열 결과와 글리프 표시를 별도로 기록한다.
6. L-01의 필드 이름을 규약에 맞춘다.

## Final Verdict

**정적 코드 리뷰: Approve with nits.**

아키텍처·정확성·직렬화에서 확인된 차단 결함은 없으며, Low naming 항목 1건이 남는다. **LocalizeSystem 1차의 최종 완료 판정은 Unity 실행 검증 이후로 보류한다.**