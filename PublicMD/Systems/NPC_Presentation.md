# NPC Presentation

## 기능 목적

NPC의 이동, 좌우 방향, 건물 출입, 작업 도구와 Animator 표현을 gameplay 판단에서 분리해 설명한다.

## 책임 경계

- `NPCComponent`는 actor-local Unity 참조와 표현 API를 소유한다.
- selector와 action은 `NPCComponent`의 의미 기반 메서드만 호출하고 Animator parameter나 visual child를 직접 찾지 않는다.
- NPC의 방향은 animation이 잡는 visual rotation이 아니라 이동 root의 local scale로 반전한다.
- presentation은 gameplay 완료 시간을 결정하지 않는다.

## 현재 실행 흐름

```text
MoveAction/GuardAction -> NPCComponent.Move(direction)
  -> root position 이동
  -> LateUpdate에서 Speed parameter 갱신

이동 방향 -> NPCComponent.Flip(isLeft)
  -> _transform.localScale = FacingLeft/FacingRight

BaseBuildingAction -> SetInsideBuilding(bool)
FarmingAction -> SetWorking(bool)
WorkerNPC.Init(Farmer) -> SetToolVisible(true)
```

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/NPCComponent.cs` | NPC 이동 root, 방향 scale, Animator parameter, 도구 표시와 combat adapter 접근 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 이동·Flip | `NPCComponent.cs`, `Assets/Prefab/InGame/NPCGirl.prefab` |
| 통통 튀는 이동 clip | `Assets/Animation/DefaultAnim/NPCGirl_Move.anim`, animator controller |
| 건물 출입 표현 | `NPCComponent.cs`, [Interaction and Destinations](Interaction_and_Destinations.md), enter/inside/exit clips |
| 호미 표시·작업 모션 | `NPCComponent.cs`, tool carry/work clips, NPC prefab |
| Animator 없는 role | `NPCComponent.cs`, 해당 role prefab, spawning 문서 |

## 에셋과 검증 도구

- `Assets/Animation/NPCGirl_Move.controller`: `Speed`, `IsInsideBuilding`, `IsWorking` parameter와 상태 전이를 소유한다.
- `Assets/Animation/DefaultAnim/*`: idle, move, building, tool animation clip.
- `Assets/Prefab/InGame/NPCGirl.prefab`: Animator, visual, tool renderer, sensor가 배선된 공유 NPC prefab.
- `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs`: parameter와 전이를 멱등 구성·검증한다.
- `Assets/TestOnly/Editor/NPCGirlToolLayerConfigurator.cs`: tool layer와 clip 배선을 구성·검증한다.

## 불변 규칙

- animation이 visual rotation을 점유할 수 있으므로 좌우 반전은 root scale 경계를 유지한다.
- `_requiresAnimator`가 true인 role은 필수 parameter 누락을 오류로 보고한다.
- pool 재사용 시 Speed, inside, working, target 표현 상태를 초기화한다.
- action은 animator state 이름이나 tool child를 직접 참조하지 않는다.

## 알려진 제약과 TBD

- Enemy는 현재 전용 Animator가 없어 `_requiresAnimator = false`다.
- Farmer와 Guard가 같은 NPC prefab을 공유하며 역할별 전용 presentation 분리는 아직 없다.

## 관련 문서

- [NPC Runtime](NPC_Runtime.md)
- [Movement](NPC_Decision_and_Actions/Movement.md)
- [Farming](Farming.md)
- [Spawning and Pooling](Spawning_and_Pooling.md)

## 문서 갱신 조건

`NPCComponent` 표현 API, animator parameter, clip/controller, prefab visual·tool 배선이 바뀌면 갱신한다.
