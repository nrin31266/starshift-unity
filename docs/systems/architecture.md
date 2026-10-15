# Unity architecture

## Phạm vi và ownership

Tài liệu này sở hữu runtime/config boundaries, scene model, pooling và time management.
Gameplay behavior do domain owners quyết; tên class và serialized shape chưa bị khóa.
Mục tiêu là các component nhỏ, có trách nhiệm rõ và có thể test từng domain,
tránh một MonoBehaviour điều khiển toàn bộ game.

## Repository và asset boundaries

Unity root duy nhất là `app/`; repository root `starshift/` chứa thêm docs và offline
tooling. Không tạo `Assets/`, `Packages/` hoặc `ProjectSettings/` tại repository root,
không giả Unity-generated metadata và không bịa version khi project chưa tồn tại.
Đọc version/package thật trước khi sửa; không tự upgrade hoặc pin dependency theo reference docs.

Project-owned assets nằm trong `app/Assets/StarShift/`: Art, Audio, Data, Prefabs,
Scenes, Scripts, UI và VFX. Không move asset user chỉ để ép namespace; nếu reorganize
phải giữ `.meta`/GUID và cập nhật references an toàn.

Scripts có thể tổ chức theo baseline sau, chỉ tạo folder khi có file thật;
không tạo hàng chục assembly/package hoặc placeholder C# để lấp scaffold.

```text
Scripts/
├── Core/
├── Player/
├── Combat/
├── Enemies/
├── Encounters/
├── Rewards/
├── Systems/
├── UI/
└── Dev/
```

Python synth, spritesheet helper, slicing/checking, batch rename và asset validation
là offline utilities ở `tools/`. Chỉ khi tool thực sự cần chạy trong Editor mới đưa
vào `app/Assets/StarShift/Editor/` hoặc vị trí Editor-only tương đương.

## Audit baseline của scaffold

Ngày 2026-10-15: ProjectVersion ghi `6000.5.9f1 (b57deb96f08d)`;
manifest yêu cầu URP `17.6.0`, Input System `1.20.0`, AI Inference `2.6.1`,
Pipeline `0.8.0-exp.1` (experimental), Test Framework `1.7.0`, cùng 2D toolset.
Đây là observation, không phải yêu cầu nâng/hạ/pin version.
URP trong `packages-lock.json`, package cache và danh sách package Editor đăng ký lại
là `17.5.0`: conflict hiện hữu cần xử lý ở task package/setup riêng, chưa resolve.
AI Assistant không có trong manifest/lock hoặc registered package list hiện tại;
buffer Pipeline còn giữ hai warning AI Toolkit Account API timeout từ trước,
không dùng log cũ để suy ra Assistant đang được cài. Không suy đoán dependency Unity MCP.
Scene build list hiện chỉ có `Assets/Scenes/SampleScene.unity`;
scene template tại `Assets/Settings/Scenes/URP2DSceneTemplate.unity` không phải level.
SampleScene đang có Main Camera và Global Light 2D; không có gameplay implementation.
Product/company đang là template `app`/`DefaultCompany`; không sửa Player Settings
trong task scaffold. Editor API báo orientation `AutoRotation`, active build target
`StandaloneLinux64`: chưa đáp ứng target landscape Android của thiết kế.
Giữ nguyên trạng thái này trong scaffold; task setup sau cần cấu hình và verify.
Target Android API/device/performance baseline còn [TBD].

## Scene structure `[PROVISIONAL]`

Baseline screen-level scenes vừa thể hiện Scene Management, vừa tránh duplicated
setup giữa các level:

```text
00_Boot
01_MainMenu
02_Gameplay
03_Results
```

12 level chạy data-driven trong `02_Gameplay`; direction, camera, background và
encounters đến từ LevelDefinition. Không cần 12 Unity scenes. Scene naming có thể
đổi khi implementation bắt đầu nhưng phải nhất quán và vẫn giữ trạng thái provisional.

Boot chỉ cần khởi tạo save/profile, persistent services cần thiết và route first screen;
có thể tối giản nếu project chưa cần nhiều initialization. Persistent service chỉ
sống qua scene khi lifetime đòi hỏi, không biến mọi domain thành singleton.
Scene transition SHOULD hỗ trợ async loading khi phù hợp, không dựng loading framework
phức tạp. [SceneManager.LoadSceneAsync](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)
là reference để kiểm tra khi implementation, không phải yêu cầu tạo scene ngay.

## Config data và mutable runtime state

Static definitions SHOULD dùng ScriptableObject/config asset khi phù hợp:
LevelDefinition, EncounterDefinition, EnemyDefinition, WeaponDefinition, DropTable,
CameraProfile và TransitionProfile. Config không phải runtime save; không ghi lives
hiện tại hoặc enemy current HP trở lại definition asset. Tham khảo
[ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScriptableObject.html)
khi triển khai, không coi version trong URL là version project bắt buộc.

### LevelDefinition

Conceptual fields giữ đủ data để một Gameplay scene chạy các level khác nhau:

```text
id
displayName
regionId
playDirection
cameraProfile
backgroundProfile
encounters[]
transitionOut
replayPreset
```

### EncounterDefinition và SpawnAction

EncounterDefinition mô tả schedule/completion/checkpoint; SpawnAction là một entry
trong schedule. Không hard-code L7 wave timing trong một MonoBehaviour bất kỳ.

```text
EncounterDefinition
  id
  completionMode
  duration optional
  spawnActions[]
  optionalEvents[]
  checkpointPolicy

SpawnAction
  timeOffset
  enemyDefinition
  count
  formation
  flightSequence
  spawnAnchor
  modifiers optional
```

### FlightSequence data

Data phải mô tả các phase rõ, không cần visual node editor. Phase content, path library,
formation, firing và exit policy thuộc
[levels/encounters](../content/levels-encounters.md#flight-path-và-flightsequence);
architecture chỉ quyết ranh giới config/runtime.

### EnemyDefinition và runtime entity

| Static EnemyDefinition | Mutable enemy instance |
|---|---|
| HP; collision; movement compatibility; weapon profile; score; drop class; damage thresholds; prefab reference | Current HP; current phase; timers; runtime target; pooled state |

Exact HP và behavior nằm ở [enemies/bosses](../content/enemies-bosses.md).
Runtime state được reset khi reuse, không sửa asset chung cho mọi instance.

### WeaponDefinition

Static config fields thuộc [weapons/items/drops](../content/weapons-items-drops.md).
Selection, unlocks, branch levels và runtime fire timers không mutate definition.
DropTable, CameraProfile và TransitionProfile cũng là ứng viên config asset;
chưa cần sinh asset hoặc khóa schema để tài liệu trông hoàn chỉnh.

## Component boundaries `[LOCKED]`

Tên class chưa khóa; bảng sau phân trách nhiệm, không yêu cầu thêm framework.

| Domain | Trách nhiệm | Boundary |
|---|---|---|
| Player | Input/movement; weapon control; life/damage; presentation; pickup interaction | Các trách nhiệm có thể tách component, không gom thành God MonoBehaviour |
| Enemy | Runtime health; movement sequence; firing; presentation; reward/death hooks | Movement không tự quyết completion hoặc drop policy |
| Encounter/Wave | Schedule; spawn; completion; optional events; checkpoint hooks | Không render HUD, serialize profile hoặc trực tiếp điều khiển mọi VFX |
| UI | Hiển thị state và nhận UI action | Button object không là source of truth cho lives/score |
| Audio/VFX | Phản ứng với gameplay events | Không quyết damage |
| Save | Serialize state đã được domain cung cấp | Không chứa death/weapon penalty rules |

GameObject/Component là nền cho player, enemy, pickup và VFX. Transform phục vụ
entry/path/rotation/framing/transition; Rigidbody2D/Collider2D dùng khi phù hợp cho
collision. MonoBehaviour lifecycle phải đặt input/update, physics step và camera/
presentation orchestration đúng vai trò.

Coroutine/timed sequence phù hợp với wave timing, death/respawn, boss intro,
transition và temporary buff. Không dùng coroutine như kiến trúc chung cho mọi
state khi state machine/config rõ hơn.

## State flow, camera và communication

[PROVISIONAL] Flow domain: initialization → menu → gameplay session → result/menu.
Gameplay có intro, encounter, death/respawn, pause, clear và transition;
behavior do [UI/UX](../presentation/ui-ux.md), [player](../gameplay/player-combat.md)
và [levels](../content/levels-encounters.md) sở hữu.

Encounter runner chịu trách nhiệm đóng schedule/callback, release object có ownership
thuộc encounter cũ và chỉ start checkpoint/schedule tiếp theo sau handoff. Completion,
cleanup và reward-grace behavior theo
[encounter lifecycle contract](../content/levels-encounters.md#encounter-lifecycle-và-handoff);
không để từng enemy, VFX hoặc pool tự quyết một cleanup policy khác.

Camera áp framing/direction từ level state; [audio/VFX](../presentation/audio-vfx.md)
thêm zoom/shake cue. World bounds và input conversion theo camera/aspect ratio;
camera không quyết encounter completion hoặc damage.

Ưu tiên direct references hoặc event nhỏ có scope rõ để decouple UI/presentation.
Subscribe/unsubscribe gắn lifecycle; không thêm generic event bus, service locator
hay DI container khi chưa có nhu cầu.

## Object pooling và performance

High-frequency objects SHOULD pool: player/enemy bullets, common explosions,
hit particles, currency pickups và common enemies có spawn/despawn dày. Boss không
bắt buộc pool. [UnityEngine.Pool.ObjectPool<T>](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Pool.ObjectPool_1.html)
là lựa chọn cần kiểm tra với project, không phải lý do pool mọi object.

Pool release/reuse phải reset HP, timers, target, sequence phase, hit/death flags,
projectile ownership và subscriptions phù hợp; callback cũ không được tạo double
reward/damage. Không load asset lặp lại vô ích. Profile churn thực tế trước khi thêm
complexity ở domain khác.

Mobile rendering cần giới hạn particle overdraw, transparent layers, giant fullscreen
additive effects, excessive glow, trail density và số enemy/projectile đồng thời.
Fast projectile dùng trigger/collider phù hợp; nếu có tunneling, cân nhắc Continuous 2D
hoặc approach tương đương. Không bật mode tốn CPU cho mọi Rigidbody2D. Reference:
[Rigidbody2D collision detection](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Rigidbody2D-collisionDetectionMode.html).

Exact target FPS vẫn [TBD]; initial goal SHOULD là gameplay ổn định trên Android
tầm trung. Không claim 60 FPS khi chưa profile thiết bị thật.

## Pause và time management `[LOCKED]`

HUD có pause button. Khi pause, gameplay simulation, enemy spawn, projectiles và
gameplay timers dừng; pause UI vẫn responsive và audio transition/loop không hỏng.
`Time.timeScale = 0` là một lựa chọn, nhưng UI animation cần unscaled time và coroutine
cần realtime phải được phân biệt với gameplay timers.

Restart Wave từ Pause restore checkpoint theo [save/profile](save-profile.md).
Pause controls thuộc [UI/UX](../presentation/ui-ux.md), music/SFX behavior thuộc
[audio/VFX](../presentation/audio-vfx.md).

## Architecture non-goals

Không ECS/DOTS, enterprise framework, custom node graph/editor hay backend.
Không premature abstraction, loading framework phức tạp hoặc pool mọi object theo
thói quen. [Dev/debug](dev-debug.md) sở hữu verification/DoD;
[save/profile](save-profile.md) sở hữu serialization, atomic write và migration.
