"""Check the approved hull boundary and capsule sheet exports."""
from pathlib import Path
from PIL import Image

SOURCE = Path(__file__).resolve().parent
ART = SOURCE.parents[3] / 'app/Assets/StarShift/Art/M8'


def check():
    for name in ('player', 'player-amber', 'player-swept', 'player-swept-amber'):
        before = Image.open(SOURCE / 'approved' / f'{name}.png').convert('RGBA')
        after = Image.open(ART / f'{name}.png').convert('RGBA')
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
        sheet = Image.open(ART / f'{name}-sheet.png').convert('RGBA')
        assert sheet.size == (256, 80), name
        frames = [sheet.crop((x * 64, 0, (x + 1) * 64, 80)).tobytes() for x in range(4)]
        assert len(set(frames)) == 4, (name, 'duplicate glint frames')
        assert Image.open(ART / f'{name}.png').convert('RGBA').tobytes() == frames[0], name
        identities.append(frames[0])
    assert len(set(identities)) == 8, 'duplicate pickup identities'
    print('PASS: approved A hull boundary; eight capsule identities; exact four-frame glint exports')


if __name__ == '__main__':
    check()
