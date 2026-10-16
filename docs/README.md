# Documentation router

## Documentation philosophy

Luồng đọc và source of truth là [AGENTS.md](../AGENTS.md) → tài liệu này → canonical
docs liên quan. Một detailed fact có một canonical owner. File khác chỉ tóm tắt
đủ ngữ cảnh rồi link tương đối đến owner; không copy bảng/contract chi tiết.
Docs mô tả target design, không chứng minh feature đã được implement.

## Status markers

| Marker | Ý nghĩa |
|---|---|
| `[LOCKED]` | Đã chốt; không tự thay vì thấy phương án khác hợp lý hơn |
| `[TUNABLE]` | Baseline/seed để cân bằng sau test; đổi có chủ ý phải cập nhật owner |
| `[TBD]` | Chưa quyết; giữ câu hỏi mở, không tự bịa để hoàn thiện file |
| `[PROVISIONAL]` | Đề xuất đủ để prototype, chưa khóa |
| `[FUTURE]` | Ngoài requirement hiện tại, không tự implement |

Marker gắn với từng quyết định trong canonical owner; không tự nâng đề xuất thành
locked decision. Số balance/timing không có nghĩa final; thiếu seed vẫn phải nói rõ
chưa quyết.

MUST là bắt buộc; SHOULD là khuyến nghị, chỉ bỏ khi có lý do rõ; MAY là tùy chọn.
Chỉ dùng các từ khóa này ở nơi cần phân biệt mức yêu cầu.

## Canonical ownership map

| Chủ đề | Owner |
|---|---|
| Vision, core loop, scope, pillars, campaign high-level | [Game design](design/game-design.md) |
| Relative drag, hold-to-fire, lives, damage/death/respawn, downgrade | [Player combat](gameplay/player-combat.md) |
| Score, Stellar Shard, unlock/progression, Mission Replay | [Progression/economy](gameplay/progression-economy.md) |
| 12 levels, exact encounters, paths/formations, FlightSequence usage, camera, directions/transitions | [Levels/encounters](content/levels-encounters.md) |
| Archetypes, HP, behavior, damage states, Elite, bosses | [Enemies/bosses](content/enemies-bosses.md) |
| Weapons, pickups/core, asteroids reward, supply, drops/Director/pity | [Weapons/items/drops](content/weapons-items-drops.md) |
| Unity runtime/config boundaries, scene model, pooling, pause/time | [Architecture](systems/architecture.md) |
| CampaignSave, PlayerProfile, Settings, replay/dev persistence isolation, schema | [Save/profile](systems/save-profile.md) |
| DevConfig/debug controls, fast testing, verification | [Dev/debug](systems/dev-debug.md) |
| Screens/navigation, HUD, pause/game-over/results/settings UX | [UI/UX](presentation/ui-ux.md) |
| Visual identity, palette/color semantics, silhouettes, region look | [Art direction](presentation/art-direction.md) |
| Feedback, particles, animation, cinematic/audio choreography, shake, haptic | [Audio/VFX](presentation/audio-vfx.md) |
| Asset generation/import/slicing/naming/licensing/tooling | [Asset pipeline](production/asset-pipeline.md) |

## Folder responsibilities

- `design/`: định hướng và scope, không chứa full content tables.
- `gameplay/`: player rules và progression/economy policy.
- `content/`: authored levels, enemies/bosses và weapon/item/reward content.
- `systems/`: cách tổ chức Unity, persistence contracts và dev access.
- `presentation/`: UI, visual identity và feedback; không quyết gameplay damage.
- `production/`: cách tạo/import/reproduce asset, không thay art direction.

Unity code/assets thuộc `../app/`; offline utilities thuộc `../tools/`.

## Task routing

| Task | Đọc trước | Đọc thêm khi cần |
|---|---|---|
| Sửa player death | Player combat | Save/profile, audio/VFX |
| Sửa relative drag | Player combat | UI/UX, architecture |
| Sửa L7/Timeline/path | Levels/encounters | Enemies/bosses, architecture |
| Sửa Scout HP | Enemies/bosses | Weapons/items/drops khi cân time-to-kill |
| Sửa Missile Lv3 | Weapons/items/drops | Enemies/bosses, audio/VFX |
| Sửa score/Shard/replay reward | Progression/economy | Save/profile, weapons/items/drops |
| Sửa CampaignSave | Save/profile | Player combat, progression/economy |
| Sửa HUD | UI/UX | Owner của state được hiển thị |
| Sửa palette | Art direction | Audio/VFX cho contrast |
| Generate/import visual asset | Art direction, asset pipeline | Audio/VFX cho animation/feedback |
| Test nhanh boss/transition | Dev/debug | Levels/encounters, enemies/bosses, audio/VFX |
| Sửa pooling/scene model | Architecture | Domain liên quan |

Tên trong bảng trỏ tới canonical owner ở ownership map; không cần đọc toàn bộ docs
cho một thay đổi chỉ thuộc một domain. Bắt đầu mỗi task bằng `../AGENTS.md`.

## Cross-link và change policy

Dùng relative Markdown links; section anchors là semantic headings, không manual
numbering. Canonical filenames lowercase kebab-case; `README.md` là ngoại lệ.
Tóm tắt được lặp ở mức high-level để file tự đọc được, nhưng bảng HP chỉ ở enemy
owner, score seed chỉ ở economy owner, drop weights chỉ ở reward owner.
Thay đổi gameplay/architecture/save contract có chủ ý phải sửa owner cùng task;
implementation bug phải sửa code theo contract, không sửa docs để hợp thức hóa bug.

## Conflict resolution

Yêu cầu hiện tại có ưu tiên cao nhất. Khi docs mâu thuẫn, canonical owner trong map
thắng summary ở file khác; sửa stale summary/link. Nếu ownership hoặc contract còn
mâu thuẫn ảnh hưởng quyết định, ghi conflict và giữ câu hỏi mở, không âm thầm chọn.
Version/package là observation từ project thực tế; không đoán hoặc sửa thiết kế
để hợp thức hóa implementation bug.
External docs chỉ xác minh API/technical facts, không override gameplay design.

## Split-doc rule

Giữ đúng bộ file này ở initialization. Chỉ split khi file thực sự quá dài/khó review
và có nhu cầu độc lập rõ; không chia trước vì có thể cần sau này. Khi split phải chuyển
fact sang một owner duy nhất, cập nhật map/routing và sửa mọi relative link cùng task.
