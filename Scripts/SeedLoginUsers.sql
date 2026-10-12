-- Demo đăng nhập (mật khẩu: 123456)
-- RoleName khớp roleId trên FE

DECLARE @PwdHash VARCHAR(255) = N'$2a$11$34BYkW1vrK3jAEi29agFQ.pJQA7ZdX.4bbQD33Tfub2YEYM0zfCcm';

IF NOT EXISTS (SELECT 1 FROM [Role] WHERE [RoleName] = N'head_trainer')
BEGIN
    INSERT INTO [Role] ([RoleName]) VALUES
    (N'head_trainer'),
    (N'veterinarian'),
    (N'groom'),
    (N'horse_owner'),
    (N'club_manager');
END;

MERGE [Users] AS target
USING (VALUES
    (N'Trần Văn Trưởng', N'trainer@equitrack.vn', N'head_trainer', N'Active'),
    (N'Nguyễn Thị Vet', N'vet@equitrack.vn', N'veterinarian', N'Active'),
    (N'Lê Văn Groom', N'groom@equitrack.vn', N'groom', N'Active'),
    (N'Phạm Thị Owner', N'owner@equitrack.vn', N'horse_owner', N'Active'),
    (N'Hoàng Văn Manager', N'manager@equitrack.vn', N'club_manager', N'Active')
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
