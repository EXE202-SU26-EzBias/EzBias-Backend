namespace EzBias.Domain.Entities;

public class Fandom
{
    public string Id { get; set; } = string.Empty; // slug
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
