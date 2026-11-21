---
description: "C# implementation practices, xUnit/FluentAssertions testing conventions, Serilog logging, and performance guidelines."
trigger: glob
globs: "**/CTUScheduler*/**/*.cs, **/CTUScheduler*/**/*.axaml, **/tests/**/*.cs, **/benchmarks/**/*.cs"
---

# Coding Conventions & Quality Guidelines

> [!IMPORTANT]
> **Phạm vi áp dụng**: Chỉ áp dụng cho mã nguồn viết tay. Agent phải chủ động bỏ qua mã tự sinh (`*.g.cs`, `*.AssemblyInfo.cs`), các thư mục build output (`bin/**`, `obj/**`, `artifacts/**`), và các thư viện vendor/third-party.

## 1. C# Implementation Standards

- **Syntax & Formatting**:
  - Tuân thủ `.editorconfig` làm nguồn chân lý (file-scoped namespaces, quy ước đặt tên `I*` cho interface, `_camelCase` cho private field, quy tắc dùng `var`, tự động dọn `using` thừa).
- **Kiểu dữ liệu & Collections**:
  - `record`: Ưu tiên cho các immutable domain model, DTO hoặc state snapshot; dùng `class` thông thường khi cần quản lý trạng thái mutable hoặc entity.
  - `IReadOnlyList<T>`: Mặc định cho các public API cần truy cập danh sách chỉ đọc theo chỉ mục.
  - `IList<T>`, `ISet<T>`: Dùng khi hợp đồng API thực sự đòi hỏi khả năng thêm/xóa phần tử hoặc đảm bảo tính duy nhất.
  - `Span<T>` / `ReadOnlySpan<T>`: Tuân thủ các giới hạn của `ref struct` theo phiên bản C# đang sử dụng (C# 13 / .NET 10). Chỉ dùng trong các đoạn xử lý vùng nhớ liên tục, thuật toán parsing hiệu năng cao. Cho phép sử dụng trong `async` method nếu biến không tồn tại xuyên qua các điểm suspension (như `await` hoặc `yield`).
- **Async Safety**:
  - `ConfigureAwait(false)`: Áp dụng ở tầng non-UI (Core, Infrastructure, AppServices) khi phù hợp để tránh context capture; không ép buộc mù quáng ở nơi cần đồng bộ ngữ cảnh luồng.
  - Cấm `async void` (trừ event handler UI bắt buộc do chữ ký framework; phải kèm comment giải thích).
  - Cấm `.Result` hoặc `.Wait()` — luôn dùng `await`.
  - Null-safety: Không dùng `!` (null-forgiving operator) trừ khi có comment giải thích rõ lý do an toàn.

## 2. Unit Testing

- **Stack kiểm thử**: `xUnit` + `FluentAssertions` + `NSubstitute` (theo cấu hình tại `tests/CTUScheduler.Tests/CTUScheduler.Tests.csproj`).
- **Quy ước đặt tên test**: `MethodName_StateUnderTest_ExpectedBehavior`.
- **Cấu trúc test**: Phân định rõ 3 khối Arrange - Act - Assert (ngăn cách bằng dòng trống).
- **Phạm vi kiểm thử**:
  - Tập trung test các thuật toán lập lịch, business rules, validator, state machine, converter và các trường hợp biên (edge cases).
  - Không ép test máy móc 100% các property getter/setter hoặc DTO tầm thường.
- **Tính độc lập**: Unit test không tạo side effect thực tế (không gọi network thật, không ghi đè file hệ thống thật).

## 3. Structured Logging (Serilog)

- Tiêm `ILogger<T>` qua constructor.
- Bắt buộc dùng static message templates với named properties:
  ```csharp
  Log.Information("Loaded {CourseCount} courses for semester {SemesterId}", count, semesterId);
  ```
- Cấm dùng string interpolation hoặc cộng chuỗi trong message template của câu lệnh log.
- *Lưu ý*: Không cấm string interpolation ngoài ngữ cảnh log (trong code C# thông thường, nối chuỗi `$""` là hoàn toàn hợp lệ).

## 4. Performance & Threading

- **Tác vụ CPU-bound nặng** (thuật toán sinh tổ hợp lịch, chấm điểm ma trận): Chạy trên `Task.Run()` để không làm đơ UI thread.
- **Tác vụ I/O bất đồng bộ** (mạng HTTP, đọc file, parse PDF): Dùng `async/await` chuẩn, không bọc `Task.Run()` thừa thãi.
- **Cập nhật UI từ luồng nền trong Rx**: Dùng `.ObserveOn(RxApp.MainThreadScheduler)`. Dùng `Dispatcher.UIThread.InvokeAsync()` hoặc `Post()` khi tương tác trực tiếp với Avalonia UI ngoài observable stream.
- **Bộ nhớ**: Giải phóng (Dispose) các đối tượng đồ họa/hình ảnh (`Bitmap`) sau khi dùng. Tránh closure capture các object lớn trong Rx chain kéo dài.

## 5. Comments & Documentation

- **Comment "why", không comment "what"**: Chỉ viết chú thích khi logic phức tạp, có magic number, workaround vì lý do đặc biệt hoặc ràng buộc không thể diễn đạt bằng mã.
- Đánh dấu giải pháp rút gọn có chủ đích bằng `// ponytail: <lý do và hướng nâng cấp>`.
- **XML doc (`///`)**: Viết trên các public/internal member trong `Core` và `AppServices` khi tên hàm chưa thể hiện hết ràng buộc tham số, ngoại lệ hoặc thuật toán. Bỏ qua khi tên hàm đã hoàn toàn tự tường minh (`IsLoading`, `Dispose()`).
