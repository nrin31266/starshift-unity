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

Matrix: baseline, close, wide và spectacle seeds theo
[camera owner](../content/levels-encounters.md#master-campaign-table-và-camera-profiles),
full và restricted/offset regions; 4 cạnh,
4 góc, footprint, outward stable/reverse ngay; Player ngoài future region khi co;
return baseline kể cả gate đóng; held intent/fresh anchor; retarget giữa transition,
pause scaled time, close request thấp hơn baseline và invalid config fallback.
Settings/tunable contract thuộc
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

## M2 Scout fire verification

`tools/unity/VerifyScoutFire.cs` chạy qua MCP `run_script` trong Play Mode. `Run(width)`
nhận width 1600 hoặc 2000, height 900; dùng production Scout/profile/projectile,
kiểm tra Entry không fire, Hold/delay/telegraph/single/cooldown, aim vào Player sau
khi charge đã bắt đầu, no-homing, speed, lifetime và pool reset ở baseline/wide view.
Đổi Game View thật và capture charge/dodge vào ignored
`app/Temp/StarShiftScoutFireCheckpoint/`; direct-position checks không được gọi là
relative drag verification.

`Dodge(width)` queue virtual Mouse qua Input System để giữ fire và kéo 350 px né
hostile shot thật, kiểm tra receiver probe không bị hit. Helper chờ đạn cũ hết lifetime
trước khi bắt đầu để không lẫn contact từ producer đã disable. Đây là synthetic
input trong Editor, không phải OS drag, Android touch hoặc manual feel approval.

`Lifecycle()` kiểm tra pause giữa charge, kill trong telegraph, root/component
disable/re-enable, flight block/re-entry, pause projectile và teardown root/pool.
`Collision()` dùng cloned config trong memory cho sweep tốc độ cao, compound hitbox,
owner exclusion, same projectile reuse, missed-shot lifetime và contact khi Player
receiver disabled. Probe chỉ nhận contact; không chứng minh Player lives.

Helpers restore scene Scout, camera/view/Game View, Player/gate, timeScale và
device policies trong finally; không mutate original profile. Chạy lại Scout,
PlayerDrag, PulseLaser và GameplayView suites theo checklist phía trên, inspect
references/missing scripts và Console sau Play exit. Global Transition/Result fire
authority chưa có; không claim đã test session orchestration chưa implement.

## M2 player damage lifecycle verification

`tools/unity/VerifyPlayerDamageLifecycle.Run(width)` chạy trong Play Mode qua MCP,
width 1600/2000, height 900. Mỗi aspect chạy nguyên lifecycle ở baseline, close và
wide framing; wide dùng lower 70% region. Không lấy các isolated VS probes thay cho
end-to-end evidence. Scene Scout tạm disable; helper spawn production Scout, dùng
real hostile projectiles và compound body contact, restore/dọn objects cuối test.

Coverage theo scenario:

- A: projectile hit, lives giảm một lần, Dying/cleanup, entrance, safe position,
  post-entry protection và enemy fire grace độc lập rồi Active.
- B: nhiều projectile/body callbacks gần nhau vẫn một accepted death/life loss.
- C: starting lives tới zero qua repeated respawns, một GameOver signal, không
  respawn hoặc nhận damage tiếp, hostile combat blocked/cleared.
- D: projectile trong entrance/invincibility bị consume, không trừ life; body
  overlap còn tồn tại sau protection kết thúc chỉ resolve một valid hit.
- E: death giữa telegraph hủy pending shot; grace không phát shot; production Scout
  resume local delay/telegraph mới. Enemy không bị xóa/freeze bởi death cleanup.
- F: cả hai aspect, current view và restricted region; end pose nằm trong footprint
  bounds, tránh body chặn preferred respawn candidate.

`PointerAndLifecycle()` queue virtual Mouse qua input thật để verify held fire
resume sau entrance, stale drag bị bỏ, release/re-press tạo delta mới. Cũng kiểm tra
callback exception không chặn mandatory life/cleanup work, pause giữa entrance và
root disable/re-enable hủy stale sequence/reset phiên. Pulse chỉ có Lv1 nên death
penalty mới chứng minh minimum Lv1, chưa chứng minh downgrade pattern Lv2/Lv3.

`HostilePool()` kiểm tra real Scout pool: clear trả shot về pool, reset đủ fields,
reuse cùng instance, pause và root-first teardown. `Presentation()` kiểm tra death
flash/protection tint/blink; capture ở ignored `app/Temp/StarShiftLifecycleCheckpoint/`.
Case tất cả candidates bị chặn phải giữ protected entrance cho tới khi blocker được
dọn; fixture chờ friendly shots cũ tự hết lifetime để chúng không giết blocker.

Regression probes cũ chạy trong **Play session riêng**, gọi `IsolateLegacy()` trước:
tạm disable damage lifecycle, mở enemy safety gate và disable scene Scout để
fake trajectories không kích hoạt production death. Không save isolated scene;
stop Play để restore wiring/state. Chạy View/PlayerDrag/PlayerBounds/Pulse/Scout/
ScoutFire suites phù hợp, inspect matrix/references và Console cả sau Play exit.
Helpers không ghi save/profile, không dùng làm runtime dependency. Đây là Editor
physics/synthetic-input evidence, chưa phải Android hardware hoặc performance test.

## M2 L1 fast start và encounter verification

VS-027 dùng `DevFastStart` trên `L1Encounters`, không tạo menu hoặc fake campaign.
Mặc định `Dev Mode Enabled = false`: Play gameplay scene chạy A → B. Trong Editor
hoặc Development Build, bật flag rồi chọn `Start Point`:

- `LEVEL_START`: bắt đầu L1 tại A;
- `ENCOUNTER_1`: bắt đầu A;
- `ENCOUNTER_2`: bắt đầu B.

`Standalone = true` dừng sau encounter đã chọn; false cho phép handoff tới encounter
tiếp nếu có. Trong Play Mode, component context menu **Replay Configured Start**
dọn enemy và hostile/friendly shots, reset Player lives/lifecycle và schedule rồi
chạy lại entry đã chọn. Player giữ vị trí hiện tại trong bounds; replay hủy stale
drag, vẫn theo held-intent contract của input. Stop/Play cũng replay từ config.
Invalid/missing/duplicate config ID báo lỗi rõ, không fallback Continue. Flag mặc
định off và dev override không hoạt động ở normal release build. Không gọi save,
PlayerPrefs hoặc sửa definition assets; persistence contract ở
[save/profile](save-profile.md#replay-và-dev-isolation).

Inspector: chọn `L1Encounters` để xem definition references/dev start; chỉnh formation,
counts, spawn/entry timing, fire coordination và handoff tại hai assets được link
trong runner. Giữ numeric seeds tại
[level owner](../content/levels-encounters.md#m2-l1-playable-checkpoint), HP tại
[enemy owner](../content/enemies-bosses.md#balance-seed-table), view controls tại
[view architecture](architecture.md#dynamic-gameplay-view-foundation).
Đổi config rồi Play lại để demo; không expose mutable counters/state làm knobs.

`tools/unity/VerifyL1Encounters.cs` chạy qua Unity MCP `run_script` trong Play Mode:

- `Run(width, size, lower70)`: actual hold/relative pointer/Pulse kills A, không count
  disable là death, handoff, pooled reset, hai cánh B ghép formation, fire trong exit,
  timeline completion một lần và cleanup trước callback. Observer đo maximum
  simultaneous telegraphs và shot spacing; frames phải tiến thật.
- `Death()`: hostile projectile thật gây death trong A sau một Pulse kill, giữ
  survivors/counters, clear/grace, entrance/protection, pause và finish bằng Pulse.
- `Lifecycle()`: kill trong telegraph, pool reset/replay, pause entry, disable runner
  giữa telegraph, reset giữa handoff, Game Over stop, dev jump/replay A/B và reject invalid start.
- `Boundary()`: timeline cắt khi enemy còn sống; release/cancel/clear trước notification,
  không gửi death/kill và không còn delayed fire.
- `Offscreen()`: gom 4 cạnh ở 16:9/Size 6 và 20:9/Size 6.8, exit release trước hết
  duration, cancel telegraph ở mép, pooled reuse, root còn visible nhưng muzzle
  ngoài view không được fire. Không chạy lại full gameplay matrix cho mỗi tuning pass.
- `RootToggle()`: tắt/bật toàn bộ encounter root rồi replay, không lỗi reparent
  trong activation traversal và vẫn reuse đủ pooled Scouts.
- `QuickTuning()`: một smoke gộp cho tuning pass: production Scout nhận đủ real Pulse
  hits theo HP, đo hit cadence, inspect projectile width và chạy B hết timeline với
  hold ngắn/exit chậm; không lặp full baseline regression.
- `DepartureFeedback()`: gom Normal/Damaged tại các mốc HP, mirrored entry theo
  từng cặp, shot cuối còn sống sau source exit/timeline cutoff và sau fallback
  lifetime khi vẫn visible; chỉ complete khi shot ra khỏi view. Explicit reset
  phải tiếp tục clear và reset camera binding.
- `Validate()`: invalid count, non-finite timing/path và timeline trước scheduled
  group bị reject; chỉ sửa cloned config trong memory.
- `Capture(startPoint, elapsed)`: capture Game View tại thời điểm encounter vào ignored
  `app/Temp/StarShiftL1Checkpoint/`, không lưu test state vào scene.

Matrix width 1600/2000, height 900; size 6, 5.25 và 6.8; wide smoke dùng lower 70%.
Đây là baseline khi thay core; tuning pass gom check bị ảnh hưởng và một lượt
end-to-end thay vì lặp lại toàn matrix cho mỗi thay đổi nhỏ.
Helpers restore camera/view/Game View/input policies/devices/lifecycle/timeScale,
stop test run, không save scene hoặc mutate definitions. Runtime state reset/replay
không chứng minh future save integration. Để isolated regression cũ, gọi
`VerifyL1Encounters.Isolate()` rồi `VerifyPlayerDamageLifecycle.IsolateLegacy()`
trong Play session riêng; không save và stop Play để restore authored scene.
Chạy lại PlayerDrag/Bounds/View/Pulse/Scout/ScoutFire/PlayerDamageLifecycle liên quan,
inspect missing references/scripts và Console sau Play exit. Đây là Editor synthetic
input/physics evidence; manual gameplay feel và Android hardware/FPS còn cần test.

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

## M2 vertical-slice closure verification

`tools/unity/VerifyVerticalSlice.cs` chạy trong Play Mode qua Unity MCP run_script;
không cài test framework hoặc thêm test MonoBehaviour vào production assets.

- `InputAndPause()`: mouse click/drag qua button, UI-start touch rời button,
  gameplay finger + second Pause finger, Resume khi gameplay finger còn held,
  fresh press, telegraph/projectile pause và snapshot timer/pose.
- `FullRun(width,size,withDeath)`: mission announcement → A (real held Pulse kills)
  → scaled handoff → B mirrored formation/timeline → clear một lần. Khi withDeath,
  nhận real hostile projectile, death/entrance/protection/grace, pause invincibility
  rồi tiếp tục. Pause combat/handoff/B kiểm timer và pose không đổi.
- `GameOver()`: ba accepted hits qua lifecycle (không sửa lives), reject duplicate,
  lives zero khóa schedule trong final death, một Game Over signal/overlay.
- `Pooling()`: sustained player/enemy firing, warmup 12 s rồi đo 12 s bằng
  ProfilerRecorder markers Instantiate/Destroy và so sánh identity pool hierarchy.
- `ResetAndStandalone()`: standalone B không phát full mission clear; dev replay
  từ Pause reset clocks/lives/input; disable session trả clock/audio; không spawn đôi.

Helpers restore Game View resolution, camera/view, Input System settings/devices,
clock/audio và lifecycle; không save runtime state hoặc mutate encounter definitions.
Capture dùng ScreenCapture (có overlay UI) vào ignored `app/Temp/StarShiftClosure/`.
Khi chạy probe sau một Play session cũ đã terminal, nên stop/start Play để trả toàn
bộ authored state; không save scene trong Play Mode.

### Closure QA checkpoint — 2026-10-18

Editor 6000.5.9f1, Unity CLI 1.0.0-beta.13. Frame counter thực sự advance trước QA.
Năm full L1 runs dùng runtime config giữ nguyên, không ép completion timer:

| Run | Game View | Size | Death/respawn | Kết quả |
|---|---|---|---|---|
| 1 | 1600 × 900 (16:9) | 6 | Real hostile hit | PASS, 1770 frames |
| 2 | 2000 × 900 (20:9) | 6 | Protected regression | PASS, 1341 frames |
| 3 | 2000 × 900 (20:9) | 6.8 | Alternate framing smoke | PASS, 1336 frames |
| 4 | 1600 × 900 (16:9) | 6 | Protected regression | PASS, 1342 frames |
| 5 | 2000 × 900 (20:9) | 6 | Real hostile hit | PASS, 1767 frames |

Mỗi run có real Pulse collision/kill ở A, B hold/exit/timeline, Pause combat/handoff,
cleanup/Result permissions, đúng một mission clear và không stale spawn/fire sau đó.
Các run protected là automated integration regression; không thay thế người chơi
manual feel review. Không kết luận Android touch latency/safe area/device FPS.

Technical UI copy/layout contract ở [UI/UX](../presentation/ui-ux.md#m2-ui-implementation-provisional);
QA không định nghĩa một copy source thứ hai. Ảnh mission-start, formation, pause,
clear và Game Over lưu tại capture folder trên; review bằng composited Game View.

Supplemental checks cùng checkpoint:

| Check | Evidence |
|---|---|
| InputAndPause | PASS mouse + synthetic multitouch; UI re-press cùng frame hủy press gameplay cũ; 222 frames ở lượt cuối sau khi dùng project InputActions |
| GameOver | PASS 3 accepted hits, lives 0, đúng 1 signal, không spawn trong final death |
| ResetAndStandalone | PASS standalone B không mission clear; dev replay từ Pause; disable restore clock/audio; không duplicate spawn |
| Pooling | PASS 12 s warmup + 12 s/718 frames đo; Instantiate 0, Destroy 0; 10 PlayerProjectile + 6 EnemyProjectile giữ identity; 7 Scout gồm sandbox inactive và pool |
| DepartureFeedback | PASS HP 4/3 Normal, 2/1 Damaged; mirrored error 1.79e-7; source exit/visible >6 s/timeline drain; reset/replay không stale drain |
| Scene inspect | PASS 1 EventSystem, 0 missing scripts/references, font đủ glyph Việt |

UX review ảnh 16:9/20:9: HUD không che B formation; announcement đầu mission ngắn,
Pause/Clear đọc rõ và không thêm “ĐỢT” để tránh thông tin thừa. Player tint override
có sẵn trong scene được giữ; batch không đổi Scout/Pulse/L1 tuning. Checkpoint đầu
batch `0ffb5a69` giữ implementation/tuning trước UI; không include ProjectSettings.

Tracker snapshot task sheet `gid=1237076761` còn Not Started cho VS-006/024/025/026/
028/029 và cả nhiều dependency đã implement. Sáu task trên đủ Editor evidence để
chuyển Done; **chưa ghi lên tracker** vì Google Sheets session chỉ có quyền “Chỉ xem”
và không có connected editor session. Bản patch status/proof/notes nằm ở ignored
`app/Temp/StarShiftClosure/tracker-updates.tsv`; không sửa các dependency chỉ dựa
vào tracker cũ. VS-030/Android và M3 không thuộc batch.

Final inspection sau Play exit: compile sạch, saved scene không dirty, 0 missing
scripts, 0 missing references, 1 EventSystem; Unity Console ground truth **0 errors,
0 warnings**. Tool capture buffer vẫn giữ lỗi cũ/compile typo đã sửa, không dùng
historical buffer counters làm Console cuối. New UI batch để uncommitted; không
push/merge, không đổi package versions hoặc ProjectSettings user có sẵn.
