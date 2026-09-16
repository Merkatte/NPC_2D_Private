# NPC Harness v1

NPC Harness는 확률적인 자연어 분류와 결정적인 Unity 변경을 분리하는 작은 개발 자동화 예제입니다.

## 구조

```text
자연어 요청
  -> Tools/NpcHarness: Codex 읽기 전용 의도 분류
  -> Harness Job JSON: 허용된 Tool과 고정 파라미터
  -> Assets/Editor/NpcHarness: 검증, 실행, 체크포인트, 결과 판정
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

## 외부 오케스트레이터에서 실행

Unity Editor를 닫은 뒤 저장소 루트에서 실행합니다.

```powershell
run-harness.cmd "TestOnly에 HarnessBeacon을 만들고 실행해줘"
```

변경 없이 WorkOrder만 확인합니다.

```powershell
run-harness.cmd "TestOnly에 HarnessBeacon을 만들고 실행해줘" --dry-run
```

대화형 승인을 생략합니다.

```powershell
run-harness.cmd "TestOnly에 HarnessBeacon을 만들고 실행해줘" --approve
```

기존 관리 대상의 다른 값을 덮어쓰는 것은 사용자가 다음 옵션을 직접 추가해야 합니다.

```powershell
run-harness.cmd "TestOnly에 HarnessBeacon을 만들고 실행해줘" --approve --allow-overwrite
```

실행 기록은 Git에서 제외된 `.harness-runs/`에 저장됩니다. Unity 위치가 기본 Hub 경로와 다르면 `NPC_HARNESS_UNITY_PATH`에 `Unity.exe` 전체 경로를 지정합니다.

현재 자연어 분류기가 지원하는 작업은 `CreateHarnessBeacon` 하나뿐입니다. 다른 요청은 Unity를 실행하기 전에 `Unsupported`로 종료됩니다.
