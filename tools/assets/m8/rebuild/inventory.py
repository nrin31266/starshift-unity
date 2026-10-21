"""Ghi inventory từ file hiện tại; evidence runtime chỉ là các scope đã chạy."""
import collections
import hashlib
import json
import re
import sys
from pathlib import Path

sys.dont_write_bytecode = True
from self_check import check

SOURCE = Path(__file__).resolve().parent
ROOT = SOURCE.parents[3]
ART = ROOT / 'app/Assets/StarShift/Art'
GUID = re.compile(r'guid: ([0-9a-f]{32})')
FOLDERS = ('Ships', 'Bosses', 'Projectiles', 'Pickups', 'UI', 'VFX', 'Environments')


def inventory():
    check()
    refs = collections.defaultdict(set)
    for path in (ROOT / 'app/Assets/StarShift').rglob('*'):
        if path.suffix in {'.unity', '.prefab', '.asset', '.anim', '.controller', '.mat'}:
            for guid in set(GUID.findall(path.read_text(errors='replace'))):
                refs[guid].add(str(path.relative_to(ROOT)))
    assets = []
    for folder in FOLDERS:
        for path in sorted((ART / folder).rglob('*.png')):
            guid = GUID.search(Path(str(path) + '.meta').read_text()).group(1)
            name = path.stem.removesuffix('-sheet')
            if name == 'warden-shield-phase':
                name = 'shield-activate-break'
            source = SOURCE / (name + '.aseprite')
            if not source.exists():
                source = SOURCE.parent / 'cleaned' / (name + '.aseprite')
            assert source.exists(), ('missing editable source', path)
            assert refs[guid], ('unreferenced runtime PNG', path)
            assets.append(dict(id=path.stem, output=str(path.relative_to(ROOT)),
                               source=str(source.relative_to(ROOT)), guid=guid,
                               sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                               serialized_references=sorted(refs[guid])))
    assert len({asset['id'] for asset in assets}) == len(assets), 'duplicate runtime IDs'
    checks = {}
    for name, path in {
        'player_trails_store_save': '.design/m8/final/player.json',
        'player_death_and_ring': '.design/m8/final/survival.json',
        'glass_cases_ui_pause': '.design/m8/final/ui.json',
        'roster_damage_pool_and_terminal_layouts': '.design/m8/final/roster.json',
        'layout_1600': '.design/m8/production-layout-1600.json',
        'layout_2000_boss_recovery_and_nodes': '.design/m8/production-layout-2000.json',
        'speed_streaks_and_background': '.design/m8/background/verification.json',
        'native_importers_and_prefabs': '.design/m8/final/import.json',
        'gameplay_collider_contracts': '.design/m8-rebuild/contracts-verification.json',
    }.items():
        evidence = ROOT / path
        checks[name] = json.loads(evidence.read_text()) if evidence.exists() else {'status': 'NOT RUN'}
    ledger = json.loads((SOURCE / 'jobs.json').read_text())
    used = sum(job['charged'] for job in ledger['jobs'])
    ready = all(evidence.get('status') == 'PASS' for evidence in checks.values())
    result = dict(phase='READY FOR OWNER VISUAL REVIEW' if ready else 'VERIFICATION INCOMPLETE',
                  verification_scope='Representative flows and art fixtures; no device FPS, Android build or full-campaign completion claim',
                  quota=dict(starting=40, charged=used, inferred_remaining=40-used, pro_used=False),
                  checks=checks, category_counts=dict(collections.Counter(Path(a['output']).parent.name for a in assets)),
                  assets=assets)
    (SOURCE.parent / 'production-manifest.json').write_text(json.dumps(result, indent=2) + '\n')
    print(f'PASS: {len(assets)} runtime PNGs with GUID, owner, hash and editable source')


if __name__ == '__main__':
    inventory()
