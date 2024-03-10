using System.Data;
using MassTransit.SqlTransport;
using MassTransit.SqlTransport.Configuration;
using MassTransit;
using MassTransit.SqlTransport.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Producer;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Consumer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("MassTransit", LogEventLevel.Debug)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();
            await CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions<SqlTransportOptions>().Configure(options =>
                    {
                        options.Host = "localhost";
                        options.Database = "masstransit_transport_tests";
                        options.Schema = "transport";
                        options.Role = "transport";
                        options.Username = "postgres";
                        options.Password = "p@ssword";
                        options.AdminUsername = "postgres";
                        options.AdminPassword = "p@ssword";
                    });

                    services.AddTransient<ISqlTransportDatabaseMigrator, PostgresDatabaseMigrator>();
                    services.AddHostedService<ProducerService>();

                    services.AddPostgresMigrationHostedService();
                    services.AddMassTransit(x =>
                    {
                        x.UsingPostgres();

                        // x.SetBusFactory(new SqlRegistrationBusFactory((ctx, config) =>
                        // {
                        //     config.UsePostgres(ctx,
                        //         hostConfig => { hostConfig.IsolationLevel = IsolationLevel.ReadCommitted; });
                        // }));
                    });
                });
        }
    }
}