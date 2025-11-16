namespace Gfd.Models;

public class IndexingData
{
    public string Url { get; init; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? PageText { get; set; }
    public IndexingData(string url) => Url = url;
}


