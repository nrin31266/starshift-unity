# Unity baseline authoring

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

Inspector: `Data/Levels/L1` giữ direction/intro/encounter order/default camera;
`Data/Encounters` giữ groups/schedule/completion/fire/grace; `Data/Flights` giữ phases;
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
