# google-for-dummies

Поисковая система для начинающих.

## API Endpoints

### GET /
Главная страница с интерфейсом поиска.

### POST /api/search/search
Выполняет поисковый запрос.

**Request Body:**
```json
{
  "query": "поисковый запрос",
  "pageSize": 10,
  "pageNumber": 1
}
```

**Response:**
```json
{
  "query": "поисковый запрос",
  "results": [
    {
      "title": "Заголовок",
      "url": "https://example.com",
      "snippet": "Описание",
      "relevanceScore": 0.95
    }
  ],
  "totalCount": 100,
  "pageNumber": 1,
  "pageSize": 10,
  "executionTimeMs": 50
}
```
