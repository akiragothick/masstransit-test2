using MassTransit.Courier.Contracts;

namespace Contracts;

public record PaymentCompleted: RoutingSlipCompleted
{
    public Guid PaymentId { get; init; }
    public Guid TrackingNumber { get; }
    public DateTime Timestamp { get; }
    public TimeSpan Duration { get; }
    public IDictionary<string, object> Variables { get; }
}