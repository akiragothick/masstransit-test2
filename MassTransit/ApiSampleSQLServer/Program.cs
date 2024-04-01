using System.Reflection;
using ApiSampleSQLServer;
using Components;
using Components.Exceptions;
using Contracts;
using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

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

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SampleDbContext>(options =>
{
    options.UseSqlServer(connectionString, opt =>
    {
        opt.MigrationsAssembly(Assembly.GetExecutingAssembly().GetName().Name);
        opt.MigrationsHistoryTable("ef_migration_history");
    });
    options.EnableDetailedErrors();
});
builder.Services.AddHostedService<MigrationHostedService<SampleDbContext>>();

builder.Services.AddSingleton<IEndpointAddressProvider, DbEndpointAddressProvider>();

builder.Services.ConfigureSqlServerTransport(connectionString);

builder.Services.AddMassTransit(x =>
{
    x.SetEntityFrameworkSagaRepositoryProvider(r =>
    {
        r.ExistingDbContext<SampleDbContext>();
        r.UseSqlServer();
    });

    x.AddSagaRepository<JobSaga>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<SampleDbContext>();
            r.UseSqlServer();
        });
    x.AddSagaRepository<JobTypeSaga>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<SampleDbContext>();
            r.UseSqlServer();
        });
    x.AddSagaRepository<JobAttemptSaga>()
        .EntityFrameworkRepository(r =>
        {
            r.ExistingDbContext<SampleDbContext>();
            r.UseSqlServer();
        });
    
    x.SetKebabCaseEndpointNameFormatter();
    
    x.AddEntityFrameworkOutbox<SampleDbContext>(opt =>
    {
        //opt.QueryDelay = TimeSpan.FromSeconds(1);
        opt.UseSqlServer();
        // opt.UseBusOutbox(e =>
        //     e.DisableDeliveryService()
        // );
        //opt.DisableInboxCleanupService();
    });

    x.AddConfigureEndpointsCallback((context, _, cfg) =>
    {
        cfg.UseDelayedRedelivery(r =>
        {
            r.Handle<LongTransientException>();
            r.Interval(10000, 15000);
        });

        cfg.UseMessageRetry(r =>
        {
            r.Handle<TransientException>();
            r.Interval(25, 50);
        });

        cfg.UseEntityFrameworkOutbox<SampleDbContext>(context);
    });
    
    x.AddConsumersFromNamespaceContaining<ComponentsNamespace>();
    x.AddActivitiesFromNamespaceContaining<ComponentsNamespace>();
    x.AddSagaStateMachinesFromNamespaceContaining<ComponentsNamespace>();

    x.AddDelayedMessageScheduler();
     x.UsingSqlServer((context, cfg) =>
     {
         cfg.UseDbMessageScheduler();
    
         cfg.AutoStart = true;
    
         cfg.ConfigureEndpoints(context);
     });
    
    // x.UsingInMemory((context, cfg) =>
    // {
    //     cfg.UseDbMessageScheduler();
    //
    //     cfg.AutoStart = true;
    //     
    //     cfg.ConfigureEndpoints(context);
    // });
    //
    // x.AddDelayedMessageScheduler();
    //
    //
    // x.UsingRabbitMq((context, cfg) =>
    // { 
    //     // cfg.UseCircuitBreaker(cb =>
    //     // {
    //     //     cb.TripThreshold = 15;
    //     //     cb.ActiveThreshold = 10;
    //     //     cb.ResetInterval = TimeSpan.FromMinutes(5);
    //     // });
    //     cfg.UseDelayedMessageScheduler();
    //     cfg.AutoStart = true;
    //     cfg.ConfigureEndpoints(context);
    // });
});

builder.Services.AddOptions<MassTransitHostOptions>()
    .Configure(options =>
    {
        options.WaitUntilStarted = true;
        options.StartTimeout = TimeSpan.FromSeconds(10);
        options.StopTimeout = TimeSpan.FromSeconds(30);
        options.ConsumerStopTimeout = TimeSpan.FromSeconds(10);
    });
builder.Services.AddOptions<HostOptions>()
    .Configure(options => options.ShutdownTimeout = TimeSpan.FromMinutes(1));

builder.Services.AddScoped<IEmployeeService, EmployeeService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/weatherforecast", async ([FromServices] IEmployeeService employeeService) =>
    {
        await employeeService.GetData();

        return "";
    })
    .WithOpenApi();

app.MapPost("/payment", async ([FromBody] Payment payment, [FromServices] IPublishEndpoint publishEndpoint) =>
    {
        await publishEndpoint.Publish<SubmitPayment>(payment);

        var response = new
        {
            payment.PaymentId,
            Actions = new Dictionary<string, string> { { "Status", "Url.Link(\"RegistrationStatus\", new { paymentId = payment.PaymentId })!" } }
        };

        return response;
    });


static Task HealthCheckResponseWriter(HttpContext context, HealthReport result)
{
    context.Response.ContentType = "application/json";

    return context.Response.WriteAsync(result.ToJsonString());
}

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter
});
app.MapHealthChecks("/health/live", new HealthCheckOptions { ResponseWriter = HealthCheckResponseWriter });

app.Run();