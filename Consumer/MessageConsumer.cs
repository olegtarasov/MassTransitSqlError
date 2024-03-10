using Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Consumer;

public class MessageConsumer : IConsumer<TestMessage>
{
    private readonly ILogger<MessageConsumer> _logger;

    public MessageConsumer(ILogger<MessageConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TestMessage> context)
    {
        _logger.LogInformation("Index {Index}, partition key {Key}", context.Message.Index, context.PartitionKey());
    }
}