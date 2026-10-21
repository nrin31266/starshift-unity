"""Check the approved hull boundary and glass-case sheet exports."""
from pathlib import Path
from PIL import Image

SOURCE = Path(__file__).resolve().parent
ART = SOURCE.parents[3] / 'app/Assets/StarShift/Art'
FILES = {p.stem: p for p in ART.rglob('*.png')}

def asset(name):
    return FILES[name]


def check():
    for name in ('player', 'player-amber', 'player-swept', 'player-swept-amber'):
        before = Image.open(SOURCE / 'approved' / f'{name}.png').convert('RGBA')
        after = Image.open(asset(name)).convert('RGBA')
        assert before.size == after.size, name
        swept = 'swept' in name
        bottom, centers = (72, (23, 41)) if swept else (68, (22, 41))
        for y in range(before.height):
            for x in range(before.width):
                if before.getpixel((x, y)) != after.getpixel((x, y)):
                    assert bottom - 10 <= y < 80 and any(abs(x - cx) <= 6 for cx in centers), (name, x, y)
    identities = []
    for name in ('pulse-core', 'plasma-core', 'homing-core', 'missile-ammo',
                 'extra-life', 'support-drone', 'shield-cell', 'overdrive'):
        sheet = Image.open(asset(name + '-sheet')).convert('RGBA')
        assert sheet.size == (256, 64), name
        assert any(0 < alpha < 100 for alpha in sheet.getchannel('A').get_flattened_data()), (name, 'glass is opaque')
        frames = [sheet.crop((x * 64, 0, (x + 1) * 64, 64)).tobytes() for x in range(4)]
        assert len(set(frames)) == 4, (name, 'duplicate glint frames')
        assert Image.open(asset(name)).convert('RGBA').tobytes() == frames[0], name
        identities.append(frames[0])
    assert len(set(identities)) == 8, 'duplicate pickup identities'
    print('PASS: approved A hull boundary; eight glass-case identities; exact four-frame glint exports')


if __name__ == '__main__':
    check()
