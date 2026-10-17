# UI và UX

## Phạm vi và ownership

Tài liệu này sở hữu screens, navigation, HUD, overlays và touch layout. UI hiển thị
state do gameplay/economy/save cung cấp; không quyết damage, scoring hay persistence policy.
Mọi màn hình dùng landscape [LOCKED], kể cả menu, với bố cục icon-first, gọn và dễ đọc.
HUD/button tránh giữa combat và cần touch target đủ lớn.

## Main Menu `[LOCKED]`

Menu có các action sau; mobile không cần Quit button:

```text
TIẾP TỤC
CHƠI MỚI
NHIỆM VỤ
CÁCH CHƠI
CÀI ĐẶT
GIỚI THIỆU
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

- Relative drag và hold-to-fire.
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
│ 🚀 3       ★ 012450          ✦ 284      ⏸ │
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
TIẾP TỤC
CHƠI LẠI ĐỢT
CÀI ĐẶT
VỀ MENU CHÍNH
```

Pause simulation/time contract ở [architecture](../systems/architecture.md#pause-và-time-management-locked).
Restart Wave restore đầu checkpoint theo [save/profile](../systems/save-profile.md);
resume phải trả lại UI/audio/gameplay state phù hợp.

## Game Over

Sau khi death sequence hoàn tất, mở Game Over overlay với selected stats và hai
đường ra. Stop spawn/progression và Retry semantics do
[player combat](../gameplay/player-combat.md) quyết.

```text
THẤT BẠI
Điểm
Nhiệm vụ / Đợt
Số mạng đã mất / thống kê chọn lọc

CHƠI LẠI ĐỢT
VỀ MENU CHÍNH
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

Không hiển thị Accuracy theo [metrics owner](../gameplay/progression-economy.md).
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
Player-facing copy dùng **tiếng Việt**; proper sci-fi names (ví dụ Pulse Laser) có
thể giữ English. Technical identifiers dùng English. Font strategy shipping vẫn
[TBD]; không tự thêm localization package.


## Vocabulary và announcement

Taxonomy duy nhất ở [level owner](../content/levels-encounters.md#terminology-và-composition).
Trong bản debug hiện tại, dùng tên cấu trúc + số/ID: Region / Planet, Mission,
Encounter; các cấp composition hiện trong existing debug view. Đây là chủ ý để đọc
flow, chưa cần copy hoa mỹ. Không gộp Spawn Group với Formation, không tự đổi một
lần spawn thành Encounter mới. Technical taxonomy được giữ English trong nhãn tạm.
“NHIỆM VỤ 01” không làm hero title của Region / Planet establishing; Mission numbering
có thể là secondary metadata ở HUD/results/mission UI hoặc short Mission label.

HUD thường trực ưu tiên icon + number; text lớn chỉ dành cho moment quan trọng,
ngắn và không chặn gameplay. Mission start, clear và future danger warning dùng
presentation tách khỏi gameplay authority; timing/fade có thể tune.

## M2 UI implementation `[PROVISIONAL]`

VS-006/024–026 dùng uGUI trong `02_Gameplay`, không thêm scene hay localization
framework. Copy tập trung ở `GameplayCopy`; font runtime hiện có đủ glyph Việt.
HUD nằm trong `Screen.safeArea`, scale theo chiều cao với reference 1600 × 900;
anchors/offsets chỉnh trên RectTransform. Lives icon + count đọc `PlayerLives`;
Pulse Laser + CẤP đọc `PulseLaserWeapon.CurrentLevel`. Score và Shard là icon + **—**
(placeholder, không phải số zero hoặc economy giả); chưa có buff/boss/weapon cycle.

Establishing scene dùng lại `GameplayAnnouncement` với location/space title và một
câu mô tả ngắn, không tạo menu hoặc cinematic framework khác. `LocationName` chỉ
được dùng khi đã author; tên hành tinh/location chưa quyết vẫn để trống `[TBD]`,
không suy từ RegionId, DisplayName hoặc tự đặt fiction name. Fallback trung tính:
“Region / Planet · TBD” / “Một hành tinh hiện ra ở phía xa.”, không phải tên riêng.
Major establishing dùng layout giới thiệu lệch trái ở phần dưới; thường dành cho
vào/đổi hành tinh/khu vực lớn. Không dùng layout này cho mọi lần đổi combat segment.

Text có thời gian xuất hiện/đọc trước khi fade in lời nhắc nhỏ “Chạm để tiếp tục”.
Không nút chữ nhật BẮT ĐẦU. Tap trái chuột hoặc touch hợp lệ ở bất kỳ điểm trong
Game View tiếp tục; drag, cancel, right-click, hold dài và press bắt đầu trước lời
nhắc không được tính. Không auto-launch khi hết thời gian đọc. Toàn bộ input của
cinematic được consume; khi trả gameplay phải có press mới để move/fire. Detailed
ownership thuộc [player owner](../gameplay/player-combat.md#m2-ui-pointer-ownership).

Ẩn combat HUD/Pause ở **major cinematic**: establishing intro hiện tại; future
major boss reveal, major direction-shift cinematic và ending áp dụng cùng rule.
Không mặc định mọi transition là major cinematic. Sau continue, giữ approach /
acceleration / deceleration; camera và HUD trở về đồng thời với deceleration,
không nhịp đứng chờ riêng hoặc menu/confirmation nữa.
Khi camera đã về khung gameplay, HUD hiện đủ và điều khiển mở, mới hiện Mission card.
Mission card **căn giữa**, gồm hai dòng: “Mission 01” và “Encounter 01” nhỏ hơn ngay
bên dưới. Mission không có câu mô tả. Encounter đầu được gộp vào card này để có
thời gian đọc, không phát thêm announcement đầu tiên cạnh tranh với Mission card.
Title Mission từ Intro config, subtitle Encounter lấy từ index đang vào trong Mission;
normal start là 01, dev encounter jump phải phản ánh đúng index đã chọn.
Card giữ lâu hơn minor handoff; nhịp dừng/hiển thị theo
[audio/VFX](audio-vfx.md#level-intro-warning-và-dynamic-audio). Không trở thành hero
location title hoặc yêu cầu tap lần nữa. Pause không nhận lệnh trong
major intro; simulation vẫn chạy. HUD fade về trong Brake; hết Brake mở control rồi
hiện card trên gameplay. Người chơi có thể kéo/bắn/Pause trong nhịp đọc, nhưng first
spawn đợi card hoàn tất. Resume từ Pause hiện lại card với thời gian còn lại.

Sau khi gameplay bắt đầu, HUD luôn hiện xuyên suốt ordinary A → B / formation
transitions. Pause và gameplay permissions vẫn theo gameplay/lifecycle bình thường;
minor handoff không tự khóa input hoặc ẩn Pause. Không Continue/forced confirmation.
Minor title + một câu mô tả nằm **giữa màn hình**, dùng cùng announcement với layout
căn giữa rồi auto fade; không chung vị trí/layout với major establishing. Incoming
encounter config có thể để trống cả hai. L1 B dùng “Encounter 02” / “Tín hiệu mới đang
tiến vào khu vực.” để debug đúng cấp, chưa dùng nhãn hoa mỹ.
Minor announcement seed 1.6 s, fade .2 s `[TUNABLE]`; Pause/terminal/reset ẩn nó.
Announcement chỉ render copy, không sở hữu gameplay authority.

Pause hiện “TẠM DỪNG”; “TIẾP TỤC” hoạt động. “CHƠI LẠI ĐỢT”, “CÀI ĐẶT”,
“VỀ MENU CHÍNH” disabled, kèm “Chưa có trong bản thử nghiệm”. Slice chưa có
checkpoint restore/Settings/Main Menu backend; dev replay không được giả làm Retry.
Gameplay pause/resume và terminal authority ở
[architecture](../systems/architecture.md#m2-gameplay-session-và-ui-boundary).

Sau full L1 A → B, compact panel hiện một lần “NHIỆM VỤ HOÀN THÀNH” sau .45 s
unscaled `[TUNABLE]`. “TIẾP THEO” và “VỀ MENU CHÍNH” disabled cùng note trên;
chưa có L2/results navigation hoặc stats giả. Combat đã khóa ngay trước clear delay.
Standalone dev encounter không phát mission-clear panel.

Game Over minimal thuộc batch UI slice: sau death sequence với lives 0 hiện
“THẤT BẠI”, “CHƠI LẠI ĐỢT” và “VỀ MENU CHÍNH” disabled cùng note. Không hiển thị
selected stats chưa track. Lưu/restore checkpoint và full navigation vẫn là task sau.

Overlay chặn raycast toàn màn; HUD decoration không bắt pointer. Nút Pause/Resume
và modal có target đủ lớn theo reference layout; chưa xác nhận dp/cutout trên Android.
Input ownership chi tiết thuộc [player owner](../gameplay/player-combat.md#m2-ui-pointer-ownership).
