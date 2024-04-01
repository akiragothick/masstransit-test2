namespace Components.Activities;

public record EventPaymentLog
{
    public Guid? TransactionId { get; init; }
    public int? DocEntryOv { get; init; }
    public int? DocEntryInvoice { get; set; }
    public string? Reference { get; init; }
    public string? DocumentNumber { get; init; }
}