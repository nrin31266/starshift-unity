# Save, profile và settings

## Phạm vi và ownership

Tài liệu này sở hữu persisted fields, checkpoint/retry serialization, replay/dev
isolation, storage và versioning. Gameplay domain cung cấp state đã resolve;
save layer không quyết damage, weapon penalty hoặc economy formula.

## State categories `[LOCKED]`

Campaign checkpoint, persistent profile và user preferences có lifetime khác nhau.
Tách chúng giúp Retry/New Game không vô tình xóa meta progression, đồng thời giữ
Replay và Dev overrides khỏi production campaign.

| State | Trách nhiệm |
|---|---|
| CampaignSave | Current-run checkpoint |
| PlayerProfile | Persistent meta/progression |
| Settings | User preferences |
| Replay runtime | State phiên replay; không overwrite CampaignSave |
| Dev overrides | Test state; không tự ghi vào production CampaignSave/Profile |

## Campaign checkpoint `[LOCKED]`

Save ở **đầu wave/encounter checkpoint**. Continue và Retry dựng lại encounter từ
snapshot này, thay vì serialize world đang chạy. Policy đó giữ save nhỏ và tránh
phụ thuộc từng enemy/projectile vào thời điểm app bị đóng.

Không lưu world snapshot gồm:

- Enemy nào đã chết, HP hiện tại của enemy và projectile đang bay.
- Player position giữa wave, active VFX và temporary hazard.
- Mid-wave Supply Drone state.

### CampaignSave fields baseline

Đây là conceptual schema, chưa khóa exact serialized shape hay field types.
`campaignRunStats` chỉ chứa thống kê cần thiết để restore checkpoint nhất quán.

```text
schemaVersion
currentLevelId
currentEncounterId / currentWaveId
livesAtCheckpoint
scoreAtCheckpoint
activeWeapon
weaponUnlocks
weaponLevels
campaignRunStats
timestamp optional
```

### Retry và quit giữa wave

Retry restore lives, weapons, score và run stats phù hợp từ đúng snapshot đầu
checkpoint. Quit giữa wave rồi mở app lại cũng đi theo `Continue → đầu wave/checkpoint`;
không cố khôi phục enemy, projectile hoặc vị trí tàu đang dở.
Rule death/downgrade nằm tại [player combat](../gameplay/player-combat.md), không nằm
trong serializer.

## PlayerProfile

Profile giữ Shard balance, records và unlocks qua các lần chạy. Baseline fields:

```text
schemaVersion
stellarShardBalance
highScore
levelUnlocks
bestScorePerLevel
bestTimePerLevel
campaignCompleted
aggregateStats optional
```

Policy score/Shard/unlock và best-record eligibility do
[progression/economy](../gameplay/progression-economy.md) sở hữu. Đổi field persistence
không tự thay policy reward hoặc record.

## Settings

Settings tồn tại độc lập với campaign. Baseline controls do [UI/UX](../presentation/ui-ux.md)
trình bày; save layer lưu các scalar preferences sau:

```text
musicVolume
sfxVolume
vibrationEnabled
screenShakeEnabled/intensity
```

## Replay và Dev isolation

[LOCKED] Replay không commit loadout, score hoặc checkpoint trở lại CampaignSave.
Replay record/reward policy ở [progression/economy](../gameplay/progression-economy.md);
persistent replay Shard vẫn [TBD], không được mặc định bật hoặc tắt trong serializer.

Dev overrides tách production CampaignSave/Profile. Chỉ explicit debug save command
mới được ghi theo target đã chọn; StartPoint, HP override hoặc force reward không
được silent save. [Dev/debug](dev-debug.md) sở hữu cách truy cập các lệnh test.

## New Game và debug reset

New Game chỉ thay current campaign; giữ PlayerProfile/Shard/high score và Settings.
Debug reset phải chọn rõ Campaign, Profile, Settings hoặc test data, không mặc định
reset toàn bộ production data. Confirmation thuộc [UI/UX](../presentation/ui-ux.md).

## Storage và write timing

Persistent gameplay/profile data SHOULD là file JSON dưới `Application.persistentDataPath`
hoặc abstraction tương đương. PlayerPrefs MAY dùng cho scalar Settings đơn giản,
không nên chứa cả CampaignSave phức tạp. API reference là
[Application.persistentDataPath](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html);
file format và corruption handling vẫn là contract của game.

Shard/profile đổi trong memory ngay; disk write theo dirty/debounce strategy.
Save ở checkpoint, level clear và campaign result; flush dirty data ở pause/main-menu
transition hoặc app lifecycle event phù hợp. Không write mỗi frame hay từng Shard pickup.

Sau level clear, cập nhật progression/record và checkpoint hợp lệ cho bước kế tiếp.
Boss cần hoàn tất post-death trước handoff. Exact resume marker khi quit giữa boss
death/reward/region transition vẫn [TBD]; không giả snapshot cinematic để lấp khoảng trống.

## Schema, corruption và migration

Save safety SHOULD có schemaVersion, temp write, atomic-ish replace và fallback
khi corrupt. Continue chỉ hợp lệ khi snapshot đọc/validate được; không nhận file
nửa chừng là campaign hợp lệ. Settings cũng cần version policy khi shape thay đổi.

[TBD] Exact version number, DTO shape, filenames, validation ranges, backup retention
và unsupported-version behavior. Trong development, mỗi schema change cần policy
migrate hoặc reset test save rõ ràng; không âm thầm xóa Profile thật để sửa loader.
Migration cụ thể chỉ được viết khi schema tồn tại.

## Các contract còn mở

- Exact `campaignRunStats` checkpoint shape và retry aggregation [TBD].
- Profile Shard không rollback cùng campaign; reward deduplication/transaction qua Retry chưa quyết, không tự reset balance để tránh farm.
- Buff/shield carry-over tại checkpoint và level boundary [TBD].
- Boss/transition resume marker và crash consistency giữa Profile/Campaign [TBD].
- Replay record write timing và failed/retried level record eligibility [TBD].

Các câu hỏi này cần giải quyết khi implement persistence, không bằng việc tự điền
schema trong editorial pass. Save/replay/dev và corrupt-save tests nằm tại [dev/debug](dev-debug.md).
