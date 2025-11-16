using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;

[Obsolete]
public class PriorityQueue<T>
{
    private Random random = new Random();
    private IChannel _channel;
    private string _queueName;

    public PriorityQueue(IChannel channel, string queueName)
    {
        _channel = channel;
        _queueName = queueName;
    }

    private string RealTimePriorityQueueName =>
        PriorityLevel.RealTime.GetQueueName(_queueName);

    private string OnlyWhenIdlePriorityQueueName =>
        PriorityLevel.OnlyWhenIdle.GetQueueName(_queueName);

    public async Task<T?> PullMessageAsync()
    {
        string? chosenQueue = null;
        uint realTimeMessages = await _channel.MessageCountAsync(RealTimePriorityQueueName);

        if (realTimeMessages > 0)
        {
            chosenQueue = RealTimePriorityQueueName;
        }

        if (chosenQueue is null)
        {
            float x = random.NextSingle();
            PriorityLevel randomPriorityLevel = (PriorityLevel)(MathF.Sqrt(x) * (int)(PriorityLevel.High + 1));
            chosenQueue = randomPriorityLevel.GetQueueName(_queueName);
        }

        uint messages = await _channel.MessageCountAsync(RealTimePriorityQueueName);

        if (messages < 0)
        {
            chosenQueue = OnlyWhenIdlePriorityQueueName;
        }

        BasicGetResult? result = await _channel.BasicGetAsync(chosenQueue, true);
        if (result is null)
            return default(T?);

        string body = Encoding.UTF8.GetString(result.Body.ToArray());
        T? message = JsonSerializer.Deserialize<T?>(body);

        return message;
    }

    public async Task PublishMessageAsync(PriorityLevel level, T message)
    {
        string json = JsonSerializer.Serialize(message);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await _channel.BasicPublishAsync(string.Empty, level.GetQueueName(_queueName), bytes);
    }
}

public static class PriorityLevelEx
{
    public static string GetQueueName(this PriorityLevel priorityLevel, string queueName)
    {
        return $"{queueName}-{Enum.GetName(priorityLevel)}";
    }
}

public enum PriorityLevel
{
    OnlyWhenIdle = -1,
    Low = 0,
    Normal = 1,
    High = 2,
    RealTime = 3
}

public delegate Task MessageHandler<T>(T message);

public class PollingService<T> : IHostedService
{
    private PollingOptions _options;
    private PriorityQueue<T> _queue;
    private MessageHandler<T> _handler;
    private Task? _pollingLoop;
    private CancellationTokenSource? _cancellationSource;
    private List<Task> _backgroundHandlers;
    

    public PollingService(
        PollingOptions options,
        PriorityQueue<T> queue,
        MessageHandler<T> handler)
    {
        _options = options;
        _queue = queue;
        _handler = handler;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_cancellationSource is null) {
            throw new InvalidOperationException("Service already running.");
        }

        _cancellationSource = new CancellationTokenSource();
        _pollingLoop = PollingLoop(_cancellationSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {   
        if (_cancellationSource is null) {
            throw new InvalidOperationException("Service is not running.");
        }
        await _cancellationSource.CancelAsync();
        await _pollingLoop!.WaitAsync(CancellationToken.None);
        _cancellationSource.Dispose();
        _cancellationSource = null;
        _pollingLoop = null;
    }

    public async Task PollingLoop(CancellationToken token)
    {   
        while (token.IsCancellationRequested) 
        {
            foreach (Task task in _backgroundHandlers.ToArray())
            {   
                if (task.IsCompleted)
                {
                    await task;
                    _backgroundHandlers.Remove(task);
                }
            }
            
            if (_backgroundHandlers.Count > _options.MaxCountOfConcurentTasks)
            {
                await Task.Delay(_options.TaskWaitingSleepTime);
                continue;
            }

            T? message = await _queue.PullMessageAsync();

            if (message is null) 
            {
                await Task.Delay(_options.IdleSleepTime);
                continue;
            }

            _backgroundHandlers.Add(_handler(message));
        }
    }
}

public class PollingOptions 
{
    public TimeSpan IdleSleepTime { get; init;} = TimeSpan.FromMilliseconds(100);
    public TimeSpan TaskWaitingSleepTime { get; init; } = TimeSpan.FromMilliseconds(20);
    public int MaxCountOfConcurentTasks { get; init; } = 10;
}
