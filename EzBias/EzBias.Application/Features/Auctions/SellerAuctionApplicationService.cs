using EzBias.Application.Features.Auctions.Dtos;
using EzBias.Application.Features.Deposits;
using EzBias.Domain.Entities;
using EzBias.Domain.Enums;
using EzBias.Domain.Interfaces;

namespace EzBias.Application.Features.Auctions;

public class SellerAuctionApplicationService : ISellerAuctionApplicationService
{
    private readonly IAuctionRepository _auctions;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _uow;
    private readonly IDepositPolicy _depositPolicy;
    private readonly IDepositApplicationService _deposits;

    public SellerAuctionApplicationService(IAuctionRepository auctions, IProductRepository products, IUnitOfWork uow, IDepositPolicy depositPolicy, IDepositApplicationService deposits)
    {
        _auctions = auctions;
        _products = products;
        _uow = uow;
        _depositPolicy = depositPolicy;
        _deposits = deposits;
    }

    public async Task<(bool Success, string? Error, AuctionActionResponse? Data)> CreateAsync(long sellerId, CreateAuctionRequest request, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null) return (false, "Product not found.", null);
        if (product.SellerId != sellerId) return (false, "Forbidden.", null);
        if (product.IsAuction) return (false, "Product already configured for auction.", null);
        if (request.FloorPrice <= 0) return (false, "Floor price must be greater than zero.", null);
        if (request.ReservePrice.HasValue && request.ReservePrice.Value < request.FloorPrice) return (false, "Reserve price must be >= floor price.", null);
        if (request.EndsAt <= DateTimeOffset.UtcNow.AddMinutes(1)) return (false, "EndsAt must be in the future.", null);

        var hasLive = await _auctions.ExistsLiveByProductIdAsync(product.Id, ct);
        if (hasLive) return (false, "A live auction already exists for this product.", null);

        // Required bid deposit is derived from the floor price (e.g. 10%), not seller-supplied.
        var resolvedDeposit = _depositPolicy.ComputeRequiredDeposit(request.FloorPrice);

        product.IsAuction = true;
        product.UpdatedAt = DateTimeOffset.UtcNow;

        var auction = new Auction
        {
            ProductId = product.Id,
            SellerId = sellerId,
            FloorPrice = request.FloorPrice,
            ReservePrice = request.ReservePrice,
            CurrentBid = request.FloorPrice,
            IsUrgent = request.IsUrgent,
            HasProofImage = request.HasProofImage,
            ExtensionSeconds = request.ExtensionSeconds,
            TriggerBeforeEnd = request.TriggerBeforeEnd,
            Status = AuctionStatus.Draft,
            EndsAt = request.EndsAt,
            RequiredDepositAmount = resolvedDeposit,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _auctions.Add(auction);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<(bool Success, string? Error, AuctionActionResponse? Data)> PublishAsync(long sellerId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null) return (false, "Auction not found.", null);
        if (auction.SellerId != sellerId) return (false, "Forbidden.", null);
        if (auction.Status == AuctionStatus.Canceled || auction.Status == AuctionStatus.Sold || auction.Status == AuctionStatus.EndedNoWinner || auction.Status == AuctionStatus.EndedPendingPayment)
            return (false, "Auction cannot be published in current status.", null);

        auction.Status = AuctionStatus.Live;
        auction.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync(ct);

        return (true, null, new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<(bool Success, string? Error, AuctionActionResponse? Data)> CancelAsync(long sellerId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null) return (false, "Auction not found.", null);
        if (auction.SellerId != sellerId) return (false, "Forbidden.", null);
        if (auction.Status == AuctionStatus.Live || auction.Status == AuctionStatus.Extended)
        {
            var hasAnyBid = await _auctions.HasAnyBidAsync(auction.Id, ct);
            if (hasAnyBid)
                return (false, "Auction with bids cannot be canceled.", null);
        }

        if (auction.Status is AuctionStatus.Sold or AuctionStatus.EndedPendingPayment or AuctionStatus.EndedNoWinner or AuctionStatus.WinnerFailed or AuctionStatus.Canceled)
            return (false, "Auction cannot be canceled in current status.", null);

        auction.Status = AuctionStatus.Canceled;
        auction.UpdatedAt = DateTimeOffset.UtcNow;
        await _deposits.ReleaseDepositsOnCancelAsync(auction.Id, ct);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<(bool Success, string? Error, AuctionActionResponse? Data)> RelistAsync(long sellerId, long auctionId, RelistAuctionRequest request, CancellationToken ct)
    {
        var source = await _auctions.GetByIdAsync(auctionId, ct);
        if (source is null) return (false, "Auction not found.", null);
        if (source.SellerId != sellerId) return (false, "Forbidden.", null);

        if (source.Status is not (AuctionStatus.Canceled or AuctionStatus.EndedNoWinner or AuctionStatus.WinnerFailed))
            return (false, "Auction cannot be relisted in current status.", null);

        if (request.FloorPrice <= 0) return (false, "Floor price must be greater than zero.", null);
        if (request.ReservePrice.HasValue && request.ReservePrice.Value < request.FloorPrice) return (false, "Reserve price must be >= floor price.", null);
        if (request.EndsAt <= DateTimeOffset.UtcNow.AddMinutes(1)) return (false, "EndsAt must be in the future.", null);

        var hasDraftOrLive = await _auctions.ExistsDraftOrLiveByProductIdAsync(source.ProductId, ct);
        if (hasDraftOrLive) return (false, "An active/draft auction already exists for this product.", null);

        // Required bid deposit is derived from the floor price (e.g. 10%), not seller-supplied.
        var resolvedDeposit = _depositPolicy.ComputeRequiredDeposit(request.FloorPrice);

        var newAuction = new Auction
        {
            ProductId = source.ProductId,
            SellerId = source.SellerId,
            FloorPrice = request.FloorPrice,
            ReservePrice = request.ReservePrice,
            CurrentBid = request.FloorPrice,
            IsUrgent = request.IsUrgent,
            HasProofImage = request.HasProofImage,
            ExtensionSeconds = request.ExtensionSeconds,
            TriggerBeforeEnd = request.TriggerBeforeEnd,
            Status = AuctionStatus.Draft,
            EndsAt = request.EndsAt,
            RequiredDepositAmount = resolvedDeposit,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _auctions.Add(newAuction);
        await _uow.SaveChangesAsync(ct);

        return (true, null, new AuctionActionResponse(newAuction.Id, newAuction.Status.ToString()));
    }

    public async Task<IReadOnlyList<SellerAuctionItem>> GetMyAuctionsAsync(long sellerId, AuctionStatus? status, CancellationToken ct)
    {
        var items = await _auctions.GetBySellerAsync(sellerId, status, ct);
        return items.Select(x => new SellerAuctionItem(
            x.Id,
            x.ProductId,
            x.FloorPrice,
            x.CurrentBid,
            x.Status,
            x.EndsAt,
            x.CreatedAt,
            new AuctionProductSummary(x.Product.Id, x.Product.Name, x.Product.Artist, x.Product.Type, x.Product.Price, x.Product.Stock, x.Product.PrimaryImageUrl, x.Product.Status, x.Product.FandomId)
        )).ToList();
    }
}
