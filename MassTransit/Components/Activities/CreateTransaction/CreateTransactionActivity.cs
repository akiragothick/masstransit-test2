using Components.Exceptions;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Components.Activities.CreateTransaction;

public class CreateTransactionActivity(ILogger<CreateTransactionActivity> logger, IEndpointAddressProvider provider) : IActivity<CreateTransactionActivityArguments, EventPaymentLog>
{
    public async Task<ExecutionResult> Execute(ExecuteContext<CreateTransactionActivityArguments> context)
    {
        var arguments = context.Arguments;
        
        logger.LogInformation("Creating transaction for payment reference: {Reference} ({DocumentNumber})", arguments.Reference, arguments.DocumentNumber);

        await Task.Delay(2000); // call transaction

        var transactionId = Guid.NewGuid();

        logger.LogInformation("Created transaction for payment reference: {Reference} ({DocumentNumber}), generated id: {transactionId}",
            arguments.Reference, arguments.DocumentNumber, transactionId);

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
            TransactionId = transactionId,
            //DocumentNumber = arguments.DocumentNumber
        };

        // Para llenar las variables y se agregue en el siguiente argumento
        var variables = new
        {
            TransactionId = transactionId
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
        logger.LogInformation("Removing transaction ({transactionId}) for payment reference: {Reference} ({DocumentNumber}",
            context.Log.TransactionId, context.Log.Reference, context.Log.DocumentNumber);

        await Task.Delay(10); // Cancel transaction

        return context.Compensated();
    }
}