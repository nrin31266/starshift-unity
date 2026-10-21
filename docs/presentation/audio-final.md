# Audio Final — StarShift

## Phạm vi và trạng thái

Integration checkpoint 2026-10-21 từ `main` `c330ea23`, worktree sạch trước task.
Owner của concrete audio event bindings, clip selection, mixing values và QA evidence.
Feedback choreography chung thuộc [audio/VFX](audio-vfx.md); assets/licensing workflow
thuộc [asset pipeline](../production/asset-pipeline.md#python-sfx-và-bgm-import).

Art freeze; không đổi gameplay data, collider, save schema, Editor/package version.
Toàn bộ mix values dưới đây là `[TUNABLE]`. Chưa khóa lựa chọn âm sắc khi chưa nghe.
**Listening revision 2026-10-21:** user phản hồi mix trước quá nhỏ ở Plasma,
coin/nổ thiếu sắc, vent giống growl, Combat BGM không hợp; không xem checkpoint
cũ là listening acceptance. Hub giữ nguyên. User chọn **B — Short Space Casual Loop #3**
cho Combat. Bản sửa tách weapon/pickup khỏi common budget, sửa các transient và warp
choreography. Automated QA ghi bên dưới; final subjective mix chưa được xác nhận.
Không Android/FPS/device/full Campaign trong phase này.

Follow-up 2026-10-22: Pulse giảm khoảng 5.2 dB so với mix trước; Homing thay bằng
soft air launch .22 s, không ignition crack/fly loop. Coin dùng resonant glass tail
.55 s và gain lớn hơn; Back dùng minimize cue đủ tail thay click .09 s. Enemy/asteroid
explosion tăng độ rõ/gain. Một wind one-shot nối các đoạn bay, không continuous loop.
Sửa trigger launch trước tap và launch lặp sau boss/Continue; không đổi BGM selection.

## Music selection

| Event / state | Selected clip / original title | Group | Gain / voices | Transition |
|---|---|---|---|---|
| Menu, Hangar, Shop, Settings từ Menu | `Music/bgm-hub.wav` — Short Space Casual Loop #1 | Music | .20 × Music setting; 1 stable loop | Giữ clip/cursor qua panels |
| Gameplay, Mission/region travel | `Music/bgm-combat.wav` — Short Space Casual Loop #3 | Music | .12 × Music setting; 1 stable loop | Giữ Combat qua Mission/region, travel duck |
| Boss encounter | `Music/bgm-boss.wav` — Drive With Me | Music | .12 × Music setting; 1 stable loop | Combat ↔ Boss crossfade |
| Boss final destruction | `SFX/victory.wav` — Little Robot Sound Factory jingle | SFX | Xem catalog | Sau staged bursts/final hit |
| Game Over | `SFX/game-over.wav` — Little Robot Sound Factory jingle | SFX | Xem catalog | Sau natural death, delay .30 s từ overlay |

Ba loop từ Andrew Sitkov **Music Loops Mini Set / Mini Set 2**. License gốc cho phép dùng
trong game thương mại và phân phối kèm project; cấm claim authorship, register/sell
hoặc phân phối nhạc riêng. [Source/reproduction](../../tools/audio/README.md),
[license PDF](../../tools/audio/licenses/andrew-sitkov.pdf),
[clip provenance + hashes](../../tools/audio/sources.json).
Không ship cả pack. WAV source được peak/RMS-normalize bằng static gain, nối loop
20 ms; Unity import Vorbis streaming, preload off. SFX mono PCM, decompress on load.
Selected exports gồm đúng ba BGM và 41 SFX/stingers, không có audition intermediate
trong Assets. 38 SFX/stinger dùng Kenney Sci-Fi/Digital/Interface và Little Robot Sound Factory,
trim/EQ/compression/reverse/fade theo family. Vent là original broadband pressure-release
noise, có seed cố định, high-pass 1.1 kHz/low-pass 10 kHz và decay .58 s. Homing và
TransitionWind là hai original filtered pink-noise cues; không dùng engine hoặc tonal oscillator. MAX dùng cue cảnh báo ngắn .20 s trước vent, không chồng growl.

## Runtime mix và lifecycle

`AudioPalette` chứa immutable authoring data. `AudioDirector` giữ mutable voices,
cooldown, music cursor và envelope; không ghi definition/save/gameplay state.
Player/Enemy/Game audio presentation dùng events và đọc state. Cannon phase ngắn
được bắt bằng event để không bỏ qua Attack trong cùng physics tick. Pool disable
unsubscribe events; enable reset phase/heat/death-beat tracking.

Music dùng hai sources, linear crossfade .85 s theo unscaled time. Briefing giữ Hub và không phát launch. Tap bắt đầu Approach phát một WarpStart + wind,
Hub fade .85 s rồi Combat fade in; không trì hoãn gameplay. Không có timer phát
ignition khi rời Menu; Continue checkpoint và Pause/Resume không phát launch.
Death còn lives không đổi track/cursor. Pause target music .30; Resume về 1.
Travel target .55, boss post-death .15, Game Over .12. Danger cue target .45.
Những duck đồng thời dùng mức nhỏ nhất; attack 5 gain/s, release 1.6 gain/s.
`GameplaySession` tiếp tục sở hữu `Time.timeScale`/`AudioListener.pause`; audio sources
có `ignoreListenerPause` để envelope/UI/music và one-shot tails vẫn chạy.
Game Over giữ battlefield và BGM thấp; Retry phục hồi track tương ứng, Menu crossfade Hub.
Không thêm ambience/engine loop chỉ để lấp overlay.

16 SFX voices được tạo một lần: 4 danger, 2 critical/stinger, 2 player weapon,
4 common action/impact, 2 pickup, 2 UI. Weapon và pickup không cạnh tranh slot hoặc
aggregate budget với explosion/hostile shot. Critical pool đầy thay voice cũ nhất
trong chính pool; per-cue cap/rate vẫn áp dụng. Không PlayOneShot chồng vô hạn.
Homing impact dùng common pool, không chiếm weapon slot. Vent, boss enter và Extra Life
được bảo vệ trong critical pool.

Aggregate nominal gain budgets lần lượt: danger .60, critical .65, weapon .65,
common .65, pickup .55, UI .35. Chỉ scale trong từng pool khi tổng nominal vượt budget;
giảm ngay, phục hồi 2 gain/s. Khi danger phát, weapon × .55, common × .30, pickup × .65;
critical/UI giữ rõ. Pause/Game Over: weapon/common/pickup × .12. Bản cũ gom tất cả vào
common budget .20 và danger duck .25, gây mất độ rõ ngay cả khi BGM tắt.
SFX export peak .68, BGM peak ≤ .68. Hub nominal .20, Combat/Boss .12 trước settings/duck.
Music/SFX settings giữ nguyên schema; UI dùng SFX preference.

Warp start là swell reverse .70 s, pass là energy flyby 1.20 s, arrival là brake/settle
.75 s. Mission Approach/Travel/Brake và region Accelerate/Decelerate đều có cue;
region departure có đúng một Start. Tap tiếp cận vùng mới chỉ nối wind/pass/arrival,
không kích hỏa lại. Minor encounter handoff chỉ có wind khi IsHandoffTravel mở,
không phát trong stationary clear pause. Same-region mission travel có arrival khi handoff xong.
Không thêm loop/travel song, không đổi duration hoặc choreography hình ảnh.
Homing launch là air puff mềm .22 s, không metallic ignition hoặc sustained fly sound; impact/heavy/boss thêm một body/crack
layer trong chính export, không tăng số runtime voices. Common enemy explosion vẫn
ngắn/nhỏ hơn heavy, boss có hai staged bursts rồi một final 1.05 s, không explosion spam.

New run/Retry và về frontend stop gameplay voices, clear cooldown/danger state;
music chỉ đổi nếu target clip đổi. SFX không có loop. Menu UI confirm/back/error
là action cues, không có hover hoặc panel-specific sound.

## Event catalog

Clip paths dưới `app/Assets/StarShift/Audio/SFX/`. Gain là nominal trước user setting,
variation, duck và budget của pool tương ứng. Tier thấp hơn có priority cao hơn (Unity source
priority = tier × 40). Variation áp pitch **và** volume; warning/player stinger giữ
pitch/gain ổn định trước mix envelope. UI dùng source pool riêng nhưng cùng persisted SFX volume.

| Event / cue ID | Clip | Role / tier | Group | Gain | Max voices / interval | Variation |
|---|---|---|---|---|---|---|
| `Pulse` | `pulse.wav` | Player weapon / 2 | SFX | 0.22 | 2 / 0.085 s | ±3.5% pitch/volume |
| `Plasma` | `plasma.wav` | Player weapon / 2 | SFX | 0.52 | 2 / 0.12 s | ±3.5% pitch/volume |
| `Homing` | `homing.wav` | Player weapon / 2 | SFX | 0.34 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HomingImpact` | `homing-impact.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `PlayerHit` | `player-hit.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `ShieldAbsorb` | `shield-absorb.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `ShieldBreak` | `shield-break.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `Overheat` | `overheat.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `Vent` | `vent.wav` | Critical / stinger / 1 | SFX | 0.52 | 1 / 0.08 s | Stable |
| `PlayerDeath` | `player-death.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `HostileShot` | `hostile-shot.wav` | Action / impact / 3 | SFX | 0.065 | 2 / 0.16 s | ±3.5% pitch/volume |
| `InterceptorCharge` | `interceptor-charge.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `InterceptorDash` | `interceptor-dash.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `SniperCharge` | `sniper-charge.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `SniperFire` | `sniper-fire.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyCharge` | `heavy-charge.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyShot` | `heavy-shot.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossEnter` | `boss-enter.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `BossCannonWarning` | `boss-cannon-warning.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `BossBeamWarning` | `boss-beam-warning.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `BossCurtainWarning` | `boss-curtain-warning.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `BossShelterWarning` | `boss-shelter-warning.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `BossEscortWarning` | `boss-escort-warning.wav` | Danger / 0 | SFX | 0.55 | 1 / 0.15 s | Stable |
| `ArmorBreak` | `armor-break.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `EnemyDeath` | `enemy-death.wav` | Action / impact / 3 | SFX | 0.42 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyDeath` | `heavy-death.wav` | Action / impact / 3 | SFX | 0.5 | 2 / 0.08 s | ±3.5% pitch/volume |
| `AsteroidBreak` | `asteroid-break.wav` | Action / impact / 3 | SFX | 0.38 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossBurst` | `boss-burst.wav` | Action / impact / 3 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossFinal` | `boss-final.wav` | Critical / stinger / 1 | SFX | 0.65 | 1 / 0.08 s | Stable |
| `Coin` | `coin.wav` | Pickup / UI / 4 | SFX | 0.48 | 1 / 0.12 s | ±3.5% pitch/volume |
| `PowerUp` | `power-up.wav` | Pickup / UI / 4 | SFX | 0.36 | 2 / 0.08 s | ±3.5% pitch/volume |
| `ExtraLife` | `extra-life.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `Confirm` | `confirm.wav` | Pickup / UI / 4 | UI | 0.3 | 1 / 0.08 s | ±3.5% pitch/volume |
| `Back` | `back.wav` | Pickup / UI / 4 | UI | 0.3 | 1 / 0.08 s | ±3.5% pitch/volume |
| `Error` | `error.wav` | Pickup / UI / 4 | UI | 0.3 | 1 / 0.08 s | ±3.5% pitch/volume |
| `WarpStart` | `warp-start.wav` | Action / impact / 3 | SFX | 0.42 | 1 / 0.08 s | Stable |
| `WarpPass` | `warp-pass.wav` | Action / impact / 3 | SFX | 0.32 | 1 / 0.08 s | Stable |
| `WarpArrival` | `warp-arrival.wav` | Action / impact / 3 | SFX | 0.38 | 1 / 0.08 s | Stable |
| `Victory` | `victory.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `GameOver` | `game-over.wav` | Critical / stinger / 1 | SFX | 0.55 | 1 / 0.08 s | Stable |
| `TransitionWind` | `transition-wind.wav` | Action / impact / 3 | SFX | 0.16 | 1 / 0.6 s | Stable |

Player shots phát theo volley quan sát `ShotsFired`, không theo từng projectile.
Homing impact đọc `EnemyHealth.HitPresented`; reserve missile dùng Fired/Detonated.
Player hit/death lấy gate Dying; shield absorb/break lấy ShieldBreaks; MAX lấy cạnh
IsOverheated rồi một vent sau .22 s scaled time. Không phát vent mỗi frame.
Coin/power-up/Extra Life lấy pickup Collected. Generic hostile shot từ VolleyFired,
không từng viên. Directed/boss lấy phase events; warning một lần mỗi ability, không
lặp lại mỗi pulse barrage. Armor break theo threshold presentation hiện hữu.
Enemy/asteroid death từ EncounterRunner.EnemyDestroyed; boss dùng death progress:
hai burst ở start/.30, final ở .79, Victory qua completion event (không polling cửa sổ frame cuối); không đổi duration/handoff.

## Sources và attribution

- Music: Andrew Sitkov, Music Loops Mini Set / Mini Set 2, acquired qua
  [Mini Set](https://store.godotengine.org/asset/andrew-sitkov-music/music-loops-mini-set/)
  và [Mini Set 2](https://store.godotengine.org/asset/andrew-sitkov-music/music-loops-mini-set-2/).
- Kenney: Sci-Fi Sounds, Digital Audio, Interface Sounds, CC0;
  [original licenses](../../tools/audio/licenses/). Credit: “Sound effects by Kenney”.
- Little Robot Sound Factory: [Sci-Fi Sound Effects Library](https://opengameart.org/content/sci-fi-sound-effects-library),
  CC BY 3.0; [source/attribution](../../tools/audio/licenses/little-robot-sound-factory.txt),
  [license text](../../tools/audio/licenses/cc-by-3.0.txt). Credits trong game ghi tác giả,
  author URL, license URL và modifications.
- [Exporter](../../tools/audio/prepare.py) xử lý selected library files và ba original
  air cues (vent, missile launch, travel wind); không gen BGM. Ba air cues không có third-party sample/license dependency.
- Per-file source, original filename, edits, parameters và hashes chỉ nằm trong
  [source manifest](../../tools/audio/sources.json), không duplicate detailed provenance ở đây.

## Chủ động bỏ

Continuous engine, Ion/Rainbow trail, starfield/particles/decorative motion,
per-frame animation, hover mỗi button, mỗi menu panel, từng hostile bullet,
common-hit click mỗi viên Pulse, coin burst không throttle, vent spam,
travel song, boss explosion layers không giới hạn. Không tạo âm chỉ vì có VFX.

## Verification

Evidence follow-up: `.design/audio-followup/`; phase checkpoint trước: `.design/audio-final/`.
Automated check không thay listening.

- Export checks: decode/hash/peak/DC/SFX tails trên 44 clips.
- Follow-up `VerifyAudioTransitions`: PASS không ignition trước tap; đúng một sau tap;
  Pause/Resume/Continue không ignition lại; minor handoff wind chỉ khi thực sự bay;
  same-region có một Start/Arrival và giữ music source; Vanguard → region journey →
  tap tiếp cận → L5 chỉ một ignition cho cả lượt bay. Production save guard PASS.
- Hierarchy cleanup: 96 GameObjects trong 13 prefabs + scene đổi sang role names,
  không còn prefix `M8*` trong authored hoặc runtime hierarchy. Prefab data ngoài
  `m_Name`, file IDs và references giữ nguyên. Scene giữ riêng local DevFastStart
  selections của user ngoài commit; không đưa chúng vào production defaults.
- Edit checks: 41 cue bindings, warning priority/variation, prefab coverage, mixer groups.
- Runtime flow: Menu → Hangar/Shop/Settings → New Game → combat → Pause/Resume →
  death/respawn → Game Over → Retry → Menu; test storage và production byte guard.
- Final grouped Editor pass: PASS `flow`, `player`, `enemies`, `travel`, `boss`.
  Interceptor dưới common-SFX pressure và disable/reuse, Sniper/Bulwark, heat MAX +
  one vent, ba weapon, shield/pickup, L1→L2/L4→L5, Vanguard/Warden skills và staged
  final/Victory đều qua targeted assertions.
- Console: 0 errors, 0 warnings sau final pass. Production save guard PASS cho
  từng isolated probe; không thao tác campaign/profile thật để seed test.
- Native data/collider contract snapshot trước/sau: PASS, byte-identical.
- Latest mix pressure (BGM muted): Plasma nominal output ~.53, coin ~.47 khi
  common pool đầy; warning vẫn phát, vent không bị duck. sampled DSP stress peak .364 < .98.
  Boss replay PASS cả completion event khi cố ý bỏ frame cuối; không còn phụ thuộc
  polling .96 để phát Victory. Flow harness restore/unlock player trước artificial
  hit để không nhầm cinematic protection thành lỗi audio.
- Source audio peak max .68; sampled runtime DSP peaks < 1, có output thật.
  Đây là peak/headroom check, không thay listening judgement.
- Credits attribution có đủ author/license/modifications; preferred height 468
  trong body cao 600 ở authored layout, không đổi layout/art.
- Nearby persistence regression: `VerifyPersistence.Core` PASS.
- User listening: giữ Hub, thay Combat bằng lựa chọn B theo phản hồi mới nhất.
  Offline SFX revision demo đã gửi; final Editor mix/listening chưa verified qua công cụ.
  Không dùng automation để claim warning dễ nghe hoặc không khó chịu.
- Android, device FPS, full Campaign: ngoài scope, chưa chạy.

Unity entry points: `VerifyAudioFinal.Edit`, `Begin`, `BeginCombat`, `BeginFinal` qua Pipeline
`run_script`; chờ JSON terminal, không chạy hai probes đồng thời. Không chạy probe khi user đang chơi Campaign;
phải bắt đầu từ idle Menu hoặc phiên DevTest do probe sở hữu. Production authoring
`ConfigureAudioFinal.Apply` chỉ Edit Mode; không rerun để reset mix sau listening.
