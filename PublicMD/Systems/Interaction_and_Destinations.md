# Interaction and Destinations

## 기능 목적

NPC가 목적지를 찾고, 그 장소가 제공하는 capability를 조회하며, 명시적 request로 상호작용하는 공통 protocol을 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| Destination registry | `BuildingType`을 위치와 destination object로 변환 |
| Provider registry | destination object와 `ActionType`으로 provider 조회 |
| Interaction protocol | 지원 여부, option 열거, request 실행, result 반환 |

세부 기능이 4개 미만이므로 현재는 단일 문서로 유지한다.

## 현재 실행 흐름

```text
DestinationDB.TryGetDestinationPos(BuildingType)

DestinationDB.TryGetInteractionProvider(BuildingType, ActionType)
  -> destination row의 GameObject
  -> InteractableManager.TryGetInteractionProvider(GameObject, ActionType)
  -> IInteractionProvider

공급 action:
  provider.AppendOptions()
  -> selector가 InteractionRequest 선택
  -> ActionContext(provider, request)
  -> provider.TryInteract()
  -> InteractionResult의 StatEffect 적용
```

`BaseBuildingAction`은 Eat/Drink/Sleep의 실내 표현을 시작하고 완료·실패·재판단·취소·pool 반환 모든 경로에서 정리한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/InteractionOption.cs` | utility 판단에 제공되는 interaction 후보 값 |
| `Assets/Data/Struct/InteractionRequest.cs` | 선택된 action, option, strength를 전달하는 요청 값 |
| `Assets/Data/Struct/InteractionResult.cs` | provider 실행 결과의 actor 효과 값 |
| `Assets/Scripts/Actor/BaseInteractionProvider.cs` | 공통 초기화·검증·dispatch template을 제공하는 provider base |
| `Assets/Scripts/Actor/Pub.cs` | item table을 이용해 Eat·Drink option과 결과를 제공하는 facility |
| `Assets/Scripts/Enum/BuildingType.cs` | destination registry의 안정적인 건물 key |
| `Assets/Scripts/Interface/IInteractionProvider.cs` | support·availability·option·execute 공통 계약 |
| `Assets/Scripts/Manager/InteractableManager.cs` | scene provider 초기화와 `(GameObject, ActionType)` registry |
| `Assets/Scripts/System/Action/BaseBuildingAction.cs` | 실내 action의 건물 출입 표현 lifecycle |
| `Assets/Scripts/System/Lib/DestinationDB.cs` | 건물 위치 조회와 provider registry 위임 진입점 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 새 destination | `BuildingType.cs`, `DestinationDB.cs`, 대상 scene |
| 새 facility provider | `IInteractionProvider.cs`, `BaseInteractionProvider.cs`, `InteractableManager.cs` |
| Eat·Drink option | `Pub.cs`, interaction value types, [Needs Actions](NPC_Decision_and_Actions/Needs_Actions.md) |
| 실내 표현 | `BaseBuildingAction.cs`, [NPC Presentation](NPC_Presentation.md) |
| 농장 interaction | [Farming](Farming.md) |

## 불변 규칙

- 새 facility마다 domain 전용 provider interface나 `TryGetXxxProvider`를 추가하지 않는다.
- provider는 자신의 domain transaction만 실행하고 NPC의 다음 행동을 결정하지 않는다.
- action은 scene registry를 직접 검색하지 않고 selector가 주입한 provider와 request를 사용한다.
- provider 초기화는 idempotent해야 하며 중복 `(object, action)` 등록은 첫 항목을 보존하고 오류로 보고한다.
- provider는 고정 scene dependency 부재만 초기화 실패로 취급한다. 정상적으로 바뀔 수 있는 domain 상태(예: 농경지에 아직 아무것도 안 심긴 상태)로 인터랙션을 막을 때는 `CanInteractCore`를 override해서 게이트하고, `TryInitializeCore`를 실패시키지 않는다 — `BaseInteractionProvider._isOperational`은 첫 초기화에서 latch되므로 여기서 실패시키면 이후 상태가 바뀌어도 영구히 복구되지 않는다. 예시: [Farming](Farming.md)의 `FarmWorkSite.CanInteractCore`.

## Unity 배선

`DestinationDB` row에는 `BuildingType`, 이동 위치, provider가 붙은 destination object를 연결한다. 모든 `BaseInteractionProvider`는 `InteractableManager._interactables`에 등록한다.

## 알려진 제약과 TBD

- `SleepAction`은 현재 provider transaction 없이 건물 위치와 시간 기반으로 동작한다.
- destination row의 중복 key는 현재 마지막 유효 row가 dictionary 값을 덮어쓴다.

## 관련 문서

- [Decision Policy](NPC_Decision_and_Actions/Decision_Policy.md)
- [Action Runtime](NPC_Decision_and_Actions/Action_Runtime.md)
- [Farming](Farming.md)

## 문서 갱신 조건

provider protocol, destination lookup, registry 조립, request/result shape 또는 건물 action lifecycle이 바뀌면 갱신한다.
