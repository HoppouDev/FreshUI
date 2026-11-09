# /// script
# requires-python = ">=3.12"
# dependencies = ["fonttools", "numpy", "pillow", "scipy"]
# ///
"""Renders a TrueType font's glyphs into a signed distance field atlas for FreshUI's text.

    uv run tools/sdf_font.py FONT.ttf OUT_DIR

Writes OUT_DIR/andy-sdf.png (the distance in alpha: 0.5 at a glyph's edge, higher inside, SPREAD
pixels of the atlas to 0 or 1) and OUT_DIR/andy-sdf.json (each glyph's atlas rectangle and its top
left corner from the pen at the baseline, in atlas pixels at EM pixels per em).
"""

import json
import sys
from pathlib import Path

import numpy as np
from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont
from scipy.ndimage import distance_transform_edt

EM = 152  # Eight times the size of Terraria's UI font (Andy Bold at 19 pixels per em).
SPREAD = (
    16  # Atlas pixels from the edge to the end of the field (about a tenth of an em).
)
SUPERSAMPLE = 4  # Rendered this much finer for exact distances.
WIDTH = 2048


def render(font: ImageFont.FreeTypeFont, char: str):
    left, top, right, bottom = font.getbbox(char, anchor="ls")
    if right <= left or bottom <= top:
        return None
    s = SUPERSAMPLE
    x0, y0 = int(np.floor(left / s)) - SPREAD, int(np.floor(top / s)) - SPREAD
    x1, y1 = int(np.ceil(right / s)) + SPREAD, int(np.ceil(bottom / s)) + SPREAD
    image = Image.new("L", ((x1 - x0) * s, (y1 - y0) * s))
    ImageDraw.Draw(image).text(
        (-x0 * s, -y0 * s), char, font=font, fill=255, anchor="ls"
    )
    inside = np.asarray(image) > 127
    signed = distance_transform_edt(inside) - distance_transform_edt(~inside)
    centres = signed[s // 2 :: s, s // 2 :: s] / s
    field = np.clip(0.5 + centres / (2 * SPREAD), 0, 1)
    return x0, y0, (field * 255 + 0.5).astype(np.uint8)


def main() -> None:
    ttf, out = sys.argv[1], Path(sys.argv[2])
    font = ImageFont.truetype(ttf, EM * SUPERSAMPLE)
    chars = sorted(c for c in TTFont(ttf).getBestCmap() if 32 < c <= 0xFFFF)
    glyphs = []
    for code in chars:
        rendered = render(font, chr(code))
        if rendered is not None:
            glyphs.append((code, *rendered))

    # Shelves, tallest first.
    glyphs.sort(key=lambda glyph: -glyph[3].shape[0])
    placed, x, y, shelf = {}, 0, 0, 0
    for code, gx, gy, field in glyphs:
        h, w = field.shape
        if x + w > WIDTH:
            x, y, shelf = 0, y + shelf + 1, 0
        placed[code] = (x, y, w, h, gx, gy, field)
        x, shelf = x + w + 1, max(shelf, h)
    atlas = np.zeros((y + shelf, WIDTH), np.uint8)
    for x, y, w, h, _, _, field in placed.values():
        atlas[y : y + h, x : x + w] = field

    out.mkdir(parents=True, exist_ok=True)
    Image.fromarray(atlas, "L").save(out / "andy-sdf.png", optimize=True)
    metadata = {
        "em": EM,
        "spread": SPREAD,
        "glyphs": {str(code): list(g[:6]) for code, g in sorted(placed.items())},
    }
    (out / "andy-sdf.json").write_text(json.dumps(metadata, separators=(",", ":")))
    print(f"{len(placed)} glyphs, atlas {WIDTH}x{atlas.shape[0]}")


if __name__ == "__main__":
    main()
