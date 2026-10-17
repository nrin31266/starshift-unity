# StarShift — Agent rules

## Read first

- Task không tầm thường: đọc file này, rồi `docs/README.md`.
- Đọc canonical docs liên quan domain; không cần đọc mọi docs cho mỗi task.
- Source of truth: `AGENTS.md` → `docs/README.md` → canonical docs liên quan.
- Yêu cầu hiện tại được ưu tiên; thay đổi contract có chủ ý phải cập nhật owner.

## Repository boundaries

- Unity project root là `app/`; không tạo `Assets/`, `Packages/`, `ProjectSettings/` ở root.
- Documentation nằm trong `docs/`.
- Offline utilities nằm trong `tools/`; Editor-only tooling chỉ vào Unity khi cần thật.
- Giữ file user và GUID; commit `.meta` cùng asset, không commit cache/build output.

## Documentation rules

- Một detailed fact có một canonical owner theo `docs/README.md`.
- File khác chỉ dùng summary ngắn và relative link.
- Không duplicate detailed table hoặc contract.
- Canonical headings không đánh số; canonical filenames lowercase kebab-case.
- Giữ đúng `[LOCKED]`, `[TUNABLE]`, `[TBD]`, `[PROVISIONAL]`, `[FUTURE]`.
- Không tự chốt TBD hoặc nâng provisional thành locked.
- Intentional gameplay/architecture/save contract change: cập nhật owner cùng task.
- Implementation bug phải sửa theo contract; không sửa docs để hợp thức hóa bug.
- Docs conflict: canonical owner theo router thắng; sửa stale reference.
- Conflict chưa rõ ownership hoặc ảnh hưởng lớn: báo rõ, không âm thầm chọn.
- Chỉ tách docs khi thực sự khó review; cập nhật router và links trong cùng task.

## Code rules

- Technical identifiers dùng English: classes, methods, properties, fields, enums.
- Filenames và config IDs kỹ thuật cũng dùng English.
- Short explanatory code comments viết bằng tiếng Việt.
- Comment chỉ giải thích intent, constraint, non-obvious behavior hoặc rationale.
- Không comment obvious code; contract dài thuộc docs.

## Engineering rules

- Dùng Unity component-based architecture; tránh God MonoBehaviour.
- Tách config data khỏi mutable runtime state; không dùng definition asset làm save.
- Ưu tiên data-driven content và tunable config thay magic number rải rác.
- Pool high-frequency objects; profile trước khi thêm tối ưu phức tạp.
- Không ECS/DOTS khi chưa có nhu cầu thật.
- Không thêm architecture/framework enterprise, DI hay event bus framework chung.
- Không premature abstraction; không implement `[FUTURE]` khi chưa được yêu cầu.
- Không tự thay Editor/package versions ngoài scope task.

## Review Skill Usage

Skill theo mức độ thay đổi; chọn công cụ review nhỏ nhất phù hợp rủi ro.
Không chồng nhiều review skills lên thay đổi nhỏ nếu không có lý do cụ thể.

- `unity-scriptdesign`: dùng khi tạo hoặc refactor đáng kể gameplay/system C#;
  tập trung responsibility, lifecycle, coupling và Inspector/data boundaries.
  Không bắt buộc cho config/tuning, copy/text, bug fix nhỏ, test-only hoặc field/property đơn giản.
- `design-review`: dùng khi tạo hoặc thay đổi đáng kể UI/presentation/layout và cần
  đánh giá hình ảnh. Xem Game View/screenshot: spacing, padding, hierarchy, icon size,
  alignment, density và readability. Không dùng cho gameplay/code không liên quan.
- Ponytail: dùng cho architecture xuyên hệ thống, phase lớn hoặc thay đổi đáng kể
  nhiều owners/systems. Không bắt buộc cho isolated fixes; không tự audit toàn repo.
  Plugin bật qua `.codex/config.toml`; tái sử dụng component hiện có, không cắt validation
  hoặc verification để giảm dòng code.
- [pr-review](.agents/skills/pr-review/SKILL.md): dùng ở phase/PR checkpoint trước merge,
  là integration review cuối, không là review cho từng task. Khi user yêu cầu review PR,
  chạy skill trong worktree riêng, tách static checks với Unity runtime evidence.

Unity-native wiring/scene/prefab và runtime verification tiếp tục dùng Unity MCP.
Không tự cài skill chỉ để đáp ứng một thay đổi không cần skill đó.

## Core invariants

- Landscape mobile, Android-first; relative drag và hold-to-fire.
- Player dùng lives; enemy dùng HP.
- Death giảm level của active weapon; chi tiết ở player combat owner.
- Checkpoint ở đầu wave/encounter; không snapshot world giữa wave.
- Replay/dev không phá CampaignSave hoặc PlayerProfile; chỉ ghi theo contract rõ ràng.
- `app/` luôn là Unity root; các invariant này chỉ là summary, xem canonical owner.

## Verification

- Compile/test phù hợp khi environment cho phép; inspect Unity Console khi cần.
- Kiểm tra canonical docs và regression liên quan khi đổi contract.
- Feature late-game phải có đường test nhanh qua DevConfig.
- Không claim runtime verified nếu chưa chạy; báo rõ kiểm tra và phần chưa verify.
- Không claim FPS trên thiết bị thật nếu chưa profile.
- Không tự commit, push, tạo branch hoặc rewrite history khi chưa được yêu cầu.

## Verification Cost

Test theo blast radius; verification phải tương xứng với thay đổi.

- Small/local fix: compile, targeted regression và một nearby smoke nếu cần.
- Medium system change: targeted regressions, smoke subsystem bị ảnh hưởng và một end-to-end flow.
- Large phase/architecture change: affected matrix, end-to-end flow, broader regression
  và review skill/PR review phù hợp.

Không chạy lại full historical suite sau mỗi sửa nhỏ. Chỉ lặp expensive tests đã pass
khi code vừa đổi có thể ảnh hưởng chúng, đang điều tra failure trước đó, hoặc đây là
final phase/PR checkpoint. Checkpoint không tự động có nghĩa chạy mọi matrix không liên quan.
Gom các fix nhỏ liên quan rồi chạy một grouped verification pass. Nếu targeted test
fail, sửa và chạy lại test đó; chỉ mở rộng verification khi failure hoặc thay đổi mới đòi hỏi.
