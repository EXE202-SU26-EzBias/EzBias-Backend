using EzBias.Domain.Entities;
using EzBias.Domain.Enums;

namespace EzBias.Domain.Interfaces;

public interface IOtpVerificationRepository
{
    Task<IReadOnlyList<OtpVerification>> GetActiveAsync(long userId, OtpPurpose purpose, DateTimeOffset now, CancellationToken ct);
    Task RevokeActiveAsync(long userId, OtpPurpose purpose, DateTimeOffset now, CancellationToken ct);
    void Add(OtpVerification otp);
}
