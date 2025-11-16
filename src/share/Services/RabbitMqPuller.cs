using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;


namespace Gfd.Services;

public interface IRabbitMqPuller
{
	Task<T?> PullAsync<T>(string queueName, CancellationToken cancellationToken = default);
	Task<(MessagePriorityLevel Priority, T? Message)> PullWithPriorityAsync<T>(string baseQueueName, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPuller : IRabbitMqPuller, IAsyncDisposable
{
	private readonly ConnectionFactory _factory;
	private readonly int _poolSize;
	private IConnection? _connection;
	private readonly ConcurrentBag<IChannel> _channels = new();
	private readonly SemaphoreSlim _channelLock;
	private readonly JsonSerializerOptions _jsonOptions;
	private readonly Random _random = new();

	public RabbitMqPuller(string hostName, string userName, string password, string virtualHost = "/", int port = 5672, int poolSize = 8, JsonSerializerOptions? jsonOptions = null)
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
		_jsonOptions = jsonOptions ?? new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
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

	private Task<T?> PullAsync<T>(string baseQueueName, MessagePriorityLevel priority, CancellationToken cancellationToken = default)
	{
		var queue = priority.GetQueueName(baseQueueName);
		return PullAsync<T>(queue, cancellationToken);
	}

	public async Task<(MessagePriorityLevel Priority, T? Message)> PullWithPriorityAsync<T>(string baseQueueName, CancellationToken cancellationToken = default)
	{
		int roll = _random.Next(1, 16);
		MessagePriorityLevel level = roll <= 1 ? MessagePriorityLevel.OnlyWhenIdle
			: roll <= 3 ? MessagePriorityLevel.Low
			: roll <= 6 ? MessagePriorityLevel.Normal
			: roll <= 10 ? MessagePriorityLevel.High
			: MessagePriorityLevel.RealTime;

		return (level, await PullAsync<T>(baseQueueName, level, cancellationToken));
	}

	public async Task<T?> PullAsync<T>(string queueName, CancellationToken cancellationToken = default)
	{
		var ch = await RentChannelAsync(cancellationToken);
		try
		{
			BasicGetResult? result = await ch.BasicGetAsync(queueName, autoAck: true, cancellationToken);
			if (result is null || result.Body.IsEmpty)
				return default;

			if (typeof(T) == typeof(byte[]))
			{
				object bytes = result.Body.ToArray();
				return (T)bytes;
			}

			if (typeof(T) == typeof(string))
			{
				object s = Encoding.UTF8.GetString(result.Body.ToArray());
				return (T)s;
			}

			return JsonSerializer.Deserialize<T>(result.Body.Span, _jsonOptions);
		}
		finally
		{
			ReturnChannel(ch);
		}
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


