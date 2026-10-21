# DefenseTest 신규 UI 연동 계획

상태: 2026-10-20 구현 완료. strict 컴파일 오류0/경고0·참조 검사·독립 리뷰 Approve. 자동 Play/화면 검증은 제외하여 사람 QA 미검증. 기준 커밋 `97dfe8b130b7704f1c53d44fbde5aecf132ecf7f`.

## 목표와 범위

- DefenseTest만 주 씬으로 전환한다. 다른 씬은 수정·삭제·전환 검증하지 않는다.
- 신규 WarmPixel 주택·건설·시청·상인 팝업, GoldNotificationHUD, DefensePhaseHUD를 실제 데이터/입력에 연결한다. 씨앗 선택창은 기존 연결을 점검하고 창고·hover·이벤트·GameOver는 기존 기능을 보존한다.
- 기존 UI controller 클래스와 GUID를 유지한다. domain 조회·이벤트·명령을 재사용하고 자원·공사·주거·모집·거래·방어 규칙을 변경하지 않는다. 부족한 데이터가 실제로 확인되는 경우에만 추가 범위를 제시한다.
- 기존 prefab 파일은 보존한다. 다른 씬의 옛 배치를 위한 별도 표시 모드를 추가하지 않으며 전체 C# 컴파일은 유지한다.
- 승인된 WarmPixel 외형/아이콘과 Background/Header/Body/Footer 및 내용별 계층을 유지한다. 필요한 상태 문구·버튼·동적 행만 같은 스타일로 추가한다. 신규 그래픽 생성은 없다.
- PC 대상. 작은 화면은 기존 시청 방식의 전체 비율 축소(여백24, 확대 없음). 해상도별 내부 재배치는 제외한다.

## 공통 계약

- Popup은 조회·구독·선택·요청 전달, 하위 view는 값 표시와 입력 의도 전달만 담당한다. PopBase/UIManager/PopupType 흐름을 유지하고 닫기는 RequestClose를 사용한다.
- 비활성 최초 바인딩에서도 초기화 가능해야 한다. 재바인딩·닫기·강제 disable에서 구독/선택/실행 중 표현을 정리한다. 대상 비활성/파괴는 닫는다.
- PopupCanvasFit을 신규 UI 공통 컴포넌트로 추가하고 신규 네 popup과 기존 SeedSelection에 적용한다. 루트 scale만 소유하며 PopBase 위치와 주택 내부 너비 전환은 건드리지 않는다. TownHall의 중복 fit 로직은 이관한다.
- 주택/건설은 공유 ResourceCostRowView로 필요/보유와 부족 색을 표시한다. 재료 수는 가변이며 미등록 아이콘도 이름/수량을 누락시키지 않는다.
- 신규 prefab GUID를 보존하고 DefenseTest registry/source 참조만 이관한다. 옛 크기 override는 복사하지 않는다. scene 의존성은 prefab에 저장하지 않고 instance에 연결한다.

## 순차 구현

### 1. 주택

- HousePopup이 데이터/입력을 소유하고 WarmPixelHousePopup은 열기·접기·즉시 초기화 API와 배치 전환만 소유한다. 표현 컴포넌트의 자체 닫기/중복 버튼 listener를 제거한다.
- 실제 단계/그림/정원/입주/현재 효과, 실제 주민 목록(기존 이름 조회와 공용 초상), 빈 목록을 표시한다. 피로 회복은 생활 기본값+해당 단계 보너스다.
- 기본은 전체 폭 현재 정보. 업그레이드로 본창 축소/오른쪽 비교 drawer를 0.3초 unscaled 전환한다. 연속 입력 반전과 텍스트 크기 유지, 닫힌/전환 중 drawer 입력 차단을 보존한다.
- 비용 부족이어도 비교를 열 수 있고 실제 시작만 제한한다. 전체 효과의 추가/유지/변경/제거를 비교하며 단계 상속을 추정하지 않는다. 최고 단계는 안내와 다음 단계 숨김이다.
- 공사 중 본창은 진행 요약, 버튼은 업그레이드 현황. drawer는 진행률·작업 인원·예상 환불·취소·완공 재시도. 시작 성공은 열린 drawer를 진행 상태로 바꾸고 완료/취소는 접고 갱신한다. 실패는 현재 화면과 이유를 유지한다.
- 재바인딩/재열기에는 접힌 상태. 기존 House/Plot/Resources 이벤트와 검증된 upgrade/cancel/retry API를 사용한다.
- Canvas 직속 시안을 PopupUI 정식 instance로 통합하고 기존 House 등록/중복 instance를 정리한다. Defense 전용 HousingDataContext와 BuildingDataContext를 사용한다.

### 2. 건설

- Definitions와 IsAllowed로 가변 카드 목록을 만들고 정의 순서의 첫 유효 건물을 기본 선택한다. 없으면 빈 안내.
- 선택된 이미지·이름·역할 설명·실제 비용을 표시한다. 역할 설명은 UI 설정이며 비용/기능 수치는 기존 정의에서 조회한다. 선택은 무지출, 확정은 TryStartConstruction이다.
- 공사 중 상세를 진행률·작업 인원·환불·취소로 전환한다. 완료 시 닫고 완료 실패 안내를 유지한다.
- 재고 갱신에는 선택/스크롤 유지, 대상 변경/재열기에는 초기화한다.

### 3. 시청

- 왼쪽 선택과 오른쪽 공유 상세/모집을 연결한다. Defense의 Farmer/Guard/Builder/Archer 네 직군을 명시 구성한다. enum 전체 열거나 세 예시만 하드코딩하지 않는다.
- 기본 Farmer, 없으면 첫 유효 직군. 선택 자체는 무지출이며 모집 버튼만 기존 dispatch API를 호출한다.
- RecruitmentStatus의 가능 여부·지원금·남은 시간·도착 중을 표시한다. 성공 후 선택/창 유지, 부족/스폰 불가/궁병 자리 부족 메시지 유지. 열기/탭으로 타이머 초기화하지 않는다.
- 체력/이속/작업속도/운반량은 —, 상세 준비 중. 마을 상태는 기존 준비중입니다 안내.

### 4. 상인

- MerchantPopup과 기존 pendingSales가 유일한 선택 상태. 신규 상품 행/명세 view를 바인딩하고 옛 drag/수량 popup을 신규 prefab에 사용하지 않는다. 기존 다른 C# 호출 계약은 컴파일 가능하게 보존한다.
- 입력은 절대 수량, +/-는1. 직접 입력 확정 시0..현재 재고,0은 제거; 빈/잘못된/음수는0. 판매 클릭은 활성 입력부터 확정한다.
- GetAvailableOffers/단일·전체 TryGetSaleQuote/TryTrade로 표시/견적/거래를 수행한다. UI에서 실제 가격 거래 규칙을 복제하지 않는다.
- 외부 재고 변경으로 기존 선택이 부족해져도 즉시 지우지 않고 부족 표시. 성공은 선택 초기화, OutOfStock만 재고로 조정, InvalidRequest는 선택 유지.
- 빈 상품/명세/0G/결과 메시지 지원. 기본 판매 탭, 구매는 준비중입니다. 탭 왕복은 선택 유지, 닫기/재열기/상인 퇴장은 세션 초기화.

### 5. HUD

- 기존 Defense GoldHUD instance를 GoldNotificationHUD로 교체하고 동일 ResourceManager를 연결한다. 최초 잔액은 조용히 읽고 변경 시 기존 unscaled 등장/수량보간/유지/퇴장.
- GoldNotificationHUD에 네 popup 참조 목록을 추가한다. 활성 여부만 저비용 확인해 하나라도 열리면 SetContextVisible(true). 자원 조회는 ResourcesChanged로 유지. 마지막 창을 닫으면 기존 hold 이후 퇴장.
- DefensePhaseHUD에 session/waves 연결. 기존 DefenseHUD에 phase control 표시를 끄는 설정을 추가하여 DefenseTest의 옛 상태/시작 버튼을 숨기고 event 알림/열기/GameOver 소유는 유지한다. 기존 Configure 시그니처 유지.
- 이벤트 host/선택 modal/pause lease/GameOver 차단 구조 유지. 모달은 일반 팝업과 HUD 위, 보이는 phase/시작 버튼은 한 벌만.

### 6. 통합

- 기존 SeedSelection instance 재사용/참조 검사/fit 적용, Warehouse와 hover 보존. registry는 종류별 하나, 운영 popup 초기 비활성, HUD host 활성.
- 예시 주민/보유량/선택/예시 안내를 실제 바인딩으로 교체한다. 옛 전체씬 생성/옛UI 재조립 helper는 실행하지 않는다.

## 검증과 운영

- ordinary common-check route, RequiredByRequestOrSkill 독립 리뷰. code/assembly 역할 분리, fork_turns=none, 배선은 코드 컴파일 후. 같은 Editor import/compile/save 순차. root는 문서·범위·증거·리뷰 조정만 수행한다.
- 현재 strict Unity 컴파일, import, 실제 diff·참조·버튼·registry·모달 순서·네 모집 직군·전용 context 검사. domain/다른 씬 변경 금지 확인. 최종 후보 독립 Approve 필수. 보정 예산2회.
- 사람 QA: 주택 모든 단계/빈 거주/가변 효과·비용/부족 비교/시작·취소·완료·재시도/전환반전·재바인딩; 건설 상태/허용 목록; 네 직군 모집·실패·타이머; 상인 입력즉시판매/재고경합/실패별선택/퇴장; 연속골드/중첩창/정지중전환/모달차단/작은PC화면.
- 자동 Play/스크린샷 제외, 새 검증전용 runner/test/helper 작성 금지. 미실행 QA는 미검증으로 기록한다. 실제 구현·조립용 helper는 code 작성/assembly 실행.
- baseline/dirty/Editor 상태를 확인하고 미저장 사용자 상태를 보호한다. SaveAll/discard/전체 scene 재생성 금지. 한 작업 기록에 범위·검사로그·리뷰·남은 QA만 유지한다. 관련 Systems와 PROGRESS를 실제 구현에 맞게 갱신한다.
