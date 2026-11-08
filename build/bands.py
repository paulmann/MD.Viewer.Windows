"""Checks that each diagram band in a rendered page actually contains ink."""

import sys

from PIL import Image

BANDS = {
    "flowchart": (182, 299),
    "sequence": (567, 345),
    "pie": (999, 297),
    "class": (1381, 308),
    "state": (1775, 272),
    "gantt": (2133, 275),
    "journey": (2494, 259),
    "fallback": (2839, 76),
}


def main() -> int:
    path = sys.argv[1] if len(sys.argv) > 1 else ".zwork/shots/mermaid.png"
    image = Image.open(path).convert("RGB")
    width, height = image.size
    left, right = 56, width - 56

    for name, (top, band) in BANDS.items():
        bottom = min(height, top + band)
        ink = 0
        coloured = 0
        for y in range(top, bottom, 2):
            for x in range(left, right, 2):
                r, g, b = image.getpixel((x, y))
                if r < 245 or g < 245 or b < 245:
                    ink += 1
                if abs(r - g) > 12 or abs(g - b) > 12:
                    coloured += 1

        print(f"{name:10} y={top:4}..{bottom:4}  ink={ink:6}  coloured={coloured:5}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
