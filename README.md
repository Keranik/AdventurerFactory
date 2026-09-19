# Adventurer Factory

**Forge heroes, conquer dungeons, build the ultimate adventurer factory.**

Adventurer Factory is a factory-building RPG / logistics-sim prototype: instead of manufacturing goods, you build a factory that produces *adventurers* — placing spawners, drawing path networks, constructing crafting and training structures, routing a living villager workforce, equipping heroes, and sending them into dungeons to level up. Factorio-style automation meets RPG progression (classes, traits, item fusion, research).

> **Status: prototype.** The simulation engine, command/event architecture, save system, and a code-driven UI layer are implemented and covered by an extensive xUnit suite. This is a work-in-progress game, not a finished release. Developed with heavy AI assistance (see `.github/copilot-instructions.md` for the project's engineering rules).

## Architecture

The codebase is split into a Unity-agnostic simulation engine and a thin Unity presentation layer:

| Project | Target | What it is |
|---|---|---|
| `ForgeFlow.Core` | .NET Standard 2.1 | The simulation engine. Zero Unity dependencies. ~30 central manager systems (structures, villagers, path traffic, dungeons, economy, research…) communicate only through a typed `EventBus`; player actions flow as validated, undoable commands through a `CommandBus` with a persistent, replayable command log. All game content (items, recipes, structures, dungeons, classes, villagers, biomes, themes, tutorials, localization) lives in JSON files embedded as resources, loaded through typed registries. |
| `ForgeFlow.Presentation.Unity` | .NET Standard 2.1 | The Unity-facing layer: input (Input System action maps behind a controller stack), a fully code-built UI Toolkit library (~25 reusable `Forge*` components, window manager, observer/data-binding), cameras, audio, and `*Mb` MonoBehaviour wrappers that mirror core entities onto Unity transforms each frame. Compiles outside the editor via stub shims; a post-build step drops the DLL into the Unity project's `Assets/Plugins`. |
| `ForgeFlow.Unity` | Unity 6000.4.1f1 (URP) | The Unity project itself. Contains exactly one script — `Assets/Scripts/FactoryBootstrap.cs` — which instantiates the plain-C# `FactoryEntryPoint` and forwards the MonoBehaviour lifecycle to it. |
| `ForgeFlow.Tests` | .NET 8 (xUnit) | ~1,700 test cases covering the command bus, event bus, simulation ticker, path routing, structure logic, save/migration, localization, and the Roslyn analyzer. Run with `dotnet test`. |
| `ForgeFlow.Analyzers` | .NET Standard 2.0 | A Roslyn analyzer (diagnostic `FF0001`) that enforces the project's localization rule at compile time: all user-facing text must go through `TranslationService` with a key declared in `LocalizationKeys`. |

Key engineering rules (enforced by convention and the analyzer): zero-allocation hot paths (pooled scratch buffers, no LINQ in tick loops), deterministic iteration order, versioned save migrations with orphaned-ID validation on load, and a validated mod-loading pipeline.

## Getting started (clone → running in Unity)

### What you need

| Tool | Why |
|---|---|
| [.NET 8 SDK](https://dotnet.microsoft.com/download) | Builds `ForgeFlow.Core`, `ForgeFlow.Analyzers`, and `ForgeFlow.Tests`. Also builds `ForgeFlow.Presentation.Unity` **when** Unity Editor assemblies are available (see below). |
| [Unity Hub](https://unity.com/download) + **Editor 6000.4.1f1** | Opens `ForgeFlow.Unity`. The Presentation project references UnityEngine / UI Toolkit / Input System DLLs from this install — without it, `dotnet build` of Presentation (and therefore the full test suite) will fail. |

`ForgeFlow.Core` and `ForgeFlow.Analyzers` build with only the .NET 8 SDK. Everything that touches Unity needs the Editor install.

### 1. Point Presentation at your Unity Editor

`ForgeFlow.Presentation.Unity` references managed assemblies under your Editor's `Editor/Data` folder (default assumes a Windows Hub install of 6000.4.1f1).

If Unity lives somewhere else (different drive, version folder, or macOS/Linux), set one of:

```powershell
# PowerShell — path must be the Editor Data folder (…/Editor/Data), not the Hub root
$env:UNITY_EDITOR_DIR = "C:\Program Files\Unity\Hub\Editor\6000.4.1f1\Editor\Data"
```

```bash
# bash / zsh
export UNITY_EDITOR_DIR="/Applications/Unity/Hub/Editor/6000.4.1f1/Editor/Data"
```

Or pass it per-build: `dotnet build -p:UnityEditorDir="…"`.

The Input System / TextMeshPro package DLLs are also resolved from that Editor's package template cache (see the `.csproj` `UnityPackageCacheDir`). After a fresh Editor install, open the Unity project once so Hub finishes installing modules/packages if prompted.

### 2. Build the managed DLLs and run tests

```powershell
# From the repo root — builds Core, Analyzers, Presentation, and copies
# ForgeFlow.Core.dll + ForgeFlow.Presentation.Unity.dll into
# ForgeFlow.Unity/Assets/Plugins/ (post-build target)
dotnet build AdventurerFactory.slnx

dotnet test ForgeFlow.Tests/ForgeFlow.Tests.csproj
```

`Assets/Plugins/*.dll` / `*.pdb` are build outputs. They are gitignored and regenerated on every build — do not commit them.

### 3. Open the Unity project

1. Unity Hub → **Open** → select the `ForgeFlow.Unity` folder (not the repo root).
2. Use Editor **6000.4.1f1** (see `ForgeFlow.Unity/ProjectSettings/ProjectVersion.txt`).
3. On first open, let the Package Manager finish resolving packages. `Packages/manifest.json` already lists `com.unity.render-pipelines.universal` (URP 17.3.x for this Editor). If Unity offers to update/re-pin URP to the Editor-matched version, accept it — that pin is what keeps a fresh clone from opening with a broken render pipeline.
4. Open `Assets/Scenes/FactoryScene.unity` and press Play.

### Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet build` fails resolving `UnityEngine.*` | `UNITY_EDITOR_DIR` / `UnityEditorDir` is wrong or Editor 6000.4.1f1 is not installed. |
| Unity opens with pink materials / missing URP | Confirm `com.unity.render-pipelines.universal` is in `Packages/manifest.json`, then Window → Package Manager → refresh. Prefer letting the Editor re-pin the version over hand-editing. |
| Play mode missing game logic / missing types | Rebuild with `dotnet build AdventurerFactory.slnx` so `Assets/Plugins` gets fresh DLLs, then return to Unity and let it refresh. |
| `AdventurerFactory.slnx` unrecognized by `dotnet` | Use .NET SDK 9+ (supports `.slnx`), or build the individual `.csproj` files in dependency order: Analyzers → Core → Presentation → Tests. |

## Docs

- `.github/copilot-instructions.md` — the project's engineering bible: architecture rules, naming taxonomy, coding standards (also used by AI coding assistants).

## License

MIT — see [LICENSE](LICENSE). Third-party components: TextMesh Pro (Unity, bundled under `Assets/TextMesh Pro`), Unity Input System and other Unity registry packages (see `ForgeFlow.Unity/Packages/manifest.json`). All game art in `Assets/Resources/Entities` is original prototype work.
