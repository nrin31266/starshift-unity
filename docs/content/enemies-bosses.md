# Enemies và bosses

## Phạm vi và ownership

Owner của archetype identity, HP, behavior, damage states, Elite và boss mechanics. Không chứa exact level spawn schedule; placement ở [levels/encounters](levels-encounters.md).

## Enemy principles và common contract

Ít archetype, tái dùng nhiều path/formation/fire sequence. Silhouette và telegraph
phải cho biết vai trò; không chỉ recolor cả roster. Enemy runtime có HP/state/timer,
nhưng static definition giữ balance/config theo [architecture](../systems/architecture.md).
Movement path tách firing behavior; completion/exit policy do encounter owner quyết.
Khi chết chỉ resolve reward/death hook một lần, sau đó release pooled state khi có pool.
Không để presentation quyết damage hay completion.

## Enemy HP và damage state

### Enemy dùng HP `[LOCKED]`

Enemy có HP và có thể nhận nhiều viên đạn trước khi chết. Không mặc định mọi enemy
one-shot; survival model này khác life-based player.

### Damage state

Damage state giúp player đọc mức tổn thương mà không cần HP bar cho mỗi enemy.
Enemy nhỏ SHOULD có hai state:

```text
Normal
Damaged
```

Ví dụ enemy có HP 2: full HP dùng Normal, hit đầu chuyển sang Damaged frame/effect,
hit tiếp chết. Không yêu cầu một frame riêng cho mỗi damage point.

Scout checkpoint dùng ngưỡng `HP / max HP <= 50%` `[TUNABLE]` cho persistent
Damaged: với seed hiện tại, 4/3 HP còn Normal, 2/1 HP là Damaged. Mỗi hit vẫn có
flash ngắn độc lập; flash kết thúc mới đọc state này. Threshold nằm trên
`EnemyDamagePresentation`, không thay HP/death authority.

Enemy to SHOULD có:

```text
Normal
Damaged
Critical
```

M8 production dùng ngưỡng visual Critical tại `HP / max HP <= 25%` `[TUNABLE]`
cho Bomber, Bulwark, Carrier, Vanguard và Warden core. Đây chỉ là
`EnemyDamagePresentation`; không đổi HP, damage, phase hoặc death authority.

Có thể dùng alternate sprite/frame, smoke, sparks và cracked emissive để biểu diễn
state; exact presentation ở [audio/VFX](../presentation/audio-vfx.md).

### Không floating HP bar cho quái thường

HP bar dành cho boss. Elite đặc biệt có thể dùng nếu thực sự cần, nhưng default
là không; enemy thường đọc state qua silhouette/damage feedback.

### Hit feedback

Projectile hit báo feedback và cập nhật damage state; death resolve một lần.
Choreography flash/particle/SFX/shake thuộc [audio/VFX](../presentation/audio-vfx.md).

## M4-A non-attacking test targets `[PROVISIONAL]`

Supply Drone dùng HP 6, Resource Asteroid dùng HP 2 `[TUNABLE]` trong shared
prefabs. Cả hai reuse health/flight; attack disabled. Supply body không damage Player;
resource asteroid body qua damaging contact gate. Không thêm archetype attack, boss
hoặc production art mới. HP của Scout giữ nguyên khi Player upgrade.
Placement ở [testbed owner](levels-encounters.md#m4-a-mission-01-system-testbed-provisional),
reward/destroy/escape ở [item owner](weapons-items-drops.md#drop-director-test-policy).

## Enemy roster

Exact HP/stats là [TUNABLE]. Bảng balance seed bên dưới là baseline prototype.

### Scout Drone

Common formation enemy để học aim/dodge và cảm nhận sức mạnh weapon: thường xuất hiện
theo nhóm, tái dùng nhiều path với tốc độ vừa và single shot đơn giản. Movement và
formation tạo variety; không mặc định mọi Scout visible đều là active shooter.
Scout dùng Normal/Damaged; score thấp theo
[economy owner](../gameplay/progression-economy.md).

VS-012–014 checkpoint có production Scout prefab nhận Pulse hit qua
`IPlayerProjectileTarget`. HP lấy seed từ balance table bên dưới, mutable HP nằm
trên instance. Persistent Damaged theo threshold bên trên; lethal hit khóa nhận damage trước
death hook, resolve một lần rồi disable root để dừng collider/movement. Không có
floating HP bar, score/drop hoặc enemy firing trong checkpoint này.

Presentation chỉ đọc HP/event, không quyết sống/chết. Disable/re-enable object reset
HP, damage cue và flight state; chưa có enemy pool/spawn scheduler. Structural API
thuộc [architecture](../systems/architecture.md#m2-scout-baseline-provisional),
entry/hold seed thuộc [flight owner](levels-encounters.md#scout-entryhold-checkpoint),
flash choreography thuộc [audio/VFX](../presentation/audio-vfx.md#enemy-damage-feedback).

#### Scout single-fire checkpoint

VS-015 thêm repeated single shot sau Hold. Flight chỉ cung cấp phase; fire profile
giữ timer riêng: initial delay → telegraph → một shot → cooldown → telegraph tiếp.
Không fire trong Entry, khi health/flight inactive hoặc target inactive/mất reference.
Disable attack/root hoặc chết hủy pending telegraph; re-enable bắt đầu delay mới,
root re-enable còn chạy lại Entry. Không bắn bù backlog.

Seed sandbox **một enemy** `[TUNABLE]` của `Data/Enemies/ScoutSingleFire.asset`:

| Thông số | Seed |
|---|---:|
| Initial delay sau Hold | 0.6 s |
| Telegraph trước mỗi shot | 0.4 s |
| Cooldown từ shot đến telegraph kế tiếp | 1.8 s |
| Projectile speed | 4 world units/s |
| Projectile lifetime | 6 s |

Đây là local attack cycle khi được phép attack, không phải cadence policy cho cả
formation. Không nhân cadence này thành mọi Scout tự fire độc lập khi có nhiều
enemy. Coordination direction và controlled selection thuộc
[encounter owner](levels-encounters.md#multi-enemy-attack-coordination); per-shooter
coordination được implement tối thiểu ở VS-022/023. Shared safety gate của lifecycle không thay policy đó.

Shot snapshot hướng từ Muzzle tới vị trí Player **lúc fire**, không khóa aim từ lúc
telegraph và không steer sau launch. Direction/speed không phụ thuộc camera size
hoặc aspect. Không burst, spread, homing hoặc predictive aim. Đạn đã launch tiếp tục
đến contact/lifetime kể cả Scout bị disable/chết. Lifetime là fallback khi ngoài
view; nếu hết lifetime mà shot vẫn visible thì tiếp tục tới view exit hoặc contact,
không đổi hướng/tốc độ để ép kịp timer. Minor handoff áp grace/safe fade trước
completion; cleanup/reset theo [handoff contract](levels-encounters.md#encounter-lifecycle-và-handoff).

Pause dùng scaled time theo [architecture](../systems/architecture.md#pause-behavior).
`PlayerGameplayGate` chỉ cấp permission cho Player; không suy enemy permission từ
`CanFire`/`CanTakeDamage`. VS-021 thêm shared `EnemyFireGate` cho death/respawn grace
và Game Over; Scout reset pending telegraph khi authority block, resume với local
delay mới. Encounter-controlled Scout chỉ bắt đầu telegraph khi coordinator cấp
quyền, có thể single fire trong finite exit của L1 B. Transition/Result orchestration
vẫn chưa có. Boundary thuộc
[architecture](../systems/architecture.md#m2-player-damage-lifecycle).
Projectile/contact/pool boundary thuộc
[architecture](../systems/architecture.md#m2-scout-single-fire-provisional), core cue thuộc
[audio/VFX](../presentation/audio-vfx.md#scout-fire-telegraph).

### Raider

Raider tạo movement pressure bằng sine/arc/cross path, di chuyển nhiều và bắn burst
ngắn. Nó có thể vừa bay vừa bắn, không bắt buộc đứng yên để attack.

### Bomber

Bomber là tank vừa, chậm hơn và dễ nhìn, tạo spread pressure bằng 3/5-way fire.
Reward class và score cao hơn Scout; exact score thuộc
[economy owner](../gameplay/progression-economy.md).

### Sniper

Sniper ép player reposition bằng telegraphed threat. Sequence baseline là
`enter → hold → aim line → charge → fire beam → exit`; không được fire beam thiếu
báo trước. Telegraph timing seed nằm ở section fire patterns bên dưới.

### Interceptor

Interceptor là fast threat gây pressure gần player. Tracking bị giới hạn và dash
phải có telegraph; không dùng homing vô hạn để đuổi player.

### Carrier

Carrier lớn, chậm, đóng vai trò battlefield anchor và priority target. Nó spawn
micro-drone/Scout-like units, có defensive/secondary fire nhẹ để tạo pressure trong
lúc player quyết định ưu tiên mục tiêu.

### Elite modifier

Elite là modifier, không cần sprite hoàn toàn mới. Nó tăng threat và reward class
nhưng không thay toàn bộ role của archetype gốc. Baseline modifiers:

- HP multiplier theo balance seed bên dưới;
- fire interval nhanh hơn ~15–25% `[TUNABLE]`;
- scale ~1.10–1.15;
- amber/gold accent hoặc distinct emissive;
- reward class tốt hơn.

Không biến mọi wave cuối thành toàn elite.

## Minion

Carrier sinh micro-drone/Scout-like units. M6 đã author bounded minion contract
[ở runtime mechanics](#m6-region-b-runtime-mechanics-provisional); các future minion
variants vẫn [TBD], không tự copy Scout stats thành locked contract.

## Balance seed table

Tất cả HP là [TUNABLE], không phải final balance.
Scout fixture seed giữ bốn hit Pulse Lv1; production Region A override theo [runtime seeds](#m5-runtime-seeds-tunable).
Damage state theo [threshold](#damage-state), không coi đây là HP campaign cuối cùng;
retune theo [weapon power progression](weapons-items-drops.md#weapon-philosophy-và-common-config).

| Archetype | HP seed |
|---|---:|
| Scout | 4 |
| Raider | 3 |
| Bomber | 6 |
| Sniper | 4 |
| Interceptor | 4 |
| Carrier | 10 |
| Minion | [TBD] |
| Elite modifier | ×1.6–1.9; điểm bắt đầu xấp xỉ ×1.75 |
| Boss / shield node | [TBD] exact HP, cần tune theo weapon damage và time-to-kill |

Elite fire interval nhanh hơn là seed, cần xác định biểu diễn multiplier khi implement;
không mặc định mọi wave cuối toàn Elite. Reward selection theo
[weapons/items/drops](weapons-items-drops.md); score values theo
[progression/economy](../gameplay/progression-economy.md).

## Fire patterns và telegraph

Single shot, short burst, 3/5-way spread, charged beam, limited tracking/dash,
secondary fire và minion summon theo role. Strong attack MUST có báo trước:
Sniper aim line khoảng 0.6–0.9 s [TUNABLE], dash có pause/engine glow,
boss beam có charge. Không beam one-frame, không homing vô hạn.
Khi nhiều Sniper, stagger telegraph theo encounter, tránh unavoidable simultaneous laser.

## Boss common contract và collision

Boss có HP bar/name, readable hit area, armor/core/module states khi mechanic cần.
Completion chỉ sau defeat và post-death sequence, không ngay HP chạm zero.
Body/beam damaging hit resolve qua [player combat](../gameplay/player-combat.md).
Exact contact hit area, node HP, invulnerability windows, damage multipliers,
phase scheduler và duration chưa được thiết kế chốt: [TBD].

## Boss 1 — Vanguard Frigate `[PROVISIONAL]`

### Role

Vanguard là boss giới thiệu, dùng ít mechanic để player học đọc volley và phase
change trước khi campaign tăng target-priority pressure.

### Silhouette và movement `[TUNABLE]`

Capital-ship placeholder ghép central hull/reactor, hai cánh, engine masses, twin
cannon pods, missile pod và beam pod; không có final raster art. Width 14 world
units tại camera 9 tương ứng ~43.75% khung 16:9 / 35% khung 20:9. Compound body
hitboxes theo thân/cánh; disabled movement envelope chỉ dùng để giữ footprint trong view.
`EntryHoldFlight` vẫn là pose writer. Settled anchor (.5,.73), repeating 8 s sine
với curve (.12,.012), ở upper combat zone. Không dive vào Player start.

### Phase structure `[TUNABLE]`

HP 680, không scale theo Player weapon. Phase A lần lượt projectile curtain → twin cannon
→ missile fan → twin cannon, rest 1.9 s. Cannon 3 viên/pod ×2 pod (fan 38°), missile 5 viên/fan 62°.
Cannon telegraph .55 s, missile 1 s. Telegraph xuất phát
từ các pod đang fire; hai cannon cùng cue.

Phase B tại 50% HP: cancel attack hiện tại, core/engine cue 1.5 s, reposition offset
(-.06,.015) qua flight owner và motion envelope ×1.2. Order projectile curtain → lane lock → cannon →
missile fan → lane lock; rest 1.7 s. Không nhân projectile count cho toàn bộ phase.

### Signature lane lock `[TUNABLE]`

Beam pod charge 1.05 s với cue hướng mảnh, rồi active 2 s với white core, bề dày 10% viewport. Trong đầu charge và active, đầu tia phía dưới đuổi lateral Player với tốc độ tối đa .22 viewport/s; cuối charge khóa hướng .45 phần duration để đọc lúc commit. Tia nối từ pod tới đầu aim, đổi hướng khi Player đổi phía, không teleport theo Player.
Tàu hãm chuyển động trong cả charge/active để origin giữ đúng pod;
khi cancel/end/death/Retry phải nhả motion pause. Luôn còn safe space rộng hai bên.
Không instant hit, không beam tracking vô hạn hoặc AoE explosion.

`BossAttackModule` tính active beam bounds, query Player hitbox và route vào existing
life/protection receiver; `AttackBeamPresentation` chỉ vẽ cùng bounds. Gate/Pause/death
hủy damage đúng owner. Beam không trở thành collider nhận friendly projectile.

Không thêm pickup giữa phase. Vanguard có escort producer cap 3, mỗi 4.5 s thử
phóng một drone từ thân tàu; drones bay ra rồi bắn qua cùng budget với boss.
Lifecycle theo [escort contract](#m6-region-b-runtime-mechanics-provisional); tránh chồng threat/reward paths. Duration là tuning, phải
đủ thấy cả hai phase và nhiều signature attacks; benchmark không thay human approval.

### Death

Sau defeat chạy post-death sequence, reward rồi handoff transition theo
[levels](levels-encounters.md) và [audio/VFX](../presentation/audio-vfx.md).

## Boss 2 — Nebula Warden `[PROVISIONAL]`

### Identity

Nebula Warden dạy target priority qua shield nodes/modules, không dựa vào complex
lock-on UI. Player cần đọc module nào đang giữ shield trước khi tập trung core.

### Shield-node mechanic

Player phải phá shield nodes/modules trước khi boss vulnerable đầy đủ.
Exact node HP và vulnerability windows chưa có seed; không tự quyết bằng effect timing.

### Attack set

- Beam Sweep;
- Drone Spawn;
- Plasma Burst.

### Shield break feedback

Node defeat làm shield phản hồi; final node làm shield collapse và boss vulnerable.
Choreography thuộc [audio/VFX](../presentation/audio-vfx.md).

### Phase rule

Phase count 2–3 [TUNABLE]. Độ khó đến từ priority/attack interaction,
không kéo dài encounter bằng HP sponge.

## Final Boss — Rift Leviathan [PROVISIONAL]

### Purpose

Tổng kết signature systems, không chỉ tăng HP.

### Phase 1 — Vertical

HP 100% → ~65% `[TUNABLE]`.

- radial/arc projectiles;
- homing limited attack;
- drone summon;
- camera wide.

### Phase 2 — Direction Shift

Rift shift đổi combat sang horizontal; logical handoff/input lock/projectile clear
thuộc [levels/encounters](levels-encounters.md#final-boss-direction-handoff).
Attack set: sweeping beam, side lanes, dash/module attack.

### Phase 3 — Final

Below ~30% `[TUNABLE]`:

- camera expands;
- boss core exposed;
- higher intensity;
- không được biến thành unreadable bullet wall.

### Final death

Staged defeat dẫn Ending/Campaign Result; exact animation/audio thuộc
[audio/VFX](../presentation/audio-vfx.md#final-rift).

## Presentation boundary

Boss 1 death chain và boss 2 shield-break chain được thể hiện theo
[audio/VFX](../presentation/audio-vfx.md); mechanic vulnerability chỉ do gameplay
resolve. Boss identity/phase trong file này không mang nghĩa có prefab/scene đã tạo.


## Emergency missile target classification

`EnemyDefinition.MissileTier` có Normal / Large / Boss `[PROVISIONAL]`. Đây là
explicit authored classification, không đoán boss từ HP hoặc coi mọi minion trong
boss Encounter là boss. Current Scout default Normal. M5 author Bomber Large và Vanguard Boss; fixture synthetic vẫn phục vụ regression. Non-hostile supply/resource giữ
BodyDamagesPlayer=false và không bị missile auto-clear. Percent damage và viewport
eligibility thuộc [weapon/item owner](weapons-items-drops.md#tên-lửa-dự-trữ).


## M5 runtime seeds `[TUNABLE]`

| Production Region A target | Max HP | Config owner |
|---|---:|---|
| Scout | 4 / 5 / 5 | HP gốc 4, mission 1 / 2 / 3 |
| Raider | 7 / 8 | HP gốc 6, mission 2 / 3 |
| Bomber | 18 | HP gốc 16, mission 2; scale 1.45 |
| Elite Bomber | 30 | Mission 3 Bomber 20 ×1.5; scale ×1.12, cooldown ×.8 |

`LevelDefinition.EnemyHealthMultiplier` theo mission 1–4: 1 / 1.1 / 1.2 / 1.25.
Làm tròn lên HP gốc × mission multiplier × region multiplier, sau đó Elite multiplier; không áp cho
Boss, asteroid hoặc supply/resource. Không scale HP theo weapon level. Common
hostile chỉ nhận damage sau .2 s hiện trong viewport; Boss còn phải tới Hold/Combat.
Player projectile bỏ qua target chưa nhận damage, kể cả Homing acquisition/splash.
Raider giữ burst hai shot cách .22 s; Bomber giữ
spread ba viên/40°, telegraph .7 s, cooldown 3.6 s. Normal/Elite không có floating HP bar.
TTK evidence thuộc [dev/debug](../systems/dev-debug.md#m5-second-human-feel-pass-verification).

Normal storm asteroid dùng một prefab, bốn config variants:

| Size | Root scale | HP | Transit multiplier |
|---|---:|---:|---:|
| Small | .70 | 1 | .86 |
| Medium | 1.05 | 2 | 1.00 |
| Large | 1.50 | 4 | 1.14 |
| Huge | 2.10 | 6 | 1.30 |

HP override được cấu hình khi rent inactive, reset trước activate; EnemyHealth vẫn
là authority, definition không mutate. Collider là inset box footprint theo visual
và root scale; spin chỉ xoay visual. Resource targets giữ reward path riêng theo
[item owner](weapons-items-drops.md#asteroid-reward-classes), không tiêu common quota.

Asteroid presentation đọc Healthy → Damaged tại ≤66% → Critical tại ≤33% → Destroyed.
Hit flash .08 s độc lập tint; optional damaged/critical sprites cho cracked assets
sau này, hiện chỉ tint. Pool reuse restore intact sprite/tint/HP/scale. Destruction
burst nhỏ reuse glow sprite và pooled renderers; huge burst lớn tương ứng, không
AoE damage hoặc reserve-scale shake. Exact choreography thuộc
[audio/VFX](../presentation/audio-vfx.md#m5-vanguard-placeholder).

Vanguard HP/movement/attacks ở [Boss 1](#boss-1--vanguard-frigate-provisional), không
coi HP/timing thành final balance. Death/handoff theo audio/VFX và encounter owner.


## M6 Region B runtime mechanics `[PROVISIONAL]`

Static balance seeds `[TUNABLE]`:

| Target | Base HP | Placeholder footprint |
|---|---:|---|
| Sniper | 18 | .95 × 1.1, medium |
| Interceptor | 16 | .85 × 1.2, directional |
| Carrier | 34 | 2 × 1.65, large anchor |
| Warden Drone | 2 | .55 × .65, light minion |
| Bulwark | 28 | 1.65 × 1.45, front armor; single shot chậm, charge .9 s, cooldown 5 s, initial delay 1.2 s, speed 3.6 |
| Nebula Warden core | 520 | Capital hull + protected front |
| Shield module | 110 each, 3 modules | Beam / Drone / Plasma, distinct accents |

Normal enemies dùng mission HP multipliers L5–L8 = 1.25 / 1.30 / 1.35 / 1.40,
nhân thêm `RegionEnemyHealthMultiplier` = 1.30 ở Region B (Region A = 1).
Ví dụ Scout L5 7 HP, L8 8 HP; Bulwark L6 48 HP.
không áp cho boss/modules/asteroids, không scale theo loadout. Raider placeholder
hull tăng lên .8 × 1.05 để role không nhỏ hơn gameplay footprint; không thu nhỏ unit
để nhét nhiều slot. Bulwark dùng body blocking và HP, không có shield/invulnerability
riêng; front guard đứng cùng lane với Sniper/Bomber để chặn đường đạn thẳng.
Flank và splash vẫn là cách vòng qua guard. Interceptor vào từ M2, Sniper xuất hiện
muộn ở M3; Carrier giữ cho Region B.

Sniper `DirectedAttack`: initial delay .35 s → coordinator permission → charge 1.15 s,
tracking chỉ .3 s đầu → fixed aim beam .45 s → cooldown 3.6 s. Beam width .20 world;
Tracking dùng line mảnh vàng nhạt; khi khóa hướng line dày/sáng hơn và ngừng bám; active có core sáng. `PlayerContactReceiver`/damage lifecycle resolve hit,
`AttackBeamPresentation` chỉ vẽ segment. Không lấy aim line làm authority riêng.

Interceptor dùng cùng attack gate nhưng strike: charge .85 s, tracking .2 s,
committed dash .65 s qua target snapshot/overshoot .12 viewport, return 1.1 s và
cooldown 4.5 s sau đòn đầu; initial delay .35 s. Một vòng khóa bám aim hiện tại khi
charge, hiệu ứng phóng ngắn tại điểm xuất phát khi commit; không aim line và không
hiện vòng khóa khi return/recovery. `EntryHoldFlight` là pose writer, không homing sau commit. Bounded
return về slot cũ, không rời required kill set; ExitDrain hủy strike để retreat.

Carrier có single secondary fire nhẹ và `EscortSpawner`: sau Hold, mỗi 5 s thử sinh
một Warden Drone, cap 3 living units/owner. Drone xuất phát tại owner/pod, entry 1.1 s → hold/idle 9 s rồi exit,
attack vẫn qua coordinator. Spawn/register/reward ownership ở encounter runner;
minion không required và không roll loot/score/quota. Owner death/disable cho drone
retreat; Retry/reset/handoff thu hồi toàn bộ bằng runner/pool, không coroutine vô hạn.

Warden bảo vệ core tới khi cả ba shield modules bị hạ và collapse 1.2 s kết thúc.
Arrival bảo vệ cả core lẫn modules; chỉ sau Hold mới nhận module damage. Shield
chặn normal projectiles vào core, feedback hit riêng; Homing chỉ acquire target có
thể nhận damage. Module có HP riêng, không có normal floating health bar hoặc reward.
Destroy module tắt attack tương ứng trong shield phase. Local break cue, shield flicker
và recoil dẫn sang exposed core; không tái tạo shield hoặc thêm lớp HP.

Warden root scale 1.55 so với placeholder M6 ban đầu; collider, movement envelope,
nodes và các pod đi cùng scale. Không tăng camera để triệt tiêu kích thước mới.

Phase 1 rotate Ion Shelter → Beam Sweep → Plasma Burst → Drone Spawn → Plasma Burst, rest 1.25 s.
Phase 2 core exposed dùng core emitters với rest .9 s, Ion Shelter → plasma → sweep → drone.
Không cần phase 3. Core emitters giữ attack pressure sau khi các module đã hạ;
module destroy giảm rotation của phase 1, không làm exposed boss vô hiệu.

Beam Sweep charge 1.15 s rồi active 2.2 s, quét lane .07↔.93 theo screen height khi
Right, đổi chiều mỗi lần dùng; arrow báo đúng chiều sắp quét. Width .36 world,
aim không chase Player. Plasma gồm 5 viên trong fan 48°,
charge .9 s, speed 4.8. Drone spawn có charge .9 s, mỗi lần tối đa 2 drone, cap 3/owner; node drones retreat
khi module chết trước core spawner tiếp tục. Boss death giữ generation-safe staged
sequence, clear producers/shots, reward, completion đúng một lần theo runner M5.
Presentation không quyết damage/vulnerability/completion.

### Boss shelter attacks `[TUNABLE]`

Vanguard Projectile Curtain: báo trước 1.1 s, chọn khe ở lateral .25 hoặc .75 phía
xa vị trí Player lúc bắt đầu. Đạn xuất phát trước mặt boss .10 viewport, không sinh sau HUD/ngoài mép trên.
Ba hàng đạn cách .95 s, speed 8.5; hàng chẵn có
25 vị trí, hàng lẻ dịch nửa slot và bỏ vị trí ngoài mép .98 để xen kẽ.
Bỏ các viên thuộc khe .18 viewport và cộng margin bằng nửa chiều rộng
đạn. Projectile amber 1.9×.45 world, animated và dày hơn; đủ chặn các khoảng đứng
lọt giữa viên ở 16:9/20:9. Khe lần lượt ở lane gốc → lệch .28 về giữa → lane gốc, không có một cột đứng yên an toàn cả ba hàng. Sau .28 s attack cue, báo trước khe hàng kế tiếp trong phần interval còn lại; đạn đã bay vẫn giữ quỹ đạo/khe cũ. Lần dùng sau chọn lại theo Player.
Producer giữ busy tới lúc hàng cuối bay khỏi view rồi mới vào rest; không stream vô hạn.

Warden Ion Shelter: hai dải laser phủ ngoài khe .24 viewport, active .55 s mỗi nhịp,
ba nhịp. Báo trước lần đầu 1.15 s; hai lần sau đổi khe .25↔.75 với .25 s nghỉ cộng
1.15 s báo trước. Không đổi khe trong lúc damage đang bật. Charge tint nhạt bao
đúng diện tích sẽ đánh; active tăng độ sáng hai mép hành lang, không vẽ lõi trắng
rộng che actor/HUD. Fill texture alpha .09 charge/.35 active (dao động .035), không border/đường viền kỹ thuật. Pause freeze, death/gate/cancel tắt
hit query và visual; Retry/pool reset mọi timer. Player phải tìm khe rồi đổi chỗ,
không thể đứng ở góc cố định hết đợt. Đây là hành lang né riêng của attack, không
thay playable bounds và không phải vật thể cover chặn mọi projectile khác.

Hai đòn dùng `BossBarrage` qua `BossAttackModule` hiện có; vẫn tính một nguồn trong
shared cap 4, không scheduler song song mới. Warden sweep/plasma/drone tiếp tục
phân biệt với Shelter; không thay shield → exposed-core contract.

Tham khảo ý tưởng: [community boss rework](https://forum.chickeninvaders.com/t/my-full-list-of-boss-rework/8206)
đề xuất phá blind spots và ép thay vị trí; đây là đề xuất cộng đồng, không phải
changelog đã phát hành. [Dev discussion về danger zones/beam warnings](https://forum.chickeninvaders.com/t/mysterious-boss-danger-zone-warning/1619)
phân biệt gợi ý vị trí với cảnh báo beam tức thời. StarShift vận dụng nguyên tắc
telegraph rõ và khe né luân chuyển; không sao chép asset, tên boss hay mã CIU.

### Continuous beam contact

Beam query chạy sau relative movement. Ngoài overlap hiện tại, box cast theo chuyển động tương đối giữa tia và Player qua hai physics steps để drag nhanh hoặc tia quét không xuyên target. Chỉ nối sweep trong cùng active window; charge, cancel, recovery và pool reset cắt lịch sử query. Vẫn gọi PlayerContactReceiver nên invincibility/shield/life loss theo player owner, không tự trừ life từ VFX. Tia xoay/chase vẫn có thời gian active hữu hạn; visual và query dùng cùng segment/width.
