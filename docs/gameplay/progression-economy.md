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
| Bulwark | 180 `[TUNABLE]` |
| Elite | ×2 base score |
| Boss | Large fixed bonus; exact amount [TBD] |
| Supply target | Bonus score; exact amount [TBD] |
| Golden asteroid | Small score + Shard; exact amount [TBD] |

Exact score formula và clear/no-hit bonus vẫn [TBD]. Không coi seed là final balance.

## M4-A runtime boundary `[PROVISIONAL]`

`RunRewards` là authority của mission/runtime Score và Shards collected, độc lập HUD.
Kill hook từ EncounterRunner chỉ award một lần khi HP về zero; despawn/exit/cleanup
không là kill. Scout dùng score seed ở bảng trên. Resource/supply score tạm 0 vì exact
bonus vẫn TBD; không tự gán clear/no-hit/boss bonus. Shard tăng khi pickup được collect,
không tăng khi enemy chết/eject reward.

RunStarted reset hai counter; death/handoff giữ chúng. Clear/Game Over freeze counters
cho presentation hiện tại. Có runtime counts enemies destroyed, cores collected và
supply opportunities collected (nhặt ít nhất một item; một source vẫn chỉ tính một) để verify loop; chưa là full Campaign Result tracking.
HUD đọc giá trị thực theo [UI owner](../presentation/ui-ux.md#gameplay-hud).
M4-A ban đầu chỉ có runtime counters. M4-B persist checkpoint counters và tách
Profile balance theo [save owner](../systems/save-profile.md). HUD currency dùng
balance từ `CampaignLifecycle`: Campaign đọc Profile wallet, giữ qua mission,
region, Retry/Continue và New Game. Replay/DevTest dùng ví tạm riêng, giữ qua
mission và Retry trong cùng lượt; lượt replay/dev mới reset ví tạm và không ghi
Profile. `RunRewards.Shards` vẫn là mission-local collected metric cho stats/ledger.

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
M4-B test policy cho reward deduplication ở phần bên dưới; final policy vẫn [TBD].
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
| Optional tracking | Per-level clear time; retries; shield breaks; reserve missile uses |

Boss clear time và longest no-hit streak phải được track khi Campaign Result phụ
thuộc chúng, không thể bỏ tracking rồi dùng số giả hoặc bỏ field trên result.
Clarification này không thêm metric, công thức hoặc timing contract mới.

Exact no-hit streak/time accounting, Supply Drones Captured được tính khi destroy
hay pickup, retry record aggregation và `campaignRunStats` checkpoint shape còn
[TBD]. Grade S/A/B/C thuộc [UI/UX](../presentation/ui-ux.md) và chưa có rubric được chốt.


## M4-B reward và record policy `[PROVISIONAL]`

Profile Shard không rollback theo Retry. Để chống duplication mà không tự reset
wallet, M4-B giữ mức **cumulative Shards collected cao nhất trong current Encounter**.
Mỗi collection chỉ cộng Profile phần vượt mức đã credit. Ví dụ checkpoint Shards 4:
collect thêm 3 → wallet +3; Retry restore counter về 4; collect lại 3 → wallet +0;
collect tổng 5 trong attempt mới → wallet thêm 2. Continue giữ cùng ledger qua disk.
Capture Encounter tiếp reset ledger với baseline counter mới; New Game tạo campaign
ledger mới nhưng giữ Profile. Đây là policy prototype được phép bởi brief M4-B,
không lock final economy hoặc guarantee identical random drops giữa các attempt.
Score và run stats rollback; Profile/ledger không rollback. Không có clear bonus giả.

Current Mission Clear lưu best score và minimum playable time trong Campaign mode;
retried campaign hiện vẫn eligible theo test policy. Failed run không ghi record.
Playable timer và checkpoint serialization thuộc [save owner](../systems/save-profile.md).
Exact final retry eligibility/time aggregation vẫn [TBD]. Chỉ L1 được unlock bởi
New Game; clear L1 không fabricate Mission 02 hoặc đánh dấu toàn campaign complete.

Replay M4-B dùng hoàn toàn isolated runtime, không ghi records hay wallet. Đây là
boundary test theo brief, không chốt SYS-028; persistent replay Shards và final
record writeback vẫn cần User decision. Dev/Test cũng không writeback.


## M5 campaign progression `[PROVISIONAL]`

Clear Mission 01–03 cập nhật record và unlock Mission kế tiếp, đồng thời đặt checkpoint
entry của Mission đó trong cùng transaction. Lives/weapon levels/reserve được carry;
mission-local stats reset cho Mission mới. Elite dùng ×2 base score. Vanguard runtime
seed 2500 `[TUNABLE]` để thử clear flow; final boss bonus formula vẫn `[TBD]`.
Replay dùng [draft presets](../content/levels-encounters.md#mission-replay-presets)
và không chain sang Mission kế; giữ nguyên isolation, chưa chốt replay economy.


M5 second human-feel pass thêm [Encounter reward floor](../content/weapons-items-drops.md#encounter-reward-floor-provisional)
`[PROVISIONAL]` để tạo nhiều progression opportunities; không đổi Score/Shard formula,
weapon persistence hoặc anti-farm high-water. Floor là world pickup, không permanent
credit lúc enemy chết. Retry rollback weapon level; Replay/DevTest không write Profile.
Không coi automated upgrade/drop counts thành final economy hoặc manual balance approval.


## M6 release progression `[PROVISIONAL]`

Sniper / Interceptor / Carrier dùng score seeds 250 / 250 / 500 từ bảng trên.
Nebula Warden dùng fixed defeat score 4000 `[TUNABLE]`; modules/minions không score
hoặc loot. Không thêm clear/no-hit bonus hoặc đổi Shard high-water semantics.

Catalog current release kết thúc L8. Clear L4 unlock L5 sau journey; L5–L7 unlock
mission tiếp theo, L8 commit current-campaign victory và không unlock L9. Đây là
release boundary, không phải implement toàn bộ 12-level target. Persistent marker
và Continue behavior thuộc [save owner](../systems/save-profile.md#m6-campaign-continuation-và-current-release-end-provisional).
Replay vẫn isolated, không ghi Profile/Campaign hoặc chain mission.
