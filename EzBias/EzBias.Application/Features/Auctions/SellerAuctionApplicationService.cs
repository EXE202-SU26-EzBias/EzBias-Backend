using EzBias.Application.Common.Results;
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
    private readonly IDepositApplicationService _deposits;

    public SellerAuctionApplicationService(IAuctionRepository auctions, IProductRepository products, IUnitOfWork uow, IDepositApplicationService deposits)
    {
        _auctions = auctions;
        _products = products;
        _uow = uow;
        _deposits = deposits;
    }

    private static (bool Success, string? Error, decimal Amount) ResolveSellerDeposit(decimal requiredDeposit, decimal floorPrice)
    {
        if (requiredDeposit < 0m)
            return (false, "Required deposit cannot be negative.", 0m);
        if (requiredDeposit > floorPrice)
            return (false, "Required deposit cannot exceed the floor price.", 0m);

        var rounded = Math.Round(requiredDeposit, 0, MidpointRounding.AwayFromZero);
        return (true, null, rounded);
    }

    public async Task<Result<AuctionActionResponse>> CreateAsync(long sellerId, CreateAuctionRequest request, CancellationToken ct)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct);
        if (product is null) return Result<AuctionActionResponse>.Fail("Product not found.", ApplicationErrorCode.ResourceNotFound);
        if (product.SellerId != sellerId) return Result<AuctionActionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (product.IsAuction) return Result<AuctionActionResponse>.Fail("Product already configured for auction.", ApplicationErrorCode.Validation);
        if (request.FloorPrice <= 0) return Result<AuctionActionResponse>.Fail("Floor price must be greater than zero.", ApplicationErrorCode.Validation);
        if (request.ReservePrice.HasValue && request.ReservePrice.Value < request.FloorPrice) return Result<AuctionActionResponse>.Fail("Reserve price must be >= floor price.", ApplicationErrorCode.Validation);
        if (request.EndsAt <= DateTimeOffset.UtcNow.AddMinutes(1)) return Result<AuctionActionResponse>.Fail("EndsAt must be in the future.", ApplicationErrorCode.Validation);

        var hasLive = await _auctions.ExistsLiveByProductIdAsync(product.Id, ct);
        if (hasLive) return Result<AuctionActionResponse>.Fail("A live auction already exists for this product.", ApplicationErrorCode.Validation);

        var (depositOk, depositError, resolvedDeposit) = ResolveSellerDeposit(request.RequiredDepositAmount, request.FloorPrice);
        if (!depositOk)
            return Result<AuctionActionResponse>.Fail(
                depositError ?? "Required deposit is invalid.", ApplicationErrorCode.Validation);

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

        return Result<AuctionActionResponse>.Ok(new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<Result<AuctionActionResponse>> PublishAsync(long sellerId, long auctionId, CancellationToken ct)
    {
        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null) return Result<AuctionActionResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
        if (auction.SellerId != sellerId) return Result<AuctionActionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (auction.Status == AuctionStatus.Canceled || auction.Status == AuctionStatus.Sold || auction.Status == AuctionStatus.EndedNoWinner || auction.Status == AuctionStatus.EndedPendingPayment)
            return Result<AuctionActionResponse>.Fail("Auction cannot be published in current status.", ApplicationErrorCode.Validation);

        if (auction.Publish(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<AuctionActionResponse>.Fail("Auction cannot be published in current status.", ApplicationErrorCode.Validation);
        await _uow.SaveChangesAsync(ct);

        return Result<AuctionActionResponse>.Ok(new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<Result<AuctionActionResponse>> CancelAsync(long sellerId, long auctionId, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var auction = await _auctions.GetByIdAsync(auctionId, ct);
        if (auction is null) return Result<AuctionActionResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
        if (auction.SellerId != sellerId) return Result<AuctionActionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);
        if (auction.Status == AuctionStatus.Live || auction.Status == AuctionStatus.Extended)
        {
            var hasAnyBid = await _auctions.HasAnyBidAsync(auction.Id, ct);
            if (hasAnyBid)
                return Result<AuctionActionResponse>.Fail("Auction with bids cannot be canceled.", ApplicationErrorCode.Validation);
        }

        if (auction.Status is AuctionStatus.Sold or AuctionStatus.EndedPendingPayment or AuctionStatus.EndedNoWinner or AuctionStatus.WinnerFailed or AuctionStatus.Canceled)
            return Result<AuctionActionResponse>.Fail("Auction cannot be canceled in current status.", ApplicationErrorCode.Validation);

        if (auction.Cancel(DateTimeOffset.UtcNow) == TransitionOutcome.Invalid)
            return Result<AuctionActionResponse>.Fail("Auction cannot be canceled in current status.", ApplicationErrorCode.Validation);

        var product = await _products.GetByIdAsync(auction.ProductId, ct);
        if (product is not null)
        {
            product.IsAuction = false;
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }
        
        await _deposits.ReleaseDepositsOnCancelAsync(auction.Id, ct);
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result<AuctionActionResponse>.Ok(new AuctionActionResponse(auction.Id, auction.Status.ToString()));
    }

    public async Task<Result<AuctionActionResponse>> RelistAsync(long sellerId, long auctionId, RelistAuctionRequest request, CancellationToken ct)
    {
        await using var transaction = await _uow.BeginTransactionAsync(ct);

        var source = await _auctions.GetByIdAsync(auctionId, ct);
        if (source is null) return Result<AuctionActionResponse>.Fail("Auction not found.", ApplicationErrorCode.ResourceNotFound);
        if (source.SellerId != sellerId) return Result<AuctionActionResponse>.Fail("Forbidden.", ApplicationErrorCode.Forbidden);

        if (source.Status is not (AuctionStatus.Canceled or AuctionStatus.EndedNoWinner or AuctionStatus.WinnerFailed))
            return Result<AuctionActionResponse>.Fail("Auction cannot be relisted in current status.", ApplicationErrorCode.Validation);

        var floorPrice = source.FloorPrice;
        if (request.ReservePrice.HasValue && request.ReservePrice.Value < floorPrice) return Result<AuctionActionResponse>.Fail("Reserve price must be >= floor price.", ApplicationErrorCode.Validation);
        if (request.EndsAt <= DateTimeOffset.UtcNow.AddMinutes(1)) return Result<AuctionActionResponse>.Fail("EndsAt must be in the future.", ApplicationErrorCode.Validation);

        var hasDraftOrLive = await _auctions.ExistsDraftOrLiveByProductIdAsync(source.ProductId, ct);
        if (hasDraftOrLive) return Result<AuctionActionResponse>.Fail("An active/draft auction already exists for this product.", ApplicationErrorCode.Validation);

        var (depositOk, depositError, resolvedDeposit) = ResolveSellerDeposit(request.RequiredDepositAmount, floorPrice);
        if (!depositOk)
            return Result<AuctionActionResponse>.Fail(
                depositError ?? "Required deposit is invalid.", ApplicationErrorCode.Validation);

        var product = await _products.GetByIdAsync(source.ProductId, ct);
        if (product is not null)
        {
            product.IsAuction = true;
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        var newAuction = new Auction
        {
            ProductId = source.ProductId,
            SellerId = source.SellerId,
            FloorPrice = floorPrice,
            ReservePrice = request.ReservePrice,
            CurrentBid = floorPrice,
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

        source.RelistedToAuctionId = newAuction.Id;
        source.UpdatedAt = DateTimeOffset.UtcNow;
        
        await _uow.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return Result<AuctionActionResponse>.Ok(new AuctionActionResponse(newAuction.Id, newAuction.Status.ToString()));
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
            new AuctionProductSummary(x.Product.Id, x.Product.Name, x.Product.Artist, x.Product.Type, x.Product.Price, x.Product.Stock, x.Product.PrimaryImageUrl, x.Product.Status, x.Product.FandomId),
            x.RelistedToAuctionId
        )).ToList();
    }
}
