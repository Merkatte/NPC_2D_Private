# Gold HUD

## 목적과 책임 경계

공유 골드 잔액·증감을 표시한다. 자원 저장과 transaction은 공유 자원 기능이 소유한다.

## 현재 실행 흐름

ResourceManager.ResourcesChanged → 골드 수량 조회 → 상시 HUD 갱신 또는 알림 HUD 연출. 자원 변경과 팝업 활성 상태는 서로 다른 표시 입력이다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/UI/GoldHUD.cs` | 공유 골드 잔액과 최신 증감을 이벤트 기반으로 표시하는 상시 HUD |
| `Assets/Scripts/UI/GoldNotificationHUD.cs` | 골드 변경 때 진입·잔액 보간·유지·퇴장하는 선택적 알림 HUD |

## 변경 유형별 최소 확인 범위

- 상시 표시: GoldHUD.cs, ResourceManager.cs, GoldHUD.prefab, BuildingTest/HUDUI.
- 알림 표시: GoldNotificationHUD.cs, GoldNotificationHUD.prefab, DefenseTest의 네 context popup.

## 불변 규칙

- 첫 조회는 증감을 표시하지 않는다. 다른 자원 변경은 골드 표시 타이머를 재시작하지 않는다.
- HUD는 골드 수량을 변경하지 않고 비활성화 때 구독과 자신이 소유한 연출을 정리한다.

## Unity 배선과 검증

상시 HUD와 선택적 알림 HUD의 scene 참조·초기화·수명은 아래 각 절을 따른다.

## 상시 골드 HUD

`GoldHUD`는 popup/hover registry에 등록하지 않는 상시 표시 컴포넌트다. scene의 `ResourceManager`를 serialized reference로 받고 `ResourcesChanged`에서 `GetQuantity(ResourceManager.GoldItemId)`를 조회한다. 골드 저장·차감·환급 규칙은 [Shared Resources](../../Economy/Inventory_and_Items/Shared_Resources.md)가 소유하며 HUD는 값을 변경하지 않는다.

활성화 때 현재 잔액을 천 단위 구분으로 즉시 표시하고 첫 조회에는 증감을 표시하지 않는다. 골드가 실제로 바뀐 경우만 최신 차이를 `+150`/`-100` 형식으로 표시한다. 다른 자원이나 용량 변경은 표시 타이머를 재시작하지 않는다. 증감은 기본 1.5초의 unscaled 시간 뒤 지우며 Inspector의 Change Display Duration이 0이면 표시하지 않는다. 비활성화 때 이벤트 구독·coroutine·이전 표시를 정리한다.

BuildingTest의 `Canvas/HUDUI/GoldHUD`에 `Assets/Prefab/UI/GoldHUD.prefab`을 배치한다. HUDUI는 Canvas의 마지막 자식으로 두어 popup과 겹칠 때도 잔액이 보이게 한다. 기존 HoveringUI·PopupUI의 상대 순서는 유지한다. 기존 목재 패널 `ui-merchant-board-panel-9slice.png`는 Sliced 이미지, `ui-gold-coin.png`의 `ui-gold-coin_0`는 골드 아이콘으로 재사용한다. AmountText와 ChangeText는 기존 UI와 같은 Legacy Text다. 배경·아이콘·텍스트의 raycast와 HUDUI의 blocksRaycasts를 꺼 world 클릭을 가로채지 않는다.

변경 시 먼저 `GoldHUD.cs`, `ResourceManager.cs`, `GoldHUD.prefab`과 BuildingTest의 HUDUI 배선을 확인한다. prefab은 scene 자원 저장소를 소유하지 않고 scene instance의 override로 ResourceManager를 연결한다.

## 선택적 골드 알림 HUD

`GoldNotificationHUD`는 DefenseTest에서 기존 상시 GoldHUD instance를 대체한다. UIManager registry에는 등록하지 않는다. 활성 root에 컴포넌트를 두고 별도 자식 `_panel` RectTransform, 같은 자식의 `_canvasGroup`, Legacy Text `_amountText`·`_changeText`를 연결한다. prefab의 `_resourceManager`는 비워 두며 scene override 또는 `Configure(ResourceManager)`로 주입한다. 필수 참조가 없으면 한 번 진단하고 숨김 상태를 유지하며 이후 유효한 Configure로 초기화할 수 있다. 다른 씬과 옛 GoldHUD prefab은 보존한다.

Awake에서 즉시 숨기고 첫 활성 초기화 때 실제 골드를 조용히 읽는다. `ResourcesChanged` 이후 골드 수량이 바뀐 경우만 위에서 내려온 뒤 이전 표시 잔액을 실제 잔액까지 보간하고, 잠시 유지한 뒤 위로 퇴장한다. 지출도 같은 경로로 감소하며 다른 자원·용량 알림은 무시한다. 표시 중 추가 변경은 현재 보간 값에서 새 실제 잔액으로 이어지고 유지 시간을 연장한다. 진입은 재시작하지 않으며 퇴장 중에는 현재 위치에서 반전한다. 최신 실제 변경의 차이를 부호 있는 숫자로 표시한다. 수량 보간은 double 계산 후 정수로 표시하고 완료 때 실제 int 잔액을 확정하여 큰 잔액의 반올림 손실을 피한다.

기본 위치는 좌상단 anchor/pivot의 `_visibleAnchoredPosition=(16,-16)`, 위쪽 `_hiddenOffset=96`이다. `_slideDuration=0.25`, `_countDuration=0.65`, `_holdDuration=1.5`는 Inspector에서 조절하고 유효한 비음수로 보정한다. 이동·수량·유지는 모두 unscaled 시간이다. `SetContextVisible(true)`는 단일 소비 UI가 열려 있는 동안 잔액을 유지하는 명시적 계약이며 false 뒤 정상 유지/퇴장한다. 현재 popup 연계는 아래 _contextPopups 계약을 따른다. 비활성화/재바인딩은 자신이 소유한 tween만 완료 callback 없이 중단하고 구독과 표시 상태를 정리한다. root를 끄지 않고 자식 위치와 alpha로 표시하며 모든 알림 Graphic과 CanvasGroup의 raycast를 차단하지 않도록 설정한다. 자원 변경 API는 호출하지 않는다.

`_contextPopups`에는 DefenseTest의 House/Construction/TownHall/Merchant 네 popup을 연결한다. Update는 활성 여부만 확인하며 하나라도 열려 있으면 SetContextVisible(true), 모두 닫히면 false로 전환한다. 자원 조회는 ResourcesChanged 경로로 유지한다. 최소 방어 상태 view DefensePhaseHUD의 주 소유와 배선 계약은 [Defense Progression](../../Combat/Defense/Progression.md)을 따른다. 실제 화면/Play 확인은 별도이며 코드만으로 검증 성공을 주장하지 않는다.

## 알려진 제약과 TBD

실제 화면·입력·Play 확인은 사람의 QA 항목이며 이 문서 재배치에서는 실행하지 않았다.

## 관련 문서

[UI 기능 인덱스](../README.md). 다른 기능의 transaction과 구체 UI는 해당 주 소유 문서를 따른다.

## 문서 갱신 조건

이 leaf의 실행 흐름, 주 소유 파일, 계약 또는 배선이 바뀌면 갱신한다.
