using Components.Activities.CreateInvoice;
using Components.Activities.CreateOrder;
using Contracts;
using MassTransit;
using MassTransit.Courier.Contracts;
using Microsoft.Extensions.Logging;

namespace Components.Consumer;

public class ProcessPaymentConsumer(ILogger<ProcessPaymentConsumer> logger, IEndpointAddressProvider provider) : IConsumer<ProcessPayment>
{
    public async Task Consume(ConsumeContext<ProcessPayment> context)
    {
        logger.LogInformation("Processing payment: {0} ({1})", context.Message.PaymentId, context.Message.DocumentNumber);

        var routingSlip = CreateRoutingSlip(context);

        await context.Execute(routingSlip).ConfigureAwait(false);
    }

    private RoutingSlip CreateRoutingSlip(ConsumeContext<ProcessPayment> context)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid());

        builder.SetVariables(new
        {
            context.Message.Reference,
            context.Message.DocumentNumber
        });

        builder.AddActivity("OrderRegistration", provider.GetExecuteEndpoint<CreateOrderActivity, CreateOrderActivityArguments>(), new
        {
            context.Message.Amount
        });

        builder.AddSubscription(context.SourceAddress!, RoutingSlipEvents.ActivityFaulted, RoutingSlipEventContents.None, "OrderRegistration",
            x => x.Send<CreateOrderFailed>(new { context.Message.PaymentId }));

        
        
        builder.AddActivity("InvoiceRegistration", provider.GetExecuteEndpoint<CreateInvoiceActivity, CreateInvoiceActivityArguments>());

        builder.AddSubscription(context.SourceAddress!, RoutingSlipEvents.ActivityFaulted, RoutingSlipEventContents.None, "InvoiceRegistration",
            x => x.Send<CreateInvoiceFailed>(new { context.Message.PaymentId }));

        
        
        builder.AddSubscription(context.SourceAddress!, RoutingSlipEvents.Completed,
            x => x.Send<PaymentCompleted>(new { context.Message.PaymentId }));
        

        return builder.Build();
    }
}