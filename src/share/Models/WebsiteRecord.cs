namespace Gfd.Models;

public record WebsiteRecord(
    Guid Id,
    string Url,
    string Title,
    string Description,
    float[] TitleMeaning,
    float[] DescriptionMeaning,
    float[] PageMeaning
);