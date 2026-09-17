# Codex 작업 하네스 구현 계획

> 문서 상태: 구현 완료, 외부 Batch·Windows 실기 검증 대기 0.2
>
> 기준일: 2026-09-17
>
> 목표 아키텍처: [`HARNESS_ARCHITECTURE.md`](../HARNESS_ARCHITECTURE.md)

## 1. 목표

프로젝트 Skill로 호출된 루트 Codex가 작업을 조정하고, 에이전트와 독립된 결정적 하네스가 후보 결과물을 검사하며, 증거가 있는 `PASS`만 최종 완료 후보가 되게 한다.

이 계획은 gameplay 기능을 추가하지 않는다. 기존 `NPC Harness v1` prototype을 재사용 가능한 Unity 작업 어댑터와 결정적 gate로 분해한 뒤, Skill·Reviewer·선택적 작업 에이전트를 순서대로 연결한다.

## 2. 완료 조건

- 새 checkout의 macOS와 Windows에서 같은 gate 명령과 결과 schema를 사용한다.
- Codex가 없어도 정상 fixture와 고장 fixture를 결정적으로 판정한다.
- 작업자의 완료 보고만으로 성공하지 않고 GateResult 증거를 요구한다.
- 프로젝트 Skill이 직접 작업, 단일 작업자와 복수 작업자 중 최소 실행 방식을 선택한다.
- 변경 작업 후 결정적 gate를 통과해야 Reviewer 단계로 이동한다.
- Reviewer의 blocking finding을 수정하면 결정적 gate를 다시 실행한다.
- 실행별 manifest, gate result, review와 로그가 `.harness-runs/<runId>`에 남는다.

## 3. 단계

| ID | 단계 | 핵심 산출물 | 상태 |
|---|---|---|---|
| H-01 | 아키텍처 경계 | 오케스트레이터·하네스·Reviewer 책임과 신뢰 경계 | completed |
| H-02 | GateResult 기반 | 공통 result contract, schema, check별 증거와 판정 테스트 | completed |
| H-03 | HarnessBeacon 결정적 profile | 구조·Play Mode gate, 정상·고장 fixture | completed |
| H-04 | cross-platform runner | tracked project file, macOS/Windows launcher, 동일 exit code | implementation complete / batch and Windows runtime pending |
| H-05 | 단일 작업 오케스트레이터 Skill | 루트 Codex의 scope→candidate→gate→완료 흐름 | completed |
| H-06 | 수정 루프 | 실패 증거를 이용한 bounded remediation과 재검증 | completed |
| H-07 | 독립 Reviewer | read-only review contract와 blocking finding 처리 | completed |
| H-08 | 선택적 멀티에이전트 | 비중첩 assignment, 결과 취합과 충돌 방지 | completed |
| H-09 | 하네스 회귀 평가 | 정상·오염 fixture와 거짓 통과 방지 suite | completed |

## 4. H-02 — GateResult 기반

### 범위

- Job 실행 결과와 분리된 `HarnessGateResult`
- `Pass`, `Fail`, `InfrastructureError` 구분
- check별 ID, expected, actual과 message
- artifact, changed file과 시작·종료 시간
- 중복 check ID 거부
- JSON Schema
- Unity 의존 없는 판정 builder 테스트

### 제외

- Git baseline 수집
- Play Mode 결과 통합
- 외부 runner에서 schema validation
- 프로젝트 Skill과 에이전트 호출

### 종료 조건

- 전체 `Assets/Editor/NpcHarness/**/*.cs` 독립 컴파일 성공
- 정상, check 실패, infrastructure failure, 중복 ID와 증거 보존 테스트 통과
- `gate-result.schema.json` JSON 파싱 성공
- Unity Test Runner에서 같은 테스트 통과

네 조건을 모두 통과했다. Unity Editor의 EditMode Test Runner에서도 6개 테스트가 통과했다.

## 5. H-03 — HarnessBeacon 결정적 profile

### 범위

- `HarnessBeacon.Structure` profile
- 기존 scene validator를 check별 GateResult로 변환
- Play Mode 성공 로그 정확히 1회
- Play Mode 중 Error, Assert와 Exception 0건
- timeout과 Unity 실행 실패를 candidate failure와 구분
- 정상 fixture와 다음 고장 fixture
  - HarnessTest component 누락
  - LineRenderer 또는 material 누락
  - Camera 설정 불일치
  - 성공 로그 0회 또는 2회
  - Play Mode 오류 로그

### 종료 조건

- 정상 fixture는 `Pass`
- 각 고장 fixture는 대응 check ID의 `Fail`
- Unity 미설치·process crash는 `InfrastructureError`
- result JSON과 process exit code가 일치
- 검증 후 tracked asset에 예상하지 않은 변경이 없음

구조 검증과 Batch 진입점은 GateResult를 반환한다. Play Mode 판정도 같은 계약으로 통합했고, 성공 로그 0회·2회, 오류 로그, timeout, 예기치 않은 종료와 구조 실패의 순수 판정 fixture를 추가했다. 실제 HarnessBeacon scene의 정상 fixture는 Unity PlayMode에서 9개 check `Pass`와 자동 종료를 확인했다. 구조·runtime·infrastructure 고장 fixture는 실제 asset을 손상시키지 않는 순수 observation으로 대응 check ID의 `Fail`을 검증했다.

## 6. H-04 — Cross-platform runner

- `Tools/NpcHarness`의 빌드 project file을 Git에 추적한다.
- 자연어 분류, Codex 재호출과 고정 Job 생성을 제거한다.
- runner는 명시적인 `verify`, `run-adapter`, `self-test` 명령만 제공한다.
- Unity 경로는 명시적 override와 macOS/Windows 기본 위치를 지원한다.
- `.cmd`와 shell wrapper는 같은 runner를 호출하는 편의 진입점으로만 둔다.

종료 조건은 새 checkout에서 외부 NuGet package download 없이 SDK-local restore/build가 가능하고 두 플랫폼 명령 계약이 동일한 것이다.

tracked `NpcHarness.csproj`, root `.cmd`/`.sh`, macOS·Windows Unity 탐색과 공통 exit contract를 구현했다. macOS에서 build와 self-test는 통과했으며 열린 Editor lock은 `InfrastructureError`로 차단됐다. 실제 Unity Batch end-to-end와 Windows 실기 실행은 환경 검증이 남아 있다.

## 7. H-05~H-08 — Skill과 에이전트 연결

### 단일 작업 Skill

Skill은 `PublicMD/ProjectStructure.md` routing, scope 작성, 후보 확인, gate 호출과 완료 판정만 지시한다. 결정적 로직을 Markdown으로 다시 구현하지 않는다.

### 수정 루프

GateResult의 실패 check만 근거로 수정 범위를 만들며, retry 상한을 RunManifest에 기록한다. 후보가 변경되면 이전 gate result는 만료된다.

### Reviewer

결정적 gate 통과 후 별도 읽기 전용 에이전트가 원본 요청, 관련 문서, diff와 GateResult만 검토한다. 구현자의 결론은 제공하지 않는다.

### 선택적 멀티에이전트

읽기 전용 조사는 병렬화할 수 있다. 쓰기 작업은 경로가 겹치지 않을 때만 병렬화하며, 그렇지 않으면 순차 실행 또는 격리된 worktree를 사용한다.

구현은 `.codex/skills/orchestrate-unity-work`와 `.codex/skills/reviewing-unity-candidate`에 있다. bounded retry ledger, WorkerAssignment/WorkerReport, native collaboration subagent 조정, 결정적 Pass 뒤 독립 Reviewer와 stale result 무효화를 포함한다. Reviewer Skill은 독립 forward-test에서 passing gate를 보존하면서 Major 결함 2개를 검출했다.

## 8. 검증 매트릭스

| 대상 | 정적 | Edit Mode | Play Mode | 독립 Review |
|---|---:|---:|---:|---:|
| GateResult contract | 필수 | 테스트 실행 | 해당 없음 | H-02 종료 전 선택 |
| HarnessBeacon structure | schema + compile | 필수 | 해당 없음 | H-03 종료 시 |
| HarnessBeacon runtime | compile | 선행 구조 gate | 필수 | H-03 종료 시 |
| cross-platform runner | build + schema | Unity 호출 smoke | runtime profile smoke | H-04 종료 시 |
| project Skill | quick_validate | dry scenario | 실제 단일 작업 | 독립 forward-test |

## 9. 위험과 대응

- Unity가 열린 상태에서 Batch 실행하면 프로젝트 lock 때문에 실패한다. runner는 이를 infrastructure failure로 보고 mutation을 시작하지 않는다.
- 검증 코드와 후보를 같은 Run에서 함께 변경하면 자기 채점이 된다. 하네스 변경은 별도 self-test 단계에서 검증한다.
- Unity-generated `.csproj`는 checkout 간 계약이 아니다. runner project는 `.gitignore` 예외와 함께 추적한다.
- Reviewer와 구현자가 같은 결론을 공유하면 독립성이 약해진다. Reviewer에는 원본 증거만 제공한다.

## 10. 명시적 보류

- production asset을 원자 Tool로 직접 편집하는 기능
- 원격·무인 Codex 실행 서비스
- 모든 gameplay 기능을 포괄하는 단일 gate profile
- 작업 에이전트별 강제 worktree

실제 단일 작업 흐름에서 필요성이 확인되기 전에는 구현하지 않는다.

## 11. 문서 갱신

- 단계 상태와 검증 결과는 이 문서와 `Status/PROGRESS.md`에 함께 반영한다.
- 책임과 신뢰 경계가 바뀌면 `HARNESS_ARCHITECTURE.md`를 갱신한다.
- 현재 폴더 책임과 routing이 바뀌면 `ProjectStructure.md`를 갱신한다.
- 완료된 계획은 `Archive/Plans`로 이동한다.
