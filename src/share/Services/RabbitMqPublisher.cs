using System.Collections.Concurrent;
using System.Text.Json;
using RabbitMQ.Client;
using GoogleForDummies.Services;

namespace Gfd.Services;

public interface IRabbitMqPublisher
{
	Task PublishAsync<T>(string queueName, T message, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default);
	Task PublishAsync<T>(string baseQueueName, MessagePriorityLevel priority, T message, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPublisher : IRabbitMqPublisher, IAsyncDisposable
{
	private readonly ConnectionFactory _factory;
	private readonly int _poolSize;
	private IConnection? _connection;
	private readonly ConcurrentBag<IChannel> _channels = new();
	private readonly SemaphoreSlim _channelLock;

	public RabbitMqPublisher(string hostName, string userName, string password, string virtualHost = "/", int port = 5672, int poolSize = 8)
	{
		_factory = new ConnectionFactory
		{
			HostName = hostName,
			UserName = userName,
			Password = password,
			VirtualHost = virtualHost,
			Port = port
		};
		_poolSize = Math.Max(1, poolSize);
		_channelLock = new SemaphoreSlim(_poolSize, _poolSize);
	}

	private async Task<IChannel> RentChannelAsync(CancellationToken ct)
	{
		if (_connection is null || !_connection.IsOpen)
		{
			_connection?.Dispose();
			_connection = await _factory.CreateConnectionAsync();
		}

		if (_channels.TryTake(out var ch) && ch.IsOpen)
			return ch;

		await _channelLock.WaitAsync(ct);
		try
		{
			if (_channels.TryTake(out ch) && ch.IsOpen)
				return ch;
			return await _connection.CreateChannelAsync();
		}
		catch
		{
			_channelLock.Release();
			throw;
		}
	}

	private void ReturnChannel(IChannel channel)
	{
		if (channel.IsOpen)
		{
			_channels.Add(channel);
			_channelLock.Release();
		}
		else
		{
			channel.Dispose();
			_channelLock.Release();
		}
	}

	public async Task PublishAsync<T>(string queueName, T message, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)
	{
		var ch = await RentChannelAsync(cancellationToken);
		try
		{
			var payload = JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
			await ch.BasicPublishAsync(exchange: string.Empty, routingKey: queueName, body: payload);
		}
		finally
		{
			ReturnChannel(ch);
		}
	}

	public Task PublishAsync<T>(string baseQueueName, MessagePriorityLevel priority, T message, JsonSerializerOptions? jsonOptions = null, CancellationToken cancellationToken = default)
	{
		var queue = priority.GetQueueName(baseQueueName);
		return PublishAsync(queue, message, jsonOptions, cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		while (_channels.TryTake(out var ch))
		{
			ch.Dispose();
		}
		_connection?.Dispose();
		await Task.CompletedTask;
		GC.SuppressFinalize(this);
	}
}


