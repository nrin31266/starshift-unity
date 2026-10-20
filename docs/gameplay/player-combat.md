# Player combat

## Phạm vi và ownership

Owner của input, hold-to-fire, lives, shield precedence, damage/death/respawn và active weapon downgrade. Weapon patterns/items thuộc [content owner](../content/weapons-items-drops.md); serialization thuộc [save/profile](../systems/save-profile.md).

## Player state model

[PROVISIONAL] Responsibility states để prototype: Entry → Active → Dying →
Respawning → Active hoặc GameOver. Pause/Transition/Result khóa gameplay theo flow.
Tên enum/class chưa khóa. Mỗi hit phải qua protection/state gate trước khi resolve;
không để nhiều collider callback trong cùng death sequence trừ life lặp.

VS-009 prototype dùng `PlayerGameplayGate` chung cho input, fire và damage permission.
Gameplay scene bắt đầu `Active`; `Entry`, `Dying`, `Respawning`, `GameOver`,
`Transition`, `Result` chặn cả ba. `PostEntryInvincibility` cho input/fire và chặn
damage, theo survival contract bên dưới. Đây là naming implementation
`[PROVISIONAL]`, không khóa class/enum cho các flow chưa triển khai.
`SetState()` báo thay đổi để xóa pending input/cadence khi bị khóa; không chạy
death/respawn sequence, tự trừ lives hoặc tự chuyển state theo timer.
Gate cũng chặn permissions khi bị disable hoặc `Time.timeScale == 0`.
Global Pause do gameplay session điều phối; đổi local state không thay thế global pause.
Establishing trước continue tap không nhận manual Pause; approach sau tap cho Pause như gameplay. Ordinary handoff giữ HUD/Pause
và permissions theo gameplay/lifecycle, không bật cinematic lock. Major cinematic lock
chặn input/fire/damage độc lập state; unlock
không reset lives/protection hoặc mở sớm enemy-fire safety gate.
VS-016–021 nối `PlayerContactReceiver` vào lifecycle/lives runtime. Gate vẫn là source
of truth của phase/permission; lifecycle điều phối timers và transitions qua gate,
không thêm state enum hoặc HUD authority song song.

## Input và điều khiển

Establishing continue tap dùng uGUI trên Canvas hiện có: sau reading delay, fresh
left-mouse/touch down + up trong màn hình, không drag/cancel/hold dài (tap tối đa
.6 s `[TUNABLE]` trên HUD). Press bắt đầu
trước prompt không được tái nhận khi prompt xuất hiện. Tap này và pointer còn giữ
trước continue không trở thành gameplay ownership; clear gesture khi nhận continue,
sau đó fresh gameplay press được move/fire ngay trong approach zoom. Giữ pose và
held gesture qua Brake end; chỉ departure/reveal/awaiting tap còn cinematic lock. Không thay relative drag/hold-to-fire contract.

### Relative drag `[LOCKED]`

Người chơi chạm vùng gameplay và kéo ở vị trí thuận tay; delta của ngón tay được
chuyển thành displacement của tàu. Tàu không teleport tới touch position, nhờ đó
ngón tay không cần che tàu trong lúc né đạn.

### Hold-to-fire continuous shooting `[LOCKED]`

Gameplay pointer down tạo fire intent; giữ pointer tiếp tục bắn theo cadence kể
cả không di chuyển. Drag đồng thời tạo relative displacement và giữ fire intent.
Pointer up/cancel dừng sinh shot mới ngay khi input layer nhận release; projectile
đã bắn vẫn kết thúc hit/lifetime như bình thường. Không có nút Fire riêng.
Lần press sau đặt anchor mới, không teleport hoặc dùng lại delta cũ.

Effective fire condition là `CanFire AND gameplay fire intent held AND thermal ready`. State gate
là permission authority; held intent không tự vượt gate. Weapon không đọc device
API hoặc lấy intent từ movement component.

Gate block hủy anchor/pending movement nhưng giữ held intent của gameplay pointer
còn giữ. Resume trong khi pointer vẫn held cho fire tiếp tục, không bắn bù cadence;
movement tự đặt lại anchor tại vị trí pointer hiện tại rồi nhận delta mới, không
kéo bù đoạn trong lúc bị khóa. Mất focus hoặc disable input adapter hủy cả gesture/fire intent; cần press mới sau đó.
Disable movement chỉ hủy drag, không hủy fire intent.

Death/respawn giữ cùng rule: pointer còn held không tạo movement từ delta cũ; input/
fire bị chặn suốt Dying/entrance. Sau entrance, held pointer còn hợp lệ cho fire tiếp,
drag cũng tiếp tục từ anchor mới mà không cần release/press. Root/input adapter
disable hủy cả held intent.

Fire permission bị khóa trong:

- cinematic entry;
- death;
- respawn entrance;
- transition;
- pause;
- game over;
- result.

### Thermal lifecycle

Primary thermal mechanic/config thuộc [weapons owner](../content/weapons-items-drops.md#primary-weapon-heat--m35).
Overheated không thay Player state hoặc movement permission; chỉ primary fire
chờ thermal ready. Held pointer hợp lệ tự fire khi recovery đủ, giữ cadence rule
không bắn bù. Không thêm input/action mới.

| Boundary | Heat/latch behavior |
|---|---|
| Pause | Đứng yên theo gameplay time |
| Dying/death | Reset heat về 0, xóa Overheated ngay khi nhận damaging hit |
| Respawn | Trở lại với weapon thermal ready |
| Encounter A → B / live Mission handoff | Giữ heat/latch/delay, tiếp tục tick; không reset |
| Entry/major cinematic hoặc gameplay gate block | Tiếp tục nguội bằng scaled time, không cộng nhiệt dù pointer held |
| NewGame / Retry / Continue / Replay / Dev restart | Reset heat/latch/cooling delay/cadence |
| Game Over / Clear | Không tiếp tục thermal simulation |

### Weapon switch

Player được switch tự do giữa các weapon đã unlock [LOCKED], cycle qua những
branch khả dụng. Nhặt Core thành công tự chọn branch vừa nâng cấp; switch vẫn giữ
một heat/latch/cooling delay dùng chung cho mọi branch; đổi loại đạn, unlock hoặc tăng level đều không reset. Chi tiết level/typing thuộc item owner. Active weapon fire theo config khi gate cho phép và gameplay pointer held;
selection này phải được checkpoint-save theo [save/profile](../systems/save-profile.md).
Bố trí nút thuộc [UI/UX](../presentation/ui-ux.md); branch/pattern ở
[weapons/items/drops](../content/weapons-items-drops.md).

### Movement release

Baseline [PROVISIONAL]: thả tay kết thúc input delta; tàu dừng với smoothing ngắn
nếu có. Không dùng inertia dài kiểu trôi ngoài không gian, vì điều đó làm né đạn
khó kiểm soát. Exact smoothing vẫn cần playtest, không khóa một duration mới ở đây.

### Camera scale independence

Vì camera orthographic size thay đổi theo level, input sensitivity SHOULD giữ cảm giác theo **screen-space**, không để cùng một delta ngón tay tạo cảm giác quá nhanh/chậm chỉ vì zoom.

### Multi-touch

Gameplay drag pointer, weapon button và pause button phải có quyền xử lý touch
riêng. Touch bắt đầu trên UI không được đồng thời kéo tàu. Khi trả control sau
entrance, không áp delta cũ khiến tàu nhảy vị trí.

Input abstraction nên hỗ trợ test nhanh bằng mouse/touch simulation. Khi triển khai,
đối chiếu [Input System](https://docs.unity3d.com/Manual/com.unity.inputsystem.html)
và [Enhanced Touch](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.4/manual/Touch.html)
với package thực tế; version trong reference không phải yêu cầu pin package.

### M2 manual feel checkpoint `[PROVISIONAL]`

VS-004–005 dùng mouse left-button trong Editor và một touch đang drag qua cùng
contract delta. Press mới đặt mốc, không tạo displacement; release/cancel hoặc
mất focus kết thúc drag. Touch thứ hai không thay pointer của drag đang chạy.
Disable/re-enable movement yêu cầu press mới để tránh áp delta cũ.

`Sensitivity = 1` là seed `[TUNABLE]`: tàu dịch chuyển cùng số pixel với pointer,
độc lập zoom orthographic. Đây chưa phải feel đã duyệt; exact sensitivity vẫn cần
manual playtest. Checkpoint không thêm smoothing/acceleration; nhả pointer không
có inertia. VS-007 đã bổ sung [movement bounds](#movement-bounds-và-camera-interaction);
UI exclusion chưa được implement. Đây là trạng thái implementation hiện tại,
không thay các requirement ở trên.
Component/data flow thuộc [architecture owner](../systems/architecture.md#m2-scene-và-player-baseline-provisional).

## Player survival

### Lives `[LOCKED]`

Player dùng lives thay cho traditional HP bar. Số khởi đầu đã chốt; cap vẫn là
đề xuất để prototype và không được nâng thành locked decision.

| Giá trị | Baseline | Trạng thái |
|---|---:|---|
| Starting lives | 3 | [LOCKED] |
| Max lives | 5 | [PROVISIONAL] |

### Damage rule `[LOCKED]`

Một hit hợp lệ khi không được bảo vệ:

```text
-1 life
```

Nguồn:

- enemy projectile;
- enemy body collision;
- damaging asteroid;
- boss beam;
- damaging environment hazard.

### Shield

Shield active absorb một damaging hit rồi vỡ. Hit này không trừ life, không chạy
weapon downgrade. Sau shield-break có .7 s brief invincibility `[TUNABLE]`,
blink cùng protected cue, input/fire vẫn mở để rời body overlap. Không teleport,
không clear enemy hoặc chạy respawn; hit lặp trong window không tiêu life/charge. Stacking/refresh final của item còn
[TBD]; M4-A chỉ dùng test semantics tại [weapon/item owner](../content/weapons-items-drops.md); protection overlap
không được tự quyết qua thứ tự collider callback.

### Player death flow `[LOCKED]`

```text
Fatal hit
→ chặn duplicate damage
→ disable player control
→ disable normal firing
→ death VFX/audio
→ lives -1
→ active weapon downgrade
→ giữ đạn đang bay, tạm block hostile fire mới
→ pause enemy fire ngắn
→ nếu lives > 0: respawn
→ nếu lives == 0: Game Over
```

VS-018 khóa state Dying trước callback, rồi synchronously trừ đúng một life, áp
active-weapon penalty và block hostile fire mới, giữ projectile đang bay. Dying state
yêu cầu placeholder death cue; presentation không được quyết khi nào trừ life hoặc cho phép hit tiếp.
Death phase trước entrance dùng 0.6 s `[TUNABLE]`; clock scaled, callback feedback
lỗi không được bỏ qua mandatory life/fire-gate work hoặc giữ sequence mắc kẹt.

### Weapon penalty on death `[LOCKED]`

Chỉ weapon đang active lúc chết:

```text
level - 1
minimum = Lv1
```

Weapon khác giữ level hiện tại.

Shield-absorbed hit không downgrade.

M4-A `WeaponLoadout` giữ level từng branch; `ApplyDeathDowngrade()` giảm đúng active
branch và reset cadence/thermal states. Weapon khác giữ level/unlock. Shield absorb
không đi qua boundary này. Pattern/config ở [weapon owner](../content/weapons-items-drops.md#weapon-progression-prototype).

### Respawn `[LOCKED]`

Tàu hồi sinh bằng entrance từ ngoài gameplay edge thay vì pop vào vị trí.
Entrance chủ yếu từ bottom khi combat vertical và từ left khi combat horizontal.
Vị trí phải tránh collider enemy/hazard còn tồn tại.

Baseline state contract [LOCKED]:

| State | Input permission | Fire permission | Nhận damage |
|---|---|---|---|
| Death sequence | Off | Off | No |
| Respawn entrance | Off | Off | No |
| Post-entry invincibility | On | On | No |
| Normal gameplay | On | On | Yes |

Respawn entrance không nhận damage trong toàn bộ animation. Khi tàu đã vào vị trí
gameplay an toàn, mở lại input/fire permissions đồng thời bắt đầu post-entry
invincibility; không có khoảng trống nhận damage giữa hai state. Các giá trị On
vẫn chịu gameplay gates như Pause/Transition; fire còn cần gameplay pointer held; Yes ở Normal gameplay vẫn qua shield
và damage rule hiện có.

```text
wait ngắn → xuất hiện ngoài edge phù hợp → bay vào gameplay position
→ mở input/fire permissions và bắt đầu post-entry invincibility
→ Normal gameplay khi invincibility hết
```

VS-019 vertical entrance dùng 0.55 s `[TUNABLE]`, bắt đầu ngoài bottom của current
camera và ease vào target trong current playable region. Framing/aspect/restricted
region thay đổi thì reproject; không cache world waypoint. Preferred target ở giữa
ngang, phần dưới region; nếu enemy body chặn target thì tìm candidate an toàn khác.
Nếu các candidate đều bị chặn, giữ protected entrance, chưa mở input/fire. M3 áp cùng safe anchors qua PlayDirection: Right entrance từ left, preferred
target bên trái current playable region; Player/muzzle face Right. Hazard producers
chưa có, safe check vẫn dùng configured blocking layers.

### Invincibility

Post-entry invincibility có baseline **2.5 s [TUNABLE]**, bắt đầu sau entrance,
không bị trừ thời gian bởi entrance animation. Player có thể điều khiển và bắn
để reposition. [Audio/VFX](../presentation/audio-vfx.md) sở hữu blink/ring cue để phân
biệt state này với damage flash; effect không quyết protection window.

### Respawn fairness

Khi chết còn life để hồi sinh, giữ cả hostile/friendly projectiles đang bay; chúng
kết thúc bằng hit/viewport exit/lifetime bình thường theo feedback mới. Không xóa
enemy. Protection trong entrance và post-entry invincibility bảo vệ Player.
Enemy-fire grace khoảng **1 s [TUNABLE]** sau entrance là tunable riêng, không thay
thế protection của respawn entrance hoặc post-entry invincibility.

Game Over vẫn release mọi `EnemyProjectile` active trong cùng gameplay scene;
terminal session cũng cleanup friendly shots theo architecture. Không sửa enemy,
snapshot/save. Release dùng callback về pool, reset ownership/target, hit flag,
timer, velocity/simulation và callback trước reuse.

Enemy-fire permission bị block suốt Dying và entrance. Khi safe entrance kết thúc,
bắt đầu enemy-fire grace ở trên, độc lập post-entry invincibility; enemy movement
vẫn chạy. Block hủy pending telegraph; resume chạy local delay/charge mới, không
bắn bù. Pause đóng băng cả hai clocks. Shared authority/API thuộc
[architecture](../systems/architecture.md#m2-player-damage-lifecycle).

### Game Over

Khi lives bằng zero, dừng encounter progression và spawn mới, nhưng vẫn chạy hết
death sequence trước khi mở Game Over. Người chơi có thể Retry Wave hoặc về Main Menu.
Retry load snapshot đầu wave/encounter checkpoint, không phục hồi world giữa wave;
serialization/restore chi tiết ở [save/profile](../systems/save-profile.md).

VS-017–021 có terminal `GameOver` state và signal sau death phase: không respawn,
không nhận hit tiếp, input/fire off và enemy-fire gate giữ block. Runner dừng
spawn/progression ngay khi lives về zero; signal sau death sequence mở minimal
Game Over overlay. UI chi tiết ở
[UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional).
Disable/re-enable Player/lifecycle reset phiên runtime về starting lives/Active,
hủy pending entrance/timers; đây không phải Retry/restore checkpoint hay save.

## Movement bounds và camera interaction

Player phải bị giới hạn trong playable region của current gameplay camera/view,
không đi khỏi màn hình. Default region là full normalized viewport; gameplay phase
có thể restrict bằng region normalized nằm trong `[0, 1]`, độc lập world coordinates.
Framing/zoom policy thuộc [camera owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles).
Từ L1 Encounter 2, Up combat dùng viewport x 0–1, y 0–.55 để không bay sâu
vào tuyến địch; phần dưới giữ để né. Right combat dùng x 0–.59, y 0–1.
Chỉ chặn phía tiến vào tuyến địch; không thêm inset hai bên/đáy tạo thành hộp nhỏ.
Đây vẫn là cơ chế normalized playable viewport hiện có, không thêm movement clamp thứ hai.
Các giá trị `[TUNABLE]` nằm trong camera profiles; intro/travel và L1 Encounter 1
vẫn dùng view riêng. Không dùng hard-coded world rectangle theo map/boss/direction. Safe area HUD không
tự đồng nghĩa với world bounds. Exact margin, drag sensitivity và smoothing [TBD]/cần playtest.
Camera đổi zoom không được làm cùng delta ngón tay có cảm giác điều khiển khác hẳn.

VS-007 baseline `[PROVISIONAL]` dùng footprint gameplay hình chữ nhật quanh root,
size local **0.8 × 1.0**, đủ bao placeholder hiện tại và độc lập với artwork/damage
hitbox. Đây là envelope cho movement bounds, không phải world rectangle cố định.
Footprint theo scale/rotation của Player. Từ current camera viewport + playable
region, project các góc footprint để thu khoảng hợp lệ cho root, rồi chuyển root
đã clamp về world. Tính lại theo current camera/projection, không cache world
rectangle. Không cộng margin riêng hoặc lấy HUD safe area làm bounds.
Exact footprint/margin vẫn cần review khi đổi kích thước tàu.

Zoom/region transition co dần allowed area; Player chỉ bị đẩy vào trong khi
footprint chạm boundary hiện tại, không teleport thẳng tới boundary cuối ngay khi
request. Clamp ở physics step và sau view update trước render, kể cả state gate
đang khóa input. Aspect change hoặc request duration 0 áp region mới và clamp ở
frame hiện tại; không thêm một movement smoothing/elastic boundary riêng.
Region giãn không kéo Player ra lại vị trí cũ. Transition không hủy pointer anchor
hoặc held/fire intent; gate block vẫn theo [input contract](#hold-to-fire-continuous-shooting-locked).

Delta vượt cạnh bị bỏ ở step đang xử lý; tiếp tục kéo ra ngoài không tích lũy
displacement chờ bù. Đảo drag vào trong phải di chuyển ngay trong cùng pointer press.
Không thêm smoothing, acceleration hoặc elastic response. Khi playable region nhỏ hơn
footprint trên một trục, đặt giữa trục đó; không thể đảm bảo bao trọn footprint
trong region nhỏ hơn chính nó. Implementation và component boundary ở
[architecture owner](../systems/architecture.md#m2-scene-và-player-baseline-provisional).

Region A second feel pass `[TUNABLE]`: asteroid Survive dùng full viewport; Vanguard
chỉ thu Player vào lower combat region qua CameraProfile, không clamp trong Boss
script. Exact region/camera values thuộc [encounter owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).
Boss body/beam đều route qua existing life/protection receiver; beam telegraph không
gây hit. Sau death/Retry/Planet transition phải reset input, beam damage và view đúng
state, không giữ restricted boss region trong storm.

## Collision matrix theo gameplay contract

Đây là interaction contract, chưa khóa Unity layer names hay Rigidbody2D settings.

| Cặp tương tác | Kết quả |
|---|---|
| Hostile projectile / player | Qua protection gate rồi áp damage rule ở trên |
| Enemy body / player | Qua protection gate; phản ứng lên enemy [TBD] |
| Damaging asteroid/hazard / player | Qua protection gate; không spawn trực tiếp lên tàu |
| Boss beam/hit area / player | Theo telegraph/collision contract của enemy owner |
| Friendly projectile / enemy hoặc destructible hazard | Damage target theo content config |
| Player / pickup hoặc crate | Pickup interaction theo item owner |

VS-016 slice không resolve friendly fire, projectile-vs-projectile hoặc enemy-vs-enemy;
không tự thêm damage behavior. Invincibility bỏ qua damaging hit;
M4-A test semantics khi protection overlap ở [item owner](../content/weapons-items-drops.md#pickup-và-effect-test-policy); final stacking decision vẫn [TBD].

`PlayerContactReceiver` route hostile hit và EnemyHealth body contact qua cùng
`TryReceiveHit()`/protection gate. Enemy projectile contact tiêu thụ shot kể cả khi
Player protected, không giữ hit chờ invincibility hết. Body overlap được kiểm tra
Stay: nếu còn overlap lúc Active trở lại, nhận đúng một valid hit rồi Dying khóa
callbacks còn lại. Unity layers/matrix concrete thuộc
[architecture](../systems/architecture.md#m2-player-damage-lifecycle).

VS-011 baseline chỉ resolve friendly hit với target opt-in qua
`IPlayerProjectileTarget`; owner và collider dưới owner bị bỏ qua. Collider không
opt-in không gây damage hoặc tiêu thụ projectile trong checkpoint này; chưa thêm
interaction cho projectile-vs-projectile/friendly fire. Projectile kết thúc sau
hit đầu tiên, khóa hit trước callback và chỉ release một lần; nhiều collider trên
cùng target không được tạo nhiều hit. Target chịu trách nhiệm damage/HP state,
không để projectile tự sửa enemy HP. VS-012/013 đã nối production
[Scout receiver](../content/enemies-bosses.md#scout-drone); probe verification vẫn
chỉ là test helper. Serialized shape/runtime lifecycle
thuộc [architecture owner](../systems/architecture.md#m2-gate-và-pulse-laser-baseline-provisional).

## Combat metrics và tunables

Life loss, shield breaks, no-hit streak và combat result được gửi sang
[statistics owner](progression-economy.md); metrics được chọn theo owner đó.
Các duration ở trên là seed; protection/shield overlap, horizontal entrance và
hazard-specific routing còn cần xác minh khi domain tương ứng được implement.
[Audio/VFX](../presentation/audio-vfx.md) chỉ thể hiện kết quả, không quyết damage.
[Dev/debug](../systems/dev-debug.md) cung cấp đường test death, respawn và Retry Wave.


## M2 UI pointer ownership

VS-006 dùng EventSystem raycast tại **Began/press**, không đọc cached hover của UI
module (input sample chạy trước UI Update). Pointer bắt đầu trên Pause/button/modal
không có gameplay anchor/fire intent; rời UI vẫn không chuyển quyền cho gameplay.
Pointer bắt đầu ở gameplay giữ ownership tới release/cancel; kéo qua Pause không
click nút. Multitouch có thể dùng ngón thứ hai để Pause trong khi ngón gameplay held.

Pause/Resume là boundary hủy **cả gesture và fire intent**, yêu cầu press mới sau
Resume. Đây là ngoại lệ có chủ ý cho generic gate block giữ held intent ở trên;
death/respawn và ordinary handoff giữ contract cũ. Major establishing intro consume
continue tap và gesture còn giữ; fresh press sau tap có thể bắn/drag trong approach. Full mission start/reset và terminal
cũng cancel gesture. Ngoại lệ same-planet Mission handoff giữ held gesture và relative
drag đang hoạt động; movement owner compose travel bias nhỏ vào displacement, không
hard-animation hoặc reset pose. Major Planet departure/reveal vẫn lock và consume tap; approach sau tap mở control/fire, giữ gesture qua finish. Không dùng lại drag delta hoặc cadence từ trước overlay.


## Gameplay feel follow-up

Death delay .6 s dành chỗ cho explosion choreography; entrance .55 s trả movement/
fire sớm sau khi đạt safe anchor. Tổng nominal 1.15 s trước post-entry control,
không thêm input lock vì camera cue. Full death VFX chưa được tạo chỉ bằng tuning này.
Emergency Missile có brief protection qua lifecycle authority; timing không bị camera
shake chi phối. Damage/window/camera presentation thuộc các owner được link ở trên.
