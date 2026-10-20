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
khi overwrite campaign thuộc [UI owner](../presentation/ui-ux.md). M7 có Main Menu và confirmation theo UI owner.

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

- `MissionId`: stable LevelDefinition ID; catalog production M6 gồm L1–L8.
- `Status`: Active, MissionCleared, RegionTransition, RegionReady (legacy) hoặc CampaignCompleted; xem boundaries bên dưới.
- `Checkpoint.EncounterId`: exact Encounter ID thuộc Mission, không lưu index/alias.
- `Checkpoint.Lives`: 1..PlayerLives.MaximumLives.
- `Checkpoint.Missiles`: 0..3 reserve; restore cùng checkpoint, ordinary death giữ nguyên.
- `Checkpoint.ActiveWeapon`, `WeaponLevels`: ba branch levels; level 0 biểu diễn
  locked branch, Pulse tối thiểu 1, active branch phải unlocked, max level 5. Schema 2 giữ nguyên shape ba branch; save Lv1–3 cũ vẫn hợp lệ, không reset/migrate loadout.
- `Checkpoint.Stats`: Score, Shards collected, enemies destroyed, cores collected,
  supply captured, lives lost, playable seconds tại checkpoint.
- `EncounterShardHighWater`: ledger riêng của checkpoint hiện tại; economy policy
  ở [reward owner](../gameplay/progression-economy.md#m4-b-reward-và-record-policy-provisional).
  Reset khi capture Encounter mới; không rollback khi Retry/Continue.

M7 Results đọc chính stats này; current-release Victory dùng Mission08 checkpoint stats, không thêm campaign aggregate schema.
Playable seconds đếm fixed gameplay time khi run active và không cinematic/terminal;
Pause không có physics ticks. Mission title overlay và handoff playable trong active run được tính; between-run travel
không tự cộng Mission-local timer. Retry/
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
consumer Mission Clear; không thêm aggregateStats rỗng. M7 thêm optional OwnedCosmetics/EquippedCosmetics như section bên dưới. Profile độc lập campaign;
Score không là currency. Weapon unlock/level thuộc campaign loadout, không duplicate
vào Profile. New Game giữ unlocks/records/balance; L1 entry được unlock nếu chưa có.

Conceptual target còn level unlocks, campaignCompleted và full campaign statistics.
M5 có Mission 02–04 consumer; không đánh dấu toàn bộ 12-level campaign complete
chỉ vì Region A clear. Eligibility/unlock
cuối cùng và future records thuộc economy owner, không tự lock trong serializer.

## Settings

[PROVISIONAL] Scalar musicVolume, sfxVolume, vibrationEnabled, screenShakeEnabled
nằm trong envelope version 2; defaults là volume 1, vibration/shake true. Volume
phải finite và thuộc [0,1]; invalid setting bị reject, giữ state trước. M7 có shared Settings menu. Screen Shake được reserve-missile presentation đọc khi activation; Vibration qua SettingsFeedback gọi Android haptic tại cùng impact. Music/SFX áp volume lên hai AudioSource channels, chưa có final audio content. UI theo [UI owner](../presentation/ui-ux.md#settings).
New Game không reset Settings. Backend `CampaignProgress.SetSettings` độc lập campaign data. M7 cho explicit user Settings action từ Menu/Pause, kể cả Replay; chỉ preferences thay đổi, không grant quyền ghi gameplay vào Campaign/Profile.

## New Game và Continue

[PROVISIONAL] New Game tạo L1/Encounter 1, lives 3, Pulse L1, branches khác locked, reserve 1,
zero stats; flush trước launch intro. Profile/Settings còn nguyên. Invalid save
không cho New Game âm thầm thay Profile: caller nhận failure và diagnostics.

Continue đọc/validate toàn envelope và catalog, restore Encounter trực tiếp, bỏ
Mission intro kể cả Encounter 1. No save → false; explicit New Game tạo state.
MissionCleared → false, giữ cleared marker/records; không chạy lại Encounter cuối
hay tạo L2. Main Menu/next-content routing chưa thuộc M4-B.

Production/Edit Play khi DevFastStart OFF đều boot Menu, không auto NewGame/Continue. Shell đọc LoadStatus/Diagnostic và chỉ explicit user action mới launch campaign. DevFastStart được bật chủ động vẫn dùng isolated DevTest path.
`Continue` hỗ trợ recreate service và recreate Gameplay scene; không yêu cầu giữ
runtime objects sống qua quit. Flush thất bại giữ dirty data để caller có thể retry.

## Replay và Dev isolation

[LOCKED] Replay không commit loadout, score hoặc checkpoint trở lại CampaignSave.
Dev overrides không silent write production CampaignSave/Profile.

[PROVISIONAL] `SessionMode`: Campaign / Replay / DevTest / Frontend (runtime-only, M7). CampaignLifecycle là
write-permission authority. Explicit NewGame/Continue mới cấp Campaign mode;
raw `EncounterRunner.StartRun` thu hồi quyền về DevTest trước reset/reward events.
DevFastStart và old verifier paths được bảo vệ ở cùng boundary.

BeginReplay chỉ nhận Mission đã unlock, dùng fresh entry preset (L1 hiện Pulse L1,
lives 3); final presets [TBD] thuộc [level owner](../content/levels-encounters.md#mission-replay-presets).
M7 Replay gameplay không writeback Profile/Campaign; explicit Settings action được phép chỉ thay preferences như section Settings. Final
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

- Full campaignRunStats (ngoài Score) và retry record eligibility [TBD].
- Final reward transaction policy và replay Shards/records [TBD]; M4 test policy ở economy owner.
- Final effect carry-over và level-boundary restore [TBD].
- Final multi-region/campaign unlock/completion [TBD]; Region A marker theo M5 bên dưới.
- Android pause/kill/filesystem behavior chưa device verified; M4-B Editor evidence
  ở [dev/debug](dev-debug.md#m4-b-persistence-verification).


## M5 mission và region boundaries `[PROVISIONAL]`

Catalog validation tra L1–L8 từ RegionCampaignFlow, không phụ thuộc level đang mở.
Continue load đúng definition trước restore checkpoint. Production L1 có hai Encounter;
legacy testbed L1 checkpoints 3–5 không có production mapping tự động, không đoán
state hoặc reset Profile để hợp thức hóa save invalid. Fixture cũ ID `M4_FIXTURE`
chỉ dùng DevTest. Migration old test saves ngoài supported catalog vẫn chưa chốt.

M5 thiết lập mission travel và semantic region markers. Contract hiện tại mở rộng
catalog, resume legacy markers và release victory nằm ở [M6 boundary](#m6-campaign-continuation-và-current-release-end-provisional).
Không giữ Right-ready skeleton làm production finish line sau khi đã có L5.

Replay lấy preset của selected Mission, không chain qua Mission kế; Profile/Campaign
bytes vẫn không writeback. Dev TRANSITION/Phase B giữ isolated boundary. Retry trong
preboss/Boss vẫn restore entry; không snapshot Boss HP/phase/attack timers.

Dev-only `UseTestStorage(path, fixture)` nhận optional cloned catalog cho historical
M4 verifier; không serialize fixture hoặc đưa test catalog vào production boot.


M5 second feel pass không thêm persisted loot/world state hoặc schema version.
Encounter quota/selection seed được dựng lại theo [reward owner](../content/weapons-items-drops.md#encounter-reward-floor-provisional).
Retry/Continue reset asteroid variant HP, damage presentation, Boss phase/motion pause,
beam charge/active bounds và pooled loot; weapon state vẫn restore checkpoint và
Shard high-water vẫn preserved. DevFastStart và replay tiếp tục cùng write-permission
boundary, không dùng seed override để cấp quyền ghi production.


## M6 campaign continuation và current release end `[PROVISIONAL]`

Catalog được mở rộng tới Mission08, không thêm production L9–L12. Save envelope
vẫn schema 2, storage path/atomic write/backup giữ nguyên. `CampaignStatus.CampaignCompleted`
append sau các enum cũ để giữ serialized numeric values. Không nâng version chỉ vì
thêm content IDs hoặc status; client cũ không hiểu status mới vẫn reject qua validation.

Clear L4 commit record và resolved checkpoint với `RegionTransition` trong một
transaction. Journey Up→Right giữ checkpoint L4 cho tới khi destination approach
hoàn tất. Sau đó `AdvanceMission` commit L5 entry/checkpoint/unlock trước khi launch.
Nếu ghi đĩa thất bại trong `CompleteRegionTravel`, in-memory state được rollback về snapshot
L4/`RegionTransition` chưa advance, session dừng ở trạng thái Pause và không vào màn hình Result
hay advance sang L5 khi chưa ghi thành công; khi Resume hoặc reload Continue, hệ thống thử ghi lại/chạy
lại transition an toàn mà không duplicate records hay corrupt save.
Continue marker `RegionTransition` của M5 chạy lại journey; `RegionReady` legacy
chuyển trực tiếp sang entry L5. Không replay Vanguard hoặc cấp lại record/reward.
Continue checkpoint Active L5–L8 load đúng definition qua catalog trước restore.
Rời Replay hoặc DevTest để Continue Campaign thực thi `ResetRuntime()`, reset `CurrentIndex = 0`
và clamp bounds của `Current` encounter, khôi phục snapshot persistent chính xác mà không rò rỉ
encounter index hay state từ phiên test/isolated.

Clear L1–L3 và L5–L7 dùng same-region mission boundary hiện có: record + next entry checkpoint
trong cùng transaction, reset mission-local stats và giữ player loadout/lives/reserve.
Retry trong Pause của same-region travel dùng incoming entry đã commit, không
replay Mission vừa clear hoặc ghép outgoing Encounter ID vào incoming Mission.
Major A→B journey/approach chưa cho Retry; Continue dùng transition marker cho tới
khi L5 entry được commit.

Clear L8 commit record, resolved Player state và `CampaignCompleted`; UI/session
vào current-campaign victory. Không tạo L9 checkpoint hoặc queue Region C travel.
Continue completed save dựng terminal victory, không spawn boss hoặc regrant clear.

Retry/Continue reset module HP, shield network, pending attacks/strikes, spawned drones,
beam visuals/queries và reward world transient qua run/pooled lifecycle; không serialize
boss phase, module state, drones hoặc mid-wave objects. Replay/DevFastStart vẫn thu
hồi write permission trước start; M6 probes dùng test storage riêng và so production bytes.


## M7 frontend transactions `[PROVISIONAL]`

Không đổi schema 2 hoặc JSON store. RunRewards/checkpoint stats và MissionRecord shape
đã đủ cho Results/Hub; không thêm save DTO khác. NewGame và SetSettings snapshot data
trước mutation, write failure restore cả state lẫn dirty flag. Không chỉ rollback UI.

Mission clear dùng `CampaignProgress.TryCommitClear`: record + resolved checkpoint/status
hoặc incoming same-region entry/unlock trong cùng transaction. Failure rollback mutation,
CampaignLifecycle giữ HasPendingClear và frozen runtime stats để thử lại; Result không
claim record/chuyển nhiệm vụ thành công. Flush retry clear có tính idempotent; UI
tự tiếp tục Campaign sau khi retry thành công theo [UI owner](../presentation/ui-ux.md#level-clear-và-campaign-result). Các shard đã credit
ở boundary trước vẫn giữ theo high-water policy; không regrant clear reward.

Region journey vẫn dùng transaction M6. Failed destination commit vào Pause; Resume
thử lại save, không advance khi chưa commit. Exit về shell flush trước; nếu fail,
modal giữ đường retry và không âm thầm abandon dirty state. Invalid/unsupported dữ liệu
không partial-load/reset Profile; UI có retry read, preserve bytes và backup diagnostics.


## M7 Profile cosmetics và explicit recovery `[PROVISIONAL]`

Envelope vẫn schema 2. PlayerProfile có additive optional `OwnedCosmetics` (unique
nonempty IDs) và `EquippedCosmetics` (ba slot Chassis/Skin/Trail). Missing/null/empty
fields từ save M6 dùng catalog defaults, không reset wallet/records. Khi explicit
mua/trang bị lần đầu, defaults được materialize cùng transaction. Unknown IDs được
giữ nhưng presentation fallback về default nếu ID/slot/ownership không hợp lệ;
không biến unknown cosmetic thành gameplay stats hoặc currency.

`SessionMode.Frontend` chỉ là runtime permission của shell sau khi run đã exit.
CampaignLifecycle chỉ cấp EquipCosmetic trong Frontend, runner inactive, qua catalog
option/price. Raw runner start thu hồi về DevTest; BeginReplay thu hồi về Replay.
CampaignProgress trừ wallet và cập nhật ownership/equip trong cùng JSON transaction,
rollback toàn bộ khi write fail. UI không giữ currency store thứ hai.

Unrecoverable Invalid data có action Tạo chiến dịch mới với explicit confirmation.
Continue vẫn không partial-load. Chỉ recovery action mới salvage Profile và Settings
còn validate được; roots không đọc/validate được dùng defaults và copy giải thích
rõ điều này trước khi confirm. Primary lỗi được store giữ nguyên bytes trong `.invalid`
trước successful replacement; invalid backup vẫn được giữ nếu primary missing.
Recovery write fail giữ data invalid/unavailable và không claim đã tạo campaign.
Unsupported version không được reset/overwrite bằng recovery; I/O failure có retry.

M7 đọc save M6, không đảm bảo client M6 cũ giữ các additive cosmetic fields khi ghi lại.


## Campaign score carry `[LOCKED]`

Schema 2 thêm optional `Campaign.ScoreBeforeMission` (`long`, nonnegative, reserve
headroom `int.MaxValue` để cộng điểm mission); giá trị là điểm đã bank từ các mission
trước trong **current campaign**, không phải tổng Profile best records. Missing field
trong save v1/v2 đọc thành 0; v1 reserve migration vẫn giữ nguyên. Không boot rewrite
hoặc đoán tổng từ records (records có thể thuộc campaign/replay khác). Vì save cũ chưa
lưu lịch sử run, không thể phục hồi chính xác điểm các mission đã qua; chỉ tiếp tục
cộng từ baseline 0 + checkpoint mission đang lưu. Profile/loadout/wallet giữ nguyên.

`RunRewards.Score` và `Checkpoint.Stats.Score` vẫn mission-local. Lifecycle cung cấp
`DisplayScore`; HUD không giữ authority riêng. Same-region clear bank outgoing score
vào incoming CampaignSave trong cùng record/entry transaction. Trong travel, nếu
runtime còn outgoing mission nhưng saved entry đã là incoming, HUD chỉ đọc baseline
đã bank để không cộng đôi outgoing score. Incoming launch restore mission stats 0.

Region marker giữ baseline trước outgoing mission cùng resolved outgoing checkpoint.
Destination commit mới bank checkpoint score; failure rollback toàn transaction,
retry không bank thêm lần nữa. CampaignCompleted giữ baseline + final mission stats
cho Continue. Capture Encounter mới chỉ đổi checkpoint, không đổi banked score.
Retry/Continue khôi phục mission score checkpoint; Replay/Dev không dùng campaign
baseline. NewGame tạo CampaignSave mới baseline 0, giữ Profile/Settings.

Như additive cosmetics, client cũ không đảm bảo giữ field này khi ghi lại; không
hỗ trợ downgrade rồi tiếp tục campaign score. Không đổi store/backup path hoặc dựng
migration framework. Policy điểm thuộc [economy owner](../gameplay/progression-economy.md#campaign-score-locked).
