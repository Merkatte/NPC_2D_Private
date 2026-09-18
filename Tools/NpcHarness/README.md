# NPC Harness

> 현재 상태: 결정적 cross-platform gate runner와 제한된 Unity 작업 adapter입니다.
>
> 루트 Codex가 오케스트레이터이며, 이 프로그램은 자연어를 해석하거나 에이전트를 지휘하지 않습니다. 전체 책임 경계는 [Codex 작업 하네스 아키텍처](../../PublicMD/HARNESS_ARCHITECTURE.md)를 기준으로 합니다.

`Tools/NpcHarness`는 명시적인 명령만 실행하고 Unity가 만든 결과 계약과 process exit code를 검증합니다. 이전의 Codex 재호출, 자연어 2분류와 고정 WorkOrder 생성 경로는 제거했습니다.

## FarmerTest·GuardTest에서 일반 작업하기

[SceneWork.md](SceneWork.md)에 따라 일반 작업은 원본 프로젝트에서 직접 수행합니다.
여러 씬·프리팹의 대규모 일괄 변환처럼 영향이 큰 작업만 격리 복사본을 사용합니다.
`SceneWorkspace.ps1`은 그 선택적 격리 경로이며 원본 직접 편집 실행기가 아닙니다.
작은 씬·프리팹 속성/참조는 YAML로 직접 편집할 수 있으며 Editor 연결이 필요 없습니다.
기존 GUID/fileID와 미저장 작업을 보존하고 diff·변경 참조를 확인합니다.
열린 Unity의 API 실행에만 실제 Editor 연결/진입점이 필요합니다. 실행하지 않은 Unity
import/load/runtime 검사는 `NOT_VERIFIED`로 기록하며 코드 검증은 완화하지 않습니다.
실제 씬·프리팹 편집은 TestOnly Job allowlist에 묶이지 않습니다. MCP 패키지 설치나
기능별 전용 검사기는 필요하지 않습니다. 공통 최소 확인과 변경 기록은 필수이며,
아래 기존 v1 Job/Scope Gate/기능 profile은 선택한 실행에 그대로 유지됩니다.

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

`verify-scope`는 `SkillPolicies/*.json`의 고정 역할 경계와 run별 `WorkerAssignment`를 함께 읽어 assignment가 정책의 경로별 확장자와 Tool 범위를 좁히는지, 역할별 실행 계약과 실제 Git 변경이 일치하는지를 `GateResult`로 기록합니다. `harness-job`은 Job·component·mutation path·adapter receipt, `direct-code`는 선언된 C#·소유 문서·신규 companion `.meta`, `raster-art`는 단일 exact PNG 출력, chunk CRC·critical chunk·scanline filter를 포함한 IDAT decode 구조, 크기·alpha·frame grid와 reference `.meta` 기반 strict import spec을 검사합니다. 루트 오케스트레이터가 작업 위임 전에 assignment SHA-256을 기록하고 검증 때 `--assignment-sha256`으로 전달하므로, 작업 뒤 assignment나 baseline dirty 예외를 바꿔 금지 변경을 숨길 수 없습니다. 현재 정책은 `assemble-unity-objects`, `author-unity-code`, `create-project-sprites` v1입니다. Scope Gate는 작업별 기능·scene·시각 acceptance를 대신하지 않으며 둘 다 통과해야 합니다.

`HarnessTest.SquareCharacter.Structure`는 첫 선언형 scene profile입니다. profile identity, scene path와 20개 구조 assertion은 `Profiles/square-character-structure.json`에 있고, 계약은 `Schemas/declarative-scene-gate.schema.json`이 소유합니다. loader는 raw JSON의 필수 필드·값 종류·중복·미등록 필드를 먼저 거부하고, Unity 쪽 공통 evaluator는 `object-layout`, `exact-children`, `line-renderer-shape`, `line-renderer-material` check만 해석합니다. 같은 종류의 구조 검증은 전용 C# Gate를 새로 만들지 않고 manifest를 추가해 구성합니다.

## 실제 Farmer 씬의 읽기 전용 검사

`FarmerScene.Structure` v1 (`farmer-scene-structure`)는 저장된 `Assets/Scenes/FarmerTest.unity`를 preview scene으로 열어 검사합니다. NPCManager·WorkerPool·Farmer selector·DestinationDB·InteractableManager·FarmWorkSite·WarehouseDepositPoint·DataManager의 단일 활성 배치, Missing Script, 필수 참조, Farmer role 및 destination/provider 중복·누락, 입고 inventory의 동일 오브젝트 연결, 독립 난수원·작업 영역, 실제 scene consumer의 CropCatalog와 item CSV를 확인합니다. 시작 작물과 pool parent 같은 선택적 참조는 필수로 강제하지 않습니다.

이는 **현재 FarmerTest의 구조 계약**이며 임의의 다중 농장 씬, NPC 행동·수확/입고 transaction·시각 결과, 클릭/전투 mask 전체를 검증하지 않습니다. production 편집 권한도 확장하지 않습니다. 일반적인 구조 관찰은 `SceneWiringChecks`, 저장본 inspection lifecycle은 `SavedSceneInspection`, Farmer 전용 배선 규칙은 `FarmerSceneValidator`가 소유합니다. 기존 네 가지 선언형 도형 check schema는 그대로입니다.

검사 대상 씬이나 로드된 의존 asset에 미저장 변경이 있거나 Editor가 Play Mode·컴파일·import 중이면 `InfrastructureError`로 거부합니다. 관련 없는 dirty 씬은 보존합니다. 검사 후 preview를 닫고 열린 씬 상태와 디스크 의존 파일 SHA-256을 비교하며, 파일별 전후 해시와 Unity 버전을 `.harness-runs/<runId>/artifacts/FarmerScene.Structure-dependencies.json`에 기록합니다. 이 증거는 해당 씬의 dependency 범위이며 전체 repository 격리나 최종 후보 수락을 대신하지 않습니다.

```powershell
.\run-harness.cmd verify --profile farmer-scene-structure
```

열린 Editor에서는 `Tools > NPC Harness > Verify Farmer Scene Structure` 메뉴 또는 같은 CLI 명령의 interactive bridge를 사용합니다. 새 C#을 먼저 Unity에 import/compile해야 합니다. 메뉴는 .NET SDK 없이 사용할 수 있습니다.

## 가벼운 리뷰 기록 확인

`review-snapshot`과 `accept-review`는 기존 한 번의 독립 리뷰를 짧은 기록으로
연결한다. 컨벤션 전체를 코드로 검사하거나 리뷰 에이전트를 추가하지 않는다.
실행 순서·정확한 JSON 계약·예시는 [ReviewEvidence.md](ReviewEvidence.md)를 따른다.

```powershell
run-harness.cmd review-snapshot --request .harness-runs/<id>/review-request.json --request-sha256 <root-pinned-hash>
run-harness.cmd accept-review --request .harness-runs/<id>/review-request.json --request-sha256 <root-pinned-hash> --snapshot-sha256 <root-pinned-snapshot-hash>
```

요청은 구현 전, snapshot은 기능 검사 전에 고정한다. 선택한 inputRoots의 파일
변경·추가·삭제, 오래된 검사, 누락된 리뷰·검토 항목·artifact, 자기 리뷰 ID와
모순된 판정을 거부한다. 출력은 `Harness.ReviewEvidence` v1 GateResult다.
root가 실제 reviewer identity와 범위의 충분성을 별도로 확인해야 하며,
이 Gate는 기능·Scope Gate나 AI 판단의 정확성을 대신하지 않는다. snapshot은
입력 범위 밖 변경과 검사 사이의 일시적 변경까지 감시하는 보안 장치가 아니다.

## 편집 가드레일

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
./run-harness.sh verify --profile square-character-structure
./run-harness.sh verify --profile farmer-scene-structure
./run-harness.sh verify-scope \
  --policy Tools/NpcHarness/SkillPolicies/<role-skill>.json \
  --assignment .harness-runs/<run-id>/assignments/<assignment>.json \
  --assignment-sha256 <root-recorded-sha256> \
  --run-id <run-id>
```

```powershell
run-harness.cmd self-test
run-harness.cmd verify --profile beacon-structure
run-harness.cmd verify --profile beacon-playmode
run-harness.cmd verify --profile square-character-structure
run-harness.cmd verify --profile farmer-scene-structure
run-harness.cmd verify-scope --policy Tools/NpcHarness/SkillPolicies/<role-skill>.json --assignment .harness-runs/<run-id>/assignments/<assignment>.json --assignment-sha256 <root-recorded-sha256> --run-id <run-id>
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

실행 기록은 Git에서 제외된 `.harness-runs/<runId>/`에 저장됩니다. 같은 프로젝트를 Unity Editor가 열고 있으면 지원되는 구조 profile(Beacon, SquareCharacter, FarmerScene)은 interactive bridge로 요청합니다. 외부 Play Mode profile과 adapter는 같은 프로젝트의 batch lock을 거부하므로 Editor 전용 진입점 또는 닫힌/격리된 프로젝트를 사용합니다.
