using System.Text.Json;

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

Console.WriteLine("\nНайденные книги:");
int count = 0;
foreach (var book in docs.EnumerateArray())
{
    if (count++ >= 40) break;

    string title = book.GetProperty("title").GetString();
    string firstPublishYear = book.TryGetProperty("first_publish_year", out var yearElem)
        ? yearElem.GetInt32().ToString()
        : "не указан";

    string author = book.TryGetProperty("author_name", out var authorNameArray) && authorNameArray.GetArrayLength() > 0
        ? authorNameArray[0].GetString()
        : "автор неизвестен";

    Console.WriteLine($"- {title} ({firstPublishYear}) — {author}");
}

Console.WriteLine("\nНажмите любую клавишу для выхода...");
Console.ReadKey();