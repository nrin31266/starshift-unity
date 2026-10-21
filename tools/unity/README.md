# Unity authoring và verification

## Current art closure và dev tools

`DevFastStart` dùng dropdown Start Point theo Start Level; không cần nhớ ID. Bật
Dev Mode, chọn mission/route và Play. Trong Play Mode có nút chạy lại, tạm dừng/
tiếp tục và về Menu. Camera, hướng và reward seed nằm trong foldout riêng.
Route TRANSITION tự chạy handoff thật; Mission04 dùng để thử đổi hành tinh.
Saved Dev Mode mặc định OFF. Các debug flags của EncounterDebugView cũng OFF.

`VerifyPolishVfx.Begin("vfx",1600/2000)` và `Begin("deaths")` chạy nhóm VFX/death; chờ
`.design/presentation-polish/current.json` terminal trước probe khác.

`VerifyDevTools.Edit` kiểm dropdown theo tám mission trong Edit Mode.
`VerifyArtClosure.Begin(scope)` chạy Play Mode từ idle Menu bằng storage/input
riêng; scopes `carry`, `strike`, `beams`, `boss`, `dev`, `art`, hoặc `all`.
Chờ `.design/art-closure/verification.json` terminal trước probe tiếp theo.
`e2e` gọi `VerifyM4Testbed.FullMission` đã nạp trong cùng domain; nạp trước bằng
entry `DropSelectionEdges`. Không chạy hai probe cùng lúc, không save runtime state.

`ConfigureArtClosure.Gameplay` và `.Art` là migration cuối qua Unity API; chỉ chạy
ở Edit Mode khi cần bind lại các thay đổi đã duyệt. Source/export recipe ở
[closure art](../assets/closure/README.md).

Đã retire 20 bootstrap/fixture M2 cũ: scene baseline, drag/bounds/camera, Pulse,
Scout/fire, player lifecycle/UI và encounter foundation/L1. Chúng đã bị current
M4/Region B/M8/closure coverage thay thế và không còn callers trong tools. Git giữ
lịch sử; các báo cáo checkpoint trong docs là evidence lịch sử, không phải lệnh chạy
các file đã retire. Giữ shared rigs còn được các probe hiện tại dùng.

## M6 Region B authoring và verification

`ConfigureRegionB.Configure` author Mission05–08, enemy/boss prefabs, shared formation
plans/flights/cameras và catalog L1–L8 qua Unity API. Chạy saved Gameplay scene/Edit Mode.
`OrganizeAssetNames` bỏ temporary Feedback/M6 prefixes, đặt camera theo combat view,
formation theo encounter và shared flight theo hướng entry; giữ GUID, không retune data.
Sau dependency scan, migration cũng bỏ Region B camera assets không còn inbound references.
`RegionAFormations` chỉnh sáu formation lớn và NormalDrops odds; không chạy lại để
reset tuning user đã chỉnh. `RewardFloors` author quota theo key combat beats, độc lập Supply/Boss pair.
`RefreshPresentation` là migration bổ sung shield-break
cues và Raider placeholder scale, giữ prefab GUID. `RefreshHoming` ghi launch arc tuning
vào existing Homing asset, không thay damage/cadence/heat. `CombatFeedback` áp human-feel
pass (front guards, common attack windows, HP, boss escorts/rewards, bounds hint, compact
Homing); full `Configure` cũng gọi pass này. Không chạy migration giữa một runtime probe.

Runtime probes reuse `VerifyPacingRefinement.Rig`, không thêm runtime test MonoBehaviour.
Chuẩn bị composite source ngoài Assets để Pipeline compile cùng existing rig:

```sh
python tools/unity/prepare_region_b_verification.py
```

Sau đó dùng MCP `run_script`, file `../.design/m6/Verification.cs`:

- `VerifyRegionB.Inspect`: catalog/config/missing refs/saved Dev flag, Edit hoặc Play.
- `BeginMechanics`: Sniper, staggered dual Sniper, Interceptor, Carrier cap/death và corridor/ExitDrain.
- `BeginBoss`: entry/shield protection, ba attack families, module collapse, exposed core, death/reward/victory.
- `BeginPlayer`: Right virtual mouse drag, ba branches, shared heat, Pause, death/respawn/held continuity.
- `BeginPersistence`: Continue/Retry/Replay, legacy L4 markers → L5, completed-save Continue.
- `BeginHandoff`: Vanguard death/reward → real journey → Mission05, pose/gesture/camera/save.
- `BeginSupply`: Right held weapon → direct Core/support collection, vertical separation/left drift và no legacy BOX.
- `BeginTravelRetry`: incoming committed checkpoint khi Retry giữa mission travel.
- `BeginShardBalance`: HUD wallet qua mission/region, Retry deduplication, Continue,
  New Game và ví Replay/DevTest độc lập; kiểm production save bytes không đổi.
- `BeginTapInput`: click qua uGUI vào approach và cleanup synthetic/native input.
- `BeginPresentation`: final proxy silhouettes và beam sweep direction ở hai aspects.
- `BeginBossBalance`: ba weapon branches Lv3/Lv5, real damage và measured shield/core pacing.
- `BeginObjectives`: mới chuyển Eliminate phải giữ required targets sau deadline cũ; kill rồi cleanup thành công.
- `BeginClosureLayouts`: sáu changed layouts ở từng width; overlap, hull clipping và HUD clearance.
- `BeginThreatProtection`: 7 threat encounters ×3 weapon branches Lv3; first attack trước death, còn frontline guard.
- `BeginBossBarrage`: Vanguard/Warden ×16:9/20:9; safe-gap movement, actual danger damage, Pause/cancel, node-HP HUD.
- `BeginPressure`: đo actual busy attack sources, rest gaps, projectile peaks và boss/Carrier escorts.
- `BeginCoverAndIdle`: coherent idle, tank chặn Pulse trước Sniper, region HP và bounds.
- `BeginVanguardTuning`: actual held Pulse L3 qua hai phase, escorts cap, staged death/gold và storage isolation.
- `BeginGoldRain`: real Warden staged death → 36 gold → drift/grace → persisted victory.
- `BeginHomingBalance`: chỉ Homing Lv3/Lv5 sau launch/target-distribution changes.
- `BeginBoundaryHint`: Up/Right half-region, no permanent mask, delayed two-pixel contact cue, inward/release reset.
- `BeginHomingEmpty`: Lv1–5 Up/Right, no-target symmetry, odd center axis và near-uniform gaps.
- `BeginHomingPaths`: Up/Right, L1/L5 với 1/2/3 valid targets; real held volleys, đường cong,
  balanced assignment + actual damage, Pause, dead/reused target và pool branch reset.
- `BeginPickupDirections`: reuse `VerifyPickupMotion` cho Up/Right ×1600/2000; bounce,
  grace, exit, pool reset, Pause/resume và collection once.
- `BeginDevRoutes`: production DevFastStart routes và bytes isolation.
- `BeginFormations(width)`: actual Game View 1600 hoặc 2000 ×900; report collider overlap và screenshots.
- `BeginFullRegion`: một continuous protected-bot L5→L8 bằng held weapons, temp campaign storage, victory/reload.

Các entry Begin trả ngay. Chờ `.design/m6/<probe>.json` terminal trước probe khác;
`CurrentTask` trên assembly `VerifyRegionB` là live Task handle để phân biệt đang chạy
với process đã dừng. Không chạy hai runtime probes đồng thời. Evidence/screenshots
ở `.design/m6/`, không commit. Saved production Dev mode phải OFF; stop Play rồi
inspect Console/refs trước delivery. Coverage/limits theo [dev owner](../../docs/systems/dev-debug.md).

## M4-A combat/reward testbed

`ConfigureM4Testbed.Configure` là one-time authoring migration trên saved Gameplay
scene/Edit Mode. Dùng Unity API để giữ GUID, author type configs/pickups/drop tables,
Player components, compact HUD và 5 testbed Encounters; giữ intro-delay override đã có.
Không chạy lại để overwrite tuning/content user đã chỉnh sau migration. `Inspect`
read-only kiểm config, references/scripts và local overrides.

`VerifyM4Testbed` có entries Weapons, PickupsAndRewards, CombatAndDrops,
DropSelectionEdges, Stress, RegressionAndPools, PowerAndLifecycle, MissilesAndProtection, PickupFade và Visuals(width).
Chạy nhóm liên quan, không full historical M2/M3 matrix mỗi lần sửa. Old suites có
fixtures/counts theo checkpoint 2-Encounter cũ; dùng M4 entries cho current testbed.
Không chạy hai runtime probes đồng thời. Sau probe missile, fixture restore flight
flags trước pool reuse. Không save scene/runtime test state.

Fast closure: `BeginClosure()` chạy nối tiếp DevFastStart 5 Encounter, weapon/heat,
pickup/effect/reward/reset, reserve missile, drop edges, handoff grace/fade, pickup fade, density
và đúng một full Mission 01 có death/Pause. Progress/result nằm ở
`.design/m4-a/closure-results.json`; chờ `current: PASS` hoặc `FAILED` trước probe
khác. Nếu fail, sửa và rerun riêng entry đó; không tự lặp toàn suite. Không chạy lại
pool benchmark, historical matrix hoặc Android trong entry này.

Full mission: gọi `BeginFullMission(1600,false)` hoặc `(2000,true)` qua `run_script`;
entry trả ngay và ghi result vào `.design/m4-a/full-result-{width}.json`. Chờ file
hết `status: running` trước probe khác. `BeginFinalRuns()` chạy đúng hai lượt nối
nhau. Cách này tránh MCP transport timeout 60s; exception vẫn được ghi với pass=false.
Visuals capture ScreenCapture có HUD vào `.design/m4-a/screenshots/`, restore Game
View/zoom/pan trong finally. Coverage và giới hạn ở
[dev owner](../../docs/systems/dev-debug.md#m4-a-combatreward-testbed-verification).

## M3.5 thermal HUD checkpoint

`ConfigureThermalHud.Configure` chạy ở saved Gameplay scene/Edit Mode qua
`run_script` (`../tools/unity/ConfigureThermalHud.cs`). Helper tạo type-owned
thermal profile nếu chưa có, bind definition và bố cục compact lên CombatHud
hiện có bằng Unity API; giữ thermal seed đã được author trên asset tồn tại.

`VerifyVerticalSlice.ThermalHud` chạy Play Mode với args `[1600,true]` cho targeted
thermal/lifecycle checks; `[2000,false]` cho visual aspect thứ hai. Height 900.
Boolean false bỏ release/burst/death checks đã chạy ở lượt targeted, không biểu
diễn test fail. Fixture restore devices/input policies/view/configs qua Rig.Dispose.
Ảnh/evidence nằm ở `.design/m3-5-thermal-hud/`; helper chụp sáu state ở Game View thật.
Một refinement visual có thể dùng `[1600,false]` và `[2000,false]` mà không mở lại
historical suites. Runtime semantics thuộc [player](../../docs/gameplay/player-combat.md#thermal-lifecycle),
tuning thuộc [weapons](../../docs/content/weapons-items-drops.md#primary-weapon-heat--m35).
Sau targeted pass, chạy đúng một full L1 smoke phù hợp; không tự chạy Android hoặc
full aspect/camera/input matrix cho phase này.

Correction vuông/nền mờ/ít chữ: `VerifyVerticalSlice.PlainHud` với args `[1600]`
hoặc `[2000]` chỉ capture HUD/Pause và smoke binding/click exclusion. Dùng entry
này cho chỉnh padding/copy/placeholder; không cần lặp thermal lifecycle/full L1.

## Frame rate verification

Ở Play Mode, chạy `VerifyFrameRateLimit.Measure` bằng MCP `run_script`, file
`../tools/unity/VerifyFrameRateLimit.cs`, args `[60]`. Helper đo 3 giây, kiểm tra
target/VSync và FPS không vượt giới hạn quá 15%; CPU trả về theo phần trăm một lõi
(có thể trên 100%), không phải tổng phần trăm CPU của máy. Kết quả Editor không
thay thế profile Android. Cách chỉnh nằm ở
[architecture owner](../../docs/systems/architecture.md#gameplay-frame-rate-tunable).

## M4-B persistence

`ConfigurePersistence.Configure` author lifecycle component + references một lần ở
saved Gameplay scene/Edit Mode, qua Unity API. `Inspect` đọc scene state/reference
integrity. Không chạy lại M4-A configuration hoặc reset local overrides.

`VerifyPersistence.Core` chạy temp real JSON tests. Play Mode entries `Retry(2)`,
`Retry(4)`, `Retry(5)`, `ActiveIsolation()` chạy targeted flows. `BeginLifecycle()`
launch async full lifecycle; chờ ignored `.design/m4-b/lifecycle-progress.json` về
PASS/FAILED trước runtime probe khác. Helper thật sự recreate Gameplay scene;
không dùng production storage. Settings/NewGame/Continue/Replay được expose bằng
CampaignLifecycle API, không cần Main Menu. Evidence/cost/limits thuộc
[dev owner](../../docs/systems/dev-debug.md#m4-b-persistence-verification).


## Gameplay feel follow-up

`ConfigureGameplayFeel.Configure` trong `ConfigureGameplayFeel.cs` là one-time authoring
follow-up, không chạy lại để overwrite later tuning. `VerifyGameplayFeel.BeginChecks`
trả ngay; chờ `.design/gameplay-feel/progress.json` terminal. Có entries targeted
Thermal, MissileAndImpact, ShieldRespawn, Pickups, EmptyEncounter, PickupCarryOver,
MissileCancellation và TerminalCueCleanup. PickupCarryOver dùng cloned level/temp Campaign storage,
kiểm đồ qua handoff, collect/Profile, Pause/lifetime và Retry/reset cleanup. Load
một entry VerifyM4Testbed (Weapons hoặc DropSelectionEdges) trước HomingMechanics/Capture(width) để reuse existing Rig.
Coverage và native screenshots ở [dev owner](../../docs/systems/dev-debug.md#gameplay-feel-verification).

`ConfigureGameplayFeel.RefineWeaponsAndMissile` là targeted authoring cho nhịp
launch/detonation, distinct projectile sprites/trails và khoảng nghỉ minor hiện tại;
không chạy lại full Configure. `Capture(width)` có thêm reserve-flight trước impact.
FullMission kiểm thêm real missile-button click và đúng một detonation. Probe
MissileCancellation kiểm Retry/exit/StopRun giữa flight; không chạy probes đồng thời.

`ConfigureGameplayFeel.TuneFallExitAndCooling` là targeted Edit Mode tuning cho
Shard, timed Exit, thermal cooling và E4 timer allowance. Verify bằng Thermal,
Pickups, EmptyEncounter và nearby PickupCarryOver; không cần full closure suite.


## M5 Region A

`ConfigureRegionA.BatchAB`, `.BatchC`, `.CuesAndBurst` author assets/wiring qua Unity
API trong saved Gameplay scene/Edit Mode. Không rerun để reset tuning đã author.
`VerifyRegionA.BeginBatch(2/3)`, `.BeginBossChecks`, `.BeginIntegrationChecks`,
`.BeginContentChecks`, `.BeginFullRegion` chạy Play Mode; chờ một terminal progress
trước entry khác. `InspectRegionA.Inspect` kiểm catalog/references.
Evidence/cost/limits nằm ở [dev owner](../../docs/systems/dev-debug.md#m5-region-a-verification).


## M5 second human-feel pass

`ConfigureHumanFeel` author assets trong Edit Mode, qua Unity MCP/run_script; không
rerun builders lịch sử để reset current tuning. `ConsolidateFlights` chỉ gộp duplicate
flight data sau khi assert tương đương và kiểm dependency trước DeleteAsset.
`PrepareVerification` tạo ignored combined source để reuse `VerifyPacingRefinement.Rig`:
chạy trước `VerifyHumanFeel` entries bằng `.design/m5-human-feel/Verification.cs`
(path từ Unity root là `../.design/m5-human-feel/Verification.cs`). Begin methods trả
ngay, chờ JSON terminal trước runtime probe khác. ClosureRemainder chỉ resume sau
các probe đã pass; không chạy song song. FullRegion có existing-file guard chống
vô tình rerun integration đã thực hiện. Evidence, limits và manual start points ở
[dev owner](../../docs/systems/dev-debug.md#m5-second-human-feel-pass-verification).

## M7 shell, Results và Hangar

Edit Mode: `ConfigureGameShell.Configure` là authoring/migration một lần trong saved
Gameplay scene; scene/prefab/config thay đổi qua Unity MCP. `Inspect` kiểm entry,
references, Dev OFF, tám missions và sáu cosmetic options. Shell UI được author trong
`GameShellUI.prefab`; không chạy lại builders lịch sử để reset current tuning.

Play Mode: dùng `VerifyGameShell.cs` trực tiếp qua Pipeline run_script. Mỗi Begin entry
trả ngay; chờ ignored `.design/m7/<name>.json` về PASS/FAILED trước runtime probe kế.
Entries: BeginCore, BeginStats, BeginInput, BeginInputCleanup, BeginLoading,
BeginProductionDamage, BeginFailures, BeginRecovery, BeginTeardown,
BeginHangar(1600/2000), BeginLayouts(1600/2000), BeginTerminalLayouts(2000),
BeginSceneRelaunch và BeginFlow. Core thuần storage, runtime probes dùng unique temp
save và kiểm bytes production primary/backup/invalid không đổi. BeginFlow bấm UI thật,
chỉ accelerate combat qua existing damage/clock hooks; không gán shell result state.

Historical `VerifyPacingRefinement.Rig` tạm opt out explicit mission results rồi restore
flag khi Dispose, để các transition probes M5/M6 tiếp tục kiểm choreography cũ. M7
Campaign tự handoff sau clear/save, Replay dừng Results; navigation proof dùng UiRig
riêng không raw-start gameplay.
Coverage/evidence và giới hạn thuộc [dev owner](../../docs/systems/dev-debug.md#m7-shell-results-và-hangar-verification-provisional).

## M8 native art slice

`ConfigureM8Art.Configure` chạy Edit Mode trên saved Gameplay scene: native importer,
Sprite Editor 8-frame slicing và stable IDs, prefab/scene/UI/font references. Migration
đã áp dụng; không rerun để reset manual art tuning. `Contracts` read-only snapshot
colliders + gameplay/balance, authoring asserts equality trước/sau.

`VerifyM8Art.Begin` args `[1600]` hoặc `[2000]` chạy Play Mode từ idle Menu; không
chạy concurrent hoặc khi owner đang chơi; fresh Play Mode mỗi width. Test storage/input được cô lập trước frame
wait và restore trong finally; production save byte equality kiểm sau cleanup.
Kết quả async ở `.design/m8/runtime-WIDTH.json`, actual screenshots trong screenshots/.
Pipeline/spec/known placeholder limits ở
[M8 recipe](../../docs/production/asset-pipeline.md#m8-selected-sources-và-production-recipe).


## Campaign score carry

`VerifyCampaignScore.Core` chạy Edit Mode: same-region bank, region marker/destination,
checkpoint total, record riêng, write-failure rollback/retry, completed Continue,
NewGame, v1/v2 missing field và validation/long JSON. `VerifyPersistence.Core` là nearby
regression cho store/ledger/Profile đã có.

Fresh Play Mode, đợi khởi tạo xong; nếu DevFastStart đang bật, ExitIsolated + Shell.Boot
chỉ trong runtime để về idle Menu → `VerifyCampaignScore.Begin`: temporary storage và disabled
native input, real L1 last encounter clear → L2 travel/start, actual HUD, Retry,
Continue/Replay isolation và real L4 marker → L5 region travel. Kết quả async tại
`.design/score-carry/runtime.json`; cleanup restore input, damage, production storage
và Menu, assert primary/backup/invalid bytes không đổi. Không thao tác Unity giữa test.

Score probe cũng kiểm fixed world scale + navy fill ở 16:9/20:9 × zoom 8/12/20,
real intro/mission travel/A→B. `Core` kiểm carry đầy đủ L1–L8.
`VerifyM8Background.Run` từ fresh Play Mode Menu kiểm moving visible intro parallax,
fixed world extents và rendered navy corners khi zoom-out; evidence `.design/m8/background/`.
Không chạy lại full historical suite.

`VerifyM8Animation.Import` chạy Edit Mode sau Aseprite cleanup và spritesheet pack/check;
import/slice bốn 32×40 frames ở 56.25 PPU, center pivot, tạo native smoke clip.
Fresh Play Mode Menu → `VerifyM8Animation.Begin` kiểm playback trên pooled Shard thật,
Pause và production save byte equality; evidence `.design/m8/animation-smoke/`.
Smoke assets không gắn vào production pickup; chỉ một PixelLab job, provenance và
cleanup recipe ở `tools/assets/m8/animation-smoke/`.

## Current presentation polish

Run final authoring in Edit Mode: `ConfigurePolishVfx.Bind`, `.BulwarkFire`, then
`ConfigurePresentationPolish.Apply`. Import reviewed exports first; source/frame
routing is in [polish sources](../assets/polish/README.md). Do not rerun old
whole-project migrations over current tuning.

Native checks: `VerifyM8ProductionLayout.Edit`; `VerifyPresentationPolish.Thermal`,
`.Settings`, `.Combat`; `VerifyTravelPresentation.Run`; `VerifyPolishVfx.Run(1600)`
and `(2000)`, `.Deaths` (includes Bulwark). Runtime entries require idle Menu and
isolate storage. Shell UI operations are asynchronous: wait for `IsBusy == false`
before restoring production storage, including exception cleanup.

One grouped full mission uses `VerifyM4Testbed.FullMission(1600,true)` with separate
storage set by the caller. It is a five-encounter fixture with actual input,
weapon/pickup/reward, Pause, life loss, reserve detonation and terminal cleanup;
not a complete campaign. Use CLI `--timeout 180` for the full run, or its existing
Begin entry and read the result file. Current evidence is local in
`.design/presentation-polish/` and the inventory qualifies older baseline evidence.


## Pickup bounce consistency

`VerifyPickupMotion` giữ regression bounce/grace/exit, pool reset, Pause/Resume và
collection once cho Up/Right; `VerifyRegionB.BeginPickupDirections` dùng lại fixture
này ở 1600/2000. Không dùng fixture drag M2 đã retire. Motion contract thuộc
[weapon/items owner](../../docs/content/weapons-items-drops.md).
