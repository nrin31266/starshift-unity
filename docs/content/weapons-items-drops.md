# Weapons, items và drops

## Phạm vi và ownership

Owner của weapon patterns/config, pickups, Weapon Core, asteroids reward classes, Supply Drone/Crate và Drop Director. Player switch/hold-to-fire/damage/death chỉ tham chiếu [player combat](../gameplay/player-combat.md).

## Weapon philosophy và common config

[LOCKED] Ba nhánh, mỗi nhánh năm level; precise sustained fire, coverage và tracking
là các vai trò khác nhau. Branch level/unlock/active selection là runtime state;
pattern/damage/cadence thuộc static weapon config.

Direction tuning `[TUNABLE]`: Lv1 không cần one-shot common enemy; upgrade phải cho
power increase rõ và clear formation nhanh/đã tay hơn. Có thể tăng damage, cadence,
coverage/count, spread, piercing hoặc tracking/priority strength theo identity từng
branch; đây là vocabulary tuning, không yêu cầu mỗi nhánh có mọi mechanic. Không
tăng enemy HP song song chỉ để giữ time-to-kill không đổi khi Player mạnh lên.
HP/DPS/clear time cần playtest cùng formation và readability, chưa chốt số final
hoặc implement upgrade mới từ direction này.

[PROVISIONAL] Common config có thể dùng các fields sau; field shape chưa khóa.

| Nhóm | Fields |
|---|---|
| Identity | id, branch, level |
| Projectile | projectile/prefab reference, damage, fireInterval, pattern, speed |
| Special/presentation | tracking/splash khi áp dụng, presentation refs |

Exact damage, interval, projectile speed/angles/range vẫn [TBD]; balance sau này là
[TUNABLE]. Không bịa DPS table để hoàn tất definition.

## Weapon system

### Primary weapon heat — M3.5

Player có một continuous runtime heat dùng chung cho cả ba weapon branch. Chỉ active branch đang được
phép bắn và giữ primary fire mới tăng heat; chỉ khi chạm capacity/MAX mới latch `Overheated`. Warning/near-hot không
chặn fire. Release trước MAX không lock: sau cooling delay heat giảm và press lại
bắn ngay. Đây là burst management, không có reload, input mới hoặc timing minigame.

Khi Overheated, chỉ primary fire bị chặn; movement và các gameplay permissions khác
không đổi. Heat giảm bằng overheat cool rate; tới recovery threshold thì mở fire,
không cần về zero. Held intent còn hợp lệ tự bắn tiếp, không yêu cầu release/press.
State/time/reset semantics thuộc [player combat](../gameplay/player-combat.md#thermal-lifecycle).

Thermal profile thuộc **weapon type**, tách khỏi progression level. Các Pulse Lv
definition sau này cùng tham chiếu profile Pulse; không tạo thermal profile riêng
cho từng level. Plasma/Homing có thể author profile khác khi được implement;
không mở thêm content/tuning của chúng trong M3.5.

Pulse seed `[TUNABLE]`, không là balance cuối:

| Field | Giá trị |
|---|---:|
| Capacity | 100 |
| Heat per second | Pulse 9; Plasma 11; Homing 8.5 |
| Cooling delay sau release | 0.30 s |
| Normal cool rate | 30/s |
| Overheat cool rate | 50/s |
| Recovery threshold | 40% capacity |

Target từ cold: Pulse 11.11 s, Plasma 9.09 s, Homing 11.76 s sustained tới MAX;
overheat recovery khoảng 1.2 s,
sai số một physics step. Burst/tap dưới MAX không có lockout. Config asset
`app/Assets/StarShift/Data/Weapons/PulseLaserThermal.asset` được tham chiếu từ Pulse
definition; asset không giữ current heat, overheat latch hoặc cooling timer.
Nhả bắn rất ngắn (dưới cooling delay) chưa xả nhiệt; nhả .5 s từ trạng thái chưa
overheat chỉ xả khoảng 6 heat. Tuning này giảm lợi thế tap/release liên tục nhưng
không rút ngắn cửa sổ sustained fire ở trên.

### Weapon count `[LOCKED]`

Ba weapon branches, mỗi branch có Lv1–Lv5 (thay đổi có chủ ý theo feedback M5). Branch identity quyết vai trò
combat; level pattern dưới đây là prototype seed, không phải final DPS balance.

### Pulse Laser

Pulse Laser cyan/blue, precise, sustained DPS ổn định và dễ đọc.
Đây là nhánh khởi đầu để người chơi hiểu fire/upgrade trước các pattern rộng hơn.

Prototype seed `[TUNABLE]`:

| Level | Pattern |
|---|---|
| Lv1 | 1 shot |
| Lv2 | twin shot |
| Lv3 | 3 tia |
| Lv4 | 4 tia |
| Lv5 | 5 tia, cadence nhanh hơn |

M2 Pulse Laser Lv1 seed `[TUNABLE]`, phục vụ VS-010/011 và chưa phải balance đã duyệt:

| Giá trị | Seed |
|---|---:|
| Fire interval | 0.17 s |
| Projectile speed | 15 world units/s |
| Damage mỗi hit | 1 |
| Projectile lifetime | 1.5 s |

Pulse placeholder dùng capsule cyan có lõi sáng, branch visual scale (.25,.52) nhân base sprite; collider width
.08 world units giữ hitbox rõ `[TUNABLE]`. Visual size không tự đổi damage hitbox.

Static asset ở `app/Assets/StarShift/Data/Weapons/PulseLaserLv1.asset` giữ các seed
và projectile prefab reference. Runtime snapshot damage/speed/lifetime khi launch;
không mutate definition để giữ cooldown hoặc current projectile state.
Continuous shooting bắn shot đầu ở physics step đầu tiên khi cả gate lẫn held
intent cho phép, giữ cadence theo scaled physics time. Khi gate chặn hoặc pointer
release thì bỏ cadence backlog, không bắn bù thành burst lúc resume. Input/fire
contract thuộc [player owner](../gameplay/player-combat.md#hold-to-fire-continuous-shooting-locked). Exact final balance vẫn [TBD]; không suy ra DPS/HP roster từ checkpoint.

### Plasma Spread

Plasma Spread purple, phủ rộng để clear formation. Tùy spread, hiệu quả giảm với
mục tiêu rất xa hoặc đơn lẻ; không tự thêm cơ chế falloff mới.

Prototype:

| Level | Pattern |
|---|---|
| Lv1 | 3-way |
| Lv2 | 4-way |
| Lv3 | 5-way |
| Lv4 | 6-way |
| Lv5 | 7-way, cadence nhanh hơn |

### Homing Missile

Homing Missile orange, cadence thấp và có tracking, phù hợp priority target/boss.
Small splash là khả năng prototype ở Lv3; exact damage và tracking chưa chốt.

Prototype:

| Level | Pattern |
|---|---|
| Lv1 | 1 missile |
| Lv2 | 2 missiles |
| Lv3 | 3 missiles + small splash |
| Lv4 | 4 missiles, splash .55 |
| Lv5 | 5 missiles, splash .65 |

### Unlock

Player bắt đầu:

```text
Pulse Laser Lv1
```

Spread/Missile unlock qua Weapon Core/reward theo progression.

Placement thuộc [levels/encounters](levels-encounters.md); loại core/reward thuộc file này.

## Item và temporary power-up

### Shield Cell

Shield Cell block một damaging hit với visual teal; không stack vô hạn.
Stacking/refresh behavior vẫn [TBD]. Absorb precedence, life và downgrade interaction
thuộc [player combat](../gameplay/player-combat.md).

### Overdrive

Overdrive tăng fire rate trong khoảng 8 s [TUNABLE]. Feedback phải cho thấy buff
đang active mà không làm projectile unreadable.

### Support Drone

Support Drone là companion bắn phụ tạm thời, duration khoảng 10–12 s [TUNABLE].
Không cần AI phức tạp; stats và buff refresh/stacking chưa được chốt.

### Emergency Missile Ammo

Thay EMP auto-use bằng reserve ammo; damage và activation thuộc
[contract tên lửa dự trữ](#tên-lửa-dự-trữ).

### Extra Life

Extra Life cộng một life tới cap theo [player combat](../gameplay/player-combat.md);
cap vẫn [PROVISIONAL].

Khi đang ở max lives:

```text
[TBD]
```

Các lựa chọn đang mở:

- convert thành Shield nếu chưa có;
- convert thành Stellar Shard;
- convert thành score bonus.

Các lựa chọn này vẫn mở; không tự chọn một conversion trong implementation.

### Weapon Core

Weapon Core unlock weapon mới hoặc tăng level đúng branch. Exact core typing vẫn
[TBD]: generic core có smart selection hay core riêng theo weapon. Reward phải đọc
được là unlock/upgrade gì; không tự chọn typing chỉ vì thuận tiện implement.

## M4-A runtime test semantics `[PROVISIONAL]`

Fixture M4 Mission 01 là [system testbed](levels-encounters.md#m4-a-mission-01-system-testbed-provisional).
Các lựa chọn dưới đủ để kiểm tra runtime; không chốt các quyết định TBD ở cuối file.
Definition assets chỉ chứa static config; không ghi level/heat/buff timer vào asset.

### Weapon progression prototype

Ba branches dùng pattern seed ở trên với năm level thực. `WeaponLoadout` giữ unlock,
active branch và level riêng; bắt đầu chỉ Pulse Lv1. Core đúng branch unlock Lv1
hoặc tăng một level tới Lv5, rồi **tự chuyển sang branch vừa nhặt**. Nút `Tn Lx` vẫn cycle các branch đã
unlock theo Pulse → Plasma → Homing. Downgrade chỉ active branch theo
[player contract](../gameplay/player-combat.md#weapon-penalty-on-death-locked).

Seeds runtime `[TUNABLE]` bổ sung cho các pattern ở trên:

| Branch | Cadence L1 / L2 / L3 / L4 / L5 | Damage mỗi projectile | Special |
|---|---|---|---|
| Pulse | .18 / .18 / .18 / .17 / .16 s | 1 | Tia mảnh/nhanh, speed 15; lateral spacing .16 world units |
| Plasma | .30 / .29 / .28 / .27 / .26 s | 1 | Speed 10; fan 30° / 36° / 42° / 48° / 54° |
| Homing | .60 / .58 / .56 / .54 / .52 s | 2 | Speed 7.5, lifetime 4 s; fan 0°/16°/24°/32°/40°; tracking 210°/s, range 18; splash L3 .45/L4 .55/L5 .65 |

Homing chọn nearest hittable target phía trước lúc launch, turn có giới hạn; chỉ
reacquire khi target cũ chết, chưa hittable hoặc instance đã được pool/reuse. Target
ngoài màn/đang entry protection không được chọn. Splash không damage trùng target chính.
Plasma lifetime 1.5 s. Ba branch có sprite placeholder riêng: Pulse capsule,
Plasma energy orb, Homing rocket có nose/fins/exhaust. Visual scale Plasma (.30,.24),
Homing (.35,.48) nhân base sprite `[TUNABLE]`; Homing có trail .13 s. Plasma xòe từ
cùng muzzle, Pulse dùng lateral spacing .16 world units; Homing xòe từ cùng muzzle. Sprite/scale/trail chỉ là presentation, không scale root/
collider. Pool reuse reset sprite, scale và xóa trail vertices trước launch mới;
Support Drone/fallback config vẫn dùng sprite gốc khi không author override.
Enemy HP không scale theo level.

Mỗi branch có một thermal profile; năm level cùng đọc profile đó. Thermal values
ở bảng thermal phía trên là tuning theo feedback, không là final balance.
Switch, unlock và upgrade giữ cùng heat/latch/cooling delay, không tạo thanh nhiệt
riêng theo branch. Capacity và cooling/recovery dùng cùng seed ở bảng trên; tốc độ
sinh nhiệt theo profile của loại đạn đang dùng. Khóa bắn không khóa clock nguội: held intent lúc gate block được
xem như không bắn. Upgrade và thả/nhấn cùng branch không reset heat/latch.
Overdrive chỉ nhân cadence 1.6× `[TUNABLE]`; heat/s vẫn theo type profile, không bypass
Overheated. Drone bắn phụ độc lập primary thermal, không mở khóa primary fire.

### Pickup và effect test policy

| Item | Semantics cho M4-A |
|---|---|
| Shield Cell | Một charge; collect lại giữ một charge, không cộng dồn. Protection gate chặn trước shield: invincibility không tiêu charge. Stacking decision vẫn TBD. |
| Overdrive | Duration seed trên; collect lại refresh duration, không stack multiplier. |
| Support Drone | Duration 11 s trong range seed trên; collect lại refresh. Một authored companion, bắn phụ 1 damage/.4 s, không elaborate AI. |
| Missile Ammo | Nhặt +1 reserve, cap 3; không tự detonate. Nút chủ động và damage policy ở phần tên lửa bên dưới. |
| Extra Life | +1 tới cap do player owner giữ. Ở cap để pickup chưa collect tới khi life giảm hoặc pickup hết scope/lifetime; không convert economy. Overflow decision vẫn TBD. |
| Weapon Core | Typed qua `PickupDefinition.Branch` cho test; code `T1+`/`T2+`/`T3+`. Maxed branch không collect/không conversion; random selection filter branch max. Generic-vs-typed và maxed fallback vẫn TBD. |
| Stellar Shard | Amount 1 `[TUNABLE]`, resource ghi Profile qua [economy owner](../gameplay/progression-economy.md); không là Score. |

Temporary effects giữ state/timer qua Spawn Group và ordinary Encounter handoff;
Pause freeze. Dying/death, Game Over, Clear, root disable và mission reset xóa Shield,
Overdrive, Drone. Respawn không restore buffs. Hit được shield absorb không chạy death
hoặc downgrade. Drone visual tắt khi gate không cho control; pending shot đã launch
vẫn theo projectile lifecycle, terminal session release friendly shots.

Pickup là pooled runtime participant trong reward scope của EncounterCleanup.
Collection dùng swept contact với đường di chuyển Player, bán kính .6 world units
`[TUNABLE]`; không magnetism. Chỉ collect khi gameplay input gate mở, kể cả khi held fire.
Power-up/crate drift xuống .8 world units/s và release khi qua viewport y -.06;
không đọng thành hàng ở đáy màn. Shard bắt đầu fall speed 3.2, gravity 11, terminal
speed 6.4 world units/s; nảy hai lần tại viewport y .06 với bounce height .06 × view
height rồi .35 × lần đầu, sau đó rơi ra và recycle, kể cả trong reward grace;
không khóa xu ở đáy hoặc kéo xu ngược lên khi bắt đầu grace. Lifetime Shard 5 s, major trực
tiếp 12 s, crate 8 s, cap active 6 `[TUNABLE]`. Relative swept contact xử lý chuyển
động cả Player và pickup, không teleport/magnetism. Đây là test policy
`[PROVISIONAL]`, không chốt lifetime/cap/magnetism ngoài phase. Full-viewport drop
clamp giữ reward đọc/nhặt được ở edge. Trong .3 s cuối lifetime/grace, symbol + code fade theo scaled time `[TUNABLE]`.
Pool reuse restore opacity. Hết lifetime/ra khỏi view không phát collection/reward.
Reward grace/cleanup thuộc [encounter owner](levels-encounters.md#reward-grace-và-chuyển-encounter).

### Drop Director test policy

`RewardDropTable` chọn Normal/Elite/MajorCrate/Resource, odds, weighted major list và
optional guaranteed pickup cho authored test moment. Normal asset dùng 2.5% major,
25% shard `[TUNABLE]`; chỉ roll shard nếu không ra major. Không loot shower toàn cục.
Elite source dùng cùng configurable table/weights; chưa author Elite combat ở M4-A.
Resource đảm bảo Shard. Supply target chết eject hai pickup trực tiếp: một Core hữu dụng
và một support item; chỉ trao effect khi nhặt, không trao khi destroy/exit. Không tăng score supply/resource khi exact
bonus vẫn TBD; score 0 tạm cho hai test targets.

Major weights theo baseline phía trên, Core weight chia đều ba branch. Filter maxed
core/Extra Life overflow; low-life defensive weight ×3, locked branch core weight ×2
`[PROVISIONAL]`. Chưa có campaign-depth policy trước M4-B. Sau hai supply low-life
không defensive, support slot tiếp chọn Shield/Extra Life; defensive hoặc roll supply khi lives
trên 1 reset history. History commit khi eject, cùng thời điểm deterministic roll. History reset theo run. Không pity trên mọi enemy kill.
Guaranteed test tables chỉ phục vụ testbed, không chốt production drop distribution.

Fixture M4 giữ seeded test overrides; production Region A dùng deterministic Encounter seed
theo [quota policy](#encounter-reward-floor-provisional). Dev/test seed không sửa asset/config hoặc disk. Đường test và release guard ở [dev owner](../systems/dev-debug.md).

## Supply Drone và Supply Crate

### Supply Drone concept `[LOCKED]`

Supply Drone là cargo craft không người lái [LOCKED], tạo nguồn gốc hợp lý cho hai pickup
thay vì hộp quà tự bay. Silhouette, material và reward accents thuộc
[art direction](../presentation/art-direction.md#asteroid-supply-drone-và-crate).

### Behavior

Drone bay qua màn hình, có HP và không cần tấn công. Việc nó rời screen tạo thời
gian săn reward tự nhiên; event có thể chạy cùng Survive/Timeline. Hai item được
nhặt trực tiếp sau ejection; bonus score của supply target theo economy owner.

```text
Drone destroyed → nổ nhẹ → Core + support eject hai phía → player nhặt từng item
```

### Crate

Production không tạo pickup `BOX` trung gian. Roll Core từ các branch còn upgrade,
roll support riêng (Shield/Overdrive/Drone/Ammo/Extra Life, vẫn filter khả dụng).
Nếu mọi branch đã Lv5, Core slot dùng support thay thế `[PROVISIONAL]`; không convert
Core đã rơi hoặc thêm currency. Hai item bật nhẹ hai phía trong .45 s `[TUNABLE]`,
sau đó drift/lifetime/scope như direct pickup; small burst không AoE/shake.
`MajorCrate` table/API cũ giữ compatibility cho fixture; production defeat path dùng pair.
SupplyCaptured ghi một lần khi nhặt item đầu tiên trong pair; item thứ hai không
ghi thêm. Không nhặt item nào thì không có capture stat.

### Scripted supply

Một số vị trí được script để người chấm chắc chắn thấy feature.

Ví dụ trước boss hoặc level cao trào.

Placement cụ thể thuộc [levels/encounters](levels-encounters.md).

### Random supply

Có thể có event random hiếm.

Baseline:

```text
8–12% per eligible level [TUNABLE]
```

Không roll mỗi wave nếu làm spawn quá thường.

## Drop rules và Drop Director

### Drop trực tiếp từ enemy phải thấp `[LOCKED]`

Enemy có thể đông nên direct major drop phải thấp, tránh loot shower làm mất
readability. Boss và resource/supply moments mới là nguồn reward nổi bật.

| Source | Drop baseline |
|---|---|
| Normal enemy | Major item 2.5%, Shard 25% nếu không ra major [TUNABLE] |
| Elite | Major item 10%, Shard 60% nếu không ra major [TUNABLE] |
| Boss | Guaranteed Core + support pair và 28 Shards rải rộng [TUNABLE] |
| Golden asteroid | Nguồn Shard rõ hơn normal enemy |

### Major reward baseline weights `[TUNABLE]`

Ví dụ khởi đầu:

| Reward | Weight |
|---|---:|
| Weapon Core | 90 tổng (30 mỗi branch) |
| Shield | 25 |
| Overdrive | 18 |
| Support Drone | 14 |
| Missile Ammo | 9 |
| Extra Life | 4 |

Không hiểu đây là % cố định; là relative weights.

### Adaptive Drop Director

Drop Director điều chỉnh relative weights theo player state để reward còn hữu ích,
không gọi là AI trong sản phẩm. Các rule điều chỉnh:

- player còn 1 life → Shield/Extra Life tăng;
- weapon chưa unlock khi đã đi sâu campaign → Core thích hợp tăng;
- weapon đã max → core branch đó weight giảm/0;
- sau nhiều supply mà không có defensive reward trong low-life state → pity.

### Pity rule

Prototype:

```text
Nếu player còn 1 life và đã eject 2 supply pair liên tiếp
mà support slot không defensive,
support slot kế tiếp ưu tiên/guarantee Shield hoặc Extra Life.
```

Exact logic `[TUNABLE]`.

### Drop clutter

Có cap active pickups để hạn chế clutter. Exact cap vẫn [TBD].

## Switch và death reference

Free switching giữa unlocked weapons, hold-to-fire gates và active weapon penalty
được sở hữu bởi [player combat](../gameplay/player-combat.md).
Checkpoint selection/levels theo [save/profile](../systems/save-profile.md).

## Stellar Shard pickup

Shard là pickup resource riêng với major items. Identity visual ở
[art direction](../presentation/art-direction.md), persistent economy ở
[progression/economy](../gameplay/progression-economy.md).

## Asteroid reward classes

| Asteroid | Contract reward/hazard |
|---|---|
| Normal | Neutral, destructible hoặc obstacle tùy size; reward theo config chưa chốt |
| Small debris | Nhanh, low HP, ít reward |
| Large asteroid | Chậm, HP cao, space pressure nhiều hơn reward |
| Golden/resource | HP cao hơn normal một chút, guaranteed Stellar Shard, không spawn dày |

Exact asteroid HP/shard amount [TBD]/tuning. Hazard collision rule thuộc player owner;
motion/placement/telegraph thuộc [levels/encounters](levels-encounters.md).

## Drop categories, lifetime và clutter

Major item, Shard và scripted progression reward phải phân biệt; Supply Crate
phải đảm bảo major reward có ý nghĩa. Normal Shard drop chỉ vài phần trăm là seed
[TUNABLE], chưa có số cố định. Major weights là relative weights, không phải % drop.
Eligible random supply levels/conditions ngoài L1 còn [TBD] khi tune content.

[TBD] Shield stacking/refresh, Weapon Core generic hay typed, maxed core fallback,
Extra Life overflow, pickup magnet, pickup lifetime ngoài crate, active pickup cap,
final boss-phase interaction của reserve missile, drone stats và buff stacking/refresh. Không tự chọn một phương án
vì dễ implement. Các giá trị duration/crate/drop/pity đã nêu giữ [TUNABLE].

## Pooling và presentation

Pickups/currency và frequent projectiles/VFX nên pool theo
[architecture](../systems/architecture.md). Pooled object phải reset lifetime/state.
Feedback từng item ở [audio/VFX](../presentation/audio-vfx.md); không dùng effect
để quyết shield absorb, missile damage hay reward selection.


## Tên lửa dự trữ

Feedback 2026-10-19 thay auto-use EMP bằng **Emergency Missile**. Đây là emergency
resource riêng, không phải nhánh primary Homing và không chịu heat/cadence primary.
Giữ serialized PickupKind slot cũ khi đổi tên sang MissileAmmo; asset rename qua
Unity giữ GUID. Testbed Encounter 4 guaranteed pickup nay cho ammo.

| Rule | Current prototype |
|---|---|
| Reserve | Cap 3; starting 1 `[PROVISIONAL]`; ammo pickup +1 |
| Activation | Bấm nút `M`; chỉ khi gameplay input mở và Encounter đang Running/ExitDrain |
| Cadence | .8 s cooldown `[TUNABLE]`, không tiêu quả tiếp do duplicate click hoặc khi còn missile đang bay |
| Launch → detonation | Flight .32 s `[TUNABLE]` theo scaled simulation; stock trừ khi launch, damage/bullet clear chỉ khi detonate |
| Hostile bullets | Clear trong cùng scene tại detonation, giữ friendly shots |
| Visible normal enemy | Destroy; không tác động enemy chưa vào màn/đã ra màn |
| Visible large enemy | 60% max HP, damage làm tròn số nguyên gần nhất |
| Visible boss | 10% max HP, damage làm tròn số nguyên gần nhất |
| Supply/resource non-hostile | Không auto-destroy để farm reward |
| Protection | .7 s brief invincibility từ launch `[TUNABLE]`, phủ cả flight; không rút ngắn window đang dài hơn |

Damage percentages và cap theo lựa chọn User cho prototype; không tự coi economy
hoặc boss-phase behavior cuối cùng đã locked. Classification normal/large/boss thuộc
[enemy owner](enemies-bosses.md#emergency-missile-target-classification).
Damage không phụ thuộc animation/camera impulse. Nhặt ở cap không consume, không
conversion; random pool filter ammo khi đầy. Activation không block enemy fire gate.

Chọn mục tiêu tại thời điểm detonation, theo combat Collider2D envelope giao với
camera frustum; địch còn ló một phần ở edge vẫn nhận damage. Nếu target không có
collider, fallback về anchor trong viewport. Không snapshot enemy list từ lúc launch.
Reserve owner giữ flight timer và generation; presentation chỉ đọc progress/events.
Pause freeze flight. Retry/Continue/NewGame/exit/terminal và run generation khác hủy
flight cũ, không cho damage trễ sang run mới; checkpoint restore stock như thường.

Nhịp phóng về tâm rồi nổ tham khảo
[mô tả của developer Chicken Invaders](https://forum.chickeninvaders.com/t/plan-update-06/395).
Ba vai bắn lấy cảm hứng ở việc phân biệt straight/spread/tracking; đây là adaptation
của StarShift, không copy asset hoặc claim cùng formula/timing/balance CIU. Cap và
damage hiện tại tiếp tục `[PROVISIONAL]`/tunable theo playtest, không chốt vì đã ghi docs.

Reserve là campaign runtime state: ordinary death giữ số còn lại; checkpoint capture/
Retry/Continue restore theo [save owner](../systems/save-profile.md). NewGame/Replay/
Dev entry có preset riêng. Không persist cooldown, burst hoặc shake. Visual/audio
cue thuộc [presentation owner](../presentation/audio-vfx.md#reserve-missile-impact-cue).


## M5 authored reward usage `[PROVISIONAL]`

Normal/Elite random odds được tăng nhẹ như policy phía trên; quota bên dưới bổ sung floor progression cho
combat quan trọng thay vì gắn guaranteed Core lên một SpawnAction. M3 Supply và M4
preboss Supply dùng MajorCrate table để eject pair; resource asteroid giữ Shard path; Vanguard eject
một defeat pair sau staged death, kèm 28 Shards rải x .07–.93, y .58–.85 viewport.
Chỉ enemy được classify Boss mới dùng `BossShardCount`; supply không rải vàng. Boss
gold dùng visual .55, initial fall 1.2, gravity 4.5, max speed 3 `[TUNABLE]`.
Collection grace/timing thuộc [level owner](levels-encounters.md#m5-region-a-authored-content-provisional). Escape của Supply/resource không nhận reward.
Mỗi item vẫn cần pickup, không auto grant; Boss không tiêu common quota.
Availability filter chỉ xét capacity/loadout, độc lập input gate: enemy/supply bị hạ
khi Player đang respawn vẫn eject đồ; collection chờ gate mở như bình thường.

## Encounter reward floor `[PROVISIONAL]`

`EncounterDefinition.GuaranteedMajorCount` và `GuaranteeTable` author minimum drop
opportunity. Production M1 Foundation floor **1**, Formation **2**; M2 Raider/Bomber
và M3 Sine/Pincer floor **1**; M3 Elite **2**. Tổng authored combat floor là 9. Foundation guarantee TYPE là Pulse Core;
các encounter còn lại draw từ ProgressionDrops (existing major weights, riêng Core
weight 90 mỗi branch `[TUNABLE]`, giữ state filters/locked-branch bias). Storm/Boss có floor 0;
Supply, resource và Boss defeat pair là reward paths độc lập.

Runtime `RewardDirector` đếm eligible normal/Elite enemies, không gồm Boss,
Supply/resource/normal asteroids. Seed dùng stable Mission ID + Encounter ID + base
seed 410; không dùng frame clock hoặc process-random string hash. Dev reward seed
chỉ thay runtime base seed. Cùng checkpoint/seed/player actions tái tạo normal roll,
major selection và kill ordinal; enemy source không được author cố định.

Sau mỗi eligible kill: normal roll trước; major phù hợp đã ra thì credit floor.
Nếu chưa đạt, seeded remaining-kill selection chọn xác suất `remaining quota /
remaining eligible population`, bắt buộc ở các kill cuối. Mỗi kill emit tối đa một
selected direct item; quota không cộng thêm một major cạnh normal major vừa credit.
Normal random drops sau khi đạt floor vẫn giữ odds đã author. Normal 2.5% major,
Elite 10%; Shard Normal 25% / Elite 60% nếu major roll không ra; Core weight mỗi branch lần lượt 30/50, Progression 90 `[TUNABLE]`.
Pity chỉ áp dụng support slot khi eject Supply/Boss pair, không chạy thêm cho quota item.

Timeline fallback: nếu có ít nhất một eligible enemy đã bị hạ nhưng phần còn lại
escape/hết cap, completion resolve phần floor thiếu tại vị trí của killed candidate
cuối. Không kill nào thì không free reward. Floor là dropped opportunity, không
phải auto collection hoặc bảo đảm đủ upgrade sau một run.

Normal pickup clutter cap giữ 6 `[TUNABLE]`; authored floor và guaranteed Supply/Boss
pair được cấp reserved overflow slot khi cap đã đầy để không mất guaranteed item. Không force thêm theo
mỗi enemy; production combat floor 1–2 theo bảng trên. Các pickup còn lại vẫn chịu normal
cap/lifetime/scope; kiểm tra actual density trước khi tăng quota. Đây là thay đổi
prototype có chủ ý, không khóa final economy hoặc maxed-core conversion.

Retry/Continue dựng lại seed/quota từ Encounter start, không snapshot loot giữa
wave. Collected Core/loadout rollback cùng checkpoint; Shard high-water và Replay/
DevTest write permission giữ theo [save owner](../systems/save-profile.md).
Typed Core vẫn `[PROVISIONAL]`; Shield stacking, overflow và final replay economy
còn TBD. Evidence và giới hạn test ở [dev/debug](../systems/dev-debug.md#m5-second-human-feel-pass-verification).
