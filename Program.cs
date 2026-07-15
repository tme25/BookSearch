using System.Text.Json;

// Создание HTTP-клиента
using var client = new HttpClient();
// User-Agent обязателен для OpenLibrary
client.DefaultRequestHeaders.Add("User-Agent", "MyBookApp (tme.25@uni-dubna.ru)");

// Запрос названия книги у пользователя
Console.Write("Введите название книги: ");
string bookname = Console.ReadLine();
// Если ввод пустой, используется значение по умолчанию
if (string.IsNullOrWhiteSpace(bookname))
    bookname = "harry potter";

// Формирование URL для поиска
string searchUrl = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(bookname)}";

// Запрос к API поиска
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
    // Год первой публикации (может отсутствовать)
    string firstPublishYear = book.TryGetProperty("first_publish_year", out var yearElem)
        ? yearElem.GetInt32().ToString()
        : "не указан";

    Console.WriteLine($"  - {title} ({firstPublishYear})");
}

// Информация об авторе первой книги
var firstBook = docs.EnumerateArray().FirstOrDefault();

if (firstBook.ValueKind != JsonValueKind.Undefined)
{
    if (firstBook.TryGetProperty("author_key", out var authorKeyArray) &&
        authorKeyArray.GetArrayLength() > 0)
    {
        string authorId = authorKeyArray[0].GetString();

        string authorUrl = $"https://openlibrary.org/authors/{authorId}.json";

        var authorResponse = await client.GetAsync(authorUrl);
        authorResponse.EnsureSuccessStatusCode();
        string authorJson = await authorResponse.Content.ReadAsStringAsync();

        using var authorDoc = JsonDocument.Parse(authorJson);
        var authorRoot = authorDoc.RootElement;

        string authorName = authorRoot.GetProperty("name").GetString();
        string birthDate = authorRoot.TryGetProperty("birth_date", out var birthElem)
            ? birthElem.GetString()
            : "неизвестно";

        Console.WriteLine($"\nАвтор первой книги: {authorName}");
        Console.WriteLine($"Дата рождения: {birthDate}");
    }
    else
    {
        Console.WriteLine("\nУ первой книги не указан автор.");
    }
}
else
{
    Console.WriteLine("\nКниги не найдены.");
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();