namespace EzBias.Application.Features.Orders.Dtos;

public record MarkShippedRequest(string Carrier);
public record FulfillmentActionResponse(long OrderId, string Status);
