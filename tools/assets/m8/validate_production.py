"""Validate production exports; visual acceptance remains a Unity review."""
import hashlib
import json
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
ART = ROOT.parents[2] / 'app/Assets/StarShift/Art/M8'
PALETTE = set(tuple(bytes.fromhex(h)) for h in '080f20 111d32 1c2e46 2d4660 46617a 6e899b a6bfcb e5f4f2 0b3948 126578 1b9bb2 41d2de a0f1ec 271b32 50273d 87354c bb4057 ed6470 ffb69c 3b2836 745047 ad704b e8aa58 ffe0a0 282846 505073 8484ac c0b8d1 464025 7e7745 bab773 f1edb3'.split())

def validate():
    checked = []
    for source in sorted((ROOT / 'cleaned').glob('*.aseprite')):
        output = ART / (source.stem + '.png')
        sheet = ART / (source.stem + '-sheet.png')
        if not output.exists():
            output = sheet
        if not output.exists():
            continue
        im = Image.open(output).convert('RGBA')
        colors = set(im.getdata())
        assert colors and any(p[3] for p in colors), f'empty: {output}'
        assert all(p[3] in (0, 255) for p in colors), f'alpha fringe: {output}'
        assert all(p[:3] in PALETTE for p in colors if p[3]), f'palette: {output}'
        if 'background' not in source.stem:
            assert any(p[3] == 0 for p in colors), f'opaque sprite: {output}'
        if sheet.exists():
            frames = Image.open(sheet).convert('RGBA')
            count = 8 if source.stem == 'explosion' else 4
            columns = 4
            cell = frames.width // columns, frames.height // (2 if count == 8 else 1)
            assert frames.width % columns == 0
            cells = [frames.crop(((i % columns)*cell[0], (i//columns)*cell[1], (i%columns+1)*cell[0], (i//columns+1)*cell[1])).tobytes() for i in range(count)]
            assert len(set(cells)) >= min(3, count), f'static animation: {sheet}'
        checked.append({'id': source.stem, 'size': list(im.size), 'sha256': hashlib.sha256(output.read_bytes()).hexdigest()})
    assert len(checked) > 70, f'incomplete source batch: {len(checked)}'
    target = ROOT.parents[2] / '.design/m8/production-pixel-validation.json'
    target.write_text(json.dumps({'status': 'PASS', 'assets': checked}, indent=2) + '\n')
    print(f'PASS: {len(checked)} source/export pairs, palette, binary alpha, distinct sheet frames')

if __name__ == '__main__':
    validate()
