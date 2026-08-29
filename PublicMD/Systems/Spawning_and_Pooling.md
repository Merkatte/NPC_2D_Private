# Spawning and Pooling

## 기능 목적

NPC role 생성 조립, prefab 종류 catalog, `WorkerNPC` GameObject pool의 수명 경계를 설명한다.

## 세부 기능

| 세부 기능 | 책임 |
|---|---|
| Role composition | `NPCType`에 selector와 stat definition을 결합 |
| Prefab catalog | prefab 형태와 pool 기본값을 data로 식별 |
| Worker pooling | 생성·대여·반환·파괴 callback과 hierarchy 이동 |

## 현재 실행 흐름

```text
Test/UI command
  -> NPCManager.CreateNPC(NPCType)
  -> NPCCreationEntry(selector + stat definition)
  -> stat definition.CreateRuntimeStat()
  -> selector.CanUseStat(stat)
  -> WorkerPool.GetWorker(position)
  -> WorkerNPC.Init(role, stat, selector)
```

`NPCPrefabCatalog`는 prefab 형태별 prefab·capacity 정의를 제공하지만, 현재 `NPCManager`의 role 생성 경로와 `WorkerPool`은 아직 catalog 기반 다중 pool로 통합되지 않았다.

## 주 소유 스크립트

| 경로 | 한 줄 책임 |
|---|---|
| `Assets/Data/ScriptableObject/Script/NPCPrefabCatalog.cs` | prefab 형태별 prefab과 pool capacity 정의 catalog |
| `Assets/Scripts/Enum/NPCPrefabType.cs` | prefab 형태의 안정적인 serialized key |
| `Assets/Scripts/Enum/NPCType.cs` | gameplay role의 안정적인 serialized key |
| `Assets/Scripts/Manager/NPCManager.cs` | role별 selector·stat definition 조립과 spawn 진입점 |
| `Assets/Scripts/System/Lib/WorkerPool.cs` | 단일 `WorkerNPC` prefab의 Unity `ObjectPool` adapter |

## 변경 유형별 최소 확인 범위

| 변경 | 먼저 읽을 파일·에셋 |
|---|---|
| 새 role | `NPCType.cs`, `NPCManager.cs`, role selector와 stat definition |
| 새 prefab 형태 | `NPCPrefabType.cs`, `NPCPrefabCatalog.cs`, catalog asset, prefab |
| pool lifecycle·capacity | `WorkerPool.cs`, `WorkerNPC.cs`, prefab |
| NPC 초기화·reset | [NPC Runtime](NPC_Runtime.md), [NPC Presentation](NPC_Presentation.md) |

## 불변 규칙

- role, selector, stat definition은 한 creation entry에서 함께 배선한다.
- 호환되지 않는 selector/stat 조합은 worker 대여 전에 거부한다.
- pool 반환 시 `OnDisable`을 통해 queue와 actor-local runtime state를 정리한다.
- prefab 형태와 gameplay role을 같은 enum 의미로 합치지 않는다.
- serialized enum 값은 재정렬하지 않고 새 값은 끝에 추가한다.

## Unity 배선과 검증 도구

- `Assets/Prefab/InGame/NPCGirl.prefab`: Farmer·Guard가 공유하는 actor prefab. presentation이 주 소유한다.
- `Assets/Prefab/InGame/Enemy.prefab`: Enemy용 별도 prefab. combat이 주 소유한다.
- `Assets/Data/ScriptableObject/NPCPrefabCatalog.asset`: prefab catalog instance.
- `Assets/TestOnly/TestNPCSpawnWindow.cs`: Farmer·Guard 생성 진입점 검증.
- `Assets/TestOnly/TestEnemyRainSpawner.cs`: Enemy 연속 생성과 melee/ranged definition 교대 검증.

## 알려진 제약과 TBD

- 현재 `WorkerPool`은 단일 prefab만 소유하며 `NPCPrefabCatalog`와 아직 연결되지 않았다.
- Enemy production spawn 경로는 없고 TestOnly spawner가 직접 구성한다.
- active worker 제거와 pool 반환을 관리하는 production despawn API는 아직 없다.

## 관련 문서

- [NPC Runtime](NPC_Runtime.md)
- [NPC Presentation](NPC_Presentation.md)
- [Combat 인덱스](Combat/README.md)

## 문서 갱신 조건

role composition, prefab catalog, worker pool, spawn·despawn lifecycle 또는 prefab 배선이 바뀌면 갱신한다.
