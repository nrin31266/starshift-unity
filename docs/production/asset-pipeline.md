# Asset pipeline

## Phạm vi và ownership

Owner của asset sources, generation/import/slicing/naming/licensing/tooling và reproducibility. Visual target do [art direction](../presentation/art-direction.md) quyết; feedback do [audio/VFX](../presentation/audio-vfx.md) quyết.

## Asset pipeline

### Asset source types

Pipeline cho phép nhiều nguồn asset; chất lượng và độ nhất quán do art direction
quyết, không phụ thuộc asset có được generate hay vẽ tay. Các nguồn được phép:

- AI image generation;
- PixelLab/MCP;
- free assets;
- original drawing;
- Python generation;
- Unity procedural VFX.

### AI sprite rules

Prompt spritesheet SHOULD mô tả layout đủ chính xác để import/slicing ổn định.
[TBD] Prompt template chuẩn cho ship/enemy/boss chưa được chốt. Các constraint cần giữ:

- transparent background;
- fixed grid;
- same canvas per frame;
- same center;
- same scale;
- no text;
- no border;
- no perspective drift;
- consistent lighting;
- hard-surface sci-fi;
- no cute/chubby.

### Frame count philosophy

Không cần animation dài nếu ít frame đã đọc được state. Frame-count examples là
[TUNABLE], không phải yêu cầu generate mọi sheet theo cùng độ dài:

| Asset | Frame-count seed |
|---|---|
| Engine | 3–4 |
| Small explosion | 6–8 |
| Portal/rift | 8–12 |
| Damaged state | Một alternate frame có thể đủ |

### Particle-first rule

Nếu Transform/Particle System làm tốt hơn, không cần generate 12-frame sprite.

### PixelLab usage

PixelLab/MCP là workflow tùy chọn khi AI frames không đều, cần pixel consistency,
cleanup hoặc sửa grid/slicing mismatch. Không bắt buộc mọi asset đi qua PixelLab;
chọn tool khi nó giải quyết lỗi cụ thể của asset.

### Free asset

Free asset cần license rõ, attribution được lưu và style phù hợp. Không dành hàng
giờ tìm một generic effect có thể tạo nhanh bằng Transform/Particle; nguồn miễn phí
không làm giảm yêu cầu art review hoặc tự bỏ nghĩa vụ credit.

### Audio generation

Python synth SFX thuộc `tools/audio/`; import generated output vào Unity Audio
folder, không đưa công cụ offline vào runtime. Naming và BGM import ở các section dưới.

### Reproducibility

Với generation quan trọng, giữ script cùng prompt/generation note và parameters
cần để tái tạo. Không cần version-control mọi intermediate lớn; giữ source hữu ích
và runtime export có mục đích rõ.

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
không chỉ ảnh full-size. Budget texture/audio/particle và device baseline [TBD];
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
