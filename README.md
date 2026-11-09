# FreshUI

An Aether mod with the ID `fresh-ui` that gives Terraria's UI a pixel-perfect, compact look:

- A stepped GUI scale in the settings window (General), in place of the UI scale slider: Auto (one step below the largest), or 1, 2, 3… screen pixels per pixel of the art, up to what fits the screen, for the title menu and the game alike.
  Every pixel of the art covers the same number of screen pixels.
- The cursor and the small icons (equipment page, hotbar lock, builder toggles, defense, empty slot silhouettes) at whole pixels; the cursor at the UI's scale, its pixels the same size as the rest of the UI's.
- Inventory slots with even borders at any scale, and item icons at a whole number of pixels per art pixel.
- Sharp pixel art (point sampling, where the game blurs its scaled UI), sprites snapped to whole pixels, and the character and world list buttons (single-texel art) at a whole number of screen pixels per texel, twice that from GUI scale 3.
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
