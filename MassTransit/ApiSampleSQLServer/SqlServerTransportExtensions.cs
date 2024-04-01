namespace ApiSampleSQLServer;

using MassTransit;
using Microsoft.Data.SqlClient;


public static class SqlServerTransportExtensions
{
    /// <summary>
    /// Works some shenanigans to get all the host options configured for the Postgresql transport
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString"></param>
    /// <param name="create"></param>
    /// <param name="delete"></param>
    /// <returns></returns>
    public static IServiceCollection ConfigureSqlServerTransport(this IServiceCollection services, string? connectionString, bool create = true,
        bool delete = false)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);

        services.AddOptions<SqlTransportOptions>().Configure(options =>
        {
            options.Host = "localhost\\SQLEXPRESS";
            options.Database = builder.InitialCatalog ?? "MassTransitSample";
            options.Schema = "transport";
            options.Role = "transport";
            options.Username = "akiragothick";
            options.Password = "S0p0rt333/";
            options.AdminUsername = "sa";
            options.AdminPassword = "S0p0rt333#";
        });

        services.AddSqlServerMigrationHostedService(create, delete);

        return services;
    }
}