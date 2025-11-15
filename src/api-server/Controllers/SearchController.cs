using Microsoft.AspNetCore.Mvc;
using Api_server.Models;

namespace Api_server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SearchController : ControllerBase
{
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

