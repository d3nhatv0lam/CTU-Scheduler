---
description: "Guidelines for shell execution, RTK proxy usage, build and test verification loop, and code navigation using CodeGraph or XERJ with fallback mechanisms."
trigger: model_decision
---

# Tools, Code Search & Verification

## 1. Verification Loop (Definition of Done)

Phân biệt rạch ròi giữa Build và Test. Build thành công chỉ xác nhận cú pháp và kiểu dữ liệu hợp lệ; Test thành công mới chứng minh logic hoạt động đúng.

- **Đối với tác vụ thay đổi mã nguồn (C# / AXAML)**:
  - Bắt buộc chạy build: `rtk dotnet build --no-restore` (hoặc `dotnet build --no-restore` nếu không có RTK).
  - Bắt buộc chạy các unit test liên quan: `rtk dotnet test` (hoặc `dotnet test`).
  - Chính sách Zero Warnings: Không để lại lỗi biên dịch hoặc cảnh báo compiler mới phát sinh.
- **Đối với tác vụ chỉ thay đổi Agent Rules / Documentation / Config**:
  - Bắt buộc kiểm tra tính hợp lệ của YAML frontmatter, tính nhất quán của nội dung và trigger.
  - Không bắt buộc build mã nguồn nếu không đụng chạm đến source code.
- **Nguyên tắc báo cáo trung thực**:
  - Chỉ báo "Thành công" đối với những bước thực sự đã thực thi trong phiên làm việc. Tuyệt đối không khẳng định test đã pass nếu chỉ mới chạy lệnh build.

## 2. RTK CLI Proxy (Tùy chọn & Fallback trong suốt)

- RTK (`rtk`) là công cụ tùy chọn (optional) dùng để lọc và nén output của câu lệnh terminal trước khi đưa vào context LLM.
- **Quy tắc sử dụng**:
  - Tự động thêm tiền tố `rtk` cho các lệnh shell được hỗ trợ khi khả dụng:
    ```bash
    rtk git status
    rtk git diff
    rtk dotnet build CTUScheduler/CTUScheduler.csproj --no-restore
    rtk dotnet test tests/CTUScheduler.Tests/CTUScheduler.Tests.csproj
    ```
- **Cơ chế Fallback trong suốt**:
  - Nếu `rtk` không được cài đặt, báo lỗi, hoặc môi trường không hỗ trợ: tự động chuyển sang câu lệnh tiêu chuẩn thông thường (`dotnet`, `git`).
  - Tuyệt đối không bao giờ làm gián đoạn, làm fail task hoặc dừng công việc vì lý do thiếu RTK.

## 3. Điều hướng mã nguồn & Cơ chế Fallback

Không giả định các công cụ nâng cao luôn tồn tại trên mọi máy lập trình viên:

- **CodeGraph**:
  - Công cụ ưu tiên để tra cứu symbol graph, call hierarchy, và phụ thuộc giữa các tầng trong CTU-Scheduler.
  - Gọi qua MCP tool `codegraph_explore` hoặc shell command `codegraph explore "<query>"`.
  - *Fallback*: Nếu không có thư mục `.codegraph/` hoặc công cụ thiếu vắng, chuyển sang dùng `read_file` và tìm kiếm mã nguồn tiêu chuẩn (`git grep`, grep/ripgrep).
- **XERJ (Code Search & Reference Coding)**:
  - Dùng để tìm kiếm đoạn mã (`xerj search --prefix ctu-scheduler "<query>"`), tra cứu định nghĩa ký hiệu (`xerj def "<symbol>" --prefix ctu-scheduler`), hoặc tham khảo giải thuật xếp lịch (`xerj code timetable-refs "<problem>"`).
  - Luôn trích dẫn `file:line` cụ thể khi báo cáo kết quả.
  - *Fallback*: Nếu `xerj` chưa được cài đặt hoặc thiếu index, sử dụng tìm kiếm văn bản thông thường và tài liệu dự án tiêu chuẩn.
