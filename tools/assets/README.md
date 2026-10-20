# Offline asset tools

Offline PNG tools và Comfy workflows cho StarShift. Chạy từ repository root;
Python cần Pillow (đã có trên máy cấu hình hiện tại).

Quy trình import, naming, nguồn và license thuộc
[asset pipeline](../../docs/production/asset-pipeline.md).

## Spritesheet commands

```sh
python tools/assets/spritesheet.py pack \
  /path/frame_00.png /path/frame_01.png /path/frame_02.png \
  --columns 2 --output /path/explosion_sheet.png
python tools/assets/spritesheet.py check /path/explosion_sheet.png \
  --cell 128 128 --frames 3
python -m unittest discover -s tools/assets -p 'test_*.py' -v
```

Thay `128 128` bằng canvas frame thực. Input là PNG có pixel trong suốt, cùng canvas;
thứ tự theo arguments, trái sang phải rồi trên xuống dưới. Tool giữ RGBA/canvas,
không resize/trim/auto-center. Output không được tồn tại trước, parent directory
phải có sẵn. Khi slice Unity chỉ lấy đúng số frame, bỏ ô transparent dư cuối.
Check xác nhận grid/alpha/count và ô dư; không đánh giá style, chuyển động hoặc pivot.

## Comfy workflow seed

[comfy-nano-banana.json](workflows/comfy-nano-banana.json) là API graph do Comfy MCP
emit, có node tính phí và SaveImageAdvanced. Chưa chạy/validate trên runtime;
không phải local diffusion workflow hay final art prompt.
Readiness, auth, dependency validation và recipe nằm trong
[asset pipeline](../../docs/production/asset-pipeline.md#recipe-comfy-workflow).

## Local fallback và Unity smoke check

[comfy-sd15-smoke.json](workflows/comfy-sd15-smoke.json) là graph SD1.5 core-only,
không có node tính phí. Canvas 512×512 chỉ dùng test; không chốt sprite size.
Launch commands, model path/hash và checkpoint evidence ở
[toolchain setup](../../docs/production/asset-pipeline.md#toolchain-setup-ngày-2026-10-20).

[verify-unity-art-toolchain.cs](verify-unity-art-toolchain.cs) chạy qua Unity MCP
`eval_file` trong Edit Mode với synthetic helper-sheet fixture tại đường dẫn ghi
trong snippet. Kiểm capability, import, 3 rects/frame names, pivot/PPU/filter
read-back rồi xóa scratch folder bằng AssetDatabase. PPU 16/center/Point chỉ là
fixture; không thay đổi import settings hoặc scene production.

## M8 selected pixel slice

`m8/sources/manifest.json` giữ selected/rejected raw generation provenance và font
sources. `m8/cleaned/*.aseprite` là editable source; `m8/frames/` giữ explicit
explosion frame order. `m8/author.lua` reproduce native selected exports bằng Aseprite;
không rerun đè artist edits. `python3 tools/assets/m8/validate.py` kiểm actual PNG
sizes, shared palette, binary alpha, distinct frames và exact sheet frame order.
Full commands/import/runtime checks ở
[M8 recipe](../../docs/production/asset-pipeline.md#m8-selected-sources-và-production-recipe).

## M8 preparation checkpoint

[Production manifest](m8/production-manifest.json) liệt kê remaining L1–L8 asset routes,
outputs, native-size proposals và presentation bindings; không là bulk runner.
Single animate-existing smoke giữ raw frames, Aseprite source/cleanup và cost provenance
trong [animation-smoke](m8/animation-smoke/provenance.json). Pipeline/verification recipe
ở [asset pipeline](../../docs/production/asset-pipeline.md#m8-preparation-checkpoint).
