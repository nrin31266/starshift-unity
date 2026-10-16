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

## M2 Pulse Laser verification

VS-009–011 có entry `VerifyPulseLaser.Run` ở `tools/unity/VerifyPulseLaser.cs`, chạy
qua Unity MCP `run_script` trong Play Mode. Helper compile probe MonoBehaviour trong
memory, tạo GameObjects DontSave và dọn tất cả sau test; không lưu probe thành
production enemy hoặc đưa dependency test vào runtime scripts.

Coverage: idle không fire, giữ yên fire, release ngừng spawn ngay, re-press anchor
mới, held intent độc lập movement, synthetic mouse/touch, gate truth table và pause,
movement block/fresh pointer, sustained cadence,
state fire block/resume khi pointer còn held, hướng Up/Right, owner filter, compound collider single-hit,
thin-target sweep, reuse reset và lifetime release. Config thử speed cao dùng clone
trong memory; original definition phải không đổi. Đây là physics/integration probe,
không chứng minh Scout HP/damage state hoặc prefab production. Xác minh phần đó ở
VS-012/013; không coi dependency đã hoàn thành chỉ vì probe pass.
Seed thuộc [weapons owner](../content/weapons-items-drops.md#pulse-laser), collision
contract thuộc [player owner](../gameplay/player-combat.md#collision-matrix-theo-gameplay-contract).

## Dynamic gameplay view verification

`tools/unity/VerifyGameplayView.Run(width)` là fast path trong gameplay scene,
không cần tới wave/boss thật. Chạy ở Play Mode qua MCP cho width 1600 và 2000,
height 900; frames phải thực sự tiến, không lấy screenshot/Console sạch làm bằng
chứng gameplay. Probe compile trong memory, quan sát sau movement LateUpdate;
không thêm runtime debug UI hoặc production verifier.

Matrix: baseline, hai larger sizes, full và restricted/offset regions; 4 cạnh,
4 góc, footprint, outward stable/reverse ngay; Player ngoài future region khi co;
return baseline kể cả gate đóng; held intent/fresh anchor; retarget giữa transition,
pause scaled time và invalid config. Settings/tunable contract thuộc
[view architecture](architecture.md#dynamic-gameplay-view-foundation), không do
test fixture quyết balance.

Chạy lại `VerifyPlayerDrag.Run` và `VerifyPulseLaser.Run` cho relative delta,
mouse/touch simulation, release/re-press, hold/drag fire, gate và projectile
hit/lifetime/pool regression, gồm teardown khi projectile root bị hủy trước weapon.
Xem Console cả sau khi thoát Play Mode và inspect scene/prefab refs.
Helpers restore camera/view/player/input policy/devices/Game View trong finally;
Game View presets chỉ ở Editor preferences. Đây là synthetic Input System events
trong Editor, chưa chứng minh Android hardware feel/readability hoặc device FPS.

## M2 Scout verification

`tools/unity/VerifyScout.Run(width)` chạy ở Play Mode qua MCP với width 1600 hoặc
2000, height 900. Spawn production prefab, dùng Player hold-to-fire và Pulse pool
thật để kiểm tra HP qua từng hit, flash/persistent Damaged và one-time death/release.
Test entry/hold, baseline/zoom-out, entry trong view transition và return; quan sát
render pose sau LateUpdate để bắt camera reprojection bị trễ một frame.

`VerifyScout.EdgeCases` dùng projectile prefab trong local verification pool để
kiểm tra compound colliders, rapid volley, cùng projectile reuse, dead/non-positive
damage guard, presentation không quyết HP, reset/re-enable và chết giữa entry.
Callback feedback lỗi không được chặn death hook/release; verifier bắt exception
cố ý để không biến nó thành Console error của gameplay.
Definition không được mutate. Test helpers/probes không được save vào production.

Để regression các suite player/view cũ, tạm disable scene Scout bằng
`VerifyScout.SetSceneScoutActive(false)` rồi restore `true` sau test; enemy thật
không được xen vào probe trajectory của isolated suite. Chạy `VerifyPlayerDrag`,
`VerifyPulseLaser` và `VerifyGameplayView`; kiểm tra Console cả sau Play exit.
Helpers restore native device/policy, camera/view/Game View/gate/player trong finally.
Đây là Editor synthetic input, chưa chứng minh Android hardware/readability/FPS.

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
| Player | Relative drag; hold-to-fire; weapon switch; death; respawn; invincibility; Shield |
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
