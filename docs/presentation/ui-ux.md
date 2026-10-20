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
│ H ▰▰▰▱▱          S 0       SS 0        ⏸ │
│                                             │
│                                             │
│                                             │
│ 🚀 ×3                                 T1 L1 │
└─────────────────────────────────────────────┘
```

| Thành phần | Cách trình bày |
|---|---|
| Lives | Icon + count, bottom-left; không dùng player HP bar |
| Score | Nhẹ trong HUD, không che combat |
| Stellar Shard | Currency riêng với score |
| Pause | Action trong HUD, touch không kéo tàu |
| Weapon | Type + level viết tắt khi chưa có icon, bottom-right; cycle giữa unlocked branches |
| Heat | Meter riêng ở top-left, ưu tiên đọc nhanh; không thu vào ô weapon |
| Buffs | Shield icon; Overdrive và Support Drone có icon + timer nhỏ, không paragraph text |
| Boss | Boss name + HP bar ở top center; phase indicator chỉ khi thực sự cần |

M3.5 HUD là functional placeholder: Lives dùng sprite hiện có + count; phần chưa
có icon thật dùng chữ viết tắt (`S` score, `SS` Stellar Shard, `H` heat) + value/state.
Không tự vẽ icon thay thế ở phase này. M4-A thay placeholder bằng
Score runtime thật và số dư Stellar Shard do CampaignLifecycle cung cấp theo
[economy owner](../gameplay/progression-economy.md#m4-a-runtime-boundary-provisional).
Số dư vàng không reset khi chuyển mission. Full weapon name không cần hiển thị
trong combat: bottom-right dùng `T1 L1` (type 1, level 1); `T1` là nhãn tạm cho
Pulse, `T2` Plasma, `T3` Homing; đây là presentation code, không phải save ID. Tên đầy đủ có
thể xuất hiện ở Pause/loadout/pickup/upgrade UI khi những consumer đó được làm.

Compact không đồng nghĩa thu nhỏ thông tin gameplay quan trọng. Heat có cụm riêng
ở top-left, nhãn `H` và notch ở recovery threshold; phải đọc được bằng
mắt liếc trong combat. Lives ở bottom-left, weapon ở bottom-right, counter ở top
center và Pause ở top-right. Placeholder dùng khối chữ nhật vuông góc, nền dark
translucent đơn giản; chưa đến phase thiết kế visual nên không cắt góc, viền accent
hoặc dựng icon trang trí. Padding bám sát nội dung, giữ cỡ chữ đọc được.

Layout dùng chung spacing scale 8, outer safe margin 8, inner padding 4, row height
44, text chính 28–32; weapon 88×44 và heat gauge 264×56, track 256×18 theo
reference 1600×900 `[TUNABLE]`. Caption/state ngắn dùng 22–24. Pause có graphic
44 nhưng hit area 56; decorative panel/text/bar không raycast. Không thêm final
art/shader/VFX framework.

Heat bar đọc trực tiếp weapon runtime: cyan ở normal, amber khi near-hot và
orange-red + cue ngắn `NÓNG` khi Overheated. Bar tiếp tục cho thấy recovery, cue không chỉ
dựa vào hue; không spam warning giữa combat hoặc thêm Continue. Warning threshold
chỉ là visual `[TUNABLE]`, không thay [weapon fire rules](../content/weapons-items-drops.md#primary-weapon-heat--m35).
Major/minor cinematic visibility vẫn theo contract phía dưới; ordinary Encounter
handoff giữ HUD visible.

M4-A counter containers tự rộng theo nội dung (`S`/`SS` + số), không giữ plate rộng
cho chữ ngắn. Weapon 88×44 là button cycle các branch đã unlock, tuân UI pointer
ownership; chỉ nhận khi gameplay gate cho control. Một hàng nhỏ bottom-center dùng
`SH`, `OD n`, `DR n` cho active effects/timer; cue pickup riêng phía trên tồn tại
1.2 s `[TUNABLE]`, Pause freeze, terminal/reset clear. Không announcement lớn ở giữa
combat, không panel riêng cho từng buff hoặc fake icon. Shield còn có teal cue quanh
Player; presentation chỉ đọc state của effect owner.

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

Phiêu lưu (Chơi mới/Tiếp tục) tự chuyển sang nhiệm vụ kế tiếp sau khi clear và lưu
thành công, dùng mission travel/region journey hiện có. Không mở panel kết quả
hoặc yêu cầu bấm Nhiệm vụ tiếp theo giữa các nhiệm vụ. Replay dừng ở kết quả của
nhiệm vụ đã chọn; Game Over và Victory vẫn có panel.

M7 current-release Victory hiển thị kết quả Mission08 (Score/Kills/Time và một ít
reward/survival counters thật), không fabricate campaign aggregation. Full target
campaign statistics như boss clear time, longest no-hit streak và ranking vẫn chưa
được consumer này sử dụng; scope tracking thuộc
[metrics owner](../gameplay/progression-economy.md#resultstatistics-tracking).

## Screen flow

Các flow dưới nói về screen/state navigation; scene topology vẫn [PROVISIONAL]
tại [architecture](../systems/architecture.md). Pause/Game Over là overlays;
region transition có thể chạy ngay trong Gameplay scene.

```text
Initial:          Boot → Main Menu
New Game:         Main Menu → Gameplay → Level 1
Continue:         Main Menu → load CampaignSave → Gameplay → level/wave checkpoint
Normal progress:  Level intro → encounters → clear/save → automatic travel → next level
Region boundary:  Boss level clear → cinematic transition → next region level
Game Over:        Gameplay → Game Over overlay → Retry Wave / Main Menu
Victory:          Nebula Warden → Victory overlay → Main Menu / Missions
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
weapon type/level đọc loadout/weapon runtime. M2 từng dùng Score/Shard placeholder;
current HUD theo [Gameplay HUD](#gameplay-hud), buff/boss scope không được suy rộng từ placeholder.

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
trước continue được consume; fresh press sau tap được move/fire ngay trong approach. Detailed
ownership thuộc [player owner](../gameplay/player-combat.md#m2-ui-pointer-ownership).

Ẩn combat HUD/Pause ở **major cinematic**: establishing intro hiện tại; future
major boss reveal, major direction-shift cinematic và ending áp dụng cùng rule.
Không mặc định mọi transition là major cinematic. Sau continue, giữ approach /
acceleration / deceleration; HUD/control mở ngay, real Player giữ pose/held input
qua cuối zoom. Khi Brake kết thúc Mission title hiện upper-center, gồm
`NHIỆM VỤ NN` và mô tả ngắn một dòng. Title là overlay: first enemy entry bắt đầu
trong lúc chữ còn hiện/fading, không chờ card hoàn tất và không yêu cầu tap nữa.
Pause không nhận lệnh trong major cinematic; simulation vẫn chạy. Trên gameplay,
Pause dừng scaled title/entry clocks; Resume cần thao tác mới theo input owner.

Ordinary Encounter/Mission handoff giữ HUD, relative drag, hold-to-fire và Pause;
không Continue/forced confirmation. Player-facing Encounter là `ĐỢT N · TÊN NGẮN`,
subtitle ngắn một dòng; Boss dùng `CẢNH BÁO` / `VANGUARD FRIGATE`. Internal IDs vẫn
English và không xuất hiện trong HUD. Fixture lịch sử chỉ dùng cho DevTest.

Mission title và Đợt dùng hai announcement instances: Mission upper-center lớn/bold,
Đợt nhỏ hơn ở phía trên; không có modal/raycast blocker hoặc card nền. Typography
seeds `[TUNABLE]`: Mission 42/bold + subtitle 22, Đợt 25/normal + subtitle 18.
Visible duration/entry lead độc lập theo [level owner](../content/levels-encounters.md#m5-region-a-authored-content-provisional).
Fallback announcement không truyền duration là 1.6 s, fade .2 s `[TUNABLE]`.
Announcement chỉ render copy, không sở hữu gameplay authority; atomic Mission launch
không được hide title chỉ vì outgoing run vừa Idle. Pause tạm ẩn cue và giữ remaining time để Resume; terminal/reset hủy cue.

Pause hiện “TẠM DỪNG”; “TIẾP TỤC” hoạt động. M4-B nối “CHƠI LẠI ĐỢT” tới
current Encounter checkpoint khi backend báo CanRetry. “CÀI ĐẶT”, “VỀ MENU CHÍNH”
vẫn disabled, kèm “Chưa có trong bản thử nghiệm”. Settings chỉ có persistence backend;
chưa Main Menu. Restore policy thuộc [save owner](../systems/save-profile.md); dev replay không là Retry.
Gameplay pause/resume và terminal authority ở
[architecture](../systems/architecture.md#m2-gameplay-session-và-ui-boundary).

Sau full Mission 01 (current testbed 5 Encounters), compact panel hiện một lần “NHIỆM VỤ HOÀN THÀNH” sau .45 s
unscaled `[TUNABLE]`. “TIẾP THEO” và “VỀ MENU CHÍNH” disabled cùng note trên;
chưa có L2/results navigation hoặc stats giả. Combat đã khóa ngay trước clear delay.
Standalone dev encounter không phát mission-clear panel.

Game Over minimal thuộc batch UI slice: sau death sequence với lives 0 hiện
“THẤT BẠI”; M4-B bật “CHƠI LẠI ĐỢT” khi có checkpoint, “VỀ MENU CHÍNH” vẫn
disabled cùng note. Không hiển thị selected stats chưa track. Full navigation chưa làm.

Overlay chặn raycast toàn màn; HUD decoration không bắt pointer. Nút Pause/Resume
và modal có target đủ lớn theo reference layout; chưa xác nhận dp/cutout trên Android.
Input ownership chi tiết thuộc [player owner](../gameplay/player-combat.md#m2-ui-pointer-ownership).


## Reserve missile HUD prototype

CombatHud dùng ký hiệu tạm `M current/cap` (sau này thay icon), nằm cao hơn
weapon-cycle với đáy nút tại y96 reference pixels, size 96×48 `[TUNABLE]`. Caption nhỏ giữa hai nút
hiện T1/T2/T3 đã unlock, bracket active branch, `-` cho locked và `+N` số loại khác.
Caption không raycast; các nút vẫn consume pointer riêng. Disable khi stock zero, cooldown, Pause, cinematic, dying/respawning,
terminal hoặc không ở Running/ExitDrain Encounter. Nút chỉ gửi intent tới reserve owner;
UI không trừ stock/damage hoặc lưu save. Pointer consumption dùng existing UI input
boundary để bấm nút không khởi động drag/fire. Không thêm inventory/menu scene.


## M5 Boss HUD `[PROVISIONAL]`

BossHud được author dưới GameplayUI/SafeArea: compact centered name và HP bar dưới
counters, không giant card hoặc common-enemy HP bar. Chỉ hiện khi Boss Encounter đang
Running và Boss sống; ẩn ngay defeat/reset/exit. Vanguard Phase B dùng `HỎA LỰC TĂNG CƯỜNG` để cue
không chỉ phụ thuộc màu. Proper name Vanguard Frigate giữ English/provisional.
Mission production dùng `NHIỆM VỤ 01`–`NHIỆM VỤ 04`; không expose internal IDs.
Region-ready giữ gameplay theo hướng Right và gesture đang held sau approach;
Mission05–08 đã nối theo [level owner](../content/levels-encounters.md#m6-region-b-authored-content-provisional), chưa thêm menu mới.


M5 second feel pass giữ Boss HUD tại SafeArea top center, đưa track/name cao hơn để
không chồng engine silhouette ở camera 9. Đây là placeholder composition; không thêm
common-enemy HP bars. Beam warning/active khác nhau bằng full lane tint và bright core,
không chỉ đổi màu. Exact combat geometry ở [enemy owner](../content/enemies-bosses.md#boss-1--vanguard-frigate-provisional).

M6 readability refinement: khi Warden còn shield, bar xanh cyan hiển thị tổng HP
node hiện tại/tổng HP node ban đầu, kèm số node còn lại. Khi zero nodes đang collapse
hiện `KHIÊN ĐANG VỠ`; exposed mới chuyển bar sang HP core và `LÕI LỘ DIỆN`. Không
để người chơi bắn node mà bar core đứng yên gây hiểu nhầm không có damage.
Thông số HP/timing ở enemy owner; HUD chỉ đọc state, không quyết vulnerability.

Directed threat warning phân biệt tracking/locked bằng độ dày, độ sáng và việc
line ngừng bám, không chỉ màu. Dash line chỉ tới aim snapshot; beam/sweep warning
đúng hướng và diện tích tác động. Những chỉnh sửa này cải thiện luồng đang có,
không thêm screen/settings/progression mới.


## M7 functional shell `[PROVISIONAL]`

Production boot ở cùng Gameplay scene và vào Menu trước mọi gameplay. Main Menu có
các action chiến dịch cùng Xưởng/Cửa hàng; no save làm Continue disabled và giải thích cần New Game.
Valid Active/region checkpoint cho Continue đúng semantic state; completed save mở
Victory, không spawn Warden hoặc grant reward lại. Unreadable/unsupported save giữ
nguyên bytes, tắt actions cần data và có modal thử đọc lại; backup recovery có notice
rằng tiến độ gần nhất có thể thiếu. Có explicit recovery có confirmation cho Invalid; không overwrite unsupported version.
Preserve/archive policy thuộc [save owner](../systems/save-profile.md#m7-profile-cosmetics-và-explicit-recovery-provisional).

`GameShell` dùng explicit `ShellScreen`: Playing, Menu, Missions, Settings, Help,
Credits, Pause, Results, Hangar, Store. Confirmation/Error/Loading là modal trên screen hiện tại.
`ShellView` giữ refs tới các authored panels trong `GameShellUI`; `ShellCopy` tập
trung copy tách navigation. Các shape/font hiện là placeholder để thay skin sau.
Menu có starfield nhẹ chạy unscaled; không chạy gameplay simulation phía sau menu.

Mission Hub có hai hàng Region A/B, đúng tám cards từ RegionCampaignFlow catalog.
Unlock/current lấy Campaign/Profile; record existence là cleared evidence. Card có
text locked/unlocked/current/cleared; selection dùng viền sáng, best score/time khi có. Không tạo
unlock database khác. Chọn card mở Replay với preset hiện có; replay result không
có Next Mission, cho replay lại hoặc về Missions/Menu. Exit Replay rồi Continue vẫn
đọc campaign gốc.

Pause có Resume, Retry checkpoint, Settings, Main Menu. Retry từ Pause và abandon
mission cần confirmation; cancel giữ Pause. New Game có campaign cần confirmation
và giữ profile/settings. Harmless Back không hỏi. Settings có chung bốn controls,
hiển thị saved values và nút Lưu và quay lại; Back bỏ draft và về đúng Menu/Pause.
Settings toggles hiển thị Bật/Tắt ngay khi chạm, tách rung thiết bị và rung màn hình.
How to Play chia bốn khối chức năng: điều khiển, vũ khí/nhiệt, sống sót/tiếp tế và tạm dừng.
Credits chỉ nêu Unity và trạng thái release/placeholder đã biết.

Result contexts phân biệt Game Over, Replay clear, current-release Victory và lỗi
lưu khi clear Campaign. Campaign clear thành công không mở Results. Mọi result đọc
frozen `RunRewards` cho Score/Kills/Time; thêm Shards collected,
Cores và Lives Lost. Best lấy Profile records; New Best chỉ hiện sau transaction clear
thành công. Clear save failure giữ kết quả và cho Thử lưu lại; sau khi lưu thành
công tự chạy tiếp hành trình, không cần nút Next. Menu/Back
không bỏ qua failed save. Game Over có semantic Retry và Menu; Victory có Menu và
Missions, không có Next/L9. Completed Continue đọc Mission08 checkpoint stats.

Clear Campaign tự dùng mission travel/region journey M6; same-region incoming entry
đã commit theo persistence boundary. Không thêm confirmation hoặc Results bắt buộc.
Major region journey vẫn có establishing tap; screen overlay không thay choreography.

Back/Escape: submenu → Menu, Settings → origin, confirmation → cancel, error → screen
trước, gameplay → Pause, Pause → Resume, Result → Menu (Replay → Missions). Important
panels đặt selected control cho keyboard. All modal backgrounds raycast và chặn
underlying UI; navigation/transition cancel pointer qua existing adapter. Loading
fade ngắn dùng unscaled time, giữ blocker qua release frame và chống double activation.
Trong toàn bộ loading, GameplaySession giữ clock/audio frozen kể cả khi RunStarted
reset session; nhả hold sau blocker và giữ Pause nếu app mất focus. Không thêm input/time ownership thứ hai.

SafeAreaLayout được reuse; shell scaler theo height 1600×900, anchors responsive.
16:9 và 2000×900 là functional QA targets; touch controls lớn, labels không bake
vào sprites. Final art/font/pixel spec và thiết bị Android thật không thuộc M7.


### Main Menu, Xưởng và Cửa hàng

Menu bỏ build/content-count labels và filler không có action. Tàu đang trang bị,
tên tàu, equipped state, wallet và dynamic current Khu vực/Nhiệm vụ hoặc completion
là trọng tâm trái. Preview không phải button; hai action XƯỞNG và CỬA HÀNG có vai trò
riêng, không thêm lối Đổi tàu trùng action.

Menu/Xưởng/Cửa hàng dùng một authored ShipStage với cùng position/scale và vùng
trống quanh tàu. Stage không nhận input; shell ẩn stage khi vào gameplay/intro.
Đây là điểm đặt presentation để art phase sau animate tàu vào gameplay, chưa implement
cinematic hoặc đổi choreography M6. Shell dùng tiếng Việt nhất quán; tên riêng như
STARSHIFT/Pulse/Core/Stellar Shard được giữ. HUD dùng tên vũ khí và CẤP, Nhiệt,
Tên lửa, Khiên/Cường hóa/Hỗ trợ thay mã debug.

Xưởng chỉ liệt kê cosmetic đã sở hữu/default để trang bị miễn phí. Cửa hàng là điểm
mua nội dung mới, có mode navigation riêng nhưng reuse panel/catalog hiện có.
Tàu là lựa chọn chính; Sơn tàu/Vệt động cơ nằm dưới ngữ cảnh tàu đang trang bị và
preview trực tiếp trên tàu đó. Selection dùng viền, equipped có dấu check cùng một
state line; không xếp thêm dòng Đang chọn. Catalog vẫn chỉ có ba slots, không thêm
shop/inventory framework; nhóm hàng tương lai có thể mở rộng Cửa hàng mà không đổi
luồng quản lý đồ đã sở hữu của Xưởng.

Preview không đổi Profile. Mua & trang bị có confirmation đúng tên/giá từ catalog;
owned trong Store không bị tính tiền/trang bị lại. Insufficient funds/equipped state
được disable có label rõ. Save error có retry, giữ wallet/equip đã commit. Back về
Menu reset preview theo Profile. Cosmetics chỉ ngoại hình, giao dịch/giá thuộc
[economy owner](../gameplay/progression-economy.md#m7-cosmetic-hangar-provisional).
`GameShellUI.prefab` là authored reskin surface.
