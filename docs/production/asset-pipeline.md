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
