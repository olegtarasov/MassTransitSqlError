using System.Data;
using MassTransit.SqlTransport;
using MassTransit.SqlTransport.Configuration;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

                    services.AddMassTransit(x =>
                    {
                        x.AddConsumer<MessageConsumer>();
                        x.UsingPostgres((ctx, config) =>
                        {
                            config.ReceiveEndpoint("message-queue", e =>
                            {
                                e.PrefetchCount = 1;
                                e.ConcurrentMessageLimit = 2;
                                e.SetReceiveMode(SqlReceiveMode.PartitionedOrdered, 1);
                                e.ConfigureConsumer<MessageConsumer>(ctx);
                            });
                        });

                        // x.SetBusFactory(new SqlRegistrationBusFactory((ctx, config) =>
                        // {
                        //     config.UsePostgres(ctx,
                        //         hostConfig => { hostConfig.IsolationLevel = IsolationLevel.ReadCommitted; });
                        //
                        //     config.ReceiveEndpoint("message-queue",
                        //         e =>
                        //         {
                        //             e.PrefetchCount = 1;
                        //             e.ConcurrentMessageLimit = 2;
                        //             e.SetReceiveMode(SqlReceiveMode.PartitionedGloballyOrdered, 1);
                        //             e.ConfigureConsumer<MessageConsumer>(ctx);
                        //         });
                        // }));
                    });
                });
        }
    }
}