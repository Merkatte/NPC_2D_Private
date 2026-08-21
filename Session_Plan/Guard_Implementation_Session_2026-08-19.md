# Guard 구현 세션 파일 목록 (2026-08-19)

> 목적: 오늘 진행한 `PublicMD/Guard_Action_Implementation_Plan.md` Phase A~D 구현과, 그 직후 진행한 중복 정리(간이 리뷰 1~4번 항목) 작업에서 손댄 스크립트를 한눈에 보기 위한 세션 메모입니다.
>
> 이 파일은 `PublicMD/ProjectStructure.md`/`CodeConvention.md`/`ARCHITECTURE.md`가 아닌 일회성 세션 기록입니다. 구현 요약과 후속 배선 작업은 `PublicMD/PROGRESS.md`의 IMP-025 항목을, 설계 근거는 `PublicMD/Guard_Action_Implementation_Plan.md`를 참고하세요.
>
> 커밋 상태: Phase A~D 구현은 `44c5461 Implement guard patrol and combat action loop`로 이미 커밋되어 있습니다. 아래 "3. 지금(중복 정리) 수정한 파일"은 그 커밋 위에 아직 커밋되지 않은 working tree 변경입니다.

## 1. 오늘 새로 작성한 파일 (Phase A~D)

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Enum/ActionResult.cs` | `Running`/`Completed`/`ReplanRequested`/`Failed` action 결과 enum |
| `Assets/Scripts/Interface/ICombatTarget.cs` | 최소 전투 대상 계약(생존/위치/피해 적용) |
| `Assets/Scripts/Interface/IMoveTarget.cs` | 최소 이동 대상 계약(`TryGetPosition`) |
| `Assets/Scripts/System/Actor/CombatTargetHandle.cs` | Target+Owner 쌍으로 유효성을 판정하는 plain C# handle (`IMoveTarget` 구현) |
| `Assets/Scripts/System/Actor/ProximitySensor2D.cs` | LayerMask 기반 범용 Trigger2D 감지기 (도메인 비인지) |
| `Assets/Scripts/System/Actor/GuardPerception.cs` | sensor collider를 살아있는 `ICombatTarget` 후보로 변환·중복 제거하는 Guard adapter |
| `Assets/Scripts/System/Actor/GuardRuntimeState.cs` | NPC별 `CombatTargetHandle` 소유(선택된 타겟 유지) |
| `Assets/Data/Struct/MoveRequest.cs` | 고정 `Vector3` 또는 `IMoveTarget` + stopping distance |
| `Assets/Scripts/Actor/Enemy.cs` | 최소 `ICombatTarget` 테스트 표적(AI 없음) |
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | 경계 반경/순찰 도착 거리/순찰점 개수, 초당 욕구 증가량, Guard 중단 임계치 |
| `Assets/Scripts/System/Action/GuardAction.cs` | 장기 순찰 action(결정적 순찰점 순환, 욕구 증가, 재계획 요청) |
| `Assets/Data/ScriptableObject/Script/AttackActionCost.cs` | 공격 반경 |
| `Assets/Scripts/System/Action/AttackAction.cs` | 장기 반복 타격 action(타격 직후 사망 재검사, hit 상한 timer clamp) |

`Assets/Scripts/System/Actor/GuardActionSelector.cs`는 이전 세션에 사용자가 만든 Farmer 복사본 골격(git 미추적 상태)이 있었고, 오늘 전면 재작성했습니다. 실질적으로 신규 작성에 가깝습니다.

## 2. 오늘 수정한 파일 (Phase A~D)

| 파일 | 변경 요지 |
|---|---|
| `Assets/Scripts/Interface/IAction.cs` | `CheckComplete()` → `ActionResult Result` |
| `Assets/Scripts/System/Action/DefaultAction.cs` | `Init()`이 `Start()` 미호출, `Complete`/`RequestReplan`/`Fail` 종료 경로 분리 |
| `Assets/Scripts/Actor/WorkerNPC.cs` | queue lifecycle 단독 소유(`AdvanceQueue`/`CancelAndReturnQueue`), `NPCType` 저장, 풀 재사용 가드 |
| `Assets/Scripts/Manager/NPCManager.cs` | 실제 `NPCType` 전달, selector 인덱스 사전 검증 |
| `Assets/Scripts/System/Actor/NPCComponent.cs` | `GuardPerception`/`GuardRuntimeState` 노출, `ResetRuntimeState()` |
| `Assets/Scripts/System/Action/MoveAction.cs` | `MoveRequest` 지원, 하드코딩 stopping distance 제거, 동적 타겟 소실 시 재계획 |
| `Assets/Scripts/System/Action/EatAction.cs`, `DrinkAction.cs` | `ActionResult` 마이그레이션, `Complete()` 오염 버그 수정 |
| `Assets/Scripts/System/Action/SleepAction.cs`, `IdleAction.cs` | `ActionResult` 마이그레이션 |
| `Assets/Scripts/System/Action/FarmingAction.cs` | `ActionResult` 마이그레이션, 컴포넌트 유실 시 `Fail()`로 수정 |
| `Assets/Scripts/System/Lib/ActionPool.cs` | null-safe `GetAction`/`ReturnAction`, Guard/Attack case 추가 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | queue 조립 트랜잭션화(대여 실패 시 롤백) |
| `Assets/Scripts/Enum/BuildingType.cs` | `GuardPost` 추가(끝에) |
| `Assets/Data/Struct/ActionContext.cs` | `MoveRequest? moveRequest`를 생성자 끝에 추가 |
| `Assets/Scripts/System/Actor/NPCStat.cs` | 공격력·공격 속도 필드/`Get...` 추가 |
| `Assets/Scripts/Interface/IStatView.cs` | `GetAttackPower`/`GetAttackSpeed` 추가 |
| `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs` | 공격력·공격 속도 직렬화 필드, `CreateStat()` 갱신 |
| `Assembly-CSharp.csproj` | 신규 `.cs` 수동 `<Compile Include>` (gitignore 대상, 커밋에는 미포함) |
| `PublicMD/ProjectStructure.md` | "Guard Combat Vertical Slice" 섹션 추가 |
| `PublicMD/PROGRESS.md` | IMP-025 기록(구현 요약, 검증 결과, 남은 배선 작업) |

## 3. 지금(중복 정리) 수정한 파일

Codex 리뷰(`PublicMD/Code_Evaluation_Result.md`)의 중복 지적 중 사용자가 승인한 1~4번 항목을 반영했습니다. **아직 커밋되지 않은 working tree 변경**입니다.

| 파일 | 변경 요지 |
|---|---|
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | `ShouldInterrupt(IStatView stat)` 추가 — Guard 욕구 임계 판정의 단일 source |
| `Assets/Data/ScriptableObject/Script/AttackActionCost.cs` | `IsInRange(Vector3 from, Vector3 targetPosition)` 추가 — 공격 사거리 판정의 단일 source |
| `Assets/Scripts/System/Action/GuardAction.cs` | `cost.ShouldInterrupt(stat)` 사용, 중복 `IsInterruptThresholdReached`/`Normalize` 제거, 빈 `UpdateCompletion()` override 제거 |
| `Assets/Scripts/System/Action/AttackAction.cs` | `cost.IsInRange(...)` 사용, 빈 `UpdateCompletion()` override 제거 |
| `Assets/Scripts/System/Action/DefaultAction.cs` | `UpdateCompletion()`을 `abstract` → `virtual` no-op으로 완화 |
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | `protected TryRentAction(...)`/`ReturnAll(...)` 공통 헬퍼 추가 |
| `Assets/Scripts/System/Actor/FarmerActionSelector.cs` | base `TryRentAction`/`ReturnAll` 사용, 중복 `TryEnqueueMove`/`ReturnAll` 제거 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | base `TryRentAction`/`ReturnAll` 사용, `cost.ShouldInterrupt`/`IsInRange` 사용, 중복 `TryRent`/`ReturnAll`/`IsNeedAboveGuardThreshold`/`Normalize` 제거 |

빌드 검증: `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0 (위 8개 파일 반영 후).

## 4. 아직 적용하지 않은 항목 (검토만, 후속 작업 후보)

Codex 리뷰의 나머지 지적 중 이번에 적용하지 않은 것들입니다. 우선순위와 이유는 대화 로그에 기록돼 있습니다.

- `NPCDecision`에 최종 `ActionType` 보존 (`DestinationDecider`/`NPCDecision` 핵심부 변경 필요, 더 신중하게)
- `MoveRequest`를 이동 입력의 단일 경로로 통일 (`MoveAction`의 `Destination` fallback 제거, Farmer 호출부까지 영향)
- `NPCComponent.TryMoveTo`로 이동 계산(diff/flip/move) 공통화
- Sleep 결과 예측·실행 단일화 (`DestinationDecider.AddSleepCandidate` vs `SleepAction`, Guard 작업 이전부터 있던 기존 부채)
- 사용처 없는 API 정리(`DataManager.instance` 전역 참조, `IDataManager`, `ActionContext.Has*`, `DefaultActionCost.MovePerThirst/Hunger/Fatigue` 등) — 대부분 Guard 작업 이전부터 있던 기존 코드
