# Code Evaluation Result

## Purpose

IMP-027 `DestinationDecider` rational utility rewrite의 구현 결과를 검토한다. 범위는 새 점수·예측 모델, Guard selector 통합, tuning 직렬화, TestOnly probe와 실제 selector/runner 연결이다. Production 코드는 수정하지 않았다.

## Review Snapshot

- Date: 2026-08-21
- Scope:
  - `Assets/Scripts/System/Lib/DestinationDecider.cs`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs`
  - `Assets/Data/ScriptableObject/Script/NPCDecisionTuning.cs`
  - `Assets/Data/ScriptableObject/NPCDecisionTuning.asset`
  - `Assets/TestOnly/TestDecisionScenarioProbe.cs`
  - 연결 계약: `FarmerActionSelector`, `WorkerNPC`, `GuardAction`, `NPCDecision`, `DestinationDB`, interaction provider
- Sources:
  - `PublicMD/CodeConvention.md`
  - `PublicMD/ProjectStructure.md`
  - `PublicMD/DestinationDecider_Rational_Utility_Plan.md`
  - `PublicMD/PROGRESS.md`
- Verification:
  - `dotnet build Assembly-CSharp.csproj --no-restore`: 0 warnings, 0 errors
  - 변경 production 파일 대상 `git diff --check`: 통과
  - `GuardTest.unity`와 `SampleScene.unity`에서 selector의 `DestinationDB`/`NPCDecisionTuning` 직렬화 참조 확인
  - `TestDecisionScenarioProbe`는 `Assembly-CSharp.csproj`에 포함되지만 어떤 scene에도 배선되지 않음
  - Unity Play Mode와 probe 실행은 미검증

## Executive Summary

새 구조는 대부분 승인된 경계를 잘 지킨다. `DestinationDecider`가 실제 stat을 변경하거나 action queue를 만들지 않고 copied snapshot만 예측하며, Farmer와 Guard 역할 후보가 같은 bounded utility 모델에서 비교된다. 공개 `Decide(...)`와 queue lifecycle도 유지됐다. 비선형 risk, depth별 후보 버퍼, critical bit mask, 결정적 tie-break와 serialized tuning은 구현 품질이 좋다.

다만 Guard의 plain Idle 후보와 GuardDuty 후보가 모두 외부에서 `NPCIntent.Idle`로 보이는 설계는 수정이 필요하다. selector는 모든 non-supply 결정을 Guard queue로 바꾸므로, Decider가 `travel=0`, `After=현재 상태`인 plain Idle을 선택했는데 실제 runtime은 GuardPost로 이동하고 need가 증가하는 GuardAction을 실행할 수 있다. 이는 단순 tuning 문제가 아니라 예측 후보와 실행 행동의 계약 불일치다. `PROGRESS.md`도 stress rate에서 plain Idle이 GuardDuty를 이길 수 있음을 기록하고 있어 실제 도달 가능한 경로다.

최종 판정은 **조건부 승인**이다. 아래 H-01을 먼저 정리한 뒤 Unity 검증과 balance 조정으로 넘어가는 것이 안전하다.

## Improvements Since Previous Review

- one-step `bestSupply >= work + SwitchMargin` 분기가 제거되고 모든 일반 후보가 하나의 점수 모델에서 비교된다.
- 공급 행동은 한 번만 반환되고, queue 종료 후 실제 stat/position으로 재판단하는 기존 계약이 유지된다.
- Guard selector가 공급 행동 이후에도 Decider를 다시 호출하므로 기존 호출 gate 문제가 해결됐다.
- critical 안전 필터가 root뿐 아니라 미래 node에도 적용된다.
- 재귀 branch별 후보 리스트와 `int` bit mask를 사용해 이전 계획의 mutable-state 공유 및 `bool[]` 할당 위험을 피했다.
- 신규 serialized tuning 값이 asset YAML에 명시되어 Unity의 silent zero default 위험을 피했다.
- TestOnly probe는 검증할 수 없는 항목을 PASS로 위장하지 않고 `NOT VERIFIED`로 구분한다.

## Findings By Severity

### Critical

None found.

### High

#### H-01 — Guard의 plain Idle 결정이 GuardDuty 실행으로 변환된다

- Location:
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:202-217`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:373-388`
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs:108-121`
  - `Assets/Data/Struct/NPCDecision.cs:3-5`
- Evidence:
  - plain Idle은 현재 위치, 이동 시간 0, 상태 변화 없음으로 예측된다.
  - GuardDuty는 GuardPost 위치, 실제 이동 시간, duty need cost를 갖지만 똑같이 `NPCIntent.Idle`로 반환된다.
  - selector는 supply가 아닌 모든 결과를 `BuildGuardQueue(...)`로 바꾼다.
  - `PublicMD/PROGRESS.md`는 10/s stress rate에서 plain Idle이 GuardDuty보다 높은 점수를 받을 수 있다고 명시한다.
- Impact:
  - Decider가 계산하지 않은 GuardPost 왕복비용과 need 증가가 runtime에서 발생한다.
  - 공급 시설에서 plain Idle이 이기면 “잠시 대기”가 아니라 즉시 GuardPost 복귀로 변환되어 이번 작업의 핵심인 post-supply 합리성을 훼손할 수 있다.
  - `NPCDecision`의 “selector는 결정을 다시 해석하지 않고 queue로 변환한다”는 계약과 어긋난다.
- Recommendation:
  - 가장 명확한 해결은 `NPCIntent.Guard`를 추가해 GuardDuty와 Idle을 구분하고 selector에서 각각 Guard queue와 Idle queue로 매핑하는 것이다.
  - 공개 enum 확장을 피해야 한다면, non-critical Guard에서 유효한 GuardDuty가 존재할 때 plain Idle을 후보 집합에서 제외하고 Idle은 실제 fallback일 때만 생성해야 한다.
  - tuning으로 두 후보의 점수만 조정해 숨기지 말 것. 식별 정보가 소실되는 구조 자체를 해결해야 한다.

### Medium

#### M-01 — Unity runtime 검증이 아직 없고 오프라인 검증은 재현할 수 없다

- Location:
  - `Assets/TestOnly/TestDecisionScenarioProbe.cs`
  - `PublicMD/PROGRESS.md:127-131`
- Evidence:
  - probe GUID는 어떤 scene YAML에도 존재하지 않는다.
  - Play Mode와 Unity probe는 미실행으로 기록되어 있다.
  - 18개 PASS를 만든 임시 .NET harness는 저장소 밖 scratchpad여서 현재 코드와 함께 다시 실행할 수 없다.
- Impact: build와 정적 구조는 검증됐지만 실제 provider 데이터, queue 종료 후 재판단, Guard combat 선점, action duration과의 상호작용은 아직 확인되지 않았다.
- Recommendation: H-01 수정 후 `GuardTest.unity`에 probe를 임시 배선해 결과를 보존하고, 계획의 Farmer/Guard Play Mode 시나리오를 실행한다. 미실행 항목은 계속 미검증으로 유지한다.

#### M-02 — interrupt/critical threshold가 같을 때 inversion warning이 누락된다

- Location:
  - `Assets/Scripts/System/Actor/GuardActionSelector.cs:53-75`
  - `Assets/Data/ScriptableObject/Script/GuardActionCost.cs:51-65`
  - `Assets/Scripts/System/Lib/DestinationDecider.cs:393-404`
- Evidence:
  - `ShouldInterrupt`는 `need >= interruptThreshold`에서 참이다.
  - critical mask는 `need > CriticalNeedThreshold`에서만 열린다.
  - warning 검사는 interrupt threshold가 critical threshold와 같으면 안전하다고 판단한다.
- Impact: 두 threshold가 같은 asset 설정에서 정확히 경계값인 NPC는 warning 없이 interrupt 상태지만 critical gate 밖에 놓이고 timed Idle fallback을 반복할 수 있다.
- Recommendation: warning의 안전 조건을 각 interrupt threshold가 critical threshold보다 **엄격히 큰 경우**로 바꾸거나 두 runtime 비교 연산자의 경계 정책을 통일한다. 현재 기본값 0.97/0.95에는 즉시 발생하지 않는다.

### Low

#### L-01 — Sleep 후보가 재귀 node마다 `StatEffect`를 할당한다

- Location: `Assets/Scripts/System/Lib/DestinationDecider.cs:293-311`
- Evidence: `AddSleepCandidate`가 매 후보 구성 시 `new StatEffect(...)`를 호출한다.
- Impact: bounded replan이라 위험은 낮지만 `PROGRESS.md`의 “node당 heap allocation 없음” 설명은 정확하지 않다.
- Recommendation: copied `NeedSnapshot`의 `Fatigue`를 직접 0으로 만든 뒤 `After`에 넣어 임시 `StatEffect` 생성을 제거한다.

#### L-02 — 비정상 점수가 조용히 버려진다

- Location: `Assets/Scripts/System/Lib/DestinationDecider.cs:494-507`
- Evidence: NaN/Infinity 후보는 건너뛰지만 계획이 요구한 development diagnostic이 없다.
- Impact: 잘못된 tuning 또는 stat 입력이 Idle fallback으로만 나타나 원인 추적이 어렵다.
- Recommendation: `UNITY_EDITOR || DEVELOPMENT_BUILD`에서 한 결정당 한 번만 invalid candidate 정보를 warning으로 출력한다. release 선택 경로는 현재처럼 안전하게 거부하면 된다.

#### L-03 — architecture 문서가 현재 구조보다 오래됐다

- Location: `PublicMD/ProjectStructure.md`
- Evidence: 문서 전반이 2026-08-01 skeleton을 설명하고 일부 후반부는 문자 인코딩이 손상되어 있다. 현재 `DestinationDecider`, queue runner, Guard stat/combat/utility 구조는 `PROGRESS.md`에만 분산돼 있다.
- Impact: 새 작업자가 책임 경계를 찾을 때 현재 코드보다 오래된 권고를 읽게 된다.
- Recommendation: IMP-027 Play Mode 검증 후 현재 구조를 기준으로 문서를 다시 정리한다.

## Findings By File

- `DestinationDecider.cs`: 책임 집중, bounded recursion, copied-state prediction은 적절하다. Guard Idle/GuardDuty identity 손실이 핵심 결함이다. Sleep allocation과 invalid-score diagnostic은 후속 정리 대상이다.
- `GuardActionSelector.cs`: combat 우선순위와 매 replan Decider 호출은 올바르다. 다만 non-supply 전체를 Guard queue로 해석하는 정책이 H-01을 만든다. threshold equality validation도 보정이 필요하다.
- `NPCDecisionTuning.cs` / `.asset`: 필드 clamp와 YAML 값이 일치하며 신규 필드의 silent zero 위험이 없다. duration이 runtime action 값의 복사본이라는 한계도 정확히 문서화됐다.
- `TestDecisionScenarioProbe.cs`: reflection이나 production test API 없이 가능한 범위를 정직하게 검증한다. 다만 scene 미배선·미실행 상태이고 일부 수치 시나리오는 의도적으로 검증하지 않는다.
- `FarmerActionSelector.cs`: 변경 없이 새 Decider를 사용하며, supply queue 종료 후 실제 상태로 재판단한다. bounded Farming batch 계약도 유지된다.
- `WorkerNPC.cs`: queue lifecycle 단독 소유를 유지하며 새 decision 계산을 직접 알지 않는다.

## Cross-Cutting Findings

- “예측 후보 하나 ↔ 외부 decision 하나 ↔ 실행 queue 하나”의 의미 보존이 utility 정확도보다 우선한다. H-01처럼 후보 종류를 외부 경계에서 합치면 좋은 수식도 실제 행동 비용과 연결되지 않는다.
- Guard duty evaluation slice와 runtime GuardAction 지속시간의 차이는 명확히 문서화되어 있어 의도된 근사로 볼 수 있다. 단, H-01과 결합하면 근사가 아니라 전혀 다른 후보가 실행되는 문제가 된다.
- 현재 tuning 값은 합성 수치 검증의 출발값이며 최종 balance 값이 아니다. 구조 결함을 tuning으로 상쇄하지 말아야 한다.

## Positive Notes

- `DestinationDecider`는 action/pool/runner를 참조하거나 실제 `NPCStat`을 mutate하지 않는다.
- public `Decide(...)`, `NPCDecision`, `ActionContext`, `IAction`, `WorkerNPC` 계약이 유지됐다.
- Farmer와 Guard가 동일한 risk/travel/future 가치 단위로 평가되면서 역할별 실행은 selector에 남아 있다.
- critical safety filtering, finite check, deterministic tie-break, depth clamp가 명시적이다.
- Guard combat가 utility decision보다 먼저 실행되는 우선순위가 유지됐다.
- scene의 기존 Farmer/Guard selector에는 `NPCDecisionTuning.asset`과 `DestinationDB` 참조가 연결되어 있어 production용 신규 재배선은 필요하지 않다.

## Recommended Next Actions

1. H-01을 수정해 GuardDuty와 plain Idle의 실행 의미를 분리한다.
2. threshold equality validation을 보정한다.
3. 수정 후 command-line build와 정적 diff를 다시 확인한다.
4. `GuardTest.unity`에서 TestOnly probe와 Farmer/Guard Play Mode 시나리오를 실행한다.
5. decision trace로 tuning을 조정하되 구조 문제를 weight 변경으로 덮지 않는다.
6. Sleep 임시 할당과 invalid-score diagnostic을 정리한다.
7. 안정화 후 `ProjectStructure.md`를 현재 아키텍처로 갱신한다.
