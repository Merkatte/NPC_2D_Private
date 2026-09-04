# NPC Presentation

## 기능 목적

NPC의 이동, 좌우 방향, 건물 출입, 작업 도구와 Animator 표현을 gameplay 판단에서 분리해 설명한다.

## 책임 경계

- `NPCComponent`는 actor-local Unity 참조와 표현 API를 소유하며, plain C# runtime state(`CombatRuntimeState`, `WorkerInventory`)를 인라인으로 들고 그 표시 요청을 실제 Unity 조작으로 옮긴다.
- 봇짐을 "언제 보일지"는 `WorkerInventory`가 판단하고, 실제로 켜고 끄는 것은 `NPCComponent.SetCarryVisible` 한 곳뿐이다. `SetCarryVisible`은 `CarryVisualPresenter.SetVisible`에 위임하며, presenter가 Animator 재생과 중복 호출 guard를 소유한다.
- selector와 action은 `NPCComponent`의 의미 기반 메서드만 호출하고 Animator parameter나 visual child를 직접 찾지 않는다. `HarvestAction`/`DepositAction`도 마찬가지로 화물 Animator를 직접 만지지 않는다.
- NPC의 방향은 animation이 잡는 visual rotation이 아니라 이동 root의 local scale로 반전한다.
- presentation은 gameplay 완료 시간을 결정하지 않는다. `CarryVisualPresenter.SetVisible`은 bool 세팅과 `Animator.SetBool` 호출로 끝나 transaction을 블록하지 않는다.
- 봇짐의 반복 이동 모션(걷기 띠용거림 등)과 1회성 등장/퇴장 모션은 서로 다른 Animator가 서로 다른 Transform을 소유한다 — 메인 Animator(`NPCGirl` 루트)는 `Visual`과 `Visual/ToolAnchor/Tool`만, 화물 Animator(`CarryAnchor`)는 `CarryMotion` 하나만 바인딩해 겹치지 않는다.

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

WorkerInventory 적재/이관/Clear -> 생성자 콜백
  -> NPCComponent.SetCarryVisible(bool)
  -> CarryVisualPresenter.SetVisible(bool)
     -> 이전과 같은 상태면 무시 (재수확 시 Show 재생 안 함)
     -> false -> true: 화물 Animator의 HasCargo=true (Hidden -> Show -> Visible)
     -> true -> false: 화물 Animator의 HasCargo=false (Visible -> Hide -> Hidden)

NPCComponent.Awake / ResetRuntimeState -> CarryVisualPresenter.ResetImmediate()
  -> Hide/Show 애니메이션 재생 없이 즉시 Hidden으로 스냅 (pool 재사용 대비)
```

`Visual`의 자손인 `CarryAnchor -> CarryMotion -> Basket` 계층은 부모 `Visual`이 Idle/Move/ToolWork에서 움직이는 `localPosition.y`/`localScale`을 자동으로 상속한다. `CarryMotion`은 그 위에 자기 자신의 등장/퇴장 모션만 합성하므로 메인 클립에 화물 트랙을 추가할 필요가 없다.

도구 표시(`SetToolVisible`)는 `WorkerNPC.Init`이 정하는 역할 스위치이고, 봇짐 표시는 cargo 상태를 따르는 별개 스위치다. 두 스위치를 겹치지 않게 두어 pool 재사용 시 복원 순서에 의존하지 않는다 — 운반 중에도 호미는 계속 보인다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/System/Actor/NPCComponent.cs` | NPC 이동 root, 방향 scale, Animator parameter, 도구 표시, cargo와 combat runtime state 소유, 화물 표시 요청을 `CarryVisualPresenter`로 전달 |
| `Assets/Scripts/System/Actor/CarryVisualPresenter.cs` | 화물 전용 Animator 재생, 동일 상태 재요청 guard, pool 재사용을 위한 즉시 Hidden 복구 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 이동·Flip | `NPCComponent.cs`, `Assets/Prefab/InGame/NPCGirl.prefab` |
| 통통 튀는 이동 clip | `Assets/Animation/DefaultAnim/NPCGirl_Move.anim`, `Assets/Animation/NPCGirl_Move.controller` |
| 건물 출입 표현 | `NPCComponent.cs`, [Interaction and Destinations](Interaction_and_Destinations.md), enter/inside/exit clips |
| 호미 표시·작업 모션 | `NPCComponent.cs`, tool carry/work clips, NPC prefab |
| 봇짐 등장/퇴장 연출 | `CarryVisualPresenter.cs`, `Assets/Animation/Carry/*`, NPC prefab의 `CarryAnchor/CarryMotion/Basket` 계층 |
| 봇짐 표시 판단(비었는지)·용량 | `NPCComponent.cs`, [Inventory and Items](Inventory_and_Items.md)의 `WorkerInventory` |
| Animator 없는 role | `NPCComponent.cs`, 해당 role prefab, spawning 문서 |

## 에셋과 검증 도구

- `Assets/Animation/NPCGirl_Move.controller`: `Speed`, `IsInsideBuilding`, `IsWorking` parameter와 상태 전이를 소유한다. 메인 Animator(`NPCGirl` 루트)가 사용하며 `Visual`/`Visual/ToolAnchor/Tool`만 바인딩한다.
- `Assets/Animation/DefaultAnim/*`: idle, move, building, tool animation clip.
- `Assets/Animation/Carry/NPCGirl_Carry.controller`: `HasCargo` parameter와 `Hidden -> Show -> Visible -> Hide -> Hidden` 상태 전이를 소유한다. `CarryAnchor`의 전용 Animator가 사용하며 `CarryMotion`만 바인딩한다.
- `Assets/Animation/Carry/NPCGirl_CarryHidden.anim` / `CarryShow.anim` / `CarryVisible.anim` / `CarryHide.anim`: 화물 등장(작게 뿅 나타나 떨어지며 착지 후 반동)·퇴장(위로 튀며 축소) 1회성 clip. 알파 페이드 대신 position/squash-stretch만 사용.
- `Assets/Prefab/InGame/NPCGirl.prefab`: 메인 Animator, visual, tool renderer, sensor, 화물 Animator+`CarryVisualPresenter`가 배선된 공유 NPC prefab.
- `Assets/TestOnly/Editor/NPCGirlAnimatorControllerConfigurator.cs`: 메인 controller의 parameter와 전이를 멱등 구성·검증한다.
- `Assets/TestOnly/Editor/NPCGirlToolLayerConfigurator.cs`: tool layer와 clip 배선을 구성·검증한다.
- `Assets/TestOnly/Editor/NPCGirlCarryVisualConfigurator.cs`: 화물 controller의 parameter·상태·전이 배선을 검증한다(수정하지 않고 검증만).

## 불변 규칙

- animation이 visual rotation을 점유할 수 있으므로 좌우 반전은 root scale 경계를 유지한다.
- `_requiresAnimator`가 true인 role은 필수 parameter 누락을 오류로 보고한다.
- pool 재사용 시 Speed, inside, working, target, cargo 표현 상태를 초기화한다. 화물은 `CarryVisualPresenter.ResetImmediate()`가 Hide 연출 없이 즉시 Hidden으로 되돌린다.
- action은 animator state 이름이나 tool·carry child를 직접 참조하지 않는다.
- `_toolRenderer`와 `_carryPresenter`는 서로 다른 스위치가 소유하며 같은 상태를 두 곳에서 쓰지 않는다.
- 표시용 renderer·presenter 참조는 모두 null 허용이며, 배선되지 않은 role에서도 gameplay는 그대로 동작해야 한다.
- 메인 Animator와 화물 Animator는 같은 Transform 속성을 동시에 제어하지 않는다. `CarryAnchor`/`CarryMotion` 경로는 기존 Idle/Move/ToolWork 클립에 추가하지 않는다.

## 알려진 제약과 TBD

- Enemy는 현재 전용 Animator가 없어 `_requiresAnimator = false`다.
- Farmer와 Guard가 같은 NPC prefab을 공유하며 역할별 전용 presentation 분리는 아직 없다.
- 작물별 봇짐 스프라이트는 `itemId -> Sprite` 테이블이 없어 지원하지 않는다. 현재는 `worker-cargo-basket.png` 단일 스프라이트만 표시한다.
- Hide 애니메이션 재생 도중 같은 tick에 새 화물이 들어오는 동시성 edge case는 다루지 않는다 — `TryTransferAllTo` 직후 같은 tick에 `TryAdd`가 성립하는 경로가 현재 구조에 없다.

## 관련 문서

- [NPC Runtime](NPC_Runtime.md)
- [Movement](NPC_Decision_and_Actions/Movement.md)
- [Farming](Farming/README.md)
- [Spawning and Pooling](Spawning_and_Pooling.md)

## 문서 갱신 조건

`NPCComponent` 표현 API, animator parameter, clip/controller, prefab visual·tool 배선이 바뀌면 갱신한다.
