-- Demo đăng nhập (mật khẩu: 123456)
-- RoleName: dùng đúng tên trong DB của bạn (Head Trainer, Horse Owner, ...)
-- BE/FE map sang roleId: head_trainer, horse_owner, ...

DECLARE @PwdHash VARCHAR(255) = N'$2a$11$34BYkW1vrK3jAEi29agFQ.pJQA7ZdX.4bbQD33Tfub2YEYM0zfCcm';

MERGE [Users] AS target
USING (VALUES
    (N'Trần Văn Trưởng', N'trainer@equitrack.vn', N'Head Trainer', N'Active'),
    (N'Nguyễn Thị Vet', N'vet@equitrack.vn', N'Veterinarian', N'Active'),
    (N'Lê Văn Groom', N'groom@equitrack.vn', N'Groom / Stable Hand', N'Active'),
    (N'Phạm Thị Owner', N'owner@equitrack.vn', N'Horse Owner', N'Active'),
    (N'Hoàng Văn Manager', N'manager@equitrack.vn', N'Club Manager', N'Active')
) AS source ([FullName], [Email], [RoleName], [Status])
ON target.[Email] = source.[Email]
WHEN MATCHED THEN
    UPDATE SET
        [FullName] = source.[FullName],
        [PasswordHash] = @PwdHash,
        [Status] = source.[Status],
        [RoleID] = (SELECT [RoleID] FROM [Role] WHERE [RoleName] = source.[RoleName])
WHEN NOT MATCHED THEN
    INSERT ([FullName], [Email], [PasswordHash], [Status], [CreatedAt], [RoleID])
    VALUES (
        source.[FullName],
        source.[Email],
        @PwdHash,
        source.[Status],
        GETUTCDATE(),
        (SELECT [RoleID] FROM [Role] WHERE [RoleName] = source.[RoleName])
    );
