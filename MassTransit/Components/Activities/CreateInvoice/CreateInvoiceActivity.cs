using Components.Exceptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Components.Activities.CreateInvoice;

public class CreateInvoiceActivity(ILogger<CreateInvoiceActivity> logger, IEndpointAddressProvider provider) : IActivity<CreateInvoiceActivityArguments, EventPaymentLog>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<CreateInvoiceActivityArguments> context)
    {
        var arguments = context.Arguments;

        logger.LogInformation("Creating invoice for OV ({docEntry})", arguments.DocEntryOv);

        await Task.Delay(2000); // call create invoice

        var docEntryInvoice = new Random().Next(1, 100);

        // if (context.GetRetryAttempt() == 0 && context.GetRedeliveryCount() == 0)
        //     throw new RoutingSlipException($"The document number is invalid");

        //if (context.GetRedeliveryCount() > 0)
            throw new TransientException("The payment provider isn't responding");

        if (context.GetRetryAttempt() > 0)
            throw new LongTransientException("The payment provider isn't responding after a long time");

        logger.LogInformation("Created invoice for OV ({docEntry})", arguments.DocEntryOv);

        // var log = new EventPaymentLog()
        // {
        //     DocEntryOv = docEntryId,
        //     DocumentNumber = arguments.DocumentNumber
        // };
        //
        // var variables = new
        // {
        //     docEntryId,
        //     Amount = total
        // };

        // Validacion para agregar un paso adicional
        // if (arguments.EventId?.StartsWith("DANGER") ?? false)
        // {
        //     return context.ReviseItinerary(log, variables, x =>
        //     {
        //         x.AddActivitiesFromSourceItinerary();
        //         x.AddActivity("Assign Waiver", _provider.GetExecuteEndpoint<AssignWaiverActivity, AssignWaiverArguments>());
        //     });
        // }

        return context.CompletedWithVariables(new
        {
            docEntryInvoice
        });
    }

    public async Task<CompensationResult> Compensate(CompensateContext<EventPaymentLog> context)
    {
        logger.LogInformation("Removing invoice ({docEntry}) for payment reference: {Reference} ({DocumentNumber}",
            context.Log.DocEntryInvoice, context.Log.Reference, context.Log.DocumentNumber);

        await Task.Delay(10); // Cancel OV, search ov por reference y document number

        return context.Compensated();
    }
}