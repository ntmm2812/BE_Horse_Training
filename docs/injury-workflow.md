# Hồ sơ chấn thương và công việc chăm sóc theo role

Backend cho hai phần ưu tiên: hồ sơ chấn thương xuyên suốt và chuyển chỉ định bác sĩ thành công việc chăm sóc. Giữ nguyên frontend và các mock/localStorage của frontend.

## Luồng thực hiện

1. Bác sĩ mở hồ sơ cho một ngựa, ghi vùng chấn thương, bên, mức độ, ghi chú lâm sàng, tóm tắt cho chủ ngựa và hướng dẫn HLV.
2. Bệnh án vẫn được tạo bằng `/api/medical/records`. Bác sĩ gắn bệnh án vào hồ sơ; hệ thống lưu bản chụp nội dung tại thời điểm gắn.
3. Nếu cần dừng tập, bác sĩ đặt khóa từ hồ sơ. Mỗi hồ sơ có tối đa một khóa đang hiệu lực; cùng ngựa có thể có khóa của nhiều hồ sơ khác nhau.
4. Bác sĩ chọn groom đang hoạt động, giao công việc có hướng dẫn, hạn hoàn thành và mức ưu tiên.
5. Groom chỉ thấy việc được giao cho mình: bắt đầu → hoàn thành kèm kết quả, hoặc báo không thực hiện được kèm lý do.
6. Bác sĩ xử lý việc không thực hiện được bằng giao lại hoặc hủy có lý do. Quản lý/bác sĩ xem việc và tái khám quá hạn.
7. Bác sĩ gắn bệnh án tái khám, mở khóa liên quan qua API y tế hiện có, rồi đóng hồ sơ khi hết việc chưa xử lý.

Không tự kết luận khỏi bệnh, nâng mức tập, mở khóa khi đến hạn hoặc khởi động lại lịch tập. Hướng dẫn HLV là văn bản do bác sĩ nhập; khóa tập hiện vẫn là khóa toàn bộ. Chức năng trở lại tập theo từng mức, ảnh đính kèm, tiếp nhận báo cáo bất thường và thông báo đẩy chưa thuộc phần triển khai này.

## Phân quyền và phạm vi dữ liệu

| Role | Quyền |
|---|---|
| `veterinarian` | Mở hồ sơ, cập nhật tóm tắt/hẹn tái khám, thêm ghi chú, gắn bệnh án, đọc timeline, chuyển trạng thái, khóa tập, giao/giao lại/hủy việc, xem tổng quan |
| `groom` | Xem các công việc y tế được giao; bắt đầu, hoàn thành, báo không thực hiện được. Không đọc toàn bộ bệnh án/timeline |
| `head_trainer` | Xem tóm tắt hồ sơ và hướng dẫn tập; dùng API fitness hiện có để xem khả năng tập. Không sửa y tế hoặc đọc timeline |
| `horse_owner` | Chỉ đọc tóm tắt hồ sơ ngựa hiện thuộc sở hữu của mình. Không đọc hướng dẫn chăm sóc, ghi chú lâm sàng hoặc timeline |
| `club_manager` | Xem tình trạng hồ sơ và tiến độ/việc quá hạn. Danh sách việc ẩn hướng dẫn chăm sóc và nội dung kết quả; không được quyết định mở/đóng hồ sơ |

JWT bắt buộc. Service còn kiểm tra tài khoản thực tế đang `Active` và role trong database; sửa role trong request body không có tác dụng. Truy cập trực tiếp ID hồ sơ ngựa của chủ khác, hoặc ID công việc của groom khác, trả `404`.

## API

Prefix: `/api/injury-cases`. Swagger runtime tự sinh nhóm `InjuryWorkflow` tại `/swagger` khi chạy môi trường Development.

| Method | Đường dẫn sau prefix | Mục đích |
|---|---|---|
| GET | `/` | Danh sách hồ sơ; lọc `horseId`, `status`, phân trang `skip`, `take` |
| POST | `/` | Bác sĩ mở hồ sơ |
| GET | `/{id}` | Tóm tắt hồ sơ theo quyền |
| PUT | `/{id}/summary` | Cập nhật tóm tắt, hướng dẫn HLV, hạn tái khám; bắt buộc lý do |
| GET | `/{id}/timeline` | Lịch sử dành cho bác sĩ; phân trang |
| POST | `/{id}/notes` | Ghi thêm diễn biến lâm sàng |
| POST | `/{id}/exams` | Gắn bệnh án và lưu bản chụp |
| POST | `/{id}/status` | Chuyển trạng thái hồ sơ |
| POST | `/{id}/training-locks` | Đặt khóa tập liên quan hồ sơ |
| GET | `/assignable-grooms` | Danh sách groom đang hoạt động để bác sĩ phân công |
| POST | `/{id}/care-tasks` | Giao việc chăm sóc |
| GET | `/care-tasks` | Danh sách việc theo quyền; lọc `caseId`, `overdueOnly`, phân trang |
| GET | `/care-overview` | Số việc chờ, đang làm, không thực hiện được, quá hạn và hồ sơ quá hạn tái khám |
| POST | `/care-tasks/{taskId}/start` | Groom bắt đầu công việc |
| POST | `/care-tasks/{taskId}/complete` | Groom hoàn thành và ghi kết quả |
| POST | `/care-tasks/{taskId}/unable` | Groom báo không thực hiện được và lý do |
| PUT | `/care-tasks/{taskId}/assignment` | Bác sĩ giao lại việc và đặt hạn mới |
| POST | `/care-tasks/{taskId}/cancel` | Bác sĩ hủy việc có lý do |

`take`: 1–100, mặc định 30; `skip`: >= 0. Danh sách trả `{ items, total, skip, take }`. `dueAt` và `reviewDueAt` nhận ngày giờ ISO 8601 có múi giờ, lưu UTC. `isOverdue`, `overdueHours`, `reviewOverdue` tính lúc đọc; không cần job nền và không tự đổi trạng thái khi quá hạn.

## Trạng thái và chống ghi đè

Hồ sơ: `Open` → `Treating` hoặc `Monitoring`; `Treating` ↔ `Monitoring`; cả ba trạng thái có thể chuyển `Closed` nếu đủ điều kiện. Hồ sơ đóng không được sửa, thêm việc hoặc mở lại; đợt chấn thương mới dùng hồ sơ mới.

Đóng hồ sơ cần bệnh án hoàn tất mới nhất của đúng ngựa, ngày khám từ ngày mở hồ sơ đến hôm nay và đã gắn vào hồ sơ. Không được còn khóa của chính hồ sơ đang hiệu lực hoặc công việc `Pending`, `InProgress`, `UnableToComplete`. Đóng hồ sơ này không đổi sức khỏe hoặc khóa của hồ sơ khác.

Công việc: `Pending` → `InProgress` → `Completed`. Groom có thể báo `UnableToComplete` từ `Pending`/`InProgress`; trạng thái này vẫn cần bác sĩ xử lý. Bác sĩ có thể giao lại từ `Pending`/`UnableToComplete` hoặc hủy việc chưa hoàn thành. Không sửa/xóa kết quả hoàn thành.

Mọi thao tác sửa phải gửi `version` từ lần đọc gần nhất. Với URL `/{id}/...`, dùng phiên bản hồ sơ; với `/care-tasks/{taskId}/...`, dùng phiên bản công việc. Giao việc, thao tác việc và mở khóa cũng tăng phiên bản hồ sơ, nên tải lại hồ sơ trước khi thao tác tiếp. Phiên bản cũ trả `409`.

Toàn bộ thay đổi đối tượng và lịch sử được ghi trong cùng transaction. Nếu ghi lịch sử thất bại, thay đổi nghiệp vụ cũng rollback. API không cung cấp sửa/xóa timeline. Đây là bảo vệ ở tầng ứng dụng; người quản trị SQL có quyền sửa database vẫn có thể sửa dữ liệu trực tiếp.

## Ví dụ request

Các ID và thời điểm dưới đây là ví dụ; dùng ID thật, ngày hẹn tương lai và phiên bản vừa đọc.

Mở hồ sơ:

```json
{
  "horseId": 1,
  "title": "Theo dõi chấn thương chân trước",
  "bodyRegion": "Chân trước",
  "side": "Left",
  "severity": "Moderate",
  "clinicalNotes": "Ghi nhận của bác sĩ sau khám",
  "ownerSummary": "Ngựa đang được theo dõi và chăm sóc",
  "trainingGuidance": "Chờ bác sĩ đánh giá lại trước khi xếp lịch",
  "reviewDueAt": "2026-10-20T09:00:00+07:00"
}
```

`side`: `Left`, `Right`, `Bilateral`, `Unspecified`. `severity`: `Mild`, `Moderate`, `Severe`. Các mã phân biệt hoa/thường.

Giao việc:

```json
{
  "version": 1,
  "groomId": 3,
  "title": "Theo dõi vận động theo chỉ định",
  "instructions": "Thực hiện và ghi kết quả theo hướng dẫn bác sĩ",
  "priority": "Normal",
  "dueAt": "2026-10-20T16:00:00+07:00"
}
```

`priority`: `Normal` hoặc `Urgent`. Người nhận phải là groom đang hoạt động.

Groom bắt đầu: `{ "version": 1 }`. Hoàn thành/báo không thực hiện được: `{ "version": 2, "notes": "Kết quả hoặc lý do cụ thể" }`. Lý do/kết quả dài 5–1000 ký tự.

Gắn bệnh án: `{ "version": 3, "medicalRecordId": 12 }`.

Đặt khóa: `{ "version": 4, "reason": "Lý do bác sĩ yêu cầu dừng tập" }`.

Mở khóa vẫn dùng `POST /api/medical/training-locks/{lockId}/release` với `newStatus`, `releaseReason`, `reexamRecordId`. Nếu khóa thuộc hồ sơ chấn thương, phải gắn bệnh án tái khám vào đúng hồ sơ trước. Thao tác mở khóa được thêm vào timeline và tăng phiên bản hồ sơ.

Đóng hồ sơ: `{ "version": 7, "status": "Closed", "reason": "Đã tái khám và xử lý xong các việc", "reexamRecordId": 12 }`.

Lỗi: `400` dữ liệu không hợp lệ; `401` chưa đăng nhập; `403` sai role/tài khoản không hoạt động; `404` không tìm thấy hoặc ngoài phạm vi; `409` phiên bản cũ, trùng liên kết hoặc sai trạng thái.

## Thay đổi SQL Server

| Đối tượng | Thay đổi |
|---|---|
| `InjuryCase` | Bảng mới: ngựa, bác sĩ mở, vùng/bên/mức độ, trạng thái, tóm tắt chủ ngựa, hướng dẫn HLV, hạn tái khám, thời điểm và phiên bản |
| `InjuryCaseEvent` | Bảng mới: lịch sử, người thực hiện, loại sự kiện, dữ liệu JSON, liên kết bệnh án, thời điểm UTC |
| `CareTask` | Thêm `InjuryCaseId INT NULL`, `AssignedByUserId INT NULL`, `Version INT NOT NULL DEFAULT 1` |
| `TrainingLock` | Thêm `InjuryCaseId INT NULL` |

Tái sử dụng bảng `CareTask`; việc y tế có `TaskType = Medical`, `InjuryCaseId` khác NULL. Với việc y tế, cột cũ `ScheduledAt` chứa hạn hoàn thành và API trả thành `dueAt`. Các công việc cũ vẫn có `InjuryCaseId = NULL`, không xuất hiện trong API công việc y tế mới. Không tự gắn bệnh án/khóa/việc cũ vào hồ sơ mới.

Script gốc: `SWP.Infrastructure/Data/Sql/InjuryWorkflow.sql`, được nhúng vào assembly; `InjuryWorkflowSchemaUpdater` đọc và chạy trong transaction sau `MedicalSchemaUpdater` khi API khởi động. Script có thể chạy lại và không seed dữ liệu. Các bảng/cột có chú thích `MS_Description` bằng tiếng Việt để xem trong SSMS.

## Kiểm thử

```powershell
$env:SWP_TEST_SQLSERVER = 'Server=.\SQLEXPRESS;Integrated Security=True;TrustServerCertificate=True'
dotnet test SWP.Tests/SWP.Tests.csproj
```

Dùng .NET 8. Test SQL tạo database tạm `SWP_InjuryWorkflow_<guid>` rồi xóa đúng database đó; không dùng database sản phẩm làm nơi tạo dữ liệu test. Test bao gồm phân quyền theo role và sở hữu, phạm vi công việc groom, hồ sơ song song, khóa tập, trạng thái, phiên bản cũ, snapshot bệnh án, quá hạn, rollback và nâng cấp schema lặp lại.
