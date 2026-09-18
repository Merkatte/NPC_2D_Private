# Legacy Harness Job Tool Contract

This is the `harness-job` v1 adapter only, not the limit of Unity Editor work.
For FarmerTest, GuardTest, production prefabs and sprite import use
[editor-workflow.md](editor-workflow.md). Keep these legacy allowlists unchanged.

This reference records the implemented adapter boundary. Re-read the linked source and schema when they change.

## Sources of Truth

- Tool registry: `Assets/Editor/NpcHarness/Core/HarnessToolRegistry.cs`
- policy allowlists: `Assets/Editor/NpcHarness/Core/HarnessToolPolicy.cs`
- Job schema: `Tools/NpcHarness/Schemas/harness-job.schema.json`
- runner usage: `Tools/NpcHarness/README.md`

## Registered Tools

- `WriteCSharpScript`
- `EnsureScene`
- `EnsureGameObject`
- `SetTransform`
- `EnsureComponent`
- `EnsureMaterial`
- `ConfigureCamera`
- `ConfigureLineRenderer`
- `SaveScene`

The object-assembly role must not use `WriteCSharpScript`; code belongs to `author-unity-code`.

## Implemented Policy

- allowed asset root: `Assets/TestOnly`;
- allowed component type IDs: `Camera`, `HarnessTest`, `LineRenderer`;
- allowed shader: `Sprites/Default`;
- hierarchy paths are absolute and may not contain empty or parent segments;
- different existing managed values require explicit overwrite approval;
- identical state may return `NoChange`;
- prefab editing, production scene paths, `SpriteRenderer`, arbitrary MonoBehaviours, generic serialized-property mutation, and TextureImporter configuration are not currently supported.

`ConfigureLineRenderer` requires exact `useWorldSpace`, `loop`, points, width, cap vertices, corner vertices, color, sorting order, and material values. It also manages `alignment=View` and `textureMode=Stretch`; include those implicit values in overwrite and acceptance reasoning even though schema v1 does not expose them as inputs.

`SaveScene` saves the target scene and calls `AssetDatabase.SaveAssets()`. Use the external closed-Editor runner or an isolated project state so unrelated in-memory dirty assets cannot be persisted.

An orchestrator assignment can narrow these capabilities but cannot broaden them. Broader production use requires a separately reviewed Tool and policy extension before the object candidate is attempted.

## Expected Tool Order

Typical scene assembly order:

```text
EnsureScene
  -> EnsureGameObject parent before child
  -> SetTransform
  -> EnsureComponent
  -> supported Configure* Tool
  -> SaveScene
```

The adapter result is execution evidence only. A root-owned GateResult must independently validate the resulting object structure.
