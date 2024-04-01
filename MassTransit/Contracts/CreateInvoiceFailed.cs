using MassTransit;

namespace Contracts;

public record CreateInvoiceFailed
{
    public Guid PaymentId { get; init; }
    public ExceptionInfo? ExceptionInfo { get; init; }
}