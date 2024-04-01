using MassTransit;

namespace Contracts;

public record CreateTransactionFailed
{
    public Guid PaymentId { get; init; }
    public ExceptionInfo? ExceptionInfo { get; init; }
}