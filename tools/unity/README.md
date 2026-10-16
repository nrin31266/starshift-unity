# Unity baseline authoring

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

Manual: Play gameplay scene, đợi Scout bay vào vùng trên rồi kéo Player để aim,
giữ pointer bắn Pulse. Scout chết sẽ disable; restart Play để thử lại, chưa có
respawn/scheduler. Chọn scene Scout để chỉnh Entry/Hold Viewport và Entry Duration;
HP chỉnh ở definition asset, flash/tint ở Enemy Damage Presentation. Các giá trị
là prototype seeds ở canonical owners, không phải final tuning.
