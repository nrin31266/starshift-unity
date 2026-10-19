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

Có thể dùng alternate sprite/frame, smoke, sparks và cracked emissive để biểu diễn
state; exact presentation ở [audio/VFX](../presentation/audio-vfx.md).

### Không floating HP bar cho quái thường

HP bar dành cho boss. Elite đặc biệt có thể dùng nếu thực sự cần, nhưng default
là không; enemy thường đọc state qua silhouette/damage feedback.

### Hit feedback

Projectile hit báo feedback và cập nhật damage state; death resolve một lần.
Choreography flash/particle/SFX/shake thuộc [audio/VFX](../presentation/audio-vfx.md).

## M4-A non-attacking test targets `[PROVISIONAL]`

Supply Drone dùng HP 3, Resource Asteroid dùng HP 2 `[TUNABLE]` trong placeholder
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

Carrier sinh micro-drone/Scout-like units. HP, fire profile, cap minion, spawn cadence
và minion có thuộc required kill set hay không [TBD] theo encounter cụ thể;
không tự copy Scout stats thành locked minion contract.

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

HP 520, không scale theo Player weapon. Phase A lần lượt twin cannon → missile fan
→ twin cannon, rest 1.9 s. Cannon 3 viên/pod ×2 pod (fan 38°), missile 5 viên/fan 62°.
Cannon telegraph .55 s, missile 1 s. Telegraph xuất phát
từ các pod đang fire; hai cannon cùng cue.

Phase B tại 50% HP: cancel attack hiện tại, core/engine cue 1.5 s, reposition offset
(-.06,.015) qua flight owner và motion envelope ×1.2. Order lane lock → cannon →
missile fan → lane lock; rest 1.7 s. Không nhân projectile count cho toàn bộ phase.

### Signature lane lock `[TUNABLE]`

Beam pod charge 1.05 s bằng vùng magenta rộng 10% viewport, rồi active 1.5 s với
white core. Tàu hãm chuyển động trong cả charge/active để beam giữ đúng pod/lane;
khi cancel/end/death/Retry phải nhả motion pause. Luôn còn safe space rộng hai bên.
Không instant hit, không beam tracking vô hạn hoặc AoE explosion.

`BossAttackModule` tính active beam bounds, query Player hitbox và route vào existing
life/protection receiver; `BossBeamPresentation` chỉ vẽ cùng bounds. Gate/Pause/death
hủy damage đúng owner. Beam không trở thành collider nhận friendly projectile.

Không thêm minion hoặc pickup giữa phase ở lượt này: ba attack identities và defeat
pair là đủ cho Boss 1; tránh chồng threat/reward paths. Duration là tuning, phải
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
| Bomber | 14 | HP gốc 12, mission 2; scale 1.45 |
| Elite Bomber | 23 | Mission 3 Bomber 15 ×1.5 rounded up; scale ×1.12, cooldown ×.8 |

`LevelDefinition.EnemyHealthMultiplier` theo mission 1–4: 1 / 1.1 / 1.2 / 1.25.
Làm tròn lên HP gốc × mission multiplier, sau đó Elite multiplier; không áp cho
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
