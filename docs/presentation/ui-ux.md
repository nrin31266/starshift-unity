# UI và UX

## Phạm vi và ownership

Tài liệu này sở hữu screens, navigation, HUD, overlays và touch layout. UI hiển thị
state do gameplay/economy/save cung cấp; không quyết damage, scoring hay persistence policy.
Mọi màn hình dùng landscape [LOCKED], kể cả menu, với bố cục icon-first, gọn và dễ đọc.
HUD/button tránh giữa combat và cần touch target đủ lớn.

## Main Menu `[LOCKED]`

Menu có các action sau; mobile không cần Quit button:

```text
CONTINUE
NEW GAME
MISSIONS
HOW TO PLAY
SETTINGS
CREDITS
```

Continue chỉ enabled/visible khi CampaignSave hợp lệ. Action này load checkpoint
rồi vào Gameplay ở đầu level/wave đã lưu theo [save/profile](../systems/save-profile.md).
New Game bắt đầu L1; nếu campaign đang tồn tại phải confirm trước khi reset current
campaign. Profile, Shard, high score và Settings được giữ theo save/economy contract.

Menu background cần chuyển động nhẹ thay cho ảnh static cứng: có thể dùng starfield
chậm, ship idle, distant planet, engine hoặc parallax. Không cần gameplay simulation
nặng để tạo background sống động.

### Missions và Credits

Missions hiển thị level unlock/clear và best records, cho replay theo
[progression/economy](../gameplay/progression-economy.md). Credits ghi nguồn, license
và attribution thực tế từ [asset pipeline](../production/asset-pipeline.md);
không bịa credit cho asset chưa dùng.

### How to Play

Hướng dẫn ngắn, ưu tiên visual và tập trung vào các action/state người chơi cần nhận ra:

- Relative drag và auto-fire.
- Weapon switch, lives và Shield.
- Supply Drone và Pause.

### Settings

Baseline controls là Music Volume, SFX Volume, Vibration và Screen Shake toggle/
intensity đơn giản. Persisted fields ở [save/profile](../systems/save-profile.md);
exact haptic use vẫn [TBD] tại [audio/VFX](audio-vfx.md).

## Gameplay HUD

HUD cho biết survival, performance, currency và active weapon mà không che vùng né.
Đây là conceptual layout, không phải thông số anchors/reference resolution đã khóa:

```text
┌─────────────────────────────────────────────┐
│ 🚀 3      SCORE 012450       ✦ 284      ⏸ │
│                                             │
│                                             │
│                                             │
│                                         ◉ 2 │
└─────────────────────────────────────────────┘
```

| Thành phần | Cách trình bày |
|---|---|
| Lives | Icon + count; không dùng player HP bar |
| Score | Nhẹ trong HUD, không che combat |
| Stellar Shard | Currency riêng với score |
| Pause | Action trong HUD, touch không kéo tàu |
| Weapon | Icon + level, ưu tiên bottom-right; cycle giữa unlocked branches |
| Buffs | Shield icon; Overdrive và Support Drone có icon + timer nhỏ, không paragraph text |
| Boss | Boss name + HP bar ở top center; phase indicator chỉ khi thực sự cần |

Boss/hazard warning ngắn, chỉ dùng khi có thông tin cần báo; không spam WARNING.
[Player combat](../gameplay/player-combat.md) sở hữu touch exclusion và weapon switch;
UI không tự tính lives hoặc chọn reward.

## Pause overlay

Pause là overlay, không cần scene riêng. Các action:

```text
RESUME
RESTART WAVE
SETTINGS
MAIN MENU
```

Pause simulation/time contract ở [architecture](../systems/architecture.md#pause-và-time-management-locked).
Restart Wave restore đầu checkpoint theo [save/profile](../systems/save-profile.md);
resume phải trả lại UI/audio/gameplay state phù hợp.

## Game Over

Sau khi death sequence hoàn tất, mở Game Over overlay với selected stats và hai
đường ra. Stop spawn/progression và Retry semantics do
[player combat](../gameplay/player-combat.md) quyết.

```text
MISSION FAILED
Score
Level / Wave
Lives lost / selected stats

RETRY WAVE
MAIN MENU
```

## Level Clear và Campaign Result

Level Clear ưu tiên compact overlay/panel để giữ pacing: một ít stats, nút Continue
và handoff sang level kế. Không bắt buộc scene riêng sau từng level;
exact số stats hiển thị là [TUNABLE].

Campaign Result có kết quả đầy đủ hơn. Bảng này sở hữu yêu cầu hiển thị;
tracking definitions/score seeds ở [progression/economy](../gameplay/progression-economy.md).

| Nhóm | Fields cần hiển thị |
|---|---|
| Performance | Final Score; Best Score; Total Time |
| Combat | Enemies Destroyed; Elites Destroyed; Boss Clear Times |
| Survival | Lives Lost; Longest No-Hit Streak |
| Rewards | Stellar Shards Collected; Supply Drones Captured; Weapon Cores Collected |
| Progress | Levels Completed |
| Optional | New Record flags |

Không dùng Accuracy vì auto-fire làm metric này không phản ánh player skill.
Grade S/A/B/C vẫn [TBD], không bắt buộc trước khi có scoring rubric đủ tốt;
UI không tự thêm công thức grade.

## Screen flow

Các flow dưới nói về screen/state navigation; scene topology vẫn [PROVISIONAL]
tại [architecture](../systems/architecture.md). Pause/Game Over là overlays;
region transition có thể chạy ngay trong Gameplay scene.

```text
Initial:          Boot → Main Menu
New Game:         Main Menu → Gameplay → Level 1
Continue:         Main Menu → load CampaignSave → Gameplay → level/wave checkpoint
Normal progress:  Level intro → encounters → compact Level Clear → save → next level
Region boundary:  Boss level clear → cinematic transition → next region level
Game Over:        Gameplay → Game Over overlay → Retry Wave / Main Menu
Victory:          Final Boss → Ending → Results scene → Main Menu
```

## Safe area, aspect ratio và touch

HUD/button phải nằm trong safe area hoặc layout đã tính cutout/notch. World bounds
cần phù hợp camera và aspect ratio, không tự coi HUD safe area là gameplay bounds.
[Screen.safeArea](https://docs.unity3d.com/ScriptReference/Screen-safeArea.html) và
[Android Player Settings](https://docs.unity3d.com/6000.0/Documentation/Manual/class-PlayerSettingsAndroid.html)
là reference để kiểm tra khi implementation; không tự thay Player Settings trong editorial task.

Test ít nhất 16:9, 18:9/19.5:9 và tablet-ish landscape nếu có. Touch trên weapon/pause
không được kéo tàu; Editor SHOULD hỗ trợ mouse/touch simulation để test relative drag.
Exact pixel/dp touch size, anchors, safe margins và reference resolution vẫn [TBD],
cần kiểm tra trên mobile thay vì chỉ desktop Game view.

## Localization readiness

Text/button không bake vào sprite; layout chừa chỗ cho text dài hơn khi localize.
Ngôn ngữ shipping và font strategy vẫn [TBD]; không tự thêm localization package.
