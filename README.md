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
