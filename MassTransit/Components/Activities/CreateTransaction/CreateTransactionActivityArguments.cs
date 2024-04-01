namespace Components.Activities.CreateTransaction;

public record CreateTransactionActivityArguments
{
    public string? Reference { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal Amount { get; set; }
}