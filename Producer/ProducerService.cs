using Contracts;
using MassTransit;
using MassTransit.SqlTransport;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Producer;

public class ProducerService : IHostedService
{
    private readonly IBus _bus;
    private readonly SqlTransportOptions _transportOptions;
    private readonly ISqlTransportDatabaseMigrator _migrator;
    private readonly ILogger<ProducerService> _logger;
    private readonly CancellationTokenSource _cts = new();

    public ProducerService(
        IBus bus,
        IOptions<SqlTransportOptions> transportOptions,
        ISqlTransportDatabaseMigrator migrator,
        ILogger<
            ProducerService> logger)
    {
        _bus = bus;
        _transportOptions = transportOptions.Value;
        _migrator = migrator;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Applying DB migration");
        await _migrator.CreateDatabase(_transportOptions);

        var endpoint = await _bus.GetSendEndpoint(new Uri("queue:message-queue"));

        _logger.LogInformation("Starting to produce messages");
        Task.Run(async () =>
        {
            uint i = 0;
            while (!_cts.IsCancellationRequested)
            {
                await endpoint.Send(new TestMessage((int)i + 1), x => x.SetPartitionKey(((i + 1) % 5).ToString()));
                i++;
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
}