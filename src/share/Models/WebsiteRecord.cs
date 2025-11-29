using Pgvector;

namespace Gfd.Models;

public record WebsiteRecord(
    Guid Id,
    string Url,
    string Title,
    string Description,
    Vector TitleMeaning,
    Vector DescriptionMeaning,
    Vector PageMeaning
);
