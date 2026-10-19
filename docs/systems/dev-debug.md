# Dev và debug

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
HomingMechanics, MissilesAndProtection, TerminalCueCleanup, PickupCarryOver và
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
Retry(5), DropSelectionEdges và TerminalCueCleanup. Fixture carry-over tự set/restore
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
DevFastStart TRANSITION chạy context handoff thật của Mission04, không alias vào wave
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
