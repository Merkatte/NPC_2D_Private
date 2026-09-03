# NPC Presentation

## 기능 목적

NPC의 이동, 좌우 방향, 건물 출입, 작업 도구와 Animator 표현을 gameplay 판단에서 분리해 설명한다.

## 책임 경계

- `NPCComponent`는 actor-local Unity 참조와 표현 API를 소유하며, plain C# runtime state(`CombatRuntimeState`, `WorkerInventory`)를 인라인으로 들고 그 표시 요청을 실제 Unity 조작으로 옮긴다.
- 봇짐을 "언제 보일지"는 `WorkerInventory`가 판단하고, `SpriteRenderer`를 실제로 켜고 끄는 것은 `NPCComponent.SetCarryVisible` 한 곳뿐이다.
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
BaseWorkingAction(Farming/Harvest) -> SetWorking(bool)
WorkerNPC.Init(Farmer) -> SetToolVisible(true)

WorkerInventory 적재/이관 -> 생성자 콜백
  -> NPCComponent.SetCarryVisible(bool)
  -> _carryRenderer.gameObject.SetActive(...)
```

도구 표시(`SetToolVisible`)는 `WorkerNPC.Init`이 정하는 역할 스위치이고, 봇짐 표시는 cargo 상태를 따르는 별개 스위치다. 두 스위치를 겹치지 않게 두어 pool 재사용 시 복원 순서에 의존하지 않는다 — 운반 중에도 호미는 계속 보인다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/NPCComponent.cs` | NPC 이동 root, 방향 scale, Animator parameter, 도구·봇짐 표시, cargo와 combat runtime state 소유 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 이동·Flip | `NPCComponent.cs`, `Assets/Prefab/InGame/NPCGirl.prefab` |
| 통통 튀는 이동 clip | `Assets/Animation/DefaultAnim/NPCGirl_Move.anim`, animator controller |
| 건물 출입 표현 | `NPCComponent.cs`, [Interaction and Destinations](Interaction_and_Destinations.md), enter/inside/exit clips |
| 호미 표시·작업 모션 | `NPCComponent.cs`, tool carry/work clips, NPC prefab |
| 봇짐 표시·용량 | `NPCComponent.cs`, [Inventory and Items](Inventory_and_Items.md)의 `WorkerInventory`, NPC prefab |
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
- pool 재사용 시 Speed, inside, working, target, cargo 표현 상태를 초기화한다.
- action은 animator state 이름이나 tool·carry child를 직접 참조하지 않는다.
- `_toolRenderer`와 `_carryRenderer`는 서로 다른 스위치가 소유하며 같은 상태를 두 곳에서 쓰지 않는다.
- 표시용 renderer 참조는 모두 null 허용이며, 배선되지 않은 role에서도 gameplay는 그대로 동작해야 한다.

## 알려진 제약과 TBD

- Enemy는 현재 전용 Animator가 없어 `_requiresAnimator = false`다.
- Farmer와 Guard가 같은 NPC prefab을 공유하며 역할별 전용 presentation 분리는 아직 없다.
- 봇짐 visual은 아직 prefab에 없다. 계획된 배선은 `Visual/ToolAnchor`의 형제로 `CarryAnchor/Carry`(기본 비활성 `SpriteRenderer`)를 두고 `NPCComponent._carryRenderer`에 연결하는 것이며, 기존 tool clip이 `Visual/ToolAnchor/Tool` 경로에만 바인딩되어 있어 animator·clip 수정은 필요하지 않다. `Assets/Art/Generated/worker-cargo-sack.png` 스프라이트 생성이 선행 조건이다(2026-09-04 기준 대기 중). 그때까지 운반은 gameplay 상 정상 동작하지만 화면에는 보이지 않는다.
- 작물별 봇짐 스프라이트는 `itemId -> Sprite` 테이블이 없어 지원하지 않는다.

## 관련 문서

- [NPC Runtime](NPC_Runtime.md)
- [Movement](NPC_Decision_and_Actions/Movement.md)
- [Farming](Farming/README.md)
- [Spawning and Pooling](Spawning_and_Pooling.md)

## 문서 갱신 조건

`NPCComponent` 표현 API, animator parameter, clip/controller, prefab visual·tool 배선이 바뀌면 갱신한다.
