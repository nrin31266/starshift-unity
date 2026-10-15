# StarShift

Game bắn tàu không gian 2D arcade cinematic, landscape mobile, ưu tiên Android.
Campaign dự kiến 12 level qua 3 region, đổi hướng chiến đấu Up → Right → Up;
điều khiển relative drag và auto-fire.

Hiện trạng: đã khởi tạo scaffold và review canonical documentation; Unity vẫn ở
scene mẫu, chưa có gameplay. Bắt đầu task từ [AGENTS.md](AGENTS.md), qua
[documentation router](docs/README.md) rồi đọc canonical docs liên quan.

## Mở project

Unity **6000.5.9f1**, revision `b57deb96f08d`, đọc từ
[ProjectVersion.txt](app/ProjectSettings/ProjectVersion.txt).
Trong Unity Hub chọn **Add project from disk**, trỏ tới thư mục `app/`, rồi mở bằng
đúng Editor version trên. Repository root không phải Unity project root.

## Repository

```text
README.md / AGENTS.md   Giới thiệu và quy tắc làm việc
app/                   Unity project
docs/                  Canonical docs và router
tools/audio/           Công cụ SFX offline trong tương lai
tools/assets/          Công cụ asset offline trong tương lai
tools/check_docs.py    Kiểm tra invariant tài liệu
```

## Phát triển

Đọc [AGENTS.md](AGENTS.md), sau đó router và canonical owner của task.
Commit `.meta` cùng asset; cache Unity và IDE đã được ignore.
Không tự upgrade Editor/package. Chưa xác minh quy trình build Android hay gameplay
runtime; hiện chưa có build command được chuẩn hóa.

Kiểm tra Markdown ở root, `docs/`, `tools/` (local links và anchor ATX cơ bản):

```sh
python tools/check_docs.py
```
