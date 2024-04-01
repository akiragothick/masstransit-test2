using MassTransit;

namespace Contracts;

[ExcludeFromTopology]
public record Payment
{
    public Guid PaymentId { get; init; }
    
    public string? Reference { get; set; }
    public string? DocumentNumber { get; init; }
    public decimal Amount { get; set; }
    public int? DocEntryOv { get; init; }
    public int? DocEntryInvoice { get; init; }
    public int? DocEntryPayment { get; init; }
}