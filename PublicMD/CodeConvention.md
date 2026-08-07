# Code Convention

## Scope
- This convention applies to scripts in this Unity project.
- When an existing file already has a clear local style, keep edits consistent with that file.

## Naming
- Use PascalCase for class names, enum names, method names, properties, and public fields.
- Use camelCase for private runtime fields.
- Use `_camelCase` for private serialized fields when the field is configured in the Inspector.
- Use clear domain names such as `WorkerNPC`, `NPCComponent`, `NPCStat`, `ActionPool`, `DestinationDB`, and `ActionType`.

## Structure
- Keep one primary class per `.cs` file unless the secondary type is a small serializable data holder used only by that file.
- The current reset prototype uses these script roots:
  - `Assets/Scripts/Actor`: scene actor MonoBehaviours such as `WorkerNPC`.
  - `Assets/Scripts/Manager`: scene-level composition, spawn, or registry managers such as `NPCManager`.
  - `Assets/Scripts/Interface`: project-wide contracts such as `IAction`.
  - `Assets/Scripts/Enum`: project-wide enum identifiers such as `ActionType` and `NPCType`.
  - `Assets/Scripts/System/Action`: concrete plain C# action implementations.
  - `Assets/Scripts/System/Actor`: NPC runtime state, Unity component holders, and selector skeletons.
  - `Assets/Scripts/System/Lib`: reusable support classes such as action pools and destination lookup.
- Do not reintroduce the old `Assets/Scripts/Actors/AI/Friendly/Common` structure unless the project intentionally migrates back to that architecture.
- `Interface/`, `Enum/`, and `Manager/` hold only project-wide types used across two or more domains.
- If the project grows into larger independent features again, prefer `Assets/Scripts/System/<Domain>` or a deliberately renamed plural `Systems` root, but do not mix both conventions casually.
- Do not create a subfolder for a feature until it has enough scripts to justify separation. A single utility class does not need its own subfolder.
- Each script must have a clear primary responsibility.
- Do not split scripts only for the sake of splitting them. If two pieces of code serve the same practical responsibility, keep them together unless separating them removes a real dependency or clarifies ownership.
- Do split code when a responsibility is clearly different. A parser should parse, a manager should manage, a provider should provide, and an action should execute behavior.

## Responsibility Boundaries
- `WorkerNPC` is the Unity-facing NPC runner. It may own update/lifecycle bridging and the currently active runtime objects, but it should not decide role priorities or implement action details.
- `NPCComponent` is the Unity reference holder for NPC-owned scene components such as `Transform`, `Animator`, visual roots, colliders, and renderers.
- `NPCStat` owns mutable NPC stat values and clamps its own invariants. It should not know about selectors, actions, destinations, managers, or scene object lookup.
- `IAction` implementations contain behavior execution logic.
- Selectors decide what the worker should do next and request or build the required action sequence.
- `ActionPool` owns reusable action instance creation and return/reset mechanics. It should not decide behavior priority.
- Destination lookup classes own scene destination data. They should not execute movement or decide why a worker needs a destination.
- A reader should usually understand or modify one concern by reading one matching script: decision logic in a selector, behavior logic in an action, stat logic in `NPCStat`, and destination lookup in a destination database/provider.

## Action Rules
- Every new behavior must be represented by an `IAction` implementation.
- Selectors choose or build an action sequence; actions execute the selected behavior.
- Selectors must be replaceable, so they should depend on interfaces, plain context data, or explicit serialized references instead of concrete runner internals.
- Adding a new worker behavior should normally mean adding a new `IAction` implementation.
- `IAction` implementations should be safe to run through `WorkerNPC` or a dedicated action runner and should not assume a specific selector implementation.
- Actions may call worker capabilities from context, but the action must still own the behavior flow, success/failure rules, and cancellation behavior.
- Actions returned to `ActionPool` must reset reusable runtime state in `Clear()`.
- `Stop()` and `Clear()` must be safe to call even if the action has not fully started or has already been stopped.
- `Tick()` should not allocate avoidable objects and should not perform expensive lookups repeatedly.
- An action should not decide the next high-level behavior.
- Action duration, cost, and reward tuning should eventually live in data assets or data providers, not as hardcoded values inside `WorkerNPC`, selectors, or concrete actions.
- The current `IAction.Tick()` returns `void`; before action queues are implemented, add an explicit action result mechanism such as `ActionState Tick(...)`, `IsComplete`, or a completion result.

## Action Queue Ownership
- A single runner should own the active action queue lifecycle.
- In the current skeleton, that runner may be `WorkerNPC` or a new plain C# `NPCActionRunner`.
- Actions should not register and unregister themselves into the worker tick list unless that ownership model is explicitly chosen and documented.
- Selectors should choose actions or queues, then the runner should call `Start`, repeated `Tick`, `Stop`, and `Clear` in one place.
- Queue or plan data should contain execution parameters, not hidden behavior logic.

## Animation Rules
- `WorkerNPC` and selectors should not contain concrete animation implementation details.
- Unity animation references should be owned by `NPCComponent` or a dedicated animation component.
- Actions may request animation through a narrow context or animation player contract once that contract exists.
- An action that starts animation must stop or release it on success, failure, cancellation, and `Clear()`.
- Do not animate the gameplay root in a way that conflicts with movement. Prefer a child visual root for visual-only animation.

## Data Assets
- A data asset (ScriptableObject or CSV) must have one clear purpose, the same way a script has one
  clear responsibility. Separate data by role, not by convenience.
- A base-stat asset for an entity must contain only that entity's own intrinsic stats. For example,
  an NPC stat asset holds only true NPC stats such as move speed or health.
- Data that is affected by a stat but is not itself that entity's stat must live in a separate
  ScriptableObject or CSV. For example, armor defense, equipment modifiers, or item bonuses belong
  in their own data asset, not inside the worker base-stat asset.
- Keep one source of truth per concept. Do not merge unrelated concepts (base stats, equipment
  tuning, drop tables, action tuning) into a single mixed asset.
- Match the storage format to the data shape: use ScriptableObject for small designer-tuned sets and
  enum-keyed lookups; use CSV when the data is large, tabular, or row-heavy and edited in bulk.
- Place ScriptableObject and CSV asset instances under `Assets/Data/<Domain>` so designer data is easy to
  find outside script folders. Keep the ScriptableObject class definitions under the script domain that owns
  them. For example, future NPC stat tuning assets could live under `Assets/Data/NPC`, while the defining
  C# type stays under the script domain that owns NPC stat data.
- A consumer should read one concept from one data asset. Mixing concepts forces readers to load and
  understand unrelated data just to use one value.
- Data assets should follow a single-purpose shape. For example, if action result tuning is reintroduced,
  it should hold only per-`ActionType` action result data and nothing else.

## Interfaces
- Use interfaces when multiple scripts share the same role and other scripts should depend on that shared role.
- Do not create a one-to-one interface for a single concrete script unless there is an immediate, concrete need.
- Prefer existing role interfaces such as `IAction` when consuming interchangeable behavior. Add selector or pool interfaces only when there is more than one real implementation or a concrete test seam needs it.

## Selectors
- Selectors are allowed to know more than ordinary scripts because they are the worker decision layer.
- Selector dependencies must be explicit through serialized fields, constructor/setup data, context, or clearly named lookup methods.
- Replacing a selector should not require modifying `WorkerNPC`, actions, movement, or provider implementation code.
- Selector logic should decide intent and required action request data. It should not perform low-level movement, stat mutation, animation, UI, or direct behavior execution.
- Selectors may reference action pools, providers, enums, and context data when building an action queue or request.
- Selector dependencies should be visible in fields or setup code. Avoid hidden global lookup from selector logic.

## UI Rules
- Put shared UI scripts under `Assets/Scripts/UI` when they are scene-level UI infrastructure. Put feature-specific UI scripts under the owning feature folder when they are only meaningful for that feature.
- A scene-level `UIManager` may own popup lookup, popup opening, top-popup closing, and the active popup stack for that scene.
- `UIManager` should act as a UI router/window manager. It should not own domain rules such as recruitment affordability, candidate generation, worker spawning, inventory mutation, or selector behavior.
- Every popup managed by `UIManager` must implement `IPopup`.
- `IPopup` should expose `Open()` and `Close()` methods. Use `OpenPopup(...)` and `ClosePopup(...)` naming on manager-level APIs when the caller is addressing a popup by type.
- Identify managed popups with a UI-layer enum such as `UIPopupType`. Enum values should describe popup identities, not domain actions.
- Store active popups in stack/LIFO order so the most recently opened popup closes first.
- Closing a popup through any path must keep the active popup stack synchronized. This includes ESC, a close button, a cancel button, clicking an overlay, or code-driven close requests.
- Prefer routing popup close requests through `UIManager` when the popup is stack-managed. If a popup can close itself, it must notify `UIManager` or call the manager close API so stale active-stack entries are not left behind.
- `CloseTopPopup()` should close only the current top popup. Closing a non-top popup should explicitly remove that popup from the active stack or be disallowed by policy.
- UI scripts may read and call public feature APIs, but they must not directly mutate worker internals, action queues, selectors, context, movement, or stats.
- Recruitment UI is not present in the current reset skeleton. If it is reintroduced, it should depend on read-only candidate surfaces for display data and call recruitment command APIs for actions.

## Enums
- Enum access should be limited to the layers that actually make decisions with those enum values.
- Keep enum usage away from lower-level execution scripts when an interface or action request data can express the dependency more clearly.
- It is acceptable to use enums when avoiding them would make the code harder to read, but the allowed enum-reading layer should remain clear.
- `WorkerNPC` should stay mostly enum-agnostic. Decision-level scripts such as selectors may use enums when building action queues or requests.

## Null Checks
- Prefer Unity-style truthiness for Unity object references: use `if (!component)` instead of `if (component == null)` when the type supports it.
- Use null-conditional calls such as `handler?.Invoke()` when skipping a missing reference is valid behavior.
- Use explicit null checks when the type does not support Unity-style truthiness or when the failure path must be handled clearly.
- Avoid casual `object != null` expressions when a clearer project style is available.
- Unity-style truthiness applies only to `UnityEngine.Object` types such as `MonoBehaviour`, `GameObject`, `Transform`, and assets.
- Plain C# objects, interfaces, and data classes may require `== null`, `!= null`, or `is null` checks because `!object` is not valid for them.

## Unity
- Prefer `[SerializeField] private` fields over public Inspector fields.
- Use `Try...` methods when failure is valid at runtime.
- Avoid LINQ in per-frame or gameplay decision code when a simple loop is enough.
- Avoid repeated `GetComponent`, `FindObjectOfType`, object allocation, and string formatting inside `Update()`, tick methods, selectors, and action `Tick()` methods.
- Resolve required Unity references in `Awake()`, `Start()`, explicit init methods, or serialized fields.
- Use `RequireComponent` when a component cannot operate without another component on the same GameObject.

## Namespaces
- Use namespaces consistently once a domain has enough scripts to benefit from them.
- Namespace names should follow domain ownership, not folder convenience alone.
- Avoid creating narrow namespaces only for one enum or one file.
- Keep namespace spelling stable. Renaming a namespace should be treated as a project-wide refactor.

## Field Style
- Use `_camelCase` for private fields, including private serialized fields.
- Use PascalCase for public properties.
- Use `readonly` when a field is assigned only during construction and never changes afterward.
- Use `const` or `static readonly` for fixed values instead of magic numbers repeated across methods.

## Comments
- Add comments only when the reason is not obvious from the code.
- Keep TODOs specific and actionable.
