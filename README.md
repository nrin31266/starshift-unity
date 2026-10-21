# StarShift

Game bắn tàu không gian 2D arcade cinematic, landscape mobile, ưu tiên Android.
Điều khiển relative drag và giữ để bắn. Campaign hiện có Mission01–08 qua hai
region; target design 12 level thuộc [game design](docs/design/game-design.md).

Gameplay và art hiện tại đã qua các lượt kiểm tra Unity đại diện; phase tiếp theo
là âm thanh. Phạm vi verification và giới hạn ở [dev/debug](docs/systems/dev-debug.md),
nguồn art/import ở [asset pipeline](docs/production/asset-pipeline.md).

## Mở project

Unity **6000.5.9f1**, revision `b57deb96f08d`, theo
[ProjectVersion.txt](app/ProjectSettings/ProjectVersion.txt).
Trong Unity Hub chọn **Add project from disk**, trỏ tới `app/`.
Scene chạy game là `Assets/StarShift/Scenes/02_Gameplay.unity`.
Dev Mode mặc định OFF; test nhanh dùng dropdown Dev Fast Start trong scene.

## Repository

```text
app/                   Unity project, runtime code và assets
docs/                 Canonical docs và router
tools/unity/          Importer, inspectors và regression probes hiện dùng
tools/assets/         Nguồn Aseprite, PixelLab provenance và công cụ export
tools/audio/          Nơi đặt công cụ offline cho phase âm thanh
tools/check_docs.py   Kiểm tra tài liệu
```

## Phát triển

Bắt đầu từ [AGENTS.md](AGENTS.md), qua [documentation router](docs/README.md).
Commit `.meta` cùng asset; giữ Editor/package versions hiện tại.
[Unity tools](tools/unity/README.md) mô tả cách chạy regression với storage riêng.
Không chạy lại migration cũ từ lịch sử Git lên tuning hiện tại.

```sh
python3 tools/check_docs.py
python3 tools/assets/m8/rebuild/inventory.py
```

Chưa chứng nhận build Android, FPS trên thiết bị hoặc final audio mix.
