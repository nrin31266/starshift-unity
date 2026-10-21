# Dev và debug

Các mục milestone bên dưới giữ evidence lịch sử. Cleanup art cuối đã retire các
migration một lần và ba probe dư; lệnh hiện dùng theo
[Unity tools](../../tools/unity/README.md). Không chạy lại builder từ Git history
lên tuning hiện tại.

## Authored reward pacing verification — 2026-10-21

`VerifyAuthoredRewards.Pure/Begin` kiểm quota deadline/legacy mode, seed/reset,
actual enemy deaths và pickup collection trên cả 15 authored combat encounters.
Coverage gồm đủ 8 item families, reserved pickup overflow, maxed Core/ammo/life
fallback và production save byte guard đều PASS. NewGame → intro → Pulse upgrade →
Formation/Plasma → Pause/Retry → Plasma lại cùng ordinal PASS; checkpoint rollback
Core đúng, không đổi production save bytes. Native validation, persistence Core và
Console 0 errors/0 warnings PASS. Schedule ở
[content owner](../content/levels-encounters.md#authored-reward-pacing-tunable), không
lặp bảng ở đây. Evidence ignored: `.design/content-rewards/`.

## Content filename cleanup — 2026-10-21

100 content assets được rename qua Unity, giữ GUID và serialized data ngoài object
name. Production Mission 01–08, shared flights và encounter fixture theo
[naming policy](../production/asset-pipeline.md#content-asset-filenames).
Native validation toàn bộ LevelDefinitions PASS, compile và persistence Core PASS;
không đổi mission/encounter IDs, seed, gameplay values hoặc save schema.
8 reward tables chỉ được fixture tham chiếu đã chuyển vào `Data/Fixtures/Rewards`;
production không dùng bảng `Test*`. Migration script một lần đã bỏ; paths trong probes/docs đang dùng được cập nhật.

## Fixed clear pause grouped verification — 2026-10-19

User điều chỉnh khoảng nghỉ cố định mặc định thành 1.0 s đứng im hoàn toàn sau khi xong địch (bắn hạ, tên lửa, hoặc tàu tự rời đi), rồi mới bay cuộn nhẹ 1.5 s chuyển sang encounter tiếp theo.
`VerifyGameplayFeel.ClearHandoffDelay` PASS bốn paths: real primary shot kill, primary kill còn hostile projectile, reserve clear trong early-exhaustion Timeline, và tàu địch tự rời khỏi màn hình (ships-exited).
Cả bốn kịch bản đo chính xác:
1. Pha 1 đứng im: đúng 1.02 s (1.0 s + 1 frame tick), sao đứng yên (`IsCueVisible == false`), Pause freeze PASS.
2. Pha 2 bay cuộn nhẹ: đúng 1.52 s (1.5 s + 1 frame tick), sao bắt đầu cuộn nhẹ (`IsCueVisible == true`), announcement title hiển thị đồng bộ.
PickupCarryOver PASS với nhịp mới (1.0 s đứng im + 1.5 s bay cuộn nhẹ = 2.5 s); MissileAndImpact, ProjectionReset, short HandoffSmoke và persistence Core PASS trong cùng grouped pass.
Ponytail 5.3.2 scoped review dùng installed `ponytail`/`ponytail-review` skills: không
có unresolved finding; không thấy dead runtime code hoặc untracked source trong
phạm vi sửa. Capture tooling có consumer, nằm ngoài Assets; artifacts vẫn ignored.
Đúng một full Mission 01 mới PASS: 4370 frames, 5 unique completions, death/respawn,
Pause/Resume, real reserve HUD input và đúng một detonation; Score 4500, Shards 2,
cores 4, supply 1, pickups 14. Final Edit Mode scene clean, 0 missing scripts/
references, Console ground truth 0 errors/0 warnings. Docs/diff checks PASS.
Report/raw evidence ở `.design/encounter-handoff/`. Không chạy PR review, Android
hoặc historical full suite.

## Reserve impact strength follow-up — 2026-10-19

Targeted tuning theo User feedback; canonical seed thuộc audio/VFX owner.
`VerifyGameplayFeel.ReserveImpactPreview` ghi 55 native Game View frames 1600×900
qua GPU readback, encode PNG sau capture để không block nhịp impact. Đã inspect
chuỗi frame: world giật rõ tại detonation, HUD đứng yên và world sớm ổn định lại;
peak observed khoảng 28.17 px ngang/9.49 px dọc. Player pose và Camera Transform
không đổi, projection cuối trở lại baseline. GIF/evidence ở ignored `.design/reserve-impact/`.
Capture fixture chờ framing ổn định trước baseline; không dùng timing PNG IO làm clock.
MissileAndImpact, ProjectionReset, ButtonInput và short HandoffSmoke PASS. Compile,
final Console, missing scripts/references, docs/diff checks PASS. Đây là Editor Game
View evidence, không là device feel approval; không chạy full Mission hoặc Android.

## PR #5 findings fix verification — 2026-10-19

Targeted grouped pass sau ba bug fixes: Unity compile completed/failed=false;
`VerifyGameplayFeel.ProjectionReset` PASS actual render impact → expiry → RequestView
zoom/aspect framing, direct ortho update và CancelImpact/OnDisable automatic projection
reset. Probe chờ initial Play frame để Start không reset impact đang đo; chỉ camera
probe được rerun sau fixture correction. Không chạy historical camera matrix.

`PresentationNullSafety` PASS full refs, burst null, persistence null, view null và
Screen Shake disabled; mỗi case dùng temp storage, real launch/detonation, stock
trừ đúng một, damage vẫn đúng và LateUpdate không lỗi. `MissileAndImpact` PASS;
`VerifyPersistence.BestTimeGuard` PASS existing 120 → input 0/100/140/-1 cho kết quả
120/100/100/100, có disk reload. Persistence Core và short HandoffSmoke PASS.
Final Console ground truth 0 errors/0 warnings; scene clean, 0 missing scripts/
references; docs checker và diff check PASS. Raw evidence: ignored `.design/pr5-fixes/`.
Projectile active registry optimization được defer tới profiling; không thêm registry.
Không chạy full Mission, historical suite, Android hoặc PR/design/architecture review.

## M4-B pre-PR closure — 2026-10-19

Một grouped closure mới, không rerun historical matrix: `VerifyPersistence.Core`,
`ActiveIsolation`, `Retry(5)`, `VerifyGameplayFeel.PickupCarryOver`, `MissileAndImpact`
và `VerifyM4Testbed.HandoffSmoke` đều PASS. Bao phủ JSON/integrity/recovery,
Retry/Continue recreate service và Shard dedupe, active Campaign/Replay isolation,
pause flush, pickup/Shard/crate carry-over với lifetime tự nhiên, stale token rejection,
reserve flight/detonation/stock restore và handoff projectile grace/fade/Pause.
Pickup handoff đo 1.0199995 scaled s. Không chạy lại lifecycle scene-reload/full
Replay matrix; evidence trước đó ở [M4-B verification](#m4-b-persistence-verification).

Đúng một `VerifyGameplayFeel.BeginFullMission` mới PASS tại 1600×900: intro-enabled
clone → 5 unique Encounter completions → Clear, accepted death/respawn, Pause/Resume,
real reserve HUD input và đúng một detonation. 4257 frames; Score 4500, Shards 4,
cores 4, supply 1, pickups 14. Fixture giữ nguyên local intro asset.

Compile `up_to_date`, failed=false; final Edit Mode Console ground truth 0 errors/
0 warnings, scene không dirty; `ConfigureM4Testbed.Inspect` báo valid, 0 missing
scripts (scene/prefabs), 0 missing scene references. Bốn obsolete Flight GUIDs không
còn reference trong Unity assets; EMP→MissileAmmo giữ GUID qua rename. Docs checker
PASS 19 Markdown/337 links; diff check PASS sau cleanup trailing whitespace.
Raw evidence ở ignored `.design/m4-b-closure/`; source entry points vẫn versioned.
L1 intro delay .01, Intro.Enabled=false và runInBackground=true chỉ giữ local,
không đưa vào PR. SYS-028 còn User decision; SYS-030 chưa có Android/device proof.
Không claim final art, missile/Homing visual polish hoặc human/device feel approval.
Không chạy PR review hoặc architecture review trong closure này.

## Gameplay feel verification

### Fall, exit và cooling tuning follow-up

`ConfigureGameplayFeel.TuneFallExitAndCooling` chỉ đổi Shard motion, bốn timed Exit
sequences, cooling của ba primary profiles và trần Timeline E4; không chạy full
authoring. Runtime code/lifecycle không đổi. Canonical values ở
[weapons](../content/weapons-items-drops.md#primary-weapon-heat--m35) và
[encounters](../content/levels-encounters.md#m4-a-mission-01-system-testbed-provisional).

Targeted PASS: Thermal giữ cold fire 11.12/9.10/11.77 s, overheat recovery 1.20 s;
nhả .2 s không mất heat, .5 s mất 6 heat. Pickups còn đủ hai bounce rồi tự exit
(sample y=2: 2.70 scaled s), power-up recycle vẫn PASS. EmptyEncounter E2/3/4 PASS
ở 15.94/21.00/20.22 s, đủ 13/12/12 spawns; E4 có upper timer mới để không cắt exit
chậm hơn. PickupCarryOver PASS, pause/handoff khoảng 1.02 s và reset vẫn đúng.
Chỉ rerun nhóm affected và nearby smoke; không lặp full mission/historical suite
cho tuning nhỏ này. Editor evidence, chưa là human/device feel approval.

### Missile flight và weapon silhouette refinement

Theo feedback tiếp 2026-10-19, `ConfigureGameplayFeel.RefineWeaponsAndMissile` là
targeted Edit Mode authoring cho sprite overrides, pooled TrailRenderer, reserve
rocket/flash và minor handoff tuning; không chạy lại full Configure để reset local
balance/flight/intro edits. Placeholder PNG được generate bằng Unity API trong
helper, import và bind bằng GUID; không tải asset CIU.

`MissileAndImpact` nay kiểm delayed damage, protection từ launch, Pause giữa flight,
exactly-once detonation và địch ló ở viewport x=1.005 vẫn nhận damage, trong khi target
hoàn toàn ngoài view/non-hostile giữ HP. `MissileCancellation` kiểm Pause visual,
Retry giữa flight khôi phục stock/xóa trail và không detonate trễ; StopRun/generation
change và exit cũng cancel. `HomingMechanics` thêm distinct sprite và trail/sprite
reset qua pool reuse; collider/root scale giữ nguyên. PickupCarryOver đo handoff
1.01999664 s theo scaled clock (config 1 s), vẫn giữ powerup/Shard/crate qua boundary.

Grouped PASS: MissileAndImpact, MissileCancellation, nine weapon salvos/heat,
HomingMechanics, MissilesAndProtection, TerminalCueFreeze, PickupCarryOver và
DropSelectionEdges. Một full Mission 01 mới PASS 4164 frames, 5 unique completions,
accepted death/respawn, Pause/Resume và real reserve HUD click → đúng một detonation;
4500 Score, 1 Shard, 5 cores, 1 supply, 11 pickups. Clear cleanup PASS. Native Game
View 1600/2000×900 có cả launch/impact captures, review không còn rectangular Homing
hoặc overlap ngay muzzle. Final scene inspect 0 missing scripts/references, clean.
Synthetic Large/Boss damage vẫn không là balance proof trên boss thật; không claim
Android/human feel approval. Canonical mechanics ở
[weapons](../content/weapons-items-drops.md#tên-lửa-dự-trữ), timing ở
[encounters](../content/levels-encounters.md#m4-a-mission-01-system-testbed-provisional).

### Earlier feedback verification

Follow-up 2026-10-19 theo User feedback; vẫn dùng Mission 01 testbed, không author M5
content. `ConfigureGameplayFeel.Configure` qua `ConfigureGameplayFeel.cs` author flight
variants, early Timeline option, thermal/visual tuning, reserve ammo rename giữ GUID,
reserve/button/burst references và respawn timings. Không chạy lại M4 baseline builder.

`VerifyGameplayFeel.BeginChecks` chạy targeted Thermal → MissileAndImpact →
ShieldRespawn → Pickups → EmptyEncounter; progress ở ignored
`.design/gameplay-feel/progress.json`. Reserve tests dùng unique temp storage và
synthetic normal/large/boss definitions, restore originals trước pool reuse. Kiểm
stock cap/activation/duplicate click, damage, visible eligibility, non-hostile exclude,
Retry/Continue/Replay isolation, known v1 migration và Screen Shake preference.
Render callbacks đo actual shifted/restored matrices, bounded retrigger và unchanged
camera pose. Camera cue không được dùng làm gameplay test clock.

EmptyEncounter không bắn qua Encounter 2/3/4, kiểm all scheduled/optional groups đã
spawn và completion trước configured upper timer, một successor start. Pickups đo
hai bounce Shard và powerup viewport-exit recycle. ShieldRespawn đo protected window,
Pause và actual death-delay/control return. Thermal đo từng profile, không giảm test
coverage để giữ timeout cũ. VerifyM4Testbed.Weapons dùng timeout từ thermal profile.

`HomingMechanics` test real bounded turning/splash và visual scaling không đổi
collider. `Capture(width)` dùng lại existing verifier Rig, sau khi load một entry
VerifyM4Testbed (Weapons/DropSelectionEdges); captures native Game View ở 1600/2000 × 900 vào
`.design/gameplay-feel/screenshots/`, restore input/view/zoom/pan trong finally.
Canonical gameplay/presentation details thuộc owners liên quan, không duplicate ở đây.

Grouped closure PASS: thermal cold 11.12/9.10/11.77 s, recovery .86 s;
shield/death/control return .60/1.17 s; reserve tier damage normal kill, large100→40,
boss1000→900; 9 observed impulse render samples and restored matrices, max clip
translation .00746; two Shard bounces; no-shoot E2/3/4 completion 15.68/20.98/19.54 s
trước upper timers, đủ 13/12/12 scheduled/optional spawns. Real button input consumes
pointer without primary shot/Player move. Native visual review fixed selector overlap
and opaque burst, screenshots at both aspect ratios. Synthetic Large/Boss tests không
là new combat content.

Một full Mission với enabled-intro clone PASS 4505 frames: five distinct completions,
death, Pause/Resume, score 4300, 2 Shards, 4 cores, one supply, 12 pickups. Original
intro Enabled=false đang có được giữ; verifier không đổi asset đó để ép pass. First
legacy attempt failed Intro-not-ready vì assumption enabled intro; wrapper dùng clone.
Sau terminal presentation-only cleanup, targeted GameOver cue reset, persistence
Core/ActiveIsolation và reserve/protection PASS. Evidence này có trước pickup carry-over
follow-up bên dưới.


### Pickup carry-over và cleanup follow-up

`VerifyGameplayFeel.PickupCarryOver` chạy bằng temp Campaign storage và cloned level:
real power-up/Shard/crate qua minor handoff, vẫn collect và credit Profile; lifetime
không renew, Pause freeze, stale encounter token bị từ chối. Retry/reset phải release
mọi pickup và giữ Profile balance. Shard vẫn tự bounce/exit. Final-world grace kiểm
bằng `VerifyPersistence.Retry(5)`; terminal cue regression kích hoạt tại state callback
để không phụ thuộc wall-clock sleep gần cuối death animation.

Cleanup xóa bốn flight assets không còn Unity dependency: L1A0/L1B0/L1B1/M4Arc cùng
`.meta`; legacy builders có thể tái tạo khi cần. Giữ Wide/LowerRegion vì còn là Dev
camera presets. Runtime scripts đều có consumer qua code hoặc scene/prefab GUID;
không xóa fallback còn được sandbox/builder dùng. Authoring helper gộp vào
`tools/unity/ConfigureGameplayFeel.cs`; bỏ các entry sửa layout/ring một lần đã được
hợp nhất vào Configure. Không chạy lại authoring để reset tuning user hiện có.

Grouped verification sau thay đổi PASS: PickupCarryOver, PickupsAndRewards (đợi hết
shield-break protection trước fatal hit), HandoffSmoke, persistence Core/ActiveIsolation,
Retry(5), DropSelectionEdges và TerminalCueFreeze. Fixture carry-over tự set/restore
clock để chạy được sau terminal probe. Một full Mission 01 mới PASS 4349 frames,
5 unique completions, accepted death/Pause, 4200 Score, 4 Shards, 4 cores, 1 supply,
14 pickups; Clear không còn buff/pickup/projectile. Compile và final Console 0 error/
warning, scene sạch, 0 missing scripts/references; docs checker PASS. Đây là Editor
evidence; không rerun Android/device hoặc historical full matrix.



## M4-B persistence verification

Base `621b4361` (PR #4 merged). `ConfigurePersistence.Configure` author lifecycle
component/references bằng Unity API, giữ original scene/assets GUIDs và local
L1 intro delay .01 / runInBackground true. Không chạy lại M4-A authoring migration.

`VerifyPersistence.Core` chạy Edit hoặc Play Mode, temp storage riêng: real JSON
roundtrip/overwrite, orphan temp, malformed/required-root/version/ID/enum/range,
validated backup recovery, preserve invalid diagnostic, shard ledger qua reload,
clear idempotence, New Game/Profile/Settings và invalid setting. Có regression
HasCampaign cho JsonUtility inline-null cloning. Không chạm save thật.

`VerifyPersistence.Retry(index)` trong Play Mode cho indices 2, 4, 5: real dropped
Shard collection, reward/effect/death/downgrade, Pause button Retry, restored owner
state và clean combat objects; Continue sau recreate service; không duplicate wallet.
Encounter 4 thêm normal respawn, lives-zero GameOver và Retry; Encounter 5 retry
trong final reward grace trước Mission Clear. Không full Mission cho mỗi case.

`VerifyPersistence.BeginLifecycle` trả ngay; chờ `.design/m4-b/lifecycle-progress.json`
PASS/FAILED trước runtime probe khác. Một flow NewGame intro → Encounter 3 → pause/
flush → thật sự reload Gameplay scene → Continue bỏ intro → Clear commit → full
Replay với mutation/death/clear → DevFastStart với reward/upgrade/death/clear; so
sánh bytes/data và production file trước/sau. Failure tail dùng corrupt test copy,
backup recovery, invalid/no-save Continue/NewGame behavior. Evidence trong ignored
`.design/m4-b/`; test dirs được xóa sau test, không commit cache/save files.

Persistence/restore policy thuộc [save owner](save-profile.md), reward prototype
thuộc [economy](../gameplay/progression-economy.md#m4-b-reward-và-record-policy-provisional).
Chưa có Android process kill/device QA; scripted kills chỉ accelerate lifecycle,
không claim weapon-pattern/density benchmark. Runtime smoke khác chạy theo blast radius.
Tracker patch tiếp tục dùng duy nhất `.design/m4-a/tracker-updates.tsv`; Google Drive
connector cần reauthentication, tracker hiện đọc qua public CSV. SYS-028 là User
decision còn mở; SYS-030 device lifecycle proof chưa thuộc M4-B Editor QA.


## M4-A combat/reward testbed verification

Checkpoint M4-A 2026-10-18, base sau PR #3 (`9d0fafed`).
Mission 01 hiện là [provisional system testbed](../content/levels-encounters.md#m4-a-mission-01-system-testbed-provisional),
không production campaign. Weapon/item tuning và các assumption chưa chốt ở
[weapon owner](../content/weapons-items-drops.md#m4-a-runtime-test-semantics-provisional).

### Entry và deterministic rewards

`DevFastStart` giữ release guard `UNITY_EDITOR || DEVELOPMENT_BUILD`, default off.
Có `startingWeaponLevel`, `unlockAllWeapons`, `deterministicRewards`, `rewardSeed`;
apply override sau run reset, không mutate definitions hoặc disk/save.
Jump dùng real Encounter IDs/aliases ở [level owner](../content/levels-encounters.md#checkpoint-và-stable-dev-start-ids).
Reward seed chỉ đổi RNG/history của runtime RewardDirector, không global Unity RNG.
Production dùng random; authored guaranteed tables chỉ nằm trên reward moments của
testbed. Không thêm fake production checkpoint hoặc checkpoint DTO ở M4-A.

### Grouped targeted evidence

`VerifyM4Testbed` chạy Play Mode qua Unity MCP `run_script`, file ngoài Assets:

| Entry | Evidence |
|---|---|
| Weapons | PASS 9 branch/level salvos; active-only death penalty/minimum; release-before-MAX, MAX-only lock, movement, recovery, held auto-resume, Pause |
| PickupsAndRewards | PASS contact/moving collection, pickup reuse, Shield/life/downgrade, Extra Life cap, OD/Drone apply/expiry/Pause, kill score once-only, final-kill reward grace, death/reset |
| CombatAndDrops | PASS 12 visible enemies; 4,000 seeded rolls reproducible (23 major, 159 Shard); defensive pity; limited missile tracking/Lv3 splash; supply destroy/escape; guaranteed resource Shard; cap 6 |
| DropSelectionEdges | PASS pity reset và maxed core/life filtering |
| Stress | PASS 12 visible, multiple groups, continuous fire/heat changes, effects, deaths, 81 primary projectiles fired, Score 500/Shard 1, peak 3 pickups |
| RegressionAndPools | PASS hostile grace → damage-off fade → release, Pause fade, Game Over, reset; enemy counts 15/15/15/15 và primary created counts 26/26/26/26 sau warmup |
| PowerAndLifecycle | PASS actual Pulse TTK trên cùng Scout HP: Lv1 .70 s, Lv3 .18 s; Plasma Lv3+OD created 60/60 sau warmup; UI switch pointer exclusion; pickup expiry; effect/heat retention qua handoff; run reset release friendly shots |
| EmpAndProtection | PASS EMP radius/damage, hostile-only clear, friendly shots/fire permission giữ nguyên, invincibility trước Shield |
| PickupFade | PASS lifetime/grace fade, Pause freeze, grace extension và opacity reset khi pool reuse |
| Visuals(width) | PASS capture 8 runtime states ở 1600×900 và 2000×900; review actual Game View |

Đây là Editor/virtual Input System evidence, không Android hardware hoặc human feel
approval. Pool counts là identity/churn smoke, không profiler/FPS/device benchmark.
Phần targeted dùng runtime setup/protected fixtures; không dùng setup đó làm campaign
balance proof. Missile probe giữ HP thật, assert damage delta thay vì giả enemy HP 2.
Fixture restore flight flags khi rời probe, Game View zoom/pan/resolution, devices,
Input System policies, clock/audio và lifecycle; không save runtime scene/config.

Full mission evidence được ghi bởi `BeginFullMission`/`BeginFinalRuns` ra ignored
`.design/m4-a/full-result-{width}.json`; entry trả ngay để tránh MCP transport timeout
60 s trong khi task async vẫn tiếp tục. Không bắt đầu probe khác khi file còn
`status: running`. `FullMission` trực tiếp vẫn dùng được khi client có timeout đủ dài.
Hai final runs dùng Play session sạch sau khi sửa fixture flight cleanup. Full flow
bằng virtual pointer có protection cho reproducibility; lượt 20:9 dùng accepted hit
qua real lifecycle để kiểm death/entrance/downgrade và click Pause/Resume qua uGUI.
Không gọi những lượt này là Android/manual-human playtest.

### Final full Mission runs

| Game View | Evidence trong Play session sạch |
|---|---|
| 1600×900 / 16:9 | PASS intro → 5 encounters → Clear; 5,799 frames; peak 12 visible; Score 4,600, Shards 1, cores 4, supply crates 1, pickups 10 |
| 2000×900 / 20:9 | PASS cùng flow + accepted death/respawn/active downgrade + UI Pause/Resume; 5,727 frames; peak 12 visible; Score 4,200, Shards 2, cores 4, supply crates 1, pickups 12 |

Mỗi Encounter complete đúng một lần. Clear không còn buffs, pickups hoặc friendly/
hostile shots; runtime counters được giữ để trình bày. Captures gồm Clear overlay thật.
Reward totals khác nhau theo kill/collection, không phải expected economy/balance.
Đây là protected fixture có kiểm lifecycle có chủ ý, không manual human/device play.

### Fast closure evidence

`BeginClosure()` PASS một grouped pass: DevFastStart đủ 5 Encounter và replay,
9 weapon salvos/heat, pickup/effect/reward/reset, EMP/protection, drop/pity edges,
projectile grace/fade/Pause và pickup fade. Density mới: 12 visible,
82 primary shots, Score 600/Shard 1, peak 4 pickups.
Đúng một full run mới 1600×900 PASS intro → 5 unique completions → Clear, có accepted
death/respawn/downgrade và uGUI Pause/Resume: 5,770 frames, peak 12, Score 4,100,
Shards 2, cores 4, supply 1, pickups 12. Result local:
`.design/m4-a/closure-results.json`. Hai full runs phía trên vẫn là historical evidence
valid; closure không đổi production C#/tuning/HUD. Không rerun historical/aspect/camera
matrix, pool benchmark hoặc Android. Compile/Console PASS (0 errors/warnings), saved
scene không dirty, 0 missing scripts/references; docs checker và diff check PASS.
Hai override local intro .01 s và runInBackground=true được giữ ngoài commit.

### Review, scope và tracker

`unity-scriptdesign` review changed gameplay components: loadout/config/runtime,
effect lifecycle, subscriber cleanup, pool parent/root, missile target incarnation,
HUD authority. Một `design-review` trên 8 states × 2 aspects: normal/density/pickup/
effect/high heat/Overheated/nonzero counters/transition. Content-fit counters,
compact bottom cues/timers, không thêm fake icons/final art. Review/evidence local ở
ignored `.design/m4-a/`. Ponytail không expose callable skill/tool trong session;
không cài plugin hoặc claim đã chạy; static cross-system review giới hạn M4 diff.

Tracker Google giữ source of truth. Connected Drive cần reauthentication; browser
public sheet chỉ View. Exact Status/Proof/Notes cell patch nằm local trong
`.design/m4-a/tracker-updates.tsv`, gồm M4-A và stale M2/M3 reconciliation từ historical
canonical evidence; không tạo workbook/tracker mới. Không tìm thấy workbook Excel
project trong workspace. SYS-004/015 checkpoint/restore và SYS-021–030 persistence
vẫn ngoài M4-A; SYS-005/008/012 decision chưa locked. Không khởi động M4-B.


## M3.5 thermal HUD verification

HUD correction hiện tại theo feedback user: functional placeholder vuông góc,
nền mờ, padding sát nội dung và dùng viết tắt khi chưa có icon; contract ở
[UI owner](../presentation/ui-ux.md#gameplay-hud). Custom plate/icon tự dựng đã bỏ.
`PlainHud(1600)`/`PlainHud(2000)` chụp Game View và smoke bar/Pause/Resume; không
đổi heat tuning hoặc chạy lại full L1 cho correction presentation này.
Revision trước đã chạy sáu thermal state mỗi aspect; hai attempt 20:9 timeout
khi warm-up, lượt sau pass, root cause chưa xác định. Không ghi thành bug thermal
đã fixed; ảnh revision trước không là style hiện tại.

Checkpoint 2026-10-18, trên main sau PR #2 merge, trước M4. Thermal mechanic/seed
thuộc [weapons](../content/weapons-items-drops.md#primary-weapon-heat--m35), lifecycle
thuộc [player](../gameplay/player-combat.md#thermal-lifecycle), visual contract thuộc
[UI](../presentation/ui-ux.md#gameplay-hud). Không mở rộng rewards/save/content.

Grouped Play Mode evidence: sustained tới overheat khoảng 5.56–5.60 s; recovery
khoảng 1.18–1.20 s với seed hiện tại. Targeted PASS gồm MAX-only lock, movement
độc lập, held auto resume, release trước MAX, repeated burst, Pause freeze,
death/respawn reset, Encounter retention, mission replay reset, terminal freeze và
HUD fill/state. UI click Pause/Resume không leak drag/fire intent. Đây là Input
System virtual-device test qua loop thật, không là Android hardware test.

Một full `VerifyVerticalSlice.FullMissionRun(1600, 6, false)` PASS: 1886 frames,
đúng một Clear; major intro hide và minor handoff HUD visible vẫn đúng. Death đã
kiểm ở targeted nên không lặp trong full smoke. Compile PASS; final Editor Console
0 error/0 warning; scene saved, 0 missing script/reference. MCP retained buffer có
log cũ trước checkpoint, không có entry mới trong lượt M3.5.

`ThermalHud(1600, true)` là grouped targeted; `ThermalHud(2000, false)` là visual
aspect pass. Một refinement chỉ đổi weapon symbol và normal capture timing, rồi
chụp lại sáu state cho mỗi aspect; không lặp historical suites. Entry points ở
[tool README](../../tools/unity/README.md#m35-thermal-hud-checkpoint).
Design/script review và ảnh/JSON giữ local trong ignored
`.design/m3-5-thermal-hud/`; summary ở đây và regression entry points được version
để có thể chạy lại. Evidence local không thay canonical ownership. Không chạy Android, full
camera/input/aspect gameplay matrix, benchmark pooling hoặc claim device FPS.

## Phạm vi và ownership

Owner của DevConfig, debug controls và đường test nhanh. Không yêu cầu custom editor/debug framework. Persistence isolation chi tiết ở [save/profile](save-profile.md).

## Dev mode

### DevConfig là requirement `[LOCKED]`

12 level + boss + transition bắt buộc có đường test nhanh.

### Baseline overrides

DevConfig gom override để vào nhanh level, encounter, boss hoặc transition cần test.
Fields dưới là contract cần hỗ trợ dần khi hệ thống xuất hiện, không yêu cầu tạo
custom editor hay implement toàn bộ tooling cùng lúc.

| Nhóm | Fields |
|---|---|
| Enable và entry | DevModeEnabled; StartLevel; StartEncounter / StartWave; StartPoint |
| Player | StartingLives; Invincible |
| Weapon | UnlockAllWeapons; LaserLevel; PlasmaLevel; MissileLevel |
| Cinematic | SkipIntro; SkipTransition |
| Reward | ForceSupplyDrone; ForceReward |
| Balance | EnemyHpMultiplier; PlayerDamageMultiplier; BossHpOverride / BossHpPercent |
| Timing và visualization | GameSpeed; ShowHitboxes; ShowPaths; ShowSpawnPoints; ShowBounds; ShowFps |

### Start points

Stable IDs và availability mỗi level thuộc
[levels/encounters](../content/levels-encounters.md#checkpoint-và-stable-dev-start-ids).
StartLevel/StartEncounter/StartPoint phải resolve tới content có thật, báo lỗi config
rõ khi không hợp lệ; không silently fallback sang production Continue.

### Example workflow

Test transition sau Boss 1:

```text
StartLevel = 4
StartPoint = BOSS
BossHpPercent = 0.10
SkipTransition = false
```

### Save isolation `[LOCKED]`

Dev override không được mutate production CampaignSave/Profile trừ khi user dùng explicit debug save command.

### Release guard

Dev feature mặc định off trong normal release build.

## Encounter standalone và fast testing

Mỗi hệ thống lớn có entry nhanh: L1 combat, từng completion mode, optional supply,
boss phase/death và transition. Không test Boss 3 bằng cách chơi cả campaign.
Load config/preset test độc lập; override không sửa ScriptableObject definition asset
hoặc production save. Exact UI/config carrier và field types [TBD].

## M2 Pulse Laser verification

VS-009–011 có entry `VerifyPulseLaser.Run` ở `tools/unity/VerifyPulseLaser.cs`, chạy
qua Unity MCP `run_script` trong Play Mode. Helper compile probe MonoBehaviour trong
memory, tạo GameObjects DontSave và dọn tất cả sau test; không lưu probe thành
production enemy hoặc đưa dependency test vào runtime scripts.

Coverage: idle không fire, giữ yên fire, release ngừng spawn ngay, re-press anchor
mới, held intent độc lập movement, synthetic mouse/touch, gate truth table và pause,
movement block/fresh pointer, sustained cadence,
state fire block/resume khi pointer còn held, hướng Up/Right, owner filter, compound collider single-hit,
thin-target sweep, reuse reset và lifetime release. Config thử speed cao dùng clone
trong memory; original definition phải không đổi. Đây là physics/integration probe,
không chứng minh Scout HP/damage state hoặc prefab production. Xác minh phần đó ở
VS-012/013; không coi dependency đã hoàn thành chỉ vì probe pass.
Seed thuộc [weapons owner](../content/weapons-items-drops.md#pulse-laser), collision
contract thuộc [player owner](../gameplay/player-combat.md#collision-matrix-theo-gameplay-contract).

## Dynamic gameplay view verification

`tools/unity/VerifyGameplayView.Run(width)` là fast path trong gameplay scene,
không cần tới wave/boss thật. Chạy ở Play Mode qua MCP cho width 1600 và 2000,
height 900; frames phải thực sự tiến, không lấy screenshot/Console sạch làm bằng
chứng gameplay. Probe compile trong memory, quan sát sau movement LateUpdate;
không thêm runtime debug UI hoặc production verifier.

Matrix: baseline, close, wide và spectacle seeds theo
[camera owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles),
full và restricted/offset regions; 4 cạnh,
4 góc, footprint, outward stable/reverse ngay; Player ngoài future region khi co;
return baseline kể cả gate đóng; held intent/fresh anchor; retarget giữa transition,
pause scaled time, close request thấp hơn baseline và invalid config fallback.
Settings/tunable contract thuộc
[view architecture](architecture.md#dynamic-gameplay-view-foundation), không do
test fixture quyết balance.

Chạy lại `VerifyPlayerDrag.Run` và `VerifyPulseLaser.Run` cho relative delta,
mouse/touch simulation, release/re-press, hold/drag fire, gate và projectile
hit/lifetime/pool regression, gồm teardown khi projectile root bị hủy trước weapon.
Xem Console cả sau khi thoát Play Mode và inspect scene/prefab refs.
Helpers restore camera/view/player/input policy/devices/Game View trong finally;
Game View presets chỉ ở Editor preferences. Đây là synthetic Input System events
trong Editor, chưa chứng minh Android hardware feel/readability hoặc device FPS.

## M2 Scout verification

`tools/unity/VerifyScout.Run(width)` chạy ở Play Mode qua MCP với width 1600 hoặc
2000, height 900. Spawn production prefab, dùng Player hold-to-fire và Pulse pool
thật để kiểm tra HP qua từng hit, flash/persistent Damaged và one-time death/release.
Test entry/hold, baseline/zoom-out, entry trong view transition và return; quan sát
render pose sau LateUpdate để bắt camera reprojection bị trễ một frame.

`VerifyScout.EdgeCases` dùng projectile prefab trong local verification pool để
kiểm tra compound colliders, rapid volley, cùng projectile reuse, dead/non-positive
damage guard, presentation không quyết HP, reset/re-enable và chết giữa entry.
Callback feedback lỗi không được chặn death hook/release; verifier bắt exception
cố ý để không biến nó thành Console error của gameplay.
Definition không được mutate. Test helpers/probes không được save vào production.

Để regression các suite player/view cũ, tạm disable scene Scout bằng
`VerifyScout.SetSceneScoutActive(false)` rồi restore `true` sau test; enemy thật
không được xen vào probe trajectory của isolated suite. Chạy `VerifyPlayerDrag`,
`VerifyPulseLaser` và `VerifyGameplayView`; kiểm tra Console cả sau Play exit.
Helpers restore native device/policy, camera/view/Game View/gate/player trong finally.
Đây là Editor synthetic input, chưa chứng minh Android hardware/readability/FPS.

## M2 Scout fire verification

`tools/unity/VerifyScoutFire.cs` chạy qua MCP `run_script` trong Play Mode. `Run(width)`
nhận width 1600 hoặc 2000, height 900; dùng production Scout/profile/projectile,
kiểm tra Entry không fire, Hold/delay/telegraph/single/cooldown, aim vào Player sau
khi charge đã bắt đầu, no-homing, speed, lifetime và pool reset ở baseline/wide view.
Đổi Game View thật và capture charge/dodge vào ignored
`app/Temp/StarShiftScoutFireCheckpoint/`; direct-position checks không được gọi là
relative drag verification.

`Dodge(width)` queue virtual Mouse qua Input System để giữ fire và kéo 350 px né
hostile shot thật, kiểm tra receiver probe không bị hit. Helper chờ đạn cũ hết lifetime
trước khi bắt đầu để không lẫn contact từ producer đã disable. Đây là synthetic
input trong Editor, không phải OS drag, Android touch hoặc manual feel approval.

`Lifecycle()` kiểm tra pause giữa charge, kill trong telegraph, root/component
disable/re-enable, flight block/re-entry, pause projectile và teardown root/pool.
`Collision()` dùng cloned config trong memory cho sweep tốc độ cao, compound hitbox,
owner exclusion, same projectile reuse, missed-shot lifetime và contact khi Player
receiver disabled. Probe chỉ nhận contact; không chứng minh Player lives.

Helpers restore scene Scout, camera/view/Game View, Player/gate, timeScale và
device policies trong finally; không mutate original profile. Chạy lại Scout,
PlayerDrag, PulseLaser và GameplayView suites theo checklist phía trên, inspect
references/missing scripts và Console sau Play exit. Global Transition/Result fire
authority chưa có; không claim đã test session orchestration chưa implement.

## M2 player damage lifecycle verification

`tools/unity/VerifyPlayerDamageLifecycle.Run(width)` chạy trong Play Mode qua MCP,
width 1600/2000, height 900. Mỗi aspect chạy nguyên lifecycle ở baseline, close và
wide framing; wide dùng lower 70% region. Không lấy các isolated VS probes thay cho
end-to-end evidence. Scene Scout tạm disable; helper spawn production Scout, dùng
real hostile projectiles và compound body contact, restore/dọn objects cuối test.

Coverage theo scenario:

- A: projectile hit, lives giảm một lần, Dying/cleanup, entrance, safe position,
  post-entry protection và enemy fire grace độc lập rồi Active.
- B: nhiều projectile/body callbacks gần nhau vẫn một accepted death/life loss.
- C: starting lives tới zero qua repeated respawns, một GameOver signal, không
  respawn hoặc nhận damage tiếp, hostile combat blocked/cleared.
- D: projectile trong entrance/invincibility bị consume, không trừ life; body
  overlap còn tồn tại sau protection kết thúc chỉ resolve một valid hit.
- E: death giữa telegraph hủy pending shot; grace không phát shot; production Scout
  resume local delay/telegraph mới. Enemy không bị xóa/freeze bởi death cleanup.
- F: cả hai aspect, current view và restricted region; end pose nằm trong footprint
  bounds, tránh body chặn preferred respawn candidate.

`PointerAndLifecycle()` queue virtual Mouse qua input thật để verify held fire
resume sau entrance, stale drag bị bỏ, release/re-press tạo delta mới. Cũng kiểm tra
callback exception không chặn mandatory life/cleanup work, pause giữa entrance và
root disable/re-enable hủy stale sequence/reset phiên. Pulse chỉ có Lv1 nên death
penalty mới chứng minh minimum Lv1, chưa chứng minh downgrade pattern Lv2/Lv3.

`HostilePool()` kiểm tra real Scout pool: clear trả shot về pool, reset đủ fields,
reuse cùng instance, pause và root-first teardown. `Presentation()` kiểm tra death
flash/protection tint/blink; capture ở ignored `app/Temp/StarShiftLifecycleCheckpoint/`.
Case tất cả candidates bị chặn phải giữ protected entrance cho tới khi blocker được
dọn; fixture chờ friendly shots cũ tự hết lifetime để chúng không giết blocker.

Regression probes cũ chạy trong **Play session riêng**, gọi `IsolateLegacy()` trước:
tạm disable damage lifecycle, mở enemy safety gate và disable scene Scout để
fake trajectories không kích hoạt production death. Không save isolated scene;
stop Play để restore wiring/state. Chạy View/PlayerDrag/PlayerBounds/Pulse/Scout/
ScoutFire suites phù hợp, inspect matrix/references và Console cả sau Play exit.
Helpers không ghi save/profile, không dùng làm runtime dependency. Đây là Editor
physics/synthetic-input evidence, chưa phải Android hardware hoặc performance test.

## M2 L1 fast start và encounter verification

VS-027 dùng `DevFastStart` trên `L1Encounters`, không tạo menu hoặc fake campaign.
Mặc định `Dev Mode Enabled = false`: Play gameplay scene chạy A → B. Trong Editor
hoặc Development Build, bật flag rồi chọn `Start Point`:

- `LEVEL_START`: bắt đầu L1 tại A;
- `ENCOUNTER_1`: bắt đầu A;
- `ENCOUNTER_2`: bắt đầu B.

`Standalone = true` dừng sau encounter đã chọn; false cho phép handoff tới encounter
tiếp nếu có. Trong Play Mode, component context menu **Replay Configured Start**
dọn enemy và hostile/friendly shots, reset Player lives/lifecycle và schedule rồi
chạy lại entry đã chọn. Player giữ vị trí hiện tại trong bounds; replay hủy stale
drag, vẫn theo held-intent contract của input. Stop/Play cũng replay từ config.
Invalid/missing/duplicate config ID báo lỗi rõ, không fallback Continue. Flag mặc
định off và dev override không hoạt động ở normal release build. Không gọi save,
PlayerPrefs hoặc sửa definition assets; persistence contract ở
[save/profile](save-profile.md#replay-và-dev-isolation).

Inspector: chọn `L1Encounters` để xem definition references/dev start; chỉnh formation,
counts, spawn/entry timing, fire coordination và handoff tại hai assets được link
trong runner. Giữ numeric seeds tại
[level owner](../content/levels-encounters.md#m2-l1-playable-checkpoint), HP tại
[enemy owner](../content/enemies-bosses.md#balance-seed-table), view controls tại
[view architecture](architecture.md#dynamic-gameplay-view-foundation).
Đổi config rồi Play lại để demo; không expose mutable counters/state làm knobs.

`tools/unity/VerifyL1Encounters.cs` chạy qua Unity MCP `run_script` trong Play Mode:

- `Run(width, size, lower70)`: actual hold/relative pointer/Pulse kills A, không count
  disable là death, handoff, pooled reset, hai cánh B ghép formation, fire trong exit,
  timeline completion một lần và cleanup trước callback. Observer đo maximum
  simultaneous telegraphs và shot spacing; frames phải tiến thật.
- `Death()`: hostile projectile thật gây death trong A sau một Pulse kill, giữ
  survivors/counters, clear/grace, entrance/protection, pause và finish bằng Pulse.
- `Lifecycle()`: kill trong telegraph, pool reset/replay, pause entry, disable runner
  giữa telegraph, reset giữa handoff, Game Over stop, dev jump/replay A/B và reject invalid start.
- `Boundary()`: timeline cắt khi enemy còn sống; release/cancel/clear trước notification,
  không gửi death/kill và không còn delayed fire.
- `Offscreen()`: gom 4 cạnh ở 16:9/Size 6 và 20:9/Size 6.8, exit release trước hết
  duration, cancel telegraph ở mép, pooled reuse, root còn visible nhưng muzzle
  ngoài view không được fire. Không chạy lại full gameplay matrix cho mỗi tuning pass.
- `RootToggle()`: tắt/bật toàn bộ encounter root rồi replay, không lỗi reparent
  trong activation traversal và vẫn reuse đủ pooled Scouts.
- `QuickTuning()`: một smoke gộp cho tuning pass: production Scout nhận đủ real Pulse
  hits theo HP, đo hit cadence, inspect projectile width và chạy B hết timeline với
  hold ngắn/exit chậm; không lặp full baseline regression.
- `DepartureFeedback()`: gom Normal/Damaged tại các mốc HP, mirrored entry theo
  từng cặp, shot cuối còn sống sau source exit/timeline cutoff và sau fallback
  lifetime khi vẫn visible; chỉ complete khi shot ra khỏi view. Explicit reset
  phải tiếp tục clear và reset camera binding.
- `Validate()`: invalid count, non-finite timing/path và timeline trước scheduled
  group bị reject; chỉ sửa cloned config trong memory.
- `Capture(startPoint, elapsed)`: capture Game View tại thời điểm encounter vào ignored
  `app/Temp/StarShiftL1Checkpoint/`, không lưu test state vào scene.

Matrix width 1600/2000, height 900; size 6, 5.25 và 6.8; wide smoke dùng lower 70%.
Đây là baseline khi thay core; tuning pass gom check bị ảnh hưởng và một lượt
end-to-end thay vì lặp lại toàn matrix cho mỗi thay đổi nhỏ.
Helpers restore camera/view/Game View/input policies/devices/lifecycle/timeScale,
stop test run, không save scene hoặc mutate definitions. Runtime state reset/replay
không chứng minh future save integration. Để isolated regression cũ, gọi
`VerifyL1Encounters.Isolate()` rồi `VerifyPlayerDamageLifecycle.IsolateLegacy()`
trong Play session riêng; không save và stop Play để restore authored scene.
Chạy lại PlayerDrag/Bounds/View/Pulse/Scout/ScoutFire/PlayerDamageLifecycle liên quan,
inspect missing references/scripts và Console sau Play exit. Đây là Editor synthetic
input/physics evidence; manual gameplay feel và Android hardware/FPS còn cần test.

## Save/profile debug và logging

Cho phép inspect checkpoint/profile/settings; reset có target rõ;
force reward không tự ghi vào production profile. Explicit debug save command mới
được mutate production data khi người dùng chủ ý. Release mặc định off.
Log event/config failure hữu ích; không spam per-frame, per-projectile hay mỗi hit.

## Dev/testing strategy

### Game View và trạng thái Editor

Scale slider của Game View là preview zoom trong Editor, độc lập `Time.timeScale`,
camera framing và Transform scale của Player. Không đổi gameplay tuning để bù zoom
preview. Verification đổi resolution phải snapshot và restore preset/resolution,
zoom và pan trong cleanup, kể cả khi assertion/exception ngắt test; không ép zoom
về 1 cho mọi lượt vì người dùng có thể đã chọn zoom khác.
Để thấy toàn bộ output, dùng Fit theo resolution và kích thước pane hiện tại;
1x là pixel-for-pixel nên có thể crop khi output lớn hơn pane. Giá trị Fit không
phải gameplay config cố định; không hard-code 0.55 hoặc 0.69 vào test/gameplay.

Các helper standalone dùng Game View API nội bộ của Editor: preset test có tên riêng
theo resolution để không overwrite preset đang được chọn, restore qua size-selection
callback rồi trả zoom/pan đã snapshot. Không thêm dependency này vào runtime.
`VerifyVerticalSlice.EditorViewRestoration` kiểm tra lại sau các Editor frames cho
cả normal return và interruption ở hai resolution. Khi nâng Editor cần kiểm tra
lại API reflection cùng các entry Configure/Verify hiện có.

### Test nhanh

Mọi feature lớn phải có đường vào nhanh.

Không test Boss 3 bằng cách chơi 20 phút.

### Core manual test matrix

Chọn các hàng liên quan feature đang đổi; không claim toàn game đã test chỉ vì một
đường vào nhanh chạy được. Matrix cần bao phủ các trường hợp sau khi hệ thống tồn tại:

| Domain | Manual coverage |
|---|---|
| App flow | Launch; Main Menu; New Game; Continue; Missions; Settings |
| Player | Relative drag; hold-to-fire; weapon switch; death; respawn; invincibility; Shield |
| Encounter | Eliminate; Timeline; Survive; optional Supply event; asteroid |
| Save | Quit mid-wave; reopen; Continue từ wave start; Shard persists; replay không overwrite campaign |
| Pause | Timers stop; audio hoạt động đúng; resume; restart wave |
| Boss | Phase transition; death; reward; region transition |
| Device | Landscape; notch safe area; multiple aspect ratios |

### Automated tests

Không bắt buộc full unit test suite ngay.

Nhưng logic thuần C# phù hợp SHOULD test khi xuất hiện:

- drop weighting;
- save migration;
- score calculation;
- checkpoint snapshot;
- deterministic config validation.

## Definition of Done cho một gameplay task

Một task không được báo done chỉ vì “code đã viết”.

Tùy task, DoD SHOULD gồm:

- compile sạch;
- không Console error mới;
- feature chạy được bằng đường test;
- regression relevant kiểm tra;
- canonical docs update nếu contract đổi;
- tunable nằm config thay vì magic number nếu phù hợp;
- asset attribution update nếu thêm third-party asset;
- không phá save/replay;
- dev path tồn tại nếu feature ở late level.

Nếu không chạy runtime được, agent phải nói rõ:

```text
Not runtime-verified.
```

## Verification checklist cho persistence và device

Khi hệ thống liên quan được implement: kiểm tra corrupt/unknown-schema fallback,
New Game không xóa Profile, replay/dev isolation, pause/restart checkpoint,
loadout downgrade/restore và reward duplication qua Retry.
Device kiểm tra 16:9, 18:9/19.5:9, tablet landscape, safe area và gameplay bounds.
Không claim build/device FPS hoặc runtime verified từ Console sạch.

## M2 vertical-slice closure verification

`tools/unity/VerifyVerticalSlice.cs` chạy trong Play Mode qua Unity MCP run_script;
không cài test framework hoặc thêm test MonoBehaviour vào production assets.

- `InputAndPause()`: mouse click/drag qua button, UI-start touch rời button,
  gameplay finger + second Pause finger, Resume khi gameplay finger còn held,
  fresh press, telegraph/projectile pause và snapshot timer/pose.
- `FullRun(width,size,withDeath)`: mission announcement → A (real held Pulse kills)
  → scaled handoff → B mirrored formation/timeline → clear một lần. Khi withDeath,
  nhận real hostile projectile, death/entrance/protection/grace, pause invincibility
  rồi tiếp tục. Pause combat/handoff/B kiểm timer và pose không đổi.
- `GameOver()`: ba accepted hits qua lifecycle (không sửa lives), reject duplicate,
  lives zero khóa schedule trong final death, một Game Over signal/overlay.
- `Pooling()`: sustained player/enemy firing, warmup 12 s rồi đo 12 s bằng
  ProfilerRecorder markers Instantiate/Destroy và so sánh identity pool hierarchy.
- `ResetAndStandalone()`: standalone B không phát full mission clear; dev replay
  từ Pause reset clocks/lives/input; disable session trả clock/audio; không spawn đôi.

Helpers restore Game View resolution, camera/view, Input System settings/devices,
clock/audio và lifecycle; không save runtime state hoặc mutate encounter definitions.
Capture dùng ScreenCapture (có overlay UI) vào ignored `app/Temp/StarShiftClosure/`.
Khi chạy probe sau một Play session cũ đã terminal, nên stop/start Play để trả toàn
bộ authored state; không save scene trong Play Mode.

### Closure QA checkpoint — 2026-10-18

Editor 6000.5.9f1, Unity CLI 1.0.0-beta.13. Frame counter thực sự advance trước QA.
Năm full L1 runs dùng runtime config giữ nguyên, không ép completion timer:

| Run | Game View | Size | Death/respawn | Kết quả |
|---|---|---|---|---|
| 1 | 1600 × 900 (16:9) | 6 | Real hostile hit | PASS, 1770 frames |
| 2 | 2000 × 900 (20:9) | 6 | Protected regression | PASS, 1341 frames |
| 3 | 2000 × 900 (20:9) | 6.8 | Alternate framing smoke | PASS, 1336 frames |
| 4 | 1600 × 900 (16:9) | 6 | Protected regression | PASS, 1342 frames |
| 5 | 2000 × 900 (20:9) | 6 | Real hostile hit | PASS, 1767 frames |

Mỗi run có real Pulse collision/kill ở A, B hold/exit/timeline, Pause combat/handoff,
cleanup/Result permissions, đúng một mission clear và không stale spawn/fire sau đó.
Các run protected là automated integration regression; không thay thế người chơi
manual feel review. Không kết luận Android touch latency/safe area/device FPS.

Technical UI copy/layout contract ở [UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional);
QA không định nghĩa một copy source thứ hai. Ảnh mission-start, formation, pause,
clear và Game Over lưu tại capture folder trên; review bằng composited Game View.

Supplemental checks cùng checkpoint:

| Check | Evidence |
|---|---|
| InputAndPause | PASS mouse + synthetic multitouch; UI re-press cùng frame hủy press gameplay cũ; 222 frames ở lượt cuối sau khi dùng project InputActions |
| GameOver | PASS 3 accepted hits, lives 0, đúng 1 signal, không spawn trong final death |
| ResetAndStandalone | PASS standalone B không mission clear; dev replay từ Pause; disable restore clock/audio; không duplicate spawn |
| Pooling | PASS 12 s warmup + 12 s/718 frames đo; Instantiate 0, Destroy 0; 10 PlayerProjectile + 6 EnemyProjectile giữ identity; 7 Scout gồm sandbox inactive và pool |
| DepartureFeedback | PASS HP 4/3 Normal, 2/1 Damaged; mirrored error 1.79e-7; source exit/visible >6 s/timeline drain; reset/replay không stale drain |
| Scene inspect | PASS 1 EventSystem, 0 missing scripts/references, font đủ glyph Việt |

UX review ảnh 16:9/20:9: HUD không che B formation; announcement đầu mission ngắn,
Pause/Clear đọc rõ và không thêm “ĐỢT” để tránh thông tin thừa. Player tint override
có sẵn trong scene được giữ; batch không đổi Scout/Pulse/L1 tuning. Checkpoint đầu
batch `0ffb5a69` giữ implementation/tuning trước UI; không include ProjectSettings.

Tracker snapshot task sheet `gid=1237076761` còn Not Started cho VS-006/024/025/026/
028/029 và cả nhiều dependency đã implement. Sáu task trên đủ Editor evidence để
chuyển Done; **chưa ghi lên tracker** vì Google Sheets session chỉ có quyền “Chỉ xem”
và không có connected editor session. Bản patch status/proof/notes nằm ở ignored
`app/Temp/StarShiftClosure/tracker-updates.tsv`; không sửa các dependency chỉ dựa
vào tracker cũ. VS-030/Android và M3 không thuộc batch.

Final inspection sau Play exit: compile sạch, saved scene không dirty, 0 missing
scripts, 0 missing references, 1 EventSystem; Unity Console ground truth **0 errors,
0 warnings**. Tool capture buffer vẫn giữ lỗi cũ/compile typo đã sửa, không dùng
historical buffer counters làm Console cuối. New UI batch để uncommitted; không
push/merge, không đổi package versions hoặc ProjectSettings user có sẵn.

## M3 encounter foundation dev controls

`DevFastStart` trên `L1Encounters` giữ default OFF và compile guards
`UNITY_EDITOR || DEVELOPMENT_BUILD`. `StartLevel` optional trỏ LevelDefinition;
`StartPoint` resolve trong level đó. `SkipIntro`, standalone, explicit Up/Right và
CameraProfile override chỉ áp cho runtime, không ghi asset/save/profile.
Available L1 IDs là `LEVEL_START`, `ENCOUNTER_1`, `ENCOUNTER_2`; Boss/PostBoss/Transition
không xuất hiện khi chưa có content. Stable ID contract thuộc
[level owner](../content/levels-encounters.md#checkpoint-và-stable-dev-start-ids).

Replay từ Pause phải restore clock/audio, cancel pointer, reset Player lifecycle và
hard-clear run cũ trước start mới. Đây vẫn là dev reset, không Retry checkpoint.
Chọn camera override Wide/LowerRegion để test nhanh; không sửa production baseline
chỉ để chạy smoke. Chọn direction override Right để dùng cùng L1 path/formation
trong horizontal context; không tạo L5 content.

`EncounterDebugView` có toggles bounds/playable region, paths, spawn/formation slots,
hitboxes, FPS và Content Labels; tất cả default OFF, runtime drawing chỉ trong Editor/development.
Content Labels hiện Region / Planet, Mission, Encounter, từng Spawn Group cùng
Formation và Flight Sequence / Path IDs. Trong handoff đọc incoming Encounter để
khớp centered title. Tên gọi theo [taxonomy owner](../content/levels-encounters.md#terminology-và-composition).
Gizmos cần bật trong Scene/Game View. Không log mỗi frame hoặc phụ thuộc debug để
chạy gameplay. FPS overlay là Editor/dev diagnostic, không chứng minh device FPS.

Menu `StarShift/Validate Encounter Content` và trước Enter Play kiểm tra authored
LevelDefinition assets: duplicate level/encounter/optional IDs, missing references,
invalid sequence/duration, required target terminal hold và invalid start refs.
Runtime StartRun validate lại content được chọn, fail rõ, không fallback Continue.
Không có custom graph/editor framework. Authoring tools dùng Unity API; không sửa
scene/prefab YAML hoặc tự tạo `.meta`.

## M3 regression entry points

Sau Edit Mode migration `ConfigureEncounterFoundation.Configure`, Play gameplay scene.
Các helper ở `tools/unity/` chạy qua Unity Pipeline `run_script`; long suite phải đặt
CLI `--timeout` và command `--timeout_ms` đủ dài, đọc `result.success` bên trong
response chứ không chỉ CLI exit code.

| Entry | Coverage |
|---|---|
| `VerifyVerticalSlice.FullMissionRun(width,size,withDeath)` | Establishing valid tap → phased approach/travel/settle → A real Pulse kills → minor handoff giữ HUD/Pause → B → Clear; major intro Pause blocked, gameplay Pause/lifecycle, actual Game View aspect/profile |
| `VerifyEncounterFoundation.Validate` | Valid content, duplicate IDs, invalid refs/start, NaN/invalid required phase rejection; Mission card positive/finite duration independent of cinematic window |
| `VerifyEncounterFoundation.Boundaries` | Four completion modes, optional hooks/group, disable not kill, hostile/reward cleanup, bounded/paused grace, stale boss token, unsorted schedule, reset cancellation |
| `VerifyEncounterFoundation.DirectionAndCamera(width)` | Same four-phase sequence Up/Right, forward/respawn axis, override/lower region/return level camera |
| `VerifyEncounterFoundation.PathsAndFormations(width)` | All implemented path/formation options in both directions, finite motion/exit, deterministic distinct visible slots |
| `VerifyEncounterFoundation.FiringLimits` | Configured two-shooter cap, actual shots, pending attack cancellation |
| `VerifyEncounterFoundation.StartHooksAndReentrancy` | Encounter start trước spawn; reset trong run/encounter/title callback không resurrect schedule |
| `VerifyEncounterFoundation.CompositionAndDebug(width)` | Hai group Pincer/Cross theo lanes, actual collider overlap check và debug default/toggle capture |
| `VerifyEncounterFoundation.DevAndReset` | Intro/encounter jump, Right/camera override, replay from Pause, reject nonexistent BOSS |
| `VerifyL1Encounters.DepartureFeedback` / `Lifecycle` | Preserved HP cue, mirrored B entry, visible projectile after source exit/lifetime/schedule, kill during telegraph, reset/handoff/reuse |
| `VerifyVerticalSlice.MinorProjectileCleanup` | Late shot qua minor boundary: grace flight/damage, safe fade và trigger/sweep lock, viewport exit, pool reuse, Pause freeze, HUD/drag và extended centered handoff |
| `VerifyVerticalSlice.ThermalReviewFixes` | Missing heat HUD bindings; death reset chỉ thermal; gameplay gate và mission reset giữ authority cadence; real death/respawn smoke |
| `VerifyVerticalSlice.InputAndPause` / `GameOver` / `Pooling` | Existing UI ownership, terminal lives-zero cancellation, sustained profiler identity/churn |

Helpers clone test configs, restore original LevelDefinition and view/device/clock
state, không ghi production saves. Boundary probes fake only future participants/Boss
completion signals; không coi đây là implemented reward content hoặc boss mechanics.
Path/formation checks không thay manual attack-readability playtest của L2–L12.
Measured evidence nằm ở checkpoint M3 bên dưới sau khi QA hoàn tất.

### M3 QA checkpoint — 2026-10-18

Evidence dưới đây thuộc checkpoint `c6ef441d`. Intro/handoff Pause trong bảng lịch sử
được thay bởi contract briefing/cinematic mới ở [presentation revision](#mission-presentation-revision).

Editor 6000.5.9f1; saved `02_Gameplay` dùng L1 LevelDefinition, cùng GUID của Player,
Scout và hai encounter assets M2. Test configs là runtime clones; không tạo L2–L12,
economy, save hoặc reward content. CLI results/screenshots nằm trong ignored
`app/Temp/StarShiftM3/` và composited Game View tại `app/Temp/StarShiftClosure/`.

| Full migrated flow | Evidence |
|---|---|
| 1600 × 900, Size 6 | PASS intro → A real Pulse kills → B → Clear; real hostile hit/death/respawn; Pause intro/combat/handoff; final rerun 1947 frames |
| 2000 × 900, Size 6 | PASS cùng full flow, protected regression, 1521 frames |
| 2000 × 900, Size 6.8 | PASS wide profile override, cùng full flow, 1523 frames |

Các run xác nhận đúng một mission clear, không stale spawn/fire sau terminal,
0 enemy/hostile/friendly shots tại Result. Captures đã được xem: short planet
approach/settle, title Việt, mirrored B và HUD ở hai aspect; không claim Android QA
hoặc art-family approval.

| Standalone/regression | Evidence |
|---|---|
| Four modes + optional/grace | PASS 6 completion scenarios, 2 optional events; disable không count kill; grace bounded/paused; late completion reward được preserve; stale boss/cleanup token rejected |
| Direction/camera ở 16:9 và 20:9 | PASS same four-phase sequence Up/Right; real respawn bottom/left; camera override/lower region; return tới level baseline 6.4 thay vì global seed |
| Paths/formations ở mỗi aspect | PASS 22 path-direction runs + 20 formation-direction runs; finite motion/exit và distinct visible slots |
| Cross/Pincer + debug ở mỗi aspect | PASS two groups, sampled crossing collider lanes không overlap; debug default OFF, toggles/captures; future combat pressure vẫn cần content playtest |
| Dev/reset | PASS intro/encounter jump, direction/camera override, replay từ Pause, unavailable BOSS rejected |
| Firing config | PASS configured active-shooter limit 2, maximum 2 telegraphs, actual shots, reset hủy pending attacks; L1 giữ limit 1 |
| Standalone/session teardown | PASS standalone B không mission clear, dev replay từ Pause, disable trả clock/audio, không duplicate spawn |
| Start hooks/reentrancy | PASS 2 start signals trước spawn; reset trong run/encounter/title callbacks không resurrect schedule/intro |
| DepartureFeedback | PASS mirrored error 1.19e-7; HP 4/3 Normal, 2/1 Damaged, reuse reset; visible shot tồn tại sau source exit và hơn 6 s; timeline drain; hard reset clear |
| Lifecycle/root reuse | PASS kill/disable during telegraph, pause, reset during handoff, Game Over cancellation; root deactivate/replay reuse toàn bộ Scout, 0 errors |
| UI/terminal | PASS mouse + synthetic multitouch ownership, fresh press sau resume, telegraph/projectile pause; Game Over đúng một signal, không spawn trong final death |
| Sustained pools | PASS 12 s warmup + 12 s/719 frames đo; Instantiate 0, Destroy 0; 10 PlayerProjectile, 6 EnemyProjectile và 8 Scout (gồm sandbox) giữ identity |
| Validation | PASS valid authored content và 7 invalid config scenarios; missing/duplicate IDs, fake start/Boss alias, missing camera, invalid/NaN required flight |

Pooling evidence là sustained Editor encounter, không phải device FPS hoặc toàn bộ
campaign stress. Existing projectile pools được preserve; shared scene prefab pool
phục vụ common enemy reuse. Reward/Boss probes chứng minh runtime boundaries,
không thay actual pickup/crate/boss-content verification của M4/M5.

Tracker đọc trước/sau tại task tab `gid=1237076761`: rows 50–74 (ENC-001–025) vẫn
Not Started. Public read thành công; connected Drive yêu cầu reauthentication nên
không thể ghi. Status/proof/notes patch được xuất local vào
`app/Temp/StarShiftM3/tracker-updates.tsv`, không tạo spreadsheet duplicate trong repo.

Final Edit Mode inspection: compile sạch; scene đã lưu, không dirty; 0 missing
scripts/references; Unity Console ground truth **0 errors, 0 warnings**. Unity `.meta`
đầy đủ cho assets/folders, không duplicate GUID trong StarShift. Docs checker PASS.
M3 local checkpoint được tạo sau QA, không push; hai ProjectSettings changes có sẵn
được để ngoài commit. Self-review giới hạn toàn M3 diff/system; không chạy audit
ngoài scope hoặc dùng Ponytail plugin vì skill đó không được expose trong session.


## Mission presentation revision

`LEVEL_START` chạy establishing space/location scene, đọc text rồi nhận valid tap
sau prompt. `DevFastStart.SkipIntro` và encounter IDs vào nhanh như trước.
`ConfigureMissionPresentation.Configure` migrate scene qua Unity API: bỏ menu button,
reuse announcement title + description, thêm subtle hint và transparent tap area
trên Canvas cũ. Rerun giữ timing/copy đã tune. Checkpoint trước revision là `c6ef441d`;
không thay unrelated ProjectSettings hoặc tạo cinematic framework.

Runtime flow/timing xem [audio/VFX](../presentation/audio-vfx.md#level-intro-warning-và-dynamic-audio),
HUD/control/Pause xem [UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional).
Major intro reject Pause; minor handoff giữ HUD và Pause gameplay bình thường.
LocationName trống vẫn TBD; fallback space copy trung tính không phải fiction name.

`VerifyVerticalSlice.FullMissionRun` queue real virtual Mouse down/up ngoài vùng
text, kiểm tra delayed prompt/title, consume tap/held pointer, 8→9→level baseline,
HUD trở lại, full combat/clear và HUD/Pause xuyên handoff. `EstablishingInput` kiểm
tra early hold, drag, right-click, canceled touch, valid anywhere touch và fresh
press sau cinematic. `PresentationBoundaries` trong VerifyEncounterFoundation kiểm
tra cancel giữa các phase, stale continue, restore title/Player/backdrop và lifecycle
không bị reset trong minor handoff. Helpers dùng cloned/runtime config, không ghi
save/profile hoặc mutate authored content.

`CaptureIntroPreview` sample PNG khoảng 10 fps vào ignored
`app/Temp/StarShiftIntroReview/ux-preview-frames/`; trả thời lượng thực để encode
preview giữ nhịp. Đây là preview từ Editor, không đo device FPS. Planet/ship vẫn dùng
placeholder hiện có, không phải final art approval.

Editor Play Mode QA cho UX correction (2026-10-18):

| Kiểm tra | Evidence |
|---|---|
| Full L1, 1600 × 900, baseline 6 | PASS anywhere mouse tap → intro → A → B → đúng một Clear, 2173 frames; real Pulse kills và hostile death/respawn |
| Full L1, 2000 × 900, baseline override 6.8 | PASS 8→9→6.8, full combat/clear, 1748 frames; ordinary handoff giữ HUD/Pause và không confirmation |
| Establishing input | PASS early press/hold, long hold, drag, right-click và canceled touch rejected; valid anywhere touch; held cinematic finger không move/fire, fresh gameplay press hoạt động |
| Presentation boundaries | PASS reset tại Briefing/Travel/Framing, stale continue rejected, title/visual restore; lifecycle/lives tiếp tục đúng qua handoff và Pause |
| Gameplay input/Pause | PASS mouse UI exclusion, synthetic multitouch, fresh press sau Resume, telegraph/projectile và handoff timer freeze |
| Start hooks/reentrancy | PASS reset trong run/title/encounter callbacks không resurrect intro/schedule, đúng hai start signals trước spawn |

Đã sửa uGUI coi canceled touch như click: continue kiểm tra touch gốc có phase Ended.
JSON/captures nằm trong ignored `app/Temp/StarShiftIntroReview/ux-*.json` và
`app/Temp/StarShiftClosure/`. Establishing, contextual arrival, handoff giữ HUD ở hai
aspect và preview motion đã được xem trực tiếp. Chưa chạy Android/device QA.

Final Edit Mode: L1 valid; scene saved/không dirty; 0 missing references/scripts.
Authoring rerun PASS với migrate=false, LocationName vẫn trống/TBD. Unity compile
và Console sạch (0 errors, 0 warnings). Docs checker PASS: 19 Markdown, 271 relative
links. UX revision chưa commit; các ProjectSettings changes có sẵn giữ ngoài scope.

Follow-up taxonomy/layout: bảng thuật ngữ canonical đã chốt ở level owner; prototype
dùng Region / Planet · TBD, Mission 01 và Encounter 02. Major establishing giữ layout
giới thiệu; short Mission/Encounter labels căn giữa, độc lập HUD lock. Existing debug
view thêm Show Content Labels, default OFF; không thêm UI/cinematic framework.

Editor QA: full L1 1600 × 900 baseline 6 và 2000 × 900 override 6.8 PASS, lần lượt
1764/1743 frames; centered labels, HUD/Pause xuyên handoff, tap consumption và đúng
một Clear. CompositionAndDebug ở cả hai aspect, Up/Right PASS: đủ sáu tên taxonomy,
hai Spawn Group và sequence/path IDs đúng nguồn, debug toggles default OFF/restore.
Captures đã xem trực tiếp; JSON `taxonomy-*.json` trong ignored intro review folder.
Final scene saved/không dirty, 0 missing references/scripts, Console 0 errors/0 warnings;
docs checker PASS 19 Markdown/275 relative links. Chưa có device QA hoặc commit mới.

### Mission card closure và code review — 2026-10-18

Evidence lịch sử trước correction chuyển card sang màn hình có điều khiển;
contract hiện tại theo [UI owner](../presentation/ui-ux.md#m2-ui-implementation-provisional).

Hoàn tất QA sau follow-up gộp Mission / starting Encounter vào cùng card. Review
uncommitted presentation diff và callers theo AGENTS/Ponytail Review, trong phạm vi
single-player L1 hiện tại; không tìm thấy lỗi cần chặn trong phạm vi đã kiểm tra.
Contract/timing vẫn thuộc presentation owners, không do QA định nghĩa lại.

| Kiểm tra | Evidence |
|---|---|
| Full L1, 1600 × 900, baseline 6 | PASS 2231 frames; real hostile death/respawn, real Pulse kills, đúng một Clear |
| Full L1, 2000 × 900, override 6.8 | PASS 1803 frames; intro → A → B → Clear, HUD/Pause xuyên minor handoff |
| Mission card | Cả hai aspect PASS title/subtitle căn giữa, card vẫn opaque sau mốc thời gian cũ và chưa spawn enemy; captures được xem trực tiếp |
| Dev/reset và presentation boundaries | PASS starting Encounter 02 đúng subtitle, replay từ Pause, reset Briefing/Travel/Framing restore visuals và reject stale continue; lifecycle không bị handoff reset |
| Input/Pause và terminal | PASS early/drag/hold/right-click/canceled touch rejection, valid touch, consumed held finger và fresh press; mouse/multitouch Pause; Game Over đúng một signal, không spawn trong final death |
| Validation và completion | PASS 11 invalid configs, gồm card zero/NaN/infinity/vượt cửa sổ; card vừa đúng cửa sổ hợp lệ. Six completion scenarios, optional/grace/reset và reentrant start/title hooks PASS |
| Standalone và pools | PASS không full-clear standalone, replay/disable trả clock/audio; 12 s warmup + 12 s/719 frames đo, Instantiate/Destroy 0, pool identity ổn định |

Rà 48 file C# tự viết dưới StarShift bằng type references, GUID trong scene/prefab/assets
và Unity Editor callbacks: không có file nguyên vẹn không dùng để xoá. Đã bỏ hai
Mission copy members cũ và computed intro duration không còn caller; giữ các offline
Configure/Verify scripts vì vẫn là authoring/regression entry points.
Không thiếu script `.meta` hoặc trùng GUID trong StarShift.

Compile PASS; final Edit Mode scene không dirty, 0 missing scripts/references,
1 EventSystem, font đủ glyph Việt. Unity Console ground truth 0 errors/0 warnings;
capture buffer vẫn có entries lịch sử nên không dùng làm final Console counts.
JSON/captures/reference inventory nằm trong ignored `app/Temp/StarShiftFinalReview/`
và `app/Temp/StarShiftClosure/`. Chưa test Android, hardware touch/safe area hoặc
device FPS; chưa commit/push. Các ProjectSettings có sẵn giữ ngoài scope.

### Mission card trên gameplay — 2026-10-18

Correction theo [UI owner](../presentation/ui-ux.md#m2-ui-implementation-provisional):
card chỉ bắt đầu sau cinematic, khi HUD/control đã mở; first spawn vẫn chờ hết nhịp đọc.
Regression assertion đã fail trên flow cũ trước khi sửa.
FullMissionRun PASS ở 1600 × 900/baseline 6 với hostile death/respawn (2406 frames)
và 2000 × 900/override 6.8 (2018 frames). Cả hai xác nhận không card trong Settle,
camera baseline/HUD/control/Pause sẵn sàng trước card, real fresh drag/fire trong card,
Pause freeze/Resume restore card, chưa có enemy trong nhịp đọc và đúng một mission Clear.
Captures ở cả hai aspect đã được xem trực tiếp. JSON `card-gameplay-*.json` trong
ignored `app/Temp/StarShiftFinalReview/`; screenshots ở `app/Temp/StarShiftClosure/`.
Validation, dev jump/reset, callback reentrancy, synthetic mouse/touch và gameplay
Pause PASS; boundary suite thêm reset tại MissionTitle, reject load level khi card
còn active và reset trong gameplay title callback không resurrect spawn. Final Edit
Mode: compile/Console sạch, 0 errors/0 warnings, 0 missing scripts/references,
scene không dirty; docs checker PASS. Chưa có Android/device QA hoặc commit mới.

### Continuous arrival — 2026-10-18

Motion follow-up thay nhịp đứng chờ sau giảm tốc; timing hiện tại thuộc
[presentation owner](../presentation/audio-vfx.md#level-intro-warning-và-dynamic-audio).
Regression assertion fail trên flow cũ vì còn stationary phase sau Brake. Full L1
từ Play session mới PASS ở 1600 × 900/baseline 6 với death/respawn (2291 frames)
và 2000 × 900/override 6.8 (1867 frames): duration Travel đúng config, Brake nối thẳng
vào gameplay, camera/HUD/control sẵn sàng khi card hiện, drag/fire/Pause và Clear đúng.
Validation, reset tại Briefing/Travel/Brake/MissionTitle, dev jump và synthetic
establishing input PASS; gameplay InputAndPause chạy riêng trong session mới PASS.
Các lượt QA trước có assertion fail về synthetic held/tap/UI re-press; failed JSON
được giữ cùng các lượt pass, chưa xác định nguyên nhân và không claim đã sửa input.
Evidence `smooth-arrival-*.json` trong ignored `app/Temp/StarShiftFinalReview/`.
Preview H.264 1600 × 900 dài 9.67 s tại ignored
`app/Temp/StarShiftIntroReview/smooth-arrival.mp4`; đã xem các frame Travel/Brake/card,
HUD/backdrop blend đồng thời với giảm tốc, không nhịp đứng chờ trước control.

### Editor preview cleanup QA — 2026-10-18

Đã sửa chín đường đổi resolution trong tám verification files để restore qua
size-selection callback và trả preview zoom/pan. M3/closure/L1 presets tách theo
resolution để không mutate slot chung khi chạy hai aspect. Tám file compile-only PASS.
`EditorViewRestoration` PASS normal return và deliberate exception ở hai resolution;
custom zoom 1.37x và pan giữ nguyên sau các Editor frames. FullMissionRun 1600 × 900,
Size 6 PASS 1887 frames; snapshot trước/sau giữ đúng preset, resolution, zoom 1x và pan.
Foundation DevAndReset PASS và giữ preview 1x. Đây là sửa Editor verification cleanup,
không thay Time.timeScale, Player Transform scale hoặc gameplay camera để bù preview.

Checkpoint trước correction Fit: preview 1x, Time.timeScale 1, Player scale (1,1,1); L1 valid,
scene không dirty, 0 missing scripts/references, Console ground truth 0 errors/0 warnings.
JSON `game-view-*.json` tại ignored `app/Temp/StarShiftFinalReview/`; docs checker PASS.
Legacy helpers đã compile, không chạy lại toàn bộ gameplay suite legacy ngoài scope.
Chưa có Android/device QA hoặc commit/push.

Follow-up preview correction: không ép 1x sau QA. Game View đã trả về mức Fit theo
pane/resolution hiện tại: output 1600 × 900 dùng 0.688125x trong pane hiện có;
output 2000 × 900 tương ứng khoảng 0.5505x. Đây là quan sát Editor, không là seed
gameplay hoặc số cố định để lưu vào test. EditorViewRestoration PASS giữ Fit sau
normal return và deliberate exception; Time.timeScale và Player scale vẫn 1.

### M3 pre-PR closure — 2026-10-18

Full L1 PASS 1600 × 900/baseline 6 (2294 frames, hostile death/respawn) và
2000 × 900/override 6.8 (1884 frames). Validation, completion boundaries,
Up/Right/camera, presentation reset, reentrant start, firing limits và dev reset PASS.
Game Over, standalone/replay, InputAndPause và EditorViewRestoration PASS;
các probe terminal/input chạy trong Play session mới. Một lượt chạy nối
GameOver → ResetAndStandalone gặp NullReference ở IsTerminal; lượt session mới
pass, chưa xác định nguyên nhân chạy nối và không coi đây là bằng chứng suite isolation.

Pooling verifier loại selection Inspector khỏi cửa sổ đo rồi restore trong finally:
Inspector preview gây nhiễu marker Destroy toàn Editor. Trước sửa đo 0/60 và 0/68;
sau sửa, kể cả khi chọn Scout trước probe, 12 s warmup + 12 s/719 frames đo
Instantiate/Destroy 0/0, identity ổn định, selection được trả lại.
Giữ tooling regression; bỏ CreatedCount không caller và parameter announcement
obsolete. Không có nguyên file C# tự viết nào đủ bằng chứng obsolete hoàn toàn.

Final Edit Mode: compile PASS, Console ground truth 0 errors/0 warnings,
scene không dirty, 0 missing scripts/references; preview Fit 0.5505x được giữ.
Evidence ignored tại `app/Temp/StarShiftM3Finalization/`. Đây là QA trên author
checkout với hai ProjectSettings có sẵn giữ ngoài commit; không thay thế verification
trong worktree PR hoặc Android/device QA.

### Minor encounter cleanup QA — 2026-10-18

`VerifyVerticalSlice.MinorProjectileCleanup` PASS: late shot còn bay/visible trong
grace, fade tắt collider và từ chối queued trigger/swept target; alpha giảm trước
pool release. Measured grace .64 s và fade .32 s (physics tick tolerance), reuse
restore alpha/damage, viewport exit recycle sớm. Pause freeze flight/fade; HUD/drag
active; next Encounter không nhận shot cũ. Config/timing policy thuộc
[cleanup owner](../content/levels-encounters.md#minor-handoff-projectile-cleanup).

Nearby `FullRun(1600,6,false)` PASS 1433 frames, 3 real Pulse kills và đúng một Clear;
bao gồm combat → minor handoff → B → terminal cleanup, không chạy lại historical matrix.
Timeout chờ A completion của helper đã lấy theo cleanup config thay vì giả định
hard-clear dưới .7 s. Full intro probe dừng ở assertion reading-delay vì local dev
config của user là .01 s; không thay config đó để chạy test. Smoke này chủ ý dùng
intro bypass, không claim re-verify cinematic ở lượt này.

Compile PASS; final Editor Console ground truth 0 errors/0 warnings, scene không
dirty, 0 missing scripts/references. Buffer vẫn giữ logs từ trước lượt này, không
clear Console để che history. Evidence local tại `.design/minor-handoff-cleanup/`;
chưa có Android/device QA hoặc commit/push.

### PR #3 targeted thermal fixes — 2026-10-18

`ThermalReviewFixes` tái hiện thiếu heatFrame/heatStatus và death downgrade reset
cadence trên code trước fix; sau fix PASS ba missing-binding cases, thermal-only
death boundary, gate-owned cadence, real death/respawn và full mission reset.
`PlainHud(1600)` nearby smoke PASS binding/Pause/Resume và input exclusion. Compile,
docs/diff checks PASS; final Console ground truth 0 errors/0 warnings, scene không
dirty và 0 missing scripts/references. Logs retained đều từ trước lượt fix này.
Không chạy lại historical matrix hoặc `pr-review`; verification theo phạm vi hai
findings. Thermal/lifecycle contract không thay đổi.


## M5 Region A verification

Production starts: Mission01–03 LEVEL_START / ENCOUNTER_1–3 theo số encounter thật;
Mission01 chỉ có hai Encounter. Mission04 thêm PRE_BOSS, BOSS, BOSS_PHASE_B.
DevFastStart TRANSITION chạy context handoff thật của mission có đích kế tiếp (Mission04 để thử đổi hành tinh), không alias vào wave
hoặc Mission05 giả. Phase B seed gây damage qua health owner một lần sau spawn;
đây là DevTest, không snapshot/restore Boss phase vào campaign.

`VerifyRegionA.BeginBatch(2/3)`, `BeginBossChecks`, `BeginIntegrationChecks`,
`BeginContentChecks`, `BeginFullRegion` chạy qua Pipeline/run_script trong Play Mode.
Entry Begin trả ngay; chờ `.design/m5/batch-progress.json` terminal trước probe khác.
Không chạy hai runtime probes đồng thời. `InspectRegionA.Inspect` kiểm production
catalog, retained fixture, missing scripts/references và saved dev flag.

Evidence Editor ngày 2026-10-19:

- M2 integrated Pulse: 55.68 s simulation, peak 6; M3: 74.38 s, peak 7; protected bot.
- Boss targeted PASS: Phase A/B/sweep, Pause, real target reserve 10% max HP,
  projection reset sau đổi framing, death/respawn, staged death, crate, completion once,
  Up→Right destination. Chưa physical-device/readability/fun verified.
- Một full production Region run 1× PASS: L1 35.72 s, L2 52.46 s, L3 66.66 s,
  L4/Boss/transition 82.76 s; tổng 237.60 s. Bot được bảo vệ, có một death/respawn,
  Pause, reserve use và pickup; thêm một Plasma Core test pickup để đảm bảo switch
  coverage. Không dùng số này để claim representative human balance hoặc target 6–9 phút.
- Grouped closure fix sau full run: arrival card trước spawn, Raider short burst,
  hazard entry/Supply cues và movement-owner cinematic pose. Targeted content pass:
  Supply destroy→crate→major, Supply escape không gate, Elite HP/scale/pool reset,
  steady enemy instance reuse, Boss captures 1600×900 và 2000×900.
- Persistence PASS: M2 Encounter 3 Continue/fresh storage, Retry, Boss Retry reset
  HP/phase, replay preset + exact disk bytes isolation, transition/ready reload không
  spawn Boss/duplicate records. Temp storage riêng, không sửa production save.
- Compile PASS; Console 0 errors/warnings tại checks; 0 missing scripts/references.
  Docs checker và final diff check được ghi trong phase closure evidence.

`UseTestStorage(path, fixture)` chỉ có hiệu lực Editor/Development và cho phép clone
M4 fixture catalog riêng. VerifyPersistence giữ fixtures L1 năm Encounter trong memory,
không trộn vào production catalog. Legacy Core + Retry(5) đã PASS sau M5. VerifyM4Testbed
Rig tự mount M4 fixture và restore previous level khi dispose. Không rerun historical
M4 giant suite trong M5.

Balance RA-018 vẫn Partial: cần manual Region A feel/duration pass. ADB không có device;
RA-019 Blocked, không claim Android touch/FPS/audio. Ponytail không có callable skill/tool
trong chat này dù config enabled; không claim review đã chạy. Không chạy pr-review.
Tracker live TSV đã đọc RA-001–019; connector cần reauthentication nên không ghi Sheet.
Dùng lại duy nhất `.design/m4-a/tracker-updates.tsv` cho evidence/status updates.


## M5 gameplay pacing refinement verification

2026-10-19 follow-up sau M5 checkpoint `c75634d8`; checkpoint trước sửa `6646ebcf`.
Current production pacing thay seed cũ ở section trước; timing/composition canonical
ở [levels](../content/levels-encounters.md#m5-region-a-authored-content-provisional).
`tools/unity/VerifyPacingRefinement.cs` chạy qua Pipeline/run_script trong Play Mode.
Begin methods trả ngay; chỉ một runtime probe, chờ JSON terminal trước probe khác.
FullRegionStatus đọc Task handle live trong Editor; không restart khi chỉ chậm quan sát.
Evidence local ở `.design/m5-refinement/`, không phải production save hoặc tracker mới.

| Probe | Evidence và phạm vi |
|---|---|
| A | Initial virtual tap/3.3 s approach, title còn hiện lúc spawn, M1 fast-clear .54 s, một survivor không chặn beat sau, slow fallback 7 s, natural exit |
| B + Storm correction | Mirrored Raider .52 s fast-clear gap; real Pulse kill Bomber; no-shoot M2/M4 còn 3 lives, normal count 20/24, visible peak gồm resource/Supply 7/8, runtime flow deviation <3°, deterministic seed, hazard cleanup |
| C | Sine .52 s fast-clear gap, pincer đồng bộ, Supply T2.52 khi giảm pressure; real Pulse destroy → crate/collect và escape → reinforcement |
| D | M1→M2 và M2→M3 first spawn 2.84 s; title overlap, relative drag/held input, Pause freeze, no teleport; minor transition 2.14–2.16 s gồm entry lead |
| E + Wide journey | Native 1600×900 và 2000×900: Boss death 2.40–2.42 s, reward crate, glide/accelerate/bank/turn/palette/decelerate/reveal 7.28 s, stable tap, actual UI tap, approach .7/1.8/.8, consumed input/fresh press, Right-ready, automatic projection |
| Bounded cleanup | Normal combat reserve visible impact/projection recovery; lingering visible projectile natural bound 244.77 s bị bounded grace/fade dọn trong .70 s ở final Timeline; Pause freeze, damage off trước release |
| Integration | Temp save: later M2 Continue/fresh store, Retry, Boss Retry reset phase/HP, replay exact disk-byte isolation, RegionTransition resume qua reveal/tap/approach, RegionReady direct restore; original storage bytes unchanged |
| Visuals/reviews | One design-review sau native tuning ở hai landscape aspects; unity-scriptdesign về config/runtime boundaries, lifecycle và ownership. Không Ponytail callable tool; không pr-review |

Protected fixture tests và virtual/manual interaction không thay representative human
balance. RA-018 giữ Partial, RA-019 giữ Blocked; không claim Android touch/FPS/audio.
Same tracker artifact `.design/m4-a/tracker-updates.tsv`; live Sheet TSV read đã đối
chiếu RA-001–019, không claim Sheet write khi connector cần reauthentication.

Final integration sau toàn bộ batches/review: **đúng một full Region run**, PASS.
L1 26.72 s, L2 37.14 s, L3 38.40 s, L4/Boss/Planet travel/approach 66.88 s;
tổng 169.14 s fixed simulation (2 phút 49 giây), không thêm idle để kéo stopwatch.
Sáu empty-active-field samples giữa các combat beats: trung bình .38 s, max .80 s;
không tính intentional entry/title/reward/cinematic windows vào mean này. Các samples
đếm active enemy roots, không phải phép đo pixel-visibility hoặc human feel.
Peak visible 8; có forced death/respawn, Pause, reserve use, weapon switch và reward
collection; một Plasma Core QA được thêm để chắc chắn có switch coverage. Save trong
temp path, bốn Mission records/unlocks, RegionReady; không dùng protected bot để claim
representative balance, target 6–9 phút hoặc device PASS.

Closure: compile completed/no errors; Console 0 errors/warnings sau full run;
production catalog 4 Missions/10 Encounters, retained M4 fixture 5 Encounters,
saved DevFastStart off; 0 missing scripts/references. Docs checker/diff check được
ghi cùng closure evidence. User-only L1 intro delay và runInBackground giữ uncommitted.

Post-full narrow regression: sửa lifetime cue khi Pause sau Mission title đã hiện
(trên gameplay và cuối same-planet travel): tạm hide/freeze, Resume tiếp tục remaining
time và giữ overlap tới entry. Không thay layout, spawn/transition/gameplay clocks;
chỉ chạy targeted title-pause regression, không chạy lại full Region.

Một intermediate Editor reload session phát sinh private projectile-pool null ở
PulseLaserWeapon trước fresh QA; log lưu riêng trong intermediate-editor-console.json.
Đã stop, xác nhận Edit Mode/compile completed, rồi fresh Play để dựng lại runtime;
title-pause regression còn yêu cầu actual Pulse fire. Final fresh session Console
0 errors/warnings. Không sửa weapon runtime hoặc claim live hot-reload support trong
refinement này; full integration trước đó cũng không có lỗi Console.


## M5 second human-feel pass verification

2026-10-19, working tree trên `codex/m5-region-a`, base HEAD `6c745a88`; không commit,
push, PR, merge hoặc M6. Brief ở `.design/m5-human-feel/REQUEST.txt`, native evidence
ở cùng thư mục; số gameplay hiện hành thuộc canonical content owners, không dùng
các composition assertions của old refinement A–C làm current production spec.

`ConfigureHumanFeel.PrepareVerification` ghép hai offline verifier sources thành
ignored `.design/m5-human-feel/Verification.cs` để reuse existing virtual-input Rig;
Pipeline run_script chỉ compile một input file, không resolve một ephemeral tool
assembly từ lần gọi trước. Sau prepare, chạy `VerifyHumanFeel` entry trong generated
file. Không đưa verifier vào Assets hoặc cài thêm package.

| Entry / evidence | Phạm vi |
|---|---|
| BeginFormations / formations.json | Native 1600×900, 2000×900; seven production silhouettes, settled collider separation/unique slots, Raider mid-entry |
| BeginTtk / ttk.json | Actual held primary projectiles, thermal on; Scout/Raider/Bomber/Elite, Pulse L1/L2/L3, Plasma L2/Homing L2; frozen target cách muzzle khoảng 3 world units |
| QuotaChecks / quota-pure.json | Six seeds; repeatability, source ordinal variety, normal major credit, remaining-population fallback |
| BeginRewards / rewards.json | Seven encounters × three seeds; type/count/source ordinal, no authored carrier, slow Timeline fallback |
| BeginPersistence / persistence.json | Real temp save: collect Core/Shard → Retry/Continue restore, same source ordinal, high-water anti-duplication, Replay/Dev disk-byte isolation; production bytes unchanged |
| BeginAsteroids / asteroids.json | Both native aspects, M2/M4 count/flow/size/HP/stages/Pause, safe no-shoot lane, cleanup, full region |
| BeginBoss / boss.json | Both native aspects, silhouette width, phase cancel/reposition, warning/active beam safe lane, active hit through life owner, reserve/projection/later profile, defeat crate |
| BeginBossTtk / boss-ttk.json | Moving real boss, held Pulse L1/L2/L3, thermal on, protected aim bot; records fight seconds and beam count |
| BeginClosure / closure.json | Grouped affected checks; resume only remaining probes after a verifier-bound fix, retain unrelated passes |
| BeginFullRegion / full-region.json | Exactly one final integration attempt; rejects existing evidence file; temp campaign, actual drops/upgrades and sampled empty-field/density metrics |

Sustained target benchmark (trigger → lethal; first projectile travel included):

| Target | Pulse L1 | Pulse L2 | Pulse L3 | Plasma L2 | Homing L2 |
|---|---:|---:|---:|---:|---:|
| Scout | 1.34 s | .66 s | .34 s | .72 s | .76 s |
| Raider | 1.86 s | 1.02 s | .50 s | .98 s | 1.26 s |
| Bomber | 3.72 s | 1.84 s | 1.00 s | 1.20 s | 2.74 s |
| Elite equivalent | 6.62 s | 3.38 s | 1.68 s | 1.96 s | 4.76 s |

Elite benchmark dùng fixed HP equivalent trên Bomber prefab, không giả enemy HP
scale theo weapon. Actual composite Elite geometry được xem riêng trong formation
captures. Boss Pulse L1/L2/L3 đạt 101.68/49.92/24.58 s, tương ứng 7/3/2 active beams.
Bot aim/protection và 2× wall-clock acceleration của Boss benchmark không phải
human dodge/fun approval. L1 Boss còn dài, upgraded clear nhanh; RA-018 vẫn Partial.

Storm no-shoot checks còn 3 lives ở cả hai aspects, peak normal/resource 14 (M2) /
15 (M4), runtime angle deviation <3°. Four size HP classes và damage states được
assert qua health authority. Numeric clamp được bổ sung virtual edge drag trong
camera closure; không coi numeric clamp một mình là full input evidence.

### Manual fast-start handoff

Mở `app/Assets/StarShift/Scenes/02_Gameplay.unity`. Trong Play Mode, chọn
`L1Encounters → DevFastStart`; bật `Dev Mode Enabled`, đặt `Standalone=true`,
`Skip Intro=true`, `Override Direction=false`, `Camera Override=None`,
`Deterministic Rewards=true`, `Reward Seed=410`. Chọn Start Level / Start Point theo
bảng, dùng component context menu **Replay Configured Start**. Play-only overrides
không ghi production scene; saved scene giữ dev off. `Starting Weapon Level=1` để
xem durability, 2–5 để so progression; `Unlock All Weapons=true` nếu cần switch.

| Test | Start Level asset trong Data/RegionA | Start Point |
|---|---|---|
| M1 special formation | Mission01 | ENCOUNTER_2 |
| M2 Raider | Mission02 | ENCOUNTER_1 |
| M2 Bomber escort | Mission02 | ENCOUNTER_3 |
| M2 storm | Mission02 | ENCOUNTER_2 |
| M3 Pincer/Supply | Mission03 | ENCOUNTER_2 |
| M3 Elite | Mission03 | ENCOUNTER_3 |
| M4 storm | Mission04 | PRE_BOSS |
| Boss Phase A | Mission04 | BOSS |
| Boss Phase B/beam | Mission04 | BOSS_PHASE_B |

BOSS_PHASE_B là real Boss encounter với dev health reduction sau spawn, không có
fake checkpoint. Thả fire để xem silhouette/entry; giữ fire để so TTK, drag né beam,
thử reserve và Pause/Retry. Campaign Retry chỉ được cấp qua lifecycle checkpoint;
raw DevFastStart không tự ghi CampaignSave/Profile. Để chơi toàn Region: Start Level
Mission01, Start Point LEVEL_START, Standalone=false, Skip Intro=false.


### Second pass closure and final integration evidence

Technical closure PASS: native formation captures/numeric separation ở cả hai aspects;
invalid duplicate/nonfinite/out-of-range slots và impossible/shard-major quota bị reject.
Twenty camera/aspect cases có ortho/projection và actual 100 px drag đúng mapping;
virtual drag chạm cả bốn cạnh storm và restricted Boss region. Full-cap regression giữ
normal cap và reserved guaranteed item/crate, không duplicate. Boss brace/active-render
captures bổ sung sau khi sửa pod disconnect; beam damage/safe lane, death cancel,
reserve restore và later camera profile đều PASS. Boss camera consolidation chỉ đổi
reference về existing GUID với identical view/timing, không đổi gameplay sau full run.

Một verifier wait ban đầu chỉ 600 ms, chưa bao gồm .45 s projectile grace + .25 s fade
ở outer-survivor test; đổi window thành 1.6 s và rerun test đó + phần closure còn lại.
Không đổi runtime để hợp thức hóa test; không chạy lại các probe độc lập đã pass.
Compile completed/up-to-date, final Unity Console ground truth 0 errors/0 warnings,
0 missing scripts/references; saved DevFastStart off. Docs checker và git diff check PASS.

Đúng **một full Region A run** cuối: **222.06 s (3:42.06)** fixed simulation.
Mission boundary durations L1 38.16 s, L2 60.60 s, L3 57.08 s, L4 66.22 s; L1 gồm
initial arrival, L4 gồm Boss/reward/Planet travel/tap/approach. Encounter timing/raw
metrics ở `full-region.json`. Actual Boss combat clock tới defeat 30.32 s (entry
3.5 s; hold-to-defeat 26.82 s), active Plasma L2 + reserve; `full-boss-clock.json`
đọc retained runner clock ngay sau completion, không rerun fight. Entire last Boss
segment 48.08 s gồm death/reward và outro; không gọi segment đó là pure TTK.

Full run peak visible 14, max common telegraph/shooter 1. Drop observer subscribe
synchronous owner facts để không bỏ sót pickup spawn/collect giữa hai poll; exact
12 drops: Pulse/Plasma/Homing Core mỗi loại 2, Overdrive 1, Shield 1, MajorCrate 3,
Shard 1. Cả 12 được collect; crate selection làm collected OD=2/Shield=3. Sáu upgrade
Core, một death downgrade; final loadout 2/2/2. Có actual death/respawn, Pause,
weapon cycle và reserve; **không injected Core**. Temp campaign ghi đủ bốn Mission
records/RegionReady, production storage bytes giữ nguyên.

Empty-visible-field samples trung bình **3.32 s**, max **6.60 s**, gồm cả playable
entry, Mission travel và post-death/reward windows. Khác definition của prior pass
đã loại intentional windows, không so trực tiếp với .38/.80 s cũ. Không thêm idle
để kéo duration. Protected aim/collection bot không đại diện human pace, chưa chứng
minh target 6–9 phút hoặc fun/final balance. RA-018 giữ **Partial** tới manual approval.
`adb devices -l` không có thiết bị: RA-019 giữ **Blocked**. Tracker dùng đúng existing
`.design/m4-a/tracker-updates.tsv`; phiên này không có writable Sheet connector,
không claim live Google Sheet update. Không tạo tracker mới.

Focused unity-scriptdesign và native design-review ở `.design/m5-human-feel/`.
Cleanup gộp 16 generated duplicate flights sau data/dependency checks, reuse existing
BossCamera GUID, bỏ superseded Vanguard child visuals/EnemyFirePresentation; giữ
fixture, legacy transit/layout consumers và useful regression tooling. No final art,
new packages/Editor version, minion framework, phase reward, commit/push/PR/merge/M6.

## M5 balance and transition follow-up

`ConfigureBalancePass.Configure` áp dụng tuning mới qua Unity API/Edit Mode, chỉ save
các asset đã sửa; không save scene đang dirty. Không rerun builder lịch sử để reset
current content. `PrepareVerification` ghép existing Rig, VerifyHumanFeel và probe mới
vào ignored `.design/m5-balance-pass/Verification.cs`; evidence mới có folder riêng,
không ghi đè measurements của pass trước. Chờ JSON terminal giữa các runtime probes.

| Probe | Coverage |
|---|---|
| VerifyBalancePass.BeginSystems | 15 patterns Lv1–5; Core auto-select/max/death; Supply pair khi gate đóng và cap đầy; ejection Pause; capture stat một lần; shooter peak 1/4/6; Boss fan counts |
| BeginProgression | Real projectile TTK trên Bomber; deterministic two-item floors cho 3 seeds × 2 attempts; branch heat không reset khi Core đổi súng |
| BeginCameras | Intro.Enabled false/true, first-entry zoom, M1→M2 monotonic zoom; Planet 2 Brake→control pose continuity ở 1600×900 và 2000×900 |
| BeginPersistence | Lv5 disk roundtrip; save Lv1–3 cũ hợp lệ, Lv6 bị reject; Retry/Continue restore; Replay/Dev và production bytes isolation |
| BeginFullRegion | Một lượt dùng real primary aim bot sau grouped prerequisites; NewGame→4 missions→Boss→Planet ready, pause/death/reserve/pickup/save |
| BeginBossVisual | Native Game View của six-shot cannon và five-shot missile fan sau khi các viên đã tách ra |

Targeted probe results và giới hạn acceptance nằm trong evidence folder; duration/
fun balance vẫn cần human playtest. Các bảng TTK của second pass ở trên là historical
measurements trước khi giảm HP; không coi chúng là số đo tuning hiện tại.

### Tắt intro đúng asset và test nhanh

Scene production: `02_Gameplay → L1Encounters → LevelFlow → Level` trỏ tới
`Data/RegionA/Missions/Mission01.asset`. Trong asset đó, mở **Intro → Enabled** và bỏ chọn để
bỏ establishing intro khi Play từ `LEVEL_START`. `Data/Fixtures/Levels/EncounterSystemsFixture.asset`, ID
`M4_FIXTURE`, chỉ điều khiển system testbed, không điều khiển production Mission01.
Tooltip trên LevelFlow/Level và Intro/Enabled nhắc rõ ranh giới này.

Để chỉ bỏ intro trong lượt test: vào Play Mode, chọn `L1Encounters → DevFastStart`,
bật **Dev Mode Enabled**, **Skip Intro**, đặt **Start Level=Mission01**,
**Start Point=LEVEL_START**, rồi context menu **Replay Configured Start**.
`Starting Weapon Level` hỗ trợ 1–5; bật `Unlock All Weapons` để so cả ba nhánh.
Các Start Point formation/storm/Boss ở bảng fast-start phía trên vẫn dùng được.
Dev default trong scene giữ off; thay đổi Play Mode này không cần save scene.

Shooter config ở `Data/RegionA/Encounters/<Encounter>.asset → Active Shooter Limit` (1–6),
`Shot Spacing`, `Shots Per Volley`, `Volley Pause`. Exact production seeds thuộc
[level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).

### Follow-up verification results

Grouped probes và đúng một full Region mới PASS. Real held Pulse/Bomber HP19,
distance3: Lv1/2/3/4/5 = 3.22/1.68/.82/.66/.46 s; Plasma L3/L5 .64/.42 s,
Homing L3/L5 1.74/.70 s. Đây là protected frozen-target benchmark, không thay human aim.

Camera 16:9/20:9 không có reverse tại M1→M2; largest sampled size step .00724/.00548,
second-intro handoff gap 0 viewport trong lần cuối. Actual DevFastStart Skip Intro
true/false cũng PASS trên production Mission01. Native shots/captures chứng minh
cannon six + missile five; riêng limit6 chỉ là configurable stress case.

Full Region 187.96 s: L1 45.28, L2 52.32, L3 41.26, L4/travel 49.10. Peak14,
max4 shooter; 20 spawned/collected pickups, 14 Cores, final5/5/5; one forced death,
Pause/switch/reserve và 4 profile records, RegionReady; original production save
bytes unchanged. Maxed protected bot Boss clock 12.56 s gồm entry/death: độ khó khi
max loadout cần human feedback, không claim 45–75 s fight hoặc 6–9 min human run.

Final compile up_to_date; Console ground truth 0 errors, 1 warning từ Unity Pipeline
vì request Play khi Editor đã Play (tooling, không là gameplay warning). Catalog +
fixture inspectors: 0 missing scripts/references, dev default off. Scene dirty từ
đầu được giữ, scene file/fixture intro/runInBackground không đổi trong follow-up.
Docs/diff checks PASS. Không chạy lại full historical matrix, không có Android thật.

## Planet 1 flow polish verification

Latest feedback pass dùng `tools/unity/ConfigureFlowPolish.cs` và
`tools/unity/VerifyFlowPolish.cs`; generated runtime probes/evidence nằm trong
`.design/m5-flow-polish/`. Tuning mới theo [enemy owner](../content/enemies-bosses.md#m5-runtime-seeds-tunable),
[weapon/reward owner](../content/weapons-items-drops.md#encounter-reward-floor-provisional)
và [level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).
Các số đo pass trước ở trên là historical; không dùng làm TTK của HP/pattern mới.

Production `Mission01 → Intro → Enabled` điều khiển establishing intro. Có thể tắt
trên local test config để vào nhanh; bật lại để kiểm tra approach. Để thử nhanh mà không đổi
production config, dùng DevFastStart trong Play Mode theo hướng dẫn phía trên;
`BOSS_PHASE_B` chờ Boss hết entry protection rồi mới giảm HP để vào Phase B.

Folder fixtures đã đổi tên; các builder/verifier cập nhật đường dẫn, GUID giữ nguyên.
Không xóa các flight seed cũ còn được historical builders hoặc tests sử dụng.
Native Editor từng crash GTK focus trong lúc test; automation tái dùng GameView đang
mở để tránh gọi focus mỗi lần đổi aspect. Không suy ra đây là lỗi Android runtime.

### Flow polish results

Grouped targeted probes PASS tại 1600×900 và 2000×900: 15 weapon patterns/aspect,
real held fire trong initial và second approach; không cancel held gesture tại finish.
Largest sampled handoff movement dưới .000002 world units, initial Brake không reverse.
Đây là boundary sampling, không phải claim mọi frame trên mọi thiết bị đều hoàn hảo.
Scout HP4 được hạ bằng real held Pulse L1 trong 2.18 s gồm thao tác align và shot travel.
Storm còn 7 visible asteroids tại deadline; chờ thêm 4.40 s tới khi chúng bay ra,
không có asteroid bị release giữa viewport. Boss entry damage bị từ chối trước Hold;
28 gold + major pair xuất hiện, có sprite, Pause freeze và giữ collectible trong grace.

Save regression PASS: old-save compatibility, max-five roundtrip, reject level six,
Retry rollback, Replay/Dev isolation và production bytes unchanged. Actual DevFastStart
`BOSS_PHASE_B` PASS sau entry protection, không ghi production save.

Đúng một full Region integration run PASS: 173.84 s; Mission clocks 26.24 / 53.04 /
36.42 / 58.14 s (L4 gồm reward/travel). Peak 12 enemies, max 3 common shooters.
72 pickups gồm 13 Cores, 28 normal/resource Shards, 28 boss gold và 3 support items;
final levels 5/5/4 sau một forced death. Có Pause/Resume, cycle weapon, reserve missile,
4 completion records và RegionReady. Boss clock 20.62 s gồm entry/death; không coi là
pure combat TTK. Protected aim/collection bot, seed 410, không inject Core; chưa thay
human acceptance. Playable empty-field mean/max 3.18/7.16 s gồm entry và travel.

Final scene/data/prefab reference inspection PASS (zero missing references), fixture
GUID giữ nguyên, Dev mode saved off. Final evidence ở `.design/m5-flow-polish/`.
Android touch/performance và final fun/difficulty chưa verify; RA-018 vẫn Partial,
RA-019 vẫn Blocked vì chưa có device run.

## Thermal independence and asset cleanup

Checkpoint trước khi đổi sang heat dùng chung: `VerifyHeatLifecycle` qua
`OrganizeRegionA.PrepareHeatVerification` PASS: real held fire tạo 8.10 heat; release/press giữ nhiệt; gate block vẫn
nguội 63 → 46.80 và không bắn; Pause freeze; inactive branch nguội; Overheated hồi
và held fire tiếp tục. Live `CampaignLifecycle.AdvanceMission(..., true)` giữ chính
xác ba heat states 45 / 55 / 42.5 rồi tiếp tục nguội. Session reset vẫn về zero.

Folder migration PASS: 68 assets giữ GUID; 6 obsolete generated flights có zero
asset dependencies trước khi xóa. Các callers/builders cập nhật qua Editor-only
`RegionAAssetPaths`; detailed folder contract thuộc
[asset pipeline](../production/asset-pipeline.md#region-a-authored-data-layout).
Scene/prefab/data reference inspection sau migration PASS, Dev mode saved off.

Một full Region trên bản thermal + layout mới PASS: 179.92 s, peak13 enemies,
max3 common shooters, 72 pickups, final levels 5/5/4 sau một forced death. Có
Pause/Resume, switch, reserve, bốn completion records và RegionReady. Boss clock
21.30 s gồm entry/death. Đây vẫn là protected aim/collection bot, seed410, không
inject Core; không phải manual difficulty hoặc Android FPS acceptance.
Evidence local ở `.design/m5-heat-cleanup/`. Không chạy full historical M4 suite.

### Shared primary heat correction — 2026-10-20

Theo feedback mới, mọi loại đạn dùng một thermal state; mechanic thuộc
[weapons owner](../content/weapons-items-drops.md#primary-weapon-heat--m35).
`VerifyHeatLifecycle.Begin` PASS trong Unity Play Mode: real held fire 8.10 heat;
release/press giữ nhiệt; gate block nguội 63 → 45.60, không thêm shot; Pause freeze.
Nhặt Plasma/Homing Core, tăng level và cycle cả ba branch giữ chính xác heat và
cooling delay; cycle hoặc nhặt Core khi Overheated không mở khóa fire. Recovery
vẫn tự bắn lại khi held; live Mission handoff giữ 55 → 55 rồi tiếp tục nguội;
death và session reset về zero/unlatched. Compile PASS, native Console zero errors
và warnings. Evidence local `.design/m5-shared-heat/heat.json`.
Không chạy lại full Region hoặc Android cho sửa thermal state này; full Region
ở checkpoint trước không được coi là runtime evidence của bản shared heat.


### Pickup bounce consistency — 2026-10-20

`VerifyPickupMotion.Run` PASS trong Unity: normal/grace cùng quỹ đạo và nảy hai lần;
late grace không kéo xu ngược lên; 28 boss gold trajectories nảy hai lần rồi viewport
exit; pooled state reset. Core/support vẫn giữ trong grace, Pause/Resume và collect
một lần PASS qua frame thật. Motion dùng controlled physics callback stepping;
không coi đây là full boss fight hoặc device evidence. Compile PASS và native Console
zero errors/warnings. Reproduction ở [tool README](../../tools/unity/README.md#pickup-bounce-consistency),
evidence local `.design/m5-pickup-motion/result.json`.


### Respawn projectile and held-input continuity — 2026-10-20

`VerifyFlowPolish.RespawnContinuity` PASS trong Unity Play Mode qua combined script
của `ConfigureFlowPolish.PrepareVerification`. Với mouse và synthetic touch: giữ
pointer xuyên Dying/entrance; delta bị khóa không teleport; sau entrance kéo
30×20 px nhận đúng 30×20 px, held fire tự tiếp tục. Accepted death giữ hostile và
friendly shots đang bay, hostile tiếp tục di chuyển; duplicate hit không trừ thêm
life và post-entry protection vẫn chặn damage. Pause còn yêu cầu fresh press;
Ở checkpoint lịch sử này Game Over cleanup đạn; current policy thuộc
[player owner](../gameplay/player-combat.md#game-over). Evidence `.design/m5-respawn-continuity/result.json`.
`VerifyPlayerDrag.Run` PASS short/long/release, projection 16:9/20:9 × ba zoom,
held drag sau movement re-enable và touch/second-finger isolation. Cần bắt đầu run
skip-intro trước suite này; chạy ngay sau terminal fixture với overlay còn mở sẽ
bị UI chặn pointer. Compile và native Console không có error/warning mới.
Không chạy lại full campaign hoặc Android cho sửa này.


## M6 Region B verification `[PROVISIONAL]`

Production catalog L1–L8; M6 current release dừng sau Warden, không có L9 content.
DevFastStart dùng existing StartLevel và exact Encounter IDs. L6 thêm SNIPER/CORRIDOR;
L7 CARRIER/DUAL_SNIPER; L8 PRE_BOSS/BOSS/BOSS_PHASE_B và special VICTORY route.
VICTORY chạy real boss/module/death path trong DevTest; không giả campaign completed
hoặc ghi save. BOSS_PHASE_B phá các node sau entry qua health authority, không restore
serialized phase snapshot. Production scene DevModeEnabled giữ OFF.

`tools/unity/prepare_region_b_verification.py` ghép `VerifyRegionB`, `VerifyPickupMotion` và existing
`VerifyPacingRefinement.Rig` vào ignored `.design/m6/Verification.cs` cho MCP run_script.
Không duplicate input/Game View rig, không đưa verifier vào Assets. Probe CurrentTask
cho biết work còn live; chỉ chạy probe tiếp khi task trước terminal. Entry/usage ở
[authoring README](../../tools/unity/README.md#m6-region-b-authoring-và-verification).

Evidence từ protected bot/virtual Input System và native Game View chỉ chứng minh
Editor behavior; không chứng minh Android touch, FPS, audio, human difficulty hoặc
representative human mission duration. Không chạy full historical M5 matrix trong M6.

Editor evidence M6 ngày 2026-10-20:

- Fresh Unity compile PASS; targeted Inspector/ref validation và Console check ở closure.
- Native Game View: 17 major formations × 1600×900 và 2000×900, 0 settled collider
  overlaps ở cả hai aspects. Final proxy/beam-arrow review thêm 6 captures.
- Mechanics PASS: Sniper charge/fire/Pause; dual Sniper max 1 busy sequence; Interceptor
  committed strike; Carrier spawn/cap/death retreat; corridor right→left và natural ExitDrain.
- Player Right PASS: virtual mouse delta (55,35) pixels; Pulse/Plasma/Homing held fire;
  shared heat qua switch, gate-disabled cooling, Pause freeze, death/downgrade/respawn
  và held drag/fire continuity.
- Warden PASS: entry protection cho core/modules, shield block, 3 node deaths, collapse,
  exposed phase, Beam Sweep/Plasma Burst/Drone Spawn, staged death/reward và completion once.
- Trước human-feedback pass bên dưới: sáu real-weapon boss probes `[TUNABLE]`: Lv3 Pulse/Plasma/Homing khoảng
  48.84/75.22/71.08 s simulation; Lv5 khoảng 27.66/68.78/40.66 s. Core 360 HP
  cố định; cả sáu có beam/plasma/drone và shield loop. Bot được bảo vệ, Plasma dùng
  forward firing position phù hợp range; không phải human difficulty benchmark.
- Persistence PASS: L6 Continue/Retry, Warden Retry reset nodes/phase, Replay bytes isolation,
  legacy L4 RegionTransition/RegionReady → L5, completed-save Continue không spawn lại boss.
- Real Vanguard death/reward → journey → approach → L5 PASS, held gesture/pose giữ;
  max sampled camera step .263 ortho ở sim speed 2, L4 record đúng 1 và L5 entry saved.
- Incoming mission travel Retry regression PASS: Pause/Retry trong L5→L6 khởi động L6 E1
  đã commit, không replay outgoing L5. Đây là grouped persistence boundary fix.
- Supply Right PASS bằng held Pulse: destroy → Core/support pair trực tiếp → collect;
  không legacy BOX và SupplyCaptured đúng 1. Chín DevFastStart routes PASS, production bytes unchanged.
- Trước human-feedback pass bên dưới: continuous full Region B PASS trên reward configuration M6 đầu: L5 61.20 s,
  L6 59.88 s, L7 62.22 s, L8 119.08 s; tổng 302.38 s simulation, peak EnemyHealth 17.
  Played đủ 11 encounters bằng actual held weapons, protected aiming bot ở sim speed 3.
  Bốn Mission records, unlock tới L8, CampaignCompleted saved và Continue victory PASS.
  Observed entry/exit weapon levels chưa tự max: sau L5/L6 [3,2,2], sau L7 [4,2,2];
  bot không cố thu hết pickup, không dùng số này thay economy approval.
- Tất cả campaign probes dùng storage riêng và compare production save bytes. Không
  đổi schema/storage path, không author Region C, không Android QA/FPS/audio claim.

Evidence local `.design/m6/` gồm mechanics, boss, boss-balance, player-right, persistence,
mission-travel-retry, astra-to-crimson, supply-right, dev-routes, formations-1600/2000,
presentation và full-region-b JSON; screenshots không commit. Các failed draft probes
được sửa theo runtime/contract rồi chỉ chạy lại nhóm liên quan; không full historical suite.


### M6 human-feedback pass — 2026-10-20 `[PROVISIONAL]`

Actual Editor probes sau front-guard/bounds/idle/cadence changes:

- `BeginPressure` PASS trên L1 E2, L3 E3, L6 E1, L7 E2, Vanguard và Warden.
  Busy sources tối đa 1/2/1/3/2/2, đều dưới authored limit và hard cap 4.
  Collective quiet gaps >=3 s; peak live projectile counts 3/4/2/6/16/10.
  Vanguard/Warden có escort spawn từ hull/pod; không dùng edge spawn giả.
- `BeginCoverAndIdle` PASS: L6 Bulwark 48 HP nhận 9 damage từ held Pulse, rear
  Sniper chưa mất HP; formation delta cùng hướng/biên độ, camera footprint clamp đúng.
- `BeginGoldRain` PASS real Warden entry → nodes → core → staged death: 36 gold,
  rải theo screen height và trôi trái, grace hoàn tất, CampaignCompleted saved;
  production bytes unchanged. Lỗi cũ là Warden dùng Supply table có BossShardCount 0.
- `BeginVanguardTuning` PASS bằng held Pulse L3: 680 HP, hai phase, 53.12 s simulation
  gồm staged reward; 10 escort spawns tổng, peak living 3, giữ 28 gold.
- `BeginHomingPaths` PASS 8 Up/Right × L1/L5 ×1/2/3 target cases. Compact arc vẫn
  phân biệt từng rocket trong native captures, assignment đều, actual damage đủ;
  Pause, dead/reused target và reuse sang Pulse không giữ tracking/arc.
- `BeginHomingBalance` PASS Warden 520 core +3×110 nodes bằng real held Homing:
  L3 96.48 s, L5 53.12 s simulation. Bot protected; forward viewport .29 để nằm
  trong tracking range sau khi arena rộng hơn. Mẫu cũ đứng .17 đã timeout ngoài
  tầm lock, không dùng số đó làm balance acceptance. Không tăng weapon range.
- 17 formation views ×1600/2000 PASS 0 settled overlaps sau guard authoring.
  Bản hint 3.5% alpha ở lượt này đã bị thay bằng reactive boundary cue bên dưới.
- `BeginObjectives` PASS: M2 Raider, M3 Sine, M6 Sniper, M8 Fortress giữ required
  enemies sau deadline cũ, không auto-clear/escape; clear và optional cleanup được
  sau kill. Non-kill encounters còn lại giữ transit/survival làm nhịp nghỉ xen kẽ.

Probe evidence local `.design/m6/feedback-*`, `homing-paths`, `homing-balance`,
refreshed formations và screenshots. Đây là protected bot/controlled input trong
Editor, không thay representative human difficulty hoặc Android QA.


Integration trước correction cuối về boundary cue và no-target Homing: `BeginFullRegion` PASS đủ 11
encounters L5→L8→Warden→victory, 292.20 s simulation (58.22 / 64.18 / 68.10 /
101.70 s theo mission), peak 17 EnemyHealth. Actual held weapons, protected aim/
collection bot ở sim speed 3; bốn records, tám unlocked missions, CampaignCompleted
saved và Continue terminal PASS, production bytes unchanged. Collected loadout sau
L5 [4,3,2], L6 [4,4,3], L7 [4,4,4]; không injection Core trong flow.
Không coi thời gian bot hoặc số pickup thu được là human balance acceptance.

Correction cuối theo feedback về hộp giới hạn và viên Homing giữa:

- Bỏ hẳn mask thường trực, bỏ inset hai bên/đáy. Vẫn dùng normalized camera region
  và PlayerMovementBounds hiện có; chỉ thêm telemetry BoundaryPressure cho visual.
- `BeginBoundaryHint` PASS Up/Right: idle/giữ bắn không vẽ, cố đẩy cạnh .2 s mới hiện
  đoạn .14 viewport ×2 pixel, kéo vào trong hoặc nhả tay thì tắt. Native idle/held
  Game View captures đã review, không tách màu nền hoặc khung hộp.
- `BeginHomingEmpty` PASS 10 cases Lv1–5 ×Up/Right: no-target fan đối xứng, gap ratio
  dưới 1.15; center Lv1/3/5 lệch trục <.001 world units. Lv5 Right đo ~4.5e-7.
- `BeginHomingPaths` rerun PASS 8 targeted cases sau center correction: chia đều
  mục tiêu, damage thật đủ, bốn đường ngoài vẫn cong khi có một đích; Pause và pool
  reuse không leak target/arc. Central rocket của volley lẻ giữ đúng trục launch.
- `BeginPlayer` rerun PASS: drag (55,35), cả ba branch, shared thermal state,
  cooling khi gate khóa, Pause, death/downgrade và held drag/fire sau Right respawn.
  Failed draft “Respawn lost held fire” không tái hiện trong fresh controlled rerun.
- `BeginHandoff` rerun PASS sau half-region correction: real Vanguard defeat → reward
  → planet journey/approach → L5; held gesture/pose giữ, max camera step .2195 ortho
  ở sim speed 2; L4 record đúng một, L5 checkpoint saved, production bytes unchanged.
- Final Edit Mode inspection PASS: tám mission hợp lệ, 11 Region B encounters,
  missing scripts/references 0/0, Dev mode OFF; scene không dirty, không compile/import
  đang chạy. Console không có warning/error. `python tools/check_docs.py` PASS
  19 docs/388 relative links và `git diff --check` sạch. Sau correction cuối chỉ
  rerun nhóm liên quan; không lặp full historical matrix hoặc Android benchmark.

### M6 threat/boss/UI closure — 2026-10-20 `[PROVISIONAL]`

Sau source/module cleanup và authored tuning, Editor verification:

- `BeginThreatProtection` PASS 21 cases: bảy encounters ×Pulse/Plasma/Homing Lv3.
  Threat sống lúc ra đòn đầu ở 3.66–4.26 s simulation; còn frontline guard.
- `BeginBossBarrage` PASS Vanguard/Warden ×1600/2000: ba nhịp, kéo thật vào khe
  không mất mạng, đứng ngoài mất đúng một life; Warden đổi hành lang ba lần.
  Pause/cancel PASS; rail endpoints trùng mép damage gap; node-damage HUD cập nhật.
  Origin trước mặt boss, không sinh hàng đạn đè thanh tên.
- 17 formation captures ×hai aspects PASS zero collider overlaps. Sau chỉnh các
  hàng sát HUD, `BeginClosureLayouts` rerun sáu changed layouts ×hai aspects PASS
  zero overlap, hull không clip hoặc chồng top/right HUD. Representative native
  changed-layout và boss captures đã review.
- `BeginPressure` PASS sáu scenarios, max sources 1/2/1/3/3/2; collective quiet gaps
  3.00–3.28 s. Peak projectiles 3/4/2/6/49/7; Vanguard peak gồm đợt curtain có hình
  rõ và hữu hạn, không stream liên tục. Hai boss vẫn spawn escorts.
- `BeginVanguardTuning` PASS held Pulse Lv3: 680 HP, hai phase, 53.68 s simulation,
  10 escorts tổng/peak 3, 28 gold; production save bytes unchanged.
- `BeginFullRegion` PASS L5→L8→Warden→victory, đủ 11 encounters, 330.30 s simulation
  (65.64/70.16/78.06/116.44 s), peak 17 enemies. Protected aiming/collection bot,
  real held weapons ở speed 3; bốn records, persisted victory/Continue PASS.
  Collected loadout sau L5 [4,2,2], L6 [4,3,3], L7 [4,3,4]; không inject Core.
- `BeginPersistence` PASS Continue/Retry, boss nodes/phase reset, Replay isolation,
  legacy L4→L5 và completed Continue; production bytes unchanged.
- `BeginPlayer` PASS ba branch, relative drag (55,35), shared heat, cooling khi
  blocked, Pause, death/downgrade/respawn và held continuity.
- Cả sáu probes integration chạy nối tiếp PASS; Console không warning/error.

Draft pressure failure được truy ra fixture tắt AI trong isolated barrage test nhưng
chưa phục hồi trên pooled instances. Đã restore đúng enabled states trong finally;
barrage→pressure và cả chuỗi integration rerun PASS. Không sửa cadence để che lỗi test.
Một queued Editor hot reload ở draft trước làm mất nonserialized pools trong phiên
đang Play; fresh Play và chuỗi final giữ reload lock không tái hiện. Không claim
hot-reload support hoặc human/device difficulty benchmark.

Cleanup: 18 scripts được nhóm theo responsibility, giữ GUID; bỏ 11 generated
formation/flight assets không còn inbound references. Không đổi namespace/assembly,
Editor/packages/save schema hoặc ProjectSettings ngoài scope; không commit/push.
Evidence ignored local: `.design/m6/closure-*`, refreshed pressure/Vanguard/full-region/
persistence/player JSON và native screenshots; test tooling ở `tools/unity`.

Final closure checks sau script move/whitespace-only serialization cleanup: compile
xong, gameplay scene không dirty; missing scene/prefab scripts/references 0, missing
beam materials 0, unused generated formation/flight candidates 0. GUID check giữ cả
13 tracked scripts đã chuyển và metadata của 5 script mới. Fresh-Play `BeginPlayer`
smoke PASS sau lần move cuối; Console 0 warning/error. Docs checker PASS 19 docs/
389 relative links, `git diff --check` sạch. Workspace để lại Edit Mode.

Follow-up input regression: QA rig phải gỡ synthetic devices và phục hồi native
input trước cleanup gameplay, kể cả khi dừng Play hoặc reload cắt ngang async probe.
`BeginTapInput` kiểm cú click qua UI thật để rời establishing, rồi assert không còn
device giả hoặc native device bị disable thủ công. PASS cả normal disposal và
manual interrupted-Play/re-enter smoke; Console không warning/error.

Wallet follow-up: `BeginShardBalance` PASS actual HUD balance qua same-region và
Up→Right handoff, mission-local stats reset, Retry high-water deduplication,
Continue, New Game giữ wallet, Replay isolation và Dev wallet carry/reset.
Production save bytes unchanged. HUD không đọc mission-local collected counter
làm currency nữa; authority/policy ở [economy owner](../gameplay/progression-economy.md#m4-a-runtime-boundary-provisional).

Delivery naming cleanup: đổi 76 generated camera/formation/flight assets bằng
AssetDatabase, giữ GUID; tên mô tả encounter/combat view/entry direction/required
hold, bỏ temporary Feedback/M6 prefixes. Bỏ thêm tám Region B camera assets không
có inbound references; không retune gameplay data. Builder dùng cùng tên mới.

PR #7 review closure: `BeginReplayContinuation` xác nhận rời Replay/DevTest để Continue
Campaign khôi phục đúng snapshot persistent (reset `CurrentIndex = 0` và clamp bounds,
không rò rỉ isolated encounter index hay mode). `BeginRegionSaveFailure` xác nhận
transaction-safe khi lưu đĩa thất bại trong major transition (in-memory rollback về L4/`RegionTransition`,
pause session, không advance level hay vào Result, recovery mượt qua Resume hoặc Continue reload).
Cả hai probe PASS, 0 console error/warning, production bytes nguyên vẹn.


## M7 shell, Results và Hangar verification `[PROVISIONAL]`

Ngày 2026-10-20, Unity 6000.5.9f1 Editor, production scene `02_Gameplay`, Dev OFF.
Authoring/inspect qua Unity MCP/Pipeline; regression source ở
[VerifyGameShell](../../tools/unity/VerifyGameShell.cs). Evidence JSON và native Game
View PNG giữ cục bộ trong ignored `.design/m7/`; không đưa QA captures vào Assets.

| Entry / evidence | Coverage |
|---|---|
| Core / core.json | NewGame/clear/settings rollback, best score/time guards, backup, invalid/unsupported preservation, explicit recovery/archive và cosmetic spend/equip transaction |
| Teardown / teardown.json | Cleanup an toàn/idempotent khi Player/backdrop đã bị hủy; scene-relaunch kiểm object lifetime thật |
| Stats / stats.json | Eligible death once, pooled reuse, escape/pressure/asteroid/shield-node exclusions, HUD authority, active/Pause time |
| Input / input.json | Real multitouch second-finger Pause, canceled held intent, fresh Resume, Escape hierarchy và Pause checkpoint Retry |
| Input cleanup / input-cleanup.json | Normal Dispose và interruption callback khi thoát Play/reload trả native mouse/touch, bỏ synthetic devices; cleanup input chạy trước storage/UI teardown |
| Failures / failures.json | Region A→B failed destination transaction rồi Resume recovery, background pause, corrupt read retry, backup notice, failed NewGame return/retry, AudioSource volume consumers |
| Recovery / recovery.json | Invalid save confirmation/cancel, archive original bytes, successful fresh Campaign; không overwrite unsupported save |
| Hangar / hangar-1600.json, hangar-2000.json | Shared stage, Workshop owned-only/Store navigation, ba slots/sáu options, preview/purchase/cancel/re-equip, wallet once, rollback, reload/NewGame preservation, runtime chassis và Replay/Dev denial |
| Layouts / layouts-1600.json, layouts-2000.json | Menu/Missions/Settings Bật-Tắt/Help bốn khối/HUD có vũ khí/hỗ trợ/Pause/confirmation và clear save-error/retry rồi tự tiếp tục tại native aspects |
| Loading / loading.json | Real Continue/Retry, runner elapsed + playable timer frozen trong loading, clock release; một focus-loss event trước/sau RunStarted vẫn giữ Pause, fresh input |
| Production damage / production-damage.json | Reload scene thật, lifecycle enabled từ saved scene trước fixture, Continue vào combat không Protect/enable override, một hit giảm lives và production bytes giữ nguyên |
| Terminal / terminal-layouts-2000.json | Wide Boss HUD, GameOver/Retry, Victory, overwrite confirmation và Replay GameOver/Retry |
| Scene relaunch / scene-relaunch.json | Hai lần recreate production scene; completed Continue không spawn/regrant và Region B Encounter checkpoint đúng |
| Full flow / flow.json | Real Menu/NewGame/Pause/Settings/automatic clear/Pause/Menu/storage reopen/Continue/Missions/Replay/result/exit/Continue/GameOver/Retry, sau đó L2–L8 tự handoff/journey tap, tám records, Victory và completed Continue |

BeginFlow dùng protected Player, time acceleration và existing EnemyHealth damage
để rút ngắn combat; shell transitions vẫn đi qua nút/pointer thật. Đây là functional
Editor evidence, không phải manual balance approval hoặc device FPS. Save của probe
nằm trong unique temp directory; primary/.bak/.invalid production được so bytes trước
và sau. Scene-relaunch recreate object/storage lifetime trong Editor, không claim
Android process-kill behavior. VerifyPersistence.Core được chạy như nearby schema/
JSON/high-water compatibility regression.

Game View functional review gồm 1600×900 và 2000×900: ship-led Main Menu, ba Hangar
tabs/purchase/error, Missions, Settings, Pause, GameOver, clear, Victory, HUD,
confirmation/loading và invalid/recovered state. Bỏ build/filler menu copy, thu gọn
trail để không chạm ship name, shell opaque để không hiện tàu world phía sau.
Product pass cuối dùng một ShipStage không nhận input cho Menu/Xưởng/Cửa hàng,
selection viền sáng, copy tiếng Việt, toggle Bật/Tắt và How To Play bốn khối.
HUD thay shorthand bằng tên đầy đủ; labels hỗ trợ tách khỏi vị trí tàu.
Prefab authoring qua LoadPrefabContents giữ GUID và local IDs của object còn dùng.
Static checks: docs checker và diff whitespace; native inspector kiểm missing
scripts/references, required UI refs, catalog defaults và production build entry.

Không làm final art/font/pixel spec, Android build/certification, physical haptic/audio
QA, controller feature hoặc FPS profiling. Current audio foundation chưa có final
clips; kiểm volume routing không chứng minh final mix. Chỉ terminal PASS của probe mới là evidence hoàn thành.

## Presentation polish follow-up — 2026-10-21

Grouped native evidence ở `.design/presentation-polish/`, manifest/source routing thuộc [asset pipeline](../production/asset-pipeline.md#presentation-polish-follow-up--2026-10-21). Các scope mới: factory reset Menu/Pause và failure paths; zero-heat vent/held resume/Pause; Coin UI/world; life-loss generic fragments; natural projectile exit/pool; travel và continuous region blend; hit/focus/boss skills/armor ở 1600×900 và 2000×900; common enemy death one-shot/no-reward/pool và Supply 6 HP; Bulwark telegraph/single shot/5 s cooldown/shared firing cap. Full mission đại diện đi qua năm encounter với real input, reward, Pause, death/respawn, reserve và terminal cleanup; không phải full Campaign hay Android evidence.

Trong một draft Pause-reset fixture, coroutine New Game còn pending khi fixture trả storage về production. Đã dừng fixture và khôi phục checkpoint L2 từ backup; Profile và Settings đối chiếu giống nhau. Các probe sau đối chiếu bytes, fixture có shell transition phải đợi `IsBusy == false` trước khi trả storage cả trong finally. Không claim lịch sử backup trước sự cố giữ nguyên. Chi tiết recovery chỉ lưu local, không đưa save cá nhân vào Git.

Test nhanh đổi hành tinh: trong scene `02_Gameplay`, chọn `L1Encounters` → `Dev Fast Start`. Route đã chuẩn bị **Start Level = Mission04**, **Start Point = TRANSITION**, **Standalone = off**. Bật **Dev Mode Enabled** rồi Play để đi thẳng vào hành trình Astra→Crimson; raw start chạy DevTest, không ghi CampaignSave/Profile. Sau test tắt checkbox để khởi động về Menu bình thường. Saved default của checkpoint này để OFF; đã smoke Menu với save L2 và Console sạch sau khi sửa start point trống.


## Current dev Inspector và art closure

Start Point của `DevFastStart` là dropdown theo Start Level: đầu mission, encounter
thực, AdditionalStarts (gồm boss phase B), chuyển cảnh nếu có mission kế tiếp và
Victory nếu mission có boss. ID cũ không hợp lệ hiện cảnh báo và không được replay.
Validation chạy trước khi dừng/reset lượt hiện tại. TRANSITION dùng handoff thật
cho cả cùng hành tinh và đổi hành tinh; không cần mission có encounter BOSS.

Inspector có nút chạy lại route, Pause/Resume và Menu trong Play Mode; replay cần
Dev Mode, các thao tác bị khóa khi shell đang xử lý. Camera/hướng/seed nằm trong
foldout nâng cao. Không thêm debug runtime manager mới; giữ EncounterDebugView
(bounds/path/spawn/hitbox/FPS/labels), mặc định mọi flag OFF. Utility Editor không
được đọc state runtime chưa Awake khi xem scene ngoài Play Mode.

Current probes và cách chạy ở [Unity tools](../../tools/unity/README.md#current-art-closure-và-dev-tools).
Các checkpoint M2 bên trên là lịch sử: 20 bootstrap/fixture đã retire sau khi kiểm
callers; shared rigs và current targeted regression vẫn giữ. Art closure dùng
storage riêng, disk snapshot và byte-compare primary/backup/invalid sau cleanup;
không chạy chồng probes hoặc sửa save người chơi để test.

Final closure checkpoint 2026-10-21: `VerifyArtClosure.Begin("all")` PASS trên final
source; native references kiểm 3.061 owners, 186 PNG và dropdown 48 routes/8 missions.
VFX 16:9/20:9, enemy/Bulwark deaths và fixture năm encounter đã PASS; saved dev/debug
OFF, scene sạch. Save byte guard chỉ chứng minh các lượt verification sau recovery;
không thay giới hạn sự cố đã ghi ở trên. Không claim full Campaign/Android/FPS.

Death-world follow-up: `VerifyArtClosure.Begin("death-world")` PASS cho bốn boss
skills và Interceptor/Sniper/Bomber qua mất mạng, hostile/friendly shots, respawn,
Game Over giữ/freeze world, không spawn/complete khi hết mạng, Menu cleanup và
Retry checkpoint qua shell. Save byte guard PASS; strike và beam crossing regression
được chạy lại. Các fixture lịch sử có assertion terminal cleanup đã đổi theo
[player owner](../gameplay/player-combat.md#game-over); không coi evidence cũ là
bằng chứng đã rerun toàn suite. Không claim Android/device FPS.

## Final art cleanup — 2026-10-21

Art đóng ở checkpoint này; phase tiếp theo là âm thanh. Đã retire 24 offline
migration/probe dư, 34 assets không dùng, bảy proxy renderer ẩn và các API legacy
không còn caller. Giữ current regression/shared fixtures, editable sources, PixelLab
ledgers và recovery cá nhân; chi tiết ownership ở [asset pipeline](../production/asset-pipeline.md#final-cleanup-và-bàn-giao-âm-thanh).

Final grouped verification PASS: closure carry/strike/beams/boss/dev/art; roster
damage/pool/asteroid sizes; death-world bốn skill boss và ba quái, respawn/Game Over,
Retry/Menu; một fixture đầy đủ năm encounter có Pause/death/missile; enemy/supply/
asteroid/Bulwark bursts. Clock-zero guard đã được kiểm bằng physics tick gọi trực
tiếp ở Game Over để skill không bị hard-cancel bởi tick còn trong batch.

Harness chờ hủy input component trước probe tiếp theo; E2E đặt pointer trước nhấn
và xử lý Pause do Editor mất focus. Chỉ rerun nhóm fail, không lặp toàn historical
suite. Source 24 retained tools compile; native references 3.017 owners, 186 PNG,
48 dev routes PASS; snapshot gameplay data/collider trước/sau giống nhau. Sau test
reload saved scene, Edit Mode sạch, dev/debug OFF; Console zero errors/warnings.
Save byte guards PASS. Evidence local `.design/final-cleanup/`, manifest lưu summary.
Không claim Android/device FPS, full Campaign hoặc final audio.
