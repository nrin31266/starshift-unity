"""Compile the M6 probes with the existing gameplay/input rig outside Assets."""
from pathlib import Path

root = Path(__file__).resolve().parents[2]
imports, bodies = [], []
for name in ("VerifyPacingRefinement.cs", "VerifyPickupMotion.cs", "VerifyRegionB.cs"):
    body = []
    for line in (root / "tools/unity" / name).read_text().splitlines():
        if line.startswith("using "):
            if line not in imports:
                imports.append(line)
        else:
            body.append(line)
    bodies.append("\n".join(body))
output = root / ".design/m6/Verification.cs"
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text("\n".join(imports + bodies))
print(output)
