# Defense Presentation

## 목적과 현재 흐름

Defense 전용 actor prefab에 붙는 표현 adapter다. 전투 선택과 피해 시간은 [Battlefield and Response](Battlefield_and_Response.md)가 소유하고 표현은 해당 요청을 자세/도구/말풍선으로 옮긴다.

DefenseCombatPresentation은 Init의 역할 sprite, 공격 시작/종료의 weapon 회전/scale, Downed body 회전을 적용한다. 공격 시작은 weapon을 표시하고 StopAttack은 자세만 복원하므로 공격이 끝나도 이동·경비 중 검/활을 계속 든다. ShowDowned가 weapon을 숨기고 실제 disable/pool reset은 기존 NPCComponent.ResetRuntimeState가 숨긴다. ResetPose는 재초기화 때 기본 자세로 돌린 뒤 SetRole이 새 역할 무기를 표시한다. runtime hit는 animation event에 의존하지 않는다. DefenseFleeSpeech는 Actor.FleeStarted에서 설정 문구를 자동으로 열고 scaled 시간 만료/Downed/disable에 닫는다. hover 입력 없이 보이며 메시지를 selector·action·NPCComponent에 넣지 않는다.

선택적 적 공격은 `PresentEnemyAttack(definition, elapsed)`로 액션이 제공한 시간의 자세만 표시한다. 고블린은 BodyPose를 눌렀다가 windup 끝에 위로 튀고 회복 시간 동안 원래 위치로 돌아간다. 샤먼은 windup 중 빠른 상하 squash/stretch를 반복하면서 `_chargeVisual`을 키우며 release 시점부터 숨긴다. 샐러맨더는 천천히 세로로 늘어 들이마신 뒤 연사 동안 원래 크기로 돌아간다. 이 분기는 LateUpdate의 별도 시계를 돌리지 않아 action pause에서도 자세가 진행하지 않는다. StopAttack은 저장한 BodyPose 위치/scale과 charge scale을 복구하고 charge를 숨긴다. ResetPose/OnDisable/actor Unbind는 회전까지 복구한다.

샤먼·샐러맨더는 선택적 `_windupParticles`로 충전/들숨 동안 모여드는 실제 Unity ParticleSystem을 표시한다. 샤먼은 구체 주위의 청록색 작은 입자, 샐러맨더는 입 앞쪽의 옅은 따뜻한 불씨가 중심으로 들어온다. 액션의 `elapsed` 차이만 `ParticleSystem.Simulate`에 전달하고 호출 뒤 시스템은 paused 상태에 남긴다. `Play`나 독립 Update 시계를 사용하지 않아 timeScale0뿐 아니라 action Pause에서도 입자가 멈춘다. 새 windup/되감긴 sample은 restart하고 release 시 즉시 StopEmittingAndClear한다. Awake·StopAttack·Downed·ResetPose·disable·pool 재사용도 입자와 이전 sample 시간을 정리한다. 전투 시간·발사·피해는 변경하지 않는다.

## 주 소유 스크립트

| 파일 | 책임 |
|---|---|
| `Assets/Scripts/System/Actor/DefenseCombatPresentation.cs` | 역할 무기 sprite, 공격/Downed 자세 및 적 BodyPose/충전 구체·windup 파티클 표현 |
| `Assets/Scripts/Actor/DefenseFleeSpeech.cs` | 자동 도주 메시지의 구독·표시·수명·방향 유지 |

## 변경 유형별 최소 확인 범위

- 공격/Downed: 두 actor prefab, DefenseCombatPresentation, DefenseAttackAction, DefenseActor.
- 말풍선: DefenseFleeSpeech, DefenseCombatSettings의 문구/시간, world canvas/panel/Text 연결.
- 이동·도구·Animator 충돌: [NPC Presentation](../../NPC/Presentation/Adapter.md), Defense controller/clip override와 BodyPose/ToolAnchor 경로.

## Unity 배선과 불변 규칙

Defense prefab은 기존 Animator가 제어하는 Visual 위에 BodyPose를 두고 override clip 경로를 맞춘다. `_body`는 BodyPose, `_weapon`은 ToolAnchor, `_weaponRenderer`는 기존 tool renderer다. 같은 transform 회전을 Animator와 동시에 소유하지 않는다. NPCComponent 역할 도구 표에 Guard sword/Archer bow를 연결한다. 공격 action은 NPCComponent로 방향을 요청하고 무기 표시/공격 자세는 DefenseCombatPresentation에 맡긴다. RequestReplan/Stop/pooled Clear는 무기를 숨기지 않는다.

새 세 적은 기존 DefenseEnemy의 root/Sensor/BodyPose/Visual/Image 구조와 root collider를 보존한다. 몸 그림의 alpha bounds로 보이는 높이를 고블린·샤먼 1.2, 샐러맨더 0.7 world unit에 맞추고 pivot은 보이는 하단으로 잡는다. BodyPose를 해당 높이의 절반만큼 아래로 배치하여 기존 actor 중심을 유지한 채 발 위치에서 늘어난다. PNG 픽셀은 바꾸지 않는다. NPCComponent는 root +X scale일 때 왼쪽, -X일 때 오른쪽이므로 오른쪽을 보는 그림은 Image SpriteRenderer.flipX로 정규화한다. BodyPose만 변형하여 navigation 위치·sensor·collider는 늘어나지 않는다.

`ProjectileOrigin`은 root의 별도 자식으로 native 왼쪽(-X)에 두어 Flip을 따르면서 BodyPose 변형의 영향을 받지 않는다. DefenseActor의 `_projectileOrigin`은 여기를 참조하고, 샤먼의 `_chargeVisual`은 그 아래의 비활성 `ChargeVisual`이다. 에너지 구체와 불덩이는 각각 보이는 높이 0.28/0.22 world unit의 단일 sprite이며, 충전 구체의 최대 scale은 1.4다. 발사체는 이동 방향으로 회전하는 기존 DefenseProjectile을 재사용한다.

두 적의 `ProjectileOrigin/WindupParticles`에는 ParticleSystem과 renderer를 연결한다. Local simulation/Hierarchy scale로 actor facing을 따르며 BodyPose 변형과는 분리된다. Circle 외곽과 음수 radial velocity를 사용하고, 샐러맨더는 native 왼쪽에 한정된 120도 부채꼴에서 입으로 모인다. 최대 입자 수는 샤먼40/샐러맨더32, 초당 방출은45/32, 수명은 각각0.34~0.43초/0.36~0.4초다. 입자는 커졌다 작아지며 alpha로 사라진다. playOnAwake=false, 고정 seed, 충돌·피해 없이 표시만 수행한다.

파티클의 공용 편집 원본은 `Assets/Prefab/Defense/Enemies/Effects/ShamanGather.prefab`와 `SalamanderInhale.prefab`다. 두 actor의 `ProjectileOrigin/WindupParticles`는 해당 effect의 실제 중첩 prefab instance이며 `_windupParticles`는 그 내부 ParticleSystem을 직접 참조한다. actor에 module/renderer 설정을 복사한 override를 두지 않아 방출량·색·수명·크기·material 변경은 effect prefab에서 관리한다. actor별 이름/배치만 instance 경계에 남는다. 액션 시간 기반 재생·정리는 기존 DefenseCombatPresentation을 그대로 사용한다.

`Assets/Prefab/Defense/Enemies/Effects/ShamanGather.mat`는 기존 EnergyBall texture를, `SalamanderInhale.mat`는 기존 FireProjectile texture를 재사용한다. URP `Particles/Unlit`의 particle vertex color와 alpha blending·ZWrite0·Transparent queue를 사용한다. 프로젝트 Renderer2D가 지원하는 `SRPDefaultUnlit` pass로 그리며 sprite 전용 shader의 SpriteRenderer uniform에는 의존하지 않는다. 이미지·importer·공용 material은 수정하지 않는다.

실제 조립 helper는 Editor 전용 `Assets/Editor/SceneWork/EnemyArchetypeSetup.cs`의 `Tools/NPC/Defense/Create Enemy Archetype Prefabs` 메뉴 또는 `Create`다. 다섯 PNG 경로는 `Assets/Art/Generated/Defense/Enemies/` 안으로 제한하고 각 이미지의 오른쪽 방향 여부를 인자로 지정할 수 있다. 정확한 importer·새 stat/attack asset·새 prefab만 저장하며 기존 출력이 있으면 덮어쓰지 않는다. scene/wave 저장과 전체 SaveAssets는 수행하지 않는다. asset 실제 적용/검사 결과는 작업 기록과 PROGRESS를 따른다.

초기 로컬 파티클 추가 메뉴는 `Tools/NPC/Defense/Add Enemy Windup Particles`(`AddWindupParticles`)다. 현재 Prefab Stage가 대상 prefab을 열었거나 대상 asset에 미저장 변경이 있으면 중단한다. 기존 동명 child가 해당 presentation에 연결된 로컬 ParticleSystem이 아니면 보존하고 중단한다. effect prefab이 하나라도 존재하면 material이나 module을 수정하기 전에 중단하여 관리 중인 설정을 초기 기본값으로 덮어쓰지 않는다.

`Tools/NPC/Defense/Extract Enemy Particle Prefabs`(`ExtractWindupParticlePrefabs`)는 현재 로컬 파티클을 독립 effect prefab으로 추출하는 조립 명령이다. 새 출력이 이미 존재하거나 대상 Prefab Stage/미저장 source 변경/참조 불일치가 있으면 시작하지 않는다. 실제 ParticleSystem과 renderer의 현재 값을 그대로 저장하고 Configure 기본값을 재적용하지 않는다. isolated actor contents에서 로컬 child를 연결된 nested instance로 교체하고 기존 presentation 참조를 갱신한다. 저장 범위는 새 effect 두 개와 기존 actor 두 개뿐이며 material·고블린·projectile prefab·stat/profile·scene/wave는 건드리지 않는다. 적용 후 표시 결과는 사람 확인 항목이다.

말풍선은 기존 흰색 9-slice/폰트를 사용하는 전용 world canvas panel이다. `_panel`은 비활성 표시 child이며 스크립트는 활성 actor에 둔다. 표시 중 world rotation을 고정하고 부모의 음수 X scale 부호를 상쇄해 facing을 바꾸어도 글자가 뒤집히지 않게 한다. 실제 가독성은 사람 확인 항목이다. UI는 gameplay state를 변경하지 않는다.

## 검증·제약·갱신

표현은 runtime 완료 조건을 결정하지 않는다. 컴파일과 serialized 연결 검사만으로 공격 자세·사거리·한글·화살·말풍선을 시각 통과로 보고하지 않는다. 이 항목은 사람 확인이며 자동 Play/스크린샷 검증은 제외한다. [NPC Messages](../../NPC/Presentation/Messages.md)의 hover 생각 표시는 기존대로 유지한다. 표현 API·transform ownership·말풍선 수명/배선이 변경되면 갱신한다.
