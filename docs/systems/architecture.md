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

## Unity và Android baseline

Baseline xác minh ngày 2026-10-16:

| Hạng mục | Giá trị |
|---|---|
| Unity Editor | 6000.5.9f1, revision b57deb96f08d |
| URP / Core / ShaderGraph / URP Config | 17.5.0, nguồn BuiltIn cho Unity 6000.5 |
| Input System | 1.20.0, giữ action asset của template và active input handler hiện tại |
| Product Name | StarShift |
| Orientation | LandscapeLeft cố định; Portrait và Portrait Upside Down disabled |
| Rendering | UniversalRP asset với Renderer2D tại mọi quality level; camera post-processing off |
| Android toolchain | SDK Build Tools / Platform Tools 36.0.0, Command-line Tools 16.0; NDK r27c (27.2.12479018); OpenJDK 17.0.18+8, đều trong AndroidPlayer của Editor |
| Android Player | IL2CPP, ARM64; minimum API 26, target API Automatic; graphics API Automatic (Vulkan, OpenGLES3) |
| Android application identifier | `com.DefaultCompany.StarShift`, giữ giá trị hợp lệ hiện có; chưa chốt branding |

URP manifest, lock và Editor đã nhất quán ở version đi kèm Editor. Khi thay Editor
hoặc render pipeline sau này, kiểm tra metadata/compatibility trước khi đổi package;
không pin version từ một template/reference khác. AI Inference 2.6.1, Pipeline
0.8.0-exp.1 experimental, Test Framework 1.7.0 và các package khác giữ nguyên.

Editor đã nhận Android Build Support và active target là Android sau khi switch/import
hoàn tất. Cài đặt ban đầu thiếu NDK và SDK Command-line Tools dù module cha đã được
đánh dấu installed; đã bổ sung đúng child modules bằng Unity CLI installer cho
6000.5.9f1. SDK/NDK ngoài Unity không thay thế module Editor. Smoke output phải nằm
trong ignored folder như `app/Builds/Android/`; device/performance baseline vẫn
[TBD]. Không đổi identifier cho branding trong baseline task.

Development smoke APK từ SampleScene đã build thành công (IL2CPP/ARM64, debug
signing mặc định) tại `app/Builds/Android/StarShift-smoke.apk`. Manifest xác nhận
minimum API 26, target/compile API 36 và activity landscape. BuildReport có 0 error,
979 warnings (shader AI Inference và Pipeline runtime config chưa được tạo); không
đổi package để né warnings. ADB không có device nên chưa xác minh install/launch,
startup crash hoặc landscape trên thiết bị thật. Build cache `.utmp/` cũng được ignore.

Build list vẫn chỉ có `Assets/Scenes/SampleScene.unity` với Main Camera và Global
Light 2D; scene template không phải level. SampleScene đã chạy Play Mode với frame
counter tăng, Renderer2D active và Input System event probe thành công trong memory.
Các kiểm tra này không xác nhận Android runtime, touch thật hoặc gameplay.
Play Mode sau khi switch Android tiếp tục có frame counter tăng, Renderer2D active,
post-processing off và Input System event probe thành công trong memory.

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

### M2 scene và Player baseline `[PROVISIONAL]`

VS-001–003 có foundation tại `app/Assets/StarShift/Scenes/02_Gameplay.unity`.
Mở scene này trực tiếp để Play; build list vẫn giữ SampleScene của baseline trước.
Camera orthographic dùng seed L1 từ
[camera owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles),
giữ Renderer2D và post-processing off. Global Light 2D chiếu sáng sprite;
EnvironmentPlaceholders chỉ có hai asteroid trang trí, không collider/behavior.
Placeholder geometry theo workflow của [asset pipeline](../production/asset-pipeline.md#placeholder-first),
không phải approved art hoặc visual family đã khóa.

Player scene instance nối tới `app/Assets/StarShift/Prefabs/Player/Player.prefab`:

```text
Player                         Transform, Rigidbody2D,
                               InputSystemPointerInput, RelativeDragMovement,
                               PlayerMovementBounds, PlayerGameplayGate,
                               PulseLaserWeapon
├── Visual                     Transform
│   └── HullPlaceholder         SpriteRenderer
├── Collision                  CircleCollider2D (trigger)
└── Muzzle                     Transform (up = hướng bắn gameplay)
```

Root giữ transform/physics của runtime entity; Visual cho phép thay presentation
mà không đổi hitbox; Collision giữ representation độc lập với artwork.
Rigidbody2D Kinematic, gravity zero và freeze rotation là baseline prototype;
collider radius/offset cũng provisional, chưa khóa movement API hay collision matrix.
Prefab root ở origin, unit scale; spawn position thuộc scene instance.
Chưa có lives/death/respawn sequence, encounter, HUD hoặc persistence.
Không tạo component rỗng để giữ chỗ cho các trách nhiệm chưa implement.

VS-004–005 có runtime scripts ở `Scripts/Player/`:
`PlayerPointerInput` là abstract MonoBehaviour với `ReadDelta()` (Vector2 pixel)
và `ResetDrag()`, thêm `FireHeld`/`FireIntentChanged` cho weapon. `ResetDrag()`
chỉ xóa anchor/delta movement, không xóa held intent. `InputSystemPointerInput`
là adapter duy nhất biết Input System
Mouse/Enhanced Touch. Adapter sample trong Update trước movement và publish
snapshot không bị consume; movement đọc delta, weapon đọc held intent độc lập.
Không cần movement chạy để input/weapon hoạt động; weapon không đọc device API.
Enhanced Touch enable/disable đi theo adapter lifecycle; không đổi template action
asset, Input System update mode hay thêm input framework.

`RelativeDragMovement` đọc delta một lần mỗi Update, gom pixel delta cho FixedUpdate,
rồi đổi sang world displacement qua current pixel dimensions/projection của camera. FixedUpdate
áp lên position của Rigidbody2D Kinematic ở root; không nhân deltaTime cho displacement
và không tạo velocity/inertia. Direct body-position update chưa giải quyết blocking
collision; physics response cần review khi có gameplay tương ứng.
Prefab reference tới input component trên cùng root; camera được bind ở scene instance,
với Main Camera fallback khi prefab được instantiate độc lập. Inspector chỉ thêm một
feel knob; seed và behavior thuộc
[player owner](../gameplay/player-combat.md#m2-manual-feel-checkpoint-provisional).

VS-007 tách `PlayerMovementBounds` trên cùng root. Movement đưa proposed body
position, camera và current playable viewport vào `ClampPosition()` trước khi áp
position trong FixedUpdate, rồi bỏ pending delta. Bounds project các góc footprint
local sang viewport, clamp root theo phần region còn lại và chuyển về world.
Component không đọc pointer, renderer hoặc damage collider và không ghi position.
Movement là writer duy nhất: thêm LateUpdate để clamp sau view update, kể cả gate
đóng hoặc không có physics step trong render frame. Footprint có gizmo khung vàng khi
chọn Player; Inspector không thêm camera reference trùng với movement.
Seed và boundary behavior thuộc
[player owner](../gameplay/player-combat.md#movement-bounds-và-camera-interaction).

### Dynamic gameplay view foundation

`Scripts/Gameplay/GameplayViewSettings` là serializable value config nhỏ gồm target
orthographic size và normalized playable Rect. `GameplayViewController` trên Main
Camera sở hữu cả camera framing lẫn current playable region; không biết level ID,
boss ID, player input, damage hoặc encounter completion. Chưa cần ScriptableObject,
registry profile, tween/cinematic framework hoặc authoring system.

Inspector có `Target View` và `Transition Duration`; sửa target trong Play Mode đi
qua cùng request path với runtime caller. Gameplay code giữ direct reference tới
controller, gọi `RequestView(new GameplayViewSettings(size, viewport))`, hoặc overload
thêm duration tính bằng giây; `ReturnToBaseline()` request baseline full viewport.
API và naming hiện tại là implementation `[PROVISIONAL]`; camera policy thuộc
[content owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles).
Editor drawer dùng delayed numeric fields: commit bằng Enter hoặc đổi focus trước
khi validation chạy, tránh giá trị tạm rỗng/0 khi gõ decimal bị reset về full viewport.

Controller Update chạy trước input/movement, interpolate size và bốn Rect edges bằng
smoothstep không overshoot, dùng scaled deltaTime; pause đóng băng transition.
Request mới bắt đầu từ camera size/current region đang hiển thị, không snap về source
profile cũ. Duration 0 áp ngay, còn seed duration 0.6 s là `[TUNABLE]`. View config
không chứa mutable elapsed/current state; state đó nằm riêng trong controller.
Size dưới baseline được nâng lên baseline; Rect được intersect với full viewport,
Rect rỗng/đảo ngược/non-finite fallback full. Không expose easing knob chưa cần.

Movement lấy controller trên camera đã bind, đọc `CurrentPlayableViewport`; nếu
camera không có controller thì dùng full viewport như baseline cũ. Bounds giữ
geometry-only responsibility; camera không tự di chuyển Player. Clamp bằng current
projection và pending **pixel** delta tránh dùng world displacement tính từ zoom cũ.
Không có signal đổi view làm reset input/fire hoặc bypass `PlayerGameplayGate`.
Foundation yêu cầu movement/bounds components đang enabled; gate chặn permission
bằng state, không disable movement để làm pause/entry.

Gizmo khi chọn Main Camera: full viewport trắng, current playable region cyan
(Edit Mode hiển thị target region); footprint vàng vẫn thuộc Player bounds.
Gizmo chỉ phục vụ development. Chưa triển khai camera rotation/dynamic direction,
camera shake hoặc sequencer.

### M2 gate và Pulse Laser baseline `[PROVISIONAL]`

VS-009–011 nối gate vào movement và weapon qua component references trên Player
root. `PlayerGameplayGate` là nguồn permission duy nhất; input adapter đọc pointer
và giữ delta/held intent riêng. Movement xóa pending delta khi gate đóng, kiểm tra
lại trước physics position update. Weapon chỉ spawn khi `CanFire && FireHeld`,
bỏ cadence backlog khi gate hoặc held intent đóng; release/press mới reset cadence.
Weapon reference trực tiếp cùng `PlayerPointerInput` với movement, không reference
`RelativeDragMovement`. Permission/state
contract thuộc [player combat](../gameplay/player-combat.md#player-state-model).

`PulseLaserDefinition` là ScriptableObject static, còn `PulseLaserWeapon` giữ
cooldown và local `UnityEngine.Pool.ObjectPool<PlayerProjectile>`. Không có weapon
switch/upgrade framework. `Muzzle.up` cung cấp hướng bắn trong slice; Player facing
hiện tại quyết Up/Right, chưa có LevelDefinition/direction handoff runtime.
Pool instances ở runtime root `PlayerProjectiles`, không parent vào tàu đang di
chuyển; root được dọn cùng weapon. Disable weapon chỉ ngừng spawn, projectile đã
bắn vẫn kết thúc bình thường. Seed/config ownership thuộc
[weapons owner](../content/weapons-items-drops.md#pulse-laser).

`Prefabs/Projectiles/PlayerPulseProjectile.prefab` có root Rigidbody2D Kinematic,
BoxCollider2D trigger và `PlayerProjectile`; child Visual chỉ có SpriteRenderer.
Projectile snapshot config, giữ owner, sweep Rigidbody2D qua đoạn đường của mỗi
FixedUpdate rồi xử lý trigger overlap qua cùng single-hit guard. Receiver opt-in
được tìm trên collider/parent; projectile không chứa enemy HP hoặc reward logic.
Hit/lifetime đi qua một completion path rồi callback trả về local pool; pool reset
owner, timers, direction, damage, hit flag, collider/simulation và callback trước reuse.
Global timeScale pause dừng cả cooldown lẫn projectile movement/lifetime.
Không thay collision layer matrix hoặc tạo framework projectile chung cho enemy.

`tools/unity/VerifyPulseLaser.cs` tạo probe chỉ trong memory qua MCP `run_script`;
không có scene/prefab production tham chiếu probe. Checklist/giới hạn verification
thuộc [dev/debug](dev-debug.md#m2-pulse-laser-verification).

### M2 Scout baseline `[PROVISIONAL]`

VS-012–014 tạo `Prefabs/Enemies/Scout.prefab`, đặt một instance trong `Enemies` của
gameplay scene; không thêm scheduler, encounter runner hoặc enemy pool.

```text
Scout                      Rigidbody2D (Kinematic), EnemyHealth,
                           EntryHoldFlight, EnemyDamagePresentation
├── Visual
│   ├── HullPlaceholder     SpriteRenderer
│   └── CorePlaceholder     SpriteRenderer
└── Collision               BoxCollider2D (trigger, attached to root body)
```

`EnemyDefinition` hiện chỉ chứa max HP static; `Data/Enemies/Scout.asset` dùng seed
từ [enemy owner](../content/enemies-bosses.md#balance-seed-table). Không thêm field
weapon/drop/score chưa có consumer. `EnemyHealth` giữ current HP và implement
`IPlayerProjectileTarget`, ignore hit khi inactive/dead hoặc damage không dương.
HP về zero khóa hit trước notification. `Damaged`/`Died` là direct events có scope
instance; death disable root, không đợi presentation hoặc tự roll reward.
OnEnable reset HP; không mutate definition. Đây là reuse reset contract, chưa mang
nghĩa production enemy pooling đã có.

`EnemyDamagePresentation` subscribe/unsubscribe theo lifecycle, đọc HP và flash
timer riêng. Nó chỉ đổi Hull renderer tint; không sửa HP, collider hoặc phase.
Normal tint lấy từ authored renderer, reset khi disable; enable giữa damaged state
phải phản ánh HP hiện tại. Presentation seed thuộc
[audio/VFX](../presentation/audio-vfx.md#enemy-damage-feedback).

`EntryHoldFlight` là movement component nhỏ cho Entry → Hold, không phải generic
FlightSequence framework. Normalized anchors/duration nằm trên component và có
thể override ở instance; camera scene reference hoặc Main Camera fallback trên
prefab. FixedUpdate advance scaled clock và kinematic pose; LateUpdate reproject
cùng phase theo camera sau view Update. Đồng bộ Transform cùng pose để render
không trễ một physics step khi zoom. Chỉ component này ghi Scout position.
Hold clamp dùng BoxCollider envelope và current projection, độc lập final sprite;
region nhỏ hơn envelope thì dùng midpoint trên trục đó. Path không đọc pointer,
player gate, weapon hoặc completion policy. OnEnable reset clock/entry; health
dead/inactive dừng motion. Content seed thuộc
[flight owner](../content/levels-encounters.md#scout-entryhold-checkpoint).

Production prefab chỉ reference runtime components/assets; các helpers trong
`tools/unity/` không thuộc runtime assembly. Verification contract ở
[dev/debug](dev-debug.md#m2-scout-verification).

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

Gameplay phải chạy với placeholder trước final art theo
[placeholder-first workflow](../production/asset-pipeline.md#placeholder-first).
Presentation có thể thay sprite/VFX mà không đổi gameplay contract; boss modular
presentation theo [art direction](../presentation/art-direction.md#boss), không áp
một hierarchy/class cố định hoặc để animation tự quyết damage/phase.

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

Camera nhận view request từ gameplay state theo
[foundation](#dynamic-gameplay-view-foundation); direction orchestration còn future.
[Audio/VFX](../presentation/audio-vfx.md) sở hữu feedback cues và shake.
World bounds và input conversion theo camera/aspect ratio/current playable region;
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

### Gameplay frame rate `[TUNABLE]`

`FrameRateLimit` trên Main Camera của `02_Gameplay` giới hạn mặc định 60 FPS để
tránh chạy không giới hạn khi Play. Component tắt VSync ở runtime để
`Application.targetFrameRate` có hiệu lực trên desktop. Chỉnh Target Frame Rate
trong Inspector trước khi Play: 30 tiết kiệm hơn, 90/120 cho màn hình tần số cao.
Đây là giới hạn yêu cầu, không bảo đảm thiết bị đạt FPS đó; chưa profile Android.

### Pause behavior

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
