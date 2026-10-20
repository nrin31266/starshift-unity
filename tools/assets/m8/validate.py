"""Validate the actual M8 native assets, not synthetic lookalikes."""
import json
import re
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parent
ART = ROOT.parents[2] / 'app/Assets/StarShift/Art/M8'
PALETTE = {tuple(bytes.fromhex(h)) for h in re.search(r'local colors = \{(.*?)\}', (ROOT / 'author.lua').read_text()).group(1).replace('"', '').split(',')}
SIZES = {'player': (64, 80), 'scout': (80, 64), 'raider': (80, 80), 'heavy': (128, 112), 'boss-module': (96, 96), 'background': (768, 336), 'player-pulse': (12, 32), 'enemy-bolt': (20, 24), 'enemy-telegraph': (28, 28), 'stellar-shard': (32, 40), 'star': (5, 5), 'ui-panel': (32, 32), 'ui-button': (32, 32), 'explosion-sheet': (256, 128)}
rows = []
for name, size in SIZES.items():
    im = Image.open(ART / (name + '.png'))
    assert im.mode == 'RGBA' and im.size == size, (name, im.mode, im.size)
    pixels = set(im.get_flattened_data())
    alpha = {c[3] for c in pixels}
    assert alpha <= {0, 255}, (name, 'fractional alpha')
    assert all(c[:3] in PALETTE for c in pixels if c[3]), (name, 'outside shared palette')
    assert 255 in alpha and (name == 'background' or 0 in alpha), (name, 'empty or opaque sprite')
    rows.append(dict(name=name, size=size, opaqueColors=len({c[:3] for c in pixels if c[3]}), alpha=sorted(alpha)))
sheet = Image.open(ART / 'explosion-sheet.png')
frames = []
for i in range(8):
    frame = Image.open(ROOT / 'frames' / f'explosion_{i:02}.png')
    assert frame.mode == 'RGBA' and frame.size == (64, 64)
    x, y = i % 4 * 64, i // 4 * 64
    assert sheet.crop((x, y, x + 64, y + 64)).tobytes() == frame.tobytes(), ('frame order', i)
    frames.append(frame.tobytes())
assert len(set(frames)) == 8 and Image.open(ROOT / 'frames/explosion_07.png').getbbox() is None
result = dict(status='PASS', assets=rows, exactFrameOrder=True, distinctFrames=8, blankFinalFrame=True)
evidence = ROOT.parents[2] / '.design/m8/pixel-validation.json'
if evidence.parent.exists():
    evidence.write_text(json.dumps(result, indent=2) + '\n')
print(json.dumps(result, indent=2))
