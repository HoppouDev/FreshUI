# FreshUI

An Aether mod with the ID `fresh-ui` that gives Terraria's UI a pixel-perfect, compact look:

- A stepped GUI scale in the settings window (General), in place of the UI scale slider: Auto, or 1, 2, 3… screen pixels per pixel of the art, up to what fits the screen, for the title menu and the game alike.
  Every pixel of the art covers the same number of screen pixels.
- Inventory slots with even borders at any scale, and item icons at a whole number of pixels per art pixel.
- With Aether's Sharp UI on, sprites are snapped to whole pixels.
- The hotbar at the bottom centre: inventory-sized slots that stay put while scrolling, and the held item's name above them at half size, fading out.
- Buffs in the top left corner, and the boss health bar above the hotbar.

It needs an Aether build with `SamplerState.PixelSnapEXT` in its FNA.
Open `FreshUI.slnx` in your IDE.

Run `./aether` on Linux or macOS, or `.\aether.cmd` on Windows.
The first run clones Aether into `.aether/client` and builds the client and Aether API using your Terraria install.
Add `setup` to prepare the client without starting the game.

Run `dotnet build -t:PackMod` to create an `.aethermod`.
See [AGENTS.md](AGENTS.md) for prerequisites and build options.
