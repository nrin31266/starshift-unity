# Unity authoring và verification

## Current art closure và dev tools

Art đã chốt trên các scene/prefab/data hiện tại. Các migration Configure/Organize/
Rebuild đã áp dụng một lần được retire ở cleanup cuối; lịch sử Git giữ cách dựng
cũ. Không chạy lại chúng để overwrite tuning. Giữ importer và regression đang dùng.

`DevFastStart` có dropdown Start Point theo Start Level, nút Replay/Pause/Menu khi
Play và foldout camera/hướng/seed. Saved Dev Mode và mọi flag EncounterDebugView
mặc định OFF. Mission04 TRANSITION chạy handoff Astra→Crimson thật; DevTest tách save.

`VerifyDevTools.Edit` kiểm 48 route trên tám mission trong Edit Mode.
`VerifyVietnameseInspector.Edit` kiểm nhãn, cấp súng, đường dẫn tra cứu và trường
camera nhập trễ trong Edit Mode; không sửa asset gốc hoặc chạy gameplay.
`VerifyM8ProductionLayout.Edit` kiểm importer/frame count và prefab proxy.
`InspectM8Production.References` kiểm serialized references; `.FinalState` kiểm
Edit Mode, scene sạch, dev/debug OFF. `.Contracts` xuất collider/gameplay snapshot
để đối chiếu trước/sau cleanup; không so với baseline tuning của milestone cũ.

`ConfigureM8Production.Import(ids, frameCount)` là importer còn giữ: chỉ chạy Edit
Mode với PNG đã duyệt và đúng frame count từ Aseprite; giữ GUID/Sprite ID. Final
binding nằm trong authored prefabs/scene/data, không cần script migration để boot.

## Runtime probes

Chạy từ idle Menu trong Play Mode, một probe mỗi lần. Probes dùng storage/input
riêng và đối chiếu production save bytes. Không thao tác Editor khi probe còn chạy,
không save runtime state. Chờ JSON terminal sau entry Begin; shell phải hết
`IsBusy` trước khi trả storage cả trong finally.

| Entry | Phạm vi | Kết quả local |
|---|---|---|
| `VerifyArtClosure.Begin(scope)` | carry, strike, beams, boss, dev, art; `all` ghép sáu scope | `.design/art-closure/verification.json` |
| `Begin("death-world")` | Bốn boss skills, ba quái, respawn, Game Over giữ world, Retry/Menu | Cùng file; chi tiết `death-world.json` |
| `Begin("roster")` | Damage/pool/size của roster hiện tại; nạp VerifyM8Final trước | Cùng file; ảnh `.design/m8/final/` |
| `Begin("e2e")` | Một fixture năm encounter có input, reward, Pause, death và missile | Cùng file; chi tiết `e2e.json` |
| `VerifyPolishVfx.Begin("vfx",1600/2000)` | Impact families và bốn boss skills ở hai aspect | `.design/presentation-polish/current.json` |
| `VerifyPolishVfx.Begin("deaths")` | Enemy/Bulwark/supply/asteroid burst, Pause và pool | Cùng file |
| `VerifyPresentationPolish.Thermal/Settings/Combat` | Heat, Settings reset, HUD và death fragments | `.design/presentation-polish/` |
| `VerifyTravelPresentation.Run` | Moving backdrop, region blend, Pause và wide zoom | `.design/presentation-polish/screenshots/verification.json` |
| `VerifyM8Final.Player/Survival/Ui` | Player cosmetics, protection và UI captures | `.design/m8/final/` |
| `VerifyM8Background.Run` | Background extents, moving intro và zoom | `.design/m8/background/` |

`e2e` gọi `VerifyM4Testbed.FullMission(1600,true)` đã nạp trong cùng domain; nạp bằng
entry `DropSelectionEdges`. Đây là fixture đại diện, không phải full Campaign.
Các method thuần/async không có Begin trả trực tiếp; timeout transport không chứng
minh probe kết thúc, phải kiểm Task/file kết quả trước probe khác.

## M6 Region B authoring và verification

Production data đã author trong Unity; không còn bootstrap authoring command.
`InspectRegionA.Inspect`, `VerifyRegionB.Inspect` kiểm catalog/content/references.
Các runtime probes Region B vẫn giữ cho boss, formation, pressure, input, reward,
mission/region handoff và persistence khi domain đó thay đổi.

Shared rig cần compile cùng source, không đưa test MonoBehaviour vào Assets:

```sh
python3 tools/unity/prepare_region_b_verification.py
```

Dùng file `../.design/m6/Verification.cs` từ Unity root; các Begin entries trả ngay,
chờ JSON trong `.design/m6/`. `CurrentTask` phân biệt đang chạy với process đã dừng.
Không rerun full region/balance matrix khi chỉ sửa audio hoặc cleanup local.

## Shared fixtures và regression

`VerifyM4Testbed`, `VerifyVerticalSlice`, `VerifyPacingRefinement`, `VerifyHumanFeel`
và `VerifyFlowPolish` còn giữ các rig/probes có consumer. Không xóa chỉ vì không
được đóng vào production build. Fixture assets nằm trong `Data/Fixtures`; Test*
pickups và M4 flights hỗ trợ fixture năm encounter. `TopHold` là seed flight
còn được shared probe dùng; ArcLeft/ArcRight không còn caller đã retire.

Khi cần compile nhiều source qua run_script, gom using duy nhất lên đầu rồi nối
body của các file liên quan vào ignored `.design/`; không thêm test assembly vào
runtime. Region B có script chuẩn bị ở trên. Các kết quả milestone cũ trong docs là
lịch sử; không suy ra chúng còn PASS sau contract change.

`VerifyVerticalSlice.NaturalProjectileDrain` giữ current natural-exit/pool smoke;
`VerifyM4Testbed.FullMission` giữ E2E fixture. Các entry cũ còn nằm cùng shared rig
không là release checklist. Cleanup cuối chỉ chạy grouped scope bị ảnh hưởng.

## M3.5 thermal HUD checkpoint

Current thermal checks là `VerifyPresentationPolish.Thermal` và `.Combat`.
`VerifyVerticalSlice.ThermalHud(width,lifecycleChecks)` vẫn có thể dùng cho targeted
HUD/projection investigation; không rerun historical thermal/input matrix mỗi lần.
Heat/lifecycle contract thuộc [player owner](../../docs/gameplay/player-combat.md#thermal-lifecycle).

## Campaign score carry

`VerifyCampaignScore.Core` kiểm score/checkpoint/store rollback, validation và legacy
save. `VerifyPersistence.Core` là nearby storage regression. `VerifyCampaignScore.Begin`
chạy mission/region handoff, Retry/Continue/Replay, HUD và byte guard từ fresh Menu;
chờ `.design/score-carry/runtime.json`. Không đụng save thật để seed checkpoint.

`VerifyGameShell` giữ các BeginCore/Stats/Input/Loading/ProductionDamage/Failures/
Recovery/Teardown, Hangar/Layouts/TerminalLayouts, SceneRelaunch và Flow. Chạy đúng
scope khi navigation/storage thay đổi; chờ JSON trong `.design/m7/`.

## Pickup bounce consistency

`VerifyPickupMotion` kiểm bounce/grace/exit, pool reset, Pause và collection once.
`VerifyRegionB.BeginPickupDirections` dùng lại fixture này cho Up/Right ×1600/2000.
Contract thuộc [weapons/items owner](../../docs/content/weapons-items-drops.md).

## Frame rate verification

`VerifyFrameRateLimit.Measure(60)` đo Editor trong ba giây; không thay device profile.
Không claim Android/FPS/final audio hoặc full Campaign từ các fixture trên.


## Audio integration

`ConfigureAudioFinal.Apply` chạy Edit Mode để import selected library WAV, bind palette,
routing Music/SFX/UI và audio-only components/credits. Không tạo mixer structure bằng
reflection hoặc edit YAML. Không rerun để reset mix sau listening. `VerifyAudioFinal.Edit`
kiểm bindings; `Begin` kiểm shell/death/pause/retry; `BeginCombat(scope)` hỗ trợ
`player`, `enemies`, `travel`, `boss`, `all` trên test storage và production byte guard.
`BeginFinal` gom flow và bốn combat scopes. Chờ JSON terminal trong `.design/audio-final/`,
không chạy probes đồng thời hoặc can thiệp phiên Campaign user đang chơi.
Clip/event/mix/license và giới hạn listening thuộc
[Audio Final](../../docs/presentation/audio-final.md).

## Authored rewards

`VerifyAuthoredRewards.Pure` kiểm deadline, default seeded mode và native content.
`VerifyAuthoredRewards.Begin` chạy async từ idle Menu trong Play Mode, chỉ dùng test
storage; đọc `.design/content-rewards/runtime.json` và `save-guard.json` sau terminal.
Probe dùng real enemy deaths/pickup collection, kiểm deadline cho mỗi authored
encounter và fallback khi preferred item đầy/max; thêm NewGame/intro → hai
encounter đầu → Pause/Retry để kiểm Core rollback và reward deadline reset. Không chạy cùng probe khác hoặc
khi user đang chơi Campaign. Schedule thuộc [content owner](../../docs/content/levels-encounters.md#authored-reward-pacing-tunable).

## Audio transition regression

`VerifyAudioTransitions.Begin` chạy async từ idle Menu trong Play Mode. Probe đếm
actual non-loop source starts để kiểm pre-tap silence, một launch, Continue/Pause,
minor handoff wind, same-region Start/Arrival và actual Vanguard death → region
journey → tap → L5. Evidence ở `.design/audio-followup/`, gồm production save guard.
Không bấm Play/Stop/Pause hoặc chạy probe khác khi probe đang hoạt động.
