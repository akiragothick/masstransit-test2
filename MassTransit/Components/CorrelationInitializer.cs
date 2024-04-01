namespace Components;

using System.Runtime.CompilerServices;
using Contracts;
using MassTransit;


public static class CorrelationInitializer
{
    #pragma warning disable CA2255
    [ModuleInitializer]
    #pragma warning restore CA2255
    public static void Initialize()
    {
        // MessageCorrelation.UseCorrelationId<GetRegistrationStatus>(x => x.SubmissionId);
        MessageCorrelation.UseCorrelationId<SubmitPayment>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<PaymentReceived>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<ProcessTransaction>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<TransactionCompleted>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<CreateTransactionFailed>(x => x.PaymentId);
        
        MessageCorrelation.UseCorrelationId<ProcessPayment>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<PaymentCompleted>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<CreateOrderFailed>(x => x.PaymentId);
        MessageCorrelation.UseCorrelationId<CreateInvoiceFailed>(x => x.PaymentId);
        
        // MessageCorrelation.UseCorrelationId<RegistrationStatus>(x => x.SubmissionId);
        
        MessageCorrelation.UseCorrelationId<RetryDelayExpired>(x => x.RegistrationId);
    }
}