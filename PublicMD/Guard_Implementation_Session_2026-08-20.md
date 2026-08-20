# Stat/Cost 분리 세션 파일 목록 (2026-08-20)

> 목적: `PublicMD/Code_Evaluation_Result.md`(Codex, 2026-08-20)의 stat/cost 책임 경계 리뷰를 Opus Plan Mode로 계획하고 구현한 작업에서 손댄 스크립트를 한눈에 보기 위한 세션 메모입니다.
>
> 이 파일은 `PublicMD/ProjectStructure.md`/`CodeConvention.md`/`ARCHITECTURE.md`가 아닌 일회성 세션 기록입니다. 구현 요약과 후속 배선 작업은 `PublicMD/PROGRESS.md`의 IMP-026 항목을 참고하세요. 계획 원문은 세션 종료 시 `C:\Users\Merkatte\.claude\plans\purring-waddling-rainbow.md`에 있었습니다(Claude Code plan 파일, 저장소 밖).
>
> 커밋 상태는 이 문서가 아니라 Git 이력을 기준으로 확인합니다.

## 1. 오늘 새로 작성한 파일

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Interface/ICombatStatView.cs` | `AttackPower`/`AttackSpeed`/`AttackRange` read-only 계약. `IStatView` 비상속(`AttackAction`이 health/needs를 안 씀) |
| `Assets/Scripts/Interface/IGuardStatView.cs` | `ICombatStatView` 상속, `GuardRadius` 추가 |
| `Assets/Scripts/System/Actor/GuardStat.cs` | `NPCStat, IGuardStatView`. 4개 capability(공격력/공격속도/공격범위/순찰반경)를 생성 시점에 결정되는 값으로 소유하는 Guard 전용 runtime stat |
| `Assets/Scripts/System/Lib/CombatRange.cs` | `IsInRange(from, to, range)` static utility — 범위 판정을 stat에서 분리 |
| `Assets/Data/ScriptableObject/Script/NPCStatDefinition.cs` | `abstract NPCStat CreateRuntimeStat()` — NPCType별 stat 생성 팩토리 계약 |
| `Assets/Data/ScriptableObject/Script/GuardStatDefinition.cs` | `NPCStatDefinition` 구현, `GuardStat` 생성. `[CreateAssetMenu]`로 에셋 생성 가능 |

## 2. 오늘 수정한 파일

| 파일 | 변경 요지 |
|---|---|
| `Assets/Scripts/Interface/IStatView.cs` | `GetAttackPower`/`GetAttackSpeed` 제거(→`ICombatStatView`), 호출부 0건인 `Current*Percentage` 3개 제거 |
| `Assets/Scripts/System/Actor/NPCStat.cs` | 공격 필드/ctor 파라미터 제거(10-인자 ctor로 복귀). 호출부 0건인 4-인자 ctor·`ChangeMoveSpeed`(버그 있음)·`Current*Percentage` 제거 |
| `Assets/Data/Struct/ActionContext.cs` | 호출부 0건인 `HasStat` 제거. `Stat` 타입은 `NPCStat` 그대로(생성자 시그니처 불변) |
| `Assets/Data/ScriptableObject/Script/DefaultActionCost.cs` | 호출부 0건인 `MovePerThirst/Hunger/Fatigue` 제거, 미사용 `using Unity.Properties;` 제거 |
| `Assets/Data/ScriptableObject/Script/GuardActionCost.cs` | `GuardRadius` 제거(→`GuardStat`). `AttackStoppingDistanceRatio`(기본 0.9) tuning 신설 — selector 매직넘버 승격 |
| `Assets/Scripts/System/Actor/BaseNPCActionSelector.cs` | `CanUseStat(NPCStat)` virtual 계약 추가(기본 `stat != null`) |
| `Assets/Scripts/System/Action/GuardAction.cs` | `Start()`에서 `stat as IGuardStatView` 1회 캐스트해 필드 캐싱, `Clear()`에서 null화. `TickPatrol`/`GetPatrolPoint`가 `GuardRadius`를 `cost` 대신 `guardStat`에서 읽음 |
| `Assets/Scripts/System/Action/AttackAction.cs` | `AttackActionCost` 참조 전부 제거. `Start()`에서 `stat as ICombatStatView` 1회 캐스트, `CombatRange.IsInRange` 사용 |
| `Assets/Scripts/System/Actor/GuardActionSelector.cs` | `_attackActionCostInfo` 제거. `CanUseStat` override(`stat is IGuardStatView`). `RequestNewActionQueue` 진입부에서 `stat as IGuardStatView` 1회 캐스트 후 local parameter로만 하위 메서드(`TryBuildCombatQueue`/`BuildCombatQueue`/`BuildGuardQueue`)에 전달(필드 캐싱 금지 — selector는 여러 NPC가 공유) |
| `Assets/Scripts/Manager/NPCManager.cs` | `_selectors`(List\<BaseNPCActionSelector\>) → `_creationEntries`(List\<NPCCreationEntry\>, `NPCType`+`Selector`+`NPCStatDefinition` 결합)로 재작성. `Awake()`에서 null/누락/중복 검증. `CreateNPC()`가 `_workerPool` 누락/stat 생성 실패/`CanUseStat` 불일치/worker null까지 실패 경로 완비, 성공 시에만 `_workers`에 등록(기존 등록 누락 버그 수정) |
| `Assets/Scripts/Manager/DataManager.cs` | `_defaultStatContext` 필드와 `GetStat()` 제거 |
| `Assets/Scripts/Interface/IDataManager.cs` | `GetStat()` 제거(유일 호출부였던 `NPCManager`가 `NPCStatDefinition.CreateRuntimeStat()`으로 대체) |
| `Assets/Data/ScriptableObject/Script/DefaultStatContext.cs` | 기반 타입 `ScriptableObject`→`NPCStatDefinition`(GUID/asset 인스턴스 보존). `_attackPower`/`_attackSpeed` 제거. `CreateStat()`을 `CreateRuntimeStat()` override로 교체 |
| `Assembly-CSharp.csproj` | 신규 6개 `.cs` `<Compile Include>` 추가, 삭제된 `AttackActionCost.cs` 항목 제거 |
| `PublicMD/PROGRESS.md` | IMP-026 기록(구현 요약, 검증 결과, 남은 배선 작업) |

## 3. 오늘 삭제한 파일

`AttackActionCost`는 유일한 실사용 필드(`AttackRange`)가 `GuardStat`으로 이동하며 빈 껍데기만 남아 완전히 삭제했습니다.

| 파일 |
|---|
| `Assets/Data/ScriptableObject/Script/AttackActionCost.cs` |
| `Assets/Data/ScriptableObject/Script/AttackActionCost.cs.meta` |
| `Assets/Data/ScriptableObject/AttackActionCost.asset` |
| `Assets/Data/ScriptableObject/AttackActionCost.asset.meta` |

삭제 순서: 코드 의존 제거 → `GuardTest.unity`의 `DataManager._costInfos`에서 해당 entry 제거(자산을 먼저 지우면 dangling reference로 `DataManager.Awake()`가 `NullReferenceException`을 던짐) → 4개 파일 삭제 → `Assembly-CSharp.csproj` 정리.

## 4. 오늘 편집한 씬

| 파일 | 변경 요지 |
|---|---|
| `Assets/Scenes/GuardTest.unity` | `DataManager._costInfos`에서 삭제되는 `AttackActionCost.asset`을 가리키던 entry 제거. 새 기능 배선이 아니라 크래시 방지 목적으로 예외적으로 직접 편집(그 외 씬 배선은 평소대로 사용자 작업으로 남김) |

빌드 검증: `dotnet build Assembly-CSharp.csproj --no-restore` — 오류 0, 경고 0.

## 5. 아직 적용하지 않은 항목 (의도적 제외, 후속 작업 후보)

- Detection range → `CircleCollider2D` 동기화(Codex M-03). `GuardStat`에 `DetectionRange` 필드도 미리 추가하지 않음.
- 레벨업/progression 시스템. `GuardStat`의 4개 capability 필드는 이번 slice에서 **생성 시점에 결정되는 immutable 값** — 런타임 성장은 아직 불가능. `ApplyGrowth()` 등은 미리 만들지 않음.
- `FarmStat` 신설 — 코드 어디에도 농부 숙련도 데이터가 없어 빈 서브클래스를 만들지 않음. Farmer/Cook은 `DefaultStatContext`로 plain `NPCStat` 생성.
- `WorkerPool`의 NPCType별 분리 — 프리팹이 `NPCGirl` 하나뿐이라 `NPCCreationEntry`에 포함하지 않음.
- `GuardActionCost`/`FarmingActionCost`의 `Tuning`/`Policy` 개명(Codex L-01, cosmetic).

## 6. 사용자 후속 작업 (Unity 에디터)

`NPCManager._selectors`→`_creationEntries` 필드 타입이 바뀌어 기존 씬 배선이 초기화됩니다(크래시 없이 에러 로그 후 NPC 생성 실패).

1. `GuardTest.unity`의 `NPCManager._creationEntries`에 3 row 재구성: Farmer/Cook은 `FarmerActionSelector`+`DefaultStatContext.asset`, Guard는 `GuardActionSelector`+신규 `GuardStatDefinition.asset`.
2. `GuardStatDefinition.asset` 신규 생성(Create 메뉴), 값 입력: attackPower=10, attackSpeed=10(기존 `DefaultStatContext`), attackRange=1(기존 `AttackActionCost`), guardRadius=5(기존 `GuardActionCost`).
