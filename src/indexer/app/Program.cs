using Gfd.RabbitMQ;
using LMKit.Model;
using LMKit.Embeddings;
using RabbitMQ.Client;
using Npgsql;


const string QueueName = "IndexingQueue";

string EmbedderModelUrl = Environment.GetEnvironmentVariable("EMBEDDER_MODEL_URL") ??
    "https://huggingface.co/lm-kit/nomic-embed-text-1.5/resolve/main/nomic-embed-text-1.5-F16.gguf";

string DbConnectionString = Environment.GetEnvironmentVariable("DB_CONNSTR") ??
    "Host=127.0.0.1;Username=postgress;Password=postgress;Database=postgress";

string RabbitConnectionString = Environment.GetEnvironmentVariable("RABBITMQ_CONNSTR") ??
    "amqp://guest:guest@127.0.0.1/";

LMKit.Global.Runtime.Initialize();
LM model = new LM(EmbedderModelUrl);
Embedder embedder = new Embedder(model);

var dataSourceBuilder = new NpgsqlDataSourceBuilder(DbConnectionString);
NpgsqlDataSource dataSource = dataSourceBuilder.Build();
NpgsqlConnection database = await dataSource.OpenConnectionAsync();

var factory = new ConnectionFactory();
factory.AutomaticRecoveryEnabled = true;
factory.Uri = new Uri(RabbitConnectionString);
IConnection connection = await factory.CreateConnectionAsync();
IChannel channel = await connection.CreateChannelAsync();
await channel.QueueDeclareAsync(QueueName);
var queue = new PriorityQueue<IndexingData>(channel, QueueName);

var consumingService = new BasicQueuePollingService<IndexingData>(
        new PollingOptions() {
        }, queue, IndexingHandler);

await consumingService.StartAsync(CancellationToken.None);

await Task.Delay(-1);

Task IndexingHandler(IndexingData data)
{
    WebsiteRecord record = IndexingDataToWebsiteRecord(data);
    InsertWebsiteRecord(record);
    return Task.CompletedTask;
}

void InsertWebsiteRecord(WebsiteRecord websiteRecord)
{
    using (var cmd = new NpgsqlCommand(
                @"INSERT INTO WebsiteRecord (Url, Title, Description, TitleMeaning, DescriptionMeaning, PageMeaning) 
                VALUES (@url, @title, @description, @titleMeaning, @descriptionMeaning, @pageMeaning)", database))
    {
        cmd.Parameters.AddWithValue("url", websiteRecord.Url);
        cmd.Parameters.AddWithValue("title", websiteRecord.Title);
        cmd.Parameters.AddWithValue("description", websiteRecord.Description);
        cmd.Parameters.AddWithValue("titleMeaning", websiteRecord.TitleMeaning);
        cmd.Parameters.AddWithValue("descriptionMeaning", websiteRecord.DescriptionMeaning);
        cmd.Parameters.AddWithValue("pageMeaning", websiteRecord.DescriptionMeaning);
        cmd.ExecuteNonQuery();
    }
    Console.WriteLine($"New record added to database: {websiteRecord}");
}

WebsiteRecord IndexingDataToWebsiteRecord(IndexingData data)
{
    float[] titleMeaning = embedder.GetEmbeddings(data.Title);
    float[] descriptionMeaning = embedder.GetEmbeddings(data.Description);
    float[] pageMeaning = embedder.GetEmbeddings(data.PageText);

    return new WebsiteRecord(
            Id: Guid.Empty,
            Url: data.Url,
            Title: data.Title,
            Description: data.Description,
            TitleMeaning: titleMeaning,
            DescriptionMeaning: descriptionMeaning,
            PageMeaning: pageMeaning
            );
}

record IndexingData(
    string Url,
    string Title,
    string Description,
    string PageText
);

record WebsiteRecord(
    Guid Id,
    string Url,
    string Title,
    string Description,
    float[] TitleMeaning,
    float[] DescriptionMeaning,
    float[] PageMeaning
);
