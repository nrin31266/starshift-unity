# Art direction

## Phạm vi và ownership

Owner của visual identity, palette/color semantics, silhouette và region look. Workflow generation/import/slicing thuộc [asset pipeline](../production/asset-pipeline.md); animation/audio choreography thuộc [audio/VFX](audio-vfx.md).

## Art direction tổng quát

### Style `[LOCKED]`

Visual target là **stylized hard-surface sci-fi arcade**. Silhouette và contrast
phải đọc được ở scale mobile; mechanical detail chỉ thêm khi không làm mất hình khối.
Không cần photorealism để thể hiện mass hoặc vật liệu.

Owner clarification 2026-10-21: icon, pickup và VFX ưu tiên màu sáng bóng,
khối đủ dày, vuông bo tròn; dùng mặt sáng/cạnh tối để đọc thể tích, tránh hình dẹp.
Pickup là hộp kính trong suốt hình vuông bo góc, icon lớn bên trong và glint nhẹ
ở mép; không dùng hình lọ có cổ/nắp (owner correction). Áp dụng cho các nhóm này,
không đổi silhouette của cặp tàu A đã được chọn.

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

Enemy dùng ngôn ngữ mechanical/military/alien-tech với **7 armor family màu riêng biệt**;
không kéo mọi family về gray/red. Cohesion đến từ construction, shading, 1px contour
và chuyển động, không từ việc dùng chung một hue. Silhouette phải phân biệt vai trò;
không chỉ recolor cùng hình cho toàn roster. Family swatch và mapping ở
[M8 fleet family](#m8-fleet-family-và-damage-state-provisional) bên dưới; bảng
color semantics `[LOCKED]` vẫn giữ vai trò cho player/reward/shield/hazard.

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
| Player/friendly base | ivory/slate hull, cyan / electric-blue identity |
| Enemy hull | 7 family ramps riêng (xem M8 fleet family); không dùng một gray/red chung |
| Enemy normal shot | coral hoặc pale lemon, outline tối + lõi sáng |
| Enemy special / dangerous telegraph | magenta (chỉ special, không dùng cho shot thường) |
| Reward / currency | amber / gold |
| Shield / defensive | teal |
| Extra life / positive survival | green-white |
| Neutral asteroid | gray / brown |
| Explosive hazard | orange-red |

M8 shared swatches được khóa trong section M8 bên dưới; semantic accents giữ bảng
này. Biến thể region/asset ngoài slice vẫn `[TUNABLE]` qua review.

## Region look

Tên region [PROVISIONAL]. Astra Frontier: deep blue/cyan space, asteroid field,
military scouts, readability cho đoạn học. Crimson Nebula: red/crimson nebula,
nguy hiểm hơn nhưng vẫn giữ contrast hostile shots. Titan Ring / Rift Approach:
planet/ring khổng lồ, debris và rift, scale rộng. M8 dark-blue patch là baseline Astra Frontier; palette final cho các regional
packs khác vẫn [TUNABLE] và cần review riêng.

## Projectile contrast và reward identity

Friendly base cyan/blue; Pulse cyan/blue, Plasma purple, Missile orange theo
[weapon identity](../content/weapons-items-drops.md). Enemy shots phải phân biệt
được với cả ba nhánh: normal hostile dùng coral hoặc pale lemon, special danger
dùng magenta. Cần shape/value/outline và telegraph bổ trợ; không dựa duy nhất vào
hue. Mỗi branch giữ charge/muzzle/hit/trail riêng; silhouette khác nhau để đọc khi
nhiều projectile cùng hiện diện. Final contrast pass [TBD]/playtest mobile.
Reward amber/gold nghĩa worth collecting; Stellar Shard có icon tinh thể/ngôi sao
đơn giản, không coin cartoon.

## Asteroid, Supply Drone và crate

Normal asteroid gray/brown; golden/resource có crystal vein amber/gold, đọc khác
neutral debris. Supply Drone là tàu vận tải cơ khí chở một **hộp quà/cargo crate
lớn** ở trung tâm: crate amber với ribbon kem, đai hazard, kẹp kim loại và khóa năng
lượng teal; không phải hộp quà cute tự bay. Khi trúng, lớp giấy gói rách lộ hộp quà
bên trong; khi nổ, crate bung ra cùng burst riêng thay vì explosion chung.
Crate drop có cargo/reward silhouette rõ, nhận ra là vật cần pickup.
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
Enemy roster dùng 7 family ramp riêng nhưng cùng angular hull, cùng hướng sáng
upper-left và cùng shading language; phân biệt vai trò bằng silhouette, không bằng
cách recolor toàn roster về một hue. Không tạo lore/faction mới để giải thích một
style khác.

## M8 fleet family và damage state `[PROVISIONAL]`

Quyết định Batch 3: bỏ baseline crimson chung cho mọi enemy. Mỗi family có ramp
riêng, accent emissive riêng; cohesion giữ bằng construction, 1px navy contour,
3–4 value bands, light upper-left và chuyển động. Không family nào được trôi về
gray/red đơn điệu. Ramp/accent/glow của từng family nằm trong
[author script](../../tools/assets/m8/rebuild/fleet.lua); runtime sprite ở
các folder theo loại trong `app/Assets/StarShift/Art/`.

| Family | Armor ramp | Accent / emissive | Native canvas |
|---|---|---|---|
| Scout | steel xanh lạnh (navy → pale steel) | cyan sáng | 80×64 |
| Raider | navy xanh đêm | coral | 80×80 |
| Bomber | bronze ấm | orange | 128×112 |
| Bulwark | olive | cream/amber | 112×96 |
| Sniper | violet/indigo | violet sáng | 80×112 |
| Interceptor | graphite | lime | 64×80 |
| Carrier | teal | teal sáng + blue-gray | 160×128 |
| Supply Drone | ivory/khung thép | amber | 112×80 |
| Vanguard | steel-tím riêng | crimson + reactor vàng | modules 96×96 |
| Warden | indigo riêng | teal energy | modules 96×96 |

Enemy có engine/nozzle và muzzle hợp lý theo cấu trúc tàu; không bắt gắn plume
phía sau (owner clarification rebuild). Carrier bay mở theo launch thật. Player
giữ hai plume cố định theo chassis, tách khỏi một vệt di chuyển nhỏ theo lịch sử vị trí.

### Damage và destruction

Damage state là repaint thật, không tint: mảng giáp bị cắt khỏi silhouette theo
đa giác răng cưa, khoang lộ ra có depth band, dầm ngang, conduit và highlight mép
trên/trái. Damaged mở một vùng; Critical mở thêm vùng thứ hai, có sparks/smoke tại
chỗ hỏng. Không mất quá nửa silhouette; family identity phải còn đọc được.
Asteroid mất khối, nứt sâu và bong đá; resource asteroid giữ crystal identity và
lộ cụm tinh thể amber ở Critical thay vì lỗ trống.
Heavy/boss dùng staged destruction: nổ cục bộ theo module, mảnh văng và khói, giữ
respawn gate và death timing hiện có. Player death có flash, hull breakup và debris
độc lập với hull bị ẩn.

### Hit type và impact

Projectile gửi point/normal/type tới presentation; damage/splash không đổi.
Shield block dùng vòng teal/lục giác, hull dùng sparks vàng-cam, module dùng vòng
vuông + tia. Impact hiện tại mép cánh/module bị trúng, không luôn ở tâm enemy.
Owner của HP/damage state vẫn là [enemies/bosses](../content/enemies-bosses.md).



## M6 gameplay proxy readability `[PROVISIONAL]`

Tiếp tục native placeholder sprites, không coi là approved production family.
Interceptor dùng pointed hull hướng strike và engine plume; Sniper có long barrel;
Carrier có large body/hangar rail. Hostile hull/accent giữ gunmetal/crimson/magenta,
không dùng friendly cyan để phân biệt minion. Warden nodes có accent khác nhau nhưng
shield network giữ teal defensive cue; active hostile beam vẫn magenta.

Scene Player bỏ tint đỏ legacy, trở về cyan tint của prefab để tách friendly khỏi
Interceptor/hostile silhouettes. Hai asteroid props không collider trong EnvironmentPlaceholders được giảm contrast
để không nhầm với corridor hazards thật. Formation silhouettes/role placement ở
[level owner](../content/levels-encounters.md#m6-region-b-authored-content-provisional);
HP/physics footprints ở enemy owner. Chưa bắt đầu final art/audio/frontend pass.


### Playable area hint `[PROVISIONAL]`

Bình thường không tô nền hoặc vẽ khung giới hạn. Khi người chơi cố kéo vượt cạnh
nội bộ rồi tiếp tục giữ khoảng .2 s, hiện một đoạn báo mảnh ngay gần vị trí chạm cạnh.
Đoạn dài .14 viewport dọc cạnh, dày 2 pixel Game View, màu xanh xám RGBA
(.45,.72,.80,.24) với alpha pulse .45–1 và fade .12 s `[TUNABLE]`.
Nhả tay hoặc kéo vào trong thì tắt; giữ bắn mà chưa đẩy vượt cạnh không hiện.
Không báo ở mép ngoài màn hình hoặc lúc camera đang transition. Animation dùng
scaled time nên Pause đóng băng. `PlayableAreaPresentation` chỉ đọc camera region
và telemetry từ movement; không quyết bounds/damage. Art thật có thể thay visual
này mà giữ nguyên cơ chế giới hạn. Không phủ HUD hoặc chia nền thành hai mảng màu.

## M8 vertical art slice `[LOCKED]`

Baseline được chọn sau review source và Play Mode 1600×900 / 2000×900 ngày
2026-10-20: **modern medium-detail pixel art**, overhead hard-surface, player ivory
với cyan cockpit, hostile gunmetal/crimson, nền navy thoáng. Đây là style lock cho
batch tiếp theo, không phải owner acceptance cho toàn roster hoặc full game polish.
Selected anchors/source provenance ở
[manifest](../../tools/assets/m8/sources/manifest.json); cleaned `.aseprite` nằm trong
`tools/assets/m8/cleaned/`, exports runtime trong các folder theo loại trong `app/Assets/StarShift/Art/`.

### Pixel density và native canvas

World sprites dùng **56.25 PPU**. Camera combat orthographic size 8 cho view cao
16 units: `900 / 16 / 56.25 = 1` screen pixel/native texel ở cả hai target aspects.
Giữ nguyên camera, formation, collider, HP và weapon cadence; không phóng art lên
512px hoặc đổi root gameplay scale để vừa ảnh. Visual child của heavy/Raider bù
root scale để hull có net scale 1. Menu player stage dùng integer 4×.

| Family | Native canvas / baseline |
|---|---|
| Player | 64×80, nose up, center pivot |
| Scout | 80×64, nose down, center pivot |
| Raider derivative | 80×80, dài cánh/prongs; reuse Scout material |
| Heavy/Bomber | 128×112, broad armor và separate pods |
| Boss cannon module | 96×96, paired barrels, center pivot; full boss chưa final |
| Friendly Pulse | 12×32; thin cyan/white needle |
| Hostile bolt / charge cue | 20×24 diamond / 28×28 magenta brackets |
| Stellar Shard | 32×40, faceted amber crystal |
| HUD life icon | 24×28; icon texel density theo UI, PPU 100 |
| UI panel / button | 32×32, 6px nine-slice border, PPU 100 |
| Explosion | 64×64 frames; timing ở audio/VFX owner |
| Environment patch | 768×336, background PPU 21, static far layer |

Đây là reviewed anchors cho các family đã có; size của archetype khác phải đối chiếu
silhouette ở camera thật, không scale đồng loạt theo một square canvas. Bounds là
canvas gồm transparent padding, không phải hitbox. Runtime rotation/subpixel movement
vẫn theo gameplay hiện có; Point filtering giữ cạnh sắc nhưng không phải cam kết
pixel-grid-perfect ở mọi pose/zoom/cinematic hay mọi độ phân giải.

### Palette, outline và vật liệu

Shared 32-swatch palette, không dithering hoặc alpha antialias ở source:

```text
080f20 111d32 1c2e46 2d4660 46617a 6e899b a6bfcb e5f4f2
0b3948 126578 1b9bb2 41d2de a0f1ec 271b32 50273d 87354c
bb4057 ed6470 ffb69c 3b2836 745047 ad704b e8aa58 ffe0a0
282846 505073 8484ac c0b8d1 464025 7e7745 bab773 f1edb3
```

Mỗi sprite chỉ dùng subset: player 16 opaque colors, Scout 14, heavy 15. Selective
1px dark navy contour, 2px ở occlusion/deep seams khi cần; không viền đen dày quanh
mọi panel. Metal dùng 3–4 value bands, light upper-left, highlight ở bevel và mặt
trên; panel lớn trước, clusters nhỏ sau. Enemy tránh ivory-white diện lớn của player.
Cyan/crimson emissive là 1–3px accent có bright core, không soft bloom halo. Friendly
needle, hostile diamond và gold faceted pickup khác shape/value, không chỉ hue.
Hit/damage tint hiện có giữ nguyên gameplay timing; damaged/critical repaint riêng
chưa sản xuất, không reuse old proxy silhouette qua sprite override.

### Environment và UI typography

Nền chỉ 5 dark swatches, 87 sparse stars trong patch, stepped nebula ở góc và planet
rim ở upper-right; giữa view trống. Far patch giữ fixed world scale dưới existing moving starfield; camera Solid Color
navy #080f20 lấp vùng ngoài patch khi zoom/aspect mở rộng, không auto-scale art theo camera.
parallax/movement giữ choreography cũ. Không rải điểm trắng/đỏ sáng giống đạn. Các
planet/hazard/transition placeholder khác chưa được duyệt thành final environment.

UI dùng chamfered navy panels, teal button plate, cyan upper-left border accent.
Nine-slice giữ góc và viền; highlight sáng hơn, pressed tối hơn, disabled giảm alpha.
HUD icon ít detail hơn hull, giữ silhouette. Score/Shard top row có 10px margin ở
900px-height view sau sửa clipping; navigation M7 vẫn là baseline.

**VT323 Regular** cho title/ship display; **Chakra Petch Medium/SemiBold** cho body,
HUD và buttons. Cả ba font đi kèm SIL OFL 1.1, cover toàn bộ Vietnamese precomposed
U+1EA0–U+1EF9 cùng Ă/Đ/Ơ/Ư và lowercase trong Unity `Font.HasCharacter` check.
Giữ tiếng Việt; không bake text vào PNG, không fake-bold display hoặc auto shrink
để che overflow. Glyph coverage không thay cho proofreading/composition mọi màn.

### Review scope và phần chưa khóa

Real L1 formation là primary playable anchor; L2 heavy encounter và L4 boss dùng
đường chạy production để đối chiếu mass/module. Snapshot collider/physics/balance
trước/sau authoring giống hệt. Screenshot và runtime assertions nằm ở local
`.design/m8/` (ignored evidence theo convention hiện tại); recipe ở pipeline owner.

Rejected: generated boss bị isometric/full-ship drift; generated background có quá
nhiều điểm sáng/đỏ giống projectile. Thay bằng original Aseprite work bám anchors,
không tốn thêm generation. Chỉ 5 Pixen outputs; không tạo dozens of variants.

`[TBD]`: full boss hull/remaining modules, Tank/other roster, damage repaint, shield
và Plasma/Missile contrast in final-art mix, remaining UI screens, regional background
packs, full audio, device readability/performance. M8 slice không tự duyệt các phần
này và không mở bulk batch tự động. Không Android/build/release trong pass này.

## M8 L1–L8 production pass `[PROVISIONAL]`

Production ngày 2026-10-21 mở rộng slice theo brief của owner; không mở Region C.
Giữ medium-detail pixel art, shared palette, light upper-left, Point filtering và
56.25 world PPU. Crimson vẫn là hostile accent chung; bronze/slate của Bulwark,
violet của Sniper/Carrier/Warden và violet sensor của Interceptor phân biệt role.
Friendly ivory/cyan và reward amber giữ semantic priority; không bắt mọi enemy
recolor đỏ. Ship guns, rear engines và carrier bays theo hướng di chuyển/bắn thật.

Native canvases mới: Swept 64×80; Bulwark 112×96; Sniper 80×112; Interceptor 64×80;
Carrier 160×128; Supply Drone 112×80; Warden Drone 48×48. Boss central hull 256×192,
wing 256×112, mount/module 96×96 và truss 192×48; đây là các mảnh ghép ở mount cũ,
không kéo giãn một hull để phủ boss. Collider-bearing wing/node giữ Transform cũ,
art nằm ở child bù scale. Asteroid dùng 48/64/96/128 canvases theo size đã có,
chỉ bù scale visual; không đổi root scale, collider hoặc HP.

Pickup family dùng khung cargo 32×40 với symbol riêng cho từng branch/effect;
Shard vẫn là amber crystal. HUD icons 24×24, giữ chữ native và shared nine-slice
panel/button. Menu/Hangar ship có idle drift nhẹ; navigation và các state M7 giữ nguyên.
Đây là coverage production, không coi mọi overlay/cosmetic state đã qua visual audit.

Astra/Crimson far patches giữ canvas 768×336, PPU 21 và fixed world scale. Nebula
là cụm kín nằm trong patch, mép trả về navy; bỏ planet vẽ sẵn để không trùng với
planet reveal 256×256 riêng. Background đọc region hiện tại cả khi Continue/dev jump;
journey vẫn sở hữu blend/choreography. Navy camera fill tiếp tục lấp ngoài artwork,
không scale background theo camera và không triệt tiêu cảm giác zoom.

[Production manifest](../../tools/assets/m8/production-manifest.json) ghi source,
output, binding và lifecycle từng asset. Source/import readiness không tự đồng nghĩa
runtime verified. Remaining polish: selective amber skin mask; riêng buff-expiry/warp
sheet playback; audit các overlay/result variants ít gặp.
Không chốt device readability/FPS hoặc full audio trong pass này.
Asteroid damage repaint đã hoàn tất trong Batch 3; xem
[M8 fleet family](#m8-fleet-family-và-damage-state-provisional).

## M8 final overhaul `[PROVISIONAL]`

Player chassis giữ ID/ownership/giá nhưng thay thiết kế và display name thành
Mũi Giáo (default) và Ưng Kích (broad swept armor). Mỗi chassis có engine/muzzle
sockets riêng; plume thuộc chassis, movement trail là cosmetic riêng. Vệt nhỏ,
chỉ phát khi player di chuyển, không tạo thêm lửa ở engine socket. Gameplay,
approach và UI preview/card dùng cùng selected hull/skin và plume family. Amber là repaint vùng giáp, không tint toàn hull.
Default là Không Vệt (`trail-none`): tắt movement trail, giữ hai engine plumes.
Ion dùng cyan/white; Rainbow dùng sáu dải màu sáng và preview riêng trong Store/Hangar.
Pricing thuộc [progression/economy](../gameplay/progression-economy.md#m7-cosmetic-hangar-provisional). Banking chỉ đổi visual child. Source và evidence status ở
[production manifest](../../tools/assets/m8/production-manifest.json).

Batch 3 hoàn thiện fleet/combat visual: 7 armor family + Supply Drone crate +
2 boss palette riêng, damage/destruction thật, projectile family riêng và
PREPARE → LOCK → ATTACK → RECOVERY cho Sniper/Interceptor/boss skill. Contract
chi tiết ở [M8 fleet family](#m8-fleet-family-và-damage-state-provisional).
