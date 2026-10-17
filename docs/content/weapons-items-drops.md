# Weapons, items và drops

## Phạm vi và ownership

Owner của weapon patterns/config, pickups, Weapon Core, asteroids reward classes, Supply Drone/Crate và Drop Director. Player switch/hold-to-fire/damage/death chỉ tham chiếu [player combat](../gameplay/player-combat.md).

## Weapon philosophy và common config

[LOCKED] Ba nhánh, mỗi nhánh ba level; precise sustained fire, coverage và tracking
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

### Weapon count `[LOCKED]`

Ba weapon branches, mỗi branch có Lv1, Lv2 và Lv3. Branch identity quyết vai trò
combat; level pattern dưới đây là prototype seed, không phải final DPS balance.

### Pulse Laser

Pulse Laser cyan/blue, precise, sustained DPS ổn định và dễ đọc.
Đây là nhánh khởi đầu để người chơi hiểu fire/upgrade trước các pattern rộng hơn.

Prototype seed `[TUNABLE]`:

| Level | Pattern |
|---|---|
| Lv1 | 1 shot |
| Lv2 | twin shot |
| Lv3 | twin + center shot / stronger center |

M2 Pulse Laser Lv1 seed `[TUNABLE]`, phục vụ VS-010/011 và chưa phải balance đã duyệt:

| Giá trị | Seed |
|---|---:|
| Fire interval | 0.2 s |
| Projectile speed | 12 world units/s |
| Damage mỗi hit | 1 |
| Projectile lifetime | 1.5 s |

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
| Lv2 | 5-way |
| Lv3 | 5-way stronger / center-enhanced |

### Homing Missile

Homing Missile orange, cadence thấp và có tracking, phù hợp priority target/boss.
Small splash là khả năng prototype ở Lv3; exact damage và tracking chưa chốt.

Prototype:

| Level | Pattern |
|---|---|
| Lv1 | 1 missile |
| Lv2 | 2 missiles |
| Lv3 | 3 missiles + small splash |

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

### EMP

EMP clear hostile bullets và gây damage pulse lên enemy. Effect mạnh nhưng ngắn,
không one-shot boss; exact damage/radius vẫn [TBD].

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

## Supply Drone và Supply Crate

### Supply Drone concept `[LOCKED]`

Supply Drone là cargo craft không người lái [LOCKED], tạo nguồn gốc hợp lý cho crate
thay vì hộp quà tự bay. Silhouette, material và reward accents thuộc
[art direction](../presentation/art-direction.md#asteroid-supply-drone-và-crate).

### Behavior

Drone bay qua màn hình, có HP và không cần tấn công. Việc nó rời screen tạo thời
gian săn reward tự nhiên; event có thể chạy cùng Survive/Timeline. Major item được
trao qua crate pickup sau ejection; bonus score của supply target theo economy owner.

```text
Drone destroyed → crate ejected → crate drift → player pickup
```

### Crate

Crate drift trong khoảng 6–8 s [TUNABLE], đủ để player có cơ hội pickup.
Crate guaranteed major reward có ý nghĩa; không trả reward “rác” sau khi player
đã săn được drone.

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
| Normal enemy | Major item khoảng 0.5–1% [TUNABLE]; Shard thấp, khoảng vài % |
| Elite | Major item khoảng 5–8% [TUNABLE] |
| Boss | Guaranteed major reward/crate |
| Golden asteroid | Nguồn Shard rõ hơn normal enemy |

### Major reward baseline weights `[TUNABLE]`

Ví dụ khởi đầu:

| Reward | Weight |
|---|---:|
| Weapon Core | 30 |
| Shield | 25 |
| Overdrive | 18 |
| Support Drone | 14 |
| EMP | 9 |
| Extra Life | 4 |

Không hiểu đây là % cố định; là relative weights.

### Adaptive Drop Director

Drop Director điều chỉnh relative weights theo player state để reward còn hữu ích,
không gọi là AI trong sản phẩm. Các rule điều chỉnh:

- player còn 1 life → Shield/Extra Life tăng;
- weapon chưa unlock khi đã đi sâu campaign → Core thích hợp tăng;
- weapon đã max → core branch đó weight giảm/0;
- sau nhiều crate mà không có defensive reward trong low-life state → pity.

### Pity rule

Prototype:

```text
Nếu player còn 1 life và đã mở 2 major crate liên tiếp
mà không nhận defensive reward,
crate phù hợp tiếp theo ưu tiên/guarantee Shield hoặc Extra Life.
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
EMP damage/radius, drone stats và buff stacking/refresh. Không tự chọn một phương án
vì dễ implement. Các giá trị duration/crate/drop/pity đã nêu giữ [TUNABLE].

## Pooling và presentation

Pickups/currency và frequent projectiles/VFX nên pool theo
[architecture](../systems/architecture.md). Pooled object phải reset lifetime/state.
Feedback từng item ở [audio/VFX](../presentation/audio-vfx.md); không dùng effect
để quyết shield absorb, EMP damage hay reward selection.
