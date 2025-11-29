public static class Extentions
{
    public static bool IsBaseUrlAndRobotsTxt(this Uri uri)
    {
        if (uri == null)
        {
            return false;
        }

        // 1. Проверяем, является ли путь корневым (базовым)
        // PathAndQuery должен быть "/robots.txt" для базовой версии
        bool isBaseUrl = uri.PathAndQuery.ToLowerInvariant() == "/robots.txt";

        // 2. Проверяем, что имя файла — robots.txt
        bool isRobotsFile = uri.Segments[^1].ToLowerInvariant() == "robots.txt";

        return isBaseUrl && isRobotsFile;
    }
    public static bool IsPlainText(this HttpContent content)
    {
        // Проверяем наличие заголовка Content-Type и его значение
        if (content?.Headers?.ContentType != null)
        {
            // Используем StringComparison.OrdinalIgnoreCase для надежного сравнения без учета регистра
            return string.Equals(content.Headers.ContentType.MediaType, "text/plain", StringComparison.OrdinalIgnoreCase);
        }

        // Если заголовок Content-Type отсутствует, по умолчанию это может быть "application/octet-stream" 
        // или другой тип в зависимости от контекста. В данном случае считаем, что это не text/plain.
        return false;
    }
}