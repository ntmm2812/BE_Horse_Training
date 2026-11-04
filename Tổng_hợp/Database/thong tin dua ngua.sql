GO
USE Horse_managerment;
GO

    INSERT INTO Role (RoleName)
    VALUES
        ('Head Trainer'),
        ('Veterinarian'),
        ('Groom / Stable Hand'),
        ('Horse Owner'),
        ('Club Manager');
GO
    INSERT INTO Permission (PermissionName, Description, CreatedAt)
    VALUES
        ('VIEW_USERS', N'Xem thông tin tài khoản người dùng', GETDATE()),
        ('MANAGE_USERS', N'Tạo, cập nhật và quản lý tài khoản', GETDATE()),
        ('VIEW_HORSES', N'Xem thông tin chiến mã', GETDATE()),
        ('MANAGE_HORSES', N'Tạo và cập nhật hồ sơ chiến mã', GETDATE()),
        ('VIEW_TRAINING', N'Xem giáo án và lịch huấn luyện', GETDATE()),
        ('MANAGE_TRAINING', N'Lập và quản lý giáo án huấn luyện', GETDATE()),
        ('VIEW_HEALTH', N'Xem chỉ số sức khỏe và thể lực ngựa', GETDATE()),
        ('MANAGE_HEALTH', N'Ghi nhận chỉ số sức khỏe ngựa', GETDATE()),
        ('MANAGE_MEDICAL', N'Quản lý hồ sơ khám và điều trị', GETDATE()),
        ('MANAGE_STABLE', N'Quản lý chuồng và vị trí ngựa', GETDATE()),
        ('MANAGE_CARE', N'Quản lý công việc chăm sóc ngựa', GETDATE()),
        ('MANAGE_FEEDING', N'Quản lý kế hoạch cho ăn', GETDATE()),
        ('MANAGE_SUPPLIES', N'Quản lý vật tư và kho', GETDATE()),
        ('VIEW_RACES', N'Xem thông tin cuộc đua', GETDATE()),
        ('MANAGE_RACES', N'Tạo và quản lý cuộc đua', GETDATE()),
        ('REGISTER_RACES', N'Đăng ký ngựa tham gia cuộc đua', GETDATE()),
        ('VIEW_FINANCE', N'Xem giao dịch tài chính', GETDATE()),
        ('MANAGE_FINANCE', N'Quản lý giao dịch tài chính', GETDATE()),
        ('VIEW_REPORTS', N'Xem báo cáo và thống kê', GETDATE()),
        ('MANAGE_PERMISSIONS', N'Quản lý vai trò và quyền hạn', GETDATE());
GO
    INSERT INTO RolePermission (RoleID, PermissionID, CreatedAt)
    VALUES
        (1, 3, GETDATE()), (1, 4, GETDATE()),
        (1, 5, GETDATE()), (1, 6, GETDATE()),
        (1, 7, GETDATE()), (1, 14, GETDATE()),
        (1, 16, GETDATE()), (1, 19, GETDATE()),
        (2, 3, GETDATE()), (2, 7, GETDATE()),
        (2, 8, GETDATE()), (2, 9, GETDATE()),
        (2, 19, GETDATE()),
        (3, 3, GETDATE()), (3, 5, GETDATE()),
        (3, 7, GETDATE()), (3, 10, GETDATE()),
        (3, 11, GETDATE()), (3, 12, GETDATE()),
        (4, 3, GETDATE()), (4, 5, GETDATE()),
        (4, 7, GETDATE()), (4, 14, GETDATE()),
        (4, 16, GETDATE()), (4, 17, GETDATE()),
        (4, 19, GETDATE()),
        (5, 1, GETDATE()), (5, 2, GETDATE()),
        (5, 3, GETDATE()), (5, 4, GETDATE()),
        (5, 5, GETDATE()), (5, 6, GETDATE()),
        (5, 7, GETDATE()), (5, 8, GETDATE()),
        (5, 9, GETDATE()), (5, 10, GETDATE()),
        (5, 11, GETDATE()), (5, 12, GETDATE()),
        (5, 13, GETDATE()), (5, 14, GETDATE()),
        (5, 15, GETDATE()), (5, 16, GETDATE()),
        (5, 17, GETDATE()), (5, 18, GETDATE()),
        (5, 19, GETDATE()), (5, 20, GETDATE());
GO

    INSERT INTO Users (FullName, Email, PhoneNumber, PasswordHash, DateOfBirth, Gender, Address, Status, CreatedAt, UpdatedAt, GoogleID, RoleID)
    VALUES
        (N'Nguyen Minh Quan', 'trainer@horseclub.test', '0901000001', '$2a$11$RrtMA/P.CA8/V71/gKUq5.e6XLsnzNqhgetVBRyzzAEYkD84j2cti', '1985-03-15', 'Male', N'Ho Chi Minh City', 'Active', GETDATE(), NULL, NULL, 1),
        (N'Tran Thu Ha', 'vet@horseclub.test', '0901000002', '$2a$11$RrtMA/P.CA8/V71/gKUq5.e6XLsnzNqhgetVBRyzzAEYkD84j2cti', '1988-07-20', 'Female', N'Binh Duong', 'Active', GETDATE(), NULL, NULL, 2),
        (N'Le Hoang Nam', 'groom@horseclub.test', '0901000003', '$2a$11$RrtMA/P.CA8/V71/gKUq5.e6XLsnzNqhgetVBRyzzAEYkD84j2cti', '1995-11-08', 'Male', N'Dong Nai', 'Active', GETDATE(), NULL, NULL, 3),
        (N'Pham Minh Duc', 'owner@horseclub.test', '0901000004', '$2a$11$RrtMA/P.CA8/V71/gKUq5.e6XLsnzNqhgetVBRyzzAEYkD84j2cti', '1990-05-12', 'Male', N'Long An', 'Active', GETDATE(), NULL, NULL, 4),
        (N'Hoang Ngoc Mai', 'manager@horseclub.test', '0901000005', '$2a$11$RrtMA/P.CA8/V71/gKUq5.e6XLsnzNqhgetVBRyzzAEYkD84j2cti', '1982-09-25', 'Female', N'Ho Chi Minh City', 'Active', GETDATE(), NULL, NULL, 5);
GO
	
    INSERT INTO Horse (HorseName, Breed, Gender, DateOfBirth, Color, Weight, Height, HorseStatus, Description, CreatedAt, UpdatedAt, LockReason, LockedByUserID, LockedAt,UserID)
    VALUES
        (N'Silver Arrow', N'Thoroughbred', 'Male', '2020-03-12', N'Bay', 485.50, 162.00, 'Active', N'Chiến mã tốc độ tốt, đang trong chương trình huấn luyện thi đấu.', GETDATE(), NULL, NULL, NULL, NULL, 4),
        (N'Golden Wind', N'Thoroughbred', 'Female', '2019-06-21', N'Chestnut', 462.00, 159.50, 'Active', N'Ngựa đua có sức bền tốt.', GETDATE(), NULL, NULL, NULL, NULL, 4),
        (N'Midnight Star', N'Arabian', 'Male', '2021-01-08', N'Black', 410.00, 155.00, 'Training', N'Ngựa trẻ đang làm quen với cường độ huấn luyện.', GETDATE(), NULL, NULL, NULL, NULL, 4),
        (N'Ruby Storm', N'Thoroughbred', 'Female', '2018-11-02', N'Dark Bay', 478.00, 161.00, 'Active', N'Có kinh nghiệm thi đấu ở cự ly trung bình.', GETDATE(), NULL, NULL, NULL, NULL, 4),
        (N'White Comet', N'Arabian', 'Male', '2020-08-17', N'Gray', 425.00, 157.00, 'Resting', N'Đang nghỉ phục hồi sau giai đoạn huấn luyện.', GETDATE(), NULL, NULL, NULL, NULL, 4),
        (N'Blue Horizon', N'Thoroughbred', 'Female', '2022-04-10', N'Gray', 395.00, 153.50, 'Training', N'Ngựa trẻ trong giai đoạn phát triển thể lực.', GETDATE(), NULL, NULL, NULL, NULL, 4);
GO

	INSERT INTO [HorseHealthStatus] ([HorseID], [TrangThai], [NgayCapNhat])
	SELECT h.[HorseID], N'Đủ điều kiện', GETUTCDATE()
	FROM [Horse] h
	WHERE NOT EXISTS (
		SELECT 1 FROM [HorseHealthStatus] s WHERE s.[HorseID] = h.[HorseID]
	);

GO

    INSERT INTO TrainingPlans (PlanName, Objective, TrainingType, StartDate, EndDate, IntensityLevel, PlanStatus, Notes, CreatedAt, UpdatedAt, HorseID)
    VALUES
        (N'Speed Foundation - Silver Arrow', N'Cải thiện tốc độ nước rút và phản xạ xuất phát.', 'Speed', '2026-10-01', '2026-11-15', 'High', 'Active', N'Tăng cường độ từ từ, theo dõi nhịp tim sau mỗi buổi.', GETDATE(), NULL, 1),
        (N'Endurance Program - Golden Wind', N'Tăng sức bền ở cự ly trung bình.', 'Endurance', '2026-10-01', '2026-12-01', 'Medium', 'Active', N'Kết hợp chạy bền và ngày hồi phục.', GETDATE(), NULL, 2),
        (N'Basic Conditioning - Midnight Star', N'Phát triển nền tảng thể lực.', 'Conditioning', '2026-10-05', '2026-12-05', 'Low', 'Active', N'Ưu tiên kỹ thuật và làm quen đường chạy.', GETDATE(), NULL, 3),
        (N'Race Preparation - Ruby Storm', N'Chuẩn bị cho cuộc đua cự ly 1600 mét.', 'Race Preparation', '2026-09-20', '2026-10-25', 'High', 'Active', N'Có ngày nghỉ trước buổi chạy tốc độ cao.', GETDATE(), NULL, 4),
        (N'Recovery Plan - White Comet', N'Phục hồi thể lực và trở lại tập luyện an toàn.', 'Recovery', '2026-10-01', '2026-10-31', 'Low', 'Active', N'Theo dõi phản ứng của ngựa sau vận động.', GETDATE(), NULL, 5),
        (N'Youth Development - Blue Horizon', N'Phát triển sức bền cơ bản cho ngựa trẻ.', 'Conditioning', '2026-10-10', '2026-12-10', 'Low', 'Planned', N'Không tăng tải quá nhanh.', GETDATE(), NULL, 6);

GO
     INSERT INTO TrainingSchedule (SessionTitle, TrainingDate, StartTime, EndTime, TrainingType, DurationMinutes, DistanceKm, PerformanceScore, Result, Status, Notes, CreatedAt, TrialVideoUrl, PlanID, AssignedToUserID)
     VALUES
		(N'Khởi động và chạy nhẹ - Silver Arrow', '2026-10-12', '2026-10-12T06:00:00', '2026-10-12T06:45:00', 'Warm-up', 45, 5.00, 85.00, N'Hoàn thành bài khởi động, thể trạng ổn định.', 'Completed', N'Kiểm tra nhịp thở và dáng chạy sau buổi tập.', GETDATE(), NULL, 1, 4),
		(N'Luyện tốc độ - Golden Wind', '2026-10-13', '2026-10-13T06:00:00', '2026-10-13T07:00:00', 'Speed', 60, 8.50, 90.00, N'Hoàn thành các lượt chạy tốc độ theo kế hoạch.', 'Completed', N'Tăng tốc từ từ, theo dõi nhịp tim sau mỗi lượt.', GETDATE(), NULL, 1, 4),
		(N'Luyện sức bền - Midnight Star', '2026-10-14', '2026-10-14T06:15:00', '2026-10-14T07:15:00', 'Endurance', 60, 10.00, 82.50, N'Hoàn thành phần lớn bài tập sức bền.', 'Completed', N'Quan sát khả năng phục hồi sau vận động.', GETDATE(), NULL, 2, 4),
		(N'Chạy kỹ thuật - Ruby Storm', '2026-10-15', '2026-10-15T06:00:00', '2026-10-15T06:40:00', 'Technique', 40, 4.50, 88.00, N'Cải thiện tư thế và nhịp chạy.', 'Completed', N'Chú ý chuyển hướng và giữ nhịp ổn định.', GETDATE(), NULL, 2, 4),
		(N'Phục hồi nhẹ - White Comet', '2026-10-16', '2026-10-16T07:00:00', '2026-10-16T07:30:00', 'Recovery', 30, 2.00, NULL, NULL, 'Scheduled', N'Buổi tập nhẹ, điều chỉnh cường độ theo tình trạng ngựa.', GETDATE(), NULL, 3, 4),
		(N'Kiểm tra thể lực - Blue Horizon', '2026-10-17', '2026-10-17T06:00:00', '2026-10-17T06:50:00', 'Fitness', 50, 6.00, NULL, NULL, 'Scheduled', N'Đánh giá thể lực trước khi tăng cường độ huấn luyện.', GETDATE(), NULL, 3, 4),
		(N'Luyện xuất phát - Silver Arrow', '2026-10-18', '2026-10-18T06:00:00', '2026-10-18T06:45:00', 'Sprint', 45, 5.50, NULL, NULL, 'Scheduled', N'Luyện phản xạ xuất phát và tăng tốc ở cự ly ngắn.', GETDATE(), NULL, 4, 4),
		(N'Đánh giá cuối tuần - Golden Wind', '2026-10-19', '2026-10-19T06:00:00', '2026-10-19T07:00:00', 'Assessment', 60, 8.00, NULL, NULL, 'Scheduled', N'Tổng hợp tiến độ và điều chỉnh kế hoạch tuần tiếp theo.', GETDATE(), NULL, 4, 4);

GO

    INSERT INTO HealthMetric (HorseID, HeartRate, RespiratoryRate, BodyTemperature, BloodOxygen, SpeedKmh, DistanceKm, StressLevel, RecordedAt, DataSource, Notes)
    VALUES
        (1, 42.00, 12.00, 37.80, 98.00, 0.00, 0.00, 'Low', '2026-10-01T05:30:00', 'Manual', N'Đo lúc nghỉ trước tập.'),
        (1, 88.00, 30.00, 38.40, 97.00, 48.50, 6.50, 'Medium', '2026-10-03T06:50:00', 'Wearable', N'Đo sau bài tập tốc độ.'),
        (2, 40.00, 11.00, 37.70, 98.00, 0.00, 0.00, 'Low', '2026-10-02T05:45:00', 'Manual', N'Trạng thái nghỉ bình thường.'),
        (2, 76.00, 25.00, 38.20, 98.00, 38.00, 7.00, 'Medium', '2026-10-02T07:05:00', 'Wearable', N'Sau bài chạy bền.'),
        (3, 44.00, 13.00, 37.90, 97.00, 0.00, 0.00, 'Low', '2026-10-06T06:45:00', 'Manual', N'Kiểm tra trước tập.'),
        (3, 68.00, 22.00, 38.10, 98.00, 24.00, 3.00, 'Low', '2026-10-06T07:30:00', 'Wearable', N'Đáp ứng tốt với bài tập nhẹ.'),
        (4, 43.00, 12.00, 37.80, 98.00, 0.00, 0.00, 'Low', '2026-10-04T05:30:00', 'Manual', N'Kiểm tra trước tập.'),
        (4, 92.00, 32.00, 38.50, 97.00, 46.00, 5.50, 'Medium', '2026-10-04T06:45:00', 'Wearable', N'Sau bài chuẩn bị thi đấu.'),
        (5, 39.00, 10.00, 37.60, 99.00, 0.00, 0.00, 'Low', '2026-10-07T06:45:00', 'Manual', N'Đang trong giai đoạn hồi phục.'),
        (5, 52.00, 16.00, 37.90, 98.00, 8.00, 1.50, 'Low', '2026-10-07T07:25:00', 'Wearable', N'Sau đi bộ nhẹ.'),
        (6, 41.00, 12.00, 37.70, 98.00, 0.00, 0.00, 'Low', '2026-10-11T06:00:00', 'Manual', N'Kiểm tra trước tập.'),
        (6, 65.00, 20.00, 38.00, 98.00, 18.00, 2.50, 'Low', '2026-10-11T07:00:00', 'Wearable', N'Buổi tập nền tảng.');
GO

    INSERT INTO MedicalRecord (RecordType, ViTriChanThuong, Diagnosis, Symptoms, Treatment, Medication, Dosage, RecordDate, FollowUpDate, Status, Cost, Notes, CreatedAt, HorseID, CreateBy)
    VALUES
        ('Routine Check', N'Không có chấn thương', N'Kiểm tra sức khỏe định kỳ', N'Không có triệu chứng bất thường', N'Khám tổng quát và theo dõi chỉ số', NULL, NULL, '2026-09-20T09:00:00', '2026-12-20', 'Completed', 500000, N'Khuyến nghị kiểm tra định kỳ.', GETDATE(), 1, 2),
        ('Dental', N'Không có chấn thương', N'Cần kiểm tra răng định kỳ', N'Có dấu hiệu khó chịu khi nhai', N'Kiểm tra răng và vệ sinh', NULL, NULL, '2026-09-22T10:00:00', '2027-03-22', 'Completed', 800000, N'Theo dõi khả năng ăn.', GETDATE(), 2, 2),
        ('Lameness Assessment', N'Chân trước trái', N'Đánh giá dáng đi chân trước', N'Đi hơi khập khiễng sau vận động', N'Nghỉ ngơi và tái khám nếu triệu chứng kéo dài', NULL, NULL, '2026-10-01T11:00:00', '2026-10-15', 'Monitoring', 600000, N'Không tăng cường độ khi chưa đánh giá lại.', GETDATE(), 5, 2),
        ('Vaccination', N'Không có chấn thương', N'Tiêm phòng định kỳ', N'Không có triệu chứng', N'Tiêm phòng theo lịch của bác sĩ thú y', N'Vaccine theo chỉ định', N'Theo hướng dẫn nhà sản xuất', '2026-09-25T08:30:00', '2027-03-25', 'Completed', 1200000, N'Ghi nhận ngày tiêm và theo dõi phản ứng.', GETDATE(), 3, 2),
        ('Skin Examination', N'Vùng lưng / hông', N'Kiểm tra da', N'Vùng da khô nhẹ', N'Vệ sinh khu vực và theo dõi', NULL, NULL, '2026-10-03T14:00:00', '2026-10-17', 'Monitoring', 300000, N'Báo bác sĩ nếu vùng da lan rộng.', GETDATE(), 4, 2),
        ('Pre-race Examination', N'Không có chấn thương', N'Khám trước cuộc đua', N'Không phát hiện dấu hiệu bất thường',  N'Đủ điều kiện theo đánh giá tại thời điểm khám', NULL, NULL, '2026-10-05T09:00:00', '2026-10-20', 'Completed', 700000, N'Kiểm tra lại trước ngày thi đấu.', GETDATE(), 1, 2);

GO

    INSERT INTO TrainingLock (HorseID, VetID, LyDo, NgayDat, NgayGo)
    VALUES
        (4, 2, N'Viêm gân nhẹ chân trước', DATEADD(day, -20, CAST(GETDATE() AS DATE)), DATEADD(day, -12, CAST(GETDATE() AS DATE))),
        (3, 2, N'Viêm gân gấp chân trước trái', DATEADD(day, -12, CAST(GETDATE() AS DATE)), NULL);

    UPDATE [HorseHealthStatus]
    SET [TrangThai] = N'Đang điều trị', [NgayCapNhat] = GETDATE()
    WHERE [HorseID] = 3;

GO

    INSERT INTO VaccinationSchedule (HorseID, Loai, NgayHen, TrangThai, GhiChu)
    VALUES
        (1, N'Cúm ngựa', DATEADD(day, -40, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (1, N'Uốn ván', DATEADD(day, 4, CAST(GETDATE() AS DATE)), N'Chờ tiêm', NULL),
        (2, N'Uốn ván', DATEADD(day, -60, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (2, N'Cúm ngựa', DATEADD(day, -15, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (2, N'Viêm não Nhật Bản', DATEADD(day, -75, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (3, N'Cúm ngựa', DATEADD(day, -90, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (3, N'Uốn ván', DATEADD(day, -130, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (4, N'Uốn ván', DATEADD(day, -30, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (4, N'Cúm ngựa', DATEADD(day, -10, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (4, N'Viêm não Nhật Bản', DATEADD(day, 6, CAST(GETDATE() AS DATE)), N'Chờ tiêm', NULL),
        (5, N'Cúm ngựa', DATEADD(day, -50, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (5, N'Uốn ván', DATEADD(day, -95, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (6, N'Uốn ván', DATEADD(day, -100, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (6, N'Cúm ngựa', DATEADD(day, -3, CAST(GETDATE() AS DATE)), N'Chờ tiêm', NULL),
        (6, N'Viêm não Nhật Bản', DATEADD(day, -160, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (2, N'Tẩy giun', DATEADD(day, 45, CAST(GETDATE() AS DATE)), N'Chờ tiêm', NULL),
        (1, N'Dại', DATEADD(day, -70, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (2, N'Dại', DATEADD(day, -85, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (4, N'Dại', DATEADD(day, -120, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (6, N'Dại', DATEADD(day, -65, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (3, N'Tẩy giun', DATEADD(day, -25, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (5, N'Tẩy giun', DATEADD(day, -18, CAST(GETDATE() AS DATE)), N'Đã tiêm', NULL),
        (5, N'Viêm não Nhật Bản', DATEADD(day, -5, CAST(GETDATE() AS DATE)), N'Chờ tiêm', NULL);

GO
    INSERT INTO Stable (StableName, Location, Capacity, CurrentOccupancy, StableType, Status, Description, CreatedAt, UpdatedAt)
    VALUES
        (N'Stable A - Racing', N'Khu phía Đông', 8, 3, 'Racing', 'Active', N'Khu dành cho chiến mã đang huấn luyện thi đấu.', GETDATE(), NULL),
        (N'Stable B - Recovery', N'Khu phía Tây', 6, 1, 'Recovery', 'Active',  N'Khu nghỉ dưỡng và phục hồi.', GETDATE(), NULL),
        (N'Stable C - Young Horses', N'Khu phía Bắc', 10, 2, 'Training', 'Active',  N'Khu dành cho ngựa trẻ và huấn luyện cơ bản.', GETDATE(), NULL);

GO
    INSERT INTO StableAssignment (HorseID, StableID, StartDate, EndDate, Status, Notes)
    VALUES
        (1, 1, '2026-09-01', NULL, 'Active', N'Phân chuồng khu thi đấu.'),
        (2, 1, '2026-09-01', NULL, 'Active', N'Phân chuồng khu thi đấu.'),
        (3, 3, '2026-10-01', NULL, 'Active', N'Ngựa trẻ đang huấn luyện.'),
        (4, 1, '2026-08-15', NULL, 'Active', N'Chuẩn bị cho cuộc đua sắp tới.'),
        (5, 2, '2026-10-01', NULL, 'Active', N'Đang phục hồi.'),
        (6, 3, '2026-10-10', NULL, 'Active', N'Ngựa trẻ mới nhận vào chương trình.');
GO

    INSERT INTO CareTask (HorseID, TaskName, TaskType, Description, ScheduledAt, CompletedAt, Priority, Status, CompletionNotes, UserID, CreatedAt)
    VALUES
		(1, N'Chải lông Silver Arrow', 'Grooming', N'Chải lông và kiểm tra da ngựa sau buổi tập.', '2026-10-12T07:00:00', '2026-10-12T07:20:00', 'Medium', 'Completed', N'Đã chải lông và kiểm tra da, không phát hiện bất thường.', 4, GETDATE()),
		(2, N'Kiểm tra móng Golden Wind', 'Hoof Care', N'Kiểm tra móng và làm sạch đất cát bám trên móng.', '2026-10-12T07:30:00', '2026-10-12T07:45:00', 'High', 'Completed', N'Móng sạch, không phát hiện dấu hiệu tổn thương.', 4, GETDATE()),
		(3, N'Cho Midnight Star uống nước', 'Hydration', N'Đảm bảo ngựa được cung cấp đủ nước sau vận động.', '2026-10-12T08:00:00', NULL, 'High', 'Pending', NULL, 4, GETDATE()),
		(4, N'Kiểm tra thể trạng Ruby Storm', 'Health Check', N'Quan sát dáng đi, nhịp thở và biểu hiện mệt mỏi.', '2026-10-12T08:30:00', NULL, 'High', 'Pending', NULL, 4, GETDATE()),
		(5, N'Vệ sinh chuồng White Comet', 'Stable Cleaning', N'Dọn chất thải và thay lớp lót chuồng.', '2026-10-12T09:00:00', NULL, 'Medium', 'In Progress', NULL, 4, GETDATE()),
		(6, N'Kiểm tra thức ăn Blue Horizon', 'Feeding', N'Kiểm tra khẩu phần và chất lượng thức ăn trước khi cho ăn.', '2026-10-12T09:30:00', '2026-10-12T09:40:00', 'Medium', 'Completed', N'Thức ăn đạt yêu cầu, đã kiểm tra khẩu phần.', 4, GETDATE()),
		(1, N'Kiểm tra chân sau huấn luyện', 'Health Check', N'Kiểm tra chân và khớp sau bài tập tốc độ.', '2026-10-12T10:00:00', NULL, 'High', 'Pending', NULL, 4, GETDATE()),
		(2, N'Chuẩn bị khu vực nghỉ ngơi', 'Resting', N'Đảm bảo khu vực nghỉ sạch sẽ và thông thoáng.', '2026-10-12T10:30:00', NULL, 'Low', 'Pending', NULL, 4, GETDATE()),
		(3, N'Vệ sinh và kiểm tra dụng cụ', 'Equipment', N'Làm sạch dụng cụ chăm sóc được sử dụng trong buổi tập.', '2026-10-12T11:00:00', '2026-10-12T11:15:00', 'Low', 'Completed', N'Dụng cụ đã được vệ sinh và cất đúng nơi quy định.', 4, GETDATE()),
		(4, N'Theo dõi phục hồi Ruby Storm', 'Recovery', N'Theo dõi tình trạng phục hồi sau khi vận động.', '2026-10-12T14:00:00', NULL, 'Medium', 'Pending', NULL, 4, GETDATE());

GO
    INSERT INTO FeedingPlan  (HorseID, UserID, PlanName, StartDate, EndDate, DailyMealCount, Status, SpecialInstructions, CreatedAt, UpdatedAt)
    VALUES
        (1, 3, N'Performance Diet - Silver Arrow', '2026-10-01', '2026-11-15', 3, 'Active', N'Chia khẩu phần thành nhiều bữa; điều chỉnh theo bác sĩ thú y.', GETDATE(), NULL),
        (2, 3, N'Endurance Diet - Golden Wind', '2026-10-01', '2026-12-01', 3, 'Active', N'Đảm bảo nước sạch luôn sẵn có.', GETDATE(), NULL),
        (3, 3, N'Young Horse Diet - Midnight Star', '2026-10-05', '2026-12-05', 3, 'Active', N'Tăng khẩu phần từ từ theo thể trạng.', GETDATE(), NULL),
        (4, 3, N'Race Preparation Diet - Ruby Storm', '2026-09-20', '2026-10-25', 3,  'Active', N'Theo dõi mức ăn trước ngày thi đấu.', GETDATE(), NULL),
        (5, 3, N'Recovery Diet - White Comet', '2026-10-01', '2026-10-31', 3, 'Active', N'Khẩu phần điều chỉnh theo khuyến nghị thú y.', GETDATE(), NULL);

GO
    INSERT INTO FeedingPlanDetail (FeedingPlanID, MealName, FeedName, Quantity, Unit, FeedingTime, Calories, Instructions, CreatedAt)
    VALUES
        (1, N'Breakfast', N'Quality hay', 4.00, 'kg', '06:00', 650.00, N'Cho ăn sau khi kiểm tra nước.', GETDATE()),
        (1, N'Lunch', N'Performance pellets', 2.00, 'kg', '12:00', 720.00, N'Không cho ăn ngay trước khi tập.', GETDATE()),
        (1, N'Dinner', N'Quality hay', 4.00, 'kg', '17:30', 650.00, N'Kiểm tra lượng ăn còn lại.', GETDATE()),
        (2, N'Breakfast', N'Grass hay', 4.50, 'kg', '06:00', 600.00, N'Khẩu phần duy trì sức bền.', GETDATE()),
        (2, N'Lunch', N'Balanced grain mix', 1.80, 'kg', '12:00', 680.00, N'Điều chỉnh theo khối lượng vận động.', GETDATE()),
        (2, N'Dinner', N'Grass hay', 4.50, 'kg', '17:30', 600.00, N'Luôn cung cấp nước sạch.', GETDATE()),
        (3, N'Breakfast', N'Young horse pellets', 1.50, 'kg', '06:30', 500.00, N'Không thay đổi khẩu phần đột ngột.', GETDATE()),
        (3, N'Lunch', N'Hay', 3.00, 'kg', '12:00', 450.00, N'Kiểm tra chất lượng cỏ.', GETDATE()),
        (4, N'Breakfast', N'Performance pellets', 2.00, 'kg', '06:00', 720.00, N'Theo dõi khả năng tiêu hóa.', GETDATE()),
        (5, N'Breakfast', N'Soft hay', 3.00, 'kg', '07:00', 420.00, N'Điều chỉnh theo hướng dẫn bác sĩ thú y.', GETDATE());
GO
    INSERT INTO Supply (UserID, SupplyName, Category, Description, QuantityInStock, Unit, UnitPrice, ExpiryDate, StorageLocation, Status, CreatedAt, UpdatedAt)
    VALUES
        (5, N'Quality Hay', 'Feed', N'Cỏ khô dùng cho khẩu phần hằng ngày.', 250.00, 'kg', 18000.00, '2027-06-30', N'Feed Store A', 'Available', GETDATE(), NULL),
        (5, N'Performance Pellets', 'Feed', N'Thức ăn viên cho ngựa vận động.', 120.00, 'kg', 32000.00, '2027-03-31', N'Feed Store A', 'Available', GETDATE(), NULL),
        (5, N'Electrolyte Supplement', 'Supplement', N'Sản phẩm bổ sung điện giải; sử dụng theo hướng dẫn chuyên môn.', 25.00, 'pack', 150000.00, '2027-01-31', N'Medical Cabinet', 'Available', GETDATE(), NULL),
        (5, N'Grooming Brush', 'Grooming', N'Bàn chải chăm sóc lông ngựa.', 15.00, 'piece', 85000.00, NULL, N'Equipment Room', 'Available', GETDATE(), NULL),
        (5, N'Hoof Pick', 'Grooming', N'Dụng cụ vệ sinh móng.', 12.00, 'piece', 60000.00, NULL, N'Equipment Room', 'Available', GETDATE(), NULL),
        (5, N'Bandage Roll', 'Medical', N'Băng cuộn dùng trong chăm sóc.', 40.00, 'roll', 45000.00, '2028-12-31', N'Medical Cabinet', 'Available', GETDATE(), NULL),
        (5, N'Disinfectant', 'Cleaning', N'Dung dịch vệ sinh dụng cụ theo hướng dẫn sử dụng.', 18.00, 'bottle', 95000.00, '2027-09-30', N'Cleaning Store', 'Available', GETDATE(), NULL),
        (5, N'Water Bucket', 'Stable Equipment', N'Xô đựng nước cho chuồng.', 10.00, 'piece', 120000.00, NULL, N'Equipment Room', 'Available', GETDATE(), NULL),
        (5, N'Stable Bedding', 'Bedding', N'Vật liệu lót chuồng.', 80.00, 'bag', 70000.00, '2028-12-31', N'Bedding Store', 'Available', GETDATE(), NULL),
        (5, N'First Aid Kit', 'Medical', N'Bộ dụng cụ sơ cứu cơ bản.', 5.00, 'kit', 450000.00, '2027-12-31', N'Medical Cabinet', 'Available', GETDATE(), NULL);

GO
    INSERT INTO Race (RaceName, RaceCode, RaceType, Location, RaceDate, RegistrationDeadline, DistanceMeters, MaxParticipants, EntryFee, PrizePool, Status, Description, UserID, CreatedAt, UpdatedAt)
    VALUES
        (N'Autumn Sprint Cup', 'ASC-2026', 'Sprint', N'Racecourse A',  '2026-10-25T09:00:00', '2026-10-20T17:00:00', 1200, 12, 1500000, 50000000, 'Open', N'Cuộc đua nước rút mùa thu.', 5, GETDATE(), NULL),
        (N'Autumn Endurance Challenge', 'AEC-2026', 'Endurance', N'Racecourse A', '2026-11-08T08:00:00', '2026-11-01T17:00:00', 2400, 10, 2000000, 75000000, 'Open', N'Cuộc đua thử thách sức bền.', 5, GETDATE(), NULL),
        (N'Young Horse Trial', 'YHT-2026', 'Trial', N'Training Track B', '2026-11-15T08:30:00', '2026-11-10T17:00:00', 1000, 8, 800000, 20000000, 'Open', N'Cuộc thi đánh giá ngựa trẻ.', 5, GETDATE(), NULL),
        (N'Winter Championship', 'WC-2026', 'Championship', N'Racecourse A', '2026-12-06T09:00:00', '2026-11-28T17:00:00', 1600, 16, 2500000, 100000000, 'Planned', N'Giải vô địch cuối năm.', 5, GETDATE(), NULL);

GO
    INSERT INTO RaceRegistration (RaceID, HorseID, UserID, RegistrationDate, RegistrationStatus,  HorseNumber, FinishPosition, FinishTimeSeconds, PrizeMoney,  ResultStatus, Notes, CreatedAt, UpdatedAt)
    VALUES
        (1, 1, 4, '2026-10-02T10:00:00', 'Registered', 101, NULL, NULL, NULL, NULL, N'Đăng ký hợp lệ.', GETDATE(), NULL),
        (1, 2, 4, '2026-10-02T10:15:00', 'Registered', 102, NULL, NULL, NULL, NULL, N'Đăng ký hợp lệ.', GETDATE(), NULL),
        (1, 4, 4, '2026-10-03T09:30:00', 'Registered', 103, NULL, NULL, NULL, NULL, N'Chờ kiểm tra trước giải.', GETDATE(), NULL),
        (2, 2, 4, '2026-10-03T11:00:00', 'Registered', 201, NULL, NULL, NULL, NULL, N'Tham gia cự ly sức bền.', GETDATE(), NULL),
        (2, 4, 4, '2026-10-03T11:20:00', 'Registered', 202, NULL, NULL, NULL, NULL, N'Tham gia cự ly sức bền.', GETDATE(), NULL),
        (3, 3, 4, '2026-10-06T14:00:00', 'Registered', 301, NULL, NULL, NULL, NULL, N'Ngựa trẻ tham gia đánh giá.', GETDATE(), NULL),
        (3, 6, 4, '2026-10-06T14:15:00', 'Registered', 302, NULL, NULL, NULL, NULL, N'Ngựa trẻ tham gia đánh giá.', GETDATE(), NULL),
        (4, 1, 4, '2026-10-07T10:00:00', 'Pending', 401, NULL, NULL, NULL, NULL, N'Chờ xác nhận điều kiện thi đấu.', GETDATE(), NULL);
GO
    INSERT INTO FinancialTransaction (UserID, TransactionCode, TransactionType, Category, Amount, Currency, [Date], PaymentMethod, Status, Description, CreatedAt, UpdatedAt)
    VALUES
        (5, 'HMT-2026-0001', 'Expense', 'Feed', 4500000, 'VND', '2026-10-01T09:00:00', 'Bank Transfer', 'Completed', N'Mua cỏ khô và thức ăn viên.', GETDATE(), NULL),
        (5, 'HMT-2026-0002', 'Expense', 'Veterinary', 1200000, 'VND', '2026-10-02T10:00:00', 'Cash', 'Completed', N'Chi phí kiểm tra sức khỏe.', GETDATE(), NULL),
        (5, 'HMT-2026-0003', 'Expense', 'Equipment', 850000, 'VND', '2026-10-03T14:30:00', 'Bank Transfer', 'Completed', N'Mua dụng cụ chăm sóc.', GETDATE(), NULL),
        (4, 'HMT-2026-0004', 'Expense', 'Race Entry', 1500000, 'VND', '2026-10-02T10:30:00',  'Bank Transfer', 'Completed', N'Lệ phí đăng ký Autumn Sprint Cup.', GETDATE(), NULL),
        (4, 'HMT-2026-0005', 'Expense', 'Race Entry', 2000000, 'VND', '2026-10-03T11:30:00', 'Bank Transfer', 'Pending', N'Lệ phí đăng ký Autumn Endurance Challenge.', GETDATE(), NULL),
        (5, 'HMT-2026-0006', 'Income', 'Membership', 10000000, 'VND', '2026-10-04T08:00:00', 'Bank Transfer', 'Completed', N'Khoản thu hội viên câu lạc bộ.', GETDATE(), NULL),
        (5, 'HMT-2026-0007', 'Expense', 'Stable Maintenance', 2300000, 'VND', '2026-10-05T15:00:00', 'Cash', 'Completed', N'Bảo trì chuồng ngựa.', GETDATE(), NULL),
        (5, 'HMT-2026-0008', 'Expense', 'Staff', 8000000, 'VND', '2026-10-06T09:00:00', 'Bank Transfer', 'Completed', N'Chi phí nhân sự chăm sóc.', GETDATE(), NULL),
        (4, 'HMT-2026-0009', 'Income', 'Prize', 5000000, 'VND', '2026-09-15T16:00:00', 'Bank Transfer', 'Completed', N'Tiền thưởng từ giải trước; dữ liệu mẫu.', GETDATE(), NULL),
        (5, 'HMT-2026-0010', 'Expense', 'Medical Supplies', 950000, 'VND', '2026-10-07T13:00:00', 'Cash', 'Pending', N'Mua bổ sung vật tư y tế.', GETDATE(), NULL);
GO
	INSERT INTO AuditLog (Action, TableName, RecordID, OldValue, NewValue, IPAddress, CreatedAt, UserID)
	VALUES
		('INSERT', 'Horse', 1, NULL, N'{"HorseName":"Silver Arrow","HorseStatus":"Active"}', '192.168.1.10', GETDATE(), 4),
		('UPDATE', 'Horse', 2, N'{"HorseStatus":"Training"}', N'{"HorseStatus":"Active"}', '192.168.1.11', GETDATE(), 4),
		('INSERT', 'TrainingPlans', 1, NULL, N'{"PlanName":"Speed Training Plan"}', '192.168.1.12', GETDATE(), 4),
		('UPDATE', 'TrainingSchedule', 1, N'{"Status":"Scheduled"}', N'{"Status":"Completed"}', '192.168.1.10', GETDATE(), 4),
		('INSERT', 'CareTask', 1, NULL, N'{"TaskName":"Grooming","Status":"Pending"}', '192.168.1.13', GETDATE(), 4),
		('UPDATE', 'CareTask', 2, N'{"Status":"Pending"}', N'{"Status":"Completed"}', '192.168.1.11', GETDATE(), 4),
		('INSERT', 'MedicalRecord', 1, NULL, N'{"Diagnosis":"Routine health check"}', '192.168.1.12', GETDATE(), 4),
		('UPDATE', 'Users', 4, N'{"RoleID":3}', N'{"RoleID":2}', '192.168.1.10', GETDATE(), 1),
		('DELETE', 'RaceRegistration', 3, N'{"RegistrationID":3,"Status":"Registered"}', NULL, '192.168.1.14', GETDATE(), 4),
		('LOGIN', 'Users', 4, NULL, N'{"Result":"Success"}', '192.168.1.10', GETDATE(), 4),
		('LOGOUT', 'Users', 4, NULL, N'{"Result":"Success"}', '192.168.1.10', GETDATE(), 4),
		('UPDATE', 'Stable', 1, N'{"StableName":"Stable A"}', N'{"StableName":"Main Training Stable"}', '192.168.1.15', GETDATE(), 1);
GO