# CTU Scheduler — Agent Core Guidelines

## 1. Project Overview & Identity
- **Repository**: CTU-Scheduler (Desktop application for Can Tho University course scheduling and timetable generation).
- **Core Stack**: .NET 10 · C# latest · Avalonia 12 · ReactiveUI · Microsoft.Extensions.DependencyInjection · Serilog.
- **Solution Layout**: Single primary project `CTUScheduler` (divided by namespaces: `Core`, `Infrastructure`, `AppServices`, `Presentation`), `CTUScheduler.Desktop` (bootstrapper executable), and `tests/CTUScheduler.Tests`.

## 2. Decision-Making & Behavior Policy
- **Autonomous Technical Decisions**:
  - Tự chủ áp dụng các giải pháp kỹ thuật tiêu chuẩn, mức độ rủi ro thấp theo mẫu sẵn có trong codebase.
  - Chỉ hỏi người dùng khi: Yêu cầu mơ hồ, có sự đánh đổi kiến trúc lớn, bổ sung thư viện phụ thuộc mới, hoặc thay đổi có tính chất breaking change.
- **Kỷ luật phạm vi (Scope Discipline)**:
  - Chỉ chỉnh sửa các tệp liên quan trực tiếp đến tác vụ được giao.
  - Không tiện tay tái cấu trúc (refactor) các đoạn mã không liên quan. Nếu phát hiện vấn đề cần cải tiến ngoài phạm vi, ghi chú lại ở cuối câu trả lời.
- **Dẫn chứng thực tế (Evidence & Objectivity)**:
  - Mọi khẳng định về mã nguồn hiện có phải được kiểm chứng bằng CodeGraph, XERJ hoặc đọc file trước khi phát biểu.
  - Luôn trích dẫn nguồn cụ thể dạng `file:line` hoặc `Folder/File.cs`. Không đoán mò hoặc tự suy diễn cách triển khai.

## 3. Specialized Rules Organization
Hệ thống quy tắc chi tiết của dự án được phân tách thành các module chuyên biệt trong thư mục `.agents/rules/`, tự động kích hoạt theo ngữ cảnh qua YAML Frontmatter:

- [.agents/rules/architecture.md](.agents/rules/architecture.md): Bất biến luồng phân tầng (`Desktop → Presentation → AppServices → Core ← Infrastructure`), Core framework-free, Composition Root, ReactiveUI MVVM, DI lifetimes, `OperationResult`, và bảo tồn thuật toán lập lịch.
- [.agents/rules/coding.md](.agents/rules/coding.md): Quy chuẩn C# (tuân thủ `.editorconfig`), quản lý `Span<T>` theo C# 13, xUnit/FluentAssertions/NSubstitute testing conventions, Serilog structured logging, và performance threading.
- [.agents/rules/tools.md](.agents/rules/tools.md): Verification Loop (Definition of Done: build + test), RTK CLI proxy (optional & fallback), và cơ chế điều hướng mã nguồn CodeGraph / XERJ với fallback tiêu chuẩn.
- [.agents/rules/planning.md](.agents/rules/planning.md): Quy trình lập kế hoạch tác vụ phức tạp, Implementation Plan Artifact, duy trì workspace plan tại `docs/plans/`, và giao thức khôi phục khi lệch trạng thái.
- [.agents/rules/communication.md](.agents/rules/communication.md): Quy ước giao tiếp trực diện, gãy gọn, loại bỏ văn mẫu AI, và sử dụng tiếng Việt tự nhiên.
