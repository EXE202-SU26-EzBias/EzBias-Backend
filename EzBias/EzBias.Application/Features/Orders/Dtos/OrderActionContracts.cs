namespace EzBias.Application.Features.Orders.Dtos;

public record MarkShippedRequest(string Carrier, string? TrackingNumber);
public record FulfillmentActionResponse(long OrderId, string Status);
