using EzBias.Domain.Enums;

namespace EzBias.Domain.Entities;

public class User
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;

    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public string AvatarBg { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;

    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public DateTimeOffset? PhoneVerifiedAt { get; set; }

    public decimal AvgSellerRating { get; set; } = 0m;
    public int TotalRatings { get; set; } = 0;

    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public ICollection<SellerFollow> FollowingSellers { get; set; } = new List<SellerFollow>();
    public ICollection<SellerFollow> Followers { get; set; } = new List<SellerFollow>();

    public ICollection<Auction> AuctionsAsSeller { get; set; } = new List<Auction>();
    public ICollection<Auction> AuctionsAsWinner { get; set; } = new List<Auction>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();

    public ICollection<Order> OrdersAsBuyer { get; set; } = new List<Order>();
    public ICollection<Order> OrdersAsSeller { get; set; } = new List<Order>();
    public ICollection<CommissionTransaction> CommissionTransactionsAsSeller { get; set; } = new List<CommissionTransaction>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Payout> Payouts { get; set; } = new List<Payout>();
    public ICollection<EscrowTransaction> EscrowTransactionsAsSeller { get; set; } = new List<EscrowTransaction>();

    public ICollection<Rating> RatingsAsBuyer { get; set; } = new List<Rating>();
    public ICollection<Rating> RatingsAsSeller { get; set; } = new List<Rating>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
    public ICollection<ProductBoost> ProductBoosts { get; set; } = new List<ProductBoost>();
    public ICollection<Dispute> DisputesOpened { get; set; } = new List<Dispute>();
    public ICollection<OtpVerification> OtpVerifications { get; set; } = new List<OtpVerification>();
}
