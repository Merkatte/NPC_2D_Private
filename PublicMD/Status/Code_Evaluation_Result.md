# Code Evaluation Result

## Purpose

NPC Harness v1 변경을 대상으로 책임 배치, Job·컴파일 재개 lifecycle, overwrite guard, Unity scene/asset 안전성, Play Mode 판정, JSON 계약과 유지보수성을 감사했다. 리뷰만 수행했으며 파일은 수정하지 않았다.

## Review Snapshot

- Date: 2026-09-17
- Scope:
  - `Assets/Editor/NpcHarness/**`
  - `Tools/NpcHarness/**`
  - `run-harness.cmd`
  - `.gitignore`
  - `ProjectSettings/TagManager.asset`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/Status/PROGRESS.md`
  - 직접 영향 표면: `Assets/TestOnly`, `PointerClickRouter`, Layer 9을 사용하는 Merchant/TownHall prefab
- Sources:
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ARCHITECTURE.md`
  - `PublicMD/Systems/UI.md`
  - `PublicMD/Systems/Merchant_Caravan.md`
  - `PublicMD/Systems/Town_Hall.md`
  - 기존 `PublicMD/Status/Code_Evaluation_Result.md`
  - `reviewing-npc-work-code` audit methodology
- Verification:
  - `git status`, `git diff`, `git diff --check`
  - 변경 C# 30개와 직접 의존 표면 정적 검토
  - JSON 2개 parsing
  - 전체 `Assets`의 `.meta` GUID 640개 중복 검사
  - 신규 Editor 경로의 `.meta` 누락 검사
  - generated `Assembly-CSharp-Editor.csproj`에서 Harness C# 27개 포함 확인
  - stale `HarnessSceneBuilder` 참조와 TODO/FIXME 검색
  - `.gitignore`의 `bin/`, `obj/`, `.harness-runs/` 적용 확인

## Executive Summary

외부 자연어 분류, 결정적 Job, Unity Editor mutation을 분리한 큰 책임 방향은 적절하다. 경로와 component/shader allowlist, 컴파일 전용 첫 Step, checkpoint hash, timeout 시 process-tree 종료도 좋은 안전장치다.

그러나 현재 상태는 승인할 수 없다. 대화형 실행과 검증이 기존 Editor scene setup을 `Single` 모드로 교체하면서 dirty scene을 놓칠 수 있고, `EnsureMaterialTool`은 기존 경로를 Material로 읽지 못한 경우 overwrite 승인 없이 `CreateAsset`을 호출한다. 두 경로 모두 “명시적 승인 없는 기존 값 변경 금지”라는 핵심 가드레일을 우회하며 사용자 작업을 잃게 할 수 있다.

추가로 HarnessBeacon recipe가 외부와 Editor에 중복 정의되어 있고, 공개된 Job schema와 실제 runner 검증이 일치하지 않으며, Play Mode verifier는 첫 성공 직후 종료하므로 “정확히 한 번”을 완전히 검증하지 못한다.

최종 판정은 **Changes requested**이다.

## Improvements Since Previous Review

- 기존 단일 `HarnessSceneBuilder` 참조는 제거됐고 Tool, Job runner, batch entry, verification으로 책임이 분해됐다.
- 외부 Codex 결과는 고정 action 분류에만 사용되고 실제 Unity 변경 내용은 결정적 코드로 생성된다.
- malformed TagManager 빈 scalar는 인덱스를 유지하는 명시적 빈 문자열로 정규화됐다.
- 신규 Editor asset의 `.meta`가 모두 존재하며 GUID 중복은 발견되지 않았다.
- 이전 평가의 Merchant/TownHall 기능 자체는 이번 집중 리뷰에서 재감사하지 않았다. Layer 9 무명 상태는 여전히 해당 Systems 문서에 미완 배선으로 기록돼 있다.

## Priority Coverage

| Priority | Result |
|---|---|
| 1. Architecture and responsibility placement | M-01 발견. 그 외 외부 classifier → deterministic Job → Editor mutation 방향은 적절하다. NPC/worker runtime 책임 침범은 없다. |
| 2. Correctness, lifecycle, cancellation, regression | H-01, M-03, L-01 발견. timeout 시 child process tree 종료는 적절하다. |
| 3. Unity scene, prefab, serialized-reference safety | H-01, H-02 발견. 신규 `.meta` 누락·GUID 중복은 없다. |
| 4. Convention, maintainability, dead code, magic values | M-01, M-02, L-02 발견. 확정적인 dead code, enum 재정렬, production hot-path 회귀는 발견되지 않았다. |

## Findings By Severity

### Critical

None found.

### High

#### H-01 — scene guard가 target dirty scene과 비활성 additive dirty scene을 놓쳐 사용자 변경을 닫을 수 있다

- Severity: High
- Category: Unity scene safety / Data loss
- Location:
  - `Assets/Editor/NpcHarness/Core/HarnessToolContext.cs:35-47`
  - `Assets/Editor/NpcHarness/Core/HarnessToolContext.cs:51-66`
  - `Assets/Editor/NpcHarness/Core/HarnessToolContext.cs:145-154`
  - `Assets/Editor/NpcHarness/Verification/HarnessBeaconValidator.cs:11-13`
- Evidence:
  - `RefuseToReplaceDirtyScene()`은 active scene 하나만 검사한다.
  - active scene이 dirty여도 `activeScene.path == requestedScenePath`이면 통과한다.
  - 이후 새 context는 이미 열린 target을 재사용하지 않고 `OpenSceneMode.Single`로 다시 연다.
  - active scene이 clean이면 다른 additive scene이 dirty여도 검사되지 않는다.
  - `OpenSceneMode.Single`은 현재 열린 모든 scene을 닫는다. Unity는 별도로 모든 modified scene의 저장·취소 여부를 확인하는 API를 제공한다. [Unity OpenSceneMode](https://docs.unity3d.com/kr/current/ScriptReference/SceneManagement.OpenSceneMode.html), [Unity SaveCurrentModifiedScenesIfUserWantsTo](https://docs.unity3d.com/ja/2022.3/ScriptReference/SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo.html)
  - 이 경로는 Execute뿐 아니라 `Validate Scene`에서도 사용된다.
- Description:
  - `HarnessTest.unity` 자체가 dirty인 상태나 multi-scene setup에서 비활성 scene만 dirty인 상태가 보호되지 않는다. 읽기처럼 보이는 validation도 기존 scene setup을 교체할 수 있어 기본 허용 범위인 `Assets/TestOnly` 밖의 미저장 작업에 영향을 준다.
- Recommended fix:
  - 대화형 실행 전에 모든 loaded scene을 검사하고 저장·폐기·취소 선택을 받거나 dirty scene이 하나라도 있으면 중단한다.
  - requested scene이 이미 열려 있으면 재사용하고, dirty target을 디스크에서 다시 열지 않는다.
  - 가능하면 기존 scene manager setup을 snapshot한 뒤 실행·검증 후 복원한다.
  - batch mode에서는 dirty scene이 존재하지 않는다는 전제를 명시적으로 검증한다.
- Impact if unfixed:
  - 사용자 scene의 미저장 변경이 유실될 수 있고, Validate/Execute가 명시된 안전 범위를 넘어 Editor 작업 상태를 파괴할 수 있다.

#### H-02 — 기존 `.mat` 경로를 Material로 읽지 못하면 overwrite 승인 없이 교체한다

- Severity: High
- Category: Asset overwrite guard / Serialized-reference safety
- Location:
  - `Assets/Editor/NpcHarness/Tools/EnsureMaterialTool.cs:29-45`
  - `PublicMD/Status/PROGRESS.md:55`
  - `Tools/NpcHarness/README.md:37-39`
- Evidence:
  - `LoadAssetAtPath<Material>()`이 null이면 경로가 비어 있는지 별도로 확인하지 않고 곧바로 새 Material을 만든다.
  - 이 생성 branch는 `AllowOverwrite`를 검사하지 않는다.
  - 기존 파일이 잘못된 type이거나 import에 실패해 Material로 load되지 않는 경우에도 같은 branch로 들어간다.
  - Unity 6의 `AssetDatabase.CreateAsset`은 지정 경로에 asset이 있으면 새 asset으로 overwrite한다고 명시한다. [Unity AssetDatabase.CreateAsset](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/AssetDatabase.CreateAsset.html)
- Description:
  - 현재 코드는 “Material로 load되지 않음”과 “경로가 존재하지 않음”을 같은 상태로 취급한다. 따라서 기존 managed path의 다른 값은 명시적 승인 없이는 변경하지 않는다는 문서화된 가드레일이 보장되지 않는다.
- Recommended fix:
  - `AssetDatabase.LoadMainAssetAtPath`, GUID 또는 실제 파일 존재 여부로 경로 점유를 먼저 확인한다.
  - 경로가 점유됐지만 Material로 읽을 수 없으면 기본적으로 실패하고, 명시적 overwrite 승인 시에만 교체한다.
  - 교체 시 기존 asset type/path를 포함한 진단을 남기고 interactive Undo 또는 복구 가능한 백업 정책을 정의한다.
- Impact if unfixed:
  - `Assets/TestOnly` 안의 기존 asset 내용이 승인 없이 파괴되고, 같은 `.meta` GUID를 참조하는 scene/component가 전혀 다른 Material을 받게 될 수 있다.

### Medium

#### M-01 — HarnessBeacon recipe가 외부 orchestrator와 Editor에 이중 소유된다

- Severity: Medium
- Category: Architecture / Responsibility duplication
- Location:
  - `Assets/Editor/NpcHarness/Recipes/HarnessBeaconRecipe.cs:5-162`
  - `Tools/NpcHarness/HarnessOrchestrator.cs:9-12`
  - `Tools/NpcHarness/HarnessOrchestrator.cs:215-282`
  - `Assets/Editor/NpcHarness/Verification/HarnessBeaconValidator.cs:13-34`
- Evidence:
  - script source, scene/material path, hierarchy, camera 값, LineRenderer 값과 Step 순서가 두 코드에 별도로 작성돼 있다.
  - Editor UI는 `HarnessBeaconRecipe.Create()`를 사용하지만 CLI는 `WriteHarnessBeaconJob()`의 독립 복사본을 사용한다.
  - validator와 Play Mode verifier는 Editor-side recipe 상수를 기준으로 판정한다.
- Description:
  - 현재 값은 일치하지만 한쪽 recipe만 변경하면 Editor UI와 CLI가 다른 scene을 만들거나, CLI가 성공적으로 만든 결과를 Editor validator가 거부할 수 있다. 개발 자동화의 결정적 계약에 두 개의 변경 지점이 생겼다.
- Recommended fix:
  - versioned canonical recipe JSON/template을 두 front end가 함께 읽거나, 공통 pure recipe generator를 공유한다.
  - CLI가 Job JSON을 출력해야 한다는 경계는 유지하되 Job 내용의 소유자는 하나로 만든다.
  - canonical recipe와 schema를 함께 검증하는 drift test를 추가한다.
- Impact if unfixed:
  - 향후 Tool·recipe 확장에서 CLI와 Editor 실행 결과가 조용히 분기하고, verifier 실패가 실제 기능 오류인지 정의 drift인지 구분하기 어려워진다.

#### M-02 — 공개된 Job schema와 runner가 실제로 수락하는 JSON 계약이 다르다

- Severity: Medium
- Category: Contract enforcement / Input validation
- Location:
  - `Tools/NpcHarness/Schemas/harness-job.schema.json:5-170`
  - `Assets/Editor/NpcHarness/Core/HarnessJobStorage.cs:20-30`
  - `Assets/Editor/NpcHarness/Core/HarnessJobRunner.cs:133-196`
  - `Assets/Editor/NpcHarness/Tools/ConfigureLineRendererTool.cs:9-36`
  - `Assets/Editor/NpcHarness/UI/HarnessEditorWindow.cs:202-220`
- Evidence:
  - runner는 schema를 참조하지 않고 `JsonUtility.FromJson`으로 직접 역직렬화한다.
  - schema는 required field와 `additionalProperties: false`를 정의하지만 `JsonUtility`는 누락값을 기본값으로 채우고 알 수 없는 field를 거부하지 않는다.
  - schema는 `capVertices`와 `cornerVertices`를 0 이상으로 제한하지만 runtime validator는 points와 width만 검사한다.
  - `active`, vector/color 구성요소 등 schema-required 값의 존재 여부도 runtime에서 강제되지 않는다.
- Description:
  - schema를 통과한 Job과 runner가 수락하는 Job이 동일 집합이라는 보장이 없다. 현재 고정 orchestrator 출력은 유효하지만 batch entry나 Editor 수동 Tool 입력은 문서화된 계약 밖의 JSON도 실행할 수 있다.
- Recommended fix:
  - 역직렬화 전에 schema를 실제로 검증하거나, strict DTO parser와 명시적 required-field 검증을 사용한다.
  - schema와 runtime validator의 제약을 단일 소스에서 생성하거나 parity test로 고정한다.
  - 모든 수치에 finite/range 검증을 적용한다.
- Impact if unfixed:
  - 잘못된 Job이 예상치 못한 기본값으로 실행되거나 schema 소비자와 Unity runner가 서로 다른 결과를 내며, partial mutation 뒤에야 실패할 수 있다.

#### M-03 — Play Mode verifier가 첫 성공 직후 종료해 “정확히 한 번”을 완전히 검증하지 못한다

- Severity: Medium
- Category: Verification correctness / Lifecycle
- Location:
  - `Assets/Editor/NpcHarness/Verification/HarnessPlayModeVerifier.cs:65-73`
  - `Assets/Editor/NpcHarness/Verification/HarnessPlayModeVerifier.cs:75-93`
  - `Assets/Editor/NpcHarness/Verification/HarnessPlayModeVerifier.cs:103-118`
  - `PublicMD/Status/PROGRESS.md:59`
- Evidence:
  - `SuccessCount > 0`이 되는 첫 Editor update에서 즉시 Play Mode 종료를 요청한다.
  - `successCount != 1` 판정은 이미 종료된 뒤 실행된다.
  - 첫 로그 이후 다음 frame이나 지연 callback에서 발생할 두 번째 로그는 관찰하기 전에 검증이 끝난다.
  - log handler는 `LogType`이나 실제 Play Mode 진입 상태를 확인하지 않아 같은 문자열의 warning/error 또는 전환 중 editor log도 count할 수 있다.
- Description:
  - 현재 생성되는 `HarnessTest.Start()`는 한 번만 로그하므로 정상 recipe에는 맞지만 verifier 자체는 문서가 주장하는 “30초 안에 정확히 한 번”을 일반적으로 증명하지 못한다.
- Recommended fix:
  - 첫 성공 후 명시적인 observation window를 유지하고 그동안 두 번째 로그가 나오면 즉시 실패한다.
  - 최소한 `LogType.Log`와 실제 Play Mode 상태를 확인한다.
  - 가능하면 전역 문자열 대신 검증 대상 component의 명시적 signal이나 고유 run token을 사용한다.
- Impact if unfixed:
  - 반복 로그 또는 잘못된 출처의 로그가 있어도 Play Mode 검증이 거짓 성공할 수 있다.

### Low

#### L-01 — result 파일 쓰기 실패가 batch 종료 경로 자체를 중단시킬 수 있다

- Severity: Low
- Category: Failure handling / Process lifecycle
- Location:
  - `Assets/Editor/NpcHarness/Batch/HarnessResultWriter.cs:7-21`
  - `Assets/Editor/NpcHarness/Batch/HarnessBatchRunner.cs:23-43`
  - `Assets/Editor/NpcHarness/Verification/HarnessPlayModeVerifier.cs:119-133`
  - `Assets/Editor/NpcHarness/Verification/HarnessPlayModeVerifier.cs:170-187`
- Evidence:
  - result writer는 임시 파일 없이 대상에 직접 쓴다.
  - batch runner의 catch는 동일 writer와 동일 path를 다시 호출한 뒤에야 `EditorApplication.Exit(1)`에 도달한다.
  - play verifier callback도 writer 예외를 보호하는 `finally` 없이 그 다음 줄에서 종료한다.
  - Unity 명령에는 `-quit`가 없어 명시적 Exit에 도달하지 못하면 외부 timeout까지 남을 수 있다.
- Description:
  - disk-full, permission, invalid path 같은 result-reporting 오류가 원래 실패를 가리고 batch process 종료를 지연시킨다.
- Recommended fix:
  - process 종료를 `finally`에서 보장하고 result 기록 실패는 별도 Console 오류로 남긴다.
  - result는 같은 directory의 임시 파일에 쓴 뒤 atomic replace/move한다.
- Impact if unfixed:
  - 실패 시 2~5분 timeout까지 Unity가 프로젝트 lock을 유지하고, 원래 오류 대신 “result 없음/timeout”만 남을 수 있다.

#### L-02 — v0/v1 표기와 구조 문서 기준일이 일치하지 않는다

- Severity: Low
- Category: Documentation drift
- Location:
  - `Tools/NpcHarness/README.md:1`
  - `PublicMD/Status/PROGRESS.md:53`
  - `Tools/NpcHarness/HarnessOrchestrator.cs:58,167`
  - `PublicMD/ProjectStructure.md:3,83-84`
- Evidence:
  - README와 PROGRESS는 “NPC Harness v1”이라고 기록한다.
  - CLI 오류와 classifier prompt는 여전히 “v0 harness”라고 출력한다.
  - `ProjectStructure.md`는 2026-09-17 책임 항목이 추가됐지만 기준일은 2026-09-06이다.
- Description:
  - 실행 로그와 문서가 서로 다른 계약 버전을 가리킨다.
- Recommended fix:
  - version 상수를 한 곳에서 관리하고 CLI, prompt, schema title, README를 맞춘다.
  - 구조 문서 기준일을 실제 갱신일에 맞춘다.
- Impact if unfixed:
  - 실패 로그와 Job/schema 호환성을 해석할 때 사용자가 어느 계약이 현재인지 혼동할 수 있다.

## Findings By File

- `HarnessToolContext.cs`
  - 경로 제한과 hierarchy 중복 검출은 적절하다.
  - H-01의 loaded-scene 전체 보호와 target scene 재사용이 필요하다.
- `EnsureMaterialTool.cs`
  - shader allowlist와 값 비교는 적절하다.
  - H-02의 occupied-path/type-mismatch 분기를 먼저 처리해야 한다.
- `HarnessJobRunner.cs` / `HarnessJobStorage.cs`
  - compile Step을 첫 위치로 제한하고 checkpoint job/path/hash/index를 검증한다.
  - M-02의 strict JSON 계약은 보장하지 않는다.
- `HarnessBeaconRecipe.cs` / `HarnessOrchestrator.cs`
  - 현재 recipe 값은 서로 일치한다.
  - M-01의 이중 소유가 후속 변경 drift를 만든다.
- `HarnessPlayModeVerifier.cs`
  - SessionState로 domain reload를 견디고 callback을 대칭 해제한다.
  - M-03의 observation window와 L-01의 exit 보장이 필요하다.
- `HarnessBatchRunner.cs` / `HarnessResultWriter.cs`
  - 성공·실패 exit code 구분은 명확하다.
  - result write 자체의 실패가 종료를 막는 L-01이 있다.
- `HarnessEditorWindow.cs`
  - Tool/Recipe UI 책임이 gameplay UI와 분리됐다.
  - Editor update마다 SessionState JSON을 다시 parsing하는 비용은 존재하지만 Editor-only 범위라 finding으로 승격하지 않았다.
- `ProjectSettings/TagManager.asset`
  - `- ""` 변경은 빈 layer index를 명시적으로 보존하는 문법 교정이다.
  - Layer 9 자체는 여전히 무명이며 Merchant/TownHall 문서가 기록한 기존 미완 click wiring은 해결하지 않는다.

## Cross-Cutting Findings

- Harness는 NPC selector/action/provider/runtime에 의존하지 않으며 production worker 책임을 침범하지 않는다.
- 기본 mutation root는 `Assets/TestOnly`로 제한되지만 H-01 때문에 Editor의 열린 scene 상태까지는 같은 경계로 제한되지 않는다.
- `WriteCSharpScript`의 source는 일반 문자열이므로 Batch entrypoint 자체는 임의 TestOnly C#을 작성할 수 있다. 현재 외부 자연어 경로는 고정 source만 생성하므로 별도 finding으로 보지 않았지만, entrypoint를 다른 자동화가 소비할 경우 신뢰 경계를 문서화해야 한다.
- Layer 9은 MerchantCaravan과 TownHall prefab에서 사용되지만 아직 이름과 `_clickableMask` scene 배선이 없다. 이는 이번 Harness 변경이 새로 만든 회귀가 아니라 Systems 문서에 기록된 기존 통합 작업이다.
- compile checkpoint 이후 artifact 자체의 hash는 다시 확인하지 않는다. 현재 고정 source와 즉시 재실행 경로에서는 낮은 위험이지만, 향후 사용자 편집을 허용하면 checkpoint output integrity도 검증해야 한다.

## Positive Notes

- 자연어 분류기는 read-only Codex 과정으로 격리되고 결과 action도 `CreateHarnessBeacon`/`Unsupported`로 제한된다.
- external orchestrator가 LLM 생성 C#을 직접 쓰지 않고 고정 Job만 만든다.
- asset path는 `Assets/TestOnly` 밖으로 탈출하는 `..`와 다른 root를 거부한다.
- component와 shader allowlist가 실행 시에도 적용된다.
- `WriteCSharpScript`를 첫 Step으로 제한해 compile/domain reload 전에 scene 변경이 일어나지 않게 했다.
- checkpoint는 job ID, absolute job path, job JSON hash와 next index를 검증한다.
- timeout 시 child process tree를 종료하고 stdout/stderr를 별도 기록한다.
- callback subscription은 중복 제거 후 등록하며 완료 시 해제한다.
- scene hierarchy lookup은 이름 중복을 조용히 선택하지 않고 실패한다.
- 기존 값과 requested 값이 같으면 대부분 명확한 `NoChange`를 반환한다.
- `git diff --check`가 통과했다.
- Harness schema 2개는 JSON으로 정상 parsing된다.
- 전체 asset `.meta` GUID 640개 중 중복은 0건이다.
- 신규 `Assets/Editor/NpcHarness` 경로에 `.meta` 누락은 없다.
- `.gitignore`는 `Tools/NpcHarness/bin`, `obj`, `.harness-runs`를 정상 제외한다.
- 이전 `HarnessSceneBuilder` 잔존 참조는 없다.
- generated `Assembly-CSharp-Editor.csproj`에 신규 Editor C# 27개가 모두 포함된다.

## Verification Limits

- read-only sandbox이므로 파일을 수정하지 않았다.
- `dotnet build`는 `bin/obj`를 변경하므로 독립 재실행하지 않았다. 구현 기록의 0 warnings/0 errors 결과와 기존 build artifact는 확인했지만 콘솔 결과 자체를 재현하지 않았다.
- Unity Editor import, interactive compilation resume, Batch entrypoint와 Play Mode를 실행하지 않았다.
- 원본 프로젝트가 열려 있었고 격리 복사본은 Package Manager 단계에서 실패했다는 구현 기록을 확인했다. 실제 scene 생성, 두 번째 idempotent 실행과 `HarnessSuccess` 판정은 `NOT VERIFIED`다.
- H-01과 H-02의 Unity API 계약은 Unity 6 공식 문서와 대조했지만 실제 데이터 손실 시나리오는 실행하지 않았다.
- `TagManager.asset`의 Unity importer 수용 여부는 정적 YAML과 reported isolated-start 관찰까지만 확인했다.
- 기존 Merchant/TownHall gameplay 기능과 과거 평가 finding은 이번 집중 범위에서 재검증하지 않았다.

## Recommended Next Actions

1. H-01을 먼저 해결해 dirty target scene, clean active + dirty additive scene, 취소 선택을 모두 검증한다.
2. H-02의 occupied-path 검사를 추가하고 잘못된 type·import 실패 asset이 승인 없이 교체되지 않는지 검증한다.
3. HarnessBeacon recipe를 단일 canonical source로 통합한다.
4. schema와 runner validation의 parity를 맞추고 missing/unknown/range 오류 Job에 대한 EditMode 검증을 추가한다.
5. Play Mode verifier에 명시적 observation window와 log source/type 제한을 추가한다.
6. result write 실패와 process exit를 분리해 모든 batch 경로가 즉시 종료되게 한다.
7. v0/v1 표기와 구조 문서 기준일을 통일한다.
8. Unity에서 최초 실행, compile resume, 두 번째 no-op 실행, overwrite 거부, overwrite 승인, compilation 실패, Play Mode timeout을 end-to-end로 검증한다.
9. Harness와 별개로 Layer 9을 `Clickable`로 명명하고 Merchant/TownHall의 `_clickableMask` scene 배선을 완료한다.

## Final Verdict

**Changes requested.**

전체 책임 방향과 결정적 orchestration 설계는 타당하며 NPC/worker architecture에 대한 침범도 없다. 그러나 scene setup 교체와 Material 생성에서 두 개의 High 안전성 결함이 명시적 overwrite 가드를 우회한다. 이 결함을 해결하고 실제 Unity compilation-resume 및 Play Mode 전체 흐름을 검증하기 전에는 도구를 안전한 v1 구현으로 승인할 수 없다.