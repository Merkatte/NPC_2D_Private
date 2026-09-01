# Crop Presentation

## 기능 목적

FarmWorkSite의 current definition·phase·progress를 성장 stage sprite, 무작위 cascade transition과 최종 수확 소멸로 표현한다.

## 책임 경계

- `FarmCropPresenter`는 FarmWorkSite 상태 이벤트를 구독하고 visual command queue와 무작위 순서를 소유한다.
- `CropVisualAnimator`는 하나의 작물 visual에서 sprite와 Animator 상태를 적용한다.
- `CropVisualStage`는 normalized threshold와 sprite를 전달하는 공유 값이다.
- presentation은 gameplay 상태, inventory, yield와 production 난수를 변경하지 않는다.

## 현재 실행 흐름

```text
crop 최초 선택 / component 재활성화
  -> 현재 stage를 모든 visual에 즉시 동기화

Growing threshold 상승
  -> 통과한 stage command를 순서대로 queue
  -> visual 전용 seed로 전체 visual 순서를 shuffle
  -> 0.08초 간격으로 Disappear -> sprite 교체 -> Appear

최종 수확으로 current crop 제거
  -> 전체 visual을 shuffle 순서로 Disappear -> hide
  -> 소멸 중 새 crop 선택 시 소멸 완료 뒤 첫 stage 즉시 표시
```

Harvesting 중 progress가 감소해도 마지막 stage를 유지한다. disable/re-enable 시 실행 중 coroutine을 취소하고 현재 FarmWorkSite 상태로 즉시 복원한다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/Struct/CropVisualStage.cs` | crop stage threshold와 sprite 값 |
| `Assets/Scripts/System/Farming/CropVisualAnimator.cs` | 단일 crop visual의 즉시 표시·stage transition·수확 소멸 |
| `Assets/Scripts/System/Farming/FarmCropPresenter.cs` | farm 단위 visual 상태 동기화, command queue와 shuffle cascade |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| cascade 순서·lifecycle | `FarmCropPresenter.cs`, `FarmWorkSite.cs` |
| Animator state·clip | `CropVisualAnimator.cs`, `CropLeafSway.controller`, Crop animation clips |
| stage sprite·threshold | `CropVisualStage.cs`, `FarmProductionDefinition.cs`, crop definition asset |
| visual 수·위치 | 대상 scene/prefab, `FarmCropPresenter` Inspector 배열 |

## 불변 규칙

- stage가 증가할 때만 transition하며 한 이벤트가 여러 threshold를 넘으면 각 stage를 한 번씩 queue한다.
- visual shuffle은 별도 `System.Random` stream을 사용해 harvest yield 순서에 영향을 주지 않는다.
- Animator Controller에는 `Appear`, `Disappear`, `Leaf Sway` state가 있어야 한다.
- null·중복 visual은 한 번 진단하고 제외하며 gameplay는 계속된다.
- presentation이 비활성화되거나 없어도 FarmWorkSite transaction은 동일하게 완료된다.

## Unity 배선과 검증 도구

- `FarmCropPresenter`: `_workSite`, 미리 배치한 `CropVisualAnimator[]`, visual seed와 cascade interval을 배선한다.
- `CropVisualAnimator`: 같은 object hierarchy의 `Animator`와 `SpriteRenderer`를 배선한다.
- 첫 구현은 약 10개 visual의 위치와 배열 배선을 사용자가 scene/prefab에서 수동 구성한다.
- `CropLeafSway.controller`와 `CropAppear`/`CropDisappear`/`CropLeafSway` clips가 공통 state 계약을 제공한다.

## 알려진 제약과 TBD

- 현재 production scene에는 약 10개 visual과 `FarmCropPresenter`가 아직 배선되지 않아 Play Mode visual 검증은 `NOT VERIFIED`다.
- Potato sprite는 현재 bone idle rig가 없어 공통 controller의 leaf sway가 시각적으로 정적일 수 있다.
- runtime crop prefab 생성과 자동 위치 배치는 현재 범위에 없다.

## 관련 문서

- [Farming index](README.md)
- [Runtime and Transactions](Runtime_and_Transactions.md)
- [Definition and Catalog](Definition_and_Catalog.md)

## 문서 갱신 조건

stage 계산, visual queue, Animator state 계약, shuffle·cascade와 scene 배선이 바뀌면 갱신한다.
