using Contracts;
using MassTransit;

namespace Components.StateMachines.Payment;

public class PaymentStateMachine: MassTransitStateMachine<PaymentState>
{
    
    
    public PaymentStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        // Event(() => RegistrationStatusRequested, x =>
        // {
        //     x.ReadOnly = true;
        //     x.OnMissingInstance(m => m.Fault());
        // });
        
        Schedule(() => RetryDelayExpired, saga => saga.ScheduleRetryToken, x =>
        {
            x.Received = r =>
            {
                r.CorrelateById(context => context.Message.RegistrationId);
                r.ConfigureConsumeTopology = false;
            };
        });
                
        Initially(
            When(EventPaymentReceived)
                .Initialize()
                .InitiateTransaction()
                .TransitionTo(Received));
        
        During(Received,
            When(EventTransactionCompleted)
                .InitiatePayment()
                .TransitionTo(Saved)
            ,When(CreateTransactionFailed)
                .TransactionFailed()
                .TransitionTo(Suspended)
            );
        
        During(Saved,
            When(EventPaymentCompleted)
                .Paid()
                .TransitionTo(Paid)
            ,When(CreateOrderFailed)
                .OrderFailed()
                .TransitionTo(Suspended)
            ,When(CreateInvoiceFailed)
                .InvoiceFailed()
                .TransitionTo(Suspended)
            );

        During(WaitingToRetry,
            When(RetryDelayExpired.Received)
                .RetryProcessing()
                .TransitionTo(Received));
        
        // CreateTransactionFailed
        // During(Suspended,
        //     When(EventPaymentReceived)
        //         .Initialize()
        //         .InitiateTransaction()
        //         .TransitionTo(Received));
        
        // could easily be configured via options
        const int retryCount = 5;
        var retryDelay = TimeSpan.FromSeconds(10);
        
        WhenEnter(Suspended, x => x
            .If(context => context.Saga.RetryAttempt < retryCount,
                retry => retry
                    .Schedule(RetryDelayExpired, context => new RetryDelayExpired(context.Saga.CorrelationId), _ => retryDelay)
                    .TransitionTo(WaitingToRetry)
            )
        );

    }
    
    // ReSharper disable UnassignedGetOnlyAutoProperty
    // ReSharper disable MemberCanBePrivate.Global
    public State Received { get; } = null!;
    public State WaitingToRetry { get; } = null!;
    public State Saved { get; } = null!;
    public State Paid { get; } = null!;
    public State Suspended { get; } = null!;
    
    public Event<PaymentReceived> EventPaymentReceived { get; } = null!;
    public Event<CreateTransactionFailed> CreateTransactionFailed { get; } = null!;
    public Event<TransactionCompleted> EventTransactionCompleted { get; } = null!;
    
    public Event<CreateOrderFailed> CreateOrderFailed { get; } = null!;
    public Event<CreateInvoiceFailed> CreateInvoiceFailed { get; } = null!;
    public Event<PaymentCompleted> EventPaymentCompleted { get; } = null!;
    
    public Schedule<PaymentState, RetryDelayExpired> RetryDelayExpired { get; } = null!;
}

static class PaymentStateMachineBehaviorExtensions
{
    public static EventActivityBinder<PaymentState, PaymentReceived> Initialize(
        this EventActivityBinder<PaymentState, PaymentReceived> binder)
    {
        return binder.Then(context =>
        {
            context.Saga.Reference = context.Message.Reference;
            context.Saga.DocumentNumber = context.Message.DocumentNumber;
            context.Saga.Amount = context.Message.Amount;
            context.Saga.RetryAttempt = 0;
            //context.Saga.ScheduleRetryToken = Guid.NewGuid();

            LogContext.Info?.Log("Processing: {0} ({1})", context.Message.PaymentId, context.Message.DocumentNumber);
        });
    }
    
    public static EventActivityBinder<PaymentState, PaymentReceived> InitiateTransaction(
        this EventActivityBinder<PaymentState, PaymentReceived> binder)
    {
        return binder.PublishAsync(context => context.Init<ProcessTransaction>(context.Message));
    }
    
    public static EventActivityBinder<PaymentState, TransactionCompleted> InitiatePayment(
        this EventActivityBinder<PaymentState, TransactionCompleted> binder)
    {
        return binder.PublishAsync(context => context.Init<ProcessPayment>(context.Message));
    }
    
    public static EventActivityBinder<PaymentState, CreateTransactionFailed> TransactionFailed(
        this EventActivityBinder<PaymentState, CreateTransactionFailed> binder)
    {
        return binder.Then(context =>
        {
            LogContext.Info?.Log("Transaction failed: {0} ({1}) - {2}", context.Message.PaymentId, context.Saga.DocumentNumber,
                context.Message.ExceptionInfo?.Message);

            context.Saga.Reason = "Transaction Failed";
        });
    }
    
    public static EventActivityBinder<PaymentState, CreateOrderFailed> OrderFailed(
        this EventActivityBinder<PaymentState, CreateOrderFailed> binder)
    {
        return binder.Then(context =>
        {
            LogContext.Info?.Log("Order failed: {0} ({1}) - {2}", context.Message.PaymentId, context.Saga.DocumentNumber,
                context.Message.ExceptionInfo?.Message);

            context.Saga.Reason = "Order Failed";
        });
    }
    
    public static EventActivityBinder<PaymentState, CreateInvoiceFailed> InvoiceFailed(
        this EventActivityBinder<PaymentState, CreateInvoiceFailed> binder)
    {
        return binder.Then(context =>
        {
            LogContext.Info?.Log("Invoice Failed: {0} ({1}) - {2}", context.Message.PaymentId, context.Saga.DocumentNumber,
                context.Message.ExceptionInfo?.Message);

            context.Saga.Reason = "Invoice Failed";
        });
    }
    
    public static EventActivityBinder<PaymentState, PaymentCompleted> Paid(
        this EventActivityBinder<PaymentState, PaymentCompleted> binder)
    {
        return binder.Then(context =>
        {
            LogContext.Info?.Log("Paid: {0} ({1})", context.Message.PaymentId, context.Saga.DocumentNumber);

            context.Saga.DocEntryOv = context.GetVariable<int>("DocEntryOv");
            context.Saga.RegistrationId = context.GetVariable<Guid>("PaymentId"); //transactionid
        });
    }
    
    public static EventActivityBinder<PaymentState, RetryDelayExpired> RetryProcessing(
        this EventActivityBinder<PaymentState, RetryDelayExpired> binder)
    {
        return binder
            .Then(context => context.Saga.RetryAttempt++)
            .PublishAsync(context => context.Init<ProcessTransaction>(new
            {
                PaymentId = context.Saga.CorrelationId,
                
                context.Saga.Reference,
                context.Saga.DocumentNumber,
                context.Saga.Amount,
                
                __Header_Payment_RetryAttempt = context.Saga.RetryAttempt
            }));
    }
}