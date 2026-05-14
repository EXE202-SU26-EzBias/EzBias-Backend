using EzBias.Application.Features.Disputes.Dtos;

namespace EzBias.Application.Features.Disputes;

public interface IDisputeApplicationService
{
    Task<(bool Success, string? Error, DisputeResponse? Data)> CreateAsync(long buyerId, CreateDisputeRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, DisputeResponse? Data)> ApproveAsync(long adminId, long disputeId, ResolveDisputeRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, DisputeResponse? Data)> RejectAsync(long adminId, long disputeId, RejectDisputeRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, DisputeResponse? Data)> CompleteRefundPaymentAsync(long adminId, long disputeId, CompleteRefundPaymentRequest request, CancellationToken ct);
    Task<IReadOnlyList<DisputeListItemResponse>> GetListAsync(CancellationToken ct);
}
