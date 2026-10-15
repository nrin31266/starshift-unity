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

### Asset Bible

Các invariant dưới đây áp dụng cho mọi nguồn asset; approved reference anchors
là chuẩn đối chiếu cho asset production cùng visual family.

| Invariant | Chuẩn thị giác |
|---|---|
| Shape language | Sleek/angular, stylized hard-surface sci-fi; mechanical/military/alien-tech |
| Silhouette/proportions | Hướng thân và vai trò đọc rõ khi thu nhỏ; không cute/chubby/toy-like/mascot |
| Material/stylization | Hard-surface nhất quán, không pastel-soft; không bắt buộc photorealism |
| Viewing angle | Cùng góc nhìn trong family; tránh perspective drift giữa asset và state |
| Detail density | Mechanical detail hỗ trợ hình khối, không thành noise ở gameplay scale |
| Lighting | Cùng hướng sáng và shading language theo approved anchor |
| Emissive/glow | Có kiểm soát, cùng logic trong family; giữ contrast cho threat |
| Palette | Theo bảng color semantics bên dưới; không dùng màu thay cho silhouette |

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
Boss art SHOULD modular khi phù hợp: hull, cannon trái/phải, shield node, core và
engines có thể là các phần riêng để weapon rotate, node phá riêng, core reveal,
phase change bằng Transform/VFX và staged destruction. Đây là ngôn ngữ thị giác,
không khóa hierarchy GameObject/class và không thay boss mechanics ở
[enemy owner](../content/enemies-bosses.md). Ưu tiên cách này khi nó giảm nhu cầu
spritesheet khổng lồ mà vẫn giữ silhouette.

### Background

Background dùng dark space, nebula vừa phải cùng planet/debris/starfield để tạo
không gian. Giữ contrast cho projectile; background không tranh cùng mức sáng với threat.

### Color semantics `[LOCKED]`

| Nhóm | Màu định hướng |
|---|---|
| Player/friendly base | cyan / electric blue |
| Enemy normal | dark gunmetal + crimson / red emissive |
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

## Damage visual và gameplay-scale readability

Damage states phải phân biệt được bằng silhouette/state accent mà không mất identity
của family. Readability phải được kiểm tra với player, enemy, boss, projectile,
pickup và background cùng hiện diện ở gameplay scale; ảnh source đẹp riêng lẻ chưa đủ.
State/HP/threshold thuộc [enemies/bosses](../content/enemies-bosses.md);
[animation và damage feedback](audio-vfx.md#animation-philosophy) quyết cách thể hiện.

## Glow budget, background và UI

Background giữ dark/value thấp, nebula vừa phải, planet/debris/starfield hỗ trợ scale;
không tranh contrast với projectile. Tránh glow toàn màn hình, transparent overdraw
và trails dày. UI hard-surface sci-fi gọn, icon-first/readable, không neon phủ màn hình.
Feedback tier cụ thể thuộc [audio/VFX](audio-vfx.md).

## Visual family và approved anchors

Trước production hàng loạt, reference pack chốt viewing angle, silhouette,
hard-surface language, material, detail density, lighting direction, glow amount,
color semantics, proportions và mức stylization theo Asset Bible.
Asset production sau phải tham chiếu approved anchors; quy trình tạo/duyệt pack ở
[asset pipeline](../production/asset-pipeline.md#visual-reference-pack).

AI generation phải theo family thay vì prompt từng asset độc lập. Cùng family giữ
approved anchor, material, viewing angle, lighting, detail density và emissive logic.
Ví dụ enemy roster Scout/Raider/Bomber/Sniper/Interceptor/Carrier dùng dark gunmetal,
crimson emissive, angular hull và cùng shading language, nhưng vẫn phân biệt vai trò
bằng silhouette. Không tạo lore/faction mới để giải thích một style khác.
