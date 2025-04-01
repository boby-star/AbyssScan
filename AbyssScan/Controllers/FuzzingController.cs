using System.Diagnostics;
using AbyssScan.Models;
using Microsoft.AspNetCore.Mvc;
using AbyssScan.Core.Interfaces;
using AbyssScan.Services;
using System.Text;
using AbyssScan.Core.Models;
using System.Text.RegularExpressions;

namespace AbyssScan.Controllers
{
    public class FuzzingController : Controller
    {
        private readonly ILogger<FuzzingController> _logger;
        private readonly IThrottlingService _throttlingService;
        private readonly IDirectoryScanner _directoryScanner;
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly IUserAgentProvider _userAgentProvider;
        private readonly IHttpRequester _httpRequester;
        private static CancellationTokenSource _cts = new();
        private static string _baseUrl = "";

        public FuzzingController(IHttpRequester httpRequester, ILogger<FuzzingController> logger, IThrottlingService throttlingService, IDirectoryScanner directoryScanner, IDictionaryProvider dictionaryProvider, IUserAgentProvider userAgentProvider)
        {
            _logger = logger;
            _throttlingService = throttlingService;
            _directoryScanner = directoryScanner;
            _dictionaryProvider = dictionaryProvider;
            _userAgentProvider = userAgentProvider;
            _httpRequester = httpRequester;
        }

        public async Task<IActionResult> Index()
        {
            var directories = _dictionaryProvider.GetAvailableDictionaries();

            var allUserAgents = await _userAgentProvider.GetAllUserAgentsAsync();

            ViewBag.Dictionaries = directories;
            ViewBag.UserAgents = allUserAgents;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> StartFuzzing(string selectedUserAgent, string baseUrl, string dictionaryName, int depth, int delay, int threads)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                _logger.LogError("Ціль потрібно вказати обов'язково");
                return BadRequest("Ціль потрібно вказати обов'язково");
            }

            if (!Regex.IsMatch(baseUrl, @"^(https?:\/\/).+"))
            {
                _logger.LogError("Введіть коректний URL, який починається з http:// або https://");
                return BadRequest("Введіть коректний URL, який починається з http:// або https://");
            }

            if (threads < 1) threads = 1;
            if (threads > 100) threads = 100;

            _cts = new CancellationTokenSource();
            var cancellationToken = _cts.Token;

            _baseUrl = baseUrl;

            _logger.LogInformation("Початок перебору BaseUrl={BaseUrl}, Dictionary={Dictionary}, Depth={Depth}, Delay={Delay}, Threads={Threads}",
                baseUrl, dictionaryName, depth, delay, threads);

            _dictionaryProvider.SwitchDictionary(dictionaryName);

            if (_directoryScanner is DirectoryScanner ds) {
                ds.Threads = threads;
            }

            var userAgent = _userAgentProvider.GetUserAgent(selectedUserAgent);

            var throttlingService = new ThrottlingService();

            var paths = await _dictionaryProvider.GetEntriesAsync();

            try
            {
                if (!string.IsNullOrEmpty(userAgent) && _httpRequester is HttpRequester  hr)
                {
                    hr.FixedUserAgent = userAgent;
                }

                var result = await _directoryScanner.ScanAsync(selectedUserAgent, baseUrl, paths, depth, cancellationToken, throttlingService);

                var found = result.Distinct().ToList();

                if (!found.Any())
                {
                    _logger.LogWarning("Жодної директорії не знайдено");
                    return Json(new
                    {
                        message = "Сканування завершено. Директорії не знайдено",
                        found = new List<string>()
                    });
                }

                var msg = $"Перебір директорій закінчений. Знайдено: {found.Count} директорій";
                _logger.LogInformation(msg);

                return Json(new
                {
                    message = msg,
                    found = found
                });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Сканування було зупинено користувачем.");
                return Json(new
                {
                    message = "Сканування зупинено користувачем.",
                    found = new List<string>()
                });
            }
        }


        [HttpPost]
        public IActionResult StopFuzzing()
        {
            _cts.Cancel();
            _logger.LogInformation("Користувач зупинив сканування");

            var found = _directoryScanner.GetFoundDirectories().Distinct().ToList();

            var msg = $"Сканування зупинено. Знайдено: {found.Count} директорій";

            return Json(new
            {
                message = msg,
                found = found
            });
        }

        [HttpGet]
        public IActionResult ShowFoundDirectories()
        {
            var found = _directoryScanner.GetFoundDirectories().Distinct().ToList();

            return View(found);
        }

        [HttpGet]
        public IActionResult GetLogs()
        {
            var logs = InMemoryLogProvider.GetLogs();
            return Json(new { logs = logs });
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
