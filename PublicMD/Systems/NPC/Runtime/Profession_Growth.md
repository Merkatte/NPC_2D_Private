# Profession Growth

## 목적과 책임

주민별·직업별 숙련도와 레벨을 보관한다. NPCStat의 성장 참조는 별도 객체이므로 같은 주민의 새 stat에 전달할 수 있다. 공유 설정은 필요 숙련도·기본 상한·기여 계수, scene 관리자는 직업별 현재 상한만 소유한다. 업무 성능 보너스·전직 기능·저장·대표 건물은 이번 구현 범위가 아니다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/System/Actor/NPCProfessionGrowth.cs` | 개인 직업별 레벨·숙련도 기록, 지급·연속 레벨업·상한 적용 |
| `Assets/Data/Struct/ProfessionProgress.cs` | UI가 조회하는 레벨·숙련도·상한·다음 필요량 snapshot |
| `Assets/Data/ScriptableObject/Script/ProfessionGrowthSettings.cs` | NPCType별 기본 상한·필요량·기여 단위 계수 |
| `Assets/Scripts/Manager/ProfessionGrowthManager.cs` | scene 직업별 상한 조회·변경 |

NPCStat/WorkerNPC는 [Lifecycle and Stats](Lifecycle_and_Stats.md), NPCManager는 [Spawning](Spawning_and_Pooling.md), InteractionResult/WorkContributionKind는 [Interaction](../../World/Interaction_and_Destinations.md)가 주 소유한다. 지급 provider/action은 각 업무 문서가 소유한다.

## 기록과 상한 규칙

- 각 직업은 Lv.1/0으로 시작한다. 기본 상한 5, 다음 필요량은 100 + 50 × (현재 레벨 − 1)이다. 각 직업 row에서 독립적으로 조절한다.
- 실제 기여량에 설정 계수를 곱해 지급한다. 초과분은 다음 레벨로 이월하며 한 번에 여러 레벨을 올릴 수 있다.
- 상한에 도달하는 지급의 잔량은 버린다. 현재 레벨이 상한 이상이면 이후 지급을 무시한다.
- 상한 변경은 기록을 수정하지 않는다. 상한 하락에서도 기존 레벨과 보유 숙련도를 보존하고, 상승은 이후 성장만 허용한다.
- NPCType별 dictionary 기록은 서로 분리한다. SetProfessionGrowth 또는 NPCStat 생성자 인수로 같은 기록을 전달할 수 있으며 직업 변경·휴식에 따른 감소는 없다.
- 신규 주민 예약은 새 기록을 만든다. WorkerNPC disable/init은 수명 토큰을 바꾸며 Downed는 기존 기록과 수명을 유지한다.

## 실제 기여 연결

| 기여 종류 | 직업 | 초기 계수 |
|---|---|---|
| FarmProgress | Farmer | 실제 농장 진행도 1당 0.5 |
| HarvestItems | Farmer | cargo 수락 수량 1당 2 |
| ConstructionWork | Builder | 실제 공사 작업량 1당 0.2 |
| RepairedHealth | Builder | 실제 복구 HP 1당 0.1 |
| MaintenanceSeconds | Builder | 요구치 이내 재건·정리 진행 시간 1초당 2 |
| Damage | Guard / Archer | 실제 적 HP 감소 1당 0.1 / 0.3 |

provider는 자신의 상태 변경만 수행하고 결과를 반환한다. action은 성공 결과를 성장 객체에 전달한다. 이동·생활·운반·입고·실패·완료 재시도·처치/완공 보너스는 지급하지 않는다. 피해는 CombatTarget이 실제 HP 차이를 반환한다. 궁병은 발사 시 주민·직업·성장 객체·수명을 고정하고 명중 때 검사한다. 시간 정지는 WorkerNPC/action 및 provider/damage transaction 경계에서 적용하며 성장 계산은 이미 확정된 기여를 소비한다.

## 조회와 scene 조립

`TryGetProgress(NPCType, out ProfessionProgress)`는 설정된 직업만 조회한다. snapshot은 Level, Experience, LevelCap, RequiredExperience, IsAtCap을 제공한다. 설정이 없으면 false다. UI는 WorkerNPC.Profession/ProfessionGrowth/LifetimeVersion을 사용하며 게임 시간을 멈추지 않는다.

NPCManager의 선택적 `_professionGrowthSettings`와 `_professionGrowthManager`, ProfessionGrowthManager의 `_settings`에 같은 에셋을 연결한다. 관리자 없이도 설정의 기본 상한을 사용한다. 관리자 설정이 다르면 개인 설정의 기본 상한을 사용한다. `GetLevelCap`과 `TrySetLevelCap`은 향후 대표 건물 연결 지점이며 대표 건물 단계/상한 규칙은 TBD다. 공유 SO에는 개인 현재값과 scene 상한을 저장하지 않는다.

## 변경 유형별 최소 확인 범위

| 변경 | 최소 확인 |
|---|---|
| 레벨·필요량·상한 | NPCProfessionGrowth, ProfessionProgress, ProfessionGrowthSettings, ProfessionGrowthManager |
| 신규 생성·pool·같은 주민 보존 | NPCStat, WorkerNPC, NPCManager, [Spawning](Spawning_and_Pooling.md) |
| 실제 기여·중복 지급 | InteractionResult와 해당 provider/action의 소유 leaf |
| 궁병 명중·늦은 화살 | DefenseAttackAction, DefenseProjectile, CombatTarget, WorkerNPC |
| 주민 정보 표시 | [UI](../../UI/README.md), WorkerNPC 공개 조회, ProfessionProgress |

컴파일·기존 검사·직렬화 참조 검증과 사람 플레이 확인을 구분한다. 실제 업무 증가, 상한 표시, 늦은 화살 및 Downed/재사용은 사람 QA 항목이며 자동 Play Mode·새 검증 코드를 추가하지 않는다.
