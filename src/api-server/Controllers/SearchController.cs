using Microsoft.AspNetCore.Mvc;
using Api_server.Models;
using Gfd.Services;

namespace Api_server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
    private readonly IGfdDataService _dataService;
    public SearchController(IGfdDataService dataService)
    {
        _dataService = dataService;
    }
    
    [HttpPost("search")]
    public IActionResult Search([FromBody] SearchRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new { error = "Search query cannot be empty" });
        }

        return StatusCode(501, new { error = "Search method is not implemented yet" });
    }
}

