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
            source = SOURCE.parents[1] / 'closure' / (name + '.aseprite')
            if not source.exists():
                source = SOURCE.parents[1] / 'polish' / (name + '.aseprite')
            if not source.exists():
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
    baseline_checks = checks
    checks = {}
    for name, path in {
        'travel_blend_pause': 'screenshots/verification.json',
        'factory_reset_coin_menu': 'settings.json',
        'thermal_profiles': 'thermal.json',
        'weapon_heat_player_death_boss_hud': 'combat.json',
        'natural_projectile_drain': 'projectile-drain.json',
        'vfx_1600': 'vfx-1600.json',
        'vfx_2000': 'vfx-2000.json',
        'enemy_deaths_supply': 'deaths.json',
        'bulwark_fire': 'bulwark.json',
        'full_mission': 'full-mission.json',
        'native_importers': 'native-import.json',
        'requested_contracts_only': 'contracts.json',
        'final_editor_state': 'final-state.json',
        'serialized_references': 'references.json',
    }.items():
        evidence = ROOT / '.design/presentation-polish' / path
        checks[name] = json.loads(evidence.read_text()) if evidence.exists() else {'status': 'NOT RUN'}
    closure_checks = {}
    for name in ('carry', 'strike', 'beams', 'boss', 'dev-editor', 'dev-runtime', 'art', 'e2e', 'final-state'):
        path = ROOT / '.design/art-closure' / (name + '.json')
        closure_checks[name] = json.loads(path.read_text()) if path.exists() else {'status': 'NOT RUN'}
    closure = json.loads((SOURCE.parents[1] / 'closure/jobs.json').read_text())
    closure_used = sum(job['cost'] for job in closure['jobs'] + closure['animations'] if job['status'] == 'completed')
    assert closure_used == 34 and closure['verified_remaining'] == 0, 'closure quota ledger mismatch'
    ledger = json.loads((SOURCE / 'jobs.json').read_text())
    polish = json.loads((SOURCE.parents[1] / 'polish/jobs.json').read_text())
    used = sum(job['charged'] for job in ledger['jobs']) + sum(job['cost'] for job in polish['jobs'] + polish['animations'] if job['status'] == 'completed')
    ready = all(evidence.get('status') == 'PASS' or evidence.get('pass') is True or evidence.get('passed') is True for evidence in list(checks.values()) + list(closure_checks.values()))
    result = dict(phase='ART CLOSURE VERIFIED' if ready else 'VERIFICATION INCOMPLETE',
                  verification_scope='Representative flows and art fixtures; no device FPS, Android build or full-campaign completion claim',
                  quota=dict(original_account=dict(starting=40, charged=used, verified_remaining=0, pro_used=True), replacement_account=dict(starting=34, verified_remaining=0, charged_this_followup=closure_used)),
                  closure_checks=closure_checks, checks=checks, baseline_checks=baseline_checks, category_counts=dict(collections.Counter(Path(a['output']).parent.name for a in assets)),
                  assets=assets)
    (SOURCE.parent / 'production-manifest.json').write_text(json.dumps(result, indent=2) + '\n')
    print(f'PASS: {len(assets)} runtime PNGs with GUID, owner, hash and editable source')


if __name__ == '__main__':
    inventory()
