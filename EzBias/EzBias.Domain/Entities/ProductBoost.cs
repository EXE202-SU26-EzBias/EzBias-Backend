using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class ProductBoost
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public BoostStatus Status { get; set; } = BoostStatus.Active;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
