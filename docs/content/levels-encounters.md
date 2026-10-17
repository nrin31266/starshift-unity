# Levels và encounters

## Phạm vi và ownership

Owner của 12 level, encounter composition, paths/formations, FlightSequence usage, camera profiles, directions, transitions và checkpoint placement. Không sở hữu enemy HP hoặc weapon damage.

## Terminology và composition

Level là đơn vị campaign; encounter/wave là đơn vị schedule/completion/checkpoint.
MainWave quyết progress; Optional Event Layer chạy song song, không chặn completion.
Direction là hướng combat trong app landscape, không phải xoay màn hình thiết bị.
Exact counts/timing của các encounter dưới là [TUNABLE]; tên region/level/boss là
[PROVISIONAL], không phải tên marketing đã chốt.

## Encounter model

### Level không bắt buộc 3 wave `[LOCKED]`

Ba encounter/level là baseline phổ biến, không phải quota. Level mở đầu có thể
chỉ cần hai encounter, boss level dùng pre-boss + boss, còn level cao trào có thể
cần bốn. Chỉ thêm encounter khi pacing cần, không nhồi để đủ số.

### `Eliminate`

Wave chỉ hoàn tất khi các required enemies đã chết. Required enemy không được
bay mất vĩnh viễn rồi giữ progress bị kẹt; khi path kết thúc nó phải loop, hold,
return hoặc chuyển combat state.

### `Timeline`

Wave hoàn tất theo timeline/spawn sequence; enemy còn sống được exit/despawn.
Kill nhiều cho thêm reward nhưng không phải điều kiện chặn tiến độ, nên một enemy
bay mất không làm player bị kẹt wave.

### `Survive`

Wave hoàn tất khi timer kết thúc. Pressure có thể đến từ hazard hoặc fly-by,
trong khi optional reward cho player thêm quyết định risk/reward:

- asteroid;
- debris;
- enemy fly-by;
- optional Supply Drone;
- golden meteor.

### `Boss`

Hoàn tất khi boss defeat + post-death sequence xong.

### Optional Event Layer `[LOCKED]`

Optional Event Layer chạy song song MainWave, không tạo completion gate riêng.
`Intercept` không phải CompletionMode. Các event được dùng gồm:

- Supply Drone;
- Golden Meteor;
- Elite FlyBy;
- Bonus Formation.

Ví dụ:

```text
MainWave = Survive asteroid 18 s
OptionalEvent @ 7 s = Supply Drone
```

Player vừa né asteroid vừa quyết định săn reward.

## Multi-enemy attack coordination

`[PROVISIONAL]` product direction; runtime coordination còn `[FUTURE]`. Local enemy
attack biết thực hiện attack khi được phép; encounter layer về sau quyết lúc nào và
enemy nào được attack. Phase `canShoot` chỉ tạo eligibility, không có nghĩa mọi enemy
eligible đều tự chạy independent cooldown rồi fire cùng lúc. Enemy visible count
không bằng active shooter count; density seed nằm ở [pacing](#pacing-và-pre-boss-rules).

Concept có thể dùng: active shooter limit, shot spacing, controlled/weighted selection,
recent-attack suppression, attack permission, threat budget và strong-attack priority.
Common shots có thể tạm giảm khi Sniper/Bomber/boss đang có telegraph quan trọng để
giữ đường né và readability. Chưa khóa concept nào thành class/API/algorithm, chưa
định probability, quota hoặc thiết kế FireDirector framework.

Nếu dùng random, baseline là eligible enemies → filter state/recent attack/threat/
permission → controlled selection → attack. Random tạo variation trong giới hạn
readability, tránh projectile wall ngẫu nhiên hoặc nhiều shot trùng do timer độc lập.
Không dùng pure independent random timer trên từng enemy làm encounter policy.
VS-015 vẫn là single-enemy sandbox với
[local fire profile](enemies-bosses.md#scout-single-fire-checkpoint); reconciliation
không thêm permission hook, encounter scheduler hoặc runtime coordination.

## Encounter lifecycle và handoff

Completion xác nhận mục tiêu encounter đã xong; handoff chỉ bắt đầu encounter tiếp
khi các object cũ đã được resolve theo contract dưới đây. Optional Event Layer
không thêm kill/pickup requirement vào completion. Reward grace là khoảng nghỉ
có giới hạn sau completion, không phải điều kiện phải nhặt hết reward.

### Completion theo mode

| Mode | Điều kiện completion và xử lý enemy còn sống |
|---|---|
| Eliminate | Required enemies phải chết trước khi complete; không despawn chúng để giả hoàn thành. Non-required enemy còn sống đi qua cleanup chung. |
| Timeline | Enemy đã exit được despawn. Khi timeline kết thúc, enemy còn trên màn hình ngừng attack/collision gây damage ngay, sau đó exit không gây damage trong thời gian giới hạn [TUNABLE] hoặc despawn; hết giới hạn phải release. Không handoff enemy active sang encounter sau. |
| Survive | Timer kết thúc là completion. Dừng spawn/attack, clear hostile projectiles và vô hiệu hóa hazard cũ ngay; enemy còn sống đi qua cleanup chung trước handoff. |
| Boss | Ngay khi boss defeat, dừng hostile producers và clear hostile projectiles/hazards trước post-boss/result/region transition. Giữ staged death/presentation và reward flow; completion vẫn cần post-death sequence hoàn tất. |

### Cleanup chung

Khi nhận completion, đóng schedule của encounter và optional events: không spawn
enemy/hazard mới hoặc tiếp tục firing, minion summon, delayed attack từ encounter
cũ. Boss defeat đóng hostile producers sớm như bảng trên; scripted reward của
final kill/boss death vẫn được resolve đúng một lần theo reward flow.

| Object thuộc encounter cũ | Contract tại boundary |
|---|---|
| Enemy còn sống, không còn là required target | Ngừng attack và collision gây damage ngay; exit không gây damage với timeout [TUNABLE] hoặc despawn. Cleanup không tính kill và không roll death drop. |
| Hostile projectile/beam | Tắt damage ngay và clear/release trước reward grace hoặc handoff; không để callback cũ gây damage ở encounter mới. |
| Hazard | Tắt damaging collision/timed damage ngay rồi release; visual kết thúc ngắn có thể chạy nhưng không còn gây damage. |
| Pickup có giá trị | Giữ cho player có cơ hội nhặt trong reward grace; không xóa cùng hostile cleanup hoặc tự grant khi chưa pickup. |
| Supply Crate | Giữ crate đã kiếm được, kể cả crate ejected trong final death flow, qua reward grace; reward selection/pickup vẫn theo item owner. |
| Temporary encounter object | Dừng timer/callback sinh gameplay; release khi chức năng kết thúc. Cosmetic/VFX có thể kết thúc theo presentation timeout [TUNABLE], không được giữ encounter mở vô thời hạn. |

Default không carry enemy, hostile projectile hoặc hazard active từ encounter cũ
sang encounter mới. Background/decoration thuộc level không trở thành encounter
hazard chỉ vì còn hiển thị; ownership phải rõ khi implementation.

### Reward grace và chuyển encounter

Nếu còn valuable pickup hoặc Supply Crate, mở reward grace sau khi reward từ final
kill/boss death đã hiện và player có thể điều khiển để nhặt. Duration là [TUNABLE],
chưa gán số; tại boundary, bảo đảm pickup/crate còn ít nhất khoảng grace này nếu
lifetime cũ sắp hết. Không spawn threat mới trong cửa sổ đó. Không có reward để
nhặt thì bỏ qua grace.

Grace hết thì pickup/crate chưa nhặt được expire với cue đọc được; không auto-collect
hoặc tự đổi thành bonus. Handoff không chờ player nhặt hết. Với result/scene/region
transition, cơ hội nhặt phải diễn ra trước khi khóa control hoặc unload world.
Crate lifetime ngoài boundary và reward behavior vẫn thuộc
[weapons/items/drops](weapons-items-drops.md).

```text
Completion / boss defeat cleanup
→ kết thúc post-death và reward presentation nếu có
→ reward grace khi còn pickup/crate
→ resolve/release object cũ còn lại
→ checkpoint đầu encounter tiếp theo → start schedule mới
```

Chỉ mở checkpoint/start encounter tiếp theo sau handoff an toàn; không snapshot
object trong grace. Quit giữa grace hoặc post-boss vẫn là câu hỏi resume-marker
[TBD] tại [save/profile](../systems/save-profile.md#các-contract-còn-mở).
Duration exit, grace và presentation timeout là config/tuning, không cần thêm một
framework lifecycle riêng. Animation/audio chi tiết thuộc
[audio/VFX](../presentation/audio-vfx.md).

## Flight path và FlightSequence

### Movement path ≠ combat behavior `[LOCKED]`

Enemy archetype, flight path và attack behavior là ba responsibility độc lập.
Path chỉ mô tả di chuyển. Combat behavior quyết có bắn hay không, kiểu bắn,
hold duration, target tracking và cooldown. Cùng path có thể tạo các threat khác nhau
khi ghép fire profile, không cần thêm enemy archetype cho từng cách bay.
Formation/path dùng camera/viewport-relative reasoning, không hard-code world path
cho một aspect ratio. Attack eligibility vẫn qua
[encounter coordination](#multi-enemy-attack-coordination) khi layer đó được implement.

Ví dụ cùng `ArcRight`:

```text
Scout = ArcRight + SingleFire (khi được cấp attack permission)
Bomber = ArcRight + BurstSpread
Sniper = ArcRight + NoFire → Hold → ChargeLaser → Exit
```

### FlightSequence `[LOCKED]`

Không giới hạn cứng 3 phase.

Một sequence có thể:

```text
Entry
→ Move
→ Hold
→ Attack
→ Cross
→ Exit
```

Mỗi phase có thể chứa các data sau; đây là conceptual fields, chưa khóa schema:

| Field | Vai trò |
|---|---|
| path, speed, duration | Đường di chuyển và thời lượng phase |
| canShoot, fire profile | Khả năng và kiểu bắn |
| target behavior | Tracking/targeting trong phase |
| exit condition | Điều kiện kết thúc phase |

### Scout entry/hold checkpoint

VS-014 dùng straight entry theo normalized **full camera viewport**, rồi hold vô
hạn cho tới death hoặc owner release; không tự exit và làm kẹt required enemy.
Seed `[TUNABLE]`: entry `(0.5, 1.12)` → hold `(0.5, 0.78)`, duration 1.2 s, smoothstep
không overshoot. Đây là thời lượng local flight, không phải level spawn schedule.
Scene hiện có một Scout để manual slice test; chưa triển khai số lượng/timing của
L1 encounters bên dưới hoặc phase Attack/Exit của FlightSequence đầy đủ.

Reproject theo current camera size/aspect, không giữ world waypoint cố định.
Hold target được đưa vào full viewport theo collision envelope; Player restricted
region không tự giới hạn enemy. Zoom/view transition không restart entry clock.
Scaled time pause dừng clock; re-enable bắt đầu entry mới. Direction orchestration
vẫn future; path không gắn level ID, wave ID hoặc tự điều khiển firing/completion.
Implementation boundary thuộc [architecture](../systems/architecture.md#m2-scout-baseline-provisional).

### Path library baseline

`[PROVISIONAL]` vocabulary để reuse, không phải mandatory implementation checklist:

- Straight;
- Sine;
- ArcLeft;
- ArcRight;
- DiagonalPass;
- Dive;
- DiveAndReturn;
- Wave/Snake;
- Flyby;
- Reposition;
- Loop;
- Cross;
- Pincer;
- Orbit/Hold;
- ExitCurve.

### Formation library baseline

`[PROVISIONAL]` vocabulary; một Scout có thể tham gia nhiều formation/path mà không
cần type mới. Chỉ author pattern khi encounter thật cần:

- Line;
- Column;
- V;
- Wedge;
- Staggered;
- StaggeredColumns;
- TwinFlank;
- ConvergingWings;
- Cross;
- Escort;
- Ring/semicircle nếu cần.

Large spectacle formation có thể ghép các vocabulary trên, không yêu cầu tạo system
hoặc path asset mới ngay. Visible-count tuning thuộc [pacing](#pacing-và-pre-boss-rules).

### Spawn mode

- simultaneous;
- staggered;
- sequential;
- reinforcement.

### Timeline enemy có thể bắn khi đang chạy

Timeline enemy có thể bay vào và bắn, hold rồi burst, move tiếp, đổi formation,
vừa exit vừa bắn và rời screen sống sót. Completion theo timeline không tắt combat
behavior; không giới hạn enemy vào fly-through im lặng.

## Hazard composition và telegraph

Asteroid normal, small debris, large và golden/resource có identity/reward tại
[weapons/items/drops](weapons-items-drops.md#asteroid-reward-classes).
Motion patterns: vertical, diagonal left/right, cross, staggered, large slow sweep.
Hazard lớn ngoài screen cần edge warning, shadow/indicator hoặc thời gian nhìn thấy
đủ trước collision. Không spawn giant rock ngay trên player.

## Master campaign table và camera profiles

[LOCKED] 12 levels, 3 regions, boss L4/L8/L12; Up → Right → Up, final dynamic shift.
Orthographic size và duration là [TUNABLE]. Duration gồm boss khi có.

Gameplay view mặc định dùng **Orthographic Size = 6**, full viewport `[TUNABLE]`;
return to baseline về profile này. Baseline không phải gameplay minimum.

| Framing intent | Ortho size seed `[TUNABLE]` |
|---|---|
| Close / tight | khoảng 5–5.5 |
| Normal / general campaign | khoảng 6 |
| Wide formation | khoảng 6.5–7.5 |
| Epic / boss / large set-piece | khoảng 7.5–8.5 |
| Spectacle | có thể tới khoảng 9 nếu readability/performance cho phép |

Các range là language/tuning direction, không phải absolute limits hoặc difficulty
scalar. Framing không tự tăng active shooters hay projectile density. Giữ những
level seed close có intent cụ thể; không mechanically đổi mọi level thành default.

Framing và playable region là hai setting riêng: default full current viewport;
phase có thể request normalized restricted region, ví dụ lower 70% `[TUNABLE]`.
Direction change không mặc định tạo bounds riêng; movement đi theo current view,
chỉ override region khi content yêu cầu. Quy tắc footprint/clamp thuộc
[player owner](../gameplay/player-combat.md#movement-bounds-và-camera-interaction);
runtime API/config thuộc [architecture](../systems/architecture.md#dynamic-gameplay-view-foundation).
Foundation chưa chạy wave/boss/direction sequencing.

| Level | Tên [PROVISIONAL] | Region | Combat | Ortho size | Phút mục tiêu |
|---|---|---|---|---:|---|
| L1 | First Contact | Astra Frontier | Up | 6.0 | 1.0–1.5 |
| L2 | Broken Route | Astra Frontier | Up | 5.2 | 1.5–2.0 |
| L3 | Spearhead | Astra Frontier | Up | 5.4 | 1.5–2.0 |
| L4 | Vanguard | Astra Frontier | Up | 5.2 | 2.0–3.0 |
| L5 | Sidewind | Crimson Nebula | Right | 5.6 | 1.5–2.0 |
| L6 | Red Corridor | Crimson Nebula | Right | 5.0 | 1.5–2.0 |
| L7 | Crossfire | Crimson Nebula | Right | 6.0 | 2.0–2.5 |
| L8 | Nebula Warden | Crimson Nebula | Right | 5.5 | 2.0–3.0 |
| L9 | Titan Rain | Titan Ring / Rift Approach | Up | 6.1 | 1.5–2.0 |
| L10 | Open Expanse | Titan Ring / Rift Approach | Up | 6.8 | 2.0–2.5 |
| L11 | Rift Approach | Titan Ring / Rift Approach | Up | 5.8 | 2.0–2.5 |
| L12 | Void Core | Titan Ring / Rift Approach | Dynamic | Dynamic | 2.5–4.0 |

Normal encounter khoảng 15–35 s [TUNABLE], boss lâu hơn; không ép mọi encounter vào
range. Campaign mục tiêu 20–30 phút [TUNABLE]; các range cộng lại và transition
phải được playtest, không mặc định tổng upper bound là target.
Camera close giảm simultaneous threats; wide mở formation, không tự tăng density.
Camera movement ổn định, không overshoot gây khó drag; không scale tàu để giả zoom.

## Region A — Astra Frontier `[PROVISIONAL]`

Region mở đầu dạy core mechanics bằng threat dễ đọc: movement/hold-to-fire trước,
FlightSequence/hazard tiếp theo, rồi Supply Drone và Elite. L4 tạo recovery/resource
window trước boss giới thiệu. Visual identity thuộc [art direction](../presentation/art-direction.md).

### Level 1 — First Contact `[PROVISIONAL]`

L1 giới thiệu movement, hold-to-fire và basic enemy trong 1–1.5 phút. Hai encounter
đủ cho mục tiêu này; không thêm pressure chỉ để khớp baseline ba wave.

Camera:

```text
6.0 normal
```

Encounter A — `Eliminate`

- 3 Scout;
- Straight/soft arc entry;
- hold ở vùng trên;
- single fire;
- phải kill hết.

Encounter B — `Timeline`

- 2 nhóm Scout/Raider nhẹ;
- V/diagonal pass;
- một nhóm dừng ngắn rồi bắn burst;
- tiếp tục bay ra và vẫn có thể bắn;
- wave kết thúc theo timeline.

Reward:

- guaranteed Weapon Core hoặc scripted progression reward để player thấy upgrade system sớm `[TUNABLE]`.

Không Supply random ở L1 để giảm overload.

### Level 2 — Broken Route

L2 hướng tới khoảng 1.5–2 phút, lần lượt cho thấy FlightSequence, hazard và enemy
bền hơn có damage state.

Encounter A — `Timeline`

- Raider theo ArcLeft/ArcRight;
- enter → hold → burst → exit;
- nhấn mạnh flight sequence.

Encounter B — `Survive`

- diagonal asteroid storm;
- 10–14 s;
- có 1–2 golden asteroid;
- chưa cần Supply Drone.

Encounter C — `Eliminate`

- Bomber + Scout escort;
- Bomber giới thiệu HP cao hơn và damaged state.

### Level 3 — Spearhead

L3 hướng tới khoảng 2 phút, ghép formation nhiều hướng với lần đầu giới thiệu
Supply Drone và Elite; supply xuất hiện lúc pressure thấp để reward còn đọc được.

Encounter A — `Timeline`

- Raider sine formation;
- staggered entry;
- continuous fire mức vừa.

Encounter B — `Timeline`

- Pincer từ hai bên;
- Scouts/Raiders cross nhau;
- player học đọc nhiều hướng.

Optional Event:

- scripted hoặc forced Supply Drone đầu tiên;
- xuất hiện giữa encounter hoặc ngay sau pressure thấp.

Encounter C — `Eliminate`

- Elite Bomber hoặc Elite Raider + escort;
- giới thiệu Elite visual/reward.

Có thể có encounter thứ 4 chỉ nếu playtest cho thấy pacing cần, không mặc định nhồi.

### Level 4 — Vanguard

Pre-boss:

`Survive/Resource Storm`

- asteroid diagonal;
- golden asteroid;
- một Supply Drone guaranteed hoặc strong probability;
- tạo cơ hội chuẩn bị.

Boss:

**Vanguard Frigate**.

Post-boss:

- death cinematic;
- reward;
- Hyperlane Transition 1.

## Transition Astra → Crimson

Sau L4 boss defeat/post-death/reward, khóa input và firing trong cinematic.
Target hướng đổi Up → Right, gameplay position từ bottom-center sang left-center.
Handoff sang L5 chỉ trả control khi orientation/entry sẵn sàng và cinematic hoàn tất.
Không đổi physical device orientation. Trình tự hiệu ứng thuộc
[audio/VFX](../presentation/audio-vfx.md#transition-astra--crimson).

## Region B — Crimson Nebula `[PROVISIONAL]`

Combat chuyển sang Right. Close/wide camera tạo hai dạng pressure khác nhau;
Sniper, Interceptor và Carrier mở rộng telegraph/target-priority thay vì chỉ tăng
số enemy. Palette nằm ở [art direction](../presentation/art-direction.md).

### Level 5 — Sidewind

L5 dạy horizontal combat trước khi thêm pressure từ Sniper/Carrier ở các level sau.

Encounter A — `Eliminate`

- Scouts từ right;
- straight/soft arc.

Encounter B — `Timeline`

- Raiders theo vertical-offset sine;
- vừa fly vừa bắn.

Encounter C — `Eliminate` hoặc Timeline tùy pacing

- Bomber + escorts;
- giới thiệu firing lanes khi chơi ngang.

### Level 6 — Red Corridor

Camera:

```text
5.0 intentional close
```

L6 dùng combat gần và corridor pressure để dạy đọc telegraph. Vì camera close,
encounter cuối không được spawn quá đông.

Encounter A — `Timeline`

- Sniper intro;
- enter → stop → laser aim line → fire → exit.

Encounter B — `Survive`

- debris/asteroid corridor;
- Supply Drone có thể xuất hiện giữa survive;
- player phải chọn risk/reward.

Encounter C — `Eliminate`

- Sniper + Bomber;
- không spawn quá đông vì camera close.

### Level 7 — Crossfire

Camera:

```text
6.0 normal (rộng hơn close framing của L6)
```

L7 mở rộng chiến trường và formation để giới thiệu Carrier. Crossfire cần
staggered telegraph, không tạo laser đồng thời khiến player không có đường né.

Encounter A — `Timeline`

- two Raider groups cross;
- shooting during crossing.

Encounter B — `Eliminate`

- Carrier + Interceptor escorts.

Optional Event:

- Supply Drone random/scripted tùy balance.

Encounter C — `Timeline`

- 2 Sniper enter ở vị trí khác nhau;
- staggered telegraph;
- không đồng thời laser theo kiểu unavoidable.

Encounter D MAY exist:

- Elite formation nếu pacing 12-level cần thêm challenge.

### Level 8 — Nebula Warden

Pre-boss:

- convoy/timeline;
- Supply opportunity;
- low-intensity recovery window.

Boss:

**Nebula Warden**.

Transition 2 sau boss.

## Transition Crimson → Titan

Sau L8 boss clear, khóa gameplay cho cinematic, đổi Right → Up,
chuyển framing medium sang wide rồi trả control cho L9 khi handoff hoàn tất.
Không copy exact Hyperlane animation lần đầu. Presentation thuộc
[audio/VFX](../presentation/audio-vfx.md#transition-crimson--titan).

## Region C — Titan Ring / Rift Approach `[PROVISIONAL]`

Region cuối nhấn planetary/ring scale, wide combat và asteroid/resource pressure.
Nó kết hợp các mechanics đã học, không giới thiệu quá nhiều enemy type mới;
L11 kéo framing gần lại trước final boss. Visual look thuộc
[art direction](../presentation/art-direction.md).

### Level 9 — Titan Rain

Camera:

```text
6.1
```

Encounter A — `Survive`

- large asteroid rain;
- mix diagonal + slow giant asteroid;
- golden asteroid opportunities.

Optional Event:

- Supply Drone hoặc golden meteor.

Encounter B — `Timeline`

- Interceptor fly-through;
- telegraphed dash.

Encounter C — `Eliminate`

- Bomber/Interceptor mix;
- first serious combined pressure.

### Level 10 — Open Expanse

Camera:

```text
6.8
```

L10 là wide battlefield lớn nhất trong các non-boss level seeds hiện tại, mở chỗ
cho V/Wedge, Carrier anchor cùng nhiều firing lanes.

Encounter A — `Timeline`

- large V/Wedge formations;
- Scouts/Raiders.

Encounter B — `Eliminate`

- Carrier anchor + escorts;
- multiple lanes.

Encounter C — `Timeline`

- cross formation + Sniper support;
- spacing lớn.

Có thể thêm optional Elite fly-by.

### Level 11 — Rift Approach

Camera:

```text
5.8
```

L11 kéo camera gần lại cho final gauntlet, kết hợp mechanics trước recovery
window cuối. Encounter D không nên kéo dài nếu player đã mệt.

Encounter A — `Timeline`

- Raiders + Sniper stagger.

Encounter B — `Eliminate`

- Elite + Carrier.

Encounter C — `Survive`

- hazard/debris;
- optional Supply.

Encounter D — `Eliminate` hoặc short `Timeline`

- final mixed formation;
- không dài nếu player fatigue.

Pre-final:

- quieter space;
- guaranteed supply/resource opportunity;
- music tension;
- boss warning.

### Level 12 — Void Core

Pre-boss:

- very short atmospheric setup;
- không cần thêm 3 wave trước final boss.

Boss:

**Rift Leviathan**.

## Final boss direction handoff

L12 bắt đầu vertical. Phase shift mở rift, clear projectiles theo rule,
khóa cinematic ngắn, rotate Up → Right, reposition boss rồi resume horizontal.
Exact projectile-clear scope, safe handoff timing [TBD]. Phase thresholds và attacks
thuộc [enemies/bosses](enemies-bosses.md); Final Rift feedback thuộc
[audio/VFX](../presentation/audio-vfx.md#final-rift).

## Pacing và pre-boss rules

Không đặt ba wave dài trước boss chỉ để đủ quota. Trước boss/cao trào có recovery
window và resource/supply opportunity theo level seed; L12 setup rất ngắn.
Visible common-enemy density seeds `[TUNABLE]`:

| Encounter scale | Common enemies visible cùng lúc |
|---|---|
| Early | khoảng 4–7 |
| Mid | khoảng 6–10 |
| Large formation / later | khoảng 8–12 |

Range large formation tiếp nối seed simultaneous 10–12, không phải hard cap/quota.
Tutorial như L1 A có thể ít hơn; short low-threat spectacle có thể cao hơn nếu
performance/readability cho phép. Không đổi authored encounter count chỉ để đạt
range. Visible count không phải active shooter count; permission/shot spacing thuộc
[coordination direction](#multi-enemy-attack-coordination). Formation/movement tạo
spectacle; strong attack có telegraph, không thay encounter design bằng projectile spam.
Exact spawn timestamps, count chưa nêu, formation spacing, path coordinates,
optional-event offsets và boss attack scheduler [TBD]/tuning khi làm content.
Không tự coi lựa chọn Eliminate/Timeline ở L5 C hay L11 D đã chốt.

## Checkpoint và stable dev start IDs

Checkpoint ở đầu wave/encounter, không world snapshot giữa wave. Transition/post-boss
checkpoint serialization chi tiết theo [save/profile](../systems/save-profile.md).
Stable dev start IDs:

```text
LEVEL_START
ENCOUNTER_1
ENCOUNTER_2
ENCOUNTER_3
PRE_BOSS
BOSS
POST_BOSS
TRANSITION
```

Mỗi level chỉ expose start point thật. Encounter D không tự có ENCOUNTER_4 chỉ vì
baseline liệt kê ba ID; khi implementation cần phải bổ sung contract ID có chủ ý.
Không tạo fake wave để mọi level cùng schema start point.

## Mission Replay presets

[PROVISIONAL] Preset phù hợp level, tránh late-game max loadout nghiền L1.

| Level | Loadout preset |
|---|---|
| L1–L4 | [TBD] từng level: unlocks, active weapon, branch levels, lives |
| L5–L8 | [TBD] từng level: unlocks, active weapon, branch levels, lives |
| L9–L12 | [TBD] từng level: unlocks, active weapon, branch levels, lives |

Exact preset chưa được thiết kế chốt. Replay policy thuộc
[progression/economy](../gameplay/progression-economy.md).
Enemy stats/behavior ở [enemies/bosses](enemies-bosses.md);
weapon, core và supply/drop contract ở [weapons/items/drops](weapons-items-drops.md).
