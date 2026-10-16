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
