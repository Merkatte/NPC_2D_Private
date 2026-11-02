# Environment Presentation

## 목적과 책임

`InstancedTreeGrove`는 장면의 장식용 나무를 데이터와 GPU instancing으로 표시한다.
각 나무에 GameObject, SpriteRenderer, Animator를 만들지 않는다. 충돌, 경로 탐색,
벌목 자원, 목적지 등록은 소유하지 않는다. 현재 용도는 `DefenseTest`의 북쪽 배경이다.

## 주 소유 파일

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/Actor/InstancedTreeGrove.cs` | 고정 시드 배치, 공용 quad, 수종별 행렬 묶음과 렌더링 수명 |
| `Assets/Art/Shaders/InstancedTreeWind.shader` | 인스턴스 변환, 12프레임 atlas 선택, 알파 절단과 나무 간 깊이 |
| `Assets/Editor/SceneWork/DefenseTreeGroveSetup.cs` | 지정된 DefenseTest 나무 에셋·장면의 제한된 조립 명령 |

## 현재 실행 흐름

1. `OnEnable`은 SRP camera callback을 구독하고 cache 재생성을 예약한다.
2. `Update`가 최초 활성화·Inspector 변경·Play/Edit 전환 때 cache를 만든다.
   `OnValidate`에서는 수치 보정과 예약만 한다.
3. 지역 `System.Random(_seed)`가 root-local `Rect` 안에서 후보를 생성한다.
   최소 간격은 배치 시점의 공간 셀로 확인하고 최대 시도 수는 요청 개수의 128배다.
   배치할 공간이 부족하면 실제 개수와 경고를 남긴다. 전역 gameplay 난수는 소비하지 않는다.
4. 수종은 성공한 위치에 순환 배정한다. 같은 수종의 행렬을 최대 256개씩 묶어
   `Graphics.RenderMeshInstanced`로 제출한다. 요청 개수의 상한은 5,000, 기본값은 50이다.
5. Game/SceneView camera callback은 cache만 그린다. root 또는 부모 변환이 바뀌면
   기존 배열의 world matrix와 실제 quad를 포함하는 bounds만 갱신한다. 매 프레임 위치
   생성, Sprite 교체, managed 배열 할당, 재질 복제·수정은 없다.
6. `OnDisable`은 callback을 해제하고 숨겨진 생성 mesh를 해제한다. 다시 활성화하면
   같은 seed로 cache를 새로 만든다. 공유 재질과 atlas는 파괴하지 않는다.

## 이미지와 렌더링 계약

- 입력은 `Tree_Green`, `Tree_Apricot`, `Tree_Gold`, `Tree_Rose`의 1344×144 PNG다.
  각각 112×144 프레임 12개를 가로 배치하며, 20 PPU와 위에서 센 ground pivot (56,130)을 사용한다.
  quad는 x `[-2.8,2.8]`, y `[-0.7,6.5]`다.
- `NPC/InstancedTreeWind` 재질에 `_MainTex`, `_Cutoff=0.5`, GPU instancing을 설정한다.
  PNG의 point filter, 무압축, mipmap 없음과 shader의 pixel-center 샘플링으로 경계 누출을 피한다.
- GPU 시간과 world anchor hash가 나무별 위상을 결정한다. 프레임 시간은
  `200,160,160,200,160,160,160,200,160,160,160,160 ms`, 전체 반복은 2.04초다.
  vertex를 휘는 애니메이션은 하지 않는다.
- URP `Universal2D` unlit pass, queue 3001, alpha clip, `ZWrite On`, `Blend Off`다.
  quad 전체의 Z에 `_depthOffset + worldAnchorY * _depthPerY`를 더한다.
  기본값 -0.1과 0.001은 -Z에서 보는 XY 장면에서 아래쪽 나무가 위쪽 나무를 가리게 한다.
  알파가 없는 pixel은 깊이를 기록하지 않아 수종별 제출 순서와 무관하게 나무끼리 겹친다.

## Unity 배선과 조립

`Tools > NPC Harness > Defense Trees > Prepare Assets`는 네 PNG의 관련 import 설정과
`Assets/Art/Generated/Environment/Trees/Materials/Tree_*.mat`만 만든다/갱신한다.
PPU와 기존 sprite 정의를 보존하며 개별 재질만 저장한다. `SaveAssets`/장면 저장은 하지 않는다.

`Place Trees`는 활성 `DefenseTest`, Edit Mode, Prefab Mode 밖에서만 동작한다.
`/InGameObjects/Map/InstancedTrees`에 component 하나를 조립하고 Undo를 기록한다.
기존 host에 다른 component·child·변환이 있으면 중단한다. 기존 Tree 오브젝트는 변경하지 않는다.
월드 기준 x `[-14,8]`, `[13,36]`, `[42,61]`, 공통 y `[26.8,28]`을 host-local Rect로 변환한다.
50개, seed 20261103, 간격 1, scale 1을 설정한다. 변경된 장면은 저장하지 않는다.

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 확인 |
|---|---|
| 배치·성능·수명 | `InstancedTreeGrove.cs`, 대상 scene의 component와 부모 변환 |
| 프레임·pivot·깊이 | shader, 네 PNG importer와 재질, camera·ground 렌더링 설정 |
| DefenseTest 배치·조립 | setup script, `DefenseTest.unity`의 Map·벽·spawn anchor |

## 불변 규칙과 검증

- scene component는 장식용 instance cache만 소유한다. NPC·navigation에 의존하지 않는다.
- 재질·atlas 참조는 Inspector로 명시하며 누락/잘못된 shader·크기·GPU 미지원은
  cache 생성 때 오류를 한 번 기록하고 렌더링하지 않는다.
- `TreeCount`, `PlacedTreeCount`, `DrawBatchCount`는 요청값과 실제 cache 개수를 읽기만 한다.
- 기존 Unity compile/import와 shader 오류 조회, 직렬화 참조 검사를 사용한다.
  자동 Play Mode나 화면 수집은 기본 검증에 포함하지 않는다. 사람이 Scene/Game에서 확인할 사항은
  50개 배치, 바람 frame, 겹침과 배경 위치다.

## 제약과 TBD

개별 tree와 NPC/SpriteRenderer 사이의 sorting layer·Y 정렬을 통합하지 않는다.
현재 깊이 설정은 NPC 이동 영역 밖의 북쪽 배경과 -Z XY camera에 한정된다.
다른 camera 방향, NPC 사이의 숲, 충돌·벌목 및 대규모 GPU culling은 현재 기능 밖이며 TBD다.
간격은 root-local 좌표이므로 부모 scale을 바꾸면 월드 간격도 바뀐다. material 묶음의 bounds로
culling하며 individual instance별 culling은 하지 않는다.

관련 문서는 [World](README.md), [Navigation](Navigation.md)이다.
배치·수명·atlas 계약·장면 조립·깊이 정책을 바꾸면 이 문서를 함께 갱신한다.
