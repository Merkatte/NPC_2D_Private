# Player Gold

## 기능 목적과 책임 경계

플레이어가 소유하는 단일 전역 골드 잔액과 그 획득·지출 transaction만 소유한다.

골드는 NPC별 소유가 아니라 플레이어 소유이며, 획득 즉시 어디서나 사용할 수 있다. 따라서
scene 단위 단일 component 하나가 잔액 전체를 소유한다.

이 문서가 소유하지 않는 것: 가격과 거래 품목·판매 흐름은 [Merchant Caravan](Merchant_Caravan.md),
고용(정착지원금) 비용은 [Town Hall](Town_Hall.md)이 소유한다. 구매 흐름과 골드 표시 UI는 아직
구현되어 있지 않다.

## 현재 실행 흐름

```text
TestGoldWindow 버튼 클릭
  -> GoldManager.Add(amount)          획득. 실패 개념이 없다.
  -> GoldManager.TrySpend(amount)     지출. 전액 성공 또는 전액 거부.
  -> CurrentGold 재조회 후 화면 label + Debug.Log 1회
```

`GoldManager`는 `Awake`에서 `_initialGold`를 0 이상으로 보정해 잔액을 초기화한다. Unity는
serialized field를 field initializer 이후에 역직렬화하므로 이 초기화는 `Awake`에서만 정확하다.

`Add`는 실패 경로가 없다. 0 이하 금액은 획득이 아니므로 무시하고, 합이 `int.MaxValue`를 넘으면
음수로 감기는 대신 `int.MaxValue`에서 포화한다.

`TrySpend`는 부분 지출을 허용하지 않는다. 금액이 0 이하이거나 잔액이 부족하면 `false`를
반환하고 잔액을 전혀 바꾸지 않는다. **잔액 부족은 정상 gameplay 결과이며 구성 오류가 아니므로
`GoldManager`는 이를 로그하지 않는다.** 표시 방법은 호출자가 결정한다.

`GoldManager`에는 `Debug` 호출이 하나도 없다. 실패 경로가 모두 정상 결과이고 주입받는
dependency가 없어 초기화 경계에서 진단할 대상이 없다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Scripts/Manager/GoldManager.cs` | 플레이어 전역 골드 잔액과 획득·지출 transaction의 단일 소유자 |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·문서 |
|---|---|
| 획득·지출 계약, 잔액 guard | `GoldManager.cs` |
| 새 골드 획득·지출 지점 | `GoldManager.cs`, 호출하는 domain 문서 |
| 시작 잔액·scene 배선 | `GoldManager.cs`, `Assets/Scenes/FarmerTest.unity` |
| 수동 검증 도구 | `Assets/TestOnly/TestGoldWindow.cs` |

## 불변 규칙

- 잔액을 mutate하는 코드는 `GoldManager` 안에만 존재한다. 외부는 `Add`/`TrySpend`만 호출한다.
- `CurrentGold`는 읽기 전용이며 setter를 제공하지 않는다.
- 잔액은 음수가 될 수 없다.
- `TrySpend`는 전액 성공 또는 전액 거부다. 부분 지출은 없다.
- `TrySpend`가 `false`를 반환한 뒤 잔액이 변경된 채 남지 않는다.
- 잔액 부족과 0 이하 금액은 정상 결과이므로 `GoldManager`에서 로그하지 않는다.
- `GoldManager`는 static instance를 제공하지 않는다. 소비자는 serialized reference나 명시적
  init으로 주입받는다.
- 골드는 ScriptableObject에 저장하지 않는다. 현재 잔액은 scene runtime 상태다.
- `int` 포화 처리를 제거하지 않는다. 제거하면 `Add` 누적이 음수 잔액으로 감긴다.

## Unity 배선과 검증 도구

`FarmerTest.unity`의 `Systems > Manager` 아래(`NPCManager`/`DataManager`/`InteractManager`/
`UIManager`와 같은 부모, 같은 형제 레벨)에 `GoldManager` GameObject를 두고 `_initialGold`를
설정한다. `TestOnly!!` GameObject에 `TestGoldWindow`를 붙이고 `_goldManager`를 Inspector에서
명시적으로 연결한다. `TestGoldWindow`는 `GoldManager`를 찾지 못하면 `FindFirstObjectByType`
fallback을 쓰지만, `TestNPCSpawnWindow`와 마찬가지로 배선은 명시적으로 한다.

`TestGoldWindow`는 화면 window에 현재 잔액을 표시하고, 버튼 클릭 1회마다 이전 잔액 → 현재
잔액과 `TrySpend` 반환값을 `Debug.Log`로 1줄 남긴다. 매 frame이 아니라 사용자 조작마다 1회다.
production 코드가 아니라 이 도구가 모든 로그를 소유한다.

`GuardTest.unity`는 아직 같은 배선을 하지 않았다(2026-09-05 기준).

## 알려진 제약과 TBD

- 상인 캐러밴(생산물 판매, [Merchant Caravan](Merchant_Caravan.md))이 `Add`를, 시청 모집
  ([Town Hall](Town_Hall.md))이 `TrySpend`를 실제로 호출하는 gameplay 경로다. `TestGoldWindow`는
  여전히 수동 검증용 호출자로 남아 있다. 문서명을 경제 전반으로 넓힐지는 추가 소비·획득 지점이
  생기는 시점에 재검토한다.
- 잔액 변경 알림 event나 callback이 없다. 표시 UI가 없으므로 현재 필요가 없고, 단일 mutator
  구조라 필요해지면 `Add`/`TrySpend` 내부에 추가하는 것만으로 충분하다.
- 저장·불러오기가 없다. Play Mode를 나가면 잔액은 `_initialGold`로 돌아간다.
- `PublicMD/Status/PROGRESS.md`의 오래된 "이후 과제" 절에 `IRecruitmentCostPolicy`를
  "골드/지갑 seam"이라 부르는 대목이 있으나, 그 모집 시스템 자체가 2026-08-01 리셋으로
  코드베이스에서 완전히 삭제됐다. 이후 구현된 [Town Hall](Town_Hall.md) 모집은 이 인터페이스를
  전혀 쓰지 않고 `TownHallRecruitment`가 `GoldManager.TrySpend`를 직접 호출한다 — 여전히 죽은
  참조이며 `GoldManager`는 어떤 모집 전용 인터페이스도 구현하지 않는다.
- `Game_Plan.md` §7.1은 골드를 "런타임 일반 C# 객체"로 분류한다. `GoldManager`는 `WorkerInventory`처럼
  완전한 plain C#은 아니고 `WarehouseInventory`처럼 scene MonoBehaviour가 소유하는 runtime
  필드다 — scene 전역에서 단일 참조점으로 주입돼야 해서 plain C#으로는 그 배선을 표현할 수
  없다. "정의 asset이 아닌 실행 상태"라는 그 행의 취지는 만족한다.

## 관련 문서

- [Inventory and Items](Inventory_and_Items.md) — 골드가 아닌 item 수량 저장소
- [Merchant Caravan](Merchant_Caravan.md) — `Add` 호출자
- [Town Hall](Town_Hall.md) — `TrySpend` 호출자
- [Project Structure](../ProjectStructure.md)
- [Game Plan](../Game_Plan.md) §5.7 성장과 경제 — 골드 순환(판매·구매)과 용도(모집·투자)의 상위 기획 의도

## 문서 갱신 조건

골드 획득·지출 계약, 잔액 guard, 새 호출자 추가, scene 배선이 바뀌면 갱신한다.
