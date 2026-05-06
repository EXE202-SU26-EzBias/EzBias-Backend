using EzBias.Application.Features.Checkout.Dtos;

namespace EzBias.Application.Features.Checkout;

public interface ICheckoutApplicationService
{
    Task<(bool Success, string? Error, CheckoutPreviewResponse? Data)> PreviewAsync(long userId, CheckoutPreviewRequest request, CancellationToken ct);
    Task<(bool Success, string? Error, CheckoutSubmitResponse? Data)> SubmitAsync(long userId, CheckoutSubmitRequest request, CancellationToken ct);
}
