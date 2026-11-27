# Hướng dẫn đóng góp — CTU-Scheduler

Cảm ơn bạn đã quan tâm đến CTU-Scheduler! Tài liệu này mô tả quy trình và quy tắc dành cho contributor để mọi thứ diễn ra nhất quán và dễ review.

---

## Mục lục

- [Yêu cầu môi trường](#yêu-cầu-môi-trường)
- [Bắt đầu nhanh](#bắt-đầu-nhanh)
- [Quy tắc kiến trúc](#quy-tắc-kiến-trúc)
- [Quy ước code](#quy-ước-code)
- [Viết test](#viết-test)
- [Quy trình đóng góp (Git Workflow)](#quy-trình-đóng-góp-git-workflow)
- [Commit Convention](#commit-convention)
- [Pull Request](#pull-request)
- [Báo cáo lỗi & đề xuất tính năng](#báo-cáo-lỗi--đề-xuất-tính-năng)
- [Phạm vi ngoài giới hạn](#phạm-vi-ngoài-giới-hạn)

---

## Yêu cầu môi trường

| Công cụ | Phiên bản tối thiểu |
|---|---|
| .NET SDK | 10.0 |
| IDE | Rider / Visual Studio 2022+ / VS Code (C# Dev Kit) |
| Git | 2.x |

Kiểm tra cài đặt:

```bash
dotnet --version   # phải >= 10.0
```

---

## Bắt đầu nhanh

```bash
# 1. Fork repo trên GitHub, sau đó clone fork của bạn
git clone https://github.com/<your-username>/CTU-Scheduler.git
cd CTU-Scheduler

# 2. Build toàn bộ solution
dotnet build "CTU Scheduler.slnx"

# 3. Chạy test suite
dotnet test tests/CTUScheduler.Tests

# 4. Khởi chạy ứng dụng
dotnet run --project CTUScheduler.Desktop
```

Build và test phải **pass hoàn toàn** trước khi bắt đầu thay đổi. Nếu CI đang broken, hãy báo cáo qua Issues trước.

---

## Quy tắc kiến trúc

Project tuân theo **Clean Architecture** với hướng phụ thuộc một chiều:

```
Desktop → Presentation → AppServices → Core ← Infrastructure
```

Các nguyên tắc bắt buộc:

- **`Core` độc lập hoàn toàn**: Không được import Avalonia, ReactiveUI, hoặc bất kỳ infrastructure framework nào vào layer `Core`. Chỉ chứa business logic, domain model, interfaces trừu tượng.
- **DI qua constructor**: Constructor injection là bắt buộc. Nghiêm cấm Service Locator (`App.Current.Services`, static resolver).
- **Composition Root tại `Desktop`**: Mọi DI registration tập trung tại `CTUScheduler.Desktop`, các layer expose extension methods (`AddAppServices()`, `AddInfrastructure()`).
- **Navigation qua `INavigationRegionManager`**: Không gọi `Router.Navigate` trực tiếp.
- **Error handling**: Dùng `OperationResult` (tại `Core/Models/Shared/Results/`) cho các lỗi domain có thể dự đoán (đụng lịch, lớp đầy, dữ liệu không hợp lệ). Exception vẫn dùng cho lỗi kỹ thuật không phục hồi được (network drop, I/O crash).

> **Thuật toán xếp lịch** (`Core/Models/Academic/`, `IScheduleScorer`, `ScheduleValidator`) là vùng nhạy cảm. Không được thay đổi thuật toán, scoring, hay priority mà không có yêu cầu rõ ràng và test kiểm chứng.

---

## Quy ước code

`.editorconfig` ở gốc repo là **nguồn sự thật duy nhất** cho formatting. IDE của bạn sẽ tự áp dụng nếu hỗ trợ EditorConfig.

Các điểm chính:

**Naming**
- Interface: tiền tố `I` (ví dụ: `IScheduleService`) — bắt buộc, lỗi build.
- Private field: `_camelCase`.
- File-scoped namespace: `namespace CTUScheduler.Core.Models;` (không dùng block namespace).

**Types**
- `record`: ưu tiên cho domain model bất biến, DTOs, state snapshots.
- `IReadOnlyList<T>`: mặc định cho public API trả về collection chỉ đọc.
- `Span<T>` / `ReadOnlySpan<T>`: chỉ dùng cho xử lý memory hiệu năng cao; không dùng qua suspension point (`await`, `yield`).

**Async**
- Không `async void` (ngoại trừ UI event handler, phải có comment giải thích).
- Không `.Result` hoặc `.Wait()` — luôn `await`.
- `ConfigureAwait(false)` ở các layer không phải UI (Core, Infrastructure, AppServices).

**Logging**
- Inject `ILogger<T>` qua constructor.
- Dùng message template với named properties:
  ```csharp
  // ✅ Đúng
  _logger.LogInformation("Loaded {CourseCount} courses for {SemesterId}", count, semesterId);
  
  // ❌ Sai
  _logger.LogInformation($"Loaded {count} courses for {semesterId}");
  ```

**Threading**
- CPU-bound (giải thuật xếp lịch): `Task.Run()`.
- Async I/O (HTTP, file): `async/await` thuần, không wrap `Task.Run()` thừa.
- Dispatch về UI thread từ Rx: `.ObserveOn(RxApp.MainThreadScheduler)`.

---

## Viết test

**Stack**: `xUnit` + `FluentAssertions` + `NSubstitute`

**Naming convention**: `MethodName_StateUnderTest_ExpectedBehavior`

```csharp
[Fact]
public void GenerateSchedule_WhenCoursesConflict_ReturnsFailureResult()
{
    // Arrange
    var courses = ...;

    // Act
    var result = _scheduler.GenerateSchedule(courses);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be(ScheduleError.TimeConflict);
}
```

**Yêu cầu**:
- Test không được có side effect bên ngoài (không gọi HTTP thật, không ghi file thật).
- Ưu tiên test scheduling algorithm, business rule, validator, state machine, converter.
- Không bắt buộc test boilerplate (getter/setter đơn giản, DTO thuần).
- Mọi bug fix phải kèm regression test.

---

## Quy trình đóng góp (Git Workflow)

Project dùng **GitHub Flow**: mọi thay đổi đi qua Pull Request vào `master`.

```
master (protected, luôn stable)
 └── feat/<tên>    ← branch của bạn
 └── fix/<tên>
 └── docs/<tên>
```

1. **Fork** repo về GitHub account của bạn.
2. Tạo branch từ `master`:
   ```bash
   git checkout master
   git pull upstream master
   git checkout -b feat/ten-tinh-nang
   ```
3. Thực hiện thay đổi — commit nhỏ, focus từng mục đích.
4. Build + test trước khi push:
   ```bash
   dotnet build "CTU Scheduler.slnx" --no-incremental
   dotnet test tests/CTUScheduler.Tests
   ```
5. Push branch và mở Pull Request vào `master`.

---

## Commit Convention

Dùng [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <mô tả ngắn gọn>
```

| Type | Dùng khi |
|---|---|
| `feat` | Thêm tính năng mới |
| `fix` | Sửa lỗi |
| `refactor` | Tái cấu trúc không thay đổi hành vi |
| `test` | Thêm hoặc sửa test |
| `docs` | Cập nhật tài liệu |
| `chore` | Build, config, dependency |
| `perf` | Cải thiện hiệu năng |
| `style` | Formatting, naming (không thay đổi logic) |

Ví dụ:
```
feat(scheduler): add priority weighting for morning-shift preference
fix(ui): resolve timetable grid overflow on small screens
test(core): add regression test for overlapping course conflict detection
docs: update CONTRIBUTING with async conventions
```

**Breaking change**: thêm `!` sau type và note `BREAKING CHANGE:` trong footer commit.

---

## Pull Request

Trước khi mở PR, kiểm tra:

- [ ] Build pass: `dotnet build "CTU Scheduler.slnx"`
- [ ] Test pass: `dotnet test tests/CTUScheduler.Tests`
- [ ] Không có thay đổi ngoài phạm vi task
- [ ] Không commit file `bin/`, `obj/`, `artifacts/`, `*.user`, IDE config cá nhân
- [ ] Mô tả PR giải thích **tại sao** thay đổi này cần thiết, không chỉ **cái gì** đã thay đổi

**Template mô tả PR** (khuyến nghị):

```markdown
## Vấn đề / Động cơ
<!-- Mô tả ngắn gọn vấn đề hoặc tính năng được đề xuất -->

## Thay đổi
<!-- Liệt kê những gì đã thay đổi -->

## Cách test
<!-- Hướng dẫn tái tạo / kiểm tra thủ công nếu cần -->

## Checklist
- [ ] Build pass
- [ ] Test pass
- [ ] Không có side effect ngoài phạm vi
```

Maintainer sẽ review trong vòng vài ngày. Feedback được đưa qua GitHub Review comments — vui lòng trả lời hoặc resolve từng comment trước khi request re-review.

---

## Báo cáo lỗi & đề xuất tính năng

Dùng [GitHub Issues](https://github.com/d3nhatv0lam/CTU-Scheduler/issues):

- **Bug report**: Mô tả steps to reproduce, expected behavior, actual behavior, OS, phiên bản app.
- **Feature request**: Mô tả use case, lý do cần thiết, và nếu có thể, gợi ý hướng implement.

Trước khi mở issue mới, tìm kiếm xem đã có issue tương tự chưa.

---

## Phạm vi ngoài giới hạn

Những thay đổi sau sẽ **không được accept** dù code đúng:

- Thay đổi thuật toán xếp lịch / scoring mà không có yêu cầu từ maintainer và test kiểm chứng.
- Refactor quy mô lớn không liên quan đến task được assign.
- Thêm dependency mới mà không thảo luận trước qua Issues.
- Phá vỡ invariant kiến trúc (Core phụ thuộc framework UI/HTTP, bỏ DI qua constructor, v.v.).
- Thay đổi file `LICENSE`, `TERMS.md`, `AGENTS.md` mà không có sự đồng ý của maintainer.

---

*Nếu bạn không chắc về điều gì, cứ mở một Issue để thảo luận trước khi bắt tay vào code. Tốt hơn hỏi trước còn hơn PR bị reject sau.*
