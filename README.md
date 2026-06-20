<p>&nbsp;</p>
<p align="center">
  <img src="" width="60%" alt="Logo App"/>
<p />

<p align="center">
  <img src="https://img.shields.io/github/v/release/d3nhatv0lam/CTU-Scheduler?style=flat-square&color=blue" alt="Latest Release" />
  <img src="https://img.shields.io/github/license/d3nhatv0lam/CTU-Scheduler?style=flat-square&color=yellow" alt="License" />
  <img src="https://img.shields.io/github/repo-size/d3nhatv0lam/CTU-Scheduler?style=flat-square&color=success" alt="Repository Size" />
</p>

<p align="center">
  <b>Công nghệ sử dụng</b>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET" />
  <img src="https://img.shields.io/badge/Avalonia%20UI-170F1C?style=for-the-badge&logo=avalonia&logoColor=white" alt="Avalonia UI" />
  <img src="https://img.shields.io/badge/Playwright-2EAD33?style=for-the-badge&logo=playwright&logoColor=white" alt="Playwright" />
</p>

<p align="center"> Đừng quên thả ⭐ để tiếp thêm động lực cho team phát triển nha!</p>
---

## ➡️ [Video Demo](https://youtu.be/ubb4gyOioU0?si=qRQOwMNo4vsZeSlX)

## 📌 Mục lục

* [💫 Tổng quan về CTU-Scheduler](#about)
* [✨ Tính năng](#feature)
    * [⚡ Tạo thời khóa biểu nhanh](#auto)
    * [🛠️ Tùy chỉnh lịch thủ công](#manual)
    * [📅 Quản lý thông tin học tập](#info)
    * [💾 Lưu trữ cấu hình lịch học & Xuất Excel](#data)
* [🏗️ Kiến trúc và công nghệ sử dụng](#architecture)
* [🏆 Biên bản họp hội đồng](#certification)
* [📦 Cài đặt & Thiết lập](#setup)
* [💡 Hướng dẫn sử dụng](#usage)
* [✍️ Đội ngũ phát triển](#author)
* [🤝 Đóng góp & Góp ý](#contributing)
* [⚖️ Điều khoản sử dụng](#terms)
* [📜 Giấy phép](#license)

## <a id="about"></a>💫 Tổng quan về CTU-Scheduler

<p>
  <img src="assets/imgs/CTU_logo_180x180.png" alt="CTU Logo" align="right" width="20%" style="margin-left: 15px; margin-bottom: 10px;" />

<strong>CTU-Scheduler</strong> là ứng dụng desktop mã nguồn mở giúp sinh viên <strong>Đại học Cần Thơ</strong> (CTU) lập
kế hoạch học tập
và xếp lịch học nhanh chóng. Ứng dụng hỗ trợ chạy đa nền tảng (Windows, macOS, Linux).
</p>
<p>
  Dự án NCKH này giải quyết triệt để bài toán đụng lịch và xếp thời khóa biểu thủ công. Thay vì phải tra cứu danh mục học phần, kẻ bảng Excel phức tạp hay nháp lịch ra giấy mỗi đầu học kỳ, hệ thống sẽ <strong>tự động hóa toàn bộ quy trình xử lý</strong> để tạo ra các phương án tối ưu nhất.
</p>

## <a id="feature"></a> ✨ Tính năng nổi bật

### <a id="auto"></a> ⚡ Tạo thời khóa biểu nhanh

<p>
  <img src="assets/gifs/TaoTKBNhanh.gif" alt="Fast Scheduling" align="right" width="40%" style="margin-left: 15px; margin-bottom: 15px;" />

Giải quyết khâu tính toán lịch học tốn thời gian chỉ bằng vài cú nhấp chuột:
</p>
<ul>
  <li>Tự động sinh thời khóa biểu từ các môn trong kế hoạch học tập đã thêm.</li>
  <li>Chỉ cần ấn tạo, chọn lịch ưng ý và hoàn thành là xong.</li>
</ul>
<br clear="both" />

### <a id="manual"></a> 🛠️ Tùy chỉnh lịch thủ công

<p>
  <img src="assets/imgs/TaoTKBThuCong.png" align="left" width="40%" style="margin-right: 20px; margin-bottom: 15px;" />

Dành cho ai muốn tự tay kiểm soát chi tiết từng giờ học:
</p>
<ul>
  <li>Tự nhập các môn học mà bạn muốn đăng ký.</li>
  <li>Tự do lựa chọn nhóm học, giảng viên và khung giờ cho từng môn.</li>
  <li>Sau khi chọn nhóm xong, kết quả hiển thị và các thao tác lưu lịch hoàn toàn giống với chế độ tạo TKB nhanh.</li>
</ul>
<br clear="both" />

### <a id="info"></a> 📅 Quản lý thông tin học tập

Mọi thông tin bạn cần biết đều được tóm gọn trên một màn hình:
<ul>
  <li><strong>Nhắc lịch đăng ký:</strong> Theo dõi trực tiếp các mốc thời gian (lộ trình) mở/đóng hệ thống để không bao giờ trễ đợt đăng ký.</li>
  <li><strong>Kiểm soát môn học:</strong> Xem nhanh danh sách các môn cần học theo Kế hoạch học tập (KHHT) và theo dõi trạng thái đã đăng ký thành công hay chưa.</li>
  <li><strong>Theo dõi học phí:</strong> Nắm rõ tổng tiền phải đóng, hạn chót nộp và trạng thái thanh toán (đủ học phí hay còn nợ).</li>
</ul>

<p align="center">
  <img src="assets/imgs/HomeView.png" width="60%"/>
</p>

### <a id="data"></a> 💾 Lưu trữ cấu hình lịch học & Xuất Excel

<ul>
  <li><strong>Lưu trữ cấu hình lịch học:</strong> Hỗ trợ xuất toàn bộ thời khóa biểu đang xếp ra file JSON cục bộ để sao lưu và tải lại nhanh chóng mà không cần thao tác lập lịch lại từ đầu.</li>
  <li><strong>Xuất Excel:</strong> Hỗ trợ xuất dữ liệu thời khóa biểu ra tệp Excel định dạng rõ ràng, sẵn sàng để in ấn hoặc lưu trữ.</li>
</ul>

## <a id="architecture"></a> 🏗️ Kiến trúc và công nghệ sử dụng

Dự án áp dụng **Clean Architecture** để mã nguồn gọn gàng, app chạy nhẹ và dễ bảo trì:

* **Core Layer:** Chứa thuật toán xếp lịch siêu tốc, được tối ưu bộ nhớ để app không bị đơ hay giật lag khi xử lý nhiều
  môn.
* **AppServices Layer:** Quản lý luồng hoạt động chính và lưu trữ tiến độ xếp lịch của bạn.
* **Infrastructure Layer:** Chuyên xử lý việc kết nối với hệ thống trường (đăng nhập tự động, lấy dữ liệu API) và đọc
  file PDF kế hoạch học tập.
* **Presentation Layer:** Giao diện đa nền tảng xây dựng bằng **Avalonia UI** (kèm thư viện Semi.Avalonia & Irihi.Ursa),
  cho trải nghiệm mượt mà và ít tốn CPU.

## <a id="certification"></a> 🏆 Biên bản họp hội đồng

* **Đề tài Nghiên cứu khoa học (NCKH) cấp trường**
* **Trạng thái:** Đã được hội đồng chuyên môn xét duyệt nghiệm thu và thông qua.
* **Đơn vị quản lý:** Trường Công nghệ Thông tin & Truyền thông - Đại học Cần Thơ.

<p align="center">
  <img src="assets/imgs/KetQuaDuyet.jpg" alt="NCKH Certification" width="400px" />
</p>

## <a id="setup"></a> 📦 Cài đặt & Thiết lập

Làm theo các bước dưới đây để tải về và chạy dự án trên máy của bạn.

### 📋 Yêu cầu hệ thống

* [.NET SDK](https://dotnet.microsoft.com/download) (Phiên bản 8.0 trở lên)
* [Git](https://git-scm.com/) để tải mã nguồn (clone repository).

### 📥 Clone the Repository

```bash
https://github.com/d3nhatv0lam/CTU-Scheduler.git
```

### 💻 Local Setup (Desktop)

**1. Khôi phục các gói (Restore Dependencies)**

```bash
dotnet restore "CTU Scheduler.slnx"
```

**2. Cài đặt trình duyệt cho Playwright (Chế độ dự phòng Standby)**

```bash
pwsh bin/Debug/net8.0/playwright.ps1 install
```

*(Nếu bạn không dùng PowerShell, có thể cài đặt thông qua script của Playwright)*

**3. Build dự án**

```bash
dotnet build "CTU Scheduler.slnx" -c Release
```

## <a id="usage"></a> 💡 Hướng dẫn sử dụng

Để khởi chạy ứng dụng trực tiếp từ mã nguồn, chạy câu lệnh sau tại thư mục gốc:

```bash
dotnet run --project CTUScheduler.Desktop/CTUScheduler.Desktop.csproj
```

**Thao tác cơ bản:**

1. Mở ứng dụng, đăng nhập tài khoản sinh viên cổng thông tin CTU.
2. Đợi hệ thống tự động đồng bộ và tải về thông tin đăng ký học phần mới nhất.
3. Chuyển sang tab **Học phần** ở thanh điều hướng bên trái.
4. Bấm vào nút **Thêm thời khóa biểu** để mở bảng cấu hình lập lịch.
5. Lựa chọn phương thức **Thời khóa biểu nhanh** (để máy tự động phối hợp lớp) hoặc **Thời khóa biểu thủ công** (tự chọn
   tay) tùy theo nhu cầu.
6. Bấm **Tạo thời khóa biểu** để nhận danh sách các phương án, chọn lịch ưng ý nhất và bấm **Hoàn thành** để lưu lại.
7. Bạn có thể chọn **Export ra Excel** để in ấn hoặc kết xuất file JSON để lưu trữ cục bộ.

## ✍️ Đội ngũ phát triển

Dự án NCKH này được nghiên cứu và phát triển bởi nhóm sinh viên Đại học Cần Thơ:

* **Dương Minh Đức** ([@d3nhatv0lam](https://github.com/d3nhatv0lam)) - Chủ nhiệm đề tài & Lead Developer
* **Nguyễn Phước Lộc** ([@Lexipit3268](https://github.com/Lexipit3268)) - Developer, Designer
* **Trần Trọng Phúc** ([@phuctran1501](https://github.com/phuctran1501)) - Developer
* **Nguyễn Ngọc Đức Phát** ([@KimgionDev](https://github.com/KimgionDev)) - Developer

Nếu ứng dụng hữu ích, hãy chia sẻ dự án NCKH này nhé.

## 🤝 Đóng góp & Góp ý

Mọi ý kiến đóng góp và báo lỗi từ bạn sẽ giúp ứng dụng ngày một hoàn thiện hơn. Nếu bạn gặp khó khăn khi sử dụng hoặc có
ý tưởng mới, hãy chia sẻ với team phát triển:

* **Báo lỗi & Góp ý:** Điền thông tin nhanh qua [Form](https://xin_cai_form_diiii).
* **Đóng góp mã nguồn:** Nếu bạn là developer và muốn cải tiến dự án, hãy tạo **Pull Request** hoặc mở **Issue** trực
  tiếp tại repository này.

## <a id="terms"></a>⚖️ Điều khoản sử dụng

Xem chi tiết các quy định và tuyên bố miễn trừ trách nhiệm tại file [TERMS](./TERMS.md).

## 📜 Giấy phép

Dự án được phân phối dưới giấy phép MIT. Xem chi tiết tại file [LICENSE](./LICENSE).

---
