"""Inventory M8 files and serialized references without trusting past review flags."""
import collections
import hashlib
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ART = Path('app/Assets/StarShift/Art/M8')
GUID = re.compile(r'guid: ([0-9a-f]{32})')
KEEP = {'astra-planet', 'crimson-planet', 'background', 'crimson-background', 'explosion-sheet', 'star'}
REFERENCE = {'boss-module', 'player', 'player-amber', 'player-swept', 'player-swept-amber', 'support-companion'}
RETIRE = {'hud-ship', 'player-homing', 'player-plasma', 'legacy-crate', 'asteroid-anchor', 'ui-cosmetic-state', 'ui-mission-state', 'boost-warp', 'boost-warp-sheet'}


def classify(stem, exists, referenced):
    if stem in RETIRE and not referenced:
        return 'DELETE'
    if not exists:
        return 'MISSING BUT REQUIRED' if referenced else 'DELETE'
    if stem in KEEP:
        return 'KEEP'
    if stem in REFERENCE:
        return 'KEEP AS REFERENCE'
    return 'REDRAW'


def inventory():
    tracked = subprocess.check_output(['git', 'ls-files', str(ART)], cwd=ROOT, text=True).splitlines()
    paths = {p for p in tracked if p.endswith('.png')}
    paths.update(str(p.relative_to(ROOT)) for p in (ROOT / ART).rglob('*.png'))
    refs = collections.defaultdict(list)
    for path in (ROOT / 'app/Assets/StarShift').rglob('*'):
        if path.suffix not in {'.unity', '.prefab', '.asset', '.anim', '.controller', '.mat'}:
            continue
        for guid in set(GUID.findall(path.read_text(errors='replace'))):
            refs[guid].append(str(path.relative_to(ROOT)))
    assets = []
    for rel in sorted(paths):
        path = ROOT / rel
        meta = Path(str(path) + '.meta')
        text = meta.read_text() if meta.exists() else ''
        origin = 'filesystem'
        if not text and str(meta.relative_to(ROOT)) in tracked:
            text = subprocess.check_output(['git', 'show', 'HEAD:' + str(meta.relative_to(ROOT))], cwd=ROOT, text=True)
            origin = 'tracked metadata; restore deliberately before recreating bound asset'
        match = GUID.search(text)
        guid = match.group(1) if match else None
        references = sorted(refs.get(guid, []))
        exists = path.is_file()
        assets.append({'id': path.stem, 'output': rel, 'exists': exists,
                       'classification': classify(path.stem, exists, references),
                       'guid': guid, 'metadata_exists': meta.exists(), 'metadata_origin': origin,
                       'serialized_references': references,
                       'sha256': hashlib.sha256(path.read_bytes()).hexdigest() if exists else None,
                       'runtime_verified': False})
    return {'phase': 'rebuild-audit', 'runtime_verified': False,
            'counts': dict(collections.Counter(a['classification'] for a in assets)), 'assets': assets}


def self_check():
    assert classify('scout', False, ['prefab']) == 'MISSING BUT REQUIRED'
    assert classify('scout', True, ['prefab']) == 'REDRAW'
    assert classify('legacy-crate', False, []) == 'DELETE'
    assert classify('legacy-crate', False, ['prefab']) == 'MISSING BUT REQUIRED'
    assert classify('astra-planet', True, []) == 'KEEP'


if __name__ == '__main__':
    self_check()
    report = inventory()
    output = ROOT / 'tools/assets/m8/rebuild-audit.json'
    output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report['counts']))
