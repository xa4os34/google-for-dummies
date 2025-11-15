using Gfd.Models;
using LMKit.Embeddings;
using LMKit.Model;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace GoogleForDummys.Share.Services;

public class LanguageModel
{
    private readonly Embedder _embedder;

    public LanguageModel(IConfiguration configuration, ILogger logger)
    {
        logger.Information("Start initializing LMKit.)");
        LMKit.Global.Runtime.Initialize();
        logger.Information("End of LMKit initialization.)");

        var modelUrl = configuration.GetValue<string?>("EMBEDDER_MODEL_URL");

        if (string.IsNullOrEmpty(modelUrl))
        {
            logger.Warning("Using default model (nomic-embed-text).");
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
            TitleMeaning: titleMeaning,
            DescriptionMeaning: descriptionMeaning,
            PageMeaning: pageMeaning
        );
    }
}