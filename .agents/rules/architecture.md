---
description: "Clean Architecture layer conventions, namespace boundaries, ReactiveUI MVVM, DI lifetimes, OperationResult error handling, and schedule logic preservation."
trigger: model_decision
---

# Architecture & Structural Constraints

## 1. Architectural Invariants (Bắt buộc tuân thủ)

Bắt buộc tuân thủ trong mọi thay đổi thông thường. Chỉ được phép điều chỉnh khi có quyết định kiến trúc rõ ràng, được người dùng chấp thuận và ghi nhận lý do kỹ thuật.

- **Chiều phụ thuộc nghiệp vụ**: `Desktop → Presentation → AppServices → Core ← Infrastructure`.
  - Đây là bất biến kiến trúc, độc lập với việc mã nguồn đang nằm chung `CTUScheduler.csproj` (bảo vệ bằng namespace `CTUScheduler.<Layer>.<Area>`) hay sau này tách thành nhiều `.csproj`.
  - Nếu tương lai tách project, các ProjectReference phải tiếp tục tuân thủ đúng chiều phụ thuộc này.
  - Trách nhiệm của các tầng không thay đổi theo cấu trúc project.
- **Tính độc lập của `Core`**:
  - `Core` chỉ chứa business logic, domain models, thuật toán và interfaces trừu tượng.
  - Tuyệt đối không phụ thuộc vào framework UI (Avalonia), thư viện MVVM (ReactiveUI), các cài đặt cụ thể của Infrastructure (HTTP client, PDF, Excel), hoặc DI container.
  - Cho phép sử dụng các thư viện pure domain/math nếu có lý do kỹ thuật chính đáng và không vi phạm tính độc lập framework.
- **Dependency Injection**:
  - Bắt buộc constructor injection.
  - Tuyệt đối cấm Service Locator pattern (truy cập tĩnh qua `App.Current.Services` hoặc static accessors để giải quyết service).
- **Bảo tồn thuật toán lập lịch**:
  - Bảo vệ tính đúng đắn và hành vi nghiệp vụ của thuật toán lập lịch trong `Core/Models/Academic/`, `IScheduleScorer`, `ScheduleValidator`.
  - *Task không liên quan*: Tuyệt đối không tự ý chỉnh sửa hoặc refactor các tệp giải thuật xếp lịch.
  - *Bug fix*: Được phép sửa trong phạm vi hẹp cần thiết, bắt buộc bổ sung unit test hồi quy (regression test).
  - *Refactor*: Được phép nếu bảo toàn 100% hành vi nghiệp vụ và kết quả đầu ra.
  - *Thay đổi thuật toán / scoring / độ ưu tiên*: Bắt buộc có yêu cầu tường minh từ người dùng và kiểm chứng kết quả thực tế.

## 2. Project Conventions (Quy ước dự án mặc định)

Mặc định áp dụng, có thể điều chỉnh khi có lý do kỹ thuật chính đáng.

- **Composition Root**:
  - `Desktop` là Composition Root, hợp lệ khi tham chiếu `Infrastructure` để cấu hình DI container. Sơ đồ phân tầng nghiệp vụ không ngăn cản việc cấu hình DI tại Composition Root.
  - Mỗi tầng tự đóng gói đăng ký DI qua extension method (ví dụ: `AddAppServices()`, `AddInfrastructure()`).
- **DI Lifetimes**:
  - Mặc định: `Singleton` cho Stores dữ liệu (`PlannedCourseStore`, `TuitionFeeStore`, `CtuSessionStore`, `TeachingPlanStore`) và `AppState`; `Transient` cho Use Case services không trạng thái và ViewModels.
  - HTTP clients và Delegating Handlers tuân theo cơ chế quản lý vòng đời chuyên biệt của `IHttpClientFactory`; không tự ý áp đặt lifetime chung.
  - Không cấm `Scoped` trong tương lai nếu xuất hiện use-case phù hợp (ví dụ worker nền, scoped session).
- **Điều hướng (Navigation)**:
  - `INavigationRegionManager` là convention điều hướng mặc định hiện tại; không gọi `Router.Navigate` trực tiếp. Không ngăn cản nâng cấp kiến trúc điều hướng khi có yêu cầu mới.
- **Xử lý kết quả & Ngoại lệ (Error Handling)**:
  - `OperationResult` (trong `Core/Models/Shared/Results/`) là lựa chọn mặc định cho các lỗi nghiệp vụ dự kiến trước (expected domain failures: trùng lịch, hết chỗ, sai định dạng dữ liệu đầu vào).
  - Không ép mọi lỗi kỹ thuật hoặc exceptional failure phải chuyển thành Result; Exceptions vẫn là cơ chế chuẩn mực cho các sự cố kỹ thuật bất ngờ (mất kết nối mạng, lỗi I/O ổ đĩa, crash hệ thống).
  - Bắt cụ thể ngoại lệ, cấm `catch {}` rỗng hoặc nuốt exception im lặng.

## 3. Contextual Recommendations (Khuyến nghị theo ngữ cảnh)

Lựa chọn linh hoạt tùy theo tình huống thực tế.

- **ReactiveUI & MVVM**:
  - ViewModels kế thừa `ViewModelBase` hoặc `SessionSyncViewModelBase`.
  - Sử dụng ReactiveUI Source Generators (`[Reactive]`, `[ObservableAsProperty]`).
  - View không chứa business logic, chỉ khởi tạo UI; ViewModel không tham chiếu trực tiếp các kiểu Avalonia `Control`.
  - Quản lý Rx subscriptions: Sử dụng `.DisposeWith(_disposables)` khi vòng đời của subscription cần gắn chặt với ViewModel/Component. Không áp đặt cho observable tự complete hoặc dịch vụ singleton có cơ chế tự giải phóng riêng (`ICleanup`).
