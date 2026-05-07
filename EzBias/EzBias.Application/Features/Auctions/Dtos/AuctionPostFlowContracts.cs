using EzBias.Domain.Enums;

namespace EzBias.Application.Features.Auctions.Dtos;

public record BuyerAuctionPostItem(long AuctionId, long ProductId, decimal FinalPrice, AuctionStatus Status, DateTimeOffset? WinnerPaymentDeadline, DateTimeOffset? EndedAt);
public record SellerAuctionEndedItem(long AuctionId, long ProductId, long? WinnerId, decimal? FinalPrice, AuctionStatus Status, DateTimeOffset? EndedAt, DateTimeOffset? WinnerPaymentDeadline);
