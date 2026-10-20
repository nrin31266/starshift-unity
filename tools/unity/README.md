# Unity baseline authoring

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

## Baseline authoring

`BuildVerticalSliceBaseline.cs` là authoring script chạy trong Unity Editor qua
Pipeline `run_script`; nằm ngoài Assets để không trở thành runtime assembly.
Scene/prefab/importer và metadata được Unity tạo, không sửa YAML.

Baseline và boundaries thuộc [architecture owner](../../docs/systems/architecture.md#m2-scene-và-player-baseline-provisional).
Để xem/chỉnh baseline đã tạo, mở `app/Assets/StarShift/Scenes/02_Gameplay.unity`
trong Unity và dùng Inspector/Prefab Mode bình thường.

Các entry point dành cho lần khởi tạo trên project chưa có baseline:

- `BuildVerticalSliceBaseline.CheckpointA`: yêu cầu clean SampleScene; tạo scene copy và placeholder sprites.
- `BuildVerticalSliceBaseline.CheckpointB`: yêu cầu saved gameplay foundation; tạo và connect Player prefab.
- `BuildVerticalSliceBaseline.Inspect`: đọc hierarchy, framing và prefab/physics references của scene đang mở.

Gọi qua Unity MCP `run_script` với `file` là
`../tools/unity/BuildVerticalSliceBaseline.cs` và `entry` tương ứng.
A/B từ chối overwrite asset đã tồn tại; không chạy lại để reset chỉnh sửa của user.
`Inspect` có thể chạy lại ở Edit/Play Mode; prefab connection được kiểm tra ở Edit Mode.

## Player input và movement checkpoint

`ConfigurePlayerDrag.cs`, entry `ConfigurePlayerDrag.Configure`, gắn adapter/movement
vào Player prefab qua Unity API và bind camera cho scene instance đang mở.
Không reset transform hoặc sensitivity đã có; chạy ở Edit Mode.

`VerifyPlayerDrag.cs`, entry `VerifyPlayerDrag.Run`, chạy qua MCP `run_script`
trong Play Mode. Script tạo virtual Mouse/Touchscreen, queue Input System events
cho player loop bình thường và kiểm tra screen displacement bằng camera projection.
Bao phủ drag ngắn/dài, trái/phải, release/re-drag, re-enable khi đang giữ pointer,
touch thứ hai và hai camera projection aspect/zoom. Đây không phải thao tác OS mouse
thật hoặc test Game View resize UI; không chứng minh Android touch hardware.
Test luôn dọn virtual devices và trả player/camera về trạng thái trước test.
Khi có PlayerMovementBounds, suite input tạm disable bounds để đo displacement
độc lập rồi restore; suite VS-007 dưới đây kiểm tra clamp đang bật.

Để manual feel test, mở gameplay scene, chọn Player, Play rồi giữ chuột trái và kéo
trong Game View. Inspector của Relative Drag Movement có Sensitivity; source/camera
là references, không phải tuning knobs. Seed thuộc
[player owner](../../docs/gameplay/player-combat.md#m2-manual-feel-checkpoint-provisional).

## Player bounds checkpoint

`ConfigurePlayerBounds.cs`, entry `ConfigurePlayerBounds.Configure`, thêm component
vào prefab qua Unity API, giữ scene transforms và input references đã có.

`VerifyPlayerBounds.cs`, entry `VerifyPlayerBounds.Run`, chạy qua MCP `run_script`
trong Play Mode. Nó queue virtual Mouse events qua player loop để kiểm tra 4 cạnh,
4 góc, kéo tiếp ra ngoài không jitter và reverse drag không cần nhả pointer.
Assertion quan sát SpriteRenderer world bounds/projection của placeholder hiện tại;
runtime bounds không đọc sprite. Test đổi Game View thật sang 1600×900 và 2000×900,
giữ camera size baseline, rồi restore preset/position và dọn device trong finally.

Game View presets `StarShift Bounds 16:9` và `StarShift Bounds 20:9` được lưu trong
Editor preferences để manual test lại. Việc đổi resolution dùng internal Editor API
`GameView.SetCustomResolution`, không có dependency đó trong runtime scripts và có
thể cần cập nhật helper khi nâng Editor. Ảnh corner verification nằm trong ignored
`app/Temp/StarShiftBoundsCheckpoint/`.

Chọn Player và bật Gizmos ở Scene View để xem footprint vàng. Chỉnh Footprint Size
chỉ khi muốn đổi envelope kích thước tàu; giữ seed hiện tại khi test feel.

## Gate và Pulse Laser checkpoint

`ConfigurePulseLaser.Configure` tạo projectile sprite/prefab, definition và nối
gate/weapon/Muzzle vào Player qua Unity API. Có thể chạy lại ở Edit Mode; không reset
balance values trên definition đã có hoặc transforms Player do user chỉnh.

`VerifyPulseLaser.Run` chạy trong Play Mode qua MCP `run_script`; file tương ứng
là `../tools/unity/VerifyPulseLaser.cs`. Helper tự restore gate/player/timeScale,
dọn virtual Mouse/probes và không mutate original config. Coverage và giới hạn ở
[dev/debug owner](../../docs/systems/dev-debug.md#m2-pulse-laser-verification).

Để thử fire gate qua MCP eval, lấy PlayerGameplayGate trên Player rồi gọi
`SetState(StarShift.Player.PlayerGameplayState.Entry)` để chặn hoặc `Active` để
resume; không cần UI. Hold pointer để bắn. Gate resume khi còn held cho fire tiếp
tục; dùng press mới để drag. Play Mode mặc định Active nhưng không held thì không fire. Inspector của Pulse Laser Weapon chứa reference definition và Muzzle;
feel/balance knobs thuộc definition asset, không duplicate trên Player runtime.

Input verification helpers tạm đưa input về Game View, bỏ background reset và
suppress native Mouse/Touchscreen để virtual events không bị focus/chuột thật xen
vào. Tất cả policy/device states được restore trong finally; không đổi project input
asset/update mode. Input adapter tự sample, không để movement consume held intent.

## Dynamic gameplay view checkpoint

`ConfigureGameplayView.Configure` thêm GameplayViewController lên Main Camera qua
Unity API ở saved gameplay scene, giữ Player/prefab và placeholder edits.
`VerifyGameplayView.Run` nhận argument width `1600` hoặc `2000` qua `args` của
MCP run_script (ví dụ `[1600]`); height 900. Nó đổi Game View thật, queue virtual
Mouse events, theo dõi footprint sau movement LateUpdate và restore view/player,
gate/time/input policies/devices/Game View trong finally. Coverage/giới hạn ở
[dev/debug owner](../../docs/systems/dev-debug.md#dynamic-gameplay-view-verification).
`VerifyGameplayView.InspectorRequest` kiểm tra đường chỉnh target qua SerializedObject
trong Play Mode. `ConfigureGameplayView.Inspect` inspect scene/prefab references và
frame Scene View cho manual inspection; không save thay đổi gameplay state.

Manual: mở gameplay scene, chọn Main Camera, Play. Chỉnh `Target View` trong
Gameplay View Controller (size và Rect), xem camera/Player chuyển dần.
Các ô Target View commit khi nhấn Enter hoặc chuyển focus; có thể xóa số rồi gõ
decimal như Height `0.7`. `VerifyGameplayView.InspectorDelayedFields` kiểm tra UI Toolkit
text buffer và committed binding qua drawer thật trong Play Mode.
Context menu `Return To Baseline (Play Mode)` trả về view mặc định. Chọn Camera trong Scene View,
bật Gizmos để thấy vùng; chọn Player để xem footprint. Runtime caller/API thuộc
[architecture](../../docs/systems/architecture.md#dynamic-gameplay-view-foundation).

Baseline hiện tại và close/normal/wide/spectacle language theo
[camera owner](../../docs/content/levels-encounters.md#master-campaign-table-và-camera-profiles).
Baseline không là minimum; helper lấy default từ `GameplayViewSettings.Baseline`.
View suite kiểm tra cả close dưới baseline, wide/spectacle và lower 70% ở hai aspect;
return phải tới default/full. Baseline builder dùng cùng constant khi tạo scene mới.

## Scout checkpoint

`ConfigureScout.Configure` tạo placeholder sprite, HP definition, production prefab
và một scene instance qua Unity API. Chạy trên saved gameplay scene ở Edit Mode;
không overwrite prefab/definition đã có. `ConfigureScout.Inspect` đọc hierarchy,
HP runtime, flight, collider/body binding và prefab connection; HP runtime chỉ
khởi tạo khi Play/OnEnable.

`VerifyScout.Run` nhận args `[1600]` hoặc `[2000]` qua MCP run_script trong Play Mode.
`VerifyScout.EdgeCases` không cần args. Coverage/isolation ở
[dev/debug](../../docs/systems/dev-debug.md#m2-scout-verification).
Ảnh damaged-state nằm trong ignored `app/Temp/StarShiftScoutCheckpoint/`.

Manual hiện chạy encounter A → B theo [L1 checkpoint](#l1-encounters-và-fast-start).
Scout sandbox ở scene được giữ disabled cho isolated probes. Chọn prefab Scout để
chỉnh default Entry/Hold Viewport và Entry Duration cho sandbox;
HP chỉnh ở definition asset, flash/tint ở Enemy Damage Presentation. Các giá trị
là prototype seeds ở canonical owners, không phải final tuning.

## L1 encounters và fast start

`ConfigureL1Encounters.Configure` chạy ở saved gameplay scene trong Edit Mode qua
MCP, tạo hai encounter assets nếu chưa có, giữ existing tunables, nối runner/gates
và disable sandbox Scout. `Inspect` đọc configs, wiring, missing references/scripts.
Không raw-edit YAML hoặc reset config user đã chỉnh.
`ApplyPlaytestFeedback` áp entry đồng bộ hiện tại và damage threshold vào existing
encounter assets/Scout prefab; giữ các anchor, HP và weapon tuning đang có.
`VerifyL1Encounters.DepartureFeedback` gom kiểm tra damage state, mirrored entry và
shot còn visible qua source exit/timeline/lifetime. `QuickTuning` smoke HP/Pulse/B.

`VerifyL1Encounters.Run` nhận ví dụ `[1600,6,false]` hoặc `[2000,6.8,true]`;
`Death`, `Lifecycle`, `Boundary`, `Offscreen`, `Validate`, `Isolate` không cần args. Tất cả qua
`../tools/unity/VerifyL1Encounters.cs`. Coverage, fast-start controls và regression
isolation thuộc [dev/debug owner](../../docs/systems/dev-debug.md#m2-l1-fast-start-và-encounter-verification).
Scene mở sẵn để Play A → B; chọn `L1Encounters` để tới config/dev component.

## Scout single-fire checkpoint

`ConfigureScoutFire.Configure` thêm profile/projectile assets, fire/presentation
components và Muzzle vào production Scout qua Unity API, bind scene Player target.
Chạy trên saved gameplay scene ở Edit Mode; giữ existing asset tunables và GUID.
`ConfigureScoutFire.Inspect` kiểm tra config/wiring/missing scripts.

`VerifyScoutFire.Run(width)`, `Dodge(width)`, `Lifecycle()` và `Collision()` chạy
trong Play Mode qua MCP. Width là 1600 hoặc 2000; coverage/isolation/giới hạn ở
[dev/debug](../../docs/systems/dev-debug.md#m2-scout-fire-verification).

Manual: Play gameplay scene, đợi Scout Hold và xem Core charge rồi shot. Giữ pointer
và kéo Player để né; projectile không steer. Timing/speed/lifetime chỉnh trên
`Data/Enemies/ScoutSingleFire.asset` theo
[enemy owner](../../docs/content/enemies-bosses.md#scout-single-fire-checkpoint).
VS-015 chỉ release contact; VS-016–021 đã nối Player damage lifecycle bên dưới.

## Player damage lifecycle checkpoint

`ConfigurePlayerDamageLifecycle.Configure` author bốn combat layers, Physics2D matrix,
recursive prefab layers, Player components/references và scene shared enemy-fire gate.
Chạy trên saved gameplay scene ở Edit Mode; từ chối overwrite occupied layer slots.
`Inspect` kiểm tra matrix, bindings, tunables và missing scripts.

`VerifyPlayerDamageLifecycle.Run(width)` kiểm tra nguyên flow A–F cho width 1600/2000;
`PointerAndLifecycle()` kiểm tra held/fresh input, callback failure, pause và root reuse.
`IsolateLegacy()` chỉ dùng cho regression probes trong một Play session riêng;
không save scene sau khi isolate. Coverage/isolation và giới hạn ở
[dev/debug](../../docs/systems/dev-debug.md#m2-player-damage-lifecycle-verification).
Manual: Play scene, để Scout bắn trúng hoặc body chạm Player; inspect lives/state qua
MCP, references/tunables qua Inspector; xem entrance, blink và enemy fire resume. Game Over
đã có minimal overlay; restart Play/dev replay để reset phiên test, không coi đây là Retry Wave.


## M2 UI closure

`ConfigureGameplayUi.Configure` chạy qua Unity MCP ở saved gameplay scene trong
Edit Mode; tạo authored Canvas/HUD/overlay/EventSystem/session, dùng lại project
InputSystem actions và giữ UI đã có. `Inspect` kiểm tra scene references/scripts,
font glyphs và EventSystem. Presentation chỉnh trực tiếp RectTransform/components;
không chạy lại baseline builders để reset gameplay tuning.

`VerifyVerticalSlice.InputAndPause`, `FullRun(width,size,withDeath)`, `GameOver`,
`Pooling`, `ResetAndStandalone` chạy bằng MCP run_script trong Play Mode.
`EditorViewRestoration` kiểm tra cleanup preset/resolution/preview zoom/pan cả khi
test bị ngắt; [Editor state contract](../../docs/systems/dev-debug.md#game-view-và-trạng-thái-editor).
Ví dụ args FullRun: `[1600,6,true]`, `[2000,6,false]`, `[2000,6.8,false]`.
Coverage/evidence và giới hạn thuộc
[dev/debug](../../docs/systems/dev-debug.md#m2-vertical-slice-closure-verification).

## M3 level và encounter foundation

`ConfigureEncounterFoundation.Configure` promote saved L1 assets qua Unity API,
không đổi GUID/tuning đã có; tạo LevelDefinition, CameraProfile/FlightSequence assets,
bind LevelFlow/cleanup/prefab pool/debug và author placeholder mission approach.
`Inspect` đọc config, sequence references và scene missing scripts/references.
Dùng script này cho migration; không chạy lại M2 builders để reset tuning.

`VerifyVerticalSlice.FullMissionRun` chạy full intro + migrated L1; regression probes
và command timeouts ở [dev owner](../../docs/systems/dev-debug.md#m3-regression-entry-points).
Ví dụ ở Unity root `app/`:

```sh
unity command run_script --file ../tools/unity/VerifyVerticalSlice.cs --entry VerifyVerticalSlice.FullMissionRun --args '[1600,6,true]' --timeout 90 --timeout_ms 85000 --format json
unity command run_script --file ../tools/unity/VerifyEncounterFoundation.cs --entry VerifyEncounterFoundation.Boundaries --format json
```

Inspector: `Data/Fixtures/Levels/M4SystemTestbed` giữ direction/intro/encounter order/default camera;
`Data/Fixtures/Encounters` giữ groups/schedule/completion/fire/grace; `Data/Flights` giữ phases;
`Data/Cameras` giữ size/region/transition. Runtime authority không nằm ở assets.
Dev overrides/debug toggles nằm trên `L1Encounters`, default OFF.


## Mission establishing và cinematic revision

`ConfigureMissionPresentation.Configure` chạy ở Edit Mode sau M3 foundation để author
CombatHud CanvasGroup, location/space announcement + description, subtle ContinueHint,
transparent establishing tap area, visual-only approach ship và backdrop. Migration
bỏ nút menu cũ; rerun giữ timing/copy đã tune. Không tạo Canvas, EventSystem, menu
hoặc gameplay scene thứ hai. LocationName chưa quyết vẫn trống trong Level config.

`ConfigureMissionPresentation.TuneMinorHandoff` author L1 A→B timing và incoming copy
qua Unity API, giữ nguyên intro tuning. Outgoing Encounter có `ProjectileGraceSeconds`
và `ProjectileFadeSeconds`; cleanup policy thuộc
[level owner](../../docs/content/levels-encounters.md#minor-handoff-projectile-cleanup).
`VerifyVerticalSlice.MinorProjectileCleanup` là targeted Play Mode regression: shot
phát sát final kill còn visible/bay trong grace, tắt trigger/sweep damage ở fade,
pool reset/reuse, viewport-exit recycling, Pause freeze, HUD/drag và longer centered
handoff trước Encounter tiếp. Không cần chạy full historical matrix cho tuning này.

Play: đọc establishing text → prompt xuất hiện → valid tap bất kỳ → approach / space
travel → brake đồng thời framing/HUD → gameplay card → combat. Intro không Pause; minor A→B
giữ HUD và gameplay Pause, optional copy tự fade và không confirmation. Dev Skip Intro
hoặc encounter start bỏ qua establishing tap. Test qua FullMissionRun, EstablishingInput
và VerifyEncounterFoundation.PresentationBoundaries; evidence thuộc
[dev owner](../../docs/systems/dev-debug.md#mission-presentation-revision).

`VerifyVerticalSlice.CaptureIntroPreview` sample PNG sequence khoảng 10 fps vào ignored
`app/Temp/StarShiftIntroReview/ux-preview-frames/` để review từ establishing tới gameplay;
helper trả thời lượng thực để encode, dùng real virtual Mouse, không đo performance.

Major establishing giữ layout giới thiệu; timed Mission/Encounter titles căn giữa
trong cùng GameplayAnnouncement. Tên tạm dùng taxonomy để debug; tên planet chưa
chốt giữ TBD. Bật Content Labels trên EncounterDebugView để xem từng Spawn Group,
Formation và Flight Sequence / Path theo IDs thực; default OFF.

Mission card gộp Mission title + starting Encounter ở dòng dưới, không mô tả Mission
hoặc announcement đầu riêng; chỉ hiện khi HUD/control đã trở lại. Timing và Pause
theo [presentation owner](../../docs/presentation/audio-vfx.md#level-intro-warning-và-dynamic-audio).
FullMissionRun kiểm tra fresh drag/fire và Pause trong card trước first spawn.
Dev jump lấy đúng Encounter index đã chọn.

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

## M5 balance and transition follow-up

`ConfigureBalancePass.Configure` là tuning pass mới nhất sau HumanFeel; không save
scene dirty. `PrepareVerification` ghép source vào `.design/m5-balance-pass/Verification.cs`,
đổi output của reusable HumanFeel probes sang subfolder `human-regression` để giữ
historical evidence. Chạy VerifyBalancePass Begin methods tuần tự; BeginFullRegion
kiểm prerequisite JSON PASS và existing-file guard. Không chạy expensive historical
matrix sau mỗi fix. Native captures, coverage và đúng chỗ tắt intro ở
[dev owner](../../docs/systems/dev-debug.md#m5-balance-and-transition-follow-up).

### Planet 1 flow polish

Production levels nằm ở `Data/RegionA/Missions/Mission01–04`; system testbed nằm trong
`Data/Fixtures/Levels/M4SystemTestbed` và `Data/Fixtures/Encounters`. Move dùng
AssetDatabase và giữ GUID. Các seed flight lịch sử vẫn được builders/verifiers dùng,
không được coi là orphan chỉ vì không nằm trong production scene dependencies.

Chạy `ConfigureFlowPolish.Configure` trong Edit Mode để áp tuning/UI mới (có scene
snapshot vào `.design/m5-flow-polish/before-scene.unity`), `MoveFixtures` để migrate
folder cũ, rồi `PrepareVerification` để ghép các verifier dùng chung Rig. Trong Play
Mode chạy generated `.design/m5-flow-polish/Verification.cs` entry
`VerifyFlowPolish.Begin`; full flow entry `VerifyFlowPolish.BeginFullRegion`.
Evidence mới nằm trong `.design/m5-flow-polish/`, không ghi đè các pass cũ.

`VerifyBalancePass.BeginPersistence` trong cùng generated file kiểm tra save/Retry;
`VerifyFlowPolish.BeginDevRoute` kiểm tra đường BOSS_PHASE_B. Edit Mode closure:
`VerifyFlowPolishClosure.Inspect` kiểm tra serialized references, scene/prefab scripts,
level config và Dev mode off. Không chạy lại historical full suite cho thay đổi này.

### Heat and content cleanup

`OrganizeRegionA.Run` migrate authored data qua AssetDatabase, giữ GUID và chỉ xóa
sáu obsolete generated flights sau dependency audit. Folder contract ở
[asset pipeline](../../docs/production/asset-pipeline.md#region-a-authored-data-layout).
`RegionAAssetPaths` là Editor-only resolver chung cho builders/verifiers.

`OrganizeRegionA.PrepareHeatVerification` tạo `.design/m5-shared-heat/Verification.cs`.
Trong Play Mode: `VerifyHeatLifecycle.Begin` kiểm tra press/release, blocked cooling,
shared heat/delay/latch qua nhặt Core, upgrade và cycle, Pause, overheat recovery,
death/session reset và live Mission handoff; evidence ở `.design/m5-shared-heat/`. Sau targeted PASS,
`VerifyHeatLifecycle.BeginFullRegion` chạy một integration flow trên bản cuối.


### Pickup bounce consistency

Trong Play Mode, chạy `VerifyPickupMotion.Run` qua `run_script` trên file
`tools/unity/VerifyPickupMotion.cs` (Unity-root relative: `../tools/unity/VerifyPickupMotion.cs`).
Kiểm tra cùng quỹ đạo normal/grace, đúng hai lần nảy rồi viewport exit, late grace
không kéo xu lên, pool reset, 28 boss gold trajectories, Core/support retention,
Pause/Resume và collection không trùng. Motion regressions step trực tiếp physics
callback; Pause/Resume chạy qua frame thật. Evidence `.design/m5-pickup-motion/result.json`.


### Respawn continuity

Chạy `ConfigureFlowPolish.PrepareVerification` ở Edit Mode; trong Play Mode chạy
`VerifyFlowPolish.RespawnContinuity` từ `.design/m5-flow-polish/Verification.cs`.
Kiểm tra death giữ projectiles, mouse/touch đang held nối lại drag/fire tại safe
entrance pose, không replay delta bị khóa, protection và terminal cleanup.
Evidence `.design/m5-respawn-continuity/result.json`. Nearby regression
`VerifyPlayerDrag.Run` cần run đã skip intro và không còn terminal overlay.

M6 closure cleanup: native script moves giữ GUID; `Attacks` dùng chung beam presentation,
`Bosses` giữ phase/module/shield/barrage, `Presentation` giữ visual cues. Bỏ 11 seed
formation/flight assets không còn inbound reference; full authoring dùng tên final
formation để không tạo lại các seed đó. `Inspect` kiểm cả missing line materials.
Probe giữ assembly reload lock trong try/finally để async evidence không bị bỏ dở
bởi queued reload; không sửa runtime sources trong khi probe đang chạy.


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
