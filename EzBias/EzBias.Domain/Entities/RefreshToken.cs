namespace EzBias.Domain.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? DeviceInfo { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    public User User { get; set; } = null!;
}
