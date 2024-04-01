using Components.Activities.CreateTransaction;
using Contracts;
using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;

namespace Components.Consumer;

public class ProcessTransactionConsumer(ILogger<ProcessTransactionConsumer> logger, IEndpointAddressProvider provider) : IConsumer<ProcessTransaction>
{
    public async Task Consume(ConsumeContext<ProcessTransaction> context)
    {
        logger.LogInformation("Processing transaction: {0} ({1})", context.Message.PaymentId, context.Message.DocumentNumber);

        var routingSlip = CreateRoutingSlip(context);

        await context.Execute(routingSlip).ConfigureAwait(false);
    }

    private RoutingSlip CreateRoutingSlip(ConsumeContext<ProcessTransaction> context)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());

        builder.SetVariables(new
        {
            context.Message.Reference,
            context.Message.DocumentNumber
        });

        builder.AddActivity("TransactionRegistration", provider.GetExecuteEndpoint<CreateTransactionActivity, CreateTransactionActivityArguments>(), new
        {
            context.Message.Amount
        });

        builder.AddSubscription(context.SourceAddress!, RoutingSlipEvents.ActivityFaulted, RoutingSlipEventContents.None, "TransactionRegistration",
            x => x.Send<CreateTransactionFailed>(new { context.Message.PaymentId }));

        // register other transactions
        
        builder.AddSubscription(context.SourceAddress!, RoutingSlipEvents.Completed,
            x => x.Send<TransactionCompleted>(new { context.Message.PaymentId }));

        return builder.Build();
    }
}