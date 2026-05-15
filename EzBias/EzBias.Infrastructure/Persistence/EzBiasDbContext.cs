using EzBias.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EzBias.Infrastructure.Persistence;

public class EzBiasDbContext : DbContext
{
    public EzBiasDbContext(DbContextOptions<EzBiasDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Fandom> Fandoms => Set<Fandom>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<PhotocardDetail> PhotocardDetails => Set<PhotocardDetail>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Wishlist> Wishlists => Set<Wishlist>();
    public DbSet<SellerFollow> SellerFollows => Set<SellerFollow>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentOrder> PaymentOrders => Set<PaymentOrder>();
    public DbSet<EscrowTransaction> EscrowTransactions => Set<EscrowTransaction>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Rating> Ratings => Set<Rating>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ContactMessage> ContactMessages => Set<ContactMessage>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<ProductBoost> ProductBoosts => Set<ProductBoost>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeItem> DisputeItems => Set<DisputeItem>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<OtpVerification> OtpVerifications => Set<OtpVerification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EzBiasDbContext).Assembly);
    }
}
