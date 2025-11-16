using Microsoft.AspNetCore.Mvc;
using Api_server.Models;
using Gfd.Services;
using Gfd.Models;
using Pgvector;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace Api_server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IGfdDataService _dataService;
    private readonly LanguageModel _languageModel;

    public SearchController(IGfdDataService dataService, LanguageModel languageModel)
    {
        _dataService = dataService;
        _languageModel = languageModel;
    }
    
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Search query cannot be empty" });
        }

        var stopwatch = Stopwatch.StartNew();

        var queryVector = new Vector(_languageModel.GetEmbeddings(request.Query));

        // For now, just search Page. A more advanced implementation could search multiple targets.
        var searchResults = await _dataService.SearchAsync(queryVector, SearchTarget.Page, request.PageSize, request.PageNumber);

        stopwatch.Stop();

        var response = new SearchResponse
        {
            Query = request.Query,
            TotalCount = (int)searchResults.TotalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
            Results = searchResults.Results.Select(r => new SearchResult
            {
                Title = r.Title,
                Url = r.Url,
                Snippet = r.Description, // Using description as snippet
                RelevanceScore = 0 // As per user instruction, not calculating score.
            }).ToList()
        };

        return Ok(response);
    }
}

