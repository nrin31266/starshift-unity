import tempfile
import unittest
from pathlib import Path

from PIL import Image

from spritesheet import check, pack


class SpriteSheetTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name)
        self.frames = []
        for index in range(3):
            path = self.root / f"frame_{index}.png"
            image = Image.new("RGBA", (8, 8))
            image.putpixel((3, 4), (40 + index, 80, 120, 128))
            image.save(path)
            self.frames.append(path)
        self.output = self.root / "sheet.png"

    def test_pack_preserves_order_rgba_and_empty_padding(self):
        self.assertEqual(pack(self.frames, self.output, 2), (16, 16))
        self.assertEqual(check(self.output, (8, 8), 3), (2, 2))
        with Image.open(self.output) as sheet:
            for index, frame in enumerate(self.frames):
                x, y = (index % 2) * 8, (index // 2) * 8
                with Image.open(frame) as source:
                    self.assertEqual(sheet.crop((x, y, x + 8, y + 8)).tobytes(), source.tobytes())
            self.assertIsNone(sheet.crop((8, 8, 16, 16)).getchannel("A").getbbox())

    def test_rejects_mismatched_canvases(self):
        Image.new("RGBA", (4, 8)).save(self.frames[-1])
        with self.assertRaises(ValueError):
            pack(self.frames, self.output, 2)
        self.assertFalse(self.output.exists())

    def test_never_overwrites_output(self):
        self.output.write_bytes(b"keep existing asset")
        with self.assertRaises(FileExistsError):
            pack(self.frames, self.output, 2)
        self.assertEqual(self.output.read_bytes(), b"keep existing asset")

    def test_rejects_opaque_sources(self):
        Image.new("RGB", (8, 8), "red").save(self.frames[-1])
        with self.assertRaises(ValueError):
            pack(self.frames, self.output, 2)

    def test_rejects_invalid_grid_count_and_occupied_padding(self):
        pack(self.frames, self.output, 2)
        for cell, count in [((7, 8), 3), ((8, 8), 5), ((8, 8), 1), ((0, 8), 3)]:
            with self.subTest(cell=cell, count=count), self.assertRaises(ValueError):
                check(self.output, cell, count)
        with Image.open(self.output) as source:
            sheet = source.copy()
        sheet.putpixel((8, 8), (255, 255, 255, 255))
        sheet.save(self.output)
        with self.assertRaises(ValueError):
            check(self.output, (8, 8), 3)


if __name__ == "__main__":
    unittest.main()
