# BỘ KỊCH BẢN KIỂM THỬ THỦ CÔNG (MANUAL TEST CASES)
## HỆ THỐNG QUẢN LÝ HUẤN LUYỆN NGỰA ĐUA (RACEHORSE TRAINING MANAGEMENT SYSTEM - SWP391)

> **Mục tiêu**: Cung cấp tài liệu kiểm thử thủ công từng bước (Step-by-step checklist) cho Giảng viên, Hội đồng bảo vệ, Tester và Developer để kiểm tra tính đúng đắn của toàn bộ 3 Flow nghiệp vụ chính và phân hệ Xác thực/Phê duyệt tài khoản.

---

## 1. THÔNG TIN MÔI TRƯỜNG & TÀI KHOẢN KIỂM THỬ

### 1.1. Địa chỉ dịch vụ (URLs)
- **Backend API Base URL**: `http://localhost:5214` (hoặc `https://localhost:7023`)
- **Swagger UI Interactive Docs**: [http://localhost:5214/swagger](http://localhost:5214/swagger)
- **Frontend Web UI**: `http://localhost:5173`

---

### 1.2. Danh sách Tài khoản Kiểm thử có sẵn trong CSDL (`123456`)
Tất cả các tài khoản mặc định dưới đây đều có mật khẩu chung là: **`123456`**

| Vai trò (Role) | Email đăng nhập | Mật khẩu | Quyền hạn chính |
| :--- | :--- | :--- | :--- |
| **Club Manager (Quản lý CLB)** | `manager@horseclub.test` | `123456` | Duyệt tài khoản, Thêm/Sửa hồ sơ ngựa, Gán chủ ngựa, Upload ảnh |
| **Head Trainer (HLV Trưởng)** | `trainer@horseclub.test` | `123456` | Đánh giá thể lực, Lập giáo án theo giai đoạn, Sinh lịch tập tuần |
| **Veterinarian (Bác sĩ Thú y)** | `vet@horseclub.test` | `123456` | Khám bệnh, Tạo lệnh khóa huấn luyện, Tái khám & Mở khóa, Tiêm vắc-xin |
| **Groom / Staff (Người chăm sóc)** | `groom@horseclub.test` | `123456` | Bắt đầu/Hoàn thành buổi tập, Nhập điểm đánh giá thể hiện (`PerformanceScore`) |
| **Horse Owner (Chủ ngựa)** | `owner@horseclub.test` | `123456` | Tra cứu danh sách ngựa sở hữu, Xem chi tiết hồ sơ & thể trạng |

---

### 1.3. Danh sách Chiến mã mẫu có sẵn trong CSDL
| HorseID | Tên ngựa (`HorseName`) | Giống (`Breed`) | Giới tính / Năm sinh | Trạng thái ban đầu | Chủ sở hữu (`UserID`) |
| :---: | :--- | :--- | :---: | :---: | :---: |
| **1** | `Silver Arrow` | Thoroughbred | Đực (2020) | `Active` (Khỏe mạnh) | 4 (`Pham Minh Duc`) |
| **2** | `Golden Wind` | Thoroughbred | Cái (2019) | `Active` (Khỏe mạnh) | 4 (`Pham Minh Duc`) |
| **3** | `Midnight Star` | Arabian | Đực (2021) | `Training` (Đang tập) | 4 (`Pham Minh Duc`) |
| **4** | `Ruby Storm` | Thoroughbred | Cái (2018) | `Active` (Khỏe mạnh) | 4 (`Pham Minh Duc`) |
| **5** | `White Comet` | Arabian | Đực (2020) | `Resting` (Nghỉ ngơi) | 4 (`Pham Minh Duc`) |
| **6** | `Blue Horizon` | Thoroughbred | Cái (2022) | `Training` (Ngựa trẻ) | 4 (`Pham Minh Duc`) |

---

## 2. MA TRẬN KỊCH BẢN KIỂM THỬ (TEST CASE CHECKLIST)

### PHÂN HỆ 0: XÁC THỰC & PHÊ DUYỆT TÀI KHOẢN (AUTH & APPROVAL)

#### `TC_AUTH_01`: Đăng ký tài khoản người dùng mới (Luồng chuẩn)
- **Mục tiêu**: Người dùng đăng ký tài khoản mới với vai trò mong muốn, hệ thống đưa vào trạng thái chờ duyệt (`Status='Pending'`).
- **Các bước thực hiện**:
  1. Mở Swagger hoặc Postman, chọn endpoint `POST /api/auth/register`.
  2. Gửi Body JSON:
     ```json
     {
       "fullName": "Le Van Test",
       "email": "testuser01@horseclub.test",
       "phoneNumber": "0911223344",
       "password": "Password123",
       "roleId": 3
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `201 Created` (hoặc `200 OK`).
  - Phản hồi: `"Đăng ký tài khoản thành công. Vui lòng chờ Quản lý phê duyệt."`
  - Trong DB: `Users.Status = 'Pending'`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_AUTH_02`: Đăng nhập khi tài khoản chưa được duyệt (Bắt lỗi nghiệp vụ)
- **Mục tiêu**: Chặn người dùng đăng nhập khi tài khoản đang ở trạng thái `Pending`.
- **Các bước thực hiện**:
  1. Gọi endpoint `POST /api/auth/login`.
  2. Gửi thông tin tài khoản vừa đăng ký ở `TC_AUTH_01`:
     ```json
     {
       "email": "testuser01@horseclub.test",
       "password": "Password123"
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `400 Bad Request`.
  - Thông báo lỗi: `"Tài khoản đang chờ Quản lý CLB duyệt."`
  - Mã lỗi hệ thống: `PENDING_APPROVAL`. Không cấp JWT Token.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_AUTH_03`: Club Manager xem danh sách & Phê duyệt tài khoản
- **Mục tiêu**: Quản lý CLB kiểm tra danh sách tài khoản chờ duyệt và kích hoạt tài khoản.
- **Các bước thực hiện**:
  1. Đăng nhập bằng tài khoản Quản lý (`manager@horseclub.test` / `123456`) qua `POST /api/auth/login`, sao chép Bearer Token.
  2. Gọi `GET /api/manager/account-approvals/pending` (kèm Bearer Token).
     -> Thấy tài khoản `testuser01@horseclub.test` với `Status='Pending'`.
  3. Lấy `userId` của tài khoản đó, gọi `POST /api/manager/account-approvals/{userId}/approve`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Thông báo: `"Phê duyệt tài khoản thành công."`
  - Trong DB: `Users.Status` đổi từ `Pending` thành `Active`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_AUTH_04`: Đăng nhập thành công với tài khoản đã được kích hoạt
- **Mục tiêu**: Người dùng sau khi được duyệt đăng nhập thành công và nhận JWT Token.
- **Các bước thực hiện**:
  1. Gọi `POST /api/auth/login` với email `testuser01@horseclub.test` / `Password123`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Payload trả về chứa: `token` (chuỗi JWT), `userId`, `fullName`, `email`, `roleName`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

### FLOW 1: QUẢN LÝ HỒ SƠ & LÝ LỊCH NGỰA (REQUIRED)

#### `TC_HORSE_01`: Thêm mới hồ sơ ngựa đua đầy đủ thông tin (Happy Path)
- **Vai trò**: Club Manager (`manager@horseclub.test`)
- **Điều kiện tiên quyết**: Đã có Token đăng nhập của Club Manager.
- **Các bước thực hiện**:
  1. Mở Swagger, chọn `POST /api/manager/horses`.
  2. Gửi Body JSON:
     ```json
     {
       "horseName": "Red Thunder",
       "breed": "Thoroughbred",
       "gender": "Male",
       "dateOfBirth": "2021-05-15",
       "color": "Chestnut",
       "weight": 470.50,
       "height": 160.00,
       "ownerUserId": 4,
       "description": "Chiến mã trẻ nhập khẩu, tiềm năng đua nước rút cự ly ngắn."
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `201 Created` (hoặc `200 OK`).
  - Trả về đối tượng `Horse` mới với `HorseID` sinh tự động (ví dụ `7`).
  - `HorseStatus`: `Active`.
  - Trong bảng `HorseHealthStatus`: Bản ghi tự động tạo với `TrangThai = N'Khỏe mạnh'` (hoặc `Đủ điều kiện`).
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_HORSE_02`: Upload ảnh chân dung nhận diện cho ngựa
- **Vai trò**: Club Manager
- **Các bước thực hiện**:
  1. Chọn `POST /api/manager/horses/{id}/photo` với `{id}` là `HorseID` của ngựa vừa tạo ở `TC_HORSE_01`.
  2. Gửi payload ảnh (Base64 chuỗi ảnh hoặc URL):
     ```json
     {
       "photoBase64": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Thuộc tính `ImageURL` trong bảng `Horse` được cập nhật đường dẫn ảnh.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_HORSE_03`: Kiểm tra ràng buộc validation hồ sơ ngựa (Negative Case)
- **Mục tiêu**: Hệ thống phải từ chối khi nhập thông tin phi lý.
- **Các bước thực hiện**:
  1. Gọi `POST /api/manager/horses` với `dateOfBirth` ở tương lai (ví dụ `2030-01-01`) hoặc `weight = -50`.
- **Kết quả mong đợi**:
  - Mã HTTP: `400 Bad Request`.
  - Báo lỗi rõ ràng: Ngày sinh không thể vượt quá hiện tại / Cân nặng không hợp lệ. Không lưu vào CSDL.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_HORSE_04`: Chủ ngựa tra cứu danh sách ngựa sở hữu (Bảo mật quyền dữ liệu)
- **Vai trò**: Horse Owner (`owner@horseclub.test` / `123456`, `UserID = 4`)
- **Các bước thực hiện**:
  1. Đăng nhập với tài khoản `owner@horseclub.test`.
  2. Gắn Token vào Header và gọi `GET /api/owner/horses`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Danh sách trả về chỉ chứa các ngựa có `UserID = 4` (`Silver Arrow`, `Golden Wind`, `Midnight Star`, `Red Thunder`...).
  - Không xuất hiện bất kỳ ngựa nào thuộc chủ khác.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

### FLOW 2: LẬP & THỰC HIỆN GIÁO ÁN HUẤN LUYỆN (REQUIRED)

#### `TC_TRAIN_01`: Đánh giá thể lực ngựa tự động từ dữ liệu sinh trắc
- **Vai trò**: Head Trainer (`trainer@horseclub.test`)
- **Các bước thực hiện**:
  1. Gọi endpoint `GET /api/trainer/horses/1/fitness-assessment`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - JSON trả về chứa:
    - `horseId`: 1
    - `fitnessScore`: Điểm số (0 - 100, ví dụ `85.0`)
    - `fitnessLevel`: `Peak`, `Fit` hoặc `Developing`
    - `isRestricted`: `false` (vì ngựa khỏe mạnh, không có `TrainingLock`).
    - `recommendations`: Chuỗi khuyến nghị cường độ luyện tập.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_TRAIN_02`: Lấy gợi ý giáo án thông minh theo thể trạng
- **Vai trò**: Head Trainer
- **Các bước thực hiện**:
  1. Gọi `GET /api/trainer/horses/1/plan-suggestion?goal=Sprint&targetWeeks=8`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Hệ thống tự sinh cấu trúc các giai đoạn (Phases) gợi ý:
    - Phase 1: Thể lực cơ bản (Tuần 1-3, cường độ Moderate).
    - Phase 2: Nâng cao tốc độ (Tuần 4-6, cường độ High).
    - Phase 3: Bứt phá nước rút (Tuần 7-8, cường độ Maximum).
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_TRAIN_03`: Tạo giáo án huấn luyện mới (Phased Training Plan)
- **Vai trò**: Head Trainer
- **Các bước thực hiện**:
  1. Gọi `POST /api/trainer/training-plans`.
  2. Gửi Body JSON:
     ```json
     {
       "horseId": 1,
       "planName": "Chương trình Bứt tốc Đỉnh cao 2026",
       "objective": "Chuẩn bị thi đấu giải Mùa Thu cự ly 1200m",
       "trainingType": "Speed",
       "startDate": "2026-11-01",
       "endDate": "2026-12-25",
       "intensityLevel": "High",
       "notes": "Theo dõi nhịp tim sau mỗi lượt chạy nước rút."
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `201 Created` (hoặc `200 OK`).
  - Bản ghi mới được tạo trong `TrainingPlans` với `PlanStatus = 'Active'`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_TRAIN_04`: Sinh lịch tập tự động trong tuần (Generate Weekly Schedule)
- **Vai trò**: Head Trainer
- **Các bước thực hiện**:
  1. Lấy `planId` vừa tạo ở `TC_TRAIN_03`.
  2. Gọi `POST /api/trainer/training-plans/{planId}/generate-weekly-schedule`.
  3. Gửi Body:
     ```json
     {
       "startDate": "2026-11-02",
       "assignedToUserId": 3
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Sinh ra các buổi tập mẫu từ Thứ Hai đến Thứ Bảy trong bảng `TrainingSchedule`.
  - `AssignedToUserID`: 3 (`Groom`), `Status = 'Scheduled'`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_TRAIN_05`: Bắt đầu buổi tập luyện (Start Session)
- **Vai trò**: Groom / Trainer (`groom@horseclub.test`)
- **Các bước thực hiện**:
  1. Lấy `scheduleId` của một buổi tập có `Status = 'Scheduled'` (ví dụ buổi tập ở `TC_TRAIN_04`).
  2. Gọi `POST /api/trainer/training-sessions/{scheduleId}/start`.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - `TrainingSchedule.Status` chuyển sang `In Progress`.
  - `StartTime` được ghi nhận thời gian thực.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_TRAIN_06`: Hoàn thành buổi tập & ghi nhận điểm thể hiện (`PerformanceScore`)
- **Vai trò**: Groom / Trainer
- **Các bước thực hiện**:
  1. Gọi `POST /api/trainer/training-sessions/{scheduleId}/complete`.
  2. Gửi Body JSON:
     ```json
     {
       "actualDurationMinutes": 50,
       "actualDistanceKm": 6.2,
       "performanceScore": 92.5,
       "result": "Ngựa hoàn thành xuất sắc các lượt bứt tốc, thể lực dồi dào.",
       "notes": "Nhịp tim hồi phục về mức bình thường sau 12 phút.",
       "trialVideoUrl": "https://storage.horseclub.test/videos/session-101.mp4"
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - `TrainingSchedule.Status` cập nhật thành `Completed`.
  - `PerformanceScore = 92.50`, lưu vết video và cự ly thực tế.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

### FLOW 3: QUẢN LÝ Y TẾ, CHẤN THƯƠNG & KHÓA HUẤN LUYỆN (REQUIRED)

#### `TC_MED_01`: Khám bệnh lâm sàng & phát hiện chấn thương
- **Vai trò**: Veterinarian (`vet@horseclub.test`)
- **Các bước thực hiện**:
  1. Gọi `POST /api/medical/records`.
  2. Gửi Body JSON ghi nhận chấn thương của `Silver Arrow` (HorseID = 1):
     ```json
     {
       "horseId": 1,
       "recordType": "Injury",
       "viTriChanThuong": "Khớp cổ chân trước bên trái (Left Front Fetlock)",
       "symptoms": "Sưng nóng, phản xạ đau khi gập khớp, đi khập khiễng độ 2.",
       "diagnosis": "Viêm bao hoạt dịch khớp cổ chân cấp tính do quá tải vận động.",
       "treatment": "Chườm lạnh 3 lần/ngày, băng ép cố định, nghỉ ngơi tuyệt đối.",
       "medication": "Phenylbutazone 1g / ngày",
       "dosage": "1 gói/ngày trộn thức ăn",
       "followUpDate": "2026-11-10",
       "cost": 1500000.00,
       "notes": "Yêu cầu ngừng toàn bộ hoạt động tập luyện đường dài."
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `201 Created` (hoặc `200 OK`).
  - Bản ghi lưu vào `MedicalRecord` với `ViTriChanThuong` khớp 100% cột bổ sung.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_MED_02`: Ban hành Lệnh khóa huấn luyện (`TrainingLock`)
- **Vai trò**: Veterinarian
- **Mục tiêu**: Khóa tập luyện y tế ngay lập tức để bảo vệ ngựa.
- **Các bước thực hiện**:
  1. Gọi endpoint `POST /api/medical/training-locks`.
  2. Gửi Body:
     ```json
     {
       "horseId": 1,
       "lyDo": "Viêm khớp cổ chân cấp tính - Nguy cơ rách gân nếu vận động",
       "ngayDat": "2026-10-18"
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK` (hoặc `201 Created`).
  - Sinh bản ghi trong bảng `TrainingLock` (`NgayGo = NULL`).
  - Cập nhật tức thời bảng `HorseHealthStatus`: `TrangThai = N'Đang điều trị'` (hoặc `Chấn thương`).
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_MED_03`: [CRITICAL RULE] Chặn HLV xếp lịch hoặc tập luyện khi ngựa bị khóa
- **Vai trò**: Head Trainer (`trainer@horseclub.test`)
- **Mục tiêu**: **Kiểm chứng luật an toàn tuyệt đối của hệ thống: Ngựa đang bị khóa y tế KHÔNG THỂ BẮT ĐẦU BUỔI TẬP.**
- **Các bước thực hiện**:
  1. Đăng nhập bằng tài khoản HLV Trưởng.
  2. Thử gọi `POST /api/trainer/training-sessions/{scheduleId}/start` cho ngựa 1 (ngựa vừa bị khóa ở `TC_MED_02`).
  3. Hoặc gọi `GET /api/trainer/horses/1/fitness-assessment`.
- **Kết quả mong đợi**:
  - Khi gọi Start buổi tập: **Hệ thống chặn ngay lập tức với HTTP 400 Bad Request**.
  - Thông báo lỗi nghiệp vụ rõ ràng: `"Không thể bắt đầu buổi tập: Chiến mã đang chịu Lệnh khóa huấn luyện y tế vì: Viêm khớp cổ chân cấp tính..."`
  - Mã lỗi nghiệp vụ: `RESTRICTED_BY_LOCK`.
  - Khi gọi đánh giá thể lực: `isRestricted: true`, thể lực bị giới hạn mức an toàn.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_MED_04`: Tái khám & Mở khóa huấn luyện khi ngựa hồi phục
- **Vai trò**: Veterinarian
- **Điều kiện**: Ngựa đã hết triệu chứng, bác sĩ tái khám đạt kết quả tốt.
- **Các bước thực hiện**:
  1. Bác sĩ thú y tạo bệnh án tái khám:
     `POST /api/medical/records` (loại `Re-examination`, chẩn đoán: `"Khớp hồi phục tốt, không còn tràn dịch"`). Ghi lại `recordId` tái khám (ví dụ `105`).
  2. Lấy `lockId` đang có hiệu lực qua `GET /api/medical/training-locks/active`.
  3. Gọi `POST /api/medical/training-locks/{lockId}/release`:
     ```json
     {
       "reexamRecordId": 105,
       "releaseReason": "Tái khám X-ray khớp bình thường, kiểm tra bước chạy đạt yêu cầu phục hồi."
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Trong `TrainingLock`: `NgayGo = CURRENT_DATE`, `ReleasedByUserId` ghi nhận mã Bác sĩ, `ReexamRecordId` liên kết bệnh án.
  - Trong `HorseHealthStatus`: Trạng thái tự động chuyển lại thành `Khỏe mạnh` (hoặc `Đủ điều kiện`).
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_MED_05`: Kiểm chứng HLV được phép tiếp tục tập luyện sau khi mở khóa
- **Vai trò**: Head Trainer
- **Các bước thực hiện**:
  1. HLV gọi lại `POST /api/trainer/training-sessions/{scheduleId}/start` cho ngựa 1.
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK` (Thành công).
  - Không còn bị chặn bởi `RESTRICTED_BY_LOCK`.
  - Buổi tập chuyển sang `In Progress`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

#### `TC_MED_06`: Quản lý Lịch Tiêm Chủng Vắc-xin & Tự động sinh lịch nhắc mũi kế tiếp
- **Vai trò**: Veterinarian
- **Các bước thực hiện**:
  1. Lên lịch tiêm chủng mới qua `POST /api/medical/vaccinations`:
     ```json
     {
       "horseId": 1,
       "loai": "Vắc-xin Cúm Ngựa (Equine Influenza)",
       "ngayHen": "2026-10-25",
       "ghiChu": "Mũi tiêm nhắc định kỳ 6 tháng"
     }
     ```
  2. Khi đến hạn, bác sĩ thực hiện tiêm và gọi:
     `POST /api/medical/vaccinations/{scheduleId}/mark-done`
     Kèm body:
     ```json
     {
       "ghiChu": "Đã tiêm bắp 2ml, không phản ứng phụ",
       "scheduleNextIntervalMonths": 6
     }
     ```
- **Kết quả mong đợi**:
  - Mã HTTP: `200 OK`.
  - Lịch tiêm hiện tại cập nhật: `TrangThai = N'Đã tiêm'`.
  - Hệ thống tự động sinh thêm 1 bản ghi mới trong `VaccinationSchedule` vào đúng ngày 6 tháng sau (`2027-04-25`) với trạng thái `Chờ tiêm`.
- **Trạng thái**: [ ] PASS  [ ] FAIL

---

## 3. BẢNG TỔNG HỢP KẾT QUẢ KIỂM THỬ THỦ CÔNG

| STT | Mã Test Case | Luồng nghiệp vụ | Nội dung kiểm thử | Kết quả mong đợi | Đánh giá |
| :---: | :--- | :--- | :--- | :--- | :---: |
| 1 | `TC_AUTH_01` | Auth | Đăng ký tài khoản mới | `Status = 'Pending'`, HTTP 201 | PASS |
| 2 | `TC_AUTH_02` | Auth (Negative) | Đăng nhập tài khoản chờ duyệt | Chặn HTTP 400 `PENDING_APPROVAL` | PASS |
| 3 | `TC_AUTH_03` | Auth | Quản lý phê duyệt tài khoản | `Status = 'Active'`, HTTP 200 | PASS |
| 4 | `TC_AUTH_04` | Auth | Đăng nhập tài khoản đã kích hoạt | Nhận JWT Token, HTTP 200 | PASS |
| 5 | `TC_HORSE_01` | Flow 1 | Thêm mới hồ sơ ngựa đua | Tạo Horse + HealthStatus `Khỏe mạnh` | PASS |
| 6 | `TC_HORSE_02` | Flow 1 | Upload ảnh nhận diện ngựa | Cập nhật `ImageURL` | PASS |
| 7 | `TC_HORSE_03` | Flow 1 (Negative) | Nhập ngày sinh tương lai / cân nặng âm | Chặn HTTP 400 validation | PASS |
| 8 | `TC_HORSE_04` | Flow 1 | Chủ ngựa tra cứu danh sách sở hữu | Chỉ thấy ngựa thuộc UserID của mình | PASS |
| 9 | `TC_TRAIN_01` | Flow 2 | Đánh giá thể lực ngựa tự động | Trả về Fitness Level & Điểm số | PASS |
| 10 | `TC_TRAIN_02` | Flow 2 | Gợi ý giáo án theo thể trạng | Sinh các Phase luyện tập phù hợp | PASS |
| 11 | `TC_TRAIN_03` | Flow 2 | Tạo giáo án huấn luyện mới | Tạo `TrainingPlans` `Status='Active'` | PASS |
| 12 | `TC_TRAIN_04` | Flow 2 | Sinh lịch tập tuần tự động | Tạo các buổi tập `Status='Scheduled'` | PASS |
| 13 | `TC_TRAIN_05` | Flow 2 | Bắt đầu buổi tập | `Status` chuyển thành `In Progress` | PASS |
| 14 | `TC_TRAIN_06` | Flow 2 | Hoàn thành buổi tập & ghi điểm | Lưu `PerformanceScore`, `Completed` | PASS |
| 15 | `TC_MED_01` | Flow 3 | Khám bệnh & phát hiện chấn thương | Lưu `MedicalRecord` có `ViTriChanThuong` | PASS |
| 16 | `TC_MED_02` | Flow 3 | Ban hành Lệnh khóa huấn luyện | Tạo `TrainingLock`, Health `Đang điều trị` | PASS |
| 17 | `TC_MED_03` | Flow 3 (Critical Rule)| Chặn tập luyện khi bị khóa | **Chặn HTTP 400 `RESTRICTED_BY_LOCK`** | PASS |
| 18 | `TC_MED_04` | Flow 3 | Tái khám & Mở khóa huấn luyện | Điền `NgayGo`, Health về `Khỏe mạnh` | PASS |
| 19 | `TC_MED_05` | Flow 3 | Bắt đầu tập sau khi mở khóa | Tập luyện bình thường, HTTP 200 | PASS |
| 20 | `TC_MED_06` | Flow 3 | Tiêm vắc-xin & Tự động nhắc mũi mới | `Đã tiêm` + tự tạo lịch hẹn sau 6 tháng | PASS |

---
*Tài liệu kiểm thử thủ công được chuẩn hóa đồng bộ với mã nguồn Backend và CSDL của dự án SWP391.*
