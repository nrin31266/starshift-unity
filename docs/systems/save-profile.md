# Save, profile và settings

## Phạm vi và ownership

Owner của persisted fields, checkpoint/retry serialization, replay/dev isolation,
storage và versioning. Gameplay owners cung cấp state đã resolve; save layer không
quyết damage, weapon penalty hoặc economy formula. Reward/record policy thuộc
[progression/economy](../gameplay/progression-economy.md#m4-b-reward-và-record-policy-provisional).

## State categories `[LOCKED]`

| State | Trách nhiệm |
|---|---|
| CampaignSave | Current-run checkpoint |
| PlayerProfile | Persistent meta/progression |
| Settings | User preferences |
| Replay runtime | State phiên replay; không overwrite CampaignSave |
| Dev overrides | Test state; không tự ghi production CampaignSave/Profile |

New Game chỉ thay campaign; giữ Profile/Shard/records và Settings. UI confirmation
khi overwrite campaign thuộc [UI owner](../presentation/ui-ux.md). M4-B chỉ có
explicit backend action/dev harness, chưa có Main Menu hay confirmation UI mới.

## M4-B implementation `[PROVISIONAL]`

`SaveEnvelope` version 2 chứa `Campaign`, `Profile`, `Settings` trong một JSON
transaction. `HasCampaign` biểu diễn presence rõ ràng vì Unity inline DTO serialization
không giữ null custom-class ổn định. Không serialize Unity object, MonoBehaviour, coroutine, config asset
hoặc timer transient. Config definitions giữ immutable tại runtime.

| Owner | Responsibility |
|---|---|
| JsonSaveStore | JSON, validation callback, disk transaction và load diagnostics |
| SaveValidation | Shape/version/ranges; nhận catalog predicate từ caller |
| CampaignProgress | Persistent data mutation, dirty flag, reward ledger và clear commit |
| CampaignLifecycle | Session mode, capture/restore qua gameplay owners, save boundaries |
| GameplaySession | Clock/audio/Pause/terminal và reset combat transient |

Snapshot trả ra là deep copy; caller không mutate persistent authority qua DTO.
`CampaignProgress` là plain C#, không tìm scene hoặc điều khiển Player. Lifecycle
component được author trên `L1Encounters`, cùng reusable Gameplay scene.

## Campaign checkpoint `[LOCKED]`

Checkpoint ở **đầu Encounter** (từ cũ: wave). Continue và Retry dựng lại Encounter
từ state data, không snapshot world giữa wave. Không tạo runtime layer Wave.

Không lưu enemy HP/death, projectiles, Player position, active VFX, temporary hazard
hoặc mid-wave supply/drop objects. Definition và schedule được chạy lại bình thường.

### Serialized campaign fields `[PROVISIONAL]`

- `MissionId`: stable LevelDefinition ID; catalog production M5 gồm L1–L4.
- `Status`: Active, MissionCleared, RegionTransition hoặc RegionReady; xem M5 boundary bên dưới.
- `Checkpoint.EncounterId`: exact Encounter ID thuộc Mission, không lưu index/alias.
- `Checkpoint.Lives`: 1..PlayerLives.MaximumLives.
- `Checkpoint.Missiles`: 0..3 reserve; restore cùng checkpoint, ordinary death giữ nguyên.
- `Checkpoint.ActiveWeapon`, `WeaponLevels`: ba branch levels; level 0 biểu diễn
  locked branch, Pulse tối thiểu 1, active branch phải unlocked, max level 3.
- `Checkpoint.Stats`: Score, Shards collected, enemies destroyed, cores collected,
  supply captured, lives lost, playable seconds tại checkpoint.
- `EncounterShardHighWater`: ledger riêng của checkpoint hiện tại; economy policy
  ở [reward owner](../gameplay/progression-economy.md#m4-b-reward-và-record-policy-provisional).
  Reset khi capture Encounter mới; không rollback khi Retry/Continue.

Stats hiện chỉ phục vụ M4 runtime owners; chưa là full Campaign Result schema.
Playable seconds đếm fixed gameplay time khi run active và không cinematic/terminal;
Pause không có physics ticks. Mission card và handoff playable được tính. Retry/
Continue rollback timer/stats cùng snapshot. Full campaign aggregation vẫn [TBD].

### Restore, reset, preserve `[PROVISIONAL]`

| Retry / Continue | State |
|---|---|
| RESTORED | Exact Encounter ID; lives; active weapon và mọi branch level; checkpoint Score/Shards/stats; reserve missiles |
| RESET | Enemies/schedule; friendly/hostile projectiles; spawned pickups/crates; heat/overheat/cadence; Shield/Overdrive/Drone/reserve flight/trail/flash/burst/camera impact runtime; death/entrance timers; gesture; Player pose về safe start anchor |
| PRESERVED | Profile balance/records/unlocks; Settings; current checkpoint reward ledger |

Đồ còn rơi có thể đi qua minor handoff theo
[encounter owner](../content/levels-encounters.md#reward-grace-và-chuyển-encounter),
nhưng vẫn là world transient: Retry/Continue không dựng lại pickup của encounter cũ.

Temporary effects không được serialize. Ordinary Encounter handoff vẫn theo runtime
carry-over hiện có; Retry/Continue bỏ effects vì checkpoint không giữ duration.
Đây là M4 test policy, final carry-over policy vẫn [TBD]. Restore không apply death
penalty lần nữa. Ordinary life loss tiếp tục same Encounter; chỉ explicit Retry
mới restore checkpoint. Death/downgrade thuộc [player owner](../gameplay/player-combat.md).

Retry dùng current checkpoint khi Pause hoặc Game Over, không khi Clear. Nó stop
runner, hủy generation/callback scope, recycle combat objects, restore qua owners,
rồi StartRun đúng Encounter. `RunStarted` reset trước restore callback và trước
`EncounterStarted`; restore launch không recapture/overwrite checkpoint/ledger.
Không reload scene để Retry. Capture checkpoint mới bỏ qua nếu lives đã zero.

## PlayerProfile

[PROVISIONAL] Current implementation giữ `StellarShardBalance`, `HighScore`,
`UnlockedMissions`, `Records` (MissionId, BestScore, BestTimeSeconds). Records chỉ có
consumer Mission Clear; không thêm aggregateStats rỗng. Profile độc lập campaign;
Score không là currency. Weapon unlock/level thuộc campaign loadout, không duplicate
vào Profile. New Game giữ unlocks/records/balance; L1 entry được unlock nếu chưa có.

Conceptual target còn level unlocks, campaignCompleted và full campaign statistics.
M5 có Mission 02–04 consumer; không đánh dấu toàn bộ 12-level campaign complete
chỉ vì Region A clear. Eligibility/unlock
cuối cùng và future records thuộc economy owner, không tự lock trong serializer.

## Settings

[PROVISIONAL] Scalar musicVolume, sfxVolume, vibrationEnabled, screenShakeEnabled
nằm trong envelope version 2; defaults là volume 1, vibration/shake true. Volume
phải finite và thuộc [0,1]; invalid setting bị reject, giữ state trước. Không có
Settings menu/mixer consumer mới; Screen Shake toggle đã được reserve-missile
presentation đọc khi activation. Persist preferences không chứng minh toàn bộ audio/shake consumers
đã được apply; future UI theo [UI owner](../presentation/ui-ux.md#settings).
New Game không reset Settings. Backend `CampaignProgress.SetSettings` độc lập
campaign data; gameplay wrapper chỉ cho explicit Campaign session write.

## New Game và Continue

[PROVISIONAL] New Game tạo L1/Encounter 1, lives 3, Pulse L1, branches khác locked, reserve 1,
zero stats; flush trước launch intro. Profile/Settings còn nguyên. Invalid save
không cho New Game âm thầm thay Profile: caller nhận failure và diagnostics.

Continue đọc/validate toàn envelope và catalog, restore Encounter trực tiếp, bỏ
Mission intro kể cả Encounter 1. No save → false; explicit New Game tạo state.
MissionCleared → false, giữ cleared marker/records; không chạy lại Encounter cuối
hay tạo L2. Main Menu/next-content routing chưa thuộc M4-B.

Editor Play mặc định DevTest; không auto New Game/Continue production. Non-Editor
boot thử Continue, chỉ New Game khi Missing; invalid/unsupported dừng run và expose
LoadStatus/Diagnostic. Build có DevFastStart bật thì dùng isolated DevTest path.
`Continue` hỗ trợ recreate service và recreate Gameplay scene; không yêu cầu giữ
runtime objects sống qua quit. Flush thất bại giữ dirty data để caller có thể retry.

## Replay và Dev isolation

[LOCKED] Replay không commit loadout, score hoặc checkpoint trở lại CampaignSave.
Dev overrides không silent write production CampaignSave/Profile.

[PROVISIONAL] `SessionMode`: Campaign / Replay / DevTest. CampaignLifecycle là
write-permission authority. Explicit NewGame/Continue mới cấp Campaign mode;
raw `EncounterRunner.StartRun` thu hồi quyền về DevTest trước reset/reward events.
DevFastStart và old verifier paths được bảo vệ ở cùng boundary.

BeginReplay chỉ nhận Mission đã unlock, dùng fresh entry preset (L1 hiện Pulse L1,
lives 3); final presets [TBD] thuộc [level owner](../content/levels-encounters.md#mission-replay-presets).
M4-B Replay hoàn toàn không writeback Profile/Settings/Campaign theo brief; final
replay Shard/record policy vẫn [TBD] tại economy owner. Không duplicate gameplay.
Exit dọn run/transient/clock; Continue sau đó đọc original campaign checkpoint.

## Storage và write timing

[PROVISIONAL] `starshift-save-v1.json` dưới `Application.persistentDataPath`.
Tên file legacy được giữ để không tạo save trống khi nâng schema 1 → 2.
Một envelope giữ checkpoint/profile/ledger/settings nhất quán trong mỗi disk write;
không dùng PlayerPrefs. Tests dùng unique temp directory và không sửa production file.

Shard/profile đổi memory ngay và set dirty, không JSON/IO từng pickup. Flush ở New
Game, Encounter capture, Mission Clear, Settings change, gameplay Pause/GameOver,
app pause, quit, component teardown và trước chuyển sang isolated mode. Không save
trong Update hoặc filesystem polling. FixedUpdate chỉ đếm runtime playable time.
OS kill trước một boundary có thể mất dirty phần từ lần flush trước; không claim
OS flush/device crash durability đã được kiểm trên Android.

Clear commit idempotent theo campaign Status. Profile record và cleared marker
cùng envelope; reload/Continue không regrant clear. Chưa có clear bonus amount.
M5 Region A resume marker theo section bên dưới; future region/final campaign marker vẫn [TBD].

## Schema, corruption và migration

[PROVISIONAL] SchemaVersion hiện 2; thêm checkpoint reserve. Load validate required roots, settings ranges,
Mission/Encounter catalog, status/weapon enums, lives/weapon levels và nonnegative
stats/balance/records. Không partial-load state invalid. Có đúng một migration v1 → v2: thêm reserve 1 cho checkpoint cũ, giữ nguyên
Campaign/Profile/ledger/Settings khác. Chỉ ghi lại khi có explicit dirty boundary;
không xây migration framework. Các version khác vẫn reject.

Write validate → UTF-8 `.tmp` → Flush(true) → đọc/validate temp → File.Replace
(primary tồn tại) hoặc File.Move (lần đầu). Primary hợp lệ giữ thành một `.bak`.
Primary hỏng không thay backup hợp lệ; giữ bytes hỏng ở `.invalid` khi explicit
valid write repair. Temp orphan không được dùng làm Continue candidate.

Malformed/invalid/missing primary thử validated `.bak`; báo RecoveredBackup nếu
thành công. Invalid primary + không backup → Invalid; unsupported primary version
→ UnsupportedVersion, không downgrade sang backup hoặc overwrite version đó (v1 là known migration được hỗ trợ).
Missing cả primary/backup → Missing. IO/permission failure → IoFailure và diagnostic.
Không tự xóa/reset Profile. Backup có thể cũ hơn transaction mới nhất; caller được
báo recovery, không claim không mất tiến độ. Retain một backup/một invalid diagnostic;
OS-level atomicity trên mọi filesystem chưa được chứng minh.

## Các contract còn mở

- Full campaignRunStats, campaign aggregation và retry record eligibility [TBD].
- Final reward transaction policy và replay Shards/records [TBD]; M4 test policy ở economy owner.
- Final effect carry-over và level-boundary restore [TBD].
- Final multi-region/campaign unlock/completion [TBD]; Region A marker theo M5 bên dưới.
- Android pause/kill/filesystem behavior chưa device verified; M4-B Editor evidence
  ở [dev/debug](dev-debug.md#m4-b-persistence-verification).


## M5 mission và region boundaries `[PROVISIONAL]`

Catalog validation tra L1–L4 từ RegionCampaignFlow, không phụ thuộc level đang mở.
Continue load đúng definition trước restore checkpoint. Production L1 có hai Encounter;
legacy testbed L1 checkpoints 3–5 không có production mapping tự động, không đoán
state hoặc reset Profile để hợp thức hóa save invalid. Fixture cũ ID `M4_FIXTURE`
chỉ dùng DevTest. Migration old test saves ngoài supported catalog vẫn chưa chốt.

Clear L1–L3: CommitClear record → unlock next Mission + active entry checkpoint
trong cùng envelope → flush → travel ngắn → launch next. Entry carry lives, weapon
levels, active branch, reserve; reset mission-local stats. Không ghi checkpoint của
Mission kế vào Mission cũ. Transaction atomic/idempotence tiếp tục theo storage trên.

Clear L4: record + capture final resolved Player state + `RegionTransition` trong
một transaction. Continue marker này chỉ chạy lại cinematic, không spawn Boss hoặc
regrant death/clear rewards. Khi destination framing Right sẵn sàng, ghi `RegionReady`.
Continue `RegionReady` dựng destination skeleton trực tiếp, không chạy fight/clear lần
nữa. Marker dùng checkpoint Encounter thật của L4, không tạo L5/fake Encounter ID.
Validator chỉ chấp nhận region markers ở Boss checkpoint của Mission cuối catalog.
Schema vẫn 2; thêm enum statuses, không thay shape DTO hoặc đổi storage path.

Replay lấy preset của selected Mission, không chain qua Mission kế; Profile/Campaign
bytes vẫn không writeback. Dev TRANSITION/Phase B giữ isolated boundary. Retry trong
preboss/Boss vẫn restore entry; không snapshot Boss HP/phase/attack timers.

Dev-only `UseTestStorage(path, fixture)` nhận optional cloned catalog cho historical
M4 verifier; không serialize fixture hoặc đưa test catalog vào production boot.
