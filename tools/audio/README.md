# Offline audio sources

Runtime selection và mixing contract ở [Audio Final](../../docs/presentation/audio-final.md).
Mỗi clip có nguồn, edit recipe và SHA-256 trong [sources.json](sources.json).
Không import whole pack hoặc audition intermediates vào Unity.

## Acquire và reproduce

- Andrew Sitkov: [Mini Set](https://store.godotengine.org/asset/andrew-sitkov-music/music-loops-mini-set/)
  và [Mini Set 2](https://store.godotengine.org/asset/andrew-sitkov-music/music-loops-mini-set-2/).
  Giữ `Short Space Casual Loop #1.wav`, `EDM Loop #1.wav`, `Drive With Me.wav`
  ở thư mục staging ngoài repo/Assets. Chỉ ba file được chọn được xuất sang Unity.
- Kenney: [Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds),
  [Digital Audio](https://kenney.nl/assets/digital-audio),
  [Interface Sounds](https://kenney.nl/assets/interface-sounds). Unzip lần lượt vào
  `STAGING/kenney/`, `STAGING/digital/`, `STAGING/interface/`, giữ `Audio/` bên trong.
- Little Robot Sound Factory: [Sci-Fi Sound Effects Library](https://opengameart.org/content/sci-fi-sound-effects-library).
  Từ folder `Wav/` của pack, copy các WAV được SELECTED dùng vào `STAGING/lrsf/`
  (flatten `Laser/` khi copy). Không import mp3, loop ambience hoặc whole pack.
- License gốc và records ở [licenses/](licenses/). Andrew cho phép dùng thương mại
  và phân phối kèm project; không claim authorship, bán/register hoặc phân phối
  nhạc riêng. Kenney CC0. Little Robot Sound Factory CC BY 3.0: credit author,
  source/author URL, license và ghi modifications; Credits trong game chứa attribution.
- Không giữ original procedural tone sweep/stinger sau user feedback. Cả 40 cue
  đều chọn từ thư viện; trim/reverse/EQ/fade và normalize bằng ffmpeg/Python.

Yêu cầu Python 3 và `ffmpeg`/`ffprobe`; không cần pip dependency:

```sh
python3 tools/audio/prepare.py /absolute/path/to/staging
python3 tools/audio/check.py
```

Generator chỉ overwrite các export audio được chọn. Chạy khi Editor không Play,
refresh và reimport bằng `ConfigureAudioFinal.Apply` qua Unity Pipeline; không
chạy để reset tuning sau một listening pass. `.meta` do Unity tạo, giữ GUID.

`check.py` kiểm hash, decode, finite samples, peak/headroom, DC và SFX tail;
không chứng minh mix dễ nghe hoặc hợp gu. Không dùng kết quả này thay listening pass.
