# Player combat

## Phạm vi và ownership

Owner của input, auto-fire, lives, shield precedence, damage/death/respawn và active weapon downgrade. Weapon patterns/items thuộc [content owner](../content/weapons-items-drops.md); serialization thuộc [save/profile](../systems/save-profile.md).

## Player state model

[PROVISIONAL] Responsibility states để prototype: Entry → Active → Dying →
Respawning → Active hoặc GameOver. Pause/Transition/Result khóa gameplay theo flow.
Tên enum/class chưa khóa. Mỗi hit phải qua protection/state gate trước khi resolve;
không để nhiều collider callback trong cùng death sequence trừ life lặp.

## Input và điều khiển

### Relative drag `[LOCKED]`

Người chơi chạm vùng gameplay và kéo ở vị trí thuận tay; delta của ngón tay được
chuyển thành displacement của tàu. Tàu không teleport tới touch position, nhờ đó
ngón tay không cần che tàu trong lúc né đạn.

### Auto-fire `[LOCKED]`

Player bắn tự động khi gameplay ở state cho phép.

Không cần spam nút Fire trên mobile.

Auto-fire bị disable trong:

- cinematic entry;
- death;
- respawn entrance;
- transition;
- pause;
- game over;
- result.

### Weapon switch

Player được switch tự do giữa các weapon đã unlock [LOCKED], cycle qua những
branch khả dụng. Active weapon tự fire theo config khi gameplay state cho phép;
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
weapon downgrade và chỉ phát shield-break feedback. Stacking/refresh của item còn
[TBD] tại [weapon/item owner](../content/weapons-items-drops.md); protection overlap
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
→ clear hostile bullets theo rule
→ pause enemy fire ngắn
→ nếu lives > 0: respawn
→ nếu lives == 0: Game Over
```

### Weapon penalty on death `[LOCKED]`

Chỉ weapon đang active lúc chết:

```text
level - 1
minimum = Lv1
```

Weapon khác giữ level hiện tại.

Shield-absorbed hit không downgrade.

### Respawn `[LOCKED]`

Tàu hồi sinh bằng entrance từ ngoài gameplay edge thay vì pop vào vị trí.
Entrance chủ yếu từ bottom khi combat vertical và từ left khi combat horizontal.
Vị trí phải tránh collider enemy/hazard còn tồn tại.

Baseline state contract [LOCKED]:

| State | Input | Auto-fire | Nhận damage |
|---|---|---|---|
| Death sequence | Off | Off | No |
| Respawn entrance | Off | Off | No |
| Post-entry invincibility | On | On | No |
| Normal gameplay | On | On | Yes |

Respawn entrance không nhận damage trong toàn bộ animation. Khi tàu đã vào vị trí
gameplay an toàn, bật lại input và auto-fire đồng thời bắt đầu post-entry
invincibility; không có khoảng trống nhận damage giữa hai state. Các giá trị On
vẫn chịu gameplay gates như Pause/Transition; Yes ở Normal gameplay vẫn qua shield
và damage rule hiện có.

```text
wait ngắn → xuất hiện ngoài edge phù hợp → bay vào gameplay position
→ bật input + auto-fire và bắt đầu post-entry invincibility
→ Normal gameplay khi invincibility hết
```

### Invincibility

Post-entry invincibility có baseline **2.5 s [TUNABLE]**, bắt đầu sau entrance,
không bị trừ thời gian bởi entrance animation. Player có thể điều khiển và bắn
để reposition. [Audio/VFX](../presentation/audio-vfx.md) sở hữu blink/ring cue để phân
biệt state này với damage flash; effect không quyết protection window.

### Respawn fairness

Khi chết, clear hostile projectiles đang gây nguy cơ unfair [LOCKED] nhưng không
xóa enemy. Enemy fire tạm dừng khoảng **1 s [TUNABLE]**; cùng entrance an toàn và
invincibility, khoảng nghỉ này tránh tình trạng vừa respawn đã chết lại.
Enemy-fire grace là tunable riêng, không thay thế protection của respawn entrance
hoặc post-entry invincibility.
Exact bullet cleanup scope vẫn cần chốt khi implementation.

### Game Over

Khi lives bằng zero, dừng encounter progression và spawn mới, nhưng vẫn chạy hết
death sequence trước khi mở Game Over. Người chơi có thể Retry Wave hoặc về Main Menu.
Retry load snapshot đầu wave/encounter checkpoint, không phục hồi world giữa wave;
serialization/restore chi tiết ở [save/profile](../systems/save-profile.md).

## Movement bounds và camera interaction

Player phải bị giới hạn trong gameplay bounds phù hợp camera/aspect ratio và
hướng chiến đấu hiện tại; không đi khỏi màn hình. Safe area HUD không tự đồng nghĩa
với world bounds. Exact margin, drag sensitivity và smoothing [TBD]/cần playtest.
Camera đổi zoom không được làm cùng delta ngón tay có cảm giác điều khiển khác hẳn.

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

Friendly fire, projectile-vs-projectile và enemy-vs-enemy chưa được thiết kế chốt;
không tự thêm damage behavior. Invincibility bỏ qua damaging hit;
chi tiết shield consumption khi các protection state overlap [TBD].

## Combat metrics và tunables

Life loss, shield breaks, no-hit streak và combat result được gửi sang
[statistics owner](progression-economy.md); không dùng accuracy vì auto-fire.
Các duration ở trên là seed; respawn wait/entry duration, exact bullet cleanup scope,
protection overlap và collision layer setup còn cần xác minh khi implementation.
[Audio/VFX](../presentation/audio-vfx.md) chỉ thể hiện kết quả, không quyết damage.
[Dev/debug](../systems/dev-debug.md) cung cấp đường test death, respawn và Retry Wave.
