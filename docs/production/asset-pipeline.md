# Asset pipeline

## Phạm vi và ownership

Owner của asset sources, generation/import/slicing/naming/licensing/tooling và reproducibility. Visual target do [art direction](../presentation/art-direction.md) quyết; feedback do [audio/VFX](../presentation/audio-vfx.md) quyết.

## Canonical production workflow

`Gameplay placeholder` → `Visual reference pack` → `Style approval`
→ `Vertical-slice asset batch` → `Normalize` → `Import`
→ `Unity Game View + Play Mode review` → `Lock visual family`
→ `Produce assets theo từng region`.

Không generate toàn bộ 12 level ngay từ đầu. Style approval duyệt hướng nhìn qua
reference pack; lock visual family chỉ sau khi batch đã qua Unity gameplay-scale
review và chạy trong Unity. Theo yêu cầu setup ngày 2026-10-20, giai đoạn M8
hiện tại chỉ test trên Unity; không Android, build/release hoặc device gate.
Mobile vẫn là product target, không phải bằng chứng device-tested.

### Placeholder-first

Gameplay implementation không phụ thuộc final art. Vertical slice được dùng primitive
shape, temporary sprite, simple placeholder projectile, asteroid/background để validate
movement, shooting, collision, death, respawn và encounter. Final art chỉ thay placeholder
sau khi core loop hoạt động; không dùng production art làm điều kiện bắt đầu gameplay.

### Visual reference pack

Trước production hàng loạt, tạo pack nhỏ gồm khoảng Player Ship, Scout Drone,
Bomber, boss silhouette/module sample, normal/resource asteroid và Region A background
sample. Đây là baseline linh hoạt, không khóa số lượng tuyệt đối.
Duyệt pack theo [Asset Bible và visual family](../presentation/art-direction.md#asset-bible),
lưu các mẫu đã duyệt làm anchors; các batch sau phải đối chiếu anchors trước khi import.
Nếu lệch style, sửa source và review lại trước khi tiếp tục batch.

### Asset source types

Cho phép AI image generation, PixelLab/MCP, free assets, original drawing,
Python generation và Unity procedural VFX. Mọi nguồn đều phải qua cùng review gate.
AI generation dùng approved family anchors theo
[art direction](../presentation/art-direction.md#visual-family-và-approved-anchors),
không prompt từng asset độc lập. Prompt/seed của M8 anchors được giữ trong [source manifest](../../tools/assets/m8/sources/manifest.json); dùng làm baseline, không copy prompt bỏ qua silhouette cần làm.

## Normalization và source review

AI output không được import thẳng vào production chỉ vì nhìn đẹp.
Luồng chuẩn: `source` → `cleanup if needed` → `normalize` → `review` → `Unity import`.

Normalization kiểm tra transparent background cho sprite tách nền, consistent canvas,
centered visible bounds, consistent scale, correct orientation, không shadow/background
ngoài ý muốn, alpha edge và padding. Background artwork giữ nền có chủ đích theo
art direction. Review source kiểm tra style, alignment và readiness để import;
Unity gameplay-scale review sau import vẫn bắt buộc.
Offline PNG compose/validate utility đã có trong `tools/assets/`; reuse thay vì
viết lại packer. Công cụ này không tự đánh giá style hoặc animation quality.

### PixelLab usage

PixelLab/MCP là optional production tool cho cleanup frame, alignment/grid correction,
pivot consistency, repaint/correction, resize và edge cleanup.
Source đạt yêu cầu đi thẳng tới normalize; source chưa đạt có thể qua PixelLab cleanup
rồi normalize, review và import. Không bắt mọi asset qua PixelLab.

### Tool routing cho StarShift

Project MCP config nằm ở `../../.codex/config.toml`; chỉ bổ sung PixelLab cho
checkout này. ImageGen và Unity/Comfy MCP hiện có được tái sử dụng,
không tạo server trùng hoặc đổi cấu hình global. Tài khoản connector vẫn thuộc
tài khoản Codex, không được cô lập bởi project config; chỉ dùng cho task đang yêu cầu.

| Nhu cầu | Tool/workflow |
|---|---|
| Concept, ship/enemy/boss, background raster | ImageGen, dùng approved anchors; sprite yêu cầu transparent background |
| Pixel art hoặc sửa vùng ảnh | PixelLab image/edit/inpaint, output vẫn phải đạt art direction |
| Object states và sprite animation | PixelLab object/state/animate hoặc animate image |
| Frame/palette/alignment cleanup | Aseprite; giữ source `.aseprite` |
| Background/composite/mask/paint-over lớn | Krita; giữ source `.kra` |
| Fallback không phụ thuộc generation quota | Local ComfyUI + SD1.5, chỉ một checkpoint |
| Ghép/kiểm tra spritesheet PNG | [Offline tools](../../tools/assets/README.md) |
| Import, slicing, pivot và gameplay-scale review | Unity MCP; reuse skill `sprite-editor` và grid-slicing snippet |
| Workflow ảnh với node API | [Comfy seed](../../tools/assets/workflows/comfy-nano-banana.json), cần runtime/auth/validation |

PixelLab dùng HTTP MCP với `http_headers_helper` theo
[Codex MCP configuration](https://developers.openai.com/codex/mcp/).
Helper đọc `~/.config/starshift/pixellab-token` (quyền `0600`), secret nằm ngoài Git.
Máy khác cần tự provision token. Khi đổi token, thay nội dung file và giữ permission.
Reload/restart Codex sau khi đổi cấu hình; lưu file không chứng minh phiên hiện tại
đã nạp tool mới. Không đưa secret vào prompt, workflow hoặc source archive.

Allowlist bật image/object/state/animation/UI/VFX, correction, upload/status/balance/help;
không bật map/tileset, humanoid pipeline hoặc delete. Tra
[PixelLab tools](https://api.pixellab.ai/mcp/docs) và schema live trước khi gọi;
không giả định các model có cùng tham số. Kiểm tra balance trước batch;
cấu hình kết nối không đồng nghĩa duyệt chi phí generation.
Không đổi target art thành pixel art chỉ vì tool hỗ trợ pixel art.

### Recipe sprite và spritesheet

1. Đọc approved anchors, chọn asset/state cụ thể rồi generate/edit bằng ImageGen
   hoặc PixelLab. Với tàu/prop dùng object pipeline, không dùng humanoid skeleton.
2. PixelLab trả job ID: lưu ID, poll bằng get/status tương ứng, xử lý review
   candidate nếu cần, tải output và lưu provenance. Sprite source có sẵn dùng
   animate image theo schema live; object PixelLab dùng animate object.
3. Review frame riêng: cùng canvas/orientation/scale/lighting, tâm không drift,
   alpha sạch. Không trim/auto-center từng frame làm thay đổi chuyển động.
4. Ghép theo thứ tự frame tường minh bằng offline tool; check grid/alpha/count.
   Tool không đánh giá chất lượng animation, pivot hoặc style bằng nội dung ảnh.
5. Trong Unity Edit Mode, xác nhận đúng project `app/` và Editor sẵn sàng; import
   vào runtime folder phù hợp, giữ GUID. Dùng Sprite Mode Multiple và Sprite Editor
   data provider với capability checks theo skill `sprite-editor`. Slice đúng số
   frame, bỏ ô dư cuối; thứ tự từ trên trái theo hàng. Không sửa `.meta` bằng tay;
   dùng family spec ở [M8 art bible](../presentation/art-direction.md#m8-vertical-art-slice-locked); family chưa review vẫn giữ TBD.
6. Preview clip/state trong Game View và Play Mode ở gameplay scale, kiểm tra
   Console theo canonical workflow. Chưa chạy thì ghi rõ chưa verified; không tự
   thay production art. Không chạy Android/release trong giai đoạn này.

### Recipe Comfy workflow

JSON được emit bởi Comfy MCP là seed API-format, không phải bằng chứng graph chạy
được. Prompt mẫu chưa style-approved; seed text-to-image không có reference-image
node, nên cần graph/schema hỗ trợ image input khi task phải bám ảnh tham chiếu.

`server_info` → xác nhận runtime/target → `auth_status` cho graph API → inspect
`list_workflow_slots`/`list_workflow_notes` → chỉnh `set_workflow_slot` →
`validate_workflow`. Chỉ chạy khi dependencies đủ và chi phí đã được duyệt.
`run_workflow(wait=False)` → poll `job` → `fetch_outputs` → normalize/review.
Nếu không có local runtime, `partner_generate` là đường hosted riêng; không submit
JSON vào server chưa cài/chưa chạy và không coi `auth_login` là cài ComfyUI.

Đường hosted không yêu cầu ComfyUI local: `auth_status` → `list_partner_models`
→ `partner_model_schema` → xác nhận chi phí cho generation cụ thể →
`partner_generate` → normalize/review. Schema `nano-banana` đã kiểm tra hỗ trợ
`prompt` bắt buộc và `image` là array optional (local paths, URLs hoặc data URIs)
để đưa approved anchors vào request. Kiểm tra lại schema trước khi dùng; không
truyền tham số `image` của hosted API thẳng vào graph JSON vì hai schema khác nhau.

### Toolchain setup ngày 2026-10-20

Checkpoint setup lịch sử, trước M8 slice bên dưới. Khi setup chỉ test bằng ảnh kỹ thuật/synthetic,
không duyệt visual family. Native resolution, PPU, palette, outline, kích thước
sprite, số frame/FPS/animation rules vẫn [TBD]; chốt bằng Unity vertical art slice
ở M8. **512×512 chỉ là canvas kiểm tra SD1.5, không phải sprite size.**

Không dùng Figma/Canva trong pipeline này. Không Android hoặc release ở giai đoạn này.

#### Installed tools và exact paths

Các path dưới thuộc máy Linux của owner, không phải portable dependency trong Git.
Mở agent tại `/home/nguyenvanrin/PersonalProjects/starshift` để nạp project PixelLab
MCP config; chat mở từ repo khác có thể không expose native PixelLab tools.

| Tool | Version / path / nhiệm vụ |
|---|---|
| PixelLab MCP | Server `0.2.0`, endpoint `https://api.pixellab.ai/mcp`; 103 tools; project `/home/nguyenvanrin/PersonalProjects/starshift/.codex/config.toml`; token `/home/nguyenvanrin/.config/starshift/pixellab-token` mode `0600` |
| ImageGen | Native `image_gen.imagegen` service tool; không có local binary/version pin; generate/edit/reference raster, alpha theo request |
| Comfy hosted | `comfy-cli 1.22.0`, `/home/nguyenvanrin/.local/bin/comfy`; MCP `comfy-mcp 0.10.0`; OAuth authenticated, hosted catalog/schema `nano-banana` verified; balance **$0** |
| Local ComfyUI | `0.42.1`, commit `63be2f9b82ad0dac88a7ceaa28a9cf3154125a18`; `/home/nguyenvanrin/comfy/ComfyUI`; frontend `1.55.18`; localhost `http://127.0.0.1:8188` |
| Local Python / CUDA | `/home/nguyenvanrin/comfy/ComfyUI/.venv/bin/python`, Python `3.12.14`, PyTorch `2.14.0+cu130`, torchvision `0.29.1`; CUDA tensor test PASS |
| Aseprite | `1.3.18.6-dev` source build from official release `v1.3.18.6`; PATH `/home/nguyenvanrin/.local/bin/aseprite` → `/home/nguyenvanrin/.local/opt/starshift-art/aseprite/build/bin/aseprite`; frame/palette/alignment cleanup |
| Krita | `5.3.4.1` (`bd6e8d0`); PATH launcher `/home/nguyenvanrin/.local/bin/krita` → `/home/nguyenvanrin/.local/opt/starshift-art/squashfs-root/AppRun`; official AppImage `/home/nguyenvanrin/.local/opt/starshift-art/krita-5.3.4.1.AppImage` |
| PNG tools | `/usr/bin/python3` = `3.14.7`, Pillow `12.3.0`; `/usr/bin/magick` = ImageMagick `7.1.2-27 (Beta)` Q16-HDRI; deterministic compose/validation and nearest-neighbor conversion |
| Unity | Editor `6000.5.9f1`, project `/home/nguyenvanrin/PersonalProjects/starshift/app`; MCP ready; Sprite Editor package `com.unity.2d.sprite 1.0.0`; CLI `/home/nguyenvanrin/.local/bin/unity` = `1.0.0-beta.13`, latest check PASS |
| Build-only dependencies | CMake `4.4.4`, Ninja package `1.13.2` in `/home/nguyenvanrin/.local/bin`; official Skia `m124-08a5439a6b` in `/home/nguyenvanrin/.local/opt/starshift-art/skia`; GCC C++ support extracted from signed Fedora RPMs under `starshift-art/sysroot`, no root install |

Aseprite source/INSTALL/license are preserved at
`/home/nguyenvanrin/.local/opt/starshift-art/aseprite`; built for local use, no binary
redistribution in Git. Sources: [official release](https://github.com/aseprite/aseprite/releases/tag/v1.3.18.6),
[official build instructions](https://github.com/aseprite/aseprite/blob/main/INSTALL.md),
[official Skia](https://github.com/aseprite/skia/releases/tag/m124-08a5439a6b).
Krita uses the [official KDE download](https://download.kde.org/stable/krita/5.3.4.1/krita-5.3.4.1-x86_64.AppImage),
extracted to avoid a FUSE dependency; launcher sets system Fontconfig paths.

#### Local model và launch commands

Exactly one checkpoint installed:
`/home/nguyenvanrin/comfy/ComfyUI/models/checkpoints/v1-5-pruned-emaonly.safetensors`
(4,265,146,304 bytes).
SHA-256 `6ce0161689b3853acaa03779ec93eafe75a02f4ced659bee03f50797806fa2fa`,
verified against the [model repository LFS object](https://huggingface.co/stable-diffusion-v1-5/stable-diffusion-v1-5/blob/main/v1-5-pruned-emaonly.safetensors).
Source/model license: [model card](https://huggingface.co/stable-diffusion-v1-5/stable-diffusion-v1-5).
No SDXL, FLUX, extra checkpoints, LoRA or custom node packs installed.

```sh
# Foreground server; leave terminal running, Ctrl-C to stop.
starshift-comfy
# Exact launcher: /home/nguyenvanrin/.local/bin/starshift-comfy
# Executes, from /home/nguyenvanrin/comfy/ComfyUI:
.venv/bin/python main.py --listen 127.0.0.1 --port 8188 \
  --lowvram --disable-dynamic-vram --disable-all-custom-nodes \
  --disable-partner-nodes --reserve-vram 0.8 --preview-method none

# In another terminal, only after localhost server is ready:
comfy run --host 127.0.0.1 --port 8188 \
  --workflow /home/nguyenvanrin/PersonalProjects/starshift/tools/assets/workflows/comfy-sd15-smoke.json --wait

aseprite /path/source.aseprite
krita /path/background.kra
python3 tools/assets/spritesheet.py --help
```

Minimal tested graph: `/home/nguyenvanrin/PersonalProjects/starshift/tools/assets/workflows/comfy-sd15-smoke.json`
([repo JSON](../../tools/assets/workflows/comfy-sd15-smoke.json)). Seven core nodes,
fixed seed 42, Euler/normal, CFG 7, 20 steps, batch 1, 512×512 technical sphere.
Graph validation returned `valid=true`, zero errors/warnings, no partner nodes,
`spends_credits=false`. On this GPU, server confirmed `LOW_VRAM`, CLIP on CPU and
partial UNet offload. Baseline completed in **11.64 s**; this is one smoke result,
not a production latency guarantee. Output is opaque RGB; sprites still need alpha
cleanup. Canvas smoke không đặt native dimensions; baseline sau review nằm ở art bible.

Server was left running idle on localhost at checkpoint, with models unloaded and
~3.2 GiB VRAM free reported afterward. Check `server_info` before starting another
copy. For MCP: `server_info` → `system_stats` → `validate_workflow` →
`run_workflow(wait=false)` → `job(status/wait)` → `fetch_outputs` →
`free_memory(unload_models=true, free_memory=true)` after the batch. VRAM is shared
with Unity; keep batch 1 and unload local models before gameplay review.

#### Verification checkpoint

PASS:
- PixelLab initialize/list/get_balance; 40 trial generations, $0 paid credits and
  daily bonus 5 reported at check. No PixelLab generation consumed in setup.
- ImageGen actual technical generation: PNG RGBA, 1286×1223, alpha range 0–255.
- Local SD1.5 generation + output fetch + nonblank 512×512 PNG + visual inspection;
  prompt ID `bd4c3865-8afa-49a3-9e04-7010e07a38fc`; model checksum verified.
- Comfy restart/health after official dependencies installed; `uv pip check` PASS
  for all 103 packages. Full environment freeze is preserved with local evidence.
- Aseprite version, GUI launch, three-frame `.aseprite` creation, individual PNG
  export and 2-column spritesheet/JSON export; exact RGBA bytes/frame order/grid
  match synthetic fixture, including semitransparent alpha and unused last cell.
- Krita version and GUI launch; ImageMagick PNG32 nearest-neighbor export and alpha.
- Existing `test_spritesheet.py`: **5/5 PASS**; mismatched canvas, opaque input,
  overwrite protection, grid/count/padding rejection and exact frame order tested.
- Unity import/slice/pivot/filter/PPU read-back using synthetic 16×16 frames;
  3 named sprites in 2×2 grid, one unused cell omitted. Temporary PPU 16/center/Point
  are fixture-only. Scratch Assets folder deleted through AssetDatabase; no scene
  or production settings changed. Reusable [Unity check snippet](../../tools/assets/verify-unity-art-toolchain.cs)
  uses live Unity 6 namespaces (`UnityEditor.SpriteRect`/`SpriteNameFileIdPair`,
  `UnityEngine.SpriteAlignment`/`GUID`) and mandatory capability/name-file-ID checks.
- Existing placeholder Game View screenshot captured/inspected; Console errors 0,
  compilation failed false. One pre-existing duplicate-MCP-command warning remains.

NOT TESTED / LIMITS:
- Comfy hosted paid generation: balance confirmed $0; auth/catalog/schema only.
- PixelLab generation/animation quality and production consistency; no art M8 batch.
- Gameplay review of new art, final pixel family/animation rules, Android/release.
  A Game View capture of placeholders does not claim new-art runtime verification.

Evidence outside Git/Unity Assets:
`/home/nguyenvanrin/.local/share/starshift/art-toolchain-checks/2026-10-20/` contains
`checks.json`, `imagegen-smoke.png`, `bd4c3865_000.png`, `smoke.aseprite`,
`aseprite-frames/`, `aseprite-sheet.png/json`, synthetic frames/helper sheet,
`unity-game-view.png`, GUI/server logs and `comfy-python-lock.txt`.
Local Comfy output also exists at
`/home/nguyenvanrin/comfy/ComfyUI/output/toolchain-smoke/sd15-test_00001_.png`.
Build logs/source/install files are outside the repo under
`/home/nguyenvanrin/.local/opt/starshift-art/`.

**Ready to begin M8 art production on this machine**, using PixelLab/ImageGen and
tested local fallback. Comfy hosted remains unavailable for paid work until funded.
Setup dừng ở mốc này; kết quả M8 sau đó được ghi ở section M8 bên dưới.

### Fallback order và quota

1. PixelLab cho pixel-specific generation/animation; ImageGen cho concept/reference,
   raster generation/edit. Chọn theo công việc, dùng cùng approved anchors.
2. PixelLab hết quota/lỗi service: chuyển ImageGen, giữ source/seed/job IDs và review
   lại consistency; ImageGen không thay hoàn toàn animation model của PixelLab.
3. Comfy hosted (`nano-banana`) chỉ dùng khi auth và balance đủ; checkpoint hiện tại
   balance **$0**, vì vậy bỏ qua route này cho đến khi owner nạp credit. Không tự mua.
4. Local ComfyUI SD1.5 là fallback độc lập cloud quota. Chỉ dùng baseline đã cài;
   không tự tải SDXL/FLUX/model packs/LoRA. Local output không tự có alpha hoặc
   pixel-perfect edges; cleanup bằng Aseprite/Krita rồi kiểm tra lại.
5. Khi AI không đạt consistency: sửa thủ công trong Aseprite/Krita; không âm thầm
   đổi family hoặc tài khoản. Không có auto account rotation. Đổi token chỉ bằng
   tài khoản có quyền sử dụng, giữ file secret ngoài Git và reload client nếu cần.

### Exact asset recipe cho agent tiếp theo

1. Đọc art direction và asset cần làm. Trong M8 vertical slice, quyết định và ghi
   canvas/native size, palette/outline, PPU, pivot, filter/compression, frame order
   và FPS theo family; setup này không đặt baseline thay các quyết định đó.
2. Generate/reference qua primary route; lưu source, prompt, model/tool version,
   seed/job ID, anchor và provenance/license. Local chạy workflow mẫu bên dưới
   với prompt thay theo task; smoke prompt không dùng làm production prompt.
3. Pixel conversion nếu cần: PixelLab `image_to_pixelart`/correction/reduce colors;
   hoặc ImageMagick nearest-neighbor về **canvas đã chọn ở M8**, rồi Aseprite
   chỉnh pixel, palette và alpha thủ công. Không coi downscale tự động là art hoàn tất.
4. Aseprite: import frames theo thứ tự `frame_00.png`, `frame_01.png`, …; giữ fixed
   canvas/padding, chỉnh palette/outline và alignment trên toàn animation, preview
   loop/timing. Không trim/center từng frame. Lưu `.aseprite`; dùng Krita `.kra`
   khi cần compositing/background/mask/paint-over, export PNG về cùng review gate.
5. Export từng frame RGBA (không scale/trim). Ví dụ từ source đã cleanup:
   `aseprite --batch source.aseprite --save-as 'frames/frame_{frame00}.png'`.
   Parent folder phải có trước; tránh overwrite source/output đã có.
6. Compose theo explicit frame order, không dựa vào glob chưa sort:

   ```sh
   python3 tools/assets/spritesheet.py pack \
     /work/frame_00.png /work/frame_01.png /work/frame_02.png \
     --columns 2 --output /work/asset_sheet.png
   python3 tools/assets/spritesheet.py check /work/asset_sheet.png \
     --cell FRAME_WIDTH FRAME_HEIGHT --frames 3
   ```

   `FRAME_WIDTH/HEIGHT`, columns và 3 frames chỉ minh họa; thay bằng batch thật.
   Output là PNG RGBA, order trái→phải/trên→dưới. Check grid/count/trailing empty
   cells; test byte-for-byte frame order. Kiểm thêm visible content từng frame,
   alpha fringe trên nền sáng/tối, không checkerboard bake-in. Background opaque
   có chủ đích không đi qua sprite-transparency validator.
7. Unity Edit Mode: confirm đúng `app/`, Console không compile error; import runtime
   PNG vào folder art phù hợp. Single sprite dùng Single; sheet dùng Multiple.
   Reuse skill `sprite-editor`, capability checks và name/file-ID provider; không
   sửa `.meta` tay. Rect nguồn: `x=(i%columns)*W`,
   `y=sheetHeight-(i/columns+1)*H` (integer division), đúng `frameCount`, bỏ ô dư.
   Đặt pivot/filter/PPU/compression theo quyết định family ở M8, giữ existing GUID.
8. Tạo/wire animation theo named frames và timing đã duyệt; không dựa vào thứ tự
   subasset do AssetDatabase trả. Review Game View tại actual gameplay scale,
   Play Mode kiểm frame counter tiến, movement/rotation/loop, hit/death states,
   silhouette với nền sáng/tối, alpha/pivot/jitter và Console. Iterate source →
   export → validate → reimport giữ GUID. Không build/release hoặc Android.
9. Ghi evidence và asset lifecycle. Source preview không thay Unity runtime review;
   chỉ lock family sau vertical slice, chưa production cả campaign.

## Sprite sheet contract

Khi cần sprite animation theo [animation strategy](../presentation/audio-vfx.md#animation-philosophy),
sheet phải có transparent background, fixed grid, cùng canvas/frame, object center,
object scale và orientation; không text, border hoặc perspective drift; lighting nhất quán.

Exact frame count [TUNABLE]; các ví dụ sau chỉ là guideline:

| Asset | Frame-count guideline |
|---|---|
| Engine, nếu cần sprite animation | Khoảng 3–4 |
| Small explosion | Khoảng 6–8 |
| Portal/rift | Khoảng 8–12 |

## Asset lifecycle và Definition of Done

`NEEDED` → `GENERATED` → `REVIEW` → `NORMALIZED` → `IMPORTED`
→ `IN-GAME VERIFIED` → `DONE`.

Với asset không generate, `GENERATED` được hiểu là created/acquired.
`REVIEW` là review source ban đầu để xác định sửa gì; sau `NORMALIZED` kiểm tra lại
source trước import theo luồng normalization. Không đánh đồng review source với
`IN-GAME VERIFIED`; nếu không đạt, sửa source/normalization/import rồi review lại.

PNG/model/audio tồn tại chưa đủ để DONE. Visual asset chỉ DONE khi đúng style,
import hợp lệ, đúng scale, readable trong gameplay và không phá visual hierarchy.
Batch phải qua Unity Game View và Play Mode review trước khi lock family; preview
source không thay Unity gameplay-scale/runtime review.

## Art review environment `[PROVISIONAL]`

Cần một scene/dev environment kiểu `ArtLab` hoặc `PresentationLab` để review tại
actual gameplay scale; tên và cách tổ chức chưa khóa. So sánh player/enemy/boss,
damage states, projectile, pickup, VFX và backgrounds trong cùng ngữ cảnh.
Dev controls có thể hỗ trợ Normal, Damaged, Critical, Hit, Fire, Death, region background
và actual gameplay scale. Đây là workflow requirement đề xuất, chưa có scene được tạo.
Tên/cấu trúc môi trường review là provisional; yêu cầu kiểm tra trong Unity ở gameplay
scale trước khi asset hoàn tất vẫn bắt buộc, dù dùng scene/dev environment nào.

## Production batching

Các batch dưới là planning guideline, không thay
[exact level/encounter schedule](../content/levels-encounters.md).

| Batch | Phạm vi ví dụ |
|---|---|
| Vertical Slice | Player, Pulse projectile, Scout, simple asteroid, small explosion, simple background, basic HUD icon |
| Region A | Raider, Bomber, Elite presentation, Golden asteroid, Supply Drone, Supply Crate, Boss 1, Region A environment |
| Region B | Sniper, Interceptor, Carrier, Boss 2, Region B environment |
| Region C | Late-game/final presentation assets, final boss, Rift effects, Region C environment |

Vertical Slice batch dùng để kiểm chứng family sau style approval, trước khi lock.
Các batch theo region tiếp nối sau Unity runtime review và lock visual family trong canonical
workflow; không production cả campaign một lượt. Review batch mới cùng anchors để
phát hiện style drift.

## Asset source tracking và licensing

Mỗi asset cần có record tối thiểu; chưa yêu cầu database hoặc tool riêng:

| Trường | Nội dung |
|---|---|
| Asset ID | ID ổn định để đối chiếu source và output |
| Category / subject / variant | Loại asset, đối tượng và biến thể |
| Source | Nguồn gốc, tác giả hoặc nơi acquire |
| Generation method | Cách tạo/acquire, tool/script nếu có |
| Reference/anchor | Mẫu đã duyệt dùng để đối chiếu |
| Unity destination | Đường dẫn runtime output |
| License | Quyền sử dụng và điều kiện áp dụng |
| Attribution | Credit khi được yêu cầu |
| Status | Trạng thái theo asset lifecycle |

Prompt AI quan trọng SHOULD lưu cùng generation note/parameters đủ để regenerate.
Free asset/music cần license rõ, attribution được lưu và style phù hợp; free không
mặc định miễn credit. Không cần commit raw intermediate khổng lồ; giữ source hữu ích
và runtime export có mục đích rõ. Audio generation/import chi tiết ở
[Python SFX và BGM import](#python-sfx-và-bgm-import).

## Image naming examples

```text
player_ship_idle.png
player_ship_thrust_sheet.png

enemy_scout_normal.png
enemy_scout_damaged.png
enemy_raider_normal.png

boss_vanguard_normal.png
boss_vanguard_damaged.png

vfx_explosion_small_sheet.png
vfx_rift_loop_sheet.png

pickup_shield.png
pickup_weapon_core.png
currency_stellar_shard.png
```

## Audio naming examples

```text
sfx_player_laser.wav
sfx_player_plasma.wav
sfx_player_missile.wav
sfx_enemy_hit.wav
sfx_player_death.wav
sfx_supply_crate.wav
sfx_warp.wav
```

## Spritesheet import và slicing

Trước import, kiểm tra grid đều, canvas/frame size, frame order, alpha, centering và
scale. Dùng Unity Sprite Editor/slicing tool khi cần; không hand-edit GUID/references.
M8 reviewed families dùng spec và native importer recipe bên dưới. Family mới vẫn
phải qua camera/readability review trước khi chốt dimensions/pivot; không áp PPU
của world sprites cho UI/background một cách máy móc.
Unity tự tạo `.meta`; commit cùng asset và giữ nguyên khi move/rename.

## Sources, exports và naming

Offline source/scripts/prompts hữu ích ở `tools/assets/` hoặc `tools/audio/`;
Unity chỉ nhận output cần runtime trong `app/Assets/StarShift/Art/`, `Audio/`, `VFX/`,
UI hoặc folder phù hợp. M8 archive dùng `tools/assets/m8/{sources,cleaned,frames}`;
không commit mọi intermediate lớn. Không đổi file user hiện có để ép namespace.
Technical asset filenames English; các ví dụ ở trên là convention baseline.
Scene naming/topology thuộc [architecture](../systems/architecture.md), không sở hữu ở đây.

## Python SFX và BGM import

Python synth trong `tools/audio/`, giữ script/parameter/seed khi cần reproduce;
export file như ví dụ WAV rồi import Audio folder. Danh mục sound ở audio/VFX owner.
BGM có thể free licensed/original, không bắt buộc synth Python.
Kiểm tra license/attribution, loop seam, loudness/clipping và mobile memory;
exact sample rate, compression, load mode và streaming policy [TBD] theo clip/device.
Chỉ viết generator hoặc generate output khi có task asset/audio tương ứng.

## Mobile import concerns

Giữ transparent padding hợp lý, kiểm tra alpha edges/atlas bleeding, texture size,
compression/format và mip/filter phù hợp art style. Trong giai đoạn hiện tại, test sprite nhỏ trong Unity Game View ở aspect ratio
mục tiêu, không chỉ ảnh full-size; device/mobile profiling để sau giai đoạn này. Review transparent overdraw, excessive particles, giant
additive sprite, glow everywhere, trail count và sprite resolution quá lớn dưới
mobile constraints. Source đẹp nhưng unreadable hoặc quá nặng trong gameplay thì
không đạt. Budget texture/audio/particle và device baseline [TBD];
profile trước khi chốt quality setting hoặc thêm package.

## Asset review checklist

- Nguồn/license/attribution được ghi lại; không mặc định free nghĩa là không cần credit.
- Kết quả khớp silhouette/palette/lighting của art owner.
- Grid, frame order, canvas, alpha, pivot và scale nhất quán; không text/border lạc.
- Animation không drift tâm/perspective; ưu tiên Transform/Particle khi phù hợp.
- Import nhìn rõ ở camera scale và mobile aspect ratio; không overdraw/glow quá mức.
- Audio không clipping/spam, loop hợp lệ; category/volume hierarchy phù hợp.
- Giữ source/prompt/parameters quan trọng để regenerate; output và `.meta` đi cùng.
- Không thêm asset, generator hoặc dependency ngoài task hiện tại.

## M3 mission approach placeholder source

`Art/Placeholders/planet-disc-placeholder.png` là procedural neutral disc do
`tools/unity/ConfigureEncounterFoundation.cs` tạo bằng Unity Texture2D/TextureImporter.
Không dùng external art/license; stars dùng cùng sprite và deterministic authoring
seed trong script. Script chỉ tạo khi asset chưa tồn tại, giữ GUID/importer sau đó.
Đây là test presentation theo [placeholder-first](#placeholder-first), không lock
visual family hoặc tên hành tinh. Choreography thuộc
[audio/VFX](../presentation/audio-vfx.md#level-intro-warning-và-dynamic-audio).


## M5 placeholder authoring

`tools/unity/ConfigureRegionA.cs` chạy qua Unity Pipeline trong saved Gameplay scene,
Edit Mode. BatchAB author Mission 01–03/preboss; BatchC author Vanguard/modules,
Mission 04 và Boss HUD/catalog. Builders tạo asset bằng Unity API, giữ GUID của asset
đã có và không overwrite later tuning khi rerun. Không chạy baseline M4 builder
trên production scene để reset content. New sprite renderers reuse placeholder
sprites; không generate final raster art hoặc texture batch.

M4 fixture giữ level/encounter/reward assets riêng; production definitions trong
Data/RegionA. New `.meta` do Unity tạo, phải đi cùng assets. Device visual-family
approval chưa có, không coi placeholder thành locked/final art.


`ConfigurePacingRefinement.Configure` cập nhật production choreography/stream/copy
qua Unity API; `ConfigureTravel` wire PlanetJourney trên saved Gameplay scene. Cả hai
chạy Edit Mode qua Pipeline, giữ GUID; không chạy baseline builder để reset tuning.
`Report` chỉ đọc config và ghi snapshot evidence, không author asset hoặc save gameplay.


`ConfigureHumanFeel` là follow-up authoring cho pass thứ hai: Formations,
DurabilityAndRewards, Asteroids, Vanguard và VisualRefinements chạy Edit Mode qua
Unity Pipeline. Dùng shared FormationPlan assets, giữ GUID và sprite sources cũ;
không rerun ConfigureRegionA/ConfigurePacingRefinement để reset current tuning.
Vanguard ghép existing placeholder primitives; không generate raster art hoặc đổi
sprite importer/slicing metadata. New assets/scripts có Unity-generated `.meta`.

## Region A authored data layout

Production data thuộc `app/Assets/StarShift/Data/RegionA/`, chia theo trách nhiệm:

| Folder | Nội dung |
|---|---|
| `Missions` | Mission01–04 LevelDefinition |
| `Encounters` | Encounter schedule/completion config |
| `Cameras` | Encounter camera profiles |
| `Flights` | FlightSequence đang dùng production |
| `Formations` | FormationPlan slots |
| `Hazards` | HazardStreamDefinition |
| `Enemies` | Common enemy HP/modifier definitions |
| `Weapons` | Common enemy fire profiles |
| `Rewards` | Common/Elite progression drop tables |
| `Boss` | Vanguard health, flight, phases, attacks, camera, beam material và defeat rewards |

System fixtures tách tại `Data/Fixtures/Levels`, `Encounters`, `RegionAFlights`.
Ba seed flights TopHold/ArcLeft/ArcRight còn được verifiers dùng, giữ tại fixture
folder. Sáu generated flights cũ SineFormation/PincerLeft/PincerRight/DiagonalStorm/
EscortLeft/EscortRight đã bỏ khỏi authored assets: không có GUID dependencies hoặc
runtime/test loader; historical builders có thể tạo lại vào fixture folder.

`RegionAAssetPaths` trong Editor là owner đường dẫn authoring theo type/name.
Builders/verifiers dùng logical flat Region A name qua resolver; serialized gameplay
references vẫn là Unity GUID, runtime không dùng helper Editor này. Move/Delete qua
AssetDatabase; migrate giữ GUID và kiểm tra missing references. Không xóa fixture
hoặc công cụ regression chỉ vì production không load trực tiếp.

## M8 selected sources và production recipe

M8 slice đã chọn PixelLab Pixen player/Scout/heavy anchors, native Aseprite-derived
Raider và boss module, original background/UI/projectiles/Shard/VFX. Không dùng
Comfy hosted ($0), local SD1.5 hoặc thêm tools/model để làm quality target.
[Manifest](../../tools/assets/m8/sources/manifest.json) giữ prompt, model route,
seed, job/asset IDs, accepted/rejected decision, raw PNG SHA-256 và font source/OFL.
AI provenance không phải cam kết độc quyền copyright; distribution review giữ điều
khoản tài khoản/service áp dụng. Không lưu token/raw auth response vào source archive.

### Reproduce và import

1. Đọc [M8 art bible](../presentation/art-direction.md#m8-vertical-art-slice-locked),
   mở cleaned `.aseprite` của family gần nhất. Chọn một silhouette/role cụ thể;
   generation request lấy cùng view/material/palette/light anchor trong manifest.
   Kiểm tra balance; free tier chỉ một active job. Không dùng Pro auto-candidate
   batch khi hạn mức không đủ; failed Pro request ở M8 không tạo asset/không charge.
2. Lưu immutable raw PNG + manifest trước cleanup. Rejected variants giữ ở sources,
   không import runtime. Pixel conversion nếu cần, rồi Aseprite sửa palette, contour,
   frame canvas và alignment; không smooth resize/trim/auto-center từng frame.
3. Reproduce toàn bộ selected cleaned/native exports trên máy hiện tại:

   ```sh
   aseprite --batch \
     --script-param root="$PWD/tools/assets/m8" \
     --script-param out="$PWD/app/Assets/StarShift/Art/M8" \
     --script tools/assets/m8/author.lua
   python3 tools/assets/m8/validate.py
   python3 tools/assets/spritesheet.py check \
     app/Assets/StarShift/Art/M8/explosion-sheet.png --cell 64 64 --frames 8
   ```

   Author script ghi đè **chỉ selected generated exports/cleaned sources**; nếu artist
   chỉnh tay, giữ source đó và export asset đang sửa riêng, không rerun đè thủ công.
   Nó xuất explosion frames; sheet hiện có giữ nguyên. Khi tạo sheet mới: existing
   `spritesheet.py pack` với explicit `explosion_00.png` … `explosion_07.png`,
   `--columns 4 --output NEW.png`. Tool từ chối overwrite, không xóa source để ép chạy.
4. Unity Edit Mode, saved `02_Gameplay`, MCP `run_script` file
   `../tools/unity/ConfigureM8Art.cs`, entry `ConfigureM8Art.Configure`.
   Đây là authoring migration M8 đã áp dụng, không chạy lại để reset art tuning.
   Nó reuse GUID, kiểm Sprite Editor capabilities/name-file-ID pairs và gameplay
   contract snapshot. Không chỉnh prefab/scene YAML hoặc `.meta` bằng tay.
5. Import: Sprite/FullRect, Point, Uncompressed, no mipmaps, NPOT None, Clamp,
   alpha transparency, sRGB; max texture 2048. World/UI/background density theo art
   owner. Explosion Multiple, 8×64×64 rects, grid 4×2, top-left row-major names
   `explosion_00` … `07`; Unity rect Y đổi từ top-left. Center pivot (0.5,0.5),
   không trim; panel/button border 6. Existing sheet frame IDs được giữ khi rerun.
6. Fresh Play Mode cho mỗi width, bắt đầu từ Menu không có active user run. MCP `run_script`
   `../tools/unity/VerifyM8Art.cs`, entry `VerifyM8Art.Begin`, args `[1600]` rồi
   sau PASS stop/start Play Mode rồi args `[2000]`. Không chạy đồng thời. Test chuyển storage trước frame
   wait, disable native pointer/keyboard/gamepad, dùng virtual mouse; actual drag,
   hold fire, moving formation, hostile cue/shot, shard/pool reset, explosion frames
   và release, Pause, L2 heavy, L4 boss module. Player được bảo vệ **chỉ trong QA**.
7. Đọc `.design/m8/runtime-WIDTH.json` và screenshots, inspect ở native Game View.
   Cleanup restore input/damage, production storage và Menu; asserts primary/.bak/
   .invalid save bytes không đổi. Không thao tác Unity giữa pass; nếu user đang chơi
   phải đợi, không dừng/rollback campaign. Lần check trước bị save-byte mismatch khi
   owner đang chơi; tiến trình L4→L5 được giữ nguyên, không reset profile.
8. Review source → real motion/gameplay scale → wide aspect; sửa cùng source, import
   lại phần cần thiết rồi lặp affected review. Có code compile/Console errors thì
   chưa chốt. Không dùng isolated source screenshot để claim complete gameplay QA.

Evidence local `.design/m8/`: `pixel-validation.json`, `font-validation.json`,
`contracts-before/after.json`, `runtime-1600/2000.json`, `screenshots/`,
`DESIGN_REVIEW.md`. Evidence folder ignored như các phase trước; không cần evidence
ngoài repo để reproduce bằng tools/source đã lưu. No Android/release/device claim.

Runtime integration nhỏ: PickupDefinition optional Sprite + pooled pickup reset,
existing death-burst pool nhận 8 frames; không thêm animation/persistence framework.
Enemy/weapon/collider/reward contracts được kiểm equality trước/sau authoring.
Legacy Tank/full boss/other weapons và environment vẫn có proxy ngoài scope;
không coi screenshot heavy/boss module là full roster đã art-complete.

Font OFL/copyright texts đi cùng source; khi phân phối binary phải đóng gói notices
phù hợp, không giả định Unity tự include mọi unreferenced text asset. Chưa tạo release.

Follow-up camera coverage: M8Environment giữ local scale (1,1,1), PPU 21 và world
size cố định; camera Solid Color navy #080f20 lấp phần ngoài patch khi zoom/aspect
mở rộng. Không auto-cover scale, đổi camera framing/playable bounds hoặc generate
background mới; existing starfield/parallax và journey palette vẫn hoạt động.


## M8 preparation checkpoint

Ngày 2026-10-21: giữ selected vertical slice, chưa bulk production.
[Remaining production manifest](../../tools/assets/m8/production-manifest.json) chỉ
bao gồm release L1–L8; kích thước mới là proposed canvas cần gameplay-scale review,
không đổi collider/root scale/balance để vừa ảnh. Region C/Rift không nằm trong batch.

Một animate-from-existing smoke: Stellar Shard 32×40 → PixelLab PixMiniMax 4 frames,
`enhance_prompt=false`, confirmed/charged **1 generation**. API trả input + 4 generated
frames; giữ raw 0–4, cleanup generated 1–4 bằng Aseprite, fixed source silhouette,
binary alpha và shared palette. Seed/job/hash trong
[provenance](../../tools/assets/m8/animation-smoke/provenance.json).
Không tạo candidate/job thứ hai. Balance sau smoke: **34/40**, 6 used, trial, $0 credit.

Reproduce cleanup: `aseprite --batch --script-param root=ABSOLUTE_SMOKE_FOLDER
--script tools/assets/m8/animation-smoke/cleanup.lua`. Giữ editable
`shard-shimmer.aseprite`; export explicit `frames/shimmer_00.png` … `03.png`,
pack 4 columns bằng existing spritesheet tool và check `--cell 32 40 --frames 4`.
Không overwrite sheet đang tồn tại; export phiên bản mới khi artist chỉnh source.

Unity Edit Mode `VerifyM8Animation.Import` dùng native Sprite Editor capability/name-ID
checks, Point/uncompressed, no mipmaps, center pivot, 56.25 PPU; sheet/clip ở
`Assets/StarShift/Art/M8/Smoke/`. Play Mode `VerifyM8Animation.Begin` gắn clip lên
visual của pooled Shard trong isolated real gameplay bằng native Animator/clip, kiểm frame advance và Pause.
Definition fixture được clone, đứng yên để quan sát đủ vòng; không mutate production definition.
Không persist smoke binding hoặc đổi production pickup. Evidence local ở
`.design/m8/animation-smoke/`; preparation summary giữ trong
[checkpoint record](../../tools/assets/m8/preparation-checkpoint.json).

Score carry verification thuộc `VerifyCampaignScore.Core/Begin`; background probe
`VerifyM8Background.Run` kiểm intro parallax, fixed extents và navy rendered corners.
Dùng test storage riêng; restore Menu/input/damage/storage và assert production
primary/backup/invalid bytes không đổi. Không broad regression, Android hoặc build.

## M8 L1–L8 production checkpoint

Production ngày 2026-10-21 dùng **9 Pixen generations**, từ 34 xuống 25/40; không Pro,
không paid credit và không thêm animation jobs. Các motion ngắn được author bằng
Aseprite. [Production jobs](../../tools/assets/m8/sources/production-jobs.json) giữ
request/seed/job/asset IDs và SHA-256 originals; source manifest cũ trỏ tới archive này.
Đây là text-guided generation theo approved anchors, không claim image conditioning.

109 cleaned source/export pairs, gồm baseline được reuse; importer audit có 131 native
textures sau khi bỏ module draft không dùng. [Production manifest](../../tools/assets/m8/production-manifest.json)
là record lifecycle/binding hiện tại; asset IMPORTED có thể còn playback/review debt.
Không đánh dấu toàn roster DONE hoặc fabricated owner acceptance.

Reproduce trên máy đã READY:

```sh
aseprite --batch \
  --script-param root="$PWD/tools/assets/m8" \
  --script-param out="$PWD/app/Assets/StarShift/Art/M8" \
  --script tools/assets/m8/production.lua
python3 tools/assets/m8/validate_production.py
```

Script trên ghi đè production exports/cleaned sources; không rerun trên artist edits
về sau. Export source đã chỉnh riêng. Chờ Aseprite xong trước AssetDatabase refresh
hoặc recompile; refresh chồng export từng gây SourceAssetDB modification-time warnings.
Final synchronous import/read-back và runtime review dùng output đã ổn định.

Unity Edit Mode: `ConfigureM8Production.Import`, rồi các entry `Fleet`, `BossesWorld`,
`CombatRewards`, `FrontendEnvironment` trong
[production authoring script](../../tools/unity/ConfigureM8Production.cs).
Chỉ chạy entry cần author; giữ GUID/name-file IDs, dùng Sprite Editor capability checks.
Không rerun migration slice ConfigureM8Art.Configure để reset tuning.

[Production verifier](../../tools/unity/VerifyM8Production.cs) `Begin(1600)` dùng fresh
Play Mode/Menu, isolated save/input, actual formation/drag/fire, pickup/pool, frame/Pause,
damage, Carrier launch và Warden node/death checks. Không chạy khi owner đang chơi.
[Layout verifier](../../tools/unity/VerifyM8ProductionLayout.cs) `Run(width)` kiểm các
checkpoint 1600 hoặc 2000, effect/reserve và boss layouts; `Edit` kiểm importer/pivot
và critical enemy prefab proxy references. Screenshot capture đợi file mới được ghi
trước khi đổi state. Result screenshot của production verifier là presentation fixture,
không thay cho kiểm tra mọi terminal/save-failure flow.

Evidence local `.design/m8/`: `production-runtime-1600.json`,
`production-layout-1600.json`, `production-layout-2000.json`, `production-pixel-validation.json`, production contract
snapshots và `screenshots/production-*`. Game View smoke/save-byte guards và score-core
regression đã pass; collider/balance snapshot khớp baseline. Final primary recapture đã pass sau khi owner về Menu; các QA passes bảo vệ bytes
của save đang có tại đầu mỗi pass, không rollback tiến trình owner chơi giữa các pass.
Không Android/build/release hoặc full historical suite.

## M8 clean rebuild — 2026-10-21

Owner yêu cầu rebuild presentation L1–L8 từ working tree hiện tại. Các checkpoint
M8 ở trên là lịch sử, không còn chứng minh binding/readiness/visual acceptance hiện tại.
[Rebuild audit](../../tools/assets/m8/rebuild-audit.json) ghi file, GUID và serialized
references; [production manifest](../../tools/assets/m8/production-manifest.json) bắt đầu
lại với runtime verification false. KEEP chỉ là ứng viên salvage, phải review trong Unity.
Không khôi phục art đã xóa hàng loạt; chỉ giữ lại metadata cho asset cùng path cần
tái tạo và vẫn còn references. Baseline chứa cả thay đổi còn dở của pass trước để
giữ lịch sử, không coi chúng đã được review hoặc đạt yêu cầu.

PixelLab là công cụ pixel-art chính; Aseprite cho repaint/cleanup/derivatives và
spritesheet. AI local chỉ tùy việc phù hợp. Fresh trial được xác thực 40/40, $0 credit;
mục tiêu 12–18 fast generations, dừng đánh giá lại ở 20. Không dùng Pro multi-candidate
trừ lý do ngoại lệ. Quota còn lại được suy ra từ jobs/charges, không poll balance liên tục.

Mọi batch dùng isolated save/input, gameplay contract snapshot và Unity motion review
1600×900; kiểm tra quan trọng thêm 2000×900/farthest zoom. Không thay balance, collider,
progression hoặc save semantics. Reorganize final runtime art bằng AssetDatabase sau
integration; migration/tooling cũ chỉ retire khi replacement đã đủ để reproduce.

### Current rebuild checkpoint

Baseline cleanup commit: `4219c667`. Editable sources và thứ tự export nằm ở
[rebuild sources](../../tools/assets/m8/rebuild/README.md). Cặp player A được giữ;
chỉ hai vùng nozzle được repaint để bỏ cửa xả chồng. Mỗi cửa có một plume,
movement trail tách riêng, nhỏ và chỉ phát khi di chuyển.

Tám capsule có icon riêng, kính có cạnh sáng/tối và bốn frame glint nhẹ; Shard giữ
crystal identity. Native slicing đã kiểm tra frame size/order và tham chiếu của 11
PickupDefinition. Explosion player và mảnh giáp độc lập với hull; bảo vệ nhấp nháy
ở ring. Recovery cue của boss là presentation tail, không đổi damage window.

Evidence hiện tại: player survival/protection và UI checks bằng save riêng PASS;
Menu/Hangar/approach/combat/Pause/Vanguard/Warden tại 1600×900 và 2000×900 PASS;
boss Prepare/Lock/Attack/Recovery và freeze khi Pause PASS tại 2000×900. Native
import check: 247 root textures và 13 enemy prefabs PASS. Gameplay/collider snapshot
không đổi; Console không có compile/runtime error ở checkpoint này. Đây là các
check đại diện, không phải bằng chứng mọi asset hoặc mọi encounter đã được duyệt.

Rebuild chưa đóng: còn motion/damage review toàn roster và các VFX còn giữ lại,
review Results/Victory/error states, rồi chuyển runtime art khỏi `Art/M8` bằng
AssetDatabase và retire tooling/source probe không dùng. Không thực hiện cleanup
cuối trước các bước đó. Chưa kiểm tra Android/device performance.
