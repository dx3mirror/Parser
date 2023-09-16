using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp.Html.Parser;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System.Web;

class Program
{
    private static Program instance;

    private Program() { }

    public static Program Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new Program();
            }
            return instance;
        }
    }

    class HttpClientFactory
    {
        private static HttpClientFactory instance;

        private HttpClientFactory() { }

        public static HttpClientFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HttpClientFactory();
                }
                return instance;
            }
        }

        public HttpClient CreateClient()
        {
            return new HttpClient();
        }
    }

    class HtmlParserFactory
    {
        private static HtmlParserFactory instance;

        private HtmlParserFactory() { }

        public static HtmlParserFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new HtmlParserFactory();
                }
                return instance;
            }
        }

        public HtmlParser CreateHtmlParser()
        {
            return new HtmlParser();
        }
    }

    class TaskObserver
    {
        private List<Task> tasks;

        public TaskObserver()
        {
            tasks = new List<Task>();
        }

        public Task<T> AddTask<T>(Task<T> task)
        {
            tasks.Add(task);
            return task;
        }

        public async Task WaitForAllTasks()
        {
            await Task.WhenAll(tasks);
        }
    }

    static async Task Main(string[] args)
    {
        Program program = new Program();

        Console.WriteLine("Введите слово для определения:");
        string word = Console.ReadLine();

        // Получаем определение слова из словаря
        string definition = await program.GetDefinitionFromDictionary(word);
        Console.WriteLine(definition);

        // Парсим страницы для определения слова
        List<string> parsedPages = await program.ParsePages(word);
        Console.WriteLine("Найденные страницы:");
        foreach (var page in parsedPages)
        {
            Console.WriteLine(page);
        }
        Console.ReadKey();
    }

    private async Task<string> GetDefinitionFromDictionary(string word)
    {
        // URL для запроса определения
        string url = "https://api.dictionaryapi.dev/api/v2/entries/en/" + word;

        using (var client = HttpClientFactory.Instance.CreateClient())
        {
            // Отправляем GET-запрос и получаем тело ответа
            var response = await client.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();

            // Парсим JSON-ответ и получаем определение
            dynamic result = JsonConvert.DeserializeObject(responseBody);
            string definition = string.Empty;

            if (result != null && result.Count > 0 && result[0].shortdef != null && result[0].shortdef.Count > 0)
            {
                definition = result[0].shortdef[0];
            }

            return definition;
        }
    }

    private async Task<List<string>> ParsePages(string word)
    {
        // URL страницы поисковой выдачи на Google
        string url = "https://www.google.com/search?q=" + HttpUtility.UrlEncode(word);

        using (var httpClient = HttpClientFactory.Instance.CreateClient())
        {
            // Отправляем GET-запрос и получаем тело ответа
            var response = await httpClient.GetAsync(url);
            string responseBody = await response.Content.ReadAsStringAsync();

            // Создаем парсер HTML и загружаем HTML-страницу
            var document = await HtmlParserFactory.Instance.CreateHtmlParser().ParseDocumentAsync(responseBody);

            // Ищем ссылки на найденные страницы
            var links = document.QuerySelectorAll("a")
                .Where(a => a.HasAttribute("href"))
                .Select(a => a.GetAttribute("href"))
                .ToList();

            // Фильтруем только ссылки на HTTP или HTTPS
            var parsedLinks = links
                .Where(link => link.StartsWith("http://") || link.StartsWith("https://"))
                .ToList();

            return parsedLinks;
        }
    }
}

