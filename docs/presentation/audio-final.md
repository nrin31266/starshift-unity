# Audio Final — StarShift

## Phạm vi và trạng thái

Integration checkpoint 2026-10-21 từ `main` `c330ea23`, worktree sạch trước task.
Owner của concrete audio event bindings, clip selection, mixing values và QA evidence.
Feedback choreography chung thuộc [audio/VFX](audio-vfx.md); assets/licensing workflow
thuộc [asset pipeline](../production/asset-pipeline.md#python-sfx-và-bgm-import).

Art freeze; không đổi gameplay data, collider, save schema, Editor/package version.
Toàn bộ mix values dưới đây là `[TUNABLE]`. Chưa khóa lựa chọn âm sắc khi chưa nghe.
**Integration / automated Editor QA: PASS.** Nhóm BGM candidate mới được user
nhận xét hay/đa dạng; 40 SFX/stingers dùng selected library clips. Final subjective
listening/readability của toàn bộ mix chưa được xác nhận qua công cụ. Không dùng
kết quả automation để claim người nghe đã duyệt palette, warning readability hoặc
“không khó chịu”. Không Android/FPS/device/full Campaign trong phase này.

## Music selection

| Event / state | Selected clip / original title | Group | Gain / voices | Transition |
|---|---|---|---|---|
| Menu, Hangar, Shop, Settings từ Menu | `Music/bgm-hub.wav` — Short Space Casual Loop #1 | Music | .20 × Music setting; 1 stable loop | Giữ clip/cursor qua panels |
| Gameplay, Mission/region travel | `Music/bgm-combat.wav` — EDM Loop #1 | Music | .20 × Music setting; 1 stable loop | Giữ Combat qua Mission/region, travel duck |
| Boss encounter | `Music/bgm-boss.wav` — Drive With Me | Music | .20 × Music setting; 1 stable loop | Combat ↔ Boss crossfade |
| Boss final destruction | `SFX/victory.wav` — Little Robot Sound Factory jingle | SFX | Xem catalog | Sau staged bursts/final hit |
| Game Over | `SFX/game-over.wav` — Little Robot Sound Factory jingle | SFX | Xem catalog | Sau natural death, delay .30 s từ overlay |

Ba loop từ Andrew Sitkov **Music Loops Mini Set / Mini Set 2**. License gốc cho phép dùng
trong game thương mại và phân phối kèm project; cấm claim authorship, register/sell
hoặc phân phối nhạc riêng. [Source/reproduction](../../tools/audio/README.md),
[license PDF](../../tools/audio/licenses/andrew-sitkov.pdf),
[clip provenance + hashes](../../tools/audio/sources.json).
Không ship cả pack. WAV source được peak/RMS-normalize bằng static gain, nối loop
20 ms; Unity import Vorbis streaming, preload off. SFX mono PCM, decompress on load.
Selected exports gồm đúng ba BGM và 40 SFX/stingers, không có audition intermediate
trong Assets. Không giữ clip synth tự tạo; nguồn là Kenney Sci-Fi/Digital/Interface
và Little Robot Sound Factory, trim/EQ/reverse/fade theo từng family.

## Runtime mix và lifecycle

`AudioPalette` chứa immutable authoring data. `AudioDirector` giữ mutable voices,
cooldown, music cursor và envelope; không ghi definition/save/gameplay state.
Player/Enemy/Game audio presentation dùng events và đọc state. Cannon phase ngắn
được bắt bằng event để không bỏ qua Attack trong cùng physics tick. Pool disable
unsubscribe events; enable reset phase/heat/death-beat tracking.

Music dùng hai sources, linear crossfade .85 s theo unscaled time. Khi Start Game,
Hub fade .85 s → warp start → Combat bắt đầu fade in ở 1.35 s; không trì hoãn gameplay.
Death còn lives không đổi track/cursor. Pause target music .30; Resume về 1.
Travel target .55, boss post-death .15, Game Over .12. Danger cue target .45.
Những duck đồng thời dùng mức nhỏ nhất; attack 5 gain/s, release 1.6 gain/s.
`GameplaySession` tiếp tục sở hữu `Time.timeScale`/`AudioListener.pause`; audio sources
có `ignoreListenerPause` để envelope/UI/music và one-shot tails vẫn chạy.
Game Over giữ battlefield và BGM thấp; Retry phục hồi track tương ứng, Menu crossfade Hub.
Không thêm ambience/engine loop chỉ để lấp overlay.

16 SFX voices được tạo một lần: 4 danger, 2 player-critical/stinger, 8 common, 2 UI.
Common không mượn slot danger/player-critical. Critical pool đầy thay voice cũ nhất
trong chính pool; per-cue cap/rate vẫn áp dụng. Không PlayOneShot chồng vô hạn.
Common bị duck .25 khi danger phát, .12 khi Pause/Game Over; UI vẫn rõ.
Aggregate gain budgets: danger .50, critical .35, common .20, UI .12.
Cộng nominal volume của voices đang phát rồi scale xuống nếu vượt budget; giảm
ngay để giữ headroom, phục hồi 2 gain/s. SFX export peak .68, BGM peak ≤ .68.
Music/SFX settings giữ nguyên schema; UI dùng SFX preference.

New run/Retry và về frontend stop gameplay voices, clear cooldown/danger state;
music chỉ đổi nếu target clip đổi. SFX không có loop. Menu UI confirm/back/error
là action cues, không có hover hoặc panel-specific sound.

## Event catalog

Clip paths dưới `app/Assets/StarShift/Audio/SFX/`. Gain là nominal trước user setting,
variation, duck và aggregate budget. Tier thấp hơn có priority cao hơn (Unity source
priority = tier × 40). Variation áp pitch **và** volume; warning/player stinger giữ
pitch/gain ổn định trước mix envelope. UI dùng source pool riêng nhưng cùng persisted SFX volume.

| Event / cue ID | Clip | Role / tier | Group | Gain | Max voices / interval | Variation |
|---|---|---|---|---|---|---|
| `Pulse` | `pulse.wav` | Player weapon / 2 | SFX | 0.19 | 2 / 0.085 s | ±3.5% pitch/volume |
| `Plasma` | `plasma.wav` | Player weapon / 2 | SFX | 0.19 | 2 / 0.12 s | ±3.5% pitch/volume |
| `Homing` | `homing.wav` | Player weapon / 2 | SFX | 0.19 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HomingImpact` | `homing-impact.wav` | Player weapon / 2 | SFX | 0.19 | 2 / 0.08 s | ±3.5% pitch/volume |
| `PlayerHit` | `player-hit.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `ShieldAbsorb` | `shield-absorb.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `ShieldBreak` | `shield-break.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `Overheat` | `overheat.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `Vent` | `vent.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `PlayerDeath` | `player-death.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `HostileShot` | `hostile-shot.wav` | Heavy / action / 3 | SFX | 0.065 | 2 / 0.16 s | ±3.5% pitch/volume |
| `InterceptorCharge` | `interceptor-charge.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `InterceptorDash` | `interceptor-dash.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `SniperCharge` | `sniper-charge.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `SniperFire` | `sniper-fire.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyCharge` | `heavy-charge.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyShot` | `heavy-shot.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossEnter` | `boss-enter.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossCannonWarning` | `boss-cannon-warning.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `BossBeamWarning` | `boss-beam-warning.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `BossCurtainWarning` | `boss-curtain-warning.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `BossShelterWarning` | `boss-shelter-warning.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `BossEscortWarning` | `boss-escort-warning.wav` | Danger / 0 | SFX | 0.48 | 1 / 0.15 s | Stable |
| `ArmorBreak` | `armor-break.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `EnemyDeath` | `enemy-death.wav` | Heavy / action / 3 | SFX | 0.13 | 2 / 0.08 s | ±3.5% pitch/volume |
| `HeavyDeath` | `heavy-death.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `AsteroidBreak` | `asteroid-break.wav` | Heavy / action / 3 | SFX | 0.1 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossBurst` | `boss-burst.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `BossFinal` | `boss-final.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `Coin` | `coin.wav` | Pickup / UI / 4 | SFX | 0.11 | 2 / 0.12 s | ±3.5% pitch/volume |
| `PowerUp` | `power-up.wav` | Pickup / UI / 4 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `ExtraLife` | `extra-life.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `Confirm` | `confirm.wav` | Pickup / UI / 4 | UI | 0.22 | 1 / 0.08 s | ±3.5% pitch/volume |
| `Back` | `back.wav` | Pickup / UI / 4 | UI | 0.22 | 1 / 0.08 s | ±3.5% pitch/volume |
| `Error` | `error.wav` | Pickup / UI / 4 | UI | 0.22 | 1 / 0.08 s | ±3.5% pitch/volume |
| `WarpStart` | `warp-start.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `WarpPass` | `warp-pass.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `WarpArrival` | `warp-arrival.wav` | Heavy / action / 3 | SFX | 0.24 | 2 / 0.08 s | ±3.5% pitch/volume |
| `Victory` | `victory.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |
| `GameOver` | `game-over.wav` | Player-critical / stinger / 1 | SFX | 0.45 | 1 / 0.08 s | Stable |

Player shots phát theo volley quan sát `ShotsFired`, không theo từng projectile.
Homing impact đọc `EnemyHealth.HitPresented`; reserve missile dùng Fired/Detonated.
Player hit/death lấy gate Dying; shield absorb/break lấy ShieldBreaks; MAX lấy cạnh
IsOverheated rồi một vent sau .22 s scaled time. Không phát vent mỗi frame.
Coin/power-up/Extra Life lấy pickup Collected. Generic hostile shot từ VolleyFired,
không từng viên. Directed/boss lấy phase events; warning một lần mỗi ability, không
lặp lại mỗi pulse barrage. Armor break theo threshold presentation hiện hữu.
Enemy/asteroid death từ EncounterRunner.EnemyDestroyed; boss dùng death progress:
hai burst ở start/.30, final ở .79, Victory ở .96; không đổi duration/handoff.

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
- [Exporter](../../tools/audio/prepare.py) chỉ xử lý selected library files, không
  synth tone/stinger mới và không gen BGM.
- Per-file source, original filename, edits, parameters và hashes chỉ nằm trong
  [source manifest](../../tools/audio/sources.json), không duplicate detailed provenance ở đây.

## Chủ động bỏ

Continuous engine, Ion/Rainbow trail, starfield/particles/decorative motion,
per-frame animation, hover mỗi button, mỗi menu panel, từng hostile bullet,
common-hit click mỗi viên Pulse, coin burst không throttle, vent spam,
travel song, boss explosion layers không giới hạn. Không tạo âm chỉ vì có VFX.

## Verification

Evidence local ignored: `.design/audio-final/`. Automated check không thay listening.

- Export checks: decode/hash/peak/DC/SFX tails trên 43 clips.
- Edit checks: 40 cue bindings, warning priority/variation, prefab coverage, mixer groups.
- Runtime flow: Menu → Hangar/Shop/Settings → New Game → combat → Pause/Resume →
  death/respawn → Game Over → Retry → Menu; test storage và production byte guard.
- Final grouped Editor pass: PASS `flow`, `player`, `enemies`, `travel`, `boss`.
  Interceptor dưới common-SFX pressure và disable/reuse, Sniper/Bulwark, heat MAX +
  one vent, ba weapon, shield/pickup, L1→L2/L4→L5, Vanguard/Warden skills và staged
  final/Victory đều qua targeted assertions.
- Console: 0 errors, 0 warnings sau final pass. Production save guard PASS cho
  từng isolated probe; không thao tác campaign/profile thật để seed test.
- Native data/collider contract snapshot trước/sau: PASS, byte-identical.
- Source audio peak max .68; sampled runtime DSP peaks < 1, có output thật.
  Đây là peak/headroom check, không thay listening judgement.
- Credits attribution có đủ author/license/modifications; preferred height 468
  trong body cao 600 ở authored layout, không đổi layout/art.
- Nearby persistence regression: `VerifyPersistence.Core` PASS.
- User listening: nhóm năm candidate BGM được nhận xét hay/đa dạng, chọn ba bài
  theo role ở trên. SFX library preview đã gửi; full final mix/listening chưa verified
  qua công cụ, không dùng automation để claim warning dễ nghe hoặc không khó chịu.
- Android, device FPS, full Campaign: ngoài scope, chưa chạy.

Unity entry points: `VerifyAudioFinal.Edit`, `Begin`, `BeginCombat`, `BeginFinal` qua Pipeline
`run_script`; chờ JSON terminal, không chạy hai probes đồng thời. Không chạy probe khi user đang chơi Campaign;
phải bắt đầu từ idle Menu hoặc phiên DevTest do probe sở hữu. Production authoring
`ConfigureAudioFinal.Apply` chỉ Edit Mode; không rerun để reset mix sau listening.
