# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## About

ClassicUO is an open-source, cross-platform reimplementation of the Ultima Online Classic Client, built on [FNA](https://fna-xna.github.io/) (an XNA reimplementation). It's the client-side counterpart to the sibling `ModernUO` repo (a server emulator) — many features (a new skill, spell, or effect) touch both, and design notes for cross-cutting work live in both repos' `custom-docs/design/`. This working copy is a personal fork (`Massi17/ClassicUO`) of upstream `ClassicUO/ClassicUO`; per workspace policy, changes here are local-only and never PRed upstream.

Does not distribute any game assets — running it requires a legally obtained UO client data directory (see `Files.SetMulPath`-style configuration; the local machine's client lives at `C:\Users\massi\Documents\Ultima Online`, outside this repo).

## Requirements & Building

- .NET 10 SDK (`net10.0`, no TFM suffix — same framework across all projects, not `net10.0-windows` like the sibling UOFiddler repo, since FNA targets desktop GL/Vulkan/DirectX rather than WinForms).
- **Submodules required**: `external/FNA`, `external/MP3Sharp`, `external/FileEmbed` are git submodules — `git submodule update --init --recursive` before building if they're ever empty (already initialized in this clone).
- Build: `dotnet build` (restores + builds `ClassicUO.sln`). Release/distributable build: `cd scripts && bash build-naot.sh` (Native AOT; needs Git Bash on Windows) → output in `bin/dist`.
- Tests: `dotnet test` runs `tests/ClassicUO.UnitTests` (xunit-style, covering `Assets/`, `Game/`, `IO/`, `Utility/`). CI (`.github/workflows/build-test.yml`) runs `dotnet restore && dotnet build && dotnet test` on **macOS, Ubuntu and Windows** on every push/PR — this is a genuinely cross-platform project, not Windows-only like UOFiddler/ModernUO's Windows-leaning tooling.
- `LangVersion=Preview`, `InvariantGlobalization=true`, `AllowUnsafeBlocks=true` set repo-wide in `src/Directory.Build.props`.

## Architecture

FNA/XNA-style game loop over a from-scratch UO network client and asset pipeline. Project boundaries (all under `src/`):

- **`ClassicUO.Bootstrap`** — native entry point (`Program.cs`), `LibraryLoader.cs` (loads platform-native libs: SDL/FNA3D/etc. per OS), `Plugin.cs`/`CuoInternal.cs` (the native-interop surface legacy plugins like Razor/UOSteam hook into).
- **`ClassicUO.Client`** — the game itself:
  - `GameController : Microsoft.Xna.Framework.Game` — the FNA game loop (`Update(GameTime)`/`Draw`); this is the single place per-frame work is driven from (mouse/plugin tick/scene update/UI update/audio update each frame).
  - `Game/Scenes/` — scene stack (login, character select, in-game world, etc.); `Game/World.cs` holds live game state (mobiles, items, the map) once in a world scene.
  - `Game/Managers/`, `Game/UI/`, `Game/GameObjects/`, `Game/Map/` — gameplay systems, UI widgets/gumps-at-runtime, entity types, and map/tile logic respectively.
  - `Network/` — the actual UO wire protocol: `PacketHandlers.cs`/`OutgoingPackets.cs`/`PacketsTable.cs`, `Encryption/` + `Huffman.cs` (classic UO packet compression/encryption), `NetClient.cs`/`Socket/`. This is the layer that has to match whatever a target shard (ModernUO or otherwise) actually sends.
  - `PluginHost.cs` — the managed side of the legacy plugin API (companion to `Bootstrap/Plugin.cs`).
- **`ClassicUO.Assets`** — reads the UO client's binary data files (art, gumps, animations, maps, hues, tiledata, fonts, sounds) at runtime. This is this repo's analogue to UOFiddler's `Ultima` SDK, but built for the game's read-only runtime access pattern rather than editing; see `custom-docs/design/asset-loading.md` for the "how a shard's custom assets actually get picked up" mechanics before changing anything here. Covered by `tests/ClassicUO.UnitTests/Assets/`.
- **`ClassicUO.Renderer`** — all rendering: `Animations/`, `Arts/`, `Gumps/`, `Effects/`, `Lights/`, `MultiMaps/`, `Texmaps/`, `Sounds/`, `Batching/` (sprite batching), `fonts/`, `shaders/`.
- **`ClassicUO.IO`** — low-level stream/file IO plus `Audio/`.
- **`ClassicUO.Utility`** — cross-cutting helpers: `Logging/`, `Platforms/` (OS-specific bits), `Collections/`, `ZLib`, `StbRectPack`/`StbTextedit` (font atlas packing / text editing, vendored C-library ports).
- **`external/`** — git submodules (`FNA`, `MP3Sharp`) plus vendored/native deps (`FileEmbed` — the compile-time file-embedding analyzer referenced from every project via `Directory.Build.props` — and per-platform native libs under `cuoapi`/`lib64`/`osx`/`vulkan`/`x64`).
- **`tools/`** — `ManifestCreator`, `monokickstart` (packaging/launcher tooling), `ws` — build/release support, not part of the runtime client.

## Design notes (`custom-docs/design/`)

Feasibility/architecture research written *before* touching code for cross-cutting features, mirroring the same practice in the sibling ModernUO repo (its own `custom-docs/design/` should be read alongside these when a change spans both client and server, e.g. a new skill or spell):

- [`asset-loading.md`](custom-docs/design/asset-loading.md) — how the client loads art/gump/string data, including custom per-shard asset overrides
- [`skills-ui.md`](custom-docs/design/skills-ui.md) — skill gump, skill list, where names/positions come from
- [`spells-ui.md`](custom-docs/design/spells-ui.md) — spellbook, spell icons, definitions
- [`effects-ui.md`](custom-docs/design/effects-ui.md) — buff bar, status icons, client-side visual effects
- [`animations.md`](custom-docs/design/animations.md) — custom monster/character animations, Body ID pipeline (client-side counterpart to the animation-import workflow documented in `UOFiddler/CLAUDE.md`)

## Working conventions (`custom-docs/`)

- [`MANUALE.md`](custom-docs/MANUALE.md) — how to work in this fork: new-file-vs-modified-file rule, commit checklist, upstream-sync procedure. Read this once per session, not just this CLAUDE.md.
- [`CUSTOM_CHANGES.md`](custom-docs/CUSTOM_CHANGES.md) — log of every modification to a pre-existing file, so an upstream sync can spot likely conflicts. Add a row whenever a commit touches a file that existed before ours.
- [`LAVORI_IN_CORSO.md`](custom-docs/LAVORI_IN_CORSO.md) — work not yet verified/confirmed done. Check at the start of a session.
