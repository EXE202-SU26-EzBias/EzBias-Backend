namespace EzBias.Domain.Entities;

public class SubscriptionPlan
{
    public string Id { get; set; } = string.Empty; // free|boost|premium
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; } = 0m;
    public int DurationDays { get; set; } = 0;
    public int DurationHours { get; set; } = 0;
    public string Features { get; set; } = "{}"; // jsonb
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
