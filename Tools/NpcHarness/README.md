# NPC Harness

> 현재 상태: 결정적 cross-platform gate runner와 제한된 Unity 작업 adapter입니다.
>
> 루트 Codex가 오케스트레이터이며, 이 프로그램은 자연어를 해석하거나 에이전트를 지휘하지 않습니다. 전체 책임 경계는 [Codex 작업 하네스 아키텍처](../../PublicMD/HARNESS_ARCHITECTURE.md)를 기준으로 합니다.

`Tools/NpcHarness`는 명시적인 명령만 실행하고 Unity가 만든 결과 계약과 process exit code를 검증합니다. 이전의 Codex 재호출, 자연어 2분류와 고정 WorkOrder 생성 경로는 제거했습니다.

## 구조

```text
사용자 요청
  -> 루트 Codex + project Skill: scope, worker, candidate 조정
  -> Tools/NpcHarness: 명시적인 gate 또는 adapter 실행
  -> Assets/Editor/NpcHarness: Unity 구조·PlayMode 검증 또는 제한된 Tool 실행
  -> GateResult: check별 expected/actual과 Pass/Fail/InfrastructureError
  -> 필요한 경우 독립 read-only Reviewer
```

`Assets/Editor/NpcHarness`의 Tool은 각각 한 가지 작업만 수행합니다.

- `WriteCSharpScript`
- `EnsureScene`
- `EnsureGameObject`
- `SetTransform`
- `EnsureComponent`
- `EnsureMaterial`
- `ConfigureCamera`
- `ConfigureLineRenderer`
- `SaveScene`

여러 Tool의 순서가 있는 조합을 Job 또는 Recipe라고 합니다. 첫 Recipe는 다음 결과를 만드는 HarnessBeacon입니다.

- `Assets/TestOnly/HarnessTest.cs`
- `Assets/TestOnly/HarnessTest.unity`
- 상향 화살표 모양의 `HarnessBeacon`
- Play Mode에서 정확히 한 번 출력되는 `HarnessSuccess`

구조 검증은 `HarnessBeacon.Structure`, Play Mode 검증은 `HarnessBeacon.PlayMode` gate profile과 `Schemas/gate-result.schema.json`을 사용합니다. `HarnessGateResult`는 Job 실행 성공과 결과물 검증 성공을 분리하고 check별 기대값·실제값을 기록합니다. Play Mode gate는 구조 check를 선행하고, 1초 관찰 구간 동안 `HarnessSuccess` 정확히 1회와 Error·Assert·Exception 0회를 요구합니다.

## 가드레일

- 기본 변경 허용 범위는 `Assets/TestOnly`입니다.
- 컴포넌트와 Shader는 명시적인 허용목록을 사용합니다.
- 동일한 상태는 성공적인 no-op으로 처리합니다.
- 다른 기존 값은 `--allow-overwrite`나 Editor 창의 명시적 체크 없이는 변경하지 않습니다.
- 새 C# 파일 작성 후에는 체크포인트를 저장하고 Unity 컴파일이 끝난 뒤 재개합니다.
- Job JSON 계약은 `Schemas/harness-job.schema.json`에 있습니다.

## Editor에서 실행

Unity 메뉴에서 `Tools > NPC Harness`를 엽니다.

- `Tools`: 원자적 Tool의 JSON 입력을 Validate 또는 Execute합니다.
- `Recipes`: HarnessBeacon 전체 Job을 미리보기·실행·검증합니다.

Unity Editor가 열려 있을 때는 이 창을 사용합니다.

## 외부 runner에서 실행

Unity Editor를 닫은 뒤 저장소 루트에서 실행합니다. 현재 runner target은 `net10.0`이므로 .NET 10 SDK가 필요하며 외부 NuGet package는 사용하지 않습니다.

```sh
./run-harness.sh self-test
./run-harness.sh verify --profile beacon-structure
./run-harness.sh verify --profile beacon-playmode
```

```powershell
run-harness.cmd self-test
run-harness.cmd verify --profile beacon-structure
run-harness.cmd verify --profile beacon-playmode
```

제한된 Unity Job adapter를 실행합니다.

```sh
./run-harness.sh run-adapter --job path/to/harness-job.json
```

기존 관리 대상의 다른 값을 덮어쓰는 것은 사용자가 다음 옵션을 직접 추가해야 합니다.

```sh
./run-harness.sh run-adapter --job path/to/harness-job.json --allow-overwrite
```

공통 옵션은 `--unity`, `--run-id`, `--output`, `--timeout`입니다. Unity 위치는 명시적 `--unity`, `NPC_HARNESS_UNITY_PATH`, 플랫폼별 Unity Hub 기본 경로 순으로 결정합니다.

종료 코드는 `0` 성공, `1` candidate/adapter 실패, `2` infrastructure failure, `64` 잘못된 사용법입니다. 결과 파일 누락·손상, 빈 check, top-level/check 불일치, 요청한 run/profile/version 불일치, read-only gate의 변경 보고와 exit/result 불일치는 모두 infrastructure failure로 거부합니다.

실행 기록은 Git에서 제외된 `.harness-runs/<runId>/`에 저장됩니다. 같은 프로젝트를 Unity Editor가 열고 있으면 batch runner는 lock을 infrastructure failure로 보고 실행하지 않습니다. 이때는 Editor 창을 사용하거나 Editor를 닫고 외부 runner를 실행합니다.
