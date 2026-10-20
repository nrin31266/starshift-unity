# Asset pipeline

## Phạm vi và ownership

Owner của asset sources, generation/import/slicing/naming/licensing/tooling và reproducibility. Visual target do [art direction](../presentation/art-direction.md) quyết; feedback do [audio/VFX](../presentation/audio-vfx.md) quyết.

## Canonical production workflow

`Gameplay placeholder` → `Visual reference pack` → `Style approval`
→ `Vertical-slice asset batch` → `Normalize` → `Import`
→ `Review at gameplay scale` → `Android check` → `Lock visual family`
→ `Produce assets theo từng region`.

Không generate toàn bộ 12 level ngay từ đầu. Style approval duyệt hướng nhìn qua
reference pack; lock visual family chỉ sau khi batch đã qua Unity gameplay-scale
review và Android check. Chưa kiểm tra Android thì ghi rõ chưa hoàn tất gate này.

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
không prompt từng asset độc lập. Prompt template chuẩn cho ship/enemy/boss vẫn [TBD].

## Normalization và source review

AI output không được import thẳng vào production chỉ vì nhìn đẹp.
Luồng chuẩn: `source` → `cleanup if needed` → `normalize` → `review` → `Unity import`.

Normalization kiểm tra transparent background cho sprite tách nền, consistent canvas,
centered visible bounds, consistent scale, correct orientation, không shadow/background
ngoài ý muốn, alpha edge và padding. Background artwork giữ nền có chủ đích theo
art direction. Review source kiểm tra style, alignment và readiness để import;
Unity gameplay-scale review sau import vẫn bắt buộc.
Offline Python utility trong `tools/assets/` có thể được tạo khi có task riêng;
workflow này không yêu cầu tool mới.

### PixelLab usage

PixelLab/MCP là optional production tool cho cleanup frame, alignment/grid correction,
pivot consistency, repaint/correction, resize và edge cleanup.
Source đạt yêu cầu đi thẳng tới normalize; source chưa đạt có thể qua PixelLab cleanup
rồi normalize, review và import. Không bắt mọi asset qua PixelLab.

### Tool routing cho StarShift

Project MCP config nằm ở `../../.codex/config.toml`; chỉ bổ sung PixelLab cho
checkout này. ImageGen/Figma/Canva và Unity/Comfy MCP hiện có được tái sử dụng,
không tạo server trùng hoặc đổi cấu hình global. Tài khoản connector vẫn thuộc
tài khoản Codex, không được cô lập bởi project config; chỉ dùng cho task đang yêu cầu.

| Nhu cầu | Tool/workflow |
|---|---|
| Concept, ship/enemy/boss, background raster | ImageGen, dùng approved anchors; sprite yêu cầu transparent background |
| Pixel art hoặc sửa vùng ảnh | PixelLab image/edit/inpaint, output vẫn phải đạt art direction |
| Object states và sprite animation | PixelLab object/state/animate hoặc animate image |
| HUD/layout và vector source | Figma; Canva cho moodboard/presentation khi task cần |
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
   không tự chốt PPU/filter/compression còn TBD.
6. Preview clip/state ở gameplay scale, kiểm tra Console và Android gate theo
   canonical workflow. Chưa chạy thì ghi rõ chưa verified; không tự thay production art.

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

### Connection evidence ngày 2026-10-20

- PixelLab: MCP initialize/list tools thành công, trả 103 tools; allowlist khớp
  schema live. Sau reload đã gọi native tool `get_balance` thành công, còn 40
  generations trial tại thời điểm kiểm tra. Chưa chạy generation hoặc xác minh output.
- ImageGen: có tool trong phiên; chưa chạy generation ở task cấu hình này.
- Unity MCP: thấy một Editor chạy đúng project StarShift `app/`; chưa import art mới.
- Figma: một kết nối whoami thành công, RIN cần reauthenticate; chưa xác minh quyền
  edit file đích. Canva: một kết nối search thành công, Primary cần reauthenticate;
  search không xác nhận có design StarShift hoặc quyền edit design đích.
- Comfy MCP: OAuth đã đăng nhập, token chưa hết hạn; partner catalog/schema gọi được.
  Hosted `nano-banana` hỗ trợ prompt và reference images. ComfyUI local chưa chạy,
  đường dẫn được báo không tồn tại trên đĩa; GPU 4 GiB VRAM, chưa cài/runtime-test
  local diffusion. Chưa xác minh billing balance hoặc chạy hosted generation/graph JSON.
- Offline PNG tool: Python/Pillow hiện có; synthetic pack/check tests pass.

Đây là snapshot; kiểm tra lại readiness/auth trước generation. Không cần cài thêm
desktop art editor để chạy recipe PNG.

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
Batch còn phải qua Android check trước khi lock family; preview source không thay
Unity gameplay-scale review hoặc Android check.

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
Các batch theo region tiếp nối sau Android check và lock visual family trong canonical
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
[TBD] Frame dimensions, PPU, pivot theo asset family, filter/compression và packing
chỉ chốt sau khi có asset/camera readability test. Pivot nhất quán cho ship/rotation
và boss module; không bịa PPU chuẩn cho toàn project.
Unity tự tạo `.meta`; commit cùng asset và giữ nguyên khi move/rename.

## Sources, exports và naming

Offline source/scripts/prompts hữu ích ở `tools/assets/` hoặc `tools/audio/`;
Unity chỉ nhận output cần runtime trong `app/Assets/StarShift/Art/`, `Audio/`, `VFX/`,
UI hoặc folder phù hợp. Exact source archive convention [TBD] khi có asset thật;
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
compression/format và mip/filter phù hợp art style. Test sprite nhỏ trên mobile,
không chỉ ảnh full-size. Review transparent overdraw, excessive particles, giant
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
