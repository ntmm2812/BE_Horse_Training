using SWP.Application.Common;
using SWP.Domain.Common;
using SWP.Domain.Models.Entity;
using SWP.Tests.Support;

namespace SWP.Tests.Services;

public class RaceServiceTests : IDisposable
{
    private readonly TrainingTestContext _t = new();

    public void Dispose() => _t.Dispose();

    [Fact]
    public async Task Register_FutureRace_ThenDuplicateLockAndInjuryAreRejected()
    {
        var horseId = _t.AddHorse("Thunder");
        var raceId = AddRace(TestData.Today.AddDays(6), "Cúp Thành Phố", maxParticipants: 1);

        var saved = await _t.RaceService().RegisterAsync(raceId, horseId, _t.Ids.TrainerId);

        Assert.True(saved.Success, saved.ErrorMessage);
        Assert.Equal(horseId, saved.Data!.HorseID);
        Assert.Equal("2026-10-12", saved.Data.NgayDangKy);
        Assert.Null(saved.Data.ThuHang);

        var duplicate = await _t.RaceService().RegisterAsync(raceId, horseId, _t.Ids.TrainerId);
        Assert.Equal(ServiceErrorCodes.Conflict, duplicate.ErrorCode);

        var otherId = _t.AddHorse("Storm");
        var full = await _t.RaceService().RegisterAsync(raceId, otherId, _t.Ids.TrainerId);
        Assert.Equal(ServiceErrorCodes.Conflict, full.ErrorCode);

        var lockedId = _t.AddHorse("Locked", lockReason: "viêm gân");
        var openRace = AddRace(TestData.Today.AddDays(10), "Giải Mở");
        var locked = await _t.RaceService().RegisterAsync(openRace, lockedId, _t.Ids.TrainerId);
        Assert.Equal(ServiceErrorCodes.Conflict, locked.ErrorCode);

        var injuredId = _t.AddHorse("Injured", healthStatus: HorseHealthStatuses.ChanThuong);
        var injured = await _t.RaceService().RegisterAsync(openRace, injuredId, _t.Ids.TrainerId);
        Assert.Equal(ServiceErrorCodes.Conflict, injured.ErrorCode);
    }

    [Fact]
    public async Task Cancel_PastRace_IsRejected_FutureRace_RemovesRegistration()
    {
        var horseId = _t.AddHorse("Thunder");
        var pastId = AddRace(TestData.Today.AddDays(-3), "Giải đã qua");
        AddRegistration(pastId, horseId);
        var futureId = AddRace(TestData.Today.AddDays(4), "Giải sắp tới");
        await _t.RaceService().RegisterAsync(futureId, horseId, _t.Ids.TrainerId);

        var past = await _t.RaceService().CancelAsync(pastId, horseId);
        Assert.Equal(ServiceErrorCodes.Conflict, past.ErrorCode);

        var future = await _t.RaceService().CancelAsync(futureId, horseId);
        Assert.True(future.Success, future.ErrorMessage);
        var left = await _t.RaceService().GetRegistrationsAsync(null);
        Assert.DoesNotContain(left, r => r.RaceID == futureId && r.HorseID == horseId);
        Assert.Contains(left, r => r.RaceID == pastId && r.HorseID == horseId);
    }

    [Fact]
    public async Task UpdateResult_AfterRace_ClearsPrizeWhenRankChanges_AndBlocksDuplicateRank()
    {
        var firstId = _t.AddHorse("Thunder");
        var secondId = _t.AddHorse("Lightning");
        var raceId = AddRace(TestData.Today.AddDays(-9), "Mùa Thu");
        AddRegistration(raceId, firstId);
        AddRegistration(raceId, secondId);

        var tooSoon = AddRace(TestData.Today, "Hôm nay");
        AddRegistration(tooSoon, firstId);
        var early = await _t.RaceService().UpdateResultAsync(tooSoon, firstId, 1);
        Assert.Equal(ServiceErrorCodes.Conflict, early.ErrorCode);

        var first = await _t.RaceService().UpdateResultAsync(raceId, firstId, 1);
        Assert.True(first.Success, first.ErrorMessage);
        Assert.Equal(1, first.Data!.Registration.ThuHang);
        Assert.False(first.Data.PrizeCleared);

        var prize = await _t.RaceService().SetPrizeAsync(raceId, firstId, 20_000_000);
        Assert.True(prize.Success, prize.ErrorMessage);
        Assert.Equal(20_000_000, prize.Data!.TienThuong);

        var kept = await _t.RaceService().UpdateResultAsync(raceId, firstId, 1);
        Assert.False(kept.Data!.PrizeCleared);
        Assert.Equal(20_000_000, kept.Data.Registration.TienThuong);

        var second = await _t.RaceService().UpdateResultAsync(raceId, secondId, 1);
        Assert.Equal(ServiceErrorCodes.Conflict, second.ErrorCode);

        var changed = await _t.RaceService().UpdateResultAsync(raceId, firstId, 2);
        Assert.True(changed.Data!.PrizeCleared);
        Assert.Null(changed.Data.Registration.TienThuong);
        Assert.Equal(2, changed.Data.Registration.ThuHang);

        var alerts = await _t.RaceService().GetPrizeAlertsAsync();
        Assert.Single(alerts);
        Assert.Equal(firstId, alerts[0].HorseID);

        var again = await _t.RaceService().SetPrizeAsync(raceId, firstId, 8_000_000);
        Assert.True(again.Success, again.ErrorMessage);
        Assert.Empty(await _t.RaceService().GetPrizeAlertsAsync());

        var unranked = await _t.RaceService().UpdateResultAsync(raceId, secondId, 0);
        Assert.True(unranked.Success, unranked.ErrorMessage);
        var noPrize = await _t.RaceService().SetPrizeAsync(raceId, secondId, 1000);
        Assert.Equal(ServiceErrorCodes.Conflict, noPrize.ErrorCode);
    }

    [Fact]
    public async Task GetRegistrations_OwnerSeesOnlyOwnHorses()
    {
        var ownId = _t.AddHorse("Thunder");
        var otherId = _t.Db(db => TestData.AddHorse(db, _t.Ids.ManagerId, "Other", 5, "Đủ điều kiện", null));
        var raceId = AddRace(TestData.Today.AddDays(3), "Cúp CLB");
        AddRegistration(raceId, ownId);
        AddRegistration(raceId, otherId);

        var own = await _t.RaceService().GetRegistrationsAsync(_t.Ids.OwnerId);
        Assert.Single(own);
        Assert.Equal(ownId, own[0].HorseID);

        var all = await _t.RaceService().GetRegistrationsAsync(null);
        Assert.Equal(2, all.Count);
    }

    private int AddRace(DateOnly date, string name, int? maxParticipants = null) =>
        _t.Db(db =>
        {
            var race = new Race
            {
                RaceName = name,
                RaceDate = date.ToDateTime(TimeOnly.MinValue),
                Location = "Trường đua",
                Status = "Open",
                MaxParticipants = maxParticipants,
                CreatedAt = TestData.Now.UtcDateTime,
            };
            db.Races.Add(race);
            db.SaveChanges();
            return race.RaceId;
        });

    private void AddRegistration(int raceId, int horseId) =>
        _t.Db(db =>
        {
            db.RaceRegistrations.Add(new RaceRegistration
            {
                RaceId = raceId,
                HorseId = horseId,
                RegistrationDate = TestData.Today.ToDateTime(TimeOnly.MinValue),
                RegistrationStatus = RaceCatalog.Registered,
                CreatedAt = TestData.Now.UtcDateTime,
            });
            db.SaveChanges();
        });
}
