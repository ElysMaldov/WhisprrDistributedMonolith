using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Whisprr.Infrastructure.RabbitMQ.Config;

/// <summary>
/// Manages the RabbitMQ connection lifecycle with automatic recovery.
/// </summary>
public sealed class RabbitMQConnectionManager : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQConnectionManager> _logger;

    public RabbitMQConnectionManager(IConnection connection, ILogger<RabbitMQConnectionManager> logger)
    {
        _connection = connection;
        _logger = logger;

        _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;
        _connection.ConnectionRecoveryErrorAsync += OnConnectionRecoveryErrorAsync;
    }

    public Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        return _connection.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public bool IsConnected => _connection.IsOpen;

    private Task OnConnectionShutdownAsync(object sender, ShutdownEventArgs args)
    {
        _logger.LogWarning("RabbitMQ connection shutdown: {Reason}", args.ReplyText);
        return Task.CompletedTask;
    }

    private Task OnConnectionRecoveryErrorAsync(object sender, ConnectionRecoveryErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ connection recovery error");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;
        _connection.ConnectionRecoveryErrorAsync -= OnConnectionRecoveryErrorAsync;
        await _connection.DisposeAsync();
    }
}
