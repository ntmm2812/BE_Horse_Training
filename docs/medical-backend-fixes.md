# Bổ sung backend y tế và khóa huấn luyện

Phạm vi: backend ASP.NET Core và cấu trúc SQL Server. Không sửa repository frontend, dữ liệu mẫu hoặc service mock/localStorage của frontend.

## Phân quyền

Tất cả endpoint `/api/medical/*` yêu cầu JWT có role `veterinarian`. Không có token trả `401`; các role còn lại trả `403`. Không gán người chưa đăng nhập thành bác sĩ ID 2. Người ghi bệnh án, đặt khóa và mở khóa được lấy từ token; client không được chọn người thực hiện.

HLV tiếp tục dùng API đánh giá thể trạng của trainer để xem khả năng tập và lý do khóa. Không mở quyền truy cập toàn bộ bệnh án cho HLV, groom hoặc chủ ngựa.

## Khóa tập và sức khỏe

- Đánh giá thể trạng đọc khóa chưa có `NgayGo` trong `TrainingLock`, đồng thời vẫn tôn trọng `Horse.LockedAt` của dữ liệu cũ.
- Khóa đang hiệu lực chặn mọi cường độ, kể cả phục hồi, khi xếp lịch và bắt đầu buổi tập. Buổi đã xếp lịch được giữ nguyên; thao tác bắt đầu sẽ kiểm tra lại.
- Nhãn cũ `Tốt` và `Đang điều trị` vẫn được nhận. Bản ghi sức khỏe mới được chuẩn hóa thành `Đủ điều kiện` và `Chấn thương`. Bộ đánh giá hiểu cả hai bộ nhãn để dữ liệu cũ tiếp tục dùng được.
- Tình trạng chấn thương chưa có khóa vẫn giữ quy tắc chỉ tập phục hồi. Khóa tập và trạng thái sức khỏe là hai thông tin riêng.
- Đặt/mở khóa không tự xóa tình trạng cách ly. Mở một khóa không tự gỡ các khóa khác, không tự khởi động lại buổi tập.
- Tạo/sửa bệnh án cùng cập nhật sức khỏe và khóa tự động nằm trong một transaction. Nếu một bước lưu thất bại, các bước trước được rollback.

## Mở khóa qua API thật

`POST /api/medical/training-locks/{lockId}/release`

```json
{
  "newStatus": "Cần theo dõi",
  "releaseReason": "Đã tái khám, đủ điều kiện trở lại tập nhẹ",
  "reexamRecordId": 123
}
```

`releaseReason` và `reexamRecordId` là tùy chọn để khớp frontend, vốn chỉ gửi `newStatus`. Lý do để trống thì lưu lý do mặc định; nếu có nhập thì dài 5–500 ký tự. Nếu có `reexamRecordId`, bệnh án phải thuộc đúng ngựa, đã hoàn tất, mới nhất theo ngày khám/ID, có ngày từ ngày đặt khóa đến hôm nay. Khóa thuộc hồ sơ chấn thương (`InjuryCaseId` khác NULL) luôn bắt buộc `reexamRecordId` và bệnh án phải được gắn vào đúng hồ sơ. Trạng thái mới nhận `Cần theo dõi`, `Đủ điều kiện` hoặc nhãn cũ `Tốt`.

Response bổ sung `releasedByUserId`, `releaseReason`, `reexamRecordId`. Người mở khóa luôn lấy từ JWT, không lấy từ request.

## Thay đổi database

`MedicalSchemaUpdater` chạy khi API khởi động với SQL Server, trong transaction và có thể chạy lại. Cấu hình local hiện dùng `.\SQLEXPRESS` / `Horse_managerment`; không lưu thông tin đăng nhập vào tài liệu này.

| Bảng | Cột bổ sung | Ý nghĩa |
|---|---|---|
| TrainingLock | ReleasedByUserId INT NULL | Bác sĩ mở khóa; khóa ngoại tới Users.UserID |
| TrainingLock | ReleaseReason NVARCHAR(500) NULL | Lý do mở khóa |
| TrainingLock | ReexamRecordId INT NULL | Bệnh án làm căn cứ mở khóa; khóa ngoại tới MedicalRecord.MedicalRecordID |

Các cột có chú thích `MS_Description` bằng tiếng Việt trong SSMS → Column Properties → Extended Properties. Cho phép NULL để giữ nguyên lịch sử cũ chưa có thông tin. Có index lọc theo ngựa cho các khóa chưa mở.

Nếu database chưa có phần y tế, updater vẫn bổ sung `MedicalRecord.ViTriChanThuong`, bảng `TrainingLock` và `VaccinationSchedule` như phiên bản trước. Không tự chèn khóa tập/lịch tiêm demo, không đổi sức khỏe ngựa ID cố định, không suy diễn trường vị trí NULL thành “Không có chấn thương”. Những dữ liệu demo đã tồn tại từ lần chạy trước vẫn được giữ lại.

Các khóa cũ trên `Horse.LockedAt` vẫn được chặn tập độc lập; mở một bản ghi `TrainingLock` không xóa khóa cũ này. Chỉ chuyển đổi dữ liệu cũ sau khi đã xác định nguồn và lý do của từng khóa.

## Kiểm tra

```powershell
dotnet test SWP.Tests/SWP.Tests.csproj
```

Dự án target .NET 8. Cần runtime .NET 8 để bộ HTTP test dùng ASP.NET TestHost 8 chạy đúng phiên bản.

Để kiểm tra cả script nâng cấp trên SQL Server:

```powershell
$env:SWP_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True'
dotnet test SWP.Tests/SWP.Tests.csproj
```

Các test SQL tạo database tạm tên `SWP_TrainingSchema_<guid>` và `SWP_MedicalSchema_<guid>`, rồi xóa đúng database tạm đó. Không dùng database sản phẩm làm nơi chạy test.

## Kết quả thực hiện ngày 15/10/2026

- Build và toàn bộ 329 test đạt trên .NET SDK 8.0.426, không có test bị bỏ qua; bao gồm 2 test schema trên SQL Server thật.
- Đã bổ sung 3 cột mở khóa, khóa ngoại, index và chú thích vào `Horse_managerment`. So sánh dữ liệu trước/sau trong transaction xác nhận bệnh án, sức khỏe, khóa tập và lịch tiêm cũ không bị thay đổi.
- Số bản ghi sau nâng cấp: 6 ngựa, 5 tài khoản, 6 trạng thái sức khỏe, 6 bệnh án, 2 khóa tập, 23 lịch tiêm.
- Repository frontend không có thay đổi. Chưa kết nối service mock frontend sang API thật.
- NuGet báo NU1900 do không truy cập được nguồn kiểm tra lỗ hổng package; build/test vẫn thành công. Kết quả trên không bao gồm kiểm toán lỗ hổng các dependency.
