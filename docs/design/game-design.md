# StarShift — Game design

## Phạm vi và ownership

Tài liệu này sở hữu vision, core loop, scope, design pillars và campaign high-level.
Các contract chi tiết được phân về owner trong [documentation router](../README.md).
Đây là thiết kế cần triển khai; danh sách feature không có nghĩa gameplay đã tồn tại trong Unity.

## Game identity

**StarShift** là tên làm việc [LOCKED] cho dự án 2D cinematic arcade space shooter.
Tên marketing có thể đổi sau, nhưng tài liệu và asset namespace hiện dùng StarShift
để giữ nhất quán. Game ưu tiên mobile, Android-first và landscape [LOCKED]; touch là
input chính, mouse/touch simulation được khuyến khích cho Editor và dev.

Người chơi lái tàu qua những chiến trường có hướng, quy mô và nhịp độ khác nhau,
đọc telegraph, né đạn, chọn vũ khí và săn reward giữa combat. Sự thay đổi này đến từ
camera, path, formation và encounter composition, không chỉ từ số enemy trên màn hình.

Campaign hướng tới 20–30 phút [TUNABLE], không ép các level có cùng thời lượng.
Bảng thời lượng từng level và camera profile thuộc [levels/encounters](../content/levels-encounters.md).

## Experience pillars

### Readable arcade combat

Friendly projectile, hostile projectile, shield, reward, hazard và telegraph phải
phân biệt được trên màn hình mobile. Difficulty đến từ path, timing, crossfire,
telegraph kết hợp reposition và target priority; tăng density đơn thuần không thay
cho encounter design. HUD gọn và touch không che tàu là một phần của readability.

### Cinematic movement và directional variety

Entry, boss intro, warp, orbit turn, respawn và region transition cần chuyển tiếp
mềm để state và không gian được đọc rõ. Hướng chiến đấu Up → Right → Up và dynamic
shift của final boss là điểm nhận diện; không đổi physical device orientation để
đổi hướng gameplay. Camera close tạo pressure, wide mở formation và quy mô,
còn dynamic framing nhấn boss phase. Không đổi ship scale để giả zoom.

### Reusable content và controlled scope

Một roster nhỏ được dùng với nhiều path, formation và fire sequence. Không phải
encounter nào cũng yêu cầu giết sạch enemy; optional supply tạo những lúc cân nhắc
risk/reward mà không chặn progress. Ưu tiên polish core systems thay cho mở rộng
feature. Feedback cần rõ và đủ mềm, không cần animation AAA.

## Core loop

```text
Main Menu → New Game / Continue → level intro
→ encounters + optional events → compact Level Clear
→ checkpoint / progression → next level
```

Boss ở biên region nối với cinematic transition; final victory dẫn Ending rồi
Campaign Result. Khi mất life, player đi qua death/respawn hoặc Game Over;
Retry trở về đầu checkpoint theo [player combat](../gameplay/player-combat.md)
và [save contract](../systems/save-profile.md).

## Campaign và region overview

[LOCKED] Campaign gồm 12 level, 3 region và 3 boss tại L4/L8/L12. Nhịp combat chính
là Up → Right → Up; L12 dùng dynamic Direction Shift. Tên region, level và boss
là [PROVISIONAL], chưa phải tên marketing cuối cùng.

| Region | Vai trò trong campaign |
|---|---|
| Astra Frontier | Dạy core mechanics trong chiến trường dễ đọc |
| Crimson Nebula | Đưa horizontal combat, crossfire và target priority vào nhịp chơi |
| Titan Ring / Rift Approach | Kết hợp các cơ chế ở quy mô lớn hơn trước final boss |

[Levels/encounters](../content/levels-encounters.md) sở hữu exact composition,
paths, direction/camera profiles và transition handoff. Region palette thuộc
[art direction](../presentation/art-direction.md).

## Reward và death philosophy

Player dùng lives, enemy dùng HP. Death có penalty nhưng respawn phải đủ công bằng
để player reposition; rule damage, shield, downgrade và Retry nằm tại
[player combat](../gameplay/player-combat.md).

Reward cần hiếm và dễ nhận ra. Supply/resource moments, low direct major drop,
Drop Director và pity được định nghĩa ở [weapons/items/drops](../content/weapons-items-drops.md).
Score phản ánh performance; Stellar Shard là persistent resource riêng theo
[progression/economy](../gameplay/progression-economy.md).

## Presentation và replay philosophy

Visual target là stylized hard-surface sci-fi, không cute/chubby/toy-like.
Readability đứng trước hiệu ứng dày; state quan trọng cần cue chuyển tiếp ngắn,
không chuyển cứng nếu animation đơn giản có thể giúp người chơi hiểu hơn.
Chi tiết ở [art direction](../presentation/art-direction.md),
[audio/VFX](../presentation/audio-vfx.md) và [UI/UX](../presentation/ui-ux.md).

Mission Replay cho chơi lại level đã mở mà không ghi đè campaign. Loadout preset
phù hợp từng level là [PROVISIONAL]; reward policy còn mở theo
[progression/economy](../gameplay/progression-economy.md).

## Required scope và phạm vi học phần

Game phát triển từ nền Space Explorer: movement, shooting, collision, asteroid,
collectible, scoring, menu, gameplay, end-game, scene transitions và documentation.
StarShift phải thể hiện rõ bước phát triển tiếp bằng life/death/respawn, data-driven
encounters, ba weapon branches × ba levels, boss/region transitions, save/profile,
Mission Replay và DevConfig.

Unity GameObject/Component, Transform, Physics 2D, MonoBehaviour lifecycle, scene
management, timed sequences, UI, Sprite/Animator/Particle animation, audio và
persistence được dùng theo nhu cầu thực tế. Không copy architecture LibGDX cũ;
component boundaries và cách dùng các cơ chế Unity nằm ở [architecture](../systems/architecture.md).

## Nice-to-have

Mouse/touch simulation cho Editor, optional result metrics/New Record flags và
Elite fly-by có thể hỗ trợ trải nghiệm. Chỉ thêm encounter khi pacing cần;
không tự nâng các lựa chọn này thành requirement.

## Locked và tunable decisions

[LOCKED] Các invariant chính là landscape mobile, relative drag, auto-fire,
life-based player/enemy HP, free switching giữa unlocked weapons, data-driven
campaign, wave-start checkpoint và replay/dev isolation. Owner từng domain giữ
contract chi tiết; bản tóm tắt này không thay thế chúng.

[TUNABLE] Balance, timing, formation count, camera values và campaign duration
được cân qua playtest. [TBD] là câu hỏi còn mở, không phải chỗ trống để agent tự điền.
Các giá trị [PROVISIONAL] vẫn là đề xuất, kể cả khi đã đủ để prototype.

## Explicit out-of-scope và future ideas

Các nhóm sau nằm ngoài scope hiện tại:

- Multiplayer/networking, cloud save, online leaderboard, backend và procedural galaxy.
- Endless mode bắt buộc, complex inventory, crafting, talent tree và gacha.
- ECS/DOTS, third-party DI, custom visual scripting hoặc custom level editor trước khi data model chứng minh nhu cầu.
- Shop, ad SDK và analytics SDK.

[FUTURE] Shop, hangar, cosmetic và permanent progression bằng Shard; online
leaderboard cũng chỉ là ý tưởng tương lai, không phải requirement để triển khai.

## Trình tự phát triển

Repository/docs và Unity setup đi trước vertical slice L1. Slice này cần input,
ship/auto-fire, enemy/collision, life/death/respawn, một weapon, một encounter, HUD
và pause. Sau đó mới mở rộng data-driven encounters, FlightSequence/path,
pooling và DevConfig; tiếp đến weapons/pickups/supply, Shard, save/profile và replay.

Region A/Boss 1/Transition 1 được hoàn thiện trước Region B/C, horizontal combat,
các boss còn lại và final transition. Audio/VFX, art consistency, result stats,
mobile profiling và tài liệu/hình báo cáo là giai đoạn polish; không làm polish
diện rộng trước khi vertical slice và encounter model ổn định.
