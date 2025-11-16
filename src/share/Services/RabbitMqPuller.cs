using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace GoogleForDummies.Services;

public interface IRabbitMqPuller
{
	Task<T?> PullAsync<T>(string queueName, CancellationToken cancellationToken = default);
}

public sealed class RabbitMqPuller : IRabbitMqPuller, IAsyncDisposable
{
	private readonly ConnectionFactory _factory;
	private readonly int _poolSize;
	private IConnection? _connection;
	private readonly ConcurrentBag<IChannel> _channels = new();
	private readonly SemaphoreSlim _channelLock;
	private readonly JsonSerializerOptions _jsonOptions;

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


