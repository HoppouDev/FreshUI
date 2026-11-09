# FreshUI

An Aether mod with the ID `fresh-ui` that gives Terraria's UI a pixel-perfect, compact look:

- One tabbed settings window for the title menu and the game, in the style of Aether's Mods window: General, Display, Graphics, Effects, Interface, Volume, Cursor and Controls in the left panel with the window's actions (Language and Back, or Achievements, Close Menu and Save & Exit) at its bottom, and the open tab's settings in groups in the right one, scrolling when they don't fit.
  Every setting of the game's two settings menus and Aether's graphics page is on one tab: Display has the window, frame rate and scales, Graphics the lighting, terrain renderer, background and world options.
- A stepped GUI scale in the settings window (Display), in place of the UI scale slider: Auto (one step below the largest), or 1, 2, 3… screen pixels per pixel of the art, up to what fits the screen, for the title menu and the game alike.
  Every pixel of the art covers the same number of screen pixels.
- The cursor and the small icons (equipment page, hotbar lock, builder toggles, defense, empty slot silhouettes) at whole pixels; the cursor at the UI's scale, its pixels the same size as the rest of the UI's.
- Inventory slots with even borders at any scale, and item icons at a whole number of pixels per art pixel.
- Sharp pixel art (point sampling, where the game blurs its scaled UI), sprites snapped to whole pixels, and the character and world list buttons as plain icons, their pixels the size of the rest of the UI's.
- The hotbar at the bottom centre: slots a little larger than the inventory's that stay put while scrolling, and the held item's name above them when you switch items, fading out (not when entering a world or closing the inventory).
- No "Inventory" label above the open inventory.
- Buffs in the top left corner, and the boss health bar above the hotbar.
- Text drawn from a signed distance field of Andy Bold, sharp at any size, with the game's spacing.

It needs an Aether build whose FNA has `SamplerState.PixelSnapEXT`, `SpriteBatch.PixelScaleEXT` and `Texture.EffectEXT`, and whose ReLogic has `DynamicSpriteFont.SetCharacterImage`.
Open `FreshUI.slnx` in your IDE.

Run `./aether` on Linux or macOS, or `.\aether.cmd` on Windows.
The first run clones Aether into `.aether/client` and builds the client and Aether API using your Terraria install.
Add `setup` to prepare the client without starting the game.

Run `dotnet build -t:PackMod` to create an `.aethermod`.
See [AGENTS.md](AGENTS.md) for prerequisites and build options.
