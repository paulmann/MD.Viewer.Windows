"""Coarse ASCII preview of a screenshot — a text-mode way to check layout.

Usage: python build/ascii_preview.py <file.png> [columns]

The image is reduced to a luminance grid; each cell becomes one character.
Dark pixels map to dense characters, light pixels to spaces, so bands, panels,
columns and text blocks are visible in a terminal.
"""

import sys
from PIL import Image

RAMP = " .:-=+*#%@"


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: ascii_preview.py <file.png> [columns]")
        return 2

    path = sys.argv[1]
    columns = int(sys.argv[2]) if len(sys.argv) > 2 else 110

    image = Image.open(path).convert("RGB")
    width, height = image.size
    cell = max(1, width // columns)
    rows = max(1, height // (cell * 2))

    small = image.resize((columns, rows), Image.LANCZOS)
    pixels = small.load()

    print(f"{path}  {width}x{height}  grid {columns}x{rows}")
    for y in range(rows):
        line = []
        for x in range(columns):
            r, g, b = pixels[x, y]
            lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0
            index = int((1.0 - lum) * (len(RAMP) - 1))
            line.append(RAMP[max(0, min(len(RAMP) - 1, index))])
        print("".join(line))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
