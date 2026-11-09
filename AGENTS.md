# FreshUI

This is a standalone .NET 10 mod for Aether: a pixel-perfect, compact UI (see [README.md](README.md)).
All of it is mixins; the initializer does nothing.

| Path | Purpose |
| --- | --- |
| `FreshUI.slnx` | IDE solution for the mod project. |
| `.gitignore` | Keeps the local client, test saves, build output, and IDE state out of Git. |
| `src/Startup.cs` | Mod initializer (empty) and mod ID. |
| `src/Sharp.cs`, `src/GuiScale.cs`, `src/PixelSlots.cs`, `src/Hud.cs` | Shared state and helpers: the pixel-snapping sampler, the GUI scale setting, nine-sliced slots and icon scales, and the hotbar layout. |
| `src/Mixins/` | C# mixins, one per target class. Register new mixin types in the manifest. |
| `aether.mod.json` | Mod ID, version constraints, entry assembly, initializer, mixins, and display metadata. |
| `icon.png` | Optional mod icon, packaged at the archive root. |
| `build/Aether.Mod.targets` | Shared reference, validation, packaging, and install targets. |

- Keep the mod ID, entry assembly, and type names in sync with the manifest.
- Keep C# sources under `src/` and use exact mixin signatures for the Terraria versions in `depends`.
- Add required mods to `depends` by manifest ID with a version constraint, such as `"shared-library": ">=1.2 <2"`.
  Aether checks these constraints on launch.
- Leave the generated Aether and Terraria version placeholders in place to use exact versions from the local client when packaging.
  Set a literal version or range to target a different version.
- Keep the local Aether checkout path and game binaries out of Git and the mod archive.

`PackMod` copies the manifest to the archive root and preserves files under `resources/`.
Use archive-relative paths in manifest entries such as `"mappings": "resources/mappings.json"`.
From C# use a resource-relative path such as `ModResources.OpenRead(Startup.ModId, "welcome.txt")` and dispose the returned stream.

## Build and test

Run `./aether setup` on Linux or macOS, or `.\aether.cmd setup` on Windows, to prepare the client without opening the game.
The launcher clones Aether into `.aether/client`, runs the checkout's setup if Terraria sources have not been generated, and builds the client and Aether API in the selected configuration.
Launching the mod directly also prepares the client on first use.
You need the checkout's setup prerequisites and the pinned Linux Terraria build.
If setup cannot find that build, see the cloned checkout's `AGENTS.md` for its `download` and setup instructions.

Use `AETHER_CONFIGURATION=Release` to select Release.
The launcher reuses the checkout and builds the client in the selected configuration on every run, which is quick when nothing changed.
After you pull new commits into `.aether/client`, the next run re-applies its patches and rebuilds the client.
Set `AETHER_REPOSITORY` before the first clone to use a different Git remote.
To use an existing built checkout, set `AETHER_ROOT` or put its path in `.aether-root`.
Relative paths are resolved from the mod repo.

- Open `FreshUI.slnx` in your IDE after setup.
- `dotnet build` checks that the mod compiles against the local client and API.
- `dotnet build -t:PackMod` creates the archive in the build output.
- `dotnet build -t:InstallMod` packages the mod and installs it alongside `aether-api.aethermod` into `.aether/saves/Mods`.
  Set `-p:AetherSaveDirectory=/path/to/test-saves` to install into a different profile.
- `./aether -- -mixindebug` builds, installs, and launches with the local test profile on Linux or macOS.
  On Windows use `.\aether.cmd -- -mixindebug` or `.\aether.ps1 -- -mixindebug`.

For manual builds, `-p:AetherClientDirectory=/path/to/client-output` can point directly to the folder containing `Aether.Modding.dll` and `Terraria.dll`.
Set `AetherApiDirectory` to use an API build from another directory.
For installation, that directory must contain both `Aether.Api.dll` and `aether-api.aethermod` from the matching build.

The template references Aether API `0.1.0` by default.
Distribute the mod archive and tell users to install a matching Aether API archive separately.
To opt out, set `AetherUseApi` to `false` in the project and remove `aether-api` from `depends` in the manifest.
See `docs/api/index.md` in the Aether checkout for the API guide.

## How it works

- `PixelArtMixin`: Aether's UI sampler (`Aether.Graphics.PixelArt.Sampler`) is point sampling while Aether's Sharp UI is on; FreshUI swaps in `Sharp.Snapped`, which also has FNA's SpriteBatch put sprites' edges on whole pixels (`SamplerState.PixelSnapEXT`, from Aether's patch to FNA), and records whether Sharp UI is on (`Sharp.Enabled`).
- `MainMixin`, `UIIngameOptionsMixin`: `Main.UIScale`'s setter stores FreshUI's GUI scale (`GuiScale`) whatever it's given, so the title menu and the game share it.
  The GUI scale is Auto (the largest that fits: vanilla's UI scale limit) or 1, 2, 3… screen pixels per art pixel (2×2 texels), so the UI scale is a half-step.
  It's kept in Terraria's `config.json` as `FreshUIGuiScale` (until then, the vanilla UI scale's nearest step).
  The settings window's UI scale slider (the fifth `Slider` call in `BuildGeneral`) is replaced by a button that cycles it: Auto, 1, 2… the largest, Auto.
- `CursorMixin`: the cursor is drawn at the UI's size, its scale read as 1 where it's drawn (vanilla's `cursorScale` pulses between about 0.8 and 1.1).
- `ItemSlotMixin`, `CoinSlotMixin`: `ItemSlot.Draw` draws slot backgrounds (52×52 textures at a slot's position) nine-sliced, and its item icons and coins at a multiple of ½ screen pixel per texel that fits the slot (`PixelSlots.Slot` tells `DrawItem_GetColorAndScale` and `DrawItemCoin` they're in one).
- `UIIconButtonMixin`, `DefenseCounterMixin`, and the rest of `ItemSlot.Draw`'s sprites (empty slot silhouettes, the trash can, overlays): pixel art drawn at fractional scales gets a whole number of screen pixels per art pixel through Aether's `SpriteBatch.PixelScaleEXT` (`Sharp.WholePixels`), set only while they draw: the nine-sliced slot backgrounds must keep their exact sizes.
- `UIHotbarMixin`, `UIHotbarSlotMixin`: Aether's retained hotbar, laid out again after its own layout at the bottom centre, all slots at the inventory's scale; its name is drawn instead of Aether's.
- `UIBuffBarMixin`, `BigProgressBarHelperMixin`: the buff rows move up to the top left corner, and the boss health bar above the hotbar while it's shown.
- Mixin code can't reference Aether's `internal` types, so it reaches them through selectors, `[Shadow]` and return values instead.
