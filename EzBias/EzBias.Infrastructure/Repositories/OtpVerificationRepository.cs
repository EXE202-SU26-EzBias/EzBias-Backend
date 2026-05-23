using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;
using EzBias.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Repositories;

public class OtpVerificationRepository : IOtpVerificationRepository
{
    private readonly EzBiasDbContext _db;

    public OtpVerificationRepository(EzBiasDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OtpVerification>> GetActiveAsync(long userId, OtpPurpose purpose, DateTimeOffset now, CancellationToken ct)
        => await _db.OtpVerifications
            .Where(x => x.UserId == userId
                        && x.Purpose == purpose
                        && !x.IsUsed
                        && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task RevokeActiveAsync(long userId, OtpPurpose purpose, DateTimeOffset now, CancellationToken ct)
    {
        var active = await _db.OtpVerifications
            .Where(x => x.UserId == userId
                        && x.Purpose == purpose
                        && !x.IsUsed
                        && x.ExpiresAt > now)
            .ToListAsync(ct);

        foreach (var item in active)
            item.IsUsed = true;
    }

    public void Add(OtpVerification otp) => _db.OtpVerifications.Add(otp);
}
