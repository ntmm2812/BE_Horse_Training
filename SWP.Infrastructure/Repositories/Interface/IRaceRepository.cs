using SWP.Domain.Models.Entity;

namespace SWP.Infrastructure.Repositories.Interface;

public interface IRaceRepository
{
    Task<IReadOnlyList<Race>> ListRacesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> ListRegistrationsAsync(int? ownerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RaceRegistration>> ListPrizeReviewsAsync(CancellationToken cancellationToken = default);

    Task<Race?> GetRaceTrackedAsync(int raceId, CancellationToken cancellationToken = default);

    Task<RaceRegistration?> GetRegistrationTrackedAsync(int raceId, int horseId, CancellationToken cancellationToken = default);

    Task<RaceRegistration?> GetRegistrationTrackedByIdAsync(int registrationId, CancellationToken cancellationToken = default);

    Task<int> CountActiveRegistrationsAsync(int raceId, CancellationToken cancellationToken = default);

    Task<string?> GetRankHolderNameAsync(int raceId, int rank, int exceptRegistrationId, CancellationToken cancellationToken = default);

    // Giải đã có ngựa được ghi thứ hạng (đăng ký còn hiệu lực).
    Task<bool> HasResultsAsync(int raceId, CancellationToken cancellationToken = default);

    // Đăng ký (RaceApproval) của giải còn đang chờ duyệt: Draft / PendingOwner / PendingManager, đang được theo dõi để sửa.
    Task<IReadOnlyList<RaceApproval>> ListPendingProposalsTrackedAsync(int raceId, CancellationToken cancellationToken = default);

    Task<string?> GetUserNameAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> HorseExistsAsync(int horseId, CancellationToken cancellationToken = default);

    Task<string?> GetHealthStatusAsync(int horseId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveLockAsync(int horseId, CancellationToken cancellationToken = default);

    void AddRace(Race race);

    void AddRegistration(RaceRegistration registration);

    void RemoveRegistration(RaceRegistration registration);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
