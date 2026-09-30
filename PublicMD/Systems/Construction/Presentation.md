# Construction Presentation

## 책임과 주 소유 스크립트

`Assets/Scripts/Actor/ConstructionVisual.cs`는 부지의 자재·골조·가림막 표시와 DOTween 수명을 소유한다. 같은 오브젝트의 BuildingPlot이 제공하는 State/Progress를 읽고 StateChanged를 구독한다. 비용·작업·예약·완공 판정은 변경하지 않는다.

## 변경 유형별 최소 확인 범위

- 단계·애니메이션: ConstructionVisual과 [Plots](Plots.md)의 상태/알림 계약.
- 이미지·배선: BuildingTest의 6개 BuildingPlot, Construction/Stages 이미지 importer, TestOnly/Editor/BuildingConstructionSetup의 ApplyConstructionVisuals.
- 실제 시설 생성: [Facilities](Facilities.md). 팝업 표시: [UI](UI.md).

## 표시 규칙

- 기본 진행률 0/25/50/75%에서 자재·하부 골조·상부 골조·가림막을 누적 표시한다. 임계값은 Vector4 표현 설정이다.
- 새 레이어는 기본 0.4초 동안 등장한다. 자재는 위에서 내려오고, 골조와 가림막은 아래에서 올라오며 세로 크기와 불투명도를 복원한다. 동일 단계 알림은 재생을 반복하지 않으며 건너뛴 단계는 함께 표시한다.
- Completed에서 기본 0.2초 축소·페이드로 공사 표시를 제거한다. Factory 실패로 UnderConstruction에 남으면 진행률 100%여도 유지한다. 완료 도메인 처리를 애니메이션 종료까지 지연하지 않는다.
- 취소 시 빈 부지 표시를 즉시 복원하고 공사 레이어를 제거한다. 즉시 재착공하면 이전 tween을 취소하고 초기 자세에서 시작한다.
- 활성화 시 현재 상태를 즉시 반영한다. 비활성화 시 구독과 tween을 정리하고 정지 자세를 복원한다. 시간 배율을 따른다.

## 배선과 제약

같은 부지 루트에 BuildingPlot과 ConstructionVisual을 연결한다. EmptyPlot 및 Materials/LowerFrame/UpperFrame/Cover는 직접 자식이며 네 SpriteRenderer는 서로 달라야 하고 sprite가 있어야 한다. 표시 자식에는 collider를 두지 않는다. 부지·시설 루트와 작업점은 애니메이션 대상이 아니다.

BuildingTest는 기존 Materials/EmptyPlot을 재사용하며 Scaffolding을 LowerFrame으로 전환한다. 새 세 레이어는 1536×1024, PPU150, bottom-center pivot, sorting order 5/6/7, 공통 localY 1.0866667이다. 자재 importer·위치는 기존값을 유지한다. 새 레이어의 발바닥 기준을 기존 그림과 맞춘 오프셋이다.

전용 적용 메뉴는 세 이미지 import와 BuildingTest 부지 6개만 변경하며 저장되지 않은 씬·Play·컴파일·PrefabStage에서는 거부한다. 원래 씬 구성을 복원한다. 전체 Setup은 더 넓은 초기 조립 작업이므로 표현만 갱신할 때 사용하지 않는다.

검증 결과는 PROGRESS를 따른다. 자동 Play/게임 화면 검증은 제외하며 실제 단계 전환·레이어 겹침·취소/재착공은 사람 확인 항목이다.
