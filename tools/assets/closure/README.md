# Art closure sources

PixelLab atlas 16 object (10 charges), enemy/boss Pro animation (20), strike
animation (1), three UI icons (3): 34 completed charges, verified remaining 0.
`jobs.json` is the actual ledger; `atlas.json` maps selected object IDs/cells.
Raw downloads and editable Aseprite sources stay outside Unity Assets.

```sh
aseprite --batch --script-param root=/absolute/repo/tools/assets/closure \
  --script-param out=/absolute/repo/.design/art-closure/staging \
  --script tools/assets/closure/effects.lua
```

Create staging first. Seed staging with existing UI PNGs, then run effects.lua
and the rebuild `pickups.lua` with its own root and this output directory to
re-export glass cases from the new icons. Review selected exports before import.
Normal death uses eight selected Pro frames over 0.5 s. Boss uses 16; both omit
the corrupt first Pro frame and end empty. Strike lock uses eight animation
frames. Other closure sheets use eight except the four-frame curtain bolt.
UI icons and sweep arrow are single sprites. `ConfigureM8Production.Import`
keeps existing sprite IDs. Final binding is stored in the versioned prefabs, scene
and data; the one-time ConfigureArtClosure migration is retired.

The manifest resolves closure sources first. Do not rerun older whole-project
bootstrap passes on tuned production data. Runtime evidence and screenshots
belong in ignored `.design/art-closure/`; canonical tool policy is in the
[asset pipeline](../../../docs/production/asset-pipeline.md#art-closure--2026-10-21).
