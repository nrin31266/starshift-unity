# Audio, VFX và animation feedback

## Phạm vi và ownership

Owner của feedback hierarchy, animation/VFX/audio/camera cues và cinematic choreography. Không quyết damage, phase threshold, reward hoặc encounter handoff; tham chiếu gameplay/content owners.

## Feedback hierarchy

Hostile shot/telegraph phải còn đọc được khi firing, pickup và explosion cùng lúc.
Common hit nhỏ; heavy/Elite event vừa; player death/boss/transition là điểm nhấn.
Không mọi sự kiện đều shake/flash. Durations chưa có seed là [TBD]/tune presentation,
không tự khóa timing gameplay qua animation clip.

## Animation philosophy

State quan trọng cần chuyển tiếp ngắn nếu điều đó giúp đọc state tốt hơn;
không yêu cầu animation hoàn hảo hoặc dài. Chọn kỹ thuật theo công việc nó làm,
tránh generate spritesheet khi Transform hoặc Particle đã thể hiện được ý định.

Không dùng sprite-sheet animation cho mọi chuyển động. Chia ba nhóm:

| Kỹ thuật | Trường hợp phù hợp |
|---|---|
| Transform/code animation | Ship entry; enemy path; arc/sine movement; rotations; asteroid spin; Supply Drone movement; pickup hover; orbit/slingshot; camera transition; boss reposition |
| Particle / Trail / material | Engine; muzzle flash; sparks; smoke; hit flash; shield; debris; warp streak; explosion particles; glow/emissive feedback |
| Sprite animation | Khi hình dạng thực sự đổi hoặc hiệu ứng được lợi rõ: explosion, rift/portal, special energy; engine flame nếu particle không đủ; mechanical state nếu sprite swap/module transform không đủ |

Không tạo spritesheet dài chỉ để animate Transform movement. Entry/exit/orbit/respawn
có easing phù hợp; không buộc state gameplay chờ một animation dài.
Frame count và generation/import workflow ở
[asset pipeline](../production/asset-pipeline.md#sprite-sheet-contract).

### Enemy damage feedback

Common enemy ưu tiên `Normal` → `Damaged`; heavy enemy có thể dùng
`Normal` → `Damaged` → `Critical`. Kết hợp short material flash, particles,
smoke và sprite/state swap; không cần một frame cho mỗi HP point.
Đây là presentation mapping; state áp dụng, HP và threshold do
[enemies/bosses](../content/enemies-bosses.md#enemy-hp-và-damage-state) quyết.
Hit flash ngắn phải giữ silhouette và không che attack telegraph.

VS-013 placeholder dùng Hull tint trắng trong 0.08 s `[TUNABLE]`, sau đó giữ
Damaged tint theo threshold của [enemy owner](../content/enemies-bosses.md#damage-state).
Root disable ngay khi chết; chưa có explosion,
death fade, particles hoặc SFX. Cue reset khi object reuse; thời gian flash dùng
scaled time. Đây là presentation tạm cho [Scout checkpoint](../content/enemies-bosses.md#scout-drone),
không phải approved final palette/art hoặc authority cho HP/death.

### Scout fire telegraph

VS-015 placeholder đổi Core từ red emissive sang magenta khi fire component báo
`IsTelegraphing`, rồi trả authored tint sau shot/cancel/disable. Màu theo
[color semantics](art-direction.md#color-semantics-locked); hostile projectile dùng
red/pink, không dùng reward amber/gold. Hull hit flash/Damaged không ghi Core nên
không che mất charge cue. Timing thuộc
[fire profile](../content/enemies-bosses.md#scout-single-fire-checkpoint), không do
presentation quyết. Cue có thể bị disable mà shot logic vẫn chạy; chưa có final
VFX, animation clip, glow framework hoặc audio.

## Audio design

SFX đơn giản có thể synth bằng Python; không bắt buộc synth toàn bộ BGM.
Music có thể là free licensed music, original simple track hoặc asset hợp license.
Workflow và attribution nằm tại [asset pipeline](../production/asset-pipeline.md).

### SFX catalog

| Nhóm | Sounds cần có |
|---|---|
| Weapons | Player laser; plasma; missile; enemy shot; sniper charge |
| Damage | Hit; shield break; small explosion; big explosion; player death |
| Rewards | Pickup; crate; weapon upgrade |
| Movement/transition | Engine; boost; warp |
| Signals/UI | Boss warning; UI click |

### Spam control

Nhiều hit/explosion trong cùng frame không được gây identical SFX spam hoặc clipping.
Có thể dùng cooldown/throttle, randomized pitch nhỏ, max simultaneous voices theo
category và volume hierarchy. Exact voice caps/cooldowns chưa có seed; không tự khóa
chúng trong editorial pass.

### Transition audio và pause

Transition dùng music duck, engine pitch ramp, warp whoosh và fade-in nhạc region mới.
Khi gameplay pause, UI audio vẫn chạy; music có thể duck nhưng loop state phải resume
đúng. Exact mixer/pause mechanism vẫn [TBD]; simulation dừng theo
[architecture](../systems/architecture.md#pause-và-time-management-locked).

## Presentation event matrix

Baseline presentation matrix:

| Event | Animation | VFX | Audio | Camera |
|---|---|---|---|---|
| Player enter | move/ease | engine trail | engine ramp | none/light |
| Enemy hit | flash | small spark | throttled hit | none |
| Enemy death | scale/fade | explosion/debris | small explosion | none/tiny heavy only |
| Player death | disable/rotate | staged explosion | player death | medium |
| Respawn | entry ease | shield/blink | respawn cue | none |
| Shield break | pulse | teal ring burst | shield break | tiny |
| Supply destroyed | break | small explosion | supply hit | tiny |
| Boss phase | mechanical/open | burst | phase stinger | small |
| Boss death | staged | large explosions | big explosion | strong |
| Warp | ship boost | star streak | warp | subtle |

Exact timing chưa có seed giữ [TBD]; khi tune sẽ cập nhật tại owner này.

## Event coverage cho player và firing

Player entrance dùng transform/ease + engine trail + engine ramp; firing cue riêng
cho Pulse/Plasma/Missile, muzzle/shot rõ nhưng không phủ vùng né.
Player death dùng staged explosion, player-death SFX và medium shake;
respawn entry có cue riêng, blink alpha/ring teal cho invincibility, tránh nhầm hit flash.
Hit/death/protection resolve bởi [player combat](../gameplay/player-combat.md),
không chờ particle để quyết có nhận damage.

### Player damage lifecycle placeholder

VS-018–020 dùng Hull trắng trong Dying, entrance ease và teal blink khi post-entry
invincible; Game Over hide Hull. Blink cycle 0.2 s, visible 60% `[TUNABLE]`, dùng
scaled time nên pause giữ cue hiện tại. Disable presentation/root restore authored
tint/enabled state. Đây là placeholder, chưa có explosion particles, audio, final
VFX hoặc shield-item system. Gameplay phase/timing thuộc
[player owner](../gameplay/player-combat.md#player-survival); tắt cue không đổi lives,
cleanup, entrance hoặc protection permission.

## Enemy, Elite, boss và hazard feedback

Enemy hit: bright flash cực ngắn, spark nhỏ, throttled SFX, không shake mọi viên.
Heavy death có debris/explosion và tiny/small shake khi cần.
Elite accent rõ, cue reward/death nổi hơn normal nhưng dưới boss.
Boss intro có entrance/mass, warning/stinger; phase mechanical open/burst nhỏ,
không che attack telegraph.

Vanguard death: small explosions → core glow → final explosion → shake → music duck
→ reward → transition. Nebula Warden: node local explosion → shield flicker;
final node → shield collapse/SFX, vulnerability do enemy mechanic quyết.
Asteroid có stone/debris impact; golden asteroid có amber crystal burst/reward cue,
không biến cả asteroid storm thành chuỗi full-screen flash.

## Supply và pickups

| Event | Feedback định hướng |
|---|---|
| Supply Drone xuất hiện | Nhận ra cargo/reward bằng trail/navigation cue, không warning như boss |
| Supply Drone destroyed | Break/small explosion, supply hit, small burst; hai direct pickup bật nhẹ hai phía, không BOX trung gian |
| Crate drift/pickup | Cargo drift, reward reveal + crate cue; không giấu pickup trong explosion |
| Weapon Core | Upgrade pulse và upgrade cue, branch/level đọc qua HUD |
| Shield Cell | Teal defensive ring/activate cue |
| Shield break | Teal burst, distinct break SFX, tiny shake nếu phù hợp |
| Overdrive | Firing/engine accent và timer cue; không làm projectile unreadable |
| Support Drone | Entrance/trail ngắn và summon cue, bắn phụ dưới main weapon volume |
| Reserve Missile | Amber burst + brief camera impact; exact prototype ở [cue owner section](#reserve-missile-impact-cue), không quyết damage bằng effect |
| Extra Life | Green-white positive cue, ngắn và nhận ra survival reward |
| Stellar Shard | Amber/gold glint và collect cue nhẹ, throttle khi nhiều pickup |

## Camera shake và time dilation

| Tier | Seed event mapping |
|---|---|
| Tiny | Strong hit/weapon; shield break/supply khi cần |
| Small | Heavy enemy death, boss phase |
| Medium | Player death, Elite event |
| Strong | Boss explosion, major transition |

Exact amplitude/duration [TUNABLE] chưa có số. Screen Shake setting được tôn trọng;
camera zoom cue smooth theo level profile, không overshoot làm input khó.
Hit-stop/time dilation hạn chế, không mỗi bullet; final death có slow motion ngắn.
Exact hit-stop rules [TBD], không âm thầm pause damage timer ngoài gameplay contract.

## Transition Astra → Crimson

Target major travel/approach ~8–15 s `[TUNABLE]`, không gồm chờ tap hoặc reward window;
không bắt buộc hai transition bằng nhau. HUD fade → glide/breath → acceleration đồng
thời ship bank → star vector cong và old environment trôi đi → gradual palette blend
→ deceleration → Planet reveal/settle → tap → approach signature → destination ready.
Engine pitch/warp whoosh/music duck là presentation polish khi có assets; không cần
real 3D warp. Current placeholder implementation/timing ở
[level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).
Input lock, orientation và handoff do [levels](../content/levels-encounters.md#transition-astra--crimson) sở hữu.

## Transition Crimson → Titan

Ship acceleration → ringed planet/structure reveal → ring slingshot curve
→ camera widen/debris scale → title. Giữ cảm giác không gian lớn hơn,
không copy exact Hyperlane animation; âm thanh nối vùng và music fade phù hợp.
Right → Up/handoff ở [levels](../content/levels-encounters.md#transition-crimson--titan).

## Final Rift

Phase shift có rift opening, cinematic rotation/reposition và resume cue rõ;
projectile clear không được effect tự quyết.
Final death: core crack → staged explosions → short slow motion → final flash/explosion
→ music resolution → Ending → Campaign Result. Threshold/attacks thuộc
[enemies/bosses](../content/enemies-bosses.md), hướng/handoff ở level owner.

## Level intro, warning và dynamic audio

Mission intro dùng các nhịp riêng `[TUNABLE]`:

| Nhịp | Seed | Presentation |
|---|---|---|
| Establishing | Reading delay 1.8 s, sau đó chờ tap | Planet làm mốc đích, starfield trôi rất nhẹ; framing 8, Region / Planet title + một câu theo major layout. Lời nhắc fade .4 s sau reading delay. |
| Approach | .7 s | Tàu trình diễn tiến nhẹ, planet/parallax thay đổi vừa phải; framing 8 → 9. |
| Travel | 1.8 s | Planet fade sang không gian, starfield tăng tốc; giữ framing 9 lâu hơn. |
| Brake và gameplay blend | .8 s | Giảm tốc đồng thời camera 9 → baseline, tàu về pose thật, backdrop/HUD fade về; hết giảm tốc mở control ngay. Không có nhịp đứng chờ riêng. |
| Mission title overlay | 2.1 s | Mission lớn/bold và subtitle ngắn; formation đầu bắt đầu sau .75 s, khi chữ vẫn hiện/fading. |

Mission title visible duration `[TUNABLE]` độc lập spawn lead; không còn blocking
card 2.8 s. Pause dừng scaled title/entry clocks. Same-planet Mission travel giữ
input, bias lùi nhẹ và tăng star travel đồng thời với title/entry theo
[level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).

Tổng cinematic seed khoảng 3.3 s, không gồm thời gian chờ tap. Camera size
8 → 9 là mở rộng framing; cảm giác tiếp cận đến từ relative ship/planet/parallax,
không dùng camera zoom một mình để giả tiến tới. Planet giữ vị trí mốc đích thay
vì lao qua toàn màn hình. Tên location còn TBD dùng nhãn cấu trúc theo
[UI owner](ui-ux.md#m2-ui-implementation-provisional), không thay bằng mission number.

Dùng cùng gameplay scene; sprites/background layers chuyển trạng thái trình diễn,
không load scene riêng hoặc thêm cutscene framework. Sprite tàu intro chỉ là visual
proxy; Player movement/Rigidbody vẫn giữ writer cũ. Hide/reset restore Player Visual
và backdrop đã có; không mutate Player tint/physics hoặc instantiate theo từng phase.
Planet vẫn là procedural placeholder, không final art hoặc canonical planet identity.

A → B có starfield cue mờ, nhẹ, fade in/out trong khoảng nghỉ do encounter owner
quyết; giữ HUD, optional short contextual copy auto fade, không confirmation. Không
bắt đầu cue khi còn shot cũ cần
resolve. UI/control rules thuộc [UI/UX](ui-ux.md#vocabulary-và-announcement).

Level intro/sector title gọn, boss/hazard warning ngắn và không spam WARNING.
Audio categories [PROVISIONAL]: Music, player/enemy weapons, hits/explosions,
pickup/supply, engine/transition và UI. Mixer group implementation chưa khóa.
Dynamic audio có intensity/duck/stinger ở boss phase và transition khi cần;
không cần adaptive-music framework phức tạp.

## Haptic

M7 `[PROVISIONAL]`: VibrationEnabled đi qua SettingsFeedback, gọi Android haptic ở
reserve missile detonation; không rung trong Editor. Exact final use vẫn [TBD]. Gợi ý chưa khóa: player death,
boss explosion, shield break nhẹ. Không rung mỗi bullet hit.
SFX synth workflow và BGM licensing/import ở [asset pipeline](../production/asset-pipeline.md).


## Reserve missile impact cue

Special reserve missile có rocket lớn hơn primary Homing, phóng từ vị trí Player
tại launch về tâm view theo nhịp tăng tốc, trail taper ngắn. Presentation đọc
LaunchViewport/FlightProgress từ reserve owner; không quyết thời điểm damage.
Đến Detonated mới hide rocket và phát amber flash ngắn + expanding thin ring .45 s
`[TUNABLE]`, đồng thời camera impulse mạnh hơn normal Homing fire; primary Homing
không trigger camera impulse. Flight duration thuộc [weapon owner](../content/weapons-items-drops.md#tên-lửa-dự-trữ).
Prototype tunables
trên GameplayViewController: duration .14 s (range .10–.18), strength .024 viewport
fraction (6× seed .004; clamped tối đa .032), decay nhanh theo squared envelope `[TUNABLE]`.
Retrigger restart bounded window, không cộng biên độ hoặc thời lượng vô hạn.

Dùng existing GameplayViewController; lệch orthographic projection chỉ trong URP
begin/end camera rendering và restore matrix sau render. Không đổi Camera Transform,
zoom, playable viewport, drag conversion, collider, damage hoặc gameplay timers.
Không thêm camera system thứ hai. Pause freeze impulse clock và render neutral;
NewGame/Retry/Continue/exit clear rocket/trail/flash và pending impact/burst. Respect ScreenShakeEnabled
trong persisted Settings. Animation/impulse observer không quyết damage hoặc stock.


## M5 Vanguard placeholder

`[TUNABLE]` Death beats chạy 2.4 s scaled time: bốn small burst xen kẽ → core/main
burst → short final impact → Core + support pair. Hostile damage đã bị khóa trước beats, timing
không quyết HP hoặc phase. Phase transition có amber core cue theo [enemy owner](../content/enemies-bosses.md#boss-1--vanguard-frigate-provisional);
strong attacks có module glow trước launch. Không final VFX/music/audio pack mới.

Reserve missile vẫn dùng impact/projection restore hiện có; Homing primary không
phát impact đó. Boss death tạm reuse short reserve impulse, chưa có strong final
explosion/shake mix. Region travel dùng glide → tăng tốc/bank → turn cùng star
vector/palette → giảm tốc → reveal Planet 2/tap → approach signature. Ship và star
heading đổi đồng thời, environment đổi màu dần; gameplay semantic colors giữ nguyên.
Planet reuse neutral placeholder disc với palette đích; chưa final orbit/warp art
hoặc audio polish. Timing và gates thuộc [level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).

Asteroid/resource entry có amber edge marker trước khi hazard vào view. Supply event
hiện cue `TIẾP TẾ` ngắn; không mở tutorial panel hoặc chặn Encounter.


M5 second feel pass: asteroid dùng persistent damaged/critical tint sau hit flash,
optional sprite-frame hooks cho future cracks và pooled .35 s expanding glow burst.
Burst scale theo asteroid root; không thêm AoE hoặc camera shake cho mỗi rock.

Vanguard reactor/core và engine cue làm phase break; twin pods telegraph đồng thời,
missile pod riêng, beam pod hãm tàu trước khai hỏa. Magenta lane warning có opacity
.3; active lane .9 với white core rộng 25% beam. Module gửi cùng damage bounds cho
presentation, giữ safe space rõ hai bên. Native 16:9/20:9 review phải xem beam còn
nối đúng pod khi active. Timing/HP/movement thuộc [enemy owner](../content/enemies-bosses.md#boss-1--vanguard-frigate-provisional),
không để animation/color quyết damage. Defeat chain vẫn 2.4 s trước direct reward pair/handoff.


## M6 gameplay placeholder feedback `[PROVISIONAL]`

M6 không thêm final art/audio. Sniper/Interceptor dùng mảnh aim line khi charge và
beam/committed strike khi active; cue là presentation, hit query do attack owner giữ.
Warden Beam Sweep có magenta direction arrow trong charge để đọc chiều quét trước
khi active; line/core tăng độ sáng và bề rộng khi fire, không dùng cue làm hit authority.

Shield hit pulse .1 s; từng node có local break burst .4 s, final node dẫn sang
shield flicker/collapse và recoil. Khi expose, core phase cue khác protected network;
không có shield regeneration. Các duration này là placeholder `[TUNABLE]`, chưa có
final SFX, particle pack hoặc music. Collapse/vulnerability/attack timing do
[enemy owner](../content/enemies-bosses.md#m6-region-b-runtime-mechanics-provisional) quyết.


## M7 settings audio foundation `[PROVISIONAL]`

SettingsFeedback giữ hai AudioSource Music/SFX trong gameplay scene. Saved volumes
được apply khi boot và khi explicit Settings save thành công; playback entry points
PlayMusic/PlaySfx dùng chính channels này. Chưa có final music/SFX clips hoặc adaptive
mixer content; không claim nghe được volume change khi chưa có clip. ScreenShake toggle
đọc ở reserve impact như contract trên; không tạo shake owner thứ hai.

## M8 pixel animation baseline

M8 selected explosion là **8 native 64×64 frames, 16 fps, 0.5 s scaled time**
`[LOCKED]` cho small/common burst. Frame 0–2 white/amber ignition, 3–4 expanding
clusters/shards, 5–6 darker debris/smoke, 7 empty. Center pivot/canvas không đổi,
không trim hoặc đổi scale theo từng frame. Playback dùng existing pooled
AsteroidDestructionPresentation, fixed scale theo source enemy; supply giữ scale .6.
Legacy no-frame fixture vẫn dùng burst cũ. Không đổi death/HP/drop timing, thêm AoE,
hoặc buộc gameplay đợi animation. Pause giữ frame; pool release sau duration và clear
khi run reset. Actual frame advance và pool release đã assert trong Play Mode.

Scout/Raider charge dùng magenta bracket sprite riêng, alpha 0 khi idle, hiện trắng
texture tint khi existing fire component telegraph; hit flash chỉ ghi hull nên không
che cue. Không đổi charge/fire timings. Pulse cyan thin needle, hostile bolt pink
white-core diamond; Shard amber facets. Source/import conventions thuộc
[asset pipeline](../production/asset-pipeline.md#m8-selected-sources-và-production-recipe),
palette/material thuộc [art bible](art-direction.md#m8-vertical-art-slice-locked).

M8 chỉ có một burst quality anchor; heavy/boss staged explosion/audio, hit particles,
engine sheets và special VFX chưa final. Không xem common sheet là full VFX pack.
