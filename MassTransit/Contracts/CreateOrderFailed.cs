using MassTransit;

namespace Contracts;

public record CreateOrderFailed
{
    public Guid PaymentId { get; init; }
    public ExceptionInfo? ExceptionInfo { get; init; }
}