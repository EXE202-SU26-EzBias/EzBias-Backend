using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class OtpVerification
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public OtpChannel Channel { get; set; }
    public OtpPurpose Purpose { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public bool IsUsed { get; set; } = false;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
}
