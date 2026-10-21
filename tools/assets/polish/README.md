# Presentation polish sources

Editable Aseprite sources and selected PixelLab inputs for the follow-up to the
approved player pair A. Runtime exports remain in the seven art categories;
no source or intermediate atlas belongs in Unity Assets.

`jobs.json` records the original account's 35 completed charges, including rejected
proposals. `atlas.json` maps the selected object cells. The replacement account was
verified at 34 remaining on 2026-10-21; the subsequent [closure pass](../closure/README.md) spent that separate budget.
Do not combine the two accounts' available balances.

From the repository root:

```sh
aseprite --batch --script-param root=/absolute/repo/tools/assets/polish \
  --script-param out=/absolute/repo/.design/presentation-polish/staging \
  --script tools/assets/polish/effects.lua
```

Review staging before copying selected PNGs. Import with
`ConfigureM8Production.Import(ids, frameCount)`: impacts, family cues, curtain,
armor rupture, supply and heat vent use eight frames; focus markers use four;
Warden shelter/sweep pulses use sixteen. Static beam/field/curtain bolt have no
slicing. Run `ConfigurePolishVfx.Bind`, `ConfigurePolishVfx.BulwarkFire` and
`ConfigurePresentationPolish.Apply` in Edit Mode. Apply the later closure binding after these passes; older whole-project setup tools are bootstrap history.

Coin and generic player fragments are exported by the existing rebuild Lua files.
`inventory.py` in `../m8/rebuild/` resolves closure, then polish sources and checks every
runtime PNG has a serialized owner, GUID, hash and editable source. Runtime proof
is local in `.design/presentation-polish/`; canonical tooling and verification scope
belong to [asset pipeline](../../../docs/production/asset-pipeline.md).
