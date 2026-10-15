# Art direction

## Phạm vi và ownership

Owner của visual identity, palette/color semantics, silhouette và region look. Workflow generation/import/slicing thuộc [asset pipeline](../production/asset-pipeline.md); animation/audio choreography thuộc [audio/VFX](audio-vfx.md).

## Art direction tổng quát

### Style `[LOCKED]`

Visual target là **stylized hard-surface sci-fi arcade**. Silhouette và contrast
phải đọc được ở scale mobile; mechanical detail chỉ thêm khi không làm mất hình khối.
Không cần photorealism để thể hiện mass hoặc vật liệu.

### Không được trông như

- cute;
- chubby;
- toy-like;
- mascot mobile cartoon;
- đầu to/thân tròn;
- pastel mềm;
- photorealistic bắt buộc;
- neon/glow phủ toàn màn hình.

### Player ship

Player ship có silhouette gọn, sắc, angular và hướng thân rõ. Mechanical panels
vừa đủ, engine exhaust dễ nhận ra; tránh chi tiết nhỏ làm mất silhouette khi tàu
hiển thị nhỏ trên mobile.

### Enemy

Enemy dùng dark metal, crimson/red emissive và ngôn ngữ mechanical/military/alien-tech.
Silhouette phải phân biệt vai trò; không chỉ recolor cùng hình cho toàn roster.

### Boss

Boss có mass lớn, heavy/industrial/modular. Weak point và weapon modules phải nhìn
được; armor/core states hỗ trợ việc đọc phase và target priority.

### Background

Background dùng dark space, nebula vừa phải cùng planet/debris/starfield để tạo
không gian. Giữ contrast cho projectile; background không tranh cùng mức sáng với threat.

### Color semantics `[LOCKED]`

| Nhóm | Màu định hướng |
|---|---|
| Player/friendly base | cyan / electric blue |
| Enemy normal | crimson / red |
| Enemy special / dangerous telegraph | magenta |
| Reward / currency | amber / gold |
| Shield / defensive | teal |
| Extra life / positive survival | green-white |
| Neutral asteroid | gray / brown |
| Explosive hazard | orange-red |

Hex cụ thể `[TUNABLE]`; bảng hex sẽ thuộc owner này khi có palette được review.

## Region look

Tên region [PROVISIONAL]. Astra Frontier: deep blue/cyan space, asteroid field,
military scouts, readability cho đoạn học. Crimson Nebula: red/crimson nebula,
nguy hiểm hơn nhưng vẫn giữ contrast hostile shots. Titan Ring / Rift Approach:
planet/ring khổng lồ, debris và rift, scale rộng. Exact palette hex [TUNABLE],
chưa có giá trị cố định, không tự chọn palette final khi chưa review.

## Projectile contrast và reward identity

Friendly base cyan/blue; Pulse cyan/blue, Plasma purple, Missile orange theo
[weapon identity](../content/weapons-items-drops.md). Enemy shots phải phân biệt
được với cả ba nhánh, đặc biệt enemy special magenta so với Plasma và hazard
orange-red so với Missile. Cần shape/value/outline và telegraph bổ trợ;
không dựa duy nhất vào hue. Final contrast pass [TBD]/playtest mobile.
Reward amber/gold nghĩa worth collecting; Stellar Shard có icon tinh thể/ngôi sao
đơn giản, không coin cartoon.

## Asteroid, Supply Drone và crate

Normal asteroid gray/brown; golden/resource có crystal vein amber/gold, đọc khác
neutral debris. Supply Drone là unmanned cargo craft hard-surface white/dark metal,
amber navigation/reward accent; không hộp quà cute tự bay.
Crate có cargo/reward silhouette rõ, nhận ra là vật cần pickup.
Exact crate motif vẫn [TBD].

## Damage visual và animation style

Enemy Normal/Damaged/Critical phải đọc bằng sprite/frame, smoke/spark hoặc cracked
emissive theo [enemy contract](../content/enemies-bosses.md). Boss heavy/modular có
armor/core rõ. Hit flash ngắn, không làm mất silhouette.
Entry/exit/orbit/respawn có easing mềm; không cần animation dài hoặc quá nhiều detail
nhỏ. Frame count/import workflow thuộc production owner.

## Glow budget, background và UI

Background giữ dark/value thấp, nebula vừa phải, planet/debris/starfield hỗ trợ scale;
không tranh contrast với projectile. Tránh glow toàn màn hình, transparent overdraw
và trails dày. UI hard-surface sci-fi gọn, icon-first/readable, không neon phủ màn hình.
Feedback tier cụ thể thuộc [audio/VFX](audio-vfx.md).

## Generated asset consistency

Giữ silhouette, lighting, scale, mechanical language và region palette nhất quán giữa
mọi nguồn. Art review quyết kết quả visual; prompt/grid/slicing/license và reproduce
thuộc [asset pipeline](../production/asset-pipeline.md). Không ép mọi asset qua AI
hay PixelLab và không dùng workflow để thay art direction.
