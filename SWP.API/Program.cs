using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SWP.Application;
using SWP.Infrastructure;
using SWP.Infrastructure.Options;
using SWP.Infrastructure.Persistence;

namespace SWP.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.");

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                });

            builder.Services.AddAuthorization();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Frontend", policy =>
                {
                    policy.WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173",
                            "http://localhost:5174",
                            "http://127.0.0.1:5174")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });

            builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 6 * 1024 * 1024;
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "SWP Horse Training API", Version = "v1" });
                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer",
                            },
                        },
                        Array.Empty<string>()
                    },
                });
            });

            var app = builder.Build();

            var cloudinaryConfig = app.Configuration.GetSection(SWP.Infrastructure.Options.CloudinarySettings.SectionName)
                .Get<SWP.Infrastructure.Options.CloudinarySettings>();
            var cloudinaryReady = cloudinaryConfig is not null
                && !string.IsNullOrWhiteSpace(cloudinaryConfig.CloudName)
                && !string.IsNullOrWhiteSpace(cloudinaryConfig.ApiKey)
                && !string.IsNullOrWhiteSpace(cloudinaryConfig.ApiSecret);
            app.Logger.LogInformation(
                cloudinaryReady
                    ? "Ảnh ngựa: upload Cloudinary (folder {Folder})."
                    : "Ảnh ngựa: lưu local wwwroot/media (chưa đủ Cloudinary:ApiSecret — xem appsettings.Local.json).",
                cloudinaryConfig?.Folder ?? "swp-horses");

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.ExecuteSqlRaw("""
                    IF COL_LENGTH('Horse', 'ImageURL') IS NULL
                        ALTER TABLE [Horse] ADD [ImageURL] VARCHAR(500) NULL;

                    IF OBJECT_ID(N'HorseHealthStatus', N'U') IS NULL
                    BEGIN
                        CREATE TABLE [HorseHealthStatus] (
                            [HorseID] INT NOT NULL PRIMARY KEY,
                            [TrangThai] NVARCHAR(30) NOT NULL,
                            [NgayCapNhat] DATETIME NOT NULL,
                            CONSTRAINT [FK_HorseHealthStatus_Horse] FOREIGN KEY ([HorseID]) REFERENCES [Horse]([HorseID]) ON DELETE CASCADE
                        );
                    END;

                    INSERT INTO [HorseHealthStatus] ([HorseID], [TrangThai], [NgayCapNhat])
                    SELECT h.[HorseID], N'Đủ điều kiện', GETUTCDATE()
                    FROM [Horse] h
                    WHERE NOT EXISTS (
                        SELECT 1 FROM [HorseHealthStatus] s WHERE s.[HorseID] = h.[HorseID]
                    );
                    """);
            }

            var webRoot = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
            Directory.CreateDirectory(Path.Combine(webRoot, "media", "swp-horses"));

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseCors("Frontend");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
