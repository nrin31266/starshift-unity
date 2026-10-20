"""Pack PNG frames without resizing; check sprite sheet grid and transparency."""

import argparse
import math
from pathlib import Path

from PIL import Image


def read_sprite(path):
    with Image.open(path) as source:
        if source.format != "PNG":
            raise ValueError(f"Expected PNG: {path}")
        frame = source.convert("RGBA")
    if frame.getchannel("A").getextrema()[0] == 255:
        raise ValueError(f"Missing transparent pixels: {path}")
    return frame


def pack(frames, output, columns):
    if columns < 1 or not frames:
        raise ValueError("Provide frames and a positive column count")
    if columns > len(frames):
        raise ValueError("Column count exceeds frame count")
    if output.resolve() in {Path(frame).resolve() for frame in frames}:
        raise ValueError("Output must differ from input frames")
    images = [read_sprite(frame) for frame in frames]
    width, height = images[0].size
    if any(image.size != (width, height) for image in images):
        raise ValueError("Frame canvases must have identical dimensions")
    sheet = Image.new("RGBA", (width * columns, height * math.ceil(len(images) / columns)))
    for index, image in enumerate(images):
        # Giữ nguyên RGBA và tâm canvas, không trim hoặc dùng alpha làm mask lần nữa.
        sheet.paste(image, ((index % columns) * width, (index // columns) * height))
    with output.open("xb") as target:
        sheet.save(target, format="PNG")
    return sheet.size


def check(path, cell, frames):
    width, height = cell
    if min(width, height, frames) < 1:
        raise ValueError("Cell dimensions and frame count must be positive")
    sheet = read_sprite(path)
    if sheet.width % width or sheet.height % height:
        raise ValueError("Sheet dimensions are not divisible by cell dimensions")
    columns, rows = sheet.width // width, sheet.height // height
    if frames > columns * rows or math.ceil(frames / columns) != rows:
        raise ValueError("Frame count does not match the sheet grid")
    for index in range(frames, columns * rows):
        x, y = (index % columns) * width, (index // columns) * height
        if sheet.crop((x, y, x + width, y + height)).getchannel("A").getbbox():
            raise ValueError("Unused trailing cells contain visible pixels")
    return columns, rows


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    pack_parser = commands.add_parser("pack", help="Frames follow explicit argument order, left to right, top to bottom")
    pack_parser.add_argument("frames", type=Path, nargs="+")
    pack_parser.add_argument("--output", type=Path, required=True)
    pack_parser.add_argument("--columns", type=int, required=True)
    check_parser = commands.add_parser("check")
    check_parser.add_argument("sheet", type=Path)
    check_parser.add_argument("--cell", type=int, nargs=2, required=True, metavar=("WIDTH", "HEIGHT"))
    check_parser.add_argument("--frames", type=int, required=True)
    args = parser.parse_args()
    try:
        if args.command == "pack":
            print("Sheet size:", pack(args.frames, args.output, args.columns))
        else:
            print("Grid:", check(args.sheet, args.cell, args.frames))
    except (OSError, ValueError) as error:
        parser.exit(1, f"Error: {error}\n")


if __name__ == "__main__":
    main()
