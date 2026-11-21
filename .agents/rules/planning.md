---
description: "Guidelines for task planning, Implementation Plan Artifacts, long-running multi-phase workflow management, and persistent workspace plans in docs/plans/."
trigger: model_decision
---

# Long-Running Task Planning & Artifact Management

## 1. Cơ chế quản lý 2 tầng (Two-Tier Planning Mechanism)

- **Tầng 1 — Implementation Plan Artifact (Đánh giá tương tác)**:
  - Áp dụng cho: Tác vụ đa tầng, nhiều file, thay đổi kiến trúc, rủi ro cao, hoặc yêu cầu mơ hồ.
  - Mục đích: Trình bày kế hoạch có cấu trúc để người dùng review, phản biện và bấm duyệt (Proceed) trước khi can thiệp vào mã nguồn.
- **Tầng 2 — Workspace Plan (Lưu trữ trạng thái dài hạn)**:
  - Áp dụng cho: Tác vụ dài hơi, chia nhiều giai đoạn (phases), hoặc kéo dài qua nhiều phiên làm việc.
  - Vị trí lưu trữ: `docs/plans/<task-name>.md` bên trong repository.
  - Mục đích: Nguồn sự thật bền vững lưu trữ tiến độ, các quyết định kỹ thuật đã thống nhất, checkpoints, và công việc còn lại xuyên suốt các phiên chat.

## 2. Tiêu chí kích hoạt

- **Task nhỏ, rõ ràng** (sửa typo, đổi tên, fix bug 1-file rõ nguyên nhân): Thực thi trực tiếp sau một câu tóm tắt mục đích. Tuyệt đối không tạo artifact hay workspace plan cho tác vụ đơn giản.
- **Task phức tạp**: Tạo Implementation Plan Artifact trước khi code. Nếu task dự kiến qua nhiều session, tạo song song tệp workspace plan trong `docs/plans/`.

## 3. Cấu trúc một Workspace Plan chuẩn

Một tệp kế hoạch trong `docs/plans/` cần bao gồm:
1. Mục tiêu, phạm vi và tiêu chí nghiệm thu (acceptance criteria).
2. Các quyết định kiến trúc và lý do lựa chọn.
3. Danh sách tác vụ có thứ tự, phân chia theo từng phase rõ ràng.
4. Danh sách các file và component bị ảnh hưởng.
5. Rủi ro, ràng buộc và các câu hỏi mở.
6. Lệnh kiểm chứng (build/test) và kết quả kỳ vọng.
7. Checklist tiến độ và các đầu việc còn lại.

## 4. Quản lý ngữ cảnh & Xử lý sai lệch trạng thái

- **Quy tắc khôi phục (Resuming Protocol)**:
  - Khi tiếp tục một task từ phiên trước, Agent **bắt buộc phải đọc lại tệp workspace plan hiện tại trong `docs/plans/` và kiểm tra mã nguồn thực tế**, tuyệt đối không dựa hoàn toàn vào lịch sử hội thoại (conversation history).
  - Duy trì một tóm tắt tiến độ ngắn gọn trong context đang hoạt động.
- **Tình huống A: Khi Workspace Plan không tồn tại (Missing Plan)**:
  - Nếu người dùng yêu cầu tiếp tục một task nhưng không tìm thấy plan trong `docs/plans/`:
  - Tuyệt đối không tự suy đoán hoặc bịa đặt tiến độ.
  - Phải kiểm tra repository: xem `git log`, `git status`, đọc các commit gần nhất và mã nguồn liên quan.
  - Báo cáo minh bạch cho người dùng những phần đã làm và những quyết định chưa xác định trước khi tiếp tục.
- **Tình huống B: Khi Mã nguồn thực tế khác với Workspace Plan (State Drift)**:
  - Nếu tệp plan ghi nhận một trạng thái nhưng mã nguồn thực tế đã thay đổi:
  - Không được tiếp tục chạy máy móc theo checklist cũ.
  - Bắt buộc dừng lại đối chiếu: xem `git diff`, kiểm tra kết quả build/test thực tế.
  - Cập nhật lại workspace plan để phản ánh đúng hiện trạng mã nguồn trước khi bắt đầu giai đoạn tiếp theo.

## 5. Cập nhật tiến độ sau mỗi giai đoạn

- Cập nhật tệp plan sau khi hoàn thành mỗi giai đoạn có ý nghĩa.
- Ghi nhận kết quả thực thi thực tế, kết quả build/test, các sai lệch hoặc quyết định phát sinh so với kế hoạch ban đầu.
- Tuyệt đối không đánh dấu hoàn thành task nếu chưa kiểm chứng các tiêu chí nghiệm thu.
