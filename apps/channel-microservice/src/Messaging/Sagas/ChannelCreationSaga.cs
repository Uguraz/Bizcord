using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChannelMicroservice.Messaging.Sagas;

public class ChannelCreationSaga
{
    private readonly ILogger<ChannelCreationSaga> _logger;

    public ChannelCreationSaga(ILogger<ChannelCreationSaga> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(string channelId, string name, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Saga started for channel {ChannelId} ({Name})", channelId, name);

        try
        {
            // Step 1: her kunne man fx sende en integration event til en message-bus
            // Vi simulerer arbejdet med en lille delay
            await Task.Delay(10, cancellationToken);

            // Step 2: her kunne man fx opdatere en audit-log eller en anden microservice

            _logger.LogInformation("Saga completed successfully for channel {ChannelId}", channelId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Saga for channel {ChannelId} was cancelled.", channelId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Saga failed for channel {ChannelId}. Executing compensation.", channelId);

            // Compensation-step (her ville man typisk rulle ting tilbage – vi logger for at vise stedet)
            _logger.LogWarning("Compensation executed for channel {ChannelId}", channelId);
        }
    }
}