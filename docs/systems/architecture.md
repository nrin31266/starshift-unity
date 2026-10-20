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
Ở checkpoint VS-001–003 chưa có lifecycle; phần hiện tại ở
[M2 player damage lifecycle](#m2-player-damage-lifecycle) và
[M2 L1 encounters](#m2-l1-encounter-runtime). HUD hiện có ở [M2 UI boundary](#m2-gameplay-session-và-ui-boundary); persistence chưa implement.
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
boss ID, player input, damage hoặc encounter completion. M3 CameraProfile assets
chỉ cung cấp view data/transition duration; không thêm tween/cinematic framework.

Inspector có `Target View` và `Transition Duration`; sửa target trong Play Mode đi
qua cùng request path với runtime caller. Gameplay code giữ direct reference tới
controller, gọi `RequestView(new GameplayViewSettings(size, viewport))`, hoặc overload
thêm duration tính bằng giây. LevelFlow gọi `SetBaseline` từ level/dev profile;
`ReturnToBaseline()` request đúng baseline đó (fallback seed khi chưa bind level).
API và naming hiện tại là implementation `[PROVISIONAL]`; camera policy thuộc
[content owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles).
Editor drawer dùng delayed numeric fields: commit bằng Enter hoặc đổi focus trước
khi validation chạy, tránh giá trị tạm rỗng/0 khi gõ decimal bị reset về full viewport.

Controller Update chạy trước input/movement, interpolate size và bốn Rect edges bằng
smoothstep không overshoot, dùng scaled deltaTime; pause đóng băng transition.
Request mới bắt đầu từ camera size/current region đang hiển thị, không snap về source
profile cũ. Duration 0 áp ngay, còn seed duration 0.6 s là `[TUNABLE]`. View config
không chứa mutable elapsed/current state; state đó nằm riêng trong controller.
`BaselineSize` là default framing seed, không phải gameplay minimum. Size close thấp
hơn baseline vẫn giữ nguyên; non-finite/non-positive size fallback baseline.
`MinimumSize = 0.01` chỉ là numerical guard tránh projection suy biến, không phải
camera content limit; controller dùng cùng guard khi interpolate, không ép close
request về baseline. Rect được intersect với full viewport, Rect rỗng/đảo ngược/
non-finite fallback full. Không expose easing knob chưa cần.

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

`PulseLaserDefinition` là ScriptableObject static, tham chiếu `WeaponThermalProfile`
theo weapon type. `PulseLaserWeapon` giữ cadence, một plain C# `WeaponHeatState`
dùng chung mọi branch của instance và local `UnityEngine.Pool.ObjectPool<PlayerProjectile>`.
Thermal simulation tick bằng scaled physics delta độc lập quyền bắn;
weapon kết hợp thermal ready với gate/held intent trước khi spawn. Không ghi state
vào definition/profile, không sửa Player gate để biểu diễn Overheated, không
couple movement với thermal. `GameplaySession.RunStarted` giữ thermal qua live
Mission handoff; session/checkpoint restart mới reset;
death dùng boundary downgrade/reset hiện có. HUD chỉ đọc heat/latch và cập nhật
bar khi giá trị đổi; không Find/allocate hoặc tạo UI trong heat hot path.
Semantics do [player](../gameplay/player-combat.md#thermal-lifecycle), mechanic và
seed do [weapons](../content/weapons-items-drops.md#primary-weapon-heat--m35) sở hữu.
M4-A thêm loadout/component boundaries được mô tả bên dưới; không có generic weapon framework. `Muzzle.up` cung cấp hướng bắn trong slice; Player facing
do LevelFlow apply Up/Right từ LevelDefinition/dev override; không đổi input axes.
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
gameplay scene. Instance sandbox hiện disabled; encounter runtime bên dưới dùng
cùng prefab và flight/attack components.

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
timer riêng, dùng serialized HP fraction cho persistent damage cue. Nó chỉ đổi
Hull renderer tint; không sửa HP, collider hoặc phase.
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

### M2 Scout single fire `[PROVISIONAL]`

VS-015 thêm `SingleFireAttack`, `EnemyFirePresentation` và child `Muzzle` vào Scout.
`SingleFireDefinition` giữ prefab/timing/speed/lifetime static; attack giữ phase timer,
đọc `EnemyHealth.IsAlive`/`EntryHoldFlight.CanShoot` và target Transform được scene
bind vào Player. Prefab fallback tìm Player qua existing `PlayerGameplayGate` một lần
ở Awake để lấy identity, không dùng gate đó làm authority enemy fire.
Behavior/tunables do [enemy owner](../content/enemies-bosses.md#scout-single-fire-checkpoint) sở hữu.

Attack dùng local `ObjectPool<EnemyProjectile>` theo pattern Pulse; runtime root
`EnemyProjectiles` cùng scene, không parent vào Scout. Disable producer hủy timer
nhưng không clear shot đã launch; destroy producer dispose pool và destroy root.
M3 thêm shared scene prefab pool cho common enemies và encounter stages; projectile
ownership/callback pools hiện có vẫn giữ nguyên, không thêm weapon framework.

Local timer thực thi attack, không quyết multi-enemy shot scheduling. Prefab dùng
autonomous cycle cho isolated sandbox; encounter runner gọi `SetEncounterControlled()`
trước khi activate. Chế độ này chỉ bắt đầu telegraph qua `TryRequestAttack()`, vẫn
giữ health/flight/target/safety checks và local cooldown. Coordination policy thuộc
[content owner](../content/levels-encounters.md#multi-enemy-attack-coordination).

`EnemySingleProjectile.prefab` có Rigidbody2D Kinematic, BoxCollider2D trigger và
`EnemyProjectile`; child Visual giữ SpriteRenderer. Typed component phân biệt hostile
với `PlayerProjectile`, `Owner` giữ shooter; target Player root snapshot tại launch
chỉ dùng filter contact, không đọc lại vị trí để steer. Sweep chọn contact gần nhất
trên target hierarchy; trigger đi qua cùng completion guard. Collider của owner,
enemy khác, friendly/hostile bullets và decoration không resolve Player contact.
Không thay global physics layer matrix của task sau.

Contact Player release shot kể cả chưa có receiver. `IEnemyProjectileTarget` là opt-in
notification boundary không chứa damage amount/lives; nếu receiver tồn tại trên
collider/parent, nó nhận `ReceiveEnemyHit(owner)` trước release trong `finally`.
VS-016–021 gắn `PlayerContactReceiver` production trên Player, route notification tới
lifecycle/protection gate. Probe của verifier vẫn chỉ tồn tại trong memory.
Protection/life/death resolve vẫn thuộc
[player owner](../gameplay/player-combat.md#player-state-model).
Hit/lifetime khóa completion một lần trước callback. Pool reset owner/target,
direction/speed/lifetime, collider/body simulation, callback và hit guard trước reuse.
Clock attack/projectile dùng scaled FixedUpdate; pause không advance simulation.

Presentation chỉ đọc `IsTelegraphing`, đổi Core tint; Hull damage cue độc lập.
Không có animation callback quyết shot hoặc damage. Verification fast path ở
[dev/debug](dev-debug.md#m2-scout-fire-verification).

### M2 player damage lifecycle

VS-016–021 dùng legacy Rigidbody2D/Collider2D hiện có. Unity layers được author qua
MCP, recursive trên prefab root/children; body Player/Scout giữ Kinematic, trigger và
full kinematic contacts. Các cặp khác liên quan bốn combat layers bị ignore:

| Layer pair | Physics2D interaction |
|---|---|
| Player (8) / Enemy (9) | Body contact tới Player receiver |
| Player (8) / EnemyProjectile (11) | Hostile contact tới Player receiver |
| Enemy (9) / PlayerProjectile (10) | Friendly hit tới enemy receiver |

Chưa tạo layer/producer Pickup hoặc hazard chưa có. Sweep vẫn dùng opt-in receiver/
target identity và owner filter, không dùng tag string quyết damage; projectile-vs-
projectile và friendly-vs-Player không resolve hit. `PlayerContactReceiver` route cả
projectile notification lẫn body Enter/Stay, không chứa lives hoặc death timers.

`PlayerLives` giữ số lives runtime. `PlayerDamageLifecycle` điều phối accepted-hit,
death clock, entrance, protection và terminal signal, chỉ dùng `PlayerGameplayGate`
làm phase/permission truth. `PlayerRespawnEntrance` tính viewport-relative pose,
kiểm tra candidate bằng overlap query với enemy blocking layer; không ghi Transform.
`RelativeDragMovement` là writer duy nhất, bỏ pointer delta và bypass normal clamp
khi entrance active; ghi initial pose trước khi state Respawning được publish.
Entrance clock chạy trước movement; lifecycle mở permissions trong LateUpdate sau
khi movement đã ghi safe final pose. Sau camera Update, cùng pose được reproject để
view/region/aspect không làm entrance dùng stale world bounds.

`EnemyFireGate` trên scene `Enemies` là shared safety authority: `Block()` hủy quyền
fire/pending telegraph, `ResumeAfter(seconds)` đếm scaled grace, `ClearProjectiles()`
release active hostile projectiles cùng scene qua existing pool callback. Producers
bind scene reference hoặc fallback tìm gate một lần; không có gate thì fail closed.
Nó không chọn shooter, không đóng băng movement và không là encounter/FireDirector.
Lifecycle giữ block qua death/entrance nhưng không clear đạn đang bay; request
grace khi entrance hoàn tất và block terminal ở Game Over. M2 session nối Result sau full L1; general transition chưa có;
owner sau có thể dùng cùng safety boundary, không suy từ Player `CanFire`.

`PlayerDamagePresentation` chỉ đọc gate để death flash, protection blink/tint và
hide ở Game Over; không quyết damage/phase. Active Pulse có death-penalty boundary
nhưng slice chỉ có Lv1; không thêm branch inventory/progression framework.
Ordering, timings, pointer behavior và cleanup/protection policy thuộc
[player owner](../gameplay/player-combat.md#player-survival), cue thuộc
[audio/VFX](../presentation/audio-vfx.md#player-damage-lifecycle-placeholder).
Không ghi CampaignSave/Profile. Verification ở
[dev/debug](dev-debug.md#m2-player-damage-lifecycle-verification).

### M2 L1 encounter runtime

Prototype VS-022/023 đã được promote trong M3; scene object `L1Encounters` và
GUID của Scout, L1 encounter assets được giữ. Responsibility/config hiện tại ở
[M3 runtime foundation](#m3-level-và-encounter-runtime-foundation); behavior/tuning
ở [level owner](../content/levels-encounters.md#m2-l1-playable-checkpoint).
Không chạy lại baseline builders để reset content đã migrate.

### M2 gameplay session và UI boundary

`GameplaySession` trên `L1Encounters` nhận direct `RunStarted`, `MissionCompleted`
của runner và `GameOver` sau death phase của lifecycle. Title/presentation request từ
LevelFlow được forward tới HUD với title + subtitle và optional duration override; establishing copy persistent
tới valid tap sau reading delay. `MissionCompleted` chỉ phát
ở cuối full sequence, không phải mỗi group hoặc standalone encounter. Runner kiểm
tra lives zero trước schedule/handoff trong physics tick để không spawn trong final
death sequence; completion và lives-zero cùng tick ưu tiên Game Over.

Session sở hữu Pause clock/CanPause và terminal combat boundary; CanPause chặn major intro
ở authority; ordinary handoff vẫn nhận Pause và giữ HUD. Flow.PresentationChanged và encounter
start refresh HUD visibility; UI gửi consumed continue tap/Pause/Resume và
đọc state. Pause giữ lifecycle phase, lưu/restore `Time.timeScale` và
`AudioListener.pause`; không thay state Dying/Respawning bằng enum Pause. UI vẫn dùng
unscaled time. Disable/teardown session trả clock/audio đã mượn, không giữ pause rò
sang lượt Play sau. Input cancel tại Pause/Resume theo
[player owner](../gameplay/player-combat.md#m2-ui-pointer-ownership).

Full mission completion đặt gate `Result` ngay, stop runner/coordinator, block enemy
fire, release hostile/friendly projectiles về existing pools và freeze simulation.
Không chờ presentation delay mới khóa damage/input/fire. Game Over dùng cùng cleanup
sau signal của lifecycle; session không sở hữu lives/death/respawn timers.
Dev replay vẫn reset runtime qua lifecycle trước `StartRun`, không giả checkpoint.

`PlayerGameplayGate.SetCinematicLock` tạm chặn input/fire/damage, không thay lifecycle
state/timers. Chỉ major intro dùng lock; ordinary handoff không bật nó. Gate
notification cancel pending drag/cadence; unlock
không Resume enemy safety gate để tránh bỏ qua death/respawn grace. Intro visual
proxy không ghi Player pose; presentation restore authored Visual/backdrop khi
kết thúc hoặc reset. Không tạo writer thứ hai trên Player Rigidbody.

`GameplayHud` render lives/weapon/overlay và nhận valid establishing tap qua cùng
Canvas/uGUI pointer handlers; transparent raycast area chỉ bật trong intro. Tap
chỉ arm khi prompt sẵn sàng; drag/cancel/hold dài không launch. Không poll input
thứ hai hoặc tạo menu mới. `GameplayAnnouncement` giữ title/description và fade/
duration; component đổi layout theo major establishing, Mission hoặc Đợt. Mission và
Đợt có presentation instances riêng để chồng thời gian entry. LevelFlow giữ phase
MissionTitle sau Brake chỉ tới spawn lead, độc lập visible duration; cinematic đã
hide, HUD/control/Pause mở. Title và subtitle Mission lấy từ LevelDefinition,
EncounterCuePresentation đọc Đợt từ EncounterDefinition; không dùng subtitle Encounter
thay cho Mission briefing. Scaled clocks freeze trong Pause. SafeAreaLayout chỉ cập
nhật anchors khi safe area/resolution đổi.
Canvas/RectTransform/buttons/EventSystem được author bằng `ConfigureGameplayUi` qua
Unity; không dựng UI runtime từng frame. Default execution order đặt session trước
HUD để đọc dependency sau Awake; announcement cũng initialize trước HUD. Không thêm
UI state machine/event bus hoặc pool service. Copy/layout thuộc
[UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional).

## Config data và mutable runtime state

Static definitions SHOULD dùng ScriptableObject/config asset khi phù hợp:
LevelDefinition, EncounterDefinition, EnemyDefinition, WeaponDefinition, DropTable,
CameraProfile và TransitionProfile. Config không phải runtime save; không ghi lives
hiện tại hoặc enemy current HP trở lại definition asset. Tham khảo
[ScriptableObject](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ScriptableObject.html)
khi triển khai, không coi version trong URL là version project bắt buộc.

### LevelDefinition

Tên class/asset hiện có được giữ để bảo toàn references: Mission tương ứng
`LevelDefinition`, Spawn Group tương ứng `SpawnAction`, Formation là `FormationLayout`,
Flight Sequence / Path là `FlightSequence` / `FlightPhase.Path`. Đây không phải các
cấp thay thế cho [taxonomy canonical](../content/levels-encounters.md#terminology-và-composition).

`LevelDefinition` là static ScriptableObject: stable ID, display metadata, mission
number, region ID, optional location name, PlayDirection, default CameraProfile,
ordered encounters, intro config và additional start aliases tới encounter thật.
Region ID không tự trở thành location copy. Transition-out/replay preset refs chỉ
thêm khi có consumer; M3 không tạo save/preset giả.

### EncounterDefinition và SpawnAction

`EncounterDefinition` sở hữu static completion mode/duration, spawn actions,
optional event list, camera override, firing coordination knobs và boundary timing.
M3 giữ serialized field `Groups` cùng legacy `ScoutPrefab` fallback để bảo toàn L1.
`SpawnAction` có prefab/EnemyDefinition reference, count, time offset, stagger,
formation, spacing, anchor, required flag và FlightSequence. Override definition
phải khớp prefab; không mutate definition để đổi current HP.

### FlightSequence data

`FlightSequence` asset chứa ordered `FlightPhase`: kind, path, normalized endpoints,
curve, duration/cycles, smooth interpolation, shooting eligibility, clamp và terminal
hold. Legacy entry/hold fields chỉ giữ hidden để builders/sandbox M2 còn dùng được;
L1 production assets đã trỏ sequence. Detailed phase/path behavior ở
[level owner](../content/levels-encounters.md#m3-flight-và-formation-authoring).

### M3 level và encounter runtime foundation

| Component | Runtime responsibility |
|---|---|
| LevelFlow | Bind LevelDefinition, apply direction/camera; intro phase/clock và establishing tap readiness được runner tick; gửi title/presentation changes; không render UI hoặc quản lives |
| EncounterRunner | Schedule groups theo timestamp/stable tie order; mode completion; stages Intro/Running/ProjectileDrain/BossPostDeath/RewardGrace/Handoff; generation token và exactly-once notification |
| EntryHoldFlight | Writer duy nhất của enemy pose; runtime elapsed/phase; reproject theo current camera; không quyết kill/completion/drop |
| EncounterFireCoordinator | Eligible round-robin permissions; configurable active shooter limit, spacing/volley; local attack giữ telegraph/projectile pool |
| EncounterCleanup | Registry cho hostile/temporary theo encounter token và reward theo run token; không tự tìm/drop/grant reward |
| ScenePrefabPool | Shared scene-scoped prefab storage cho common enemies; rent inactive, configure rồi activate; không chứa gameplay completion policy |
| MissionIntroPresentation | Placeholder planet/starfield nhận progress, không giữ gameplay authority |

`StartRun` validate level và start ID trước schedule. `LEVEL_START` chờ establishing
tap; readiness do LevelFlow.CanContinue kiểm tra theo scaled reading clock. Valid tap
qua session → LevelFlow.BeginApproach; phase durations/arrival copy nằm trong
MissionIntroSettings. Encounter IDs bỏ intro. Explicit dev overload nhận skip/direction/camera override,
không sửa asset hoặc persistence. Runtime counters/lists/timers luôn ở component.
Không tạo scene-per-level, event bus, DI, FireDirector hoặc cutscene framework.

`EncounterStarted(definition,generation)` phát trước spawn đầu, elapsed/counters zero,
mỗi encounter một lần; M4 có thể lấy start snapshot qua boundary này, M3 chưa ghi save.
`Completed` phát đúng một lần sau mode cleanup; bounded reward grace có thể vẫn
đang mở. `MissionCompleted` chỉ phát sau grace của encounter cuối trong full run.
Standalone không phát clear. Boss consumers giữ `Generation` token và gọi
`NotifyBossDefeated`/`NotifyBossPresentationComplete`; token cũ bị từ chối sau
reset hoặc encounter handoff. Configured post-death duration là fallback cho stub;
real staged boss content sẽ cung cấp signal qua cùng boundary.

Direct callbacks có thể reset run: runner kiểm tra generation trước khi tiếp tục.
`EncounterCleanup.Token` đổi mỗi encounter; `RewardToken` chỉ đổi khi hard reset.
RewardPickup đăng ký bằng current encounter token nhưng kiểm lifetime/collection bằng
reward token, nên stale producers bị từ chối mà pickup còn rơi vẫn hợp lệ.
Hard reset/disable hủy intro/schedule/handoff/grace, attack permissions và boundary
participants. Normal handoff behavior thuộc
[cleanup owner](../content/levels-encounters.md#encounter-lifecycle-và-handoff).

Minor projectile cleanup được `EncounterRunner` chọn từ outgoing Encounter config,
gửi qua `EnemyFireGate` tới các shot đã launch. `EnemyProjectile` giữ runtime grace/
fade, khóa cả trigger/sweep damage trước fade và reset khi về pool. UI/announcement
không điều khiển damage hoặc thời điểm completion; policy thuộc cleanup owner ở trên.

`IEncounterBoundaryObject` phân loại Hostile/Temporary/Reward, có stop-hostility,
preserve-for-grace và idempotent release hooks. Consumer register/unregister với
EncounterCleanup; M4-A reward consumer được mô tả ở boundary bên dưới.
Registry không count kill hoặc tự grant reward. Registration cần current generation
token; callback cũ bị từ chối sau reset/handoff. Reward được completion consumer
eject cũng nhận preserve-for-grace sau notification.

Prefab pool phân biệt source prefab, chỉ rent instance đã return; không lấy enemy
vừa death nhưng runner chưa resolve. Inactive root bị parent deactivate được
reclaim trước run kế tiếp vì Unity cấm reparent giữa activation traversal.
Player/enemy projectile pools `ObjectPool<T>` hiện có được giữ; không unify callback
ownership hoặc pool các content chưa tồn tại chỉ để có generic service.

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

## M4-A combat và rewards boundaries

| Component/data | Responsibility |
|---|---|
| WeaponLoadout | Mutable unlocks, per-branch level và active selection; không fire hoặc giữ heat |
| PulseLaserDefinition / WeaponLevelPattern | Static type config, năm level patterns, một thermal reference mỗi type; giữ class/GUID cũ để tương thích prefab |
| PulseLaserWeapon | Active primary cadence/heat states và pooled projectiles; loadout event reset cadence, không mutate definitions |
| PlayerProjectile | Snapshot damage/motion/tracking/splash; same-scene target registry, spawn-version guard khi enemy reuse |
| PlayerTemporaryEffects | Shield/OD/Drone runtime timer/protection; scaled gameplay gate, lifecycle clear |
| PlayerPickupReceiver | Route collect tới loadout/lives/effects/run rewards/reserve; không giữ buff state trong UI |
| SupportDrone | Một authored companion và local projectile pool; visual/timer đọc effects owner, damage không mượn primary weapon authority |
| RunRewards | Runtime score/shard và verification counters; reset theo run, không save/profile |
| RewardDropTable / PickupDefinition | Static reward source/weights/item tuning; test overrides không sửa global drops |
| RewardRoller | Plain C# seeded selection, state filtering/weights và bounded supply pity history |
| RewardDirector | Subscribe kill/run boundary, chọn drop, rent/return reward; không quản Player movement/death/UI |
| RewardPickup | Pooled reward-scope participant, contact/lifetime và callback collect; không tự chọn/áp effect |

EncounterRunner phát `EnemyDefeated` đúng một lần từ lethal hook trước root disable;
unsubscribe khi slot release. Movement/exit/cleanup không giả reward death. Group
có optional RewardOverride; normal fallback ở director. Supply/Resource tái dùng
EnemyHealth/EntryHoldFlight và prefab pool; disabled attack, contact hazard config
riêng. Optional Event chỉ schedule group qua foundation M3.

ScenePrefabPool tái dùng common enemies và pickups; rent inactive → configure/bind
scope token → parent vào active root → activate. Projectiles của primary/drone có
scene root độc lập Player, tránh dịch chuyển cùng tàu. Pool reset bỏ callbacks,
target/version, hit flags, lifetime và ownership. Static enemy registry chỉ giữ
active health trong scene; OnDisable unregister, subsystem registration clear.
Không dùng singleton/event bus/DI/loot framework mới. Game Over/Clear/reset release
rewards qua existing scope, clear effects và friendly projectiles qua session/lifecycle.

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
[foundation](#dynamic-gameplay-view-foundation); LevelFlow apply Up/Right theo level hoặc dev override.
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


## M4-B persistence integration

`CampaignLifecycle` kết nối EncounterRunner, GameplaySession và Player owners bằng
scene references. Plain C# CampaignProgress/JsonSaveStore không tìm hoặc mutate scene.
Persistent/runtime categories, schema và restore/write permissions thuộc duy nhất
[save/profile](save-profile.md#m4-b-implementation-provisional).

Runner nhận optional restore callback sau toàn bộ RunStarted consumers, trước
LevelFlow/EncounterStarted; generation guards vẫn chặn reentrant start. Raw start
thu hồi Campaign write permission. GameplaySession.ResetRuntime sở hữu pooled
friendly shot/effect/thermal cleanup và clock release; PlayerDamageLifecycle restore
hủy pending death/entrance qua public owner API. RelativeDragMovement là writer của
Player checkpoint pose. HUD chỉ gửi Retry intent và đọc CanRetry, không serialize.


## Gameplay feel boundaries

EmergencyMissileReserve sở hữu stock, activation/cooldown, scaled flight timer/
generation và detonation damage qua EnemyHealth. Fired chỉ báo launch; Detonated
báo sau damage. EmergencyMissilePresentation đọc flight progress để vẽ rocket và
chỉ phát flash/burst/GameplayViewController impulse ở Detonated. Config classification ở EnemyDefinition, không
suy luận boss từ current Encounter mode hoặc HP. UI gửi intent, không giữ reserve.
Session reset/terminal hủy pending flight trước khi freeze clock và dọn presentation;
checkpoint DTO restore stock qua reserve owner. Không coroutine, animation callback
hoặc VFX nào được cấp quyền damage. Primary config có optional sprite/trail override;
projectile reuse xóa trail và restore sprite/scale, không đổi physics root.

GameplayViewController áp impulse trong render callback rồi restore projection;
không dịch Transform của camera đang được movement/bounds/flight dùng làm authority.
Các tunables thuộc [presentation owner](../presentation/audio-vfx.md#reserve-missile-impact-cue).


## M5 composition boundaries `[PROVISIONAL]`

EnemyDefinition giữ archetype, FlightSequence giữ path, SingleFireDefinition giữ
attack. Burst count/spacing và spread count/fan là attack data, không có Bomber mission controller. Elite
modifier giữ HP/cadence/scale/accent config; EnemyHealth nhận modifier khi spawn,
EnemyModifierPresentation đọc state và reset scale/accent khi pool return.

Vanguard reuse EnemyHealth/EntryHoldFlight. BossPhaseRunner chỉ chọn module/phase;
BossAttackModule giữ telegraph và pooled projectiles. BossDeathSequence chỉ giữ scaled
presentation timer/callback. Runner giữ defeat boundary/generation/completion, khóa
producers và clear hostile shots ngay khi zero HP, giữ dying boss root tới cuối
sequence. Reward callback chạy sau beats, trước completion/reward grace. Stop/Retry
hủy callback và thu hồi root; không để presentation cấp quyền damage.

RegionCampaignFlow orchestration L1–L8, same-region travel và A→B journey, tham chiếu catalog
rõ. CampaignLifecycle giữ write permission/restore; CampaignProgress mutate DTO và
atomic transaction. GameplaySession vẫn giữ clock/terminal/Pause. BossHud chỉ đọc
health/name/phase; không quyết Boss lifecycle. Không thêm event bus hoặc scene thứ hai.


## M5 pacing refinement boundaries

EncounterDefinition giữ entry lead/title duration/adaptive beat rules; SpawnAction
giữ beat index/subgroup offset/stagger. Runner giữ beat-start clocks và enemy pressure;
không mutate definition. HazardStreamSchedule là plain seeded schedule builder,
HazardStreamDefinition giữ cone/density/size/speed; EntryHoldFlight thực thi continuous
unclamped transit, AsteroidVisualMotion chỉ xoay visual. Không có từng asteroid prefab copy.

RegionCampaignFlow giữ same-planet travel; RelativeDragMovement compose bias và pointer
delta. CampaignLifecycle đánh dấu atomic Mission advance để HUD không nhầm idle giữa
hai runs thành terminal/hide title; preserve input/pose là explicit launch option.
PlanetJourney giữ scaled major cinematic phase/callback generation; LevelFlow nối existing
intro presentation và Up/Right boundary. GameplaySession giữ input/clock cleanup;
presentation không cấp damage hoặc persistence writes. Timing thuộc level/audio owners.


## M5 shared composition and reward floor boundaries

`FormationPlan` là static slot data; SpawnAction chỉ tham chiếu plan/dải slot và
fixed HP override khi cần. Validator từ chối duplicate/range overlap. EntryHoldFlight
vẫn giữ pose/clock/reprojection; combat offset/reposition và motion pause được reset
khi pooled instance activate. Boss phase/module gửi intent, không tự clamp Transform.
Playable viewport vẫn do CameraProfile → GameplayViewController → PlayerMovementBounds
sở hữu; asteroid/Boss region policy ở [level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).

EncounterRewardQuota là plain C# deterministic selection state, tách khỏi static
EncounterDefinition. RewardDirector subscribe encounter start, kill, escape và
completion; chọn/credit/fallback floor, không mutate config hoặc persistence DTO.
RewardDirector phát drop-spawn/collected facts cho scoped verification observers;
không thêm debug log hoặc persistence write. RunStarted reset roller history; encounter boundary reseed selection nhưng giữ supply
pity history trong run. Save/profile không cần schema mới: stable encounter seed được
tái dựng, Player checkpoint/Shard ledger/write authority giữ nguyên. Quota/overflow
semantics thuộc [reward owner](../content/weapons-items-drops.md#encounter-reward-floor-provisional).

Hazard variant data thuộc stream config; scheduler snapshot scale/HP/transit vào
SpawnAction, EnemyHealth nhận HP trước activate. EnemyDamagePresentation chỉ đọc
health và chọn tint/optional frame. AsteroidDestructionPresentation subscribe lethal
reward event để phát pooled visual burst, không có gameplay damage/shake authority.

BossAttackModule giữ một local attack clock: projectile mode dùng pool hiện có;
beam mode charge/active bounds và Physics2D query route qua PlayerContactReceiver.
AttackBeamPresentation nhận bounds/read-only active cue, không quyết damage. Beam
braces motion qua flight owner và nhả khi cancel/end/death/disable. Phase runner
cancel module trước reposition; death/generation/Retry vẫn do existing owners xử lý.
Không thêm Wave runtime, per-Mission controller, loot framework hoặc boss-minion framework.


## M6 enemy và boss extension boundaries `[PROVISIONAL]`

Không thêm mission-specific runtime controller hoặc encounter framework. `DirectedAttack`
đọc immutable `DirectedAttackDefinition`, giữ attack clock/locked aim và query beam damage
qua PlayerContactReceiver; AttackBeamPresentation chỉ vẽ segment. SingleFireAttack adapter
đưa Sniper/Interceptor qua EncounterFireCoordinator hiện có. EntryHoldFlight giữ committed
strike/return pose và là writer duy nhất; attack không ghi transform của enemy hoặc Player.

`EscortSpawner` chỉ quản cadence và bounded living-owner list có SpawnVersion guard.
`EncounterRunner.SpawnReinforcement` validate group/token, rent/configure/activate và đăng
ký cùng enemy slots, fire coordinator, cleanup/death hooks. Dynamic escorts không required,
không reward/score/quota, không có world/save snapshot. Death/disable owner chỉ yêu cầu
retreat; runner vẫn giữ release ownership qua ExitDrain hoặc hard reset.

BossShieldNetwork quản core damage block và node health aggregation. BossPhaseRunner
chọn shield/exposed rotation; BossAttackModule reuse plasma/projectile pool hoặc delegate
DirectedAttack/EscortSpawner. Module health là instance riêng, không có score/drop hook.
EnemyHealth giữ HP/damage authority; blocked core hit có feedback event nhưng không
làm giảm HP, không cho presentation mở vulnerability. Core emitters duy trì phase 2
sau module destruction; không thêm generic boss framework.

RegionCampaignFlow dùng RegionId để phân biệt same-region travel với A→B journey;
catalog hết ở L8 thì GameplaySession vào terminal result. Save transaction và legacy
marker policy thuộc [save owner](save-profile.md#m6-campaign-continuation-và-current-release-end-provisional).


M6 human-feedback extension: EncounterFireCoordinator sở hữu một budget chung cho
common attacks và BossPhaseRunner; boss tính một source, minions dùng các slot còn
lại. Count theo attack start, drain cả đợt trước rest, không mở slot mới vì một enemy
vừa chết. EntryHoldFlight vẫn là pose writer duy nhất: authored MotionPeriod tách
idle loop khỏi phase lifetime, clock encounter đồng bộ các slot, blend-in tránh
pop sau entry. Escort entry origin là runtime state, không sửa FlightSequence asset.
PlayableAreaPresentation đọc CurrentPlayableViewport và BoundaryPressure từ RelativeDragMovement
để báo cạnh khi cố giữ kéo vượt giới hạn; không có lớp phủ nền thường trực hoặc
pose writer mới. Movement vẫn clamp bằng PlayerMovementBounds hiện có.

M6 combat closure organization: `Scripts/Enemies/Attacks` giữ common/direct attacks
và `AttackBeamPresentation` dùng chung; `Enemies/Bosses` giữ module/phase/shield/death
và barrage của boss; `Enemies/Presentation` giữ damage/fire/entry/asteroid cues.
Namespace `StarShift.Enemies` và script GUID giữ nguyên; không tạo assembly/framework.
`BossBarrageDefinition` giữ config, `BossBarrage` giữ timer/gap/pool runtime, damage
vẫn qua existing PlayerContactReceiver. BossHud chỉ đọc tổng node HP hoặc core HP.
