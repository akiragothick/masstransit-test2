namespace Components.Activities.CreateOrder;

public record CreateOrderActivityArguments
{
    public string? Reference { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal Amount { get; set; }
}