using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Components.Consumer;

public class SubmitPaymentConsumer: IConsumer<SubmitPayment>
{
    readonly ILogger<SubmitPaymentConsumer> _logger;

    public SubmitPaymentConsumer(ILogger<SubmitPaymentConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<SubmitPayment> context)
    {
        _logger.LogInformation("Payment received: {PaymentId} ({DocumentNumber})", context.Message.PaymentId, context.Message.DocumentNumber);

        ValidateRegistration(context.Message);

        await context.Publish<PaymentReceived>(context.Message);

        _logger.LogInformation("Payment accepted: {PaymentId} ({DocumentNumber})", context.Message.PaymentId, context.Message.DocumentNumber);
    }

    void ValidateRegistration(SubmitPayment message)
    {
        if (string.IsNullOrWhiteSpace(message.DocumentNumber))
            throw new ArgumentNullException(nameof(message.DocumentNumber));
    }
}