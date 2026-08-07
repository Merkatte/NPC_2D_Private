# NPC Decision System Plan

이 문서는 `DestinationDecider` / `NPCDecision` / `FarmerActionSelector` 판단 계층을 왜 이런 형태로 만들었는지 기록합니다. 코드만 읽어서는 알 수 없는 설계 근거를 남기는 것이 목적입니다. 세부 파일 배치와 구현 계획은 `PublicMD/PROGRESS.md`의 해당 작업 항목을 참고하세요.

## 문제 정의

작업 시작 시점의 `FarmerActionSelector`는 빈 `Queue<IAction>`만 반환하는 껍데기였고, `DestinationDecider`는 목적지 4개까지의 거리만 계산한 뒤 아무것도 반환하지 않는 미완성 스텁이었습니다. 두 클래스는 연결되어 있지 않았습니다.

만약 selector가 직접 거리·need·점수 계산을 갖게 되면 다음 문제가 생깁니다.

- selector 하나를 읽으려면 "무엇을 할지 판단하는 로직"과 "판단 결과를 실행 가능한 action으로 바꾸는 로직"을 동시에 이해해야 합니다.
- 판단 로직을 재사용하려는 다른 직업(Guard, Cook)이 selector 전체를 복사하거나 상속해야 합니다.
- 판단 수식을 튜닝할 때마다 action queue 조립 코드까지 다시 읽어야 합니다.

그래서 판단(무엇을 할지 결정)과 실행 준비(결정을 큐로 바꾸기)를 별도 클래스로 분리했습니다.

## 계층 경계

```
DestinationDecider.Decide(stat, npcType, npcLoc)  ->  NPCDecision
        (거리·need 예측·작업 가능 횟수·점수 계산)

FarmerActionSelector  ->  NPCDecision을 Queue<IAction>으로 변환
        (ActionPool에서 action을 꺼내 순서대로 담기만 함)
```

`DestinationDecider`가 하면 안 되는 것: `IAction` 생성, `Queue` 생성, `ActionPool` 접근, action 실행, `NPCStat` 직접 변경.

`FarmerActionSelector`가 하면 안 되는 것: hunger/thirst/fatigue 예측 계산, 목적지별 점수 계산, 작업 가능 횟수 계산.

이 경계를 어긴 코드가 리뷰에서 나오면 그건 버그가 아니라 계층 위반으로 취급합니다. 예를 들어 selector 안에 `Vector2.Distance`나 `Percentage` 계산이 보이면 잘못 배치된 것입니다.

## 왜 직업 중립 타입(`NPCIntent` / `NPCDecision`)인가

`DestinationDecider`는 Farmer 전용이 아니라 **직업 공용**으로 설계되었습니다. 추후 Guard, Cook 등 다른 직업도 같은 decider를 사용할 예정이기 때문입니다. `FarmerDecision`처럼 직업 이름이 붙은 타입을 반환하면, 다른 직업이 이 decider를 쓰기 시작하는 순간 타입 이름과 실제 용도가 어긋나거나 이름 변경 리팩터링이 필요해집니다. 그래서 반환 타입을 `NPCIntent`(의도) / `NPCDecision`(판단 결과)으로 정했습니다.

`DestinationDecider`는 순수 C# 클래스로 유지합니다. Unity 오브젝트 수명에 묶이지 않아야 여러 직업의 selector가 인스턴스를 공유하거나, 추후 최상위 관리자가 하나 만들어 배포하는 방식으로 바꾸기 쉽습니다.

## 왜 intent 하나가 아니라 스텝 리스트인가

초기 설계는 `NPCDecision`이 intent 하나(`Work` 또는 `Drink` 등)만 담는 형태였습니다. 이 형태로는 "이동 → 마시기 → 이동 → 일하기" 같은 연속 행동을 표현할 수 없습니다. intent 하나로는 보급을 마친 뒤 이어서 무엇을 할지 selector가 다시 판단해야 하는데, 그러면 selector가 판단 로직을 일부 떠안게 됩니다.

그래서 `NPCDecision`은 순서가 있는 `NPCDecisionStep[]`을 담습니다. selector는 이 배열을 순회하며 큐에 담기만 하면 되고, 체인이 몇 단계든 selector 코드는 바뀌지 않습니다.

## 모든 action 앞에 Move를 넣는 이유

NPC(정확히는 `WorkerNPC`/selector)는 자신이 지금 어디에 있는지 미리 알 수 없습니다 — 이전 큐를 어디까지 실행했는지, 플레이어가 개입했는지 등을 selector가 추적하지 않기 때문입니다. 따라서 모든 행동 스텝 앞에 조건 없이 `MoveAction`을 넣는 것이 가장 단순하고 안전합니다.

이게 낭비가 되지 않는 이유는 `MoveAction.Start()` 구현에 있습니다. `Start()`는 내부에서 `UpdateCompletion()`을 호출하는데, 목적지 반경(`stoppingDistance`) 안에 이미 있으면 그 자리에서 즉시 `IsComplete = true`가 됩니다. 즉 "이미 도착해 있다"는 경우에도 Move가 다음 tick으로 넘어가지 않고 그 프레임에 바로 완료 처리됩니다. 그래서 selector가 "이번엔 이동이 필요 없다"를 판단할 필요 없이 항상 Move를 앞에 붙여도 됩니다.

## "나온 김에" 판단 — 탐욕적 한계이득 체인

### 배경

보급(Drink/Eat/Sleep) 후보를 1개로 제한하면 부자연스러운 문제가 생깁니다. 목이 말라 물을 마시러 나갔다가 다시 일하러 갔다가, 얼마 안 가 배가 고파 다시 밥을 먹으러 나가는 왕복이 반복됩니다. 이동 비용만 계속 지불하면서 작업 가능 횟수는 항상 가장 급한 need 하나에만 묶입니다.

사람이라면 "물 뜨러 나온 김에 밥도 먹고 갈까?"를 판단합니다. 이 판단은 완벽한 계획을 세우는 게 아니라, **한 발짝 나간 뒤 거기서 다시 둘러보는** 과정입니다.

### 왜 순열 전체 열거가 아닌가

보급 3종(Drink/Eat/Sleep)의 순서 있는 부분집합을 모두 평가하면 후보가 16개(공집합 포함, 순서까지 고려)로 늘어납니다. 후보가 늘수록 점수 튜닝과 "왜 이 조합이 선택됐는지" 디버깅이 어려워집니다. 그리고 순열 전체 평가는 사람의 실제 의사결정 방식과도 다릅니다 — 사람은 모든 조합을 계산기로 돌려보고 고르지 않습니다.

### 알고리즘

한 번에 하나씩 고르고, 고를 때마다 위치와 need 상태를 그 지점으로 갱신한 뒤 다시 판단합니다.

```
chain     = []
remaining = { Drink, Eat, Sleep } 중 목적지 조회에 성공한 것만
state     = 현재 need snapshot
pos       = npcLoc

반복 (최대 remaining.Count 회):
    baseScore = ScoreGoWorkNow(state, pos)   // 지금 바로 작업장으로 간다면

    bestGain   = MinChainGain                // 이 문턱을 못 넘으면 채택 안 함
    bestSupply = None

    foreach supply in remaining:
        이동 후 보급을 적용한 상태에서 ScoreGoWorkNow를 다시 계산하고
        baseScore와의 차이(한계이득)를 구한다
        가장 큰 한계이득이 bestGain을 넘으면 bestSupply로 채택

    if (bestSupply == None)
        break   // "별 차이 없다" -> 그냥 일하러 간다

    chain에 추가, remaining에서 제거, pos/state를 그 지점 기준으로 갱신
```

핵심은 **매 반복마다 `pos`가 갱신된다**는 점입니다. 우물에 들른 뒤에는 "우물 → 식당" 거리로 재평가하므로, 두 목적지가 가까우면 자연스럽게 함께 처리하고 멀리 떨어져 있으면 포기합니다. 이게 "나온 김에" 판단을 코드로 옮긴 부분입니다.

평가 횟수는 최대 `3 + 2 + 1 = 6`회입니다. 순열 전체 열거(16개 후보 각각 경로 계산)보다 적고, 각 단계에서 "왜 이 보급을 골랐는지/포기했는지"가 로그 한 줄로 남길 수 있어 디버깅이 쉽습니다.

### 감수한 한계

이 알고리즘은 **국소 최적**입니다. 예를 들어 `Eat → Drink` 순서가 전체적으로 더 나은데, 1단계에서 `Drink`의 한계이득이 근소하게 더 커서 `Drink`를 먼저 채택하고 나면 `Eat → Drink`라는 조합 자체를 다시 평가하지 않습니다. 이 손실은 의도적으로 감수했습니다 — NPC 행동에서 사람도 완벽한 순열 최적화를 하지 않고, 오히려 매번 최적해를 찾는 NPC가 기계적으로 보일 수 있습니다. 나중에 이 한계가 문제가 되면 체인 길이 2까지만 전방탐색(2-step lookahead)하는 방식으로 보강할 수 있습니다.

`MinChainGain`은 "별 차이 없으면 그냥 일하러 간다"를 담당하는 튜닝 손잡이입니다. 값을 올리면 NPC가 보급을 잘 안 들르고(부지런해 보이고), 내리면 사소한 이득에도 볼일을 몰아서 봅니다.

## 위험 패널티가 선형이 아니라 제곱인 이유

목마름 86%와 98%를 같은 위험도로 취급하면 NPC가 두 상황에서 비슷한 강도로 반응합니다. 이러면 위기 상황에서도 여유로워 보이는 행동이 나옵니다. 임계값 초과분을 제곱해서 패널티를 매기면, 위험 수치가 높아질수록 회피 성향이 훨씬 급격히 강해져서 "위기 상황에서는 무조건 회복부터"라는 자연스러운 행동이 점수 계산만으로 나옵니다.

## 임시값 목록과 이관 계획

`DestinationDecider` 내부의 다음 상수들은 모두 **임시값**입니다. need 증가/감소 시스템이 아직 존재하지 않기 때문에 넣어둔 자리표시자이며, 실제 값 튜닝은 의미가 없습니다.

- `FatiguePerTravelSecond`, `HungerPerTravelSecond`, `ThirstPerTravelSecond` — 이동 중 need 증가율
- `FatiguePerWork`, `HungerPerWork`, `ThirstPerWork` — 작업 1회당 need 증가량
- `DangerThreshold`, `CriticalThreshold` — 위험/치명 임계값
- `WorkValue`, `TravelCost`, `SupplyActionCost`, `DangerPenaltyMultiplier`, `MinChainGain` — 점수 계산 가중치

`CodeConvention.md`의 데이터 에셋 규칙에 따르면, 이런 튜닝 값은 결국 별도 데이터 에셋(ScriptableObject 또는 CSV)으로 옮겨야 합니다. 지금은 need 시스템 자체가 없으므로 `const`로 박아두고, **사용자가 need 증가/최대치 시스템을 만든 뒤** 데이터 에셋으로 이관하는 것을 다음 작업으로 남깁니다.

## 알려진 제약 (이번 작업 범위에서 해결하지 않음)

- `NPCStat`의 `_fatigueMax/_hungerMax/_thirstMax`는 생성자에서 설정되지 않아 항상 0입니다. `Current*Percentage`에 방어 로직(`max <= 0 → 0`)을 넣으면 NaN은 없어지지만, 모든 need가 **항상 0%로 고정**됩니다. 그 결과 `DestinationDecider`는 현재 런타임에서 **항상 Work만 선택**합니다. 이는 버그가 아니라 need 시스템이 아직 없기 때문이며, need 시스템이 추가되면 자동으로 판단 로직이 의미를 갖기 시작합니다.
- `WorkerNPC`는 여전히 `Temp_CreateNewActionMove()`로 랜덤 이동만 반복하며, `FarmerActionSelector.RequestNewActionQueue`를 호출하는 코드가 아직 없습니다. 즉 이번 작업은 판단·큐 조립 로직을 준비하는 단계이고, 실제 NPC 행동에 반영되는 것은 큐 소비 측(runner)이 연결된 이후입니다.
- `Eat/Sleep/Farming/Drink` 각 `IAction` 구현은 여전히 스텁(`NotImplementedException`)입니다. 큐가 실제로 실행되기 전에 반드시 구현되어야 합니다.
