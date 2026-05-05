using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class Product
{
    public long Id { get; set; }
    public long SellerId { get; set; }
    public string FandomId { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public ProductCondition Condition { get; set; } = ProductCondition.Good;
    public decimal Price { get; set; }
    public int Stock { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;

    public bool IsAuction { get; set; } = false;
    public ProductStatus Status { get; set; } = ProductStatus.Active;
    public int Version { get; set; } = 0;

    public long ViewCount { get; set; } = 0;
    public bool IsBoosted { get; set; } = false;
    public DateTimeOffset? BoostEndsAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public User Seller { get; set; } = null!;
    public Fandom Fandom { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public PhotocardDetail? PhotocardDetail { get; set; }
    public ICollection<Auction> Auctions { get; set; } = new List<Auction>();
    public ICollection<ProductBoost> ProductBoosts { get; set; } = new List<ProductBoost>();
}
