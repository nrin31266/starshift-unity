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
| Supply Drone destroyed | Break/small explosion, supply hit, tiny shake; crate ejection thấy rõ |
| Crate drift/pickup | Cargo drift, reward reveal + crate cue; không giấu pickup trong explosion |
| Weapon Core | Upgrade pulse và upgrade cue, branch/level đọc qua HUD |
| Shield Cell | Teal defensive ring/activate cue |
| Shield break | Teal burst, distinct break SFX, tiny shake nếu phù hợp |
| Overdrive | Firing/engine accent và timer cue; không làm projectile unreadable |
| Support Drone | Entrance/trail ngắn và summon cue, bắn phụ dưới main weapon volume |
| EMP | Pulse mạnh ngắn + EMP cue, nhanh trả contrast; không quyết damage bằng effect |
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

Tổng transition ~8–15 s [TUNABLE], không bắt buộc hai transition bằng nhau.
HUD fade → center alignment → engine boost → scroll acceleration → star streak
→ old background crossfade → Crimson reveal/deceleration → red planet orbit curve
→ smooth rotation/movement → sector title. Small camera zoom/easing, engine pitch ramp,
warp whoosh, music duck và new-region fade-in; không cần real 3D warp.
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

Mission intro M3 dùng placeholder approach 3 s `[TUNABLE]`, authoring target 2–4 s:
starfield tăng cảm giác tốc độ → planet disc lớn dần/đi khỏi combat view → camera
wide settle về level baseline → mission/location announcement ngắn → control và
encounter đầu. Seed wide là size 8 `[TUNABLE]`, không hard-code return size trong
runner. Planet/starfield được author trong scene, không Instantiate/Destroy trong
intro hoặc build cutscene framework. Reset/skip phải hide sạch presentation.
Planet texture là procedural neutral disc tại placeholder folder; chưa là final art
hoặc canonical planet identity. Location trống thì chỉ hiện mission title.

Level intro/sector title gọn, boss/hazard warning ngắn và không spam WARNING.
Audio categories [PROVISIONAL]: Music, player/enemy weapons, hits/explosions,
pickup/supply, engine/transition và UI. Mixer group implementation chưa khóa.
Dynamic audio có intensity/duck/stinger ở boss phase và transition khi cần;
không cần adaptive-music framework phức tạp.

## Haptic

Vibration setting tồn tại; exact use [TBD]. Gợi ý chưa khóa: player death,
boss explosion, shield break nhẹ. Không rung mỗi bullet hit.
SFX synth workflow và BGM licensing/import ở [asset pipeline](../production/asset-pipeline.md).
