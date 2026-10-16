# Progression và economy

## Phạm vi và ownership

Tài liệu này sở hữu score, Stellar Shard, progression/unlocks và Mission Replay policy.
[Weapons/items/drops](../content/weapons-items-drops.md) quyết reward selection;
[save/profile](../systems/save-profile.md) quyết persisted fields và disk writes.
UI chỉ trình bày các giá trị domain cung cấp.

## Stellar Shard `[LOCKED]`

**Stellar Shard** là persistent resource trong `PlayerProfile`; balance còn nguyên
khi đóng và mở lại app. Scope hiện tại là collect, display, persist và đưa vào
result/statistics. Visual identity nằm tại [art direction](../presentation/art-direction.md).

Nguồn gồm golden/resource asteroid với Shard guaranteed, normal enemy có shard drop
thấp và supply/major reward khi config chọn loại phù hợp. Amount/rate vẫn [TUNABLE],
chưa có bảng số cố định; pickup/drop contract ở
[reward owner](../content/weapons-items-drops.md).

### Shop `[FUTURE]`

Shop, hangar, cosmetic hoặc permanent progression có thể dùng Shard sau này.
Hiện chưa có shop requirement và không triển khai các tính năng đó chỉ vì đã lưu currency.

## Score `[LOCKED]`

Score là performance metric riêng, không phải currency và không dùng cùng giá trị
hoặc display semantics với Shard. Score có thể đến từ kill, Elite, boss, optional
target, clear bonus và no-hit bonus nếu sau này cân bằng hợp lý.

### Score seed

Các giá trị dưới là [PROVISIONAL], có thể cân sau playtest. Boss/supply/golden
asteroid mới có định hướng, chưa có exact bonus; không tự gán số để hoàn tất bảng.

| Target | Score seed |
|---|---:|
| Scout | 100 |
| Raider | 150 |
| Bomber | 250 |
| Sniper | 250 |
| Interceptor | 250 |
| Carrier | 500 |
| Elite | ×2 base score |
| Boss | Large fixed bonus; exact amount [TBD] |
| Supply target | Bonus score; exact amount [TBD] |
| Golden asteroid | Small score + Shard; exact amount [TBD] |

Exact score formula và clear/no-hit bonus vẫn [TBD]. Không coi seed là final balance.

## Campaign progression và level unlock

New Game bắt đầu campaign ở L1; clear level dẫn sang level kế tiếp. Boss boundary
đi qua transition, còn final victory đánh dấu campaign completion. `PlayerProfile`
theo dõi levelUnlocks, bestScorePerLevel, bestTimePerLevel, highScore và campaignCompleted;
shape persistence ở [save/profile](../systems/save-profile.md).

MISSIONS chỉ cho replay level đã unlock/clear. Exact điều kiện unlock, thời điểm
cập nhật record và eligibility của failed/retried run vẫn [TBD]; không tự mở toàn bộ
level trong production. Score/best score và các performance metrics cần xuất hiện
ở result theo [UI/UX](../presentation/ui-ux.md).

## Mission Replay

Replay cho chơi lại content đã mở mà không thay checkpoint campaign. Ví dụ campaign
đang L8 W2, người chơi replay L3 rồi thoát; Continue vẫn trở về L8 W2 [LOCKED].
Isolation serialization thuộc [save/profile](../systems/save-profile.md).

### Loadout

[PROVISIONAL] Replay dùng preset phù hợp level, tránh late-game max loadout nghiền
L1. Exact preset từng level còn [TBD] tại
[levels/encounters](../content/levels-encounters.md#mission-replay-presets).

### Rewards

Persistent Shard từ replay vẫn [TBD]. Đề xuất hiện tại là ghi best score/best time
nhưng không cộng persistent Shard để tránh farm dễ nếu shop mở sau. Đây là đề xuất,
không phải locked policy; serializer hoặc UI không được tự quyết thay economy.

## New Game và Retry

New Game chỉ reset current campaign, giữ Profile/Shard/high score và Settings.
Nếu campaign đang tồn tại, [UI/UX](../presentation/ui-ux.md) yêu cầu confirmation.
Retry restore checkpoint campaign; nó không tự áp rollback cho persistent profile.
Reward deduplication/transaction qua Retry vẫn là câu hỏi ở
[save/profile](../systems/save-profile.md#các-contract-còn-mở).
Dev override cũng không được mutate production data nếu thiếu explicit debug save command.

## Result/statistics tracking

Tracking phục vụ result hoặc đánh giá gameplay, không thu thập metric chỉ để có số.
Accuracy vẫn bị loại khỏi metrics; input/fire contract ở
[player combat](player-combat.md#hold-to-fire-continuous-shooting-locked). Bảng dưới phân
biệt run baseline, tracking bắt buộc cho Campaign Result và phần tùy chọn;
cách hiển thị thuộc [UI/UX](../presentation/ui-ux.md).

| Mức tracking | Metrics |
|---|---|
| Must-have run stats | Score; level; elapsed play time; enemies destroyed; elites destroyed; bosses defeated; lives lost; Shards collected; supply drones captured; weapon cores collected |
| Required tracking khi triển khai Campaign Result | Boss clear time; longest no-hit streak |
| Optional tracking | Per-level clear time; retries; shield breaks; EMP uses |

Boss clear time và longest no-hit streak phải được track khi Campaign Result phụ
thuộc chúng, không thể bỏ tracking rồi dùng số giả hoặc bỏ field trên result.
Clarification này không thêm metric, công thức hoặc timing contract mới.

Exact no-hit streak/time accounting, Supply Drones Captured được tính khi destroy
hay pickup, retry record aggregation và `campaignRunStats` checkpoint shape còn
[TBD]. Grade S/A/B/C thuộc [UI/UX](../presentation/ui-ux.md) và chưa có rubric được chốt.
