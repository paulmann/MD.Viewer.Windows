"""Sample colours and ink density of a screenshot to verify chrome geometry.

Usage: python build/probe.py <file.png>
"""

import sys
from collections import Counter

from PIL import Image


def region_ink(image, box):
    crop = image.crop(box).convert("L")
    pixels = list(crop.getdata())
    dark = sum(1 for p in pixels if p < 140)
    return dark / max(1, len(pixels))


def main() -> int:
    if len(sys.argv) < 2:
        print("usage: probe.py <file.png>")
        return 2

    path = sys.argv[1]
    image = Image.open(path).convert("RGB")
    width, height = image.size
    print(f"{path} {width}x{height}")

    points = {
        "toolbar-left": (24, 12),
        "toolbar-mid": (width // 2, 12),
        "toolbar-right": (width - 40, 12),
        "status-left": (30, height - 12),
        "canvas-top": (width - 60, 70),
        "sidebar": (60, 200),
        "sidebar-border": (287, 300),
        "sheet-inside": (width // 2, 260),
        "sheet-edge-left": (300, 300),
        "body-text": (width // 2, 150),
    }
    for name, (x, y) in points.items():
        if 0 <= x < width and 0 <= y < height:
            print(f"  {name:16} ({x:4},{y:4}) = {image.getpixel((x, y))}")

    boxes = {
        "toolbar": (0, 0, width, 52),
        "sidebar": (0, 52, 286, height - 28),
        "content": (286, 52, width, height - 28),
        "statusbar": (0, height - 28, width, height),
    }
    for name, box in boxes.items():
        print(f"  ink {name:10} = {region_ink(image, box):.3f}")

    colors = Counter(image.getdata())
    print("  top colours:")
    for color, count in colors.most_common(8):
        print(f"    {color} x{count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
