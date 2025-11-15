using System.ComponentModel.DataAnnotations;

namespace Api_server.Models;

public class SearchRequest
{
    [Required(ErrorMessage = "Search query is required")]
    [MinLength(1, ErrorMessage = "Search query cannot be empty")]
    public string Query { get; set; } = string.Empty;

    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
    public int PageNumber { get; set; } = 1;
}

