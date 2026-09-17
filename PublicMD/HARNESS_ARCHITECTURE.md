# Codex 작업 하네스 아키텍처

> 문서 상태: 목표 아키텍처 초안 0.1
>
> 기준일: 2026-09-17
>
> 이 문서는 Codex가 이 Unity 프로젝트에서 작업할 때 사용하는 오케스트레이션 경계, 결정적 검증 하네스, 실행 증거와 완료 판정 규칙을 소유한다. 현재 구현과 목표 구조가 다르면 각 절의 `현재`와 `목표` 표기를 구분한다.

## 1. 목적

이 하네스의 목적은 AI가 만든 결과 자체를 결정적으로 만드는 것이 아니다. 비결정적으로 만들어진 후보 결과물을 독립된 규칙으로 검사하여 수락 여부를 재현 가능한 `PASS` 또는 `FAIL`로 판정하는 것이다.

이 프로젝트의 루트 Codex는 오케스트레이터다. 하네스는 오케스트레이터나 작업 에이전트를 대신하지 않으며, 자연어 요구를 해석하거나 서브에이전트를 지휘하지 않는다.

```text
사용자 요청
  -> 프로젝트 Skill
    -> 루트 Codex (오케스트레이터)
      -> 직접 작업 또는 선택적 작업 에이전트
        -> 후보 결과물
          -> 결정적 하네스
            -> GateResult: PASS | FAIL
              -> 독립 리뷰가 필요하면 읽기 전용 Reviewer
                -> 루트 Codex의 최종 완료 판정
```

## 2. 범위

### 포함

- 루트 Codex가 작업 범위, 실행 방식과 완료 조건을 조정하는 흐름
- 작업 에이전트를 사용할지 선택하는 기준
- 후보 결과물과 검증 기준 사이의 신뢰 경계
- Git 변경 범위, C# 컴파일, Unity 구조와 Play Mode를 검사하는 결정적 게이트
- 실행 상태, 결과, 로그와 증거를 보존하는 계약
- 검증 실패 후 수정과 재검증 흐름
- 결정적 검증 이후의 독립 읽기 전용 리뷰

### 제외

- 하네스가 자연어 요청을 임의의 Unity 변경 Job으로 직접 번역하는 기능
- 모든 작업에 복수 에이전트를 강제하는 정책
- 리뷰어 에이전트의 의견만으로 성공을 판정하는 구조
- 하네스가 gameplay 규칙이나 사용자 요구사항을 대신 결정하는 기능
- 외부 프로그램이 Codex를 다시 실행해 전체 작업을 소유하는 구조
- 무인 장기 실행 서비스와 원격 배포 자동화

## 3. 용어와 책임

| 구성요소 | 책임 | 소유하지 않는 것 |
|---|---|---|
| 사용자 | 의도, 제품 요구, 중요한 선택과 권한 제공 | 기술 검증 구현 |
| 프로젝트 Skill | 루트 Codex가 따라야 할 작업 절차, 문서 routing, 위임·검증·완료 규칙 | 실제 검증 결과, gameplay 구현 |
| 루트 Codex | 요구 해석, 범위 확정, 작업 분해, 실행 방식 선택, 결과 취합, 재작업과 최종 판정 | 결정적 검증 결과의 위조·우회 |
| 작업 에이전트 | 할당된 범위의 조사 또는 구현과 증거 보고 | 전체 완료 판정, 게이트 기준 변경 |
| Unity 작업 어댑터 | 허용된 원자적 Unity 편집을 결정적으로 실행 | 자연어 해석, 장기 계획, 완료 판정 |
| 하네스 | 정책 검사, 검증 실행, 증거 수집, `PASS`/`FAIL` 반환 | 작업 분해, 에이전트 지휘, 설계 선택 |
| Reviewer | 결정적 검사로 찾기 어려운 구조·책임·누락을 독립적으로 검토 | 결정적 게이트 대체, 직접 수정, 최종 판정 |

## 4. 필수 설계 결정

### HA-001: 루트 Codex가 유일한 오케스트레이터다

프로젝트 Skill로 호출된 루트 Codex가 현재 대화의 사용자 의도와 권한을 보존한다. 외부 .NET 프로그램, Unity Editor 창이나 Batch 진입점은 오케스트레이터라는 이름을 사용하지 않는다. 이들은 결정적 실행기 또는 검증기다.

### HA-002: 에이전트 수는 작업에 따라 최소화한다

루트 Codex는 다음 순서로 가장 작은 실행 방식을 선택한다.

1. 결정적 도구만으로 끝낼 수 있으면 도구를 사용한다.
2. 한 작업자가 안전하고 충분하면 직접 수행하거나 작업자 한 명을 사용한다.
3. 독립 조사, 분리된 파일 소유권 또는 전문 검토가 실제 이익을 줄 때만 복수 에이전트를 사용한다.

복수 에이전트 사용 자체는 성공 조건이 아니다. 공식 OpenAI 문서도 멀티에이전트 하네스에서 위임 시기와 정도를 명시적으로 조정하도록 안내한다.

### HA-003: 작업자의 완료 보고는 후보 제출이다

작업자 또는 루트 Codex가 `완료`라고 말해도 상태는 `Candidate`일 뿐이다. 필수 결정적 게이트가 모두 통과하기 전에는 `Accepted`가 될 수 없다.

### HA-004: 하네스는 작업 결과와 독립적이어야 한다

- 작업자는 현재 Run의 gate profile, validator, expected result를 수정하지 않는다.
- 하네스 코드 자체를 변경하는 작업은 변경 전 기준선이나 별도 격리 환경에서 self-test를 수행한다.
- 검증 실행은 tracked production asset을 수정하지 않는 것을 기본으로 한다.
- Unity import 등으로 변경이 발생하면 하네스가 변경 목록을 증거로 남기고 허용 여부를 판정한다.

### HA-005: Reviewer는 확률적 품질 검사다

Reviewer도 AI이므로 결정적 게이트가 아니다. Reviewer는 `PASS`가 나온 후보만 읽기 전용으로 검토하며, 파일·근거·심각도를 포함한 finding을 반환한다. 미해결 blocking finding이 있으면 루트 Codex는 수정 후 결정적 게이트를 다시 실행한다.

### HA-006: 실패는 증거와 함께 반환한다

`FAIL`은 단순 문자열이 아니라 실패한 check ID, 기대값, 실제값, 관련 경로와 로그 위치를 포함한다. 수정 작업자는 이 증거를 입력으로 받고 원래 요구사항을 임의로 축소하지 않는다.

## 5. 신뢰 경계

```text
확률적 영역
  사용자 자연어
  Skill에 따른 루트 Codex의 계획
  작업 에이전트의 조사·코드·에셋 변경
  Reviewer의 품질 판단

------------------- Gate Boundary -------------------

결정적 영역
  허용 경로와 변경 정책
  schema와 contract 검사
  컴파일과 정적 검사
  Unity Editor 구조 검사
  Play Mode assertion
  결과 직렬화와 exit code
```

결정적 영역은 같은 repository 상태, 같은 Unity 버전과 같은 gate profile에서 같은 판정을 내야 한다. 환경 차이가 판정에 영향을 주면 `FAIL`로 가장하지 않고 `Blocked` 또는 별도 infrastructure failure로 구분한다.

## 6. 오케스트레이션 흐름

### 6.1 상태

```text
Received
  -> Scoped
    -> Working
      -> Candidate
        -> Verifying
          -> GateFailed -> Remediating -> Candidate
          -> GatePassed
            -> Reviewing (필요한 경우)
              -> Remediating -> Candidate
              -> Accepted

어느 상태에서든 권한·환경·사용자 선택이 필요하면 Blocked
사용자가 중단하면 Cancelled
```

### 6.2 루트 Codex 절차

1. 관련 프로젝트 문서와 현재 작업 트리를 읽는다.
2. 사용자 요청에서 목표, 허용 범위, 금지 범위와 관찰 가능한 완료 조건을 정리한다.
3. 직접 작업, 결정적 도구, 단일 작업자 또는 복수 작업자 중 가장 작은 방식을 선택한다.
4. 작업자를 사용할 때는 겹치지 않는 책임과 파일 범위를 할당한다.
5. 결과 보고가 아니라 실제 작업 트리에서 후보 결과물을 확인한다.
6. 요청에 맞는 gate profile을 실행한다.
7. `FAIL`이면 증거를 포함해 수정하고 모든 영향받은 게이트를 다시 실행한다.
8. 구조적 위험이 있거나 Skill이 요구하면 독립 Reviewer를 호출한다.
9. 모든 필수 게이트 통과와 미해결 blocking finding 부재를 확인한 뒤 완료를 보고한다.

### 6.3 병렬 작업 규칙

- 읽기 전용 조사와 서로 독립적인 분석은 병렬로 실행할 수 있다.
- 같은 파일이나 같은 Unity asset을 쓰는 작업은 순차 실행한다.
- 복수 쓰기 작업이 필요하면 파일 소유권을 분리하거나 격리된 worktree를 사용한다.
- 공유 작업 공간에서는 작업자별 변경 귀속을 보장할 수 없으므로 동시 쓰기를 기본으로 하지 않는다.
- 어떤 작업자도 자신이 만든 결과의 최종 수락 상태를 설정하지 않는다.

### 6.4 역할 Skill 기반 작업자

작업 에이전트는 영구 프로세스로 상주하지 않는다. 루트 Codex가 필요한 역할만 선택해 일회성 서브 에이전트를 생성하고, 역할 Skill과 현재 Run의 `WorkerAssignment`를 함께 전달한다.

| 역할 Skill | 안정적인 책임 | 주요 금지 경계 |
|---|---|---|
| `author-unity-code` | 프로젝트 문서 routing과 코드 규약을 따른 C# 후보 작성 | 그래픽 제작, 직접 scene/prefab YAML 수정, accepting gate 변경 |
| `create-project-sprites` | 승인된 동일 계열 기준 에셋에 맞춘 raster 후보와 import specification 작성 | 코드·scene 배선·직접 `.meta` YAML 수정, 최종 화풍 승인 |
| `assemble-unity-objects` | 등록된 Harness Job Tool만 사용한 Unity object 후보 조립 | 직접 Unity YAML 수정, 미지원 Tool 우회, 코드·그래픽 제작 |

역할 Skill은 반복되는 행동 규약을 소유하고, Assignment는 이번 작업의 목표·입력·쓰기 경로·허용 Tool·금지 동작·수용 조건을 소유한다. Skill은 보안 경계가 아니므로 Scope Gate, Tool policy, 실행 영수증과 actual diff가 준수 여부를 별도로 검사해야 한다.

모든 작업에 세 역할을 호출하지 않는다. 코드와 그래픽은 계약과 쓰기 경로가 분리되면 병렬화할 수 있지만, 그 결과를 참조하는 object 조립은 두 후보의 경로가 확정된 뒤 순차 실행한다.

## 7. 실행 계약

계약은 자연어를 제거하기 위한 것이 아니라 경계 사이에 필요한 사실을 잃지 않기 위한 것이다. JSON은 구현 선택이며, 동일 필드를 보존하는 다른 직렬화 형식을 사용할 수 있다.

### 7.1 RunManifest

| 필드 | 의미 |
|---|---|
| `runId` | 한 실행의 안정적인 식별자 |
| `request` | 원본 사용자 요청 |
| `baseline` | 시작 commit과 시작 시 dirty 변경 목록 |
| `scope` | 허용 경로, 금지 경로와 권한 경계 |
| `acceptance` | 관찰 가능한 완료 조건 |
| `gateProfile` | 실행할 결정적 검사 집합 |
| `reviewPolicy` | Reviewer 필요 여부와 blocking 기준 |
| `retryPolicy` | 재시도 상한과 중단 기준 |

### 7.2 WorkerAssignment / WorkerReport

Assignment는 하나의 목표, 읽을 문서, 수정 가능 경로, 금지 사항과 반환할 증거를 가진다. Report는 `changedFiles`, `checksRun`, `evidence`, `unresolved`를 반환한다. Report의 성공 값은 GateResult를 대체하지 않는다.

### 7.3 GateResult

| 필드 | 의미 |
|---|---|
| `status` | `Pass`, `Fail`, `InfrastructureError` |
| `profile` | 실행한 gate profile과 version |
| `checks` | check별 상태, 기대값과 실제값 |
| `changedFiles` | 기준선 이후 tracked 변경 |
| `artifacts` | 로그, Unity 결과와 재현 자료 |
| `startedAt` / `finishedAt` | 실행 시간 |

프로세스 종료 코드는 JSON 결과와 일치해야 한다. 결과 파일이 없거나 schema가 맞지 않으면 성공으로 간주하지 않는다.

### 7.4 ReviewResult

ReviewResult는 `verdict`와 finding 목록을 가진다. 각 finding에는 최소한 `severity`, `file`, `evidence`, `recommendation`이 필요하다. Reviewer가 근거 없이 통과를 선언해도 결정적 게이트 결과는 바뀌지 않는다.

## 8. 결정적 게이트

gate profile은 작업에 필요한 검사만 조합한다. 모든 작업에 Play Mode나 전체 회귀 검사를 강제하지 않는다.

| 게이트 | 책임 |
|---|---|
| Baseline Gate | 시작 commit, 기존 dirty 변경과 도구 버전을 기록 |
| Scope Gate | 허용 경로 밖 변경, 금지 파일과 예상하지 않은 삭제 검사 |
| Contract Gate | manifest, Job과 result schema 및 version 검사 |
| Compile Gate | 해당 C# assembly 또는 Unity script compilation 검사 |
| Static Asset Gate | YAML, GUID, serialized reference와 알려진 정적 불변식 검사 |
| Edit Mode Gate | Unity Editor API로 scene/prefab/component 구조 검사 |
| Play Mode Gate | runtime assertion, 오류 로그, timeout과 정확한 발생 횟수 검사 |
| Evidence Gate | 필수 결과·로그 누락과 exit code 불일치 검사 |

검증기는 가능한 한 읽기 전용이어야 한다. 테스트용 임시 asset이나 scene이 필요하면 격리 경로 또는 임시 작업 복사본을 사용하고 정상·실패 종료 모두에서 정리한다.

## 9. Unity 작업 어댑터

원자적 Unity Tool과 Job은 하네스 전체가 아니라 선택 가능한 실행 어댑터다.

```text
루트 Codex가 결정한 구체 작업
  -> 필요한 경우 제한된 Harness Job 작성
    -> schema와 policy 선검사
      -> Unity 원자 Tool 실행
        -> 후보 asset 생성
          -> 별도 gate profile로 검증
```

- Tool은 한 가지 Unity 변경만 수행한다.
- 허용 경로, component와 shader 목록은 실행 전에 검사한다.
- 같은 상태는 성공적인 no-op으로 처리할 수 있다.
- 기존 값 overwrite는 사용자 권한과 RunManifest 범위를 모두 만족해야 한다.
- C# 작성 후 domain reload가 필요하면 job hash를 포함한 checkpoint로 재개한다.
- Job 실행 성공은 결과물 검증 성공을 의미하지 않는다.

## 10. Reviewer 흐름

Reviewer는 다음 자료만 받아 독립적으로 검토한다.

- 원본 사용자 요청과 확정된 scope
- 관련 구조 문서
- 실제 diff 또는 후보 asset 목록
- GateResult와 핵심 로그

Reviewer는 구현자의 추론, 예상 답변이나 의심되는 결론을 미리 받지 않는다. 리뷰 중 파일을 수정하지 않으며, finding만 반환한다. 수정이 발생하면 이전 GateResult는 만료되고 관련 결정적 게이트를 다시 실행한다.

## 11. 실패, 재시도와 중단

- 재시도 횟수는 무한하지 않으며 RunManifest의 `retryPolicy`가 소유한다.
- 같은 원인의 실패가 반복되면 증거를 보존하고 `Blocked`로 전환한다.
- 사용자 선택, 새 권한 또는 범위 확대가 필요하면 오케스트레이터가 임의로 진행하지 않는다.
- timeout, Unity 설치 누락, 프로젝트 lock과 validator crash는 후보 결과물 실패와 구분한다.
- 부분 성공은 최종 성공으로 승격하지 않는다.
- 검증 도중 새 변경이 생기면 기준선을 갱신하지 않고 오염으로 보고한다.

## 12. 실행 증거와 보존

실행 증거는 Git에 커밋하지 않는 `.harness-runs/<runId>/` 아래에 저장한다.

```text
run.json
assignments/
worker-reports/
gate-results/
reviews/
logs/
```

최종 보고는 최소한 변경 파일, 실행한 gate profile, 각 결과, 남은 finding과 로그 경로를 포함한다. 비밀값과 전체 환경 변수는 기록하지 않는다.

## 13. 플랫폼 경계

- 목표 CLI는 macOS와 Windows에서 같은 contract와 exit code를 제공한다.
- `.cmd`는 Windows 편의 wrapper일 수 있지만 유일한 진입점이 될 수 없다.
- 빌드에 필요한 project manifest는 Git에 추적되어 새 checkout에서도 실행 가능해야 한다.
- Unity 실행 파일 검색은 명시적 override와 플랫폼별 기본 위치를 지원한다.
- Unity Editor가 열린 상태의 interactive 흐름과 닫힌 상태의 batch 흐름은 같은 GateResult 계약을 사용한다.

## 14. 현재 구현 판정

### 유지 가능한 기반

- `Assets/Editor/NpcHarness/Core`: Job 검증, Tool registry, policy, checkpoint와 domain reload 재개
- `Assets/Editor/NpcHarness/Tools`: 제한된 원자적 Unity 편집 Tool
- `Assets/Editor/NpcHarness/Batch`: Unity Batch 진입점과 결과 파일 작성
- `Assets/Editor/NpcHarness/Verification`: 구조·Play Mode 검사 패턴
- `HarnessBeacon`: 하네스 실행기와 검증기의 self-test fixture

### 구현된 전환

- `HarnessJob`은 전체 사용자 작업 계획이 아니라 선택적 Unity 작업 어댑터 계약으로 제한한다.
- `HarnessGateResult`를 Job 실행 결과와 분리하고 check별 증거를 포함했다.
- 구조와 Play Mode는 Batch와 interactive에서 같은 GateResult 판정을 사용한다.
- `Tools/NpcHarness`는 자연어를 받지 않고 `verify`, `run-adapter`, `self-test`만 실행한다.
- `.codex/skills/orchestrate-unity-work`가 직접·단일 worker·선택적 복수 worker, bounded remediation과 Reviewer 호출을 조정한다.
- `.codex/skills/reviewing-unity-candidate`가 결정적 Pass 뒤 독립 read-only review를 수행한다.

### 제거·대체 완료

- `Tools/NpcHarness/HarnessOrchestrator`의 Codex 재호출과 2분류 WorkOrder
- `CreateHarnessBeacon`으로 고정된 자연어 분류와 Job 생성
- Windows `cmd.exe`, `codex.cmd`, `Unity.exe`만 전제하는 실행 흐름
- Git에서 제외되어 새 checkout에 없는 `.csproj`에 의존하는 launcher
- Job 실행 성공과 최종 결과 수락을 같은 의미로 취급하는 이름과 문구

외부 runner는 오케스트레이터가 아니다. 오케스트레이터는 루트 Codex이며, runner는 GateResult와 process evidence를 검증하는 결정적 실행기다.

### 남은 환경 검증과 확장

- 열린 Unity Editor 때문에 외부 Batch가 실제 GateResult 파일을 생성하는 end-to-end 실행은 아직 검증하지 않았다.
- Windows wrapper와 Unity 탐색은 동일 계약으로 구현했지만 Windows 실기 실행은 검증하지 않았다.
- 현재 production 기능별 gate profile은 아직 없으며 HarnessBeacon은 self-test profile이다.
- RunManifest와 review 기록의 완전한 machine-readable 저장은 후속 실제 작업 적용에서 검증한다.

## 15. 목표 배치

```text
.codex/skills/orchestrate-unity-work/
  SKILL.md                 루트 Codex 오케스트레이션 규칙
  references/              contract와 review 기준

.codex/skills/reviewing-unity-candidate/
  SKILL.md                 독립 read-only Reviewer
  references/              ReviewResult 계약

.codex/skills/author-unity-code/
  SKILL.md                 C# 후보 작업자 역할 규약

.codex/skills/create-project-sprites/
  SKILL.md                 raster 후보 작업자와 art-family routing
  references/              현재 프로젝트 art reference 선택 규칙

.codex/skills/assemble-unity-objects/
  SKILL.md                 Tool-only Unity object 조립 작업자
  references/              현재 adapter Tool·allowlist 계약

Tools/NpcHarness/
  tracked project file     cross-platform gate runner
  Schemas/                 RunManifest, GateResult 등

Assets/Editor/NpcHarness/
  Core/                    Unity 실행 contract와 policy
  Tools/                   선택적 원자 편집 어댑터
  Verification/            Edit/Play Mode 결정적 gate
  Batch/                   Unity process entry point

.harness-runs/             Git 제외 실행 증거
```

NPC/worker production 기능에는 기존 `implement-npc-feature`의 planning·approval 규칙을 우선하고, 일반 Unity 후보의 orchestration과 acceptance envelope로 새 Skill을 사용한다.

## 16. 구현 순서와 단계 종료 조건

1. **경계 고정**: 이 문서와 현재 prototype의 역할 차이가 문서 routing에 반영된다.
2. **결정적 GateResult**: Codex 없이 HarnessBeacon 정상·의도적 실패를 재현 가능하게 판정한다.
3. **Cross-platform runner**: 새 checkout의 macOS와 Windows에서 같은 명령 계약을 제공한다.
4. **단일 작업 흐름**: 루트 Codex가 한 후보를 만들고 하네스 결과에 따라 완료 또는 수정한다.
5. **독립 리뷰**: 결정적 통과 후 읽기 전용 Reviewer와 blocking finding 흐름을 검증한다.
6. **선택적 멀티에이전트**: 독립 작업에만 위임하고 파일 충돌·증거 누락을 검출한다.
7. **회귀 평가**: 정상 fixture와 오염 fixture 집합으로 하네스 자체의 거짓 통과를 검사한다.

각 단계는 이전 단계의 종료 조건을 통과한 뒤 진행한다. 세부 파일 변경과 검증 시나리오는 별도 구현 계획이 소유한다.

## 17. 불변 규칙

- 하네스는 에이전트를 지휘하지 않는다.
- 오케스트레이터는 결정적 게이트를 우회하지 않는다.
- 작업자의 완료 보고는 검증 증거가 아니다.
- Reviewer 의견은 결정적 GateResult를 덮어쓰지 않는다.
- 검증 기준은 검증 대상과 같은 Run에서 작업자가 변경하지 않는다.
- 후보가 바뀌면 관련 GateResult는 만료된다.
- 허용되지 않은 변경과 결과 파일 누락은 성공으로 간주하지 않는다.
- 새로운 에이전트 수, Tool 수 또는 검사 수 자체를 성숙도의 지표로 사용하지 않는다.

## 18. 알려진 제약과 TBD

- 기존 Unity Tool을 production 경로까지 확장할지, TestOnly 전용으로 유지할지는 구현 계획에서 결정한다.
- production 기능별 gate profile과 기본 retry budget은 작업 scope에서 정한다.
- Unity license·Package Manager·cold start 실패를 재현 가능하게 격리하는 방식은 미정이다.
- 작업자별 쓰기 격리에 worktree를 강제할지, 기본 순차 실행으로 충분한지는 실제 수직 흐름 후 결정한다.
- 하네스 자체 변경의 trusted baseline을 commit, 별도 checkout 또는 release artifact 중 무엇으로 둘지는 미정이다.

## 19. 관련 문서

- [Project Structure](ProjectStructure.md)
- [Project Architecture](ARCHITECTURE.md)
- [NPC Harness Implementation Plan](Plans/NPC_Harness_Implementation_Plan.md)
- [PublicMD 안내](README.md)
- [NPC Harness prototype README](../Tools/NpcHarness/README.md)
- [OpenAI model guidance — multi-agent orchestration and delegation](https://developers.openai.com/api/docs/guides/latest-model)

## 20. 갱신 조건

- 오케스트레이터 주체, Skill 경계 또는 위임 정책이 바뀌면 갱신한다.
- RunManifest, GateResult, ReviewResult의 의미가 바뀌면 갱신한다.
- 결정적 게이트의 신뢰 경계나 완료 판정 규칙이 바뀌면 갱신한다.
- 실제 구현이 목표 구조에 도달하면 `현재 구현 판정`을 코드 사실에 맞춰 갱신한다.
- 세부 구현 순서만 바뀌면 이 문서가 아니라 활성 구현 계획을 갱신한다.
