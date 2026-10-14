using SWP.Application.DTOs.Training;

namespace SWP.Application.Services.Interface;

public interface IHorseFitnessService
{
    /// <summary> Đánh giá thể trạng hiện tại; null nếu không tìm thấy ngựa. </summary>
    Task<HorseFitnessAssessmentDto?> GetAssessmentAsync(int horseId, CancellationToken cancellationToken = default);
}
