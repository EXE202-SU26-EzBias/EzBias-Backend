using EzBias.Application.Common.Results;
using EzBias.Application.Features.Disputes.Dtos;

namespace EzBias.Application.Features.Disputes;

public interface IDisputeApplicationService
{
    Task<Result<DisputeResponse>> CreateAsync(long buyerId, CreateDisputeRequest request, CancellationToken ct);
    Task<Result<DisputeResponse>> ApproveAsync(long adminId, long disputeId, ResolveDisputeRequest request, CancellationToken ct);
    Task<Result<DisputeResponse>> RejectAsync(long adminId, long disputeId, RejectDisputeRequest request, CancellationToken ct);
    Task<Result<DisputeResponse>> CompleteRefundPaymentAsync(long adminId, long disputeId, CompleteRefundPaymentRequest request, CancellationToken ct);
    Task<IReadOnlyList<DisputeListItemResponse>> GetListAsync(CancellationToken ct);
}
