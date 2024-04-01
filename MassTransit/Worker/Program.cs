using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Worker;
using Worker.Consumers;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog();

//builder.Services.AddHostedService<Worker>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<EmployeeCreatedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.ConfigureEndpoints(context);

        cfg.ReceiveEndpoint("mi-queue", e =>
        {
            e.ConfigureConsumer<EmployeeCreatedConsumer>(context);

            e.UseMessageRetry(r =>
            {
                r.Interval(3, TimeSpan.FromSeconds(5));
                r.Handle<TransientException>(); 
            });
        });
    });

    x.AddEntityFrameworkOutbox<AppDbContext>(opt =>
    {
        opt.DuplicateDetectionWindow = TimeSpan.FromSeconds(30);
        opt.UseSqlServer().UseBusOutbox();
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, opt => { opt.EnableRetryOnFailure(5); });
    //options.EnableDetailedErrors();
});

var host = builder.Build();
host.Run();

public class TransientException :
    Exception
{
    public TransientException()
    {
    }

    public TransientException(string message)
        : base("Gaaa " + message)
    {
    }
}