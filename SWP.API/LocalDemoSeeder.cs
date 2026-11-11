using Microsoft.Data.SqlClient;
using SWP.Application.DTOs.Approvals;
using SWP.Application.DTOs.Training;
using SWP.Application.Services.Implement;
using SWP.Application.Services.Interface;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Infrastructure.Persistence;
using SWP.Infrastructure.Security.Interface;

namespace SWP.API;

// Opt-in local demo only; never initialize or seed an existing project database.
public static class LocalDemoSeeder
{
    public static void Initialize(IServiceProvider services, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("LocalDemo:Enabled")) return;
        var connection = new SqlConnectionStringBuilder(configuration.GetConnectionString("DefaultConnection"));
        if (!connection.InitialCatalog.StartsWith("HorseApproval_Local_", StringComparison.Ordinal))
            throw new InvalidOperationException("LocalDemo chỉ được dùng với database riêng HorseApproval_Local_*.");
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();
        if (db.Users.Any()) return;

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(now, "SE Asia Standard Time"));
        var hasher = services.GetRequiredService<IPasswordHasher>();
        var roles = new[] { "Head Trainer", "Horse Owner", "Club Manager", "Groom", "Veterinarian" }
            .ToDictionary(name => name, name => new Role { RoleName = name });
        db.Roles.AddRange(roles.Values);
        User Account(string email, string name, string role) => new()
        {
            Email = email, FullName = name, Role = roles[role], Status = UserStatuses.Active,
            PasswordHash = hasher.Hash(configuration["LocalDemo:Password"] ?? throw new InvalidOperationException("Thiếu mật khẩu demo.")), CreatedAt = now,
        };
        var trainer = Account("trainer@equitrack.vn", "Huấn luyện viên Demo", "Head Trainer");
        var owner = Account("owner@equitrack.vn", "Chủ ngựa Demo", "Horse Owner");
        var otherOwner = Account("owner2@equitrack.vn", "Chủ ngựa thứ hai", "Horse Owner");
        db.Users.AddRange(trainer, owner, otherOwner, Account("manager@equitrack.vn", "Quản lý Demo", "Club Manager"),
            Account("groom@equitrack.vn", "Nhân viên chăm sóc Demo", "Groom"), Account("vet@equitrack.vn", "Bác sĩ Demo", "Veterinarian"));
        var horses = new[]
        {
            new Horse { HorseName = "Thunder", User = owner },
            new Horse { HorseName = "Lightning", User = owner },
            new Horse { HorseName = "Storm", User = otherOwner },
        };
        foreach (var horse in horses)
        {
            horse.Breed = "Thoroughbred"; horse.DateOfBirth = today.AddYears(-5);
            horse.HorseStatus = "Active"; horse.Weight = 480; horse.CreatedAt = now;
            db.Horses.Add(horse);
        }
        var race = new Race
        {
            RaceName = "Giải đua giao hữu Demo", RaceCode = "LOCAL-DEMO-1", RaceDate = today.AddDays(12).ToDateTime(TimeOnly.MinValue),
            RegistrationDeadline = today.AddDays(5).ToDateTime(TimeOnly.MinValue), EntryFee = 5000000,
            Status = "Open", Location = "Trường đua Demo", CreatedAt = now, MaxParticipants = 10,
        };
        db.Races.Add(race);
        db.SaveChanges();
        foreach (var horse in horses) db.HorseHealthStatuses.Add(new HorseHealthStatus { HorseId = horse.HorseId, TrangThai = HorseHealthStatuses.DuDieuKien, NgayCapNhat = now });
        db.SaveChanges();

        var plan = services.GetRequiredService<ITrainingPlanService>().CreateAsync(new CreateTrainingPlanRequestDto
        {
            HorseId = horses[0].HorseId, PlanName = "Giáo án nền tảng Demo", Objective = "Duy trì thể lực và chuẩn bị thi đấu",
            Notes = "Ghi chú nội bộ chỉ HLV được xem.",
            Phases = [new TrainingPhaseRequestDto
            {
                PhaseName = "Nền tảng", StartDate = today, EndDate = today.AddDays(27), IntensityLevel = "Low",
                SessionsPerWeek = 3, TargetDistanceKm = 2, SessionDurationMinutes = 30, Surface = "Dirt",
            }],
        }, trainer.UserId).GetAwaiter().GetResult();
        if (!plan.Success) throw new InvalidOperationException(plan.ErrorMessage);
        var approvals = services.GetRequiredService<OwnerApprovalService>();
        var sent = approvals.SendPlan(plan.Data!.PlanId, default).GetAwaiter().GetResult();
        if (!sent.Success) throw new InvalidOperationException(sent.ErrorMessage);
        var proposal = approvals.CreateRace(trainer.UserId, new RaceProposalRequest(horses[0].HorseId, race.RaceId, "Đăng ký demo để chủ ngựa và quản lý duyệt."), default).GetAwaiter().GetResult();
        if (!proposal.Success) throw new InvalidOperationException(proposal.ErrorMessage);
        approvals.ActOnRace(db.RaceApprovals.Single().Id, trainer.UserId, RoleNames.HeadTrainer, "submit", null, default).GetAwaiter().GetResult();
    }
}
