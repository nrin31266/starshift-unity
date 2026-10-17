# Levels và encounters

## Phạm vi và ownership

Owner của 12 level, encounter composition, paths/formations, FlightSequence usage, camera profiles, directions, transitions và checkpoint placement. Không sở hữu enemy HP hoặc weapon damage.

## Terminology và composition

Taxonomy `[LOCKED]` theo cách gọi đã chốt; Campaign chứa các Region / Planet và
Mission, không tính là một cấp trong bảng này.

| Cấp | Tên dùng | Ý nghĩa |
|---|---|---|
| 1 | **Region / Planet** | Hành tinh / khu vực lớn. Region có thể là không gian lớn hơn một hành tinh; không mặc định hai khái niệm là một-một. |
| 2 | **Mission** | Một level hoàn chỉnh. “Level” là alias kỹ thuật hiện có, không phải cấp khác. |
| 3 | **Encounter** | Một đoạn combat có điều kiện kết thúc riêng. |
| 4 | **Spawn Group** | Một nhóm quái được gọi ra cùng logic/thời điểm. |
| 5 | **Formation** | Cách Spawn Group đó xếp đội hình. |
| 6 | **Flight Sequence / Path** | Cách nhóm/quái bay vào, giữ vị trí, di chuyển, bay ra. Sequence chứa các phase; Path là hình đường bay trong phase. |

Không gộp Spawn Group và Formation thành một tên. Các cấp 4–6 mô tả composition
của cùng nội dung, không tạo thêm completion/checkpoint gate: một Encounter có nhiều
Spawn Group; mỗi group chọn Formation và Flight Sequence / Path. Một lần quái bay
vào hoặc đổi Formation không mặc định là Encounter mới. “Wave” trong tài liệu cũ là
alias Encounter, không thêm cấp riêng. Tên hiển thị tạm dùng taxonomy + số/ID để debug;
chưa cần fiction name hoa mỹ. Tên Region/Planet thực tế còn `[TBD]` thì giữ config
trống, không tự đặt tên. Mapping runtime và field serialization thuộc
[architecture](../systems/architecture.md#config-data-và-mutable-runtime-state).

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

Timeline đóng spawn/attack theo schedule; enemy còn sống được exit/despawn.
Nếu còn shot đã bắn đang visible, đợi chúng contact hoặc ra khỏi camera trước
completion/handoff. Đây không phải kill gate hoặc carry threat sang encounter sau.
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

`[PROVISIONAL]` product direction. Local enemy
attack biết thực hiện attack khi được phép; encounter layer quyết lúc nào và
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
VS-015 là single-enemy sandbox với
[local fire profile](enemies-bosses.md#scout-single-fire-checkpoint). VS-021 thêm
[shared safety gate](../systems/architecture.md#m2-player-damage-lifecycle) cho
death/respawn/Game Over. VS-022/023 thêm coordination tối thiểu cho L1:
round-robin trong các Scout eligible, chỉ một telegraph/attack đang được cấp quyền,
không bắt buộc mọi Scout visible phải bắn. Local initial delay/cooldown vẫn là lower
bound; coordinator không bỏ qua safety gate hoặc tạo backlog sau pause/grace.

Seed `[TUNABLE]` trong hai encounter assets: A một shot rồi nghỉ 1 s; B hai shot
luân phiên trong một volley, cách nhau ít nhất 0.55 s **sau shot/cancel trước**,
rồi nghỉ 1.8 s. Mỗi shot vẫn có telegraph riêng theo fire profile; đây là group
burst của Scout, không thêm burst weapon hoặc Raider archetype. Thuật toán còn
`[PROVISIONAL]`, chưa khóa thành Fire Director/threat-budget framework.

## Encounter lifecycle và handoff

Completion xác nhận mục tiêu encounter đã xong; handoff chỉ bắt đầu encounter tiếp
khi các object cũ đã được resolve theo contract dưới đây. Optional Event Layer
không thêm kill/pickup requirement vào completion. Reward grace là khoảng nghỉ
có giới hạn sau completion, không phải điều kiện phải nhặt hết reward.

### Completion theo mode

| Mode | Điều kiện completion và xử lý enemy còn sống |
|---|---|
| Eliminate | Required enemies phải chết trước khi complete; không despawn chúng để giả hoàn thành. Non-required enemy còn sống đi qua cleanup chung. |
| Timeline | Enemy đã exit được despawn. Khi timeline kết thúc, đóng spawn/attack/collision enemy và release chúng. Shot đã bắn còn visible được bay tiếp tới contact/view exit trong cửa sổ có upper bound theo view diagonal/speed; sau đó mới complete và cleanup. Không handoff threat active sang encounter sau. |
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
kill/boss death đã hiện và player có thể điều khiển để nhặt. Duration là [TUNABLE]; tại boundary, bảo đảm pickup/crate còn ít nhất khoảng grace này nếu
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
M3 grace seed khi có registered reward là 2 s `[TUNABLE]`, timer scaled;
không có reward thì bỏ qua. Hook chỉ preserve/expire qua consumer, không grant.
Optional hooks nhận unique ID/type, offset, condition (always/player alive/no live
enemies) và optional spawn group. Condition pending chỉ sống trong main schedule;
optional groups không trở thành required set. Boss boundary có token và bounded
post-death fallback seed 1 s `[TUNABLE]` cho stub; content boss thật nằm ngoài phase này.

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

`EntryHoldFlight` của M2 tiếp tục làm pose writer; L1 M3 dùng composable sequence.
Existing sandbox/verification `Configure(entry,hold,...)` vẫn có đường legacy, không
là content authoring chính. Collision envelope giữ hold target trong full viewport;
Player restricted region không thu hẹp enemy. Zoom không restart phase clock.

### M3 flight và formation authoring

`[PROVISIONAL]` implementation vocabulary cho ENC-004–011: ordered Entry, Move,
Hold, Combat, Cross, Exit, không giới hạn ba phase. Phase `CanShoot` chỉ tạo
eligibility, vẫn qua coordinator/safety gate; path không sở hữu attack timing.
Required target trong Eliminate/Boss phải kết thúc bằng terminal Hold/Combat;
finite sequence không được despawn required target để giả kill. Non-required hoặc
timed enemy resolve khi sequence kết thúc/exit envelope ra khỏi view.

Implemented path primitives: Straight, DiagonalPass và Cross dùng endpoints;
ArcLeft/ArcRight và Pincer/ExitCurve dùng curve envelope về zero ở hai đầu;
Sine dùng bounded wave envelope; Dive đi vào rồi return về endpoint;
Loop/Orbit dùng integer cycle count, finite duration. Hold/Combat có thể giữ endpoint.
Đây là reusable movement vocabulary, không bao gồm final threat tuning hay boss AI.
Endpoints giữa phases phải liên tục; loop không có clock/coroutine vô hạn.

Formation là offsets độc lập khỏi archetype/path: Custom (giữ L1 slots), Line,
Column, V, Wedge, Staggered, TwinFlank, Cross, Escort và Ring. Count/spacing/rise
là config. Cross/Pincer composition dùng nhiều nhóm/phase, không có graph editor.
Readability của pressure trong content sau vẫn cần playtest với attacks/hazards thật.

Up là local normalized viewport; Right map `(x,y)` thành `(y,1-x)` và offsets thành
`(y,-x)`. Spawn edge, formation, flight và respawn cùng transform, không tạo hai
bộ paths hoặc rotate Android orientation. Player/muzzle face forward tương ứng;
screen-relative drag/bounds vẫn dùng camera hiện tại.

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
vừa exit vừa bắn và rời screen sống sót. Chỉ được phát shot khi shooter và muzzle
còn trong camera gameplay; ra ngoài phải hủy pending attack, không bắn ngược từ
ngoài màn hình vào Player. Timeline enemy đã exit hoàn toàn được release, không
tính kill. Completion theo timeline không tắt combat behavior khi enemy còn visible;
không giới hạn enemy vào fly-through im lặng.

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
M3 apply profile/direction qua [runtime foundation](../systems/architecture.md#m3-level-và-encounter-runtime-foundation).

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

## Mission start và establishing intro

`[TUNABLE]` Level start chạy short establishing approach trước encounter đầu.
Major establishing (thường khi vào/đổi Region / Planet) dùng title/location và mô tả,
chờ một valid tap sau reading delay rồi approach/travel/brake. Đây là major
cinematic: input/fire/damage khóa, chưa spawn formation, không manual Pause. Timers
dùng scaled simulation; không freeze global clock khi chờ tap. Sau
camera/HUD blend trong Brake, mở control ngay khi Brake kết thúc rồi mới hiện Mission card trên gameplay.
Mission label và Encounter đầu nằm cùng card căn giữa; Mission không có mô tả,
không chèn thêm một announcement Encounter đầu riêng. Card cần hoàn tất trước first
spawn; card không khóa control/Pause. Timing/copy theo presentation owners bên dưới. Encounter tiếp theo trong cùng
khu vực là minor announcement, không thêm confirmation hoặc cinematic lớn. Handoff lớn/nhỏ phân biệt theo
thay đổi không gian và choreography, không theo số Spawn Group/Formation.
Encounter jump/standalone có thể bỏ intro, không giả checkpoint mới.
Choreography/timing và placeholder thuộc
[audio/VFX](../presentation/audio-vfx.md#level-intro-warning-và-dynamic-audio);
copy/announcement thuộc [UI/UX](../presentation/ui-ux.md#vocabulary-và-announcement).

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

#### M2 L1 playable checkpoint

VS-022/023 dùng Scout cho cả hai nhóm B; Raider-light vẫn là lựa chọn content về sau.
Seeds dưới là `[TUNABLE]`, được author trong `Data/Encounters/L1EncounterA.asset`
và `L1EncounterB.asset`; không phải final multi-enemy balance.

| Encounter/group | Spawn | Viewport formation và flight |
|---|---|---|
| A | 3 Scout, stagger 0.35 s | Entry anchor (0.5, 1.12) → hold (0.5, 0.78), horizontal slot step (0.18, 0), entry 0.9 s; hold tới death. |
| B / cánh trái | 3 Scout tại 0 s, stagger 0.25 s | Anchor (-0.24, 0.78) → (0.32, 0.72), slot step (0.13, 0.065), soft curve (0.1, 0.12); entry 3.2 s → hold 3 s → exit anchor (-0.28, 0.42) trong 7.5 s. |
| B / cánh phải | 3 Scout tại 0 s, stagger 0.25 s | Anchor (1.24, 0.78) → (0.68, 0.72), slot step (-0.13, 0.065), soft curve (-0.1, 0.12); entry 3.2 s → hold 3 s → exit anchor (1.28, 0.42) trong 7.5 s. |

Sau feedback playtest, B dùng hai cánh vào chồng nhịp từ trái/phải rồi ghép thành
một đội hình sáu Scout, tránh hai đợt rời nhau. Entry, hold và exit có duration riêng.
Hai cánh có cùng clock/stagger và thứ tự slot đối xứng theo từng cặp.
Exit chậm cho Player thêm thời gian bắn. Hold ngắn để lặp manual test nhanh; đây
vẫn là seed test, chưa chốt balance.

Slot offset được tính quanh tâm nhóm và áp cùng các anchor. Entry curve là độ lệch
normalized tại giữa entry, về zero ở hai đầu. Reproject bằng current camera ở từng
physics/render frame, giữ collision envelope của hold trong full viewport; playable
region của Player không thu hẹp formation. B được bắn trong hold/exit khi có quyền,
không bắn trong entry, ngoài camera hoặc sau exit. Khi exit, release ngay sau khi
collision envelope ra khỏi full camera; không chờ hết duration ở ngoài màn hình.
Shot đã phát hợp lệ trong view tiếp tục bay theo lifecycle projectile hiện có.

A chỉ complete khi toàn bộ enemy đã schedule có HP về zero; disable/release không
được tính kill. B đóng schedule tại 14 s kể cả enemy sống hoặc đã exit hết, rồi
đợi shot cũ còn visible ra khỏi view/contact trước completion; không xóa đạn giữa
không trung chỉ vì nguồn bắn đã exit hoặc schedule hết giờ.
Tại normal completion sau projectile drain, đóng spawn và attack, release enemy và
hostile projectile trước notification/handoff; explicit reset clear ngay. Cleanup không gửi damage/death để giả kill.
Khoảng handoff A → B là 1.25 s `[TUNABLE]`; đây là minor gameplay transition,
giữ HUD/Pause và permissions theo gameplay/lifecycle bình thường. Cue nền nhẹ và
optional contextual title/description tự fade; không menu hoặc chờ confirmation.
Incoming encounter giữ optional TransitionTitle/TransitionDescription; để trống thì
không announcement. Lifecycle death/respawn vẫn tiến theo state/timers cũ, không bị
reset/ghi đè bởi handoff. HUD chỉ ẩn cho major cinematic, theo
[UI owner](../presentation/ui-ux.md#m2-ui-implementation-provisional). L1 chưa có reward nên không mở reward
grace. M3 có bounded grace hook khi future consumer register reward, không fake pickup. Death/respawn không reset encounter hoặc survivor HP; timeline/movement vẫn
tiến theo scaled time, safety gate chỉ chặn attack. Game Over hủy schedule/handoff.
Sau B full mission dừng combat và hiện compact clear theo
[UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional); reward/save và next-level flow chưa implement.

Ownership/runtime và pooling ở
[architecture](../systems/architecture.md#m2-l1-encounter-runtime);
jump/replay và verification ở [dev/debug](../systems/dev-debug.md#m2-l1-fast-start-và-encounter-verification).

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
