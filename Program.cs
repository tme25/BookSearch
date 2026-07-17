using System.Text.Json;
using System.Net; // для WebUtility

using var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "MyBookApp (tme.25@uni-dubna.ru)");

Console.WriteLine("Выберите режим поиска:");
Console.WriteLine("1 - по названию книги");
Console.WriteLine("2 - по автору");
string choice = Console.ReadLine();

string query;
string searchUrl;

if (choice == "2")
{
    Console.Write("Введите имя автора: ");
    query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) query = "Tolstoy";
    searchUrl = $"https://openlibrary.org/search.json?author={Uri.EscapeDataString(query)}";
}
else
{
    Console.Write("Введите название книги: ");
    query = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(query)) query = "harry potter";
    searchUrl = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}";
}

var searchResponse = await client.GetAsync(searchUrl);
searchResponse.EnsureSuccessStatusCode();
string searchJson = await searchResponse.Content.ReadAsStringAsync();

using var searchDoc = JsonDocument.Parse(searchJson);
var docs = searchDoc.RootElement.GetProperty("docs");
int total = searchDoc.RootElement.GetProperty("num_found").GetInt32();

Console.WriteLine($"\nНайденные книги (всего {total}):");
int count = 0;
foreach (var book in docs.EnumerateArray())
{
    if (count++ >= 20) break; // выводим 10 книг для наглядности

    string title = book.GetProperty("title").GetString();
    string year = book.TryGetProperty("first_publish_year", out var y)
        ? y.GetInt32().ToString()
        : "не указан";

    string author = book.TryGetProperty("author_name", out var an) && an.GetArrayLength() > 0
        ? an[0].GetString()
        : "автор неизвестен";

    // Декодируем HTML-сущности
    title = WebUtility.HtmlDecode(title);
    author = WebUtility.HtmlDecode(author);

    Console.WriteLine($"- {title} ({year}) — {author}");
}

// --- Второй эндпоинт: информация об авторе первой книги ---
var firstBook = docs.EnumerateArray().FirstOrDefault();
if (firstBook.ValueKind != JsonValueKind.Undefined &&
    firstBook.TryGetProperty("author_key", out var authorKeyArray) &&
    authorKeyArray.GetArrayLength() > 0)
{
    string authorId = authorKeyArray[0].GetString();
    string authorUrl = $"https://openlibrary.org/authors/{authorId}.json";

    var authorResponse = await client.GetAsync(authorUrl);
    if (authorResponse.IsSuccessStatusCode)
    {
        string authorJson = await authorResponse.Content.ReadAsStringAsync();
        using var authorDoc = JsonDocument.Parse(authorJson);
        var authorRoot = authorDoc.RootElement;

        string authorName = authorRoot.GetProperty("name").GetString();
        authorName = WebUtility.HtmlDecode(authorName);

        string birthDate = authorRoot.TryGetProperty("birth_date", out var bd) ? bd.GetString() : null;
        if (birthDate != null)
        {
            birthDate = WebUtility.HtmlDecode(birthDate);

            if (birthDate.Contains(" ["))
                birthDate = birthDate.Substring(0, birthDate.IndexOf(" ["));
        }
            Console.WriteLine($"\n📚 Автор первой книги: {authorName}");
        if (!string.IsNullOrEmpty(birthDate))
            Console.WriteLine($"   Дата рождения: {birthDate}");
        else
            Console.WriteLine("   Дата рождения не указана");
    }
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();