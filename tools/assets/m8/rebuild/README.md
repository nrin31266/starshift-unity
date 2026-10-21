# Presentation rebuild sources

Current production status and remaining work belong to
[asset pipeline](../../../../docs/production/asset-pipeline.md#m8-clean-rebuild--2026-10-21).
This directory contains editable Aseprite sources and the scripts that export them.

`approved/` retains the selected player pair A. `player.lua` repaints only the two
stacked nozzle regions; it preserves plume and the remaining hull/skin pixels.
The rejected player pair B is archived in commit `be108628`. `jobs.json` records
the first five charged generations. The remaining 35 were subsequently spent;
current quota and follow-up sources belong to [polish](../../polish/README.md).

Run Aseprite from the repository root, providing absolute source/output paths:

```sh
aseprite --batch --script-param root=/absolute/repo/tools/assets/m8/rebuild \
  --script-param out=/absolute/repo/.design/m8-rebuild/staging \
  --script tools/assets/m8/rebuild/player.lua
```

Use the same command for `fleet.lua`, `ui.lua`, `pickups.lua`, `modules.lua`,
`environment.lua`, then `effects.lua`. Order matters: glass cases read UI icons and
cargo VFX reads the crate export. Exports are staging files; review them before import.
Keep existing Unity metadata alongside any recreated bound PNG before refreshing.

Native import/slicing uses `ConfigureM8Production.Import(ids, frameCount)` with four
frames, except explosion (eight) and rock shatter (twelve). Rock uses its workbench
anchor. Final binding is stored in the versioned prefabs, scene and data. The
one-time Configure/Rebuild migrations are retired; do not replay old setup over tuning.

`python3 tools/assets/m8/rebuild/self_check.py` checks the approved hull boundary,
glass-case identities, glint uniqueness and exact first-frame exports. Native checks:
`VerifyM8Final.Ui`, `VerifyM8Final.Survival`, `VerifyM8ProductionLayout.Edit/Run`
and `InspectM8Production.Contracts` snapshots. Runtime checks require idle Menu and use
separate save storage. Evidence is local under `.design/m8/`; it is not shipped art.

Runtime exports are organized by category under `app/Assets/StarShift/Art/`;
active import/binding tools resolve asset names there and preserve GUIDs.

`python3 tools/assets/m8/rebuild/inventory.py` refreshes the manifest from current
files and recorded evidence. Copy only reviewed exports with a runtime owner, not
the whole staging directory. Retained sources in `../cleaned/` export directly from
Aseprite; discarded variants remain reproducible from Lua and Git history.
