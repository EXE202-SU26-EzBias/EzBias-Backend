namespace EzBias.Application.Features.Notifications;

public sealed class NotificationDispatchOptions
{
    public int IntervalSeconds { get; set; } = 2;
    public int BatchSize { get; set; } = 50;
    public int LeaseSeconds { get; set; } = 30;
    public int MaxAttempts { get; set; } = 10;
    public int BaseBackoffSeconds { get; set; } = 5;
    public int MaxBackoffSeconds { get; set; } = 300;

    public void Normalize()
    {
        if (IntervalSeconds < 1) IntervalSeconds = 1;
        if (BatchSize < 1) BatchSize = 1;
        if (LeaseSeconds < 1) LeaseSeconds = 1;
        if (MaxAttempts < 1) MaxAttempts = 1;
        if (BaseBackoffSeconds < 1) BaseBackoffSeconds = 1;
        if (MaxBackoffSeconds < BaseBackoffSeconds) MaxBackoffSeconds = BaseBackoffSeconds;
    }
}
