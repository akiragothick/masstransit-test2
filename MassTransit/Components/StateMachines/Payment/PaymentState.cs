using MassTransit;

namespace Components.StateMachines.Payment;

public class PaymentState: SagaStateMachineInstance
{
    public string? Reference { get; set; }
    public string? DocumentNumber { get; set; }
    public decimal Amount { get; set; }
    public int? DocEntryOv { get; set; }
    public int? DocEntryInvoice { get; set; }
    public int? DocEntryPayment { get; set; }
    
    
    public Guid? RegistrationId { get; set; }
    public DateTime? DocEntryOvExpirationDate { get; set; }
    
    
    public string? CurrentState { get; set; }
    public string? Reason { get; set; }
    public int? RetryAttempt { get; set; }
    public Guid? ScheduleRetryToken { get; set; }
    
    
    public Guid CorrelationId { get; set; }
}