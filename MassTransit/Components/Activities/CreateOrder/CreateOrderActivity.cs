using Components.Exceptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Components.Activities.CreateOrder;

public class CreateOrderActivity(ILogger<CreateOrderActivity> logger, IEndpointAddressProvider provider) : IActivity<CreateOrderActivityArguments, EventPaymentLog>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<CreateOrderActivityArguments> context)
    {
        var arguments = context.Arguments;
        
        logger.LogInformation("Creating order for payment reference: {Reference} ({DocumentNumber})", arguments.Reference, arguments.DocumentNumber);

        var total = arguments.Amount;

        if (arguments.DocumentNumber is "48239181")
        {
            total = 10.0m;
        }

        await Task.Delay(2000); // call create ov

        var docEntryId = new Random().Next(1, 100);

        logger.LogInformation("Created order for payment reference: {Reference} ({DocumentNumber}), generated id: {docEntryId}",
            arguments.Reference, arguments.DocumentNumber, docEntryId);

        switch (context.Arguments.DocumentNumber)
        {
            case "48239181":
                throw new RoutingSlipException($"The document number is invalid");
            case "48239182" when context.GetRetryAttempt() == 0 && context.GetRedeliveryCount() == 0:
                throw new TransientException("The payment provider isn't responding");
            case "48239183" when context.GetRedeliveryCount() == 0:
                throw new LongTransientException("The payment provider isn't responding after a long time");
        }
        
        // Para que haya compensate
        var log = new EventPaymentLog()
        {
            DocEntryOv = docEntryId
            //DocumentNumber = arguments.DocumentNumber
        };

        // Para llenar las variables y se agregue en el siguiente argumento
        var variables = new
        {
            DocEntryOv = docEntryId,
            PaymentId = 222222
        };

        // Validacion para agregar un paso adicional
        // if (arguments.EventId?.StartsWith("DANGER") ?? false)
        // {
        //     return context.ReviseItinerary(log, variables, x =>
        //     {
        //         x.AddActivitiesFromSourceItinerary();
        //         x.AddActivity("Assign Waiver", _provider.GetExecuteEndpoint<AssignWaiverActivity, AssignWaiverArguments>());
        //     });
        // }

        return context.CompletedWithVariables(log, variables);
    }

    public async Task<CompensationResult> Compensate(CompensateContext<EventPaymentLog> context)
    {
        logger.LogInformation("Removing order ({docEntry}) for payment reference: {Reference} ({DocumentNumber}",
            context.Log.DocEntryOv, context.Log.Reference, context.Log.DocumentNumber);

        await Task.Delay(10); // Cancel OV, search ov por reference y document number

        return context.Compensated();
    }
}