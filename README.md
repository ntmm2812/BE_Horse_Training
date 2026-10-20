# BE Horse Training

Backend ASP.NET Core (.NET 8) cho project quản lý và huấn luyện ngựa, sử dụng Entity Framework Core và SQL Server.

## Cấu trúc

- `SWP.API`: API và cấu hình khởi chạy.
- `SWP.Application`: tầng ứng dụng.
- `SWP.Domain`: các entity nghiệp vụ.
- `SWP.Infrastructure`: DbContext và cấu hình ánh xạ database.

## Chạy trên máy local

Yêu cầu .NET 8 SDK và SQL Server.

1. Sao chép file cấu hình mẫu (PowerShell):

   ```powershell
   Copy-Item SWP.API/appsettings.example.json SWP.API/appsettings.json
   ```

2. Sửa `ConnectionStrings:DefaultConnection` trong `SWP.API/appsettings.json` theo SQL Server của bạn. File này được bỏ qua bởi Git để giữ thông tin đăng nhập trên máy local.

3. Build và chạy:

   ```powershell
   dotnet restore BE_Horse_managerment.sln
   dotnet build BE_Horse_managerment.sln --no-restore
   dotnet run --project SWP.API --launch-profile https
   ```

Ở môi trường Development, mở `/swagger` trên địa chỉ HTTPS được in trong terminal để xem các API đã đăng ký.

## Chạy test

Test chạy trên .NET 8, cùng phiên bản chính với API. Kiểm tra `dotnet --list-runtimes`
có `Microsoft.NETCore.App 8.0.x` và `Microsoft.AspNetCore.App 8.0.x`.
Nếu máy chỉ có .NET 10, cài thêm .NET 8 SDK/ASP.NET Core Runtime; không bật
`RollForward=Major` để chạy bộ test ASP.NET Core 8 bằng runtime 10.

```powershell
dotnet test BE_Horse_managerment.sln
```

Test dùng SQLite in-memory nên không cần SQL Server. Riêng test kiểm tra script nâng cấp schema trên SQL Server thật chỉ chạy khi đặt biến môi trường (test tự tạo rồi xóa một database tạm):

```powershell
$env:SWP_TEST_SQLSERVER = "Server=localhost\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True"
dotnet test BE_Horse_managerment.sln
```

## Luồng Lập & Thực hiện Giáo án Huấn luyện (HLV Trưởng)

Mọi API dưới `api/trainer/*` yêu cầu JWT của vai trò `head_trainer`.

1. **Xem thể trạng** – `GET api/trainer/horses/{horseId}/fitness-assessment`: tổng hợp trạng thái sức khỏe (bác sĩ thú y), lệnh khóa huấn luyện (`Horse.LockedAt`), tuổi, sinh hiệu lúc nghỉ trong 3 ngày và tải tập 7 ngày → mức cường độ tối đa (`Recovery` < `Low` < `Medium` < `High`) và mức khuyến nghị.
2. **Gợi ý giáo án** – `GET api/trainer/horses/{horseId}/plan-suggestion?startDate=&weeks=`: các giai đoạn đề xuất theo thể trạng, gửi lại nguyên văn được cho API tạo giáo án.
3. **Lập giáo án** – `POST api/trainer/training-plans` (trạng thái Nháp). Mỗi giai đoạn gồm khoảng ngày, cường độ tối đa, số buổi/tuần, cự ly, thời lượng, mặt sân. Giai đoạn vượt mức thể trạng cho phép bị từ chối (HTTP 422 `HORSE_NOT_FIT`).
4. **Áp dụng** – `POST .../{planId}/activate`. Mỗi ngựa chỉ có một giáo án đang áp dụng.
5. **Xếp lịch tuần** – `POST .../{planId}/weekly-schedule` sinh buổi tập cho một tuần (Chủ Nhật nghỉ, xen kẽ buổi nhẹ/buổi chính, tự hạ cường độ nếu thể trạng xấu đi). `previewOnly: true` để xem trước, `replaceExisting: true` để sinh lại. Thêm buổi thủ công: `POST .../{planId}/sessions`.
6. **Phân công & thực hiện** – `PUT api/trainer/training-sessions/{id}/assignment`, `POST .../start` (chỉ trong ngày, kiểm tra lại thể trạng), `POST .../complete` (chỉ số thực tế, điểm phong độ 0–10, nhận xét chuyên môn, link video chạy thử), `POST .../cancel`.
7. **Theo dõi** – `GET api/trainer/horses/{horseId}/weekly-schedule?weekStart=`, `GET api/trainer/training-sessions?from=&to=&horseId=&status=`, `GET api/trainer/training-plans/{planId}` (kèm tiến độ). Nhân viên được phân công xem lịch tại `GET api/training-assignments/me`.
8. **Kết thúc** – `POST .../{planId}/complete` hoặc `.../cancel`; các buổi chưa tập được hủy tự động.

Danh mục mã và nhãn tiếng Việt cho dropdown: `GET api/trainer/training-plans/options`. Lỗi trả về dạng `{ message, code, details }` với `code` ∈ `NOT_FOUND` (404), `VALIDATION_ERROR` (400), `INVALID_STATUS`/`CONFLICT` (409), `HORSE_NOT_FIT` (422).

API không tự tạo/sửa bảng hoặc seed dữ liệu khi khởi động. Database cần được chuẩn bị
trước khi chạy các luồng nghiệp vụ. Các phần bổ sung schema được định nghĩa trong
`SWP.Infrastructure/Data/TrainingSchemaUpdater.cs`, `MedicalSchemaUpdater.cs` và
`Data/Sql/InjuryWorkflow.sql`; chỉ áp dụng có chủ đích trên database đã được sao lưu.

Medical, training và injury dùng chung `ApplicationDbContext` để giữ liên kết bệnh án,
khóa tập và hồ sơ chấn thương. `OriginalCareDbContext` chỉ phục vụ API chăm sóc hằng ngày
trên các cột gốc; không dùng context này thay cho service y tế vì sẽ mất thông tin liên kết
và điều kiện tái khám khi mở khóa.
