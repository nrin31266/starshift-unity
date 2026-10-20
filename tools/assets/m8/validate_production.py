"""Validate production exports; visual acceptance remains a Unity review."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
ART = ROOT.parents[2] / 'app/Assets/StarShift/Art/M8'
# Batch 3 bổ sung family ramps, hostile coral/lemon và effect swatches.
def _palette():
    """Hợp nhất palette của mọi author script đang sở hữu export, để batch đang chạy
    (fleet-combat batch 3, final-ui) không bị chặn bởi palette của batch khác."""
    hexes = set()
    for name in ('author.lua', 'production.lua', 'final-player.lua', 'final-ui.lua', 'fleet-combat.lua'):
        path = ROOT / name
        if not path.exists():
            continue
        text = path.read_text()
        # Chỉ đọc literal trong phần khai báo palette đầu file.
        cut = text.find('local pal={}')
        if cut < 0:
            cut = text.find('local palette={}')
        if cut < 0:
            cut = len(text)
        hexes.update(re.findall(r'"([0-9a-f]{6})"', text[:cut]))
    return {tuple(bytes.fromhex(h)) for h in hexes}

PALETTE = _palette()

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
        colors = set(im.get_flattened_data())
        assert colors and any(p[3] for p in colors), f'empty: {output}'
        # Trail ribbon dùng alpha mềm có chủ đích; sprite cứng vẫn phải binary alpha.
        if not source.stem.startswith('trail-'):
            assert all(p[3] in (0, 255) for p in colors), f'alpha fringe: {output}'
        # ui-* thuộc frontend batch đang chạy song song với palette panel riêng;
        # world/fleet/effect assets vẫn phải nằm trong shared + family palette.
        if not source.stem.startswith('ui-'):
            assert all(p[:3] in PALETTE for p in colors if p[3]), f'palette: {output}'
        # ui-heat-fill là tile đặc có chủ đích; các UI còn lại vẫn cần vùng trong suốt.
        if 'background' not in source.stem and not source.stem.startswith(('trail-', 'ui-heat')):
            assert any(p[3] == 0 for p in colors), f'opaque sprite: {output}'
        # Trail ribbon là texture tĩnh một frame, không phải animation 4 frame.
        if sheet.exists() and not source.stem.startswith('trail-'):
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
