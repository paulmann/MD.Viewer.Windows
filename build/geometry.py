"""Geometry probe: verifies the sheet, sidebar and toolbar occupy the expected bands."""

import sys

from PIL import Image


def main() -> int:
    path = sys.argv[1] if len(sys.argv) > 1 else "docs/img/rendered-light.png"
    image = Image.open(path).convert("RGB")
    width, height = image.size
    print(f"{path} {width}x{height}")

    y = height // 2
    row = [image.getpixel((x, y)) for x in range(width)]
    white = [x for x, pixel in enumerate(row) if pixel == (255, 255, 255)]
    if white:
        print(f"sheet span at y={y}: {white[0]}..{white[-1]} ({len(white)} px)")
    else:
        print(f"sheet span at y={y}: none (not a light theme?)")

    for probe_y in (5, 20, 45, 55, 60):
        print(f"  y={probe_y:3} left={image.getpixel((5, probe_y))} right={image.getpixel((width - 5, probe_y))}")

    columns = []
    for x in range(0, 300):
        dark = sum(1 for yy in range(60, height - 40, 2) if sum(image.getpixel((x, yy))) / 3 < 150)
        columns.append(dark)

    inked = [i for i, count in enumerate(columns) if count > 0]
    print(f"sidebar ink columns: {inked[0] if inked else 'none'}..{inked[-1] if inked else 'none'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
