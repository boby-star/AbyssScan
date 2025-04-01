using AbyssScan.Core.Interfaces;
using AbyssScan.Core.Models;
using Microsoft.AspNetCore.Rewrite;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;

namespace AbyssScan.Services
{
    public class DirectoryScanner : IDirectoryScanner
    {
        private readonly IHttpRequester _httpRequester;
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly ILogger<DirectoryScanner> _logger;
        private readonly IEnumerable<HttpStatusCode> _successfulCodes;
        private ConcurrentBag<string> _foundDirectories = new ConcurrentBag<string>();
        private ConcurrentDictionary<string, bool> _visited = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        public int Threads { get; set; } = 3;
        private readonly int _maxDepth;
        private readonly IEnumerable<HttpStatusCode> _successfulCode;

        public DirectoryScanner(
            IHttpRequester httpRequester,
            IDictionaryProvider dictionaryProvider,
            ILogger<DirectoryScanner> logger,
            int maxDegreeOfParallelism = 3,
            int maxDepth = 1,
            IEnumerable<HttpStatusCode>? successfulCodes = null)
        {
            _httpRequester = httpRequester ?? throw new ArgumentNullException(nameof(httpRequester));
            _dictionaryProvider = dictionaryProvider ?? throw new ArgumentNullException(nameof(dictionaryProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.Threads = maxDegreeOfParallelism;
            _maxDepth = maxDepth;
            _successfulCodes = successfulCodes ?? new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.Forbidden,
                HttpStatusCode.Unauthorized,
                HttpStatusCode.MovedPermanently,
                HttpStatusCode.Found
            };
        }

        public async Task<IEnumerable<string>> ScanAsync(string selectedUserAgent, string baseUrl, IEnumerable<string> paths, int depth,CancellationToken cancellationToken, IThrottlingService throttlingService) 
        {
            return await ScanInternalAsync(selectedUserAgent, baseUrl, paths, depth, cancellationToken);
        }

        public async Task<IEnumerable<string>> ScanAsync(string selectedUserAgent, string baseUrl, int depth, CancellationToken cancellationToken, IThrottlingService throttlingService)
        {
            var paths = (await _dictionaryProvider.GetEntriesAsync());
            return await ScanInternalAsync(selectedUserAgent, baseUrl, paths, depth, cancellationToken);
        }

        private async Task<IEnumerable<string>> ScanInternalAsync(string selectedUserAgent,string baseUrl, IEnumerable<string> paths, int maxDepth, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL не може бути пустим", nameof(baseUrl));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                throw new ArgumentException("Некоректний формат Base URL", nameof(baseUrl));

            _logger.LogInformation("Починаємо сканування директорій для {BaseUrl}...", baseUrl);

            _visited.Clear();
            _foundDirectories = new ConcurrentBag<string>();
            _visited.TryAdd(baseUrl, true);

            var urls = paths
                .Select(p => new Uri(baseUri, p).ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            _visited.TryAdd(baseUrl, true);

            var semaphore = new SemaphoreSlim(Threads, Threads);
            var tasks = new List<Task>();

            foreach (var url in urls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ProcessUrlAsync(selectedUserAgent, baseUrl, url, baseUri, 1, maxDepth, paths, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            var result = _foundDirectories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return result;
        }

        private async Task ProcessUrlAsync(string selectedUserAgent,string baseUrl, string url, Uri baseUri, int currentDepth, int maxDepth, IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            if (!_visited.TryAdd(url, true))
            {
                return;
            }

            if (maxDepth > 0 && currentDepth > maxDepth)
            {
                _logger.LogInformation("Досягнуто максимальну глибину для URL: {Url}", url);
                return;
            }

            if (maxDepth <= 0)
            {
                _logger.LogInformation("Глибина повина бути більша нуля");
                return;
            }

            _logger.LogInformation("Запит до: {Url} (глибина: {Depth})", url, currentDepth);

            cancellationToken.ThrowIfCancellationRequested();
            var response = await _httpRequester.SendRequestAsync(selectedUserAgent, url, null, null, cancellationToken);

            if (IsSuccessfulStatus(response.StatusCode))
            {
                _foundDirectories.Add(url);
                _logger.LogInformation("Додано: {url} зі статусом {StatusCode}", url, (int)response.StatusCode);

                if (!url.EndsWith("/"))
                {
                    url += "/";
                }              

                foreach (var path in paths)
                {
                    var newUri = new Uri(new Uri(url), path).ToString();
                    await ProcessUrlAsync(selectedUserAgent, baseUrl, newUri, baseUri, currentDepth + 1, maxDepth, paths, cancellationToken);
                }
            }
            else
            {
                _logger.LogInformation("URL {Url} повернув {StatusCode}, пропускаємо...", url, (int)response.StatusCode);
            }
        }

        private bool IsSuccessfulStatus(HttpStatusCode statusCode)
        {
            return statusCode == HttpStatusCode.OK ||                // 200: Успішний запит
                   statusCode == HttpStatusCode.Unauthorized ||      // 401: Потрібна аутентифікація
                   statusCode == HttpStatusCode.Forbidden ||         // 403: Доступ заборонено
                   statusCode == HttpStatusCode.MovedPermanently ||  // 301: Постійне перенаправлення
                   statusCode == HttpStatusCode.Found ||             // 302: Тимчасове перенаправлення
                   statusCode == HttpStatusCode.SeeOther ||          // 303: Альтернативний ресурс
                   statusCode == HttpStatusCode.NotModified ||       // 304: Ресурс не змінювався
                   statusCode == HttpStatusCode.TemporaryRedirect || // 307: Тимчасове перенаправлення
                   statusCode == HttpStatusCode.PermanentRedirect;   // 308: Постійне перенаправлення
        }

        public IEnumerable<string> GetFoundDirectories() => _foundDirectories;
    }
}
