using System.Text.Json;

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "MyBookApp (tme.25@uni-dubna.ru)");

Console.Write("Введите название книги: ");
string bookname = Console.ReadLine();
if (string.IsNullOrWhiteSpace(bookname))
    bookname = "harry potter";

string searchUrl = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(bookname)}";

var searchResponse = await client.GetAsync(searchUrl);
searchResponse.EnsureSuccessStatusCode();
string searchJson = await searchResponse.Content.ReadAsStringAsync();

using var searchDoc = JsonDocument.Parse(searchJson);
var docs = searchDoc.RootElement.GetProperty("docs");

Console.WriteLine("\nНайденные книги:");
int count = 0;
foreach (var book in docs.EnumerateArray())
{
    if (count++ >= 3) break;

    string title = book.GetProperty("title").GetString();
    string firstPublishYear = book.TryGetProperty("first_publish_year", out var yearElem)
        ? yearElem.GetInt32().ToString()
        : "не указан";

    // Извлекаем имя автора (берём первого из массива author_name)
    string author = book.TryGetProperty("author_name", out var authorNameArray) && authorNameArray.GetArrayLength() > 0
        ? authorNameArray[0].GetString()
        : "автор неизвестен";

    Console.WriteLine($"- {title} ({firstPublishYear}) — {author}");
}

// Второй эндпоинт (информация об авторе) – делаем запрос, но не выводим дату рождения.
// Используем его для получения, например, общего количества книг автора (work_count).
var firstBook = docs.EnumerateArray().FirstOrDefault();
if (firstBook.ValueKind != JsonValueKind.Undefined &&
    firstBook.TryGetProperty("author_key", out var authorKeyArray) &&
    authorKeyArray.GetArrayLength() > 0)
{
    string authorId = authorKeyArray[0].GetString();
    string authorUrl = $"https://openlibrary.org/authors/{authorId}.json";

    var authorResponse = await client.GetAsync(authorUrl);
    authorResponse.EnsureSuccessStatusCode();
    string authorJson = await authorResponse.Content.ReadAsStringAsync();

    using var authorDoc = JsonDocument.Parse(authorJson);
    var authorRoot = authorDoc.RootElement;

    // Дата рождения убрана — не выводится.
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();