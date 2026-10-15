# Dev và debug

## Phạm vi và ownership

Owner của DevConfig, debug controls và đường test nhanh. Không yêu cầu custom editor/debug framework. Persistence isolation chi tiết ở [save/profile](save-profile.md).

## Dev mode

### DevConfig là requirement `[LOCKED]`

12 level + boss + transition bắt buộc có đường test nhanh.

### Baseline overrides

DevConfig gom override để vào nhanh level, encounter, boss hoặc transition cần test.
Fields dưới là contract cần hỗ trợ dần khi hệ thống xuất hiện, không yêu cầu tạo
custom editor hay implement toàn bộ tooling cùng lúc.

| Nhóm | Fields |
|---|---|
| Enable và entry | DevModeEnabled; StartLevel; StartEncounter / StartWave; StartPoint |
| Player | StartingLives; Invincible |
| Weapon | UnlockAllWeapons; LaserLevel; PlasmaLevel; MissileLevel |
| Cinematic | SkipIntro; SkipTransition |
| Reward | ForceSupplyDrone; ForceReward |
| Balance | EnemyHpMultiplier; PlayerDamageMultiplier; BossHpOverride / BossHpPercent |
| Timing và visualization | GameSpeed; ShowHitboxes; ShowPaths; ShowSpawnPoints; ShowBounds; ShowFps |

### Start points

Stable IDs và availability mỗi level thuộc
[levels/encounters](../content/levels-encounters.md#checkpoint-và-stable-dev-start-ids).
StartLevel/StartEncounter/StartPoint phải resolve tới content có thật, báo lỗi config
rõ khi không hợp lệ; không silently fallback sang production Continue.

### Example workflow

Test transition sau Boss 1:

```text
StartLevel = 4
StartPoint = BOSS
BossHpPercent = 0.10
SkipTransition = false
```

### Save isolation `[LOCKED]`

Dev override không được mutate production CampaignSave/Profile trừ khi user dùng explicit debug save command.

### Release guard

Dev feature mặc định off trong normal release build.

## Encounter standalone và fast testing

Mỗi hệ thống lớn có entry nhanh: L1 combat, từng completion mode, optional supply,
boss phase/death và transition. Không test Boss 3 bằng cách chơi cả campaign.
Load config/preset test độc lập; override không sửa ScriptableObject definition asset
hoặc production save. Exact UI/config carrier và field types [TBD].

## Save/profile debug và logging

Cho phép inspect checkpoint/profile/settings; reset có target rõ;
force reward không tự ghi vào production profile. Explicit debug save command mới
được mutate production data khi người dùng chủ ý. Release mặc định off.
Log event/config failure hữu ích; không spam per-frame, per-projectile hay mỗi hit.

## Dev/testing strategy

### Test nhanh

Mọi feature lớn phải có đường vào nhanh.

Không test Boss 3 bằng cách chơi 20 phút.

### Core manual test matrix

Chọn các hàng liên quan feature đang đổi; không claim toàn game đã test chỉ vì một
đường vào nhanh chạy được. Matrix cần bao phủ các trường hợp sau khi hệ thống tồn tại:

| Domain | Manual coverage |
|---|---|
| App flow | Launch; Main Menu; New Game; Continue; Missions; Settings |
| Player | Relative drag; auto-fire; weapon switch; death; respawn; invincibility; Shield |
| Encounter | Eliminate; Timeline; Survive; optional Supply event; asteroid |
| Save | Quit mid-wave; reopen; Continue từ wave start; Shard persists; replay không overwrite campaign |
| Pause | Timers stop; audio hoạt động đúng; resume; restart wave |
| Boss | Phase transition; death; reward; region transition |
| Device | Landscape; notch safe area; multiple aspect ratios |

### Automated tests

Không bắt buộc full unit test suite ngay.

Nhưng logic thuần C# phù hợp SHOULD test khi xuất hiện:

- drop weighting;
- save migration;
- score calculation;
- checkpoint snapshot;
- deterministic config validation.

## Definition of Done cho một gameplay task

Một task không được báo done chỉ vì “code đã viết”.

Tùy task, DoD SHOULD gồm:

- compile sạch;
- không Console error mới;
- feature chạy được bằng đường test;
- regression relevant kiểm tra;
- canonical docs update nếu contract đổi;
- tunable nằm config thay vì magic number nếu phù hợp;
- asset attribution update nếu thêm third-party asset;
- không phá save/replay;
- dev path tồn tại nếu feature ở late level.

Nếu không chạy runtime được, agent phải nói rõ:

```text
Not runtime-verified.
```

## Verification checklist cho persistence và device

Khi hệ thống liên quan được implement: kiểm tra corrupt/unknown-schema fallback,
New Game không xóa Profile, replay/dev isolation, pause/restart checkpoint,
loadout downgrade/restore và reward duplication qua Retry.
Device kiểm tra 16:9, 18:9/19.5:9, tablet landscape, safe area và gameplay bounds.
Không claim build/device FPS hoặc runtime verified từ Console sạch.
