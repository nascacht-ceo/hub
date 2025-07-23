using Azure.Messaging.ServiceBus;
using MediatR;
using Microsoft.Extensions.Logging;
using nc.Cloud.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace nc.Azure.Notification;

public class AzureNotificationService : INotificationService
{
    private readonly ServiceBusSender _sender;
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger _logger;

    public AzureNotificationService(ServiceBusClient client, string topic, string subscription, ILogger<AzureNotificationService> logger)
    {
        _sender = client.CreateSender(topic);
        _processor = client.CreateProcessor(topic, subscription);
        _logger = logger;
    }

    public bool IsEnabled => true;

    public async Task PublishAsync<T>(T notification, CancellationToken ct = default) where T : INotification
    {
        var json = JsonSerializer.Serialize(notification);
        var message = new ServiceBusMessage(json)
        {
            Subject = typeof(T).FullName
        };
        await _sender.SendMessageAsync(message, ct);
    }

    public Task SubscribeAsync(Func<INotification, Task> onNotification, CancellationToken ct = default)
    {
        _processor.ProcessMessageAsync += async args =>
        {
            var body = args.Message.Body.ToString();
            //var notification = JsonSerializer.Deserialize<RefreshRequested>(body); // example
            //if (notification != null)
            //    await onNotification(notification);

            await args.CompleteMessageAsync(args.Message);
        };

        _processor.ProcessErrorAsync += args =>
        {
            _logger.LogError(args.Exception, "ServiceBus processing error");
            return Task.CompletedTask;
        };

        return _processor.StartProcessingAsync(ct);
    }
}

