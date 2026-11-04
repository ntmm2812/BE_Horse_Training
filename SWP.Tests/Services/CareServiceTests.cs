using SWP.Application.Common;
using SWP.Application.DTOs.Care;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class CareServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task CompleteTask_OwnPendingTask_MarksCompleted()
    {
        var horseId = _t.AddHorse("Thunder");
        var taskId = AddTask(horseId, _t.Ids.GroomId, "Dọn chuồng", CareTaskStatuses.Pending);

        var done = await _t.CareService().CompleteTaskAsync(taskId, _t.Ids.GroomId);

        Assert.True(done.Success, done.ErrorMessage);
        Assert.True(done.Data!.TrangThai);
        Assert.Equal("Dọn chuồng", done.Data.LoaiViec);
        Assert.Equal("2026-10-12", done.Data.Ngay);
        Assert.Equal("08:00", done.Data.Gio);
    }

    [Fact]
    public async Task GetTasks_NextDay_CreatesRoutineOnceForAssignedHorses()
    {
        var horseId = _t.AddHorse("Thunder");
        AddTask(horseId, _t.Ids.GroomId, "Dọn chuồng", CareTaskStatuses.Pending);

        var today = await _t.CareService().GetTasksAsync(_t.Ids.GroomId);
        Assert.Single(today);

        _t.Clock.Advance(TimeSpan.FromDays(1));
        var tomorrow = await _t.CareService().GetTasksAsync(_t.Ids.GroomId);
        var created = tomorrow.Where(t => t.Ngay == "2026-10-13" && t.HorseID == horseId).ToList();
        Assert.Equal(6, created.Count);
        Assert.Contains(created, t => t.LoaiViec == "Cho ăn buổi sáng" && t.Gio == "06:00" && !t.TrangThai);

        var again = await _t.CareService().GetTasksAsync(_t.Ids.GroomId);
        Assert.Equal(created.Count, again.Count(t => t.Ngay == "2026-10-13" && t.HorseID == horseId));
    }

    [Fact]
    public async Task CompleteTask_OtherGroomOrAlreadyDone_IsRejected()
    {
        var horseId = _t.AddHorse("Lightning");
        var taskId = AddTask(horseId, _t.Ids.GroomId, "Tắm và chải lông", CareTaskStatuses.Pending);

        var other = await _t.CareService().CompleteTaskAsync(taskId, _t.Ids.TrainerId);
        Assert.Equal(ServiceErrorCodes.Conflict, other.ErrorCode);

        Assert.True((await _t.CareService().CompleteTaskAsync(taskId, _t.Ids.GroomId)).Success);
        var again = await _t.CareService().CompleteTaskAsync(taskId, _t.Ids.GroomId);
        Assert.Equal(ServiceErrorCodes.Conflict, again.ErrorCode);
    }

    [Fact]
    public async Task Feeding_ActivePlan_GroupsHayGrainAndVitaminByMeal()
    {
        var horseId = _t.AddHorse("Blaze");
        _t.Db(db =>
        {
            var plan = new FeedingPlan
            {
                HorseId = horseId,
                UserId = _t.Ids.GroomId,
                PlanName = "Khẩu phần",
                StartDate = TestData.Today.AddDays(-2),
                Status = "Active",
                CreatedAt = TestData.Now.UtcDateTime,
                FeedingPlanDetails =
                {
                    Detail("Sáng", "Cỏ khô", 3),
                    Detail("Sáng", "Yến mạch", 2.5m),
                    Detail("Sáng", "Vitamin tổng hợp", 20),
                    Detail("Chiều", "Cỏ", 4),
                },
            };
            db.FeedingPlans.Add(plan);
            db.SaveChanges();
        });

        var meals = await _t.CareService().GetFeedingAsync();
        var morning = Assert.Single(meals, m => m.HorseID == horseId && m.Bua == "Sáng");
        Assert.Equal(3, morning.Co);
        Assert.Equal(2.5m, morning.NguCoc);
        Assert.Equal(20, morning.Vitamin);
        Assert.Contains(meals, m => m.HorseID == horseId && m.Bua == "Chiều" && m.Co == 4);
    }

    [Fact]
    public async Task Stables_OpenAssignment_UsesStallNameAndTodaysTasks()
    {
        var horseId = _t.AddHorse("Storm");
        AddTask(horseId, _t.Ids.GroomId, "Cho ăn buổi sáng", CareTaskStatuses.Pending);
        _t.Db(db =>
        {
            var stable = new Stable
            {
                StableName = "A3",
                CurrentOccupancy = 1,
                Status = "Active",
                CreatedAt = TestData.Now.UtcDateTime,
            };
            db.Stables.Add(stable);
            db.SaveChanges();
            db.StableAssignments.Add(new StableAssignment
            {
                HorseId = horseId,
                StableId = stable.StableId,
                StartDate = TestData.Now.UtcDateTime.AddDays(-3),
                Status = "Active",
            });
            db.SaveChanges();
        });

        var stalls = await _t.CareService().GetStablesAsync();
        var stall = Assert.Single(stalls, s => s.HorseID == horseId);
        Assert.Equal("A3", stall.SoChuong);
        Assert.Contains("Cho ăn buổi sáng", stall.LichSinhHoat);
    }

    [Fact]
    public async Task Incident_FutureDate_IsRejected_AndValidReportIsStored()
    {
        var horseId = _t.AddHorse("Shadow");
        var future = await _t.CareService().CreateIncidentAsync(
            new CreateIncidentReportRequestDto
            {
                HorseID = horseId,
                LoaiSuCo = "Bỏ ăn",
                Ngay = TestData.Today.AddDays(1),
            },
            _t.Ids.GroomId);
        Assert.Equal(ServiceErrorCodes.Validation, future.ErrorCode);

        var saved = await _t.CareService().CreateIncidentAsync(
            new CreateIncidentReportRequestDto
            {
                HorseID = horseId,
                LoaiSuCo = "Khập khiễng",
                MoTa = "Chân trước bên phải",
                Ngay = TestData.Today,
            },
            _t.Ids.GroomId);

        Assert.True(saved.Success, saved.ErrorMessage);
        var mine = await _t.CareService().GetIncidentsAsync(_t.Ids.GroomId);
        Assert.Contains(mine, r => r.ReportID == saved.Data!.ReportID && r.LoaiSuCo == "Khập khiễng");
        var others = await _t.CareService().GetIncidentsAsync(_t.Ids.TrainerId);
        Assert.DoesNotContain(others, r => r.ReportID == saved.Data!.ReportID);
    }

    [Fact]
    public async Task AdjustStock_CannotGoNegative_AndDeleteRemovesSupply()
    {
        var created = await _t.CareService().CreateSupplyAsync(new SaveSupplyRequestDto
        {
            TenVatTu = "Cám ngựa đua",
            Loai = "Thức ăn",
            SoLuongTon = 10,
            DonGia = 450000,
            ManagedBy = _t.Ids.GroomId,
        });
        Assert.True(created.Success, created.ErrorMessage);

        var tooMuch = await _t.CareService().AdjustStockAsync(created.Data!.SupplyID, -11);
        Assert.Equal(ServiceErrorCodes.Conflict, tooMuch.ErrorCode);

        var issued = await _t.CareService().AdjustStockAsync(created.Data.SupplyID, -4);
        Assert.Equal(6, issued.Data!.SoLuongTon);

        Assert.True((await _t.CareService().DeleteSupplyAsync(created.Data.SupplyID)).Success);
        Assert.Null(await _t.CareService().GetSupplyAsync(created.Data.SupplyID));
    }

    private int AddTask(int horseId, int groomId, string name, string status) =>
        _t.Db(db =>
        {
            var task = new CareTask
            {
                HorseId = horseId,
                UserId = groomId,
                TaskName = name,
                Status = status,
                ScheduledAt = TestData.Now.UtcDateTime,
                CreatedAt = TestData.Now.UtcDateTime,
            };
            db.CareTasks.Add(task);
            db.SaveChanges();
            return task.CareTaskId;
        });

    private static FeedingPlanDetail Detail(string meal, string feed, decimal quantity) => new()
    {
        MealName = meal,
        FeedName = feed,
        Quantity = quantity,
        Unit = "kg",
        CreatedAt = TestData.Now.UtcDateTime,
    };
}
