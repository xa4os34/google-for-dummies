using Gfd.Models;
using Pgvector;
using LMKit.Embeddings;
using LMKit.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gfd.Services;

public class LanguageModel
{
    private readonly Embedder _embedder;

    public LanguageModel(IConfiguration configuration, ILogger<LanguageModel> logger)
    {
        logger.LogInformation("Start initializing LMKit.)");
        LMKit.Global.Runtime.Initialize();
        logger.LogInformation("End of LMKit initialization.)");

        var modelUrl = configuration.GetValue<string?>("EMBEDDER_MODEL_URL");

        if (string.IsNullOrEmpty(modelUrl))
        {
            logger.LogWarning("Using default model (nomic-embed-text).");
            modelUrl = "https://huggingface.co/lm-kit/nomic-embed-text-1.5/resolve/main/nomic-embed-text-1.5-F16.gguf";
        }

        LM model = new LM(modelUrl);
        _embedder = new Embedder(model);
    }
    public float[] GetEmbeddings(string value) => _embedder.GetEmbeddings(value);
    public WebsiteRecord IndexingDataToWebsiteRecord(IndexingData data)
    {
        float[] titleMeaning = _embedder.GetEmbeddings(data.Title);
        float[] descriptionMeaning = _embedder.GetEmbeddings(data.Description);
        float[] pageMeaning = _embedder.GetEmbeddings(data.PageText);

        return new WebsiteRecord(
            Id: Guid.Empty,
            Url: data.Url,
            Title: data.Title,
            Description: data.Description,
            TitleMeaning: new Vector(titleMeaning),
            DescriptionMeaning: new Vector(descriptionMeaning),
            PageMeaning: new Vector(pageMeaning)
        );
    }
}
