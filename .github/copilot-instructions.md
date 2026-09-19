# ForgeFlow Project Bible & Repository Instructions

- **Product Name:** Adventurer Factory
- **Working/Engine Name:** ForgeFlow (namespace, repo, and assembly prefix)
- **Genre:** Factory-Building RPG / Logistics Sim with Progression
- **Version:** 1.5 (April 2026)
- **Tagline:** Forge heroes. Conquer dungeons. Build the ultimate adventurer factory.

## Version History
- **v1.6** — Auto-Registration Pattern (§7.6), `IAutoRegisteredInspector` marker requirement (§5.1), `SaveMigrationRegistry` reflection-driven chain (§7.5), `ServiceContainer` / `IServiceResolver` as the canonical DI root (§3.6).
- **v1.5** — Scalability Mindset Rule, Structure File Organization Rule, Inspector System Architecture (§5.1), updated component list.
- **v1.4** — RPG structs rule, phased tick, ItemManager, canonical managers & registries, tutorial sequence update.
- **v1.3** — Central Manager + Events pattern, Input System Architecture, Multiplayer groundwork notes.
- **v1.1** — Strict Manager / System / Registry / Db naming rules.

---

## 1. Core Concept

You run a factory that produces adventurers instead of goods. Place spawners, draw paths, build structures, equip heroes, send them into dungeons to level up, fuse items, and manage a living workforce that wears out over time.

The game mixes Factorio-style factory building, worker-management logistics, and RPG progression (leveling, traits, profession changing, abilities).

**Gold** is the main limiting resource for construction and expansion. Secondary resources (wood, ore, etc.) feed crafting. The core tension comes from managing a degrading, reusable workforce.

## 2. Core Gameplay Loop

1. Guild creation at new game start (name + banner + logo).
2. New game awards starting gold (scaled by difficulty).
3. Tutorial guides the player step-by-step while verifying that core systems work.
4. Build spawners, structures, and paths (costs gold).
5. Spawn workers (villagers).
6. Route workers along paths to structures using PathGates.
7. Workers work their assigned profession until they wear out (tool breaks, carry full, stamina depleted, etc.).
8. Worn-out workers exit via the output path and must visit re-equip / maintenance stations.
9. Re-equip outcome depends on difficulty.
10. Repeat while expanding the factory, researching tech, and managing gold.

## 3. Architecture & Layer Separation (Strict Rule)

Never break this separation.

- **Core layer (`ForgeFlow.Core`)** — Pure `netstandard2.1`, zero Unity references, headless-capable. Contains all game logic, data, systems, events, entities, registries, and simulation.
- **Presentation layer (`ForgeFlow.Presentation.Unity`)** — Thin layer only. Handles rendering, input, UI Toolkit, particles, audio, camera. All visuals are driven by events from Core.

### 3.1 Custom RPG Structs Rule

Core uses only our self-contained pure structs: `ColorRPG`, `GridPosRPG`, `GridRelRPG`, `Vector2RPG`, `Vector3RPG`, `RectRPG`, `QuaternionRPG`, `GameId`, `TileSpec`, `TileDistanceRPG`, `GridAreaRPG`, `Percent`, `ChanceRPG`, `AngleRPG`, and any future RPG structs.

Never use `UnityEngine.Vector2`, `Vector3`, `Color`, etc. in Core. Conversion to Unity types happens only in the Presentation layer via extension methods (`.ToUnityColor()`, `.ToUnityVector3()`, etc.). This keeps Core 100% portable and headless.

### 3.2 Code Principles

- Zero-alloc hot paths where possible. Prefer arrays and structs over classes in performance-critical code.
- Use `ObjectPool<T>` (`ForgeFlow.Core/Utilities/ObjectPool.cs`) and caching for repeated operations.
- Avoid LINQ in hot paths.
- Descriptive variable and method names. If a name is not self-explanatory, make it more descriptive.
- `private` = `_lowerCamelCase` with leading underscore.
- `public` = `UpperCamelCase`.
- Always use braces `{}` even for single-line `if` / `else` / `for` / `while`.
- All inter-system communication uses events from `GameEvents.cs` via `EventBus`.
- Comprehensive tests for every new or refactored feature.
- All data-driven content (items, recipes, dungeons, themes, tutorials, etc.) is JSON-based and embedded as assembly resources. Mods can override or merge JSON.

### 3.3 Naming Conventions (strict)

- The routes adventurers walk along are called **paths** — never "belt", "conveyor", or "lane" in code, comments, or naming.
- Strict class-suffix taxonomy:
  - `XXXSystem` — active simulation / behavior logic (subscribes to tick events).
  - `XXXManager` — owns state, coordination, and queries. May tick the entities it owns.
  - `XXXRegistry` — pure data lookup / catalog.
  - `XXXDb` — central asset / database-style class (e.g. `SoundDb`, `AssetManifest`).
- All managers and registries implement `IGameSystem` or `IRegistry` as appropriate (see §3.6).

### 3.4 Canonical Core Systems & Managers

See §3.6 for the authoritative list and each class's responsibilities. The master simulation loop is `SimulationTicker`, which publishes `SimulationEarlyTickEvent` → `SimulationTickEvent` → `SimulationLateTickEvent` each fixed step.

### 3.5 Proto + Logic + Mb Pattern

All structures and entities follow a strict **Proto + Logic + Mb** architecture that separates data, behavior, and rendering.

**Proto Layer — data definition (Core, `Proto/` folder):**
- Pure data containers, loaded from JSON.
- `ProtoBase` holds common prototype data (ID, name, display name, description, icon, gold cost, etc.).
- `StructureProtoBase : ProtoBase` adds common structure data (input/output slots, tier, etc.).
- Specific prototypes inherit from `StructureProtoBase`: `VillageSpawnerProto`, `ForestryProto`, `MiningProto`, `InnProto`, `CraftStationProto`, etc.
- Each Proto class references its Logic type (e.g. `LogicType = typeof(VillageSpawnerLogic)`).
- All prototype data lives in JSON files embedded as assembly resources in Core.
- `ProtoRegistry` reads the JSON on startup, instantiates the correct Proto objects, and registers them.
- Mods can override or add to these JSON files — last registration wins.

**Logic Layer — runtime behavior (Core, `Entities/` or `Proto/Logic/`):**
- Contains all actual C# simulation code.
- `EntityBase` / `StructureBase` (via `Structure`) is the abstract base that defines the runtime contract.
- Specific Logic classes inherit from the appropriate base:
  - `VillageSpawnerLogic` handles spawning villagers on a timer.
  - `ForestryMachineLogic` handles tool wear, tool-dependent resource output, exhaustion.
  - `InnLogic` handles rest mechanics and rejecting villagers carrying goods.
- Each Logic instance is created from its Proto at runtime via `ProtoFactory`.
- Logic holds all live runtime state (durability, connected paths, occupants, timers, pending output).
- All game simulation logic lives here — never in Proto or Presentation.

**Central Manager + Events Rule (strict):**
Logic classes **never** hold an `EventBus` reference and **never** publish events themselves. Only central managers (`StructureManager`, `PathNodeManager`, `PathGateManager`, etc.) publish events. The pattern is:
1. Manager calls a simple method on the Logic class.
2. Manager reads pending result data from fields on the Logic (e.g. `LastCraftedItemId`, `PendingHarvestAmount`).
3. Manager publishes the appropriate event and clears the pending data.

**Mb Layer — presentation wrapper (Presentation only):**
- Lives in `ForgeFlow.Presentation.Unity/Visuals/Wrappers/` as one file per structure (`VillageSpawnerMb.cs`, `ForestryMb.cs`, `InnMb.cs`, etc.).
- Thin Unity `MonoBehaviour` wrapper that references the Core Logic instance.
- Handles visuals, animations, particles, audio, and Unity-specific input.
- Subscribes to Core events for visual feedback — never polls Core state directly.

**Data registration flow:**
1. JSON files contain the authoritative data for all prototypes.
2. On startup, `DataLoader` + `ProtoRegistry` read the embedded JSON and register every prototype.
3. `ProtoFactory.CreateLogic(proto)` instantiates the matching Logic class for a given Proto.
4. Mods can ship their own JSON files that override or extend the base data.

This pattern keeps data JSON-driven and moddable, simulation logic pure C# in Core, and rendering/input isolated in Presentation.

### 3.6 Manager & Registry Architecture

All major systems follow a centralized manager / registry pattern, registered through `GameBootstrapper`, communicating via `EventBus`.

**Core Simulation Layer:**
- `SimulationTicker` — master loop; publishes `SimulationEarlyTickEvent` → `SimulationTickEvent` → `SimulationLateTickEvent` each fixed step.
- `PathTrafficSystem` — movement and routing (subscribes to `SimulationTickEvent`).
- `VillagerSystem` — villager lifecycle (subscribes to `SimulationEarlyTickEvent`).
- `WorkerLifecycleSystem` — wear-out and exhaustion (subscribes to `SimulationLateTickEvent`).
- `DungeonResolver` — dungeon encounter resolution (subscribes to `SimulationTickEvent`).

**Centralized Managers:**
- `TileManager` — owns the grid, biome data, tile state, fast spatial queries.
- `EntityManager` — central lookup for all entities (heroes, villagers, structures, path segments, path gates, routing nodes, resource nodes).
- `PathNodeManager` — owns all path segments and routing nodes; add/remove/link/evict/rotate (subscribes to EarlyTick and LateTick).
- `PathGateManager` — owns all PathGates, auto-linking, rotation (subscribes to LateTick).
- `ItemManager` — single source of truth for items, resources, virtual stocks, and physical `ItemInstance` pooling.
- `StructureManager` — owns structure tick lifecycle, spawner dispatch, and publishes lifecycle events for gathering / crafting / forging (subscribes to EarlyTick and LateTick).
- `DungeonManager` — owns villager dungeon-run ticking, result processing, events.
- `ResearchManager` — current tier, tier unlocks.
- `WorldStateManager` — cataclysm, prestige, world portal state.

**Data & Registry Layer:**
- `ProtoRegistry` / `PrototypesDb`
- `ItemRegistry`, `RecipeRegistry`, `ClassRegistry`, `DungeonRegistry`, `AppearanceRegistry`
- `ThemeRegistry`, `VillageRegistry`, `ModBrowserRegistry`
- `SoundDb`, `AssetManifest`

**Presentation Layer Managers:**
- `VisualHeroManager`, `VillageManager`, `ProtoStructureRenderer`, `TerrainVisualManager`, `PathRendererSystem`, `WorkerEffectSystem`, `TileAreaHighlightRenderer`.
- `InputControllerStack` — priority-based input controller chain (see §8.1).

**Utility & Service Layer:**
- `EventBus`, `CommandBus`, `SaveManager`, `TranslationService` / `StringsDb`, `ThemeService`, `ServiceLocator` (fallback only).

---

## 4. UI/UX Vision

**Default "Fun/Whimsical" Mode:** Vibrant, colorful, energetic factory aesthetic. Dark navy/black base with bright neon/cyan, amber, and green accents. Rounded corners (8 px default), subtle glows, hover scale, particles on important events. Fun and approachable for casual players and kids.

**Minimalist / Expert Mode (toggle in Settings):** Sharp corners (2 px), reduced padding/margins, minimal glows/animations. Compact, information-dense.

**Customization:**
- Full HUD Customization Mode (drag, resize, pin any panel).
- Theme Editor for individual color overrides (dev-friendly for now).

**Theming Rule (strict):** Always use `ThemeService.GetColor(key)` for colors. Never hard-code color values in panel or component code.

**Localization Rule (strict):** Always use `TranslationService.Get(key)` for user-facing text. All string keys must be defined in `LocalizationKeys.cs`.

**Fluent API Rule:** Short and clean: `.Text("New Game")`, `.FontSize(18)`, `.BorderWidth(3)`, `.BackgroundColor("primary")`, `.FlexGrow(1)`, `.OnClick(() => { ... })`, etc. Every component ends with `.Build()`.

**No raw style access:** Never do `.style.backgroundColor = ...` in panel/window code. All styling goes through the fluent API or `ThemeService`.

**All UI must use custom `Forge*` components** — never raw `VisualElement`, `Button`, `Label`, etc. in panel code.

- All full-screen overlay panels compose `ForgeFullScreenOverlay`.
- All card-style panels compose `ForgePanel`.

**HUD Customization:** Every `ForgePanel` automatically supports drag, resize, and pin in customization mode.

**UI updates should be event-driven**, not per-frame polling, wherever possible.

---

## 5. Complete Component & Panel List

**Foundational:**
- `ForgeStyledVisualElement`, `ForgePanel`, `ForgeLabel`, `ForgeButton`, `ForgeImage`, `ForgeTooltip`, `ForgeFullScreenOverlay`.

**Basic Interactive:**
- `ForgeTextField`, `ForgeToggle`, `ForgeSlider`, `ForgeDropdown`, `ForgeProgressBar`, `ForgeFoldout`, `ForgeScrollView`, `ForgeListView`, `ForgeTabView` + `ForgeTab`, `ForgeRadioButtonGroup`.

**Game-Specific:**
- `ForgeRecipePicker`, `ForgeProductPicker`, `ForgeItemOverlay`, `ForgeStatusBadge`, `ForgeAutomationGateConfigPanel`.

**HUD & Overlay:**
- `TopBarHUD` (with `GlobalResourcesPanel` + `EntityStatusPanel`), `TutorialOverlayPanel`.

**Menu & Flow:**
- `MainMenuPanel`, `NewGameSetupPanel`, `SettingsPanel`, `PauseMenuPanel`, `GameOverPanel`, `VictoryPanel`, `ModBrowserUI`.

### 5.1 Inspector System Architecture

All entity inspectors follow a strict class hierarchy with a single event coordinator.

**Hierarchy:**
```
BaseInspectorPanel                                   (abstract, IUIWindow)
└── BaseEntityInspector                              (rich UI helpers)
    ├── BaseStructureInspector                       (common header + status + storage/queue)
    │   ├── GatheringInspectorPanel                  (Forestry, Mining, etc.)
    │   ├── SpawnerInspectorPanel                    (VillageSpawner)
    │   ├── InnInspectorPanel
    │   ├── CraftStationInspectorPanel
    │   ├── TrainingInspectorPanel
    │   ├── ForgeInspectorPanel
    │   ├── StockpileInspectorPanel                  (filter UI + ProductPicker)
    │   └── GenericStructureInspectorPanel           (fallback for untyped structures)
    ├── RichVillagerInspectorPanel                   (VillagerLogic entities)
    └── RichWorkerInspectorPanel                     (HeroEntity workers)
```

**InspectorCoordinator (strict single subscriber):**
- Only `InspectorCoordinator` subscribes to `EntitySelectedEvent` and `EntityDeselectedEvent`.
- It resolves the entity type, opens the correct inspector via `UIManager`, and enforces mutual exclusion (only one inspector visible at a time unless pinned).
- No other class subscribes to `EntitySelectedEvent` for inspector purposes.

**Rules:**
- Every new structure type gets its own inspector panel inheriting `BaseStructureInspector`.
- Structure inspectors override `BuildStructureSpecificContent` — never the full `BuildContent`.
- Inspectors that need live refresh implement `ITickableWindow`.
- All inspector files live in `ForgeFlow.Presentation.Unity/UI/Inspectors/`.
- Each inspector has its own `WindowIds` constant.
- **Every concrete inspector MUST implement `IAutoRegisteredInspector`** (a marker extending `IUIWindow`). `AutoRegistrar.RegisterInspectors(IServiceResolver, UIManager)` discovers every marked type at bootstrap via reflection, constructor-injects from the DI container, and registers the result with `UIManager` under `UILayer.Gameplay` with `saveLayout: true`. Never add manual `uiManager.Register(...)` calls for inspectors. A safety-net test (`AutoRegistrarTests`) fails the build if any concrete inspector is missing the marker — closing the Session-8 `DungeonPortalInspectorPanel` bug class (see §7.6).
- Inspectors must declare exactly one public constructor. Constructor parameters are resolved in this order: (1) anything `UIManager` is assignable to (`UIManager`, `ITransientElementTracker`, `IUIFocusProvider`), (2) anything else via `IServiceResolver.Get(Type)`.

---

## 6. Tutorial & Core Gameplay Flow

The tutorial is both an onboarding experience and an in-game verification system that confirms core mechanics are working. Every objective highlights the exact UI element or map tile the player must interact with, celebrates completion, then moves to the next step.

### 6.1 Tutorial Sequence — Phase 1 (16 steps)

1. Place primitive home (spawner).
2. Place Exit gate on the home.
3. Place Forestry structure (highlights all forest biome tiles).
4. Place Entrance gate on the Forestry structure.
5. Draw connecting path from home exit → Forestry entrance.
6. Watch the villager gather sticks until inventory is full.
7. Place a Stockpile and its Entrance gate.
8. Place an Exit gate on the Forestry structure.
9. Connect Forestry exit → Stockpile entrance with a path.
10. Watch the villager drop off sticks.
11. Place an Exit gate on the Stockpile and loop the path back to the Forestry entrance.
12. **Celebration:** first successful production loop.
13. Introduce Tier 1 Resting Spot (Inn).
14. Place the Inn with proper entrance/exit on the loop.
15. Watch the perpetual gather → rest → drop-off loop.
16. **Final celebration:** "Phase 1 complete — you have a working basic loop."

All steps are data-driven in `Tutorials.json` with `HighlightTarget` for hotbar unlocking and `HighlightArea` for tile/biome highlighting.

### 6.2 Entry / Exit System (Factorio-Style Building Connections)

Connections between buildings and paths are never automatic. The player must manually place entry/exit entities — this is the core logistics puzzle.

- Entry/exit is a single rotatable entity. Press R (or the rotate key) during placement to change direction.
- **Facing away from the building = exit** (villagers leave the building onto the path).
- **Facing toward the building = entrance** (villagers turn off the path and enter the building).
- Visually: a signpost pointing toward or away from the building.
- Villager priority: villagers walking along a path prioritize entrances over continuing on the path.
- Puzzle element: if the player routes the output path back to the same building's entrance, villagers loop.

### 6.3 Core Design Principles (Reinforced by Tutorial)

1. **Tool-dependent gathering.** What a gathering spot produces depends on the tool the villager carries. Bare hands → sticks; axe → logs; pickaxe → ore; etc.
2. **Exhaustion / durability.** Tools break and villagers get exhausted. They must leave via the output path and visit maintenance/rest stations.
3. **Manual connections.** Paths have player-defined entry/exit points. No auto-connection.
4. **Drop-off before rest.** Villagers carrying goods cannot enter the Inn — they must drop off first.
5. **Class training unlocks equipment.** Untrained villagers cannot use weapons/armor. They must attend a class trainer first.
6. **Dungeon risk/reward.** Dungeons kill ~90% of entrants. Survivors level up and unlock higher-tier equipment.
7. **Tiered progression.** Each tier unlocks new tools, resources, recipes, and dungeon difficulties.
8. **Logistics puzzle.** Routing, splitting, balancing, gating, and drop-off ordering are the core player challenges.

### 6.4 Structure & Entity Reference

| Entity | Type | Purpose |
|---|---|---|
| VillageSpawner | Structure | Produces villagers on a timer. |
| Forestry | Gathering | Collects wood-tier resources (tool-dependent output). |
| Mining | Gathering | Collects stone-tier resources (tool-dependent output). |
| Herb Patch | Gathering | Collects fiber/cloth-tier resources (tool-dependent output). |
| Resource Node | World Object | Harvestable tile-level resource source consumed by gathering structures. |
| Inn | Service | Restores villager stamina; rejects villagers carrying goods. |
| Craft Station | Production | Generic crafting: input materials → output item. |
| Weaponsmith | Production | Crafts weapons from raw materials. |
| Armorsmith | Production | Crafts armor from raw materials. |
| Class Trainer | Training | Trains villagers into a class (warrior, thief, cleric, rogue, etc.). |
| Dungeon Portal | Structure | Sends equipped villagers into dungeons; high lethality, survivors level up. |
| Stockpile | Storage | Buffer for resources; villagers drop off / pick up items. |
| Balancer | Path | Evenly splits villager flow across multiple paths. |
| Filter Splitter | Path | Routes villagers based on carried item / routing tag. |
| Check Gate | Path | Conditional routing (e.g. "only villagers carrying sticks may pass"). |
| Entry / Exit (PathGate) | Connection | Rotatable signpost connecting buildings to paths (see §6.2). |

---

## 7. Code Best Practices & Performance

### 7.1 Hot-Path Performance

- The Core simulation tick must stay fast enough for hundreds of workers running simultaneously.
- Zero-alloc in hot paths: use `ObjectPool<T>`, pre-allocated arrays, and struct-based data where possible.
- Avoid LINQ, closures, and allocating iterators in `EarlyTick`, `Tick`, `LateTick`, and per-frame `Update` methods.
- Cache frequently accessed values; avoid repeated dictionary lookups in tight loops.

### 7.2 Testing

- Write or update comprehensive tests for every new or refactored feature.
- Tests live in `ForgeFlow.Tests` (.NET 8).
- Test both happy paths and edge cases (insufficient gold, gating limits, empty collections).
- Use `EntityIdFactory.ResetForTesting()` in test setup to get deterministic IDs.

### 7.3 Event-Driven Architecture

- All inter-system communication uses `EventBus` and event structs defined in `GameEvents.cs`.
- Systems subscribe in their constructor and unsubscribe in `Dispose()`.
- Handlers should be defensive. Thrown exceptions are caught by `EventBus.OnHandlerException` so remaining handlers still run — but handlers should not rely on this.
- Presentation subscribes to Core events for visual/audio feedback — never polls Core state directly.
- **Central Manager + Events Pattern (strict):** Only managers publish lifecycle events. Logic classes never hold `EventBus` references or publish events. Managers call simple methods on Logic, read pending data, then publish the appropriate event.
- **Phased Tick:** `SimulationTicker` publishes three events per fixed step: `SimulationEarlyTickEvent` → `SimulationTickEvent` → `SimulationLateTickEvent`. Systems self-subscribe to the phase they need.

### 7.4 Data & Modding

- All game data (items, recipes, structures, tutorials, themes, biomes, villagers, resource nodes) is JSON-based.
- Default data is embedded as assembly resources under `ForgeFlow.Core/Data/DefaultData/`.
- `ProtoRegistry` loads from embedded JSON first, then applies hard-coded defaults for anything missing.
- Mods override or merge JSON via `ModLoader`. Last registration wins for duplicate IDs.

### 7.5 Save System

- Save data is serialized by `SaveManager` using `System.Text.Json`.
- All saveable state lives in Core — never in Presentation.
- Save files are stored in `Application.persistentDataPath/Saves/`.
- **Save migrations are auto-discovered.** Every concrete `ISaveMigration` implementor in the Core assembly is picked up by `SaveMigrationRegistry` at bootstrap via reflection and linked into a single contiguous chain by `FromVersion` → `ToVersion`. Migrations require a public parameterless constructor. Duplicate `FromVersion` values, multiple chain heads, or a disconnected chain throw from the registry constructor. Never maintain a hand-written ordered list of migrations — add the class, implement `ISaveMigration`, and it joins the chain automatically. A safety-net test (`SaveMigrationAutoDiscoveryTests`) fails the build on any broken chain.

### 7.6 Auto-Registration Pattern

When a family of types must all be wired into a central manager at bootstrap (inspectors into `UIManager`, migrations into `SaveMigrationRegistry`, future: command handlers, renderers), use the **marker + reflection + safety-net test** pattern. This closes the recurring "forgot to register the new class" bug category (e.g. the Session-8 `DungeonPortalInspectorPanel` incident).

**Canonical shape:**
1. **Marker interface** — empty, extends the base contract the family shares (e.g. `IAutoRegisteredInspector : IUIWindow`, or a family-specific interface like `ISaveMigration`).
2. **Registrar** — a static method on a dedicated class (Presentation) or on the central manager itself (Core) that:
   - Scans the owning assembly for concrete, non-abstract implementors of the marker.
   - Wraps `Assembly.GetTypes()` in `try/catch` for `ReflectionTypeLoadException`, falling back to `ex.Types.Where(t => t != null).Cast<Type>()`. This tolerates partial loads under the xUnit test host (where `UnityEngine.UIElementsModule` isn't present) and potential mod-hosting scenarios with missing optional deps.
   - Requires each discovered type to declare **exactly one public constructor**.
   - Resolves constructor parameters via `IServiceResolver.Get(Type)` — with an optional UIManager-compatible fallback for Presentation registrars (handles `UIManager`, `ITransientElementTracker`, `IUIFocusProvider`).
   - Exposes the discovery enumerator as a public/internal static method so tests can assert parity.
3. **Safety-net tests** — reflection-only `xUnit` tests that fail the build if (a) a concrete implementor is missing the marker, (b) discovery doesn't enumerate the expected set, (c) any implementor violates the single-ctor contract.
4. **Post-registration wiring** — if a panel needs cross-layer event hooks (e.g. `RichWorkerInspectorPanel.OnSendToMaintenance`), the registrar returns a `WindowId → instance` dictionary so the caller can wire hooks by id. The registrar itself never knows about cross-layer events.

**When NOT to use this pattern (important):** per the Scalability Mindset Rule (§8.3) corollary, do **not** force auto-registration where bespoke per-instance wiring dominates. Concrete examples where the pattern was deliberately rejected in Session 4:
- **Command handlers** are already auto-wired by each owning `Manager`'s constructor (`StructureManager._commandBus.Register<PlaceStructureCommand>(HandlePlaceStructure)`). No duplication to eliminate.
- **Input controllers** take MonoBehaviour fields owned by `FactoryEntryPoint` (attached via `AddComponent`, not in the DI container) plus self-referential closures (`() => _inputStack.ActiveToolMode != null`). A registrar would add a parallel MB-field resolver and a per-controller post-hook map — strictly more code.
- **Menu / HUD panels and `EntityBaseMb` renderers** each need unique `On*` event wiring closing over `_bootstrapper`, `_uiManager`, and bespoke handler methods. No "forgot to register" bug class exists — every site is already explicit by necessity.

Rule of thumb: adopt the pattern when the family has **uniform construction and registration** but suffers from a hand-maintained list. Reject it when bespoke wiring per call site is the norm.

---

## 8. Future Features & Long-Term Vision

### 8.1 Multiplayer & Controllable Player Character

We plan to support multiplayer (co-op or competitive) and a controllable player character (direct control of individual heroes, similar to Factorio-style character control) in the future.

These features are not being implemented right now because they are not critical to the core single-player experience at this stage. We keep the following groundwork in mind:

- The simulation is **designed to be** deterministic (dictionary iteration and ID allocation hardening are tracked in the Action Plan).
- All major state changes go through events (`GameEvents.cs`) via the Central Manager + Events pattern.
- Core remains headless-capable and free of Unity-specific code.
- `InputControllerStack` is designed to be extensible for direct hero control later.

### 8.2 Input System Architecture

`InputControllerStack` is a priority-based input controller chain. Controllers are registered with a priority; the highest-active priority wins input focus.

| Priority | Controller | Purpose |
|---:|---|---|
| 300 | `DebugDevToolController` | Developer-only hotkeys (spawn test entities, toggle overlays, speed controls). |
| 200 | `StructurePlacementController` | Placing structures from the toolbar. |
| 180 | `DemolishController` | Demolishing placed entities. |
| 150 | `PathDrawingController` | Drawing path segments. |
| 50 | `EntitySelectionController` | Selecting / inspecting / rotating placed entities. |
| 10 | `CameraRotateController` | Camera pan / zoom / rotate. |

**Generic Shortcut System:**
- `ShortcutDescriptor` struct — defines a key, label, and localization key for each shortcut.
- `IControllerWithShortcuts` interface — controllers declare their available shortcuts.
- `IControllerWithCustomActions` interface — controllers declare custom contextual actions.
- `ToolModeInfo` struct — encapsulates display name, icon, and shortcuts for a tool mode.
- `ToolModeIndicator` (Presentation) — renders the current tool mode and available shortcuts/actions as a bar at the bottom of the screen.

**Input Consumption Rule (strict):**
- `HandleLeftClickDown` (and other `Handle*` methods) return `bool`. Returning `true` consumes the input — lower-priority controllers never see it.
- When a tool controller is active (`IsActive = true`), it must return `true` from `HandleLeftClickDown` unconditionally — even if the action fails (tile occupied, can't afford). This prevents clicks from leaking to `EntitySelectionController` and opening the inspector during active tool use.
- Only passive / utility controllers (e.g. `CameraRotateController`, `DebugDevToolController`) may return `false` for actions they intentionally do not handle, allowing fall-through.

### 8.3 Scalability Mindset Rule

If you are going to do something ten times, design and implement it as if you will do it a thousand times. Never accept shortcuts, duplication, or "quick hacks" that will become technical debt. Every repeated pattern (structures, UI components, events, etc.) must be clean, consistent, and easily understandable by a random future AI or coder who has never seen the codebase before. If a pattern feels messy or is repeated in multiple places, it is a signal that it needs to be abstracted properly.

### 8.4 Structure File Organization Rule

Each structure type must live in its own dedicated file(s). No "god files" containing multiple unrelated structure logics.

### 8.5 Post-Placement Rotation

- Select a placed entity (path segment, PathGate, routing node), then press R to rotate 90° clockwise.
- `EntitySelectionController` calls the owning manager (`PathNodeManager.RotatePathSegment`, `PathGateManager.RotatePathGate`, `PathNodeManager.RotateRoutingNode`).
- The manager performs the rotation, relinks neighbors, and publishes `EntityRotatedEvent`.
- Presentation Mb wrappers sync visual rotation from the Logic class `Facing` property.

