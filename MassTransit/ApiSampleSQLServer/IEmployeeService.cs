using Components;
using Contracts;
using MassTransit;

namespace ApiSampleSQLServer;

public interface IEmployeeService
{
    Task<bool> GetData();
}

public class EmployeeService(SampleDbContext context, IPublishEndpoint publishEndpoint) : IEmployeeService
{
    public async Task<bool> GetData()
    {
        var employee = new Employee()
        {
            Code = "code2",
            Names = "name2"
        };

        await context.Set<Employee>().AddAsync(employee);

        var message = new EmployeeCreated() { Id = employee.Id, Code = employee.Code };
        await publishEndpoint.Publish(message);
        
        var result = await context.SaveChangesAsync();
        
        return result > 0;
    }
}