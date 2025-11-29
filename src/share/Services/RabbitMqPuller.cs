using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;


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

		await _channelLock.WaitAsync(ct);
		try
		{
			if (_channels.TryTake(out var ch) && ch.IsOpen)
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
		try
		{
			_channels.Add(channel);
		}
		finally
		{
			channel.Dispose();
		}
		_channelLock.Release();
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

	private async Task EnsureQueueExistsAsync(IChannel channel, string queueName)
	{
		try
		{
			await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
		}
		catch
		{
			// Queue might already exist, ignore
		}
	}

	private T? ProcessResult<T>(BasicGetResult? result)
	{
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

	public async Task<T?> PullAsync<T>(string queueName, CancellationToken cancellationToken = default)
	{
		IChannel? ch = null;
		try
		{
			ch = await RentChannelAsync(cancellationToken);
			await EnsureQueueExistsAsync(ch, queueName);
			BasicGetResult? result = await ch.BasicGetAsync(queueName, autoAck: true, cancellationToken);
			return ProcessResult<T>(result);
		}
		catch (OperationInterruptedException ex) when (ex.ShutdownReason?.ReplyCode == 404)
		{
			// Queue doesn't exist, channel might be closed, get a new one and create queue
			if (ch != null)
			{
				ReturnChannel(ch);
				ch = null; // Mark as returned
			}
			ch = await RentChannelAsync(cancellationToken);
			await EnsureQueueExistsAsync(ch, queueName);
			// Retry BasicGetAsync - queue should exist now
			BasicGetResult? result = await ch.BasicGetAsync(queueName, autoAck: true, cancellationToken);
			return ProcessResult<T>(result);
		}
		finally
		{
			if (ch != null)
			{
				ReturnChannel(ch);
			}
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


