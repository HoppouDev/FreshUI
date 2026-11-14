# FreshUI

This is a standalone .NET 10 mod for Aether: a pixel-perfect, compact UI (see [README.md](README.md)).
Most of it is mixins and the resources they load; the initializer sets up its Gum screens (`src/Gum/`).

| Path | Purpose |
| --- | --- |
| `FreshUI.slnx` | IDE solution for the mod project. |
| `.vscode/`, `.mcp.json` | VS Code build, run and debug tasks (see Build and test), and the game's MCP server for agents. |
| `.gitignore` | Keeps the local client, test saves, build output, and IDE state out of Git. |
| `src/Startup.cs` | Mod initializer (Gum screens and their keys) and mod ID. |
| `src/HotReload.cs` | Rebuilds FreshUI's screens after a .NET hot reload (`./aether watch`). |
| `src/GuiScale.cs` | The GUI scale setting, shared by the legacy UI and Gum. |
| `src/Legacy/` | The legacy UI: the game's own UI, patched by mixins, and the helpers below (namespace `FreshUI.Legacy`). |
| `src/Legacy/Sharp.cs`, `src/Legacy/PixelSlots.cs`, `src/Legacy/Hud.cs` | Shared state and helpers: the pixel-snapping sampler, nine-sliced slots and icon scales, and the hotbar layout. |
| `src/Legacy/PauseMenu.cs`, `src/Legacy/KeyDefaults.cs` | The in-game pause menu (its state, actions, hard-coded Esc and Start, and screen), and FreshUI's keys (inventory on Q, grapple on E; the gamepad's full map off Start), set once. |
| `src/Legacy/GamepadMenu.cs` | The pause menu and settings window on the game's gamepad navigation: their own link page, items linked by position, slider steps, tab switching and hints. |
| `src/Legacy/SettingsWindow.cs`, `src/Legacy/SettingsTabs.cs`, `src/Legacy/SettingsScreens.cs`, `src/Legacy/AetherGraphics.cs` | The tabbed settings window: its layout and elements, what's on each tab, its title-menu and in-game screens, and Aether's internal graphics settings through an accessor. |
| `src/Legacy/PixelImages.cs` | Image buttons' art at even pixels: 2×2-texel art as drawn, single-texel art at whole screen pixels per texel (the list items' buttons as icons at the GUI scale's pixels per texel, laid out again). |
| `src/MomentumScroll.cs` | Scrolling momentum: wheel turns as velocity, glides stepped each frame (the game's scrollbars and `FreshScrollViewer`). |
| `src/Legacy/SdfText.cs` | Text from a signed distance field: loads the shader and atlas, and replaces each font's Andy Bold glyphs with them. |
| `src/Legacy/Mixins/` | C# mixins (namespace `FreshUI.Legacy.Mixins`), one per target class. Register new mixin types in the manifest. |
| `shaders/SdfText.fx`, `resources/shaders/SdfText.fxb` | The SDF text shader's source, and the D3D9 effect FNA loads, compiled from it with the Windows SDK's `fxc` (see the source's header). |
| `src/Gum/` | Gum in game: the host layer and screen registry (`GumHost`, `GumScreens`), its distance-field text (`GumText`), pixel grid (`UiPixels`), textures (`GumTextures`) and the automation tree (`GumUiTree`). |
| `src/Components/`, `src/Screens/` | Gum components (`FreshButton`, `FreshIconTextButton`, `FreshLabel`, `FreshItemSlot`, `FreshIconButton`, `FreshTooltip`, `FreshToggleSwitch`, `FreshSlider`, `FreshScrollViewer`) and screens (`HudScreen`, `GalleryScreen`). |
| `shaders/GumText.fx`, `resources/gum/`, `tools/GenerateGumFont.cs` | Gum text's shader source, its compiled effect and font atlas (`TextSdf.*`), and the atlas's generator (`dotnet run --file tools/GenerateGumFont.cs -- --font ANDYB.ttf`). |
| `tools/sdf_font.py`, `resources/fonts/andy-sdf.*` | The SDF atlas generator (`uv run tools/sdf_font.py ANDYB.ttf resources/fonts`) and its output: the field and each glyph's rectangle and corner. |
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
- `./aether watch` (Linux, macOS; needs the ASP.NET Core runtime, which `dotnet watch` runs on) launches the game under `dotnet watch` with .NET hot reload: saved edits to FreshUI's own code apply to the running game, and `HotReload` rebuilds every Gum screen, the pause menu and the settings window, for prototyping UI without restarting.
  The project's `RunCommand` is the launcher, which keeps the agent's startup hook (`DOTNET_STARTUP_HOOKS`) out of its builds and passes it to the game; edits to mixins (merged into the game at startup) and rude edits restart it.
- `./aether -- -mixindebug` builds, installs, and launches with the local test profile on Linux or macOS.
  On Windows use `.\aether.cmd -- -mixindebug` or `.\aether.ps1 -- -mixindebug`.
- `AETHER_LAUNCH_OPTIONS` passes extra `aether launch` options through the launcher (Linux, macOS), such as `--profile` for Aether's allocation profiler (Linux; record with the MCP tool `game_profile`).
- In VS Code, the launch configurations (`.vscode/`) start the game from a task (Debug, Debug with hot reload under `./aether watch`, Widget gallery under `./aether gallery`, Debug with profiler), each serving the aether-game MCP server on port 47321 (`-aethermcp`, `.mcp.json`), and attach the debugger: pick `Aether.Bootstrap` from the list, since netcoredbg attaches only by process ID. Stopping the session ends the game.
  `PackMod` packages `FreshUI.pdb`, which Aether loads with the assembly, so breakpoints bind in FreshUI's code (mixins are merged into the game, so likely not in them).

For manual builds, `-p:AetherClientDirectory=/path/to/client-output` can point directly to the folder containing `Aether.Modding.dll` and `Terraria.dll`.
Set `AetherApiDirectory` to use an API build from another directory.
For installation, that directory must contain both `Aether.Api.dll` and `aether-api.aethermod` from the matching build.

The template references Aether API `0.1.0` by default.
Distribute the mod archive and tell users to install a matching Aether API archive separately.
To opt out, set `AetherUseApi` to `false` in the project and remove `aether-api` from `depends` in the manifest.
See `docs/api/index.md` in the Aether checkout for the API guide.

## Gum screens

FreshUI hosts [Gum](https://docs.flatredball.com/gum) screens in game, alongside the game's own UI (`src/Gum/`).

- **Adding a screen:** build its root element (a `ContainerRuntime` with Gum controls and FreshUI's components) in a class under `src/Screens/`, and register it from `Startup.Initialize` with `GumScreens.Register("fresh-ui:name", () => new MyScreen(), initiallyVisible)`; the returned `GumScreen`'s `IsVisible` shows and hides it (for example, from a key with the Aether API's `Keybinds.Register`).
  Screens are built once, on the first frame in a world, and kept between worlds. `fresh-ui:hud` is the live `HudScreen`, registered except in the widget gallery; Aether's inventory state determines which slots it presents, with Q and Esc handled by the game and `PauseMenu`.
- **Drawing:** `GumHost` registers one retained mod UI layer (`fresh-ui:gum`, Aether's `RetainedUi`), so Gum draws over the HUD and under mouse text and the cursor, and is suspended where the game's mod UI is (menus, settings, the full-screen map, hidden HUD).
  Its surface updates Gum every update and draws it every frame, ending and restarting the game's sprite batch around it.
  It takes the mouse only over an interactive Gum element, so elsewhere the game gets the click.
- **Pixels and art:** layout is in UI pixels, one pixel of the game's UI art (2×2 texels), and Gum's camera draws each as `GuiScale.Current` screen pixels (`UiPixels`), so the art stays even and follows the GUI scale setting.
  Components name the game's textures by asset name (`Images/Inventory_Back`, `Images/Item_22`), loaded through `GumTextures`.
- **Components:** FreshUI's restyle Gum's Forms controls with the game's art, keeping their behaviour (events, focus, automation): `FreshButton` (button backing), `FreshIconTextButton` (a `FreshButton` with an icon before its text, the pair centred; the icon tinted as the text), `FreshToggleSwitch` (a `ToggleButton`: the scrollbar pill as its track, blue off and teal on, and the settings toggle's filled circle as a knob that slides, a pixel inside the track), `FreshSlider` (a `Slider` in the switch's art: the pill track, filled in teal up to the round knob, gold while hovered, dragged or focused, which it tracks itself as Gum's Slider doesn't update its state for them; snapping with `IsSnapToTickEnabled`), `FreshScrollViewer` (a `ScrollViewer`: the game's scrollbar track and handle, no arrow buttons; the wheel glides with `MomentumScroll` unless `SmoothScrolling` is off, `WheelStep` UI pixels a notch).
  With the gamepad (`GumHost.GamepadEnabled`, on in the widget gallery), Gum's own focus navigation moves between them and A activates; focused, a button's text and a switch's or slider's knob are gold (the game's selected-item colour), `FreshIconButton` (an `IInputReceiver` of its own) shows its outline, a scroll area's handle is gold and the right stick scrolls it, and a slider steps by 1% on the D-pad and 10% on the bumpers (by whole ticks when it snaps).
  `GumHost.Animate` steps an animation (the knob's slide, a scroll's glide) every frame before Gum draws, by the time since the last frame, while it runs.
- **Widget gallery:** `./aether gallery` (Linux, macOS) runs `./aether watch` with `-freshuigallery` and the MCP server on port 47321: the client opens on `GalleryScreen` (`fresh-ui:gallery`, every component in a grid of panels, modelled on Gum's all-controls showcase) in place of the title menu, for prototyping components with hot reload and driving them over MCP (`game_ui_layout`'s `gum` tree, `game_ui_action`, `game_screenshot`).
  `GumGallery` and `GalleryMixin` run Gum on the title screen then (updated after `Main.Update`, drawn in `DrawMenu` before the cursor, over an empty menu state), and the toolbar's UI scale slider sets `GumHost.ScaleOverride`, whole screen pixels per UI pixel, for the gallery only.
  On the gamepad its controls are one spatial-navigation zone (a Forms `Panel`): using the gamepad focuses the first control, the mouse clears the focus, and the game's instruction bar shows the focused control's hints from a link page of the gallery's own, whose one point (the game's cursor) follows the focus.
  Add a component to the gallery as a `Cell` in `GalleryScreen`; its rows centre a control and its label (`Row`).
  The components' art is the game's 2×2-texel art at half a UI pixel per texel, and moving parts (the slider's knob) snap to whole screen pixels, so they stay even at every UI scale.
  The switch's and slider's tracks are the scrollbar pill made 9 pixels tall by repeating an interior row (`GumTextures.Taller`; the art is 8, and the 7-pixel knob sits a pixel in from each edge), drawn as three sprites (`FreshPill`: ends and stretched middle, each the art's full height), not a nine-slice: stretching the art's 2-pixel middle to 3 doubles a row unevenly, and Gum's nine-slice places its bottom slices a screen pixel off for this art. Place pixel art at whole UI pixels (the gallery's rows have the parity of their art).
  Hot reload can report success without applying an edit (one adding a method that changed code calls), and has crashed once on an edit to code that runs every update (a garbled `TypeLoadException`); when the gallery doesn't show an edit, quit it and save a file to restart it.
- **Text** is drawn from a signed distance field (`GumText`: Gum's font loading goes to one atlas, in FreshUI's resources), with procedural weights, italics, outlines, gradients and drop shadows.
- **Remote automation:** `GumUiTree` lists the Gum elements in Aether's `game_ui_layout` as their own tree (`gum`, owner `fresh-ui`), separate from the game's retained UI, with IDs like `gum:12`, each component's screen, values (slots' items and counts, toggles' state, sliders' position), whether it has the focus, and actions (`activate`, `secondary`, `set_value`, `set_text`); `game_ui_action` and component screenshots (`game_screenshot` with `component_id`) work on them.
  Gum skips input while the window is inactive, so under Aether's synthetic input `GumHost` counts the window as focused for Gum's update.

## How it works

- `PixelArtMixin`: FreshUI's sharp UI, always on: Aether's UI sampler hook (`Aether.Graphics.PixelArt.Sampler`, the batch's own sampler, linear as in vanilla) answers `Sharp.Snapped` instead, point sampling that also has FNA's SpriteBatch put sprites' edges on whole pixels (`SamplerState.PixelSnapEXT`, from Aether's patch to FNA). Fonts keep linear filtering (Aether sets their textures' `SamplerEXT`).
- `MenuScreensMixin`, `UIIngameOptionsMixin`, `IngameOptionsMixin` (with `SettingsWindow`, `SettingsTabs`, `SettingsScreens`): one tabbed settings window for the title menu and the game, in place of Aether's in-game window and title-menu settings pages.
  `MenuScreens.GetScreen` answers `MenuSettingsScreen` for Settings (mode 11) and every settings page's mode (112, 1112, 1111, 111, 28, 2008, 44110, 26, 1125, 25, 252, 1127), opening the tab that page's settings moved to, so code that opens a page by mode (Keybindings' Back) lands on the window; Language (1213) stays Aether's screen.
  In game, `UIIngameOptions.DrawWindow` draws `GameSettingsScreen` instead of Aether's window (with the rest of what it draws), and `IngameOptions._GUIHover` is the window's area.
  It's styled as Aether's Mods window (`UIModList`): a `UITextPanel` title over two `UIPanel`s, the tabs (General, Display, Graphics, Effects, Interface, Volume, Cursor, Controls) in the left one with the window's actions under them as large `UITextPanel`s (Back; on the title menu, Language and Back), and the open tab's rows (panels lit with a gold border when hovered, under muted group headers) in a scrolling `UIList` in the right one; the open tab is kept between openings.
  Group headers and the Display and Graphics tab names are FreshUI's own English text; the rest is the game's and Aether's.
  Row text is centred on Andy Bold's capitals (`SettingsWindow.DrawText`), not on the font's line height.
  The game's UIs use up the wheel's turn for UIs before the window is drawn, so it scrolls its list itself, once per turn of the wheel (`ScrollOptions`).
  Each setting appears once, with vanilla's texts and behaviour (Map Enabled only on the title menu; resolution and fullscreen applied after the frame, as in vanilla's in-game window); Aether's graphics settings are `internal`, so `IGraphicsSettingsAccess` and `AetherGraphics` reach them through accessors.
  In game, the gamepad drives it through `GamepadMenu`, a link page of FreshUI's own (vanilla's settings window pages, 1001 and 1002, treat A on their first points as choosing a category): each frame the window lists its tabs and actions, then the rows wholly in view, and each item links to its nearest neighbour in each direction by where it is on screen (right from the tabs to the first row, left from a row to the open tab).
  The game's navigator moves the cursor onto the focused item, so A clicks it as the mouse does; a slider's row takes left and right, the D-pad stepping it by 1% (with the navigator's repeat while held) and the left stick sliding it (vanilla's `HandleOptionsSpecials`); LT/RT go to the previous and next tab, moving past the first or last row in view scrolls the list a row, and B is the inventory key (back to the pause menu, then closed).
  The game's instruction bar shows the page's hints for the focused item (select or adjust, back or close, switch menu). A menu opened again starts at its first item (the settings at the open tab); back from the settings, the pause menu's focus is where it was.
- `MainMixin`, `PlayerMixin`, `IngameOptionsMixin`, `IngameFancyUIMixin`, `UIModListMixin`, `PlayerInputMixin` (with `PauseMenu`, `KeyDefaults`): a Minecraft-style pause menu in game.
  It is vanilla's settings window as far as the game knows (`Main.ingameOptionsWindow`, so Autopause pauses it), drawn as `PauseScreen` or, after its Settings, `GameSettingsScreen`, both over the game dimmed: Back to Game, Achievements | Mods (Aether's internal `UIModList`, made by reflection and opened as a fancy UI), Settings | Invite Friends (where invites work), Save & Exit.
  Esc, and the gamepad's Start, are hard-coded, not key bindings: at the end of `Main.DoUpdate_HandleInput` (after the keyboard is read, before chat, which closes on Esc itself), unless typing, rebinding or on the title menu, a press calls `Player.ToggleInv`, which closes whatever is open as the inventory key does, and whose `OpenInventory` call (nothing open) opens the pause menu instead while Esc is being handled (`PauseMenu.HandleEscape`).
  `ToggleInv`'s `IngameOptions.Close` call goes back from settings opened from the pause menu before it closes the window (also while dead, `TryOpeningInGameOptionsBasedOnInput`, where the inventory key opens the pause menu).
  `MainMixin` drops the Settings button at the bottom right (`DrawInterface_29_SettingsButton`'s `DrawSettingButton` call): Settings is the pause menu's.
  `PauseScreen` places its panel after recalculating its own size (a GUI scale change in the settings otherwise leaves it laid out for the old size).
  Closing the window leaves the inventory as it was when it opened (vanilla always reopens it); achievements, mods and key bindings opened from the window reopen it at the same view when they close (`IngameFancyUI.Close`), and the Mods window's Back closes it in game rather than going to the title menu.
  The pause menu's buttons are `GamepadMenu` items too (Back to Game focused first, Achievements and Mods side by side).
  `KeyDefaults` puts Inventory on Q (taking Esc, and the E an earlier version added, off it) and Grapple on E (taking that version's Q off it) in every input profile's keyboard modes, and takes the full map off Start in the gamepad modes (in play onto Back, which the map style gives up, where Back isn't another action's), once per version (`FreshUIKeyVersion` in the config, kept across `Main.SaveSettings` with the GUI scale), adding each key only where no other action has it.
- `UIModListLayoutMixin`: Aether's Mods window laid out as the settings window: no search icon (`UIWrappedSearchBar.HideSearchButton`; the box, which still starts a search when clicked, takes its "Search" gamepad snap point), and Open Mods Folder and Back stacked at the bottom of the left panel under the mod count, the panels reaching down to 44 units above the screen's bottom.
- `MainMixin`: `Main.UIScale`'s setter stores FreshUI's GUI scale (`GuiScale`) whatever it's given, so the title menu and the game share it.
  The GUI scale is Auto (one below the largest that fits, vanilla's UI scale limit; the largest crowds the screen) or 1, 2, 3… screen pixels per art pixel (2×2 texels), so the UI scale is a half-step.
  It's kept in Terraria's `config.json` as `FreshUIGuiScale` (until then, the vanilla UI scale's nearest step); `MainMixin` puts it back after `Main.SaveSettings` clears the config to write the game's own settings.
  The settings window's Display tab has a button that cycles it (in place of vanilla's UI scale slider): Auto, 1, 2… the largest, Auto.
- `CursorMixin`: the cursor is drawn at the UI's size, its scale read as 1 where it's drawn (vanilla's `cursorScale` pulses between about 0.8 and 1.1).
- `InventoryLabelMixin`: the open inventory's "Inventory", "Coins" and "Ammo" labels aren't drawn (the first three calls of that `DrawString` overload in `DrawInventory`): the coin and ammo row is told apart by its colours.
- `ItemSlotMixin`, `CoinSlotMixin`: `ItemSlot.Draw` draws slot backgrounds (52×52 textures at a slot's position) nine-sliced (plain backgrounds swapped: coin slots' for `Inventory_Back6`, olive, ammo slots' for `Inventory_Back8`, teal, the inventory's selected hotbar slot's for `Inventory_Back10`, and the hotbar's for `Inventory_Back9` and, for its selected slot, `Inventory_Back10`, as the inventory's hotbar row; the context and slot come from injects at its head and return), and its item icons and coins at a multiple of ½ screen pixel per texel that fits the slot (`PixelSlots.Slot` tells `DrawItem_GetColorAndScale` and `DrawItemCoin` they're in one).
- `UIIconButtonMixin`, `DefenseCounterMixin`, and the rest of `ItemSlot.Draw`'s sprites (empty slot silhouettes, the trash can, overlays): pixel art drawn at fractional scales gets a whole number of screen pixels per art pixel through Aether's `SpriteBatch.PixelScaleEXT` (`Sharp.WholePixels`), set only while they draw: the nine-sliced slot backgrounds must keep their exact sizes.
- `UIInventoryGridMixin`, and slot sizes in `PixelSlots.DrawSlot` and the hotbar: slots on the art's pixel grid (2 UI units a pixel): whole-pixel sizes (0.85 slots are 44 units, not 44.2), and the inventory's pitch is 46 units (vanilla's 47.6, truncated, alternates 47 and 48), so its gaps are exactly one art pixel.
- `UICoinAmmoSlotsMixin`, `UITrashSlotMixin`, `MainMixin`, `UIEquipmentSlotsMixin`: the coin and ammo slots as a sixth row under the inventory, at its size and pitch (coins under its first four columns, ammo under the next four), the trash slot at that row's end (under the last column; with a chest open, a row lower, as is the chest itself: `MainMixin` raises `Main.invBottom` from 258 by one row, `ChestDrop`) and the equipment page (columns and rows 46 apart, with no extra gap before the accessories; slots, accessory toggles and the defense counter) on the art's pixel grid too, with one art pixel between slots.
- `UIHousingIconMixin`, `UIHousingPageMixin`: the housing page's NPC slots draw their backgrounds nine-sliced and heads at whole pixels (`PixelSlots.DrawSprite`, as `ItemSlot.Draw`'s sprites; it recentres sprites anchored at their top left, like the trash can), 46 apart down a column and between columns.
- `UIIconButtonScreenMixin` (with `SideButtons`), `AchievementAdvisorMixin`: the quick stack, sort, bestiary and emote buttons in a grid of two 32-unit columns 34 apart where vanilla's coin and ammo columns were, starting just under the advisor's card (with a chest open, beside it as vanilla's, a row lower with it; their areas are lambdas, so they're wrapped once, when each button first updates), and the hotbar lock moved from the screen's left edge into that grid, below them (`SideButtons.PlaceHotbarLock`); and the achievement advisor's card at whole pixels (its scales, 0.75 and 0.35, rounded), its border centred over the grid on whole screen pixels (at that scale its art doesn't share the slots' grid) with its top on the inventory's (`InventoryLabelMixin` places it).
- `UIIconButtonGroupMixin` (with `PageIcons`, `SideButtons`): the camera, housing and equipment page icons above the equipment columns centred over the columns, on the art's pixel grid (so art an odd number of pixels wide sits half a pixel left of centre); and the builder toggles (block replacement, torch swapping, then the builder accessories') moved from the left edge into the side-button grid after the hotbar lock, two to a row, packed past hidden ones and following the bestiary row while a chest is open. Their areas are wrapped once, when each group first draws.
- `UIHotbarMixin`, `UIHotbarSlotMixin`: Aether's retained hotbar, laid out again after its own layout at the bottom centre, all slots at one scale, 48 units (`Hud.HotbarSlotSize`, 4 more than the inventory's); its name is drawn instead of Aether's, in its rarity's colour (`Hud.NameColor`, as vanilla's hover text) `Hud.NameLift` above the slots, only when the held item changes while the hotbar is shown (not when it appears: entering a world, closing the inventory). Hovering a slot shows no name at the cursor (`UIHotbarSlotMixin` skips `DrawSelf`'s writes to `Main.hoverItemName`).
- `SdfTextMixin` (with `SdfText`): at the start of each frame's draw, each loaded font (`MouseText`, `ItemStack`, `DeathText` and the two combat fonts) not yet replaced gets Andy Bold's glyphs from the distance field atlas (`DynamicSpriteFont.SetCharacterImage`), at a size and offset fitted to that font's own glyphs (`SdfText.Fonts`), so text measures and wraps as before.
  The atlas texture carries the shader (`Texture.EffectEXT`) and linear filtering (`Texture.SamplerEXT`); characters Andy Bold lacks keep the font's images.
  Fonts load asynchronously and again with resource packs, hence the per-frame check.
- `UIImageButtonMixin`, `UICharacterListItemMixin`, `UIWorldListItemMixin` (with `PixelImages`): `UIImageButton`'s image and hover border; art with a texel per pixel (detected once per texture: not every 2×2 block one colour) is drawn at the largest whole number of screen pixels per texel up to the UI scale, centred where it was. The character and world list items' buttons (that art) are drawn as just their icons (a copy of each texture without the 2-pixel frame, the fill and the icon's shadow on it) at the GUI scale's screen pixels per texel, laid out again after each item's constructor at that size, 2 units apart with the labels after them (their capitals level with the buttons' middle, `SdfText.MouseTextCapMiddle`), the item taller by what they gained and its left column as far in as the delete button on the right; `SetImage` (favourite, cloud) keeps that size.
- `UIListMixin`, `UIBestiaryEntryInfoPageMixin`, `UIScrollbarMixin` (with `MomentumScroll`): a wheel turn's change to a scrollbar's view position (`UIList.ScrollWheel`, the bestiary entry page's `UpdateScrollbar`) is a third of vanilla's (`MomentumScroll.StepScale`) and becomes velocity instead: each scrollbar's `DrawSelf` moves it along an exponentially decaying glide (10 per second, covering the step) by the time since its last frame, stopping at the list's ends, when nearly still, or when the handle is dragged. A turn while the bar already glides its way adds more, up to twice its step (`MaxBoost`) once the glide is as fast as a turn, doubling the fastest scroll. Turning the wheel the other way stops the glide first.
- `UIBuffBarMixin`, `BigProgressBarHelperMixin`: the buff rows move up to the top left corner, and the boss health bar above the hotbar while it's shown.
- `ChatMixin` (with `ChatInput`, `CommandPopup`): the chat input.
  Terraria's `GetInputText` call in `DoUpdate_HandleChat` goes to `ChatInput.Edit`, which handles the cursor, selection, clipboard, word deletion and sent-message history itself and gives Terraria only the text before the cursor (so typing, Backspace and IME work there), with Ctrl, Insert and Delete hidden from it; Backspace over a selection is shown to it as already held.
  The chat monitor's `Offset` call scrolls by Page Up, Page Down and the wheel instead of Up and Down.
  The selection is drawn behind the input's text (before its `DrawColorCodedStringWithShadow`), and the popup and then the cursor after `DrawPlayerChat` (vanilla's `|` is emptied), the cursor's blink timed by the clock and restarted by every change.
  `CommandPopup` shows the Aether API's `CommandCompletion.Complete` for the text and cursor (again whenever either changes, except after its own completion, whose list Tab keeps cycling, or a recalled message), polling the task, which a mod's suggestion provider can leave unfinished.
- Mixin code can't reference Aether's `internal` types, so it reaches them through selectors, `[Shadow]` and return values instead.
