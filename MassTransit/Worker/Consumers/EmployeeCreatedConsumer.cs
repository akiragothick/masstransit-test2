using Contracts;
using MassTransit;

namespace Worker.Consumers;

public class EmployeeCreatedConsumer(ILogger<EmployeeCreatedConsumer> _logger) : IConsumer<EmployeeCreated>
{
    public Task Consume(ConsumeContext<EmployeeCreated> context)
    {
        _logger.LogInformation("Employee received: {EmployeeId}", context.Message.Id);

        if (context.Message.Id == 0)
        {
            _logger.LogInformation("Employee received error: {EmployeeId}", context.Message.Id);
            throw new TransientException(nameof(context.Message.Id));
        }

        _logger.LogInformation("Employee accepted: {EmployeeId}", context.Message.Id);
        
        return Task.CompletedTask;
    }
}