using AbyssScan.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using AbyssScan.Core.Models;

namespace AbyssScan.Controllers
{
    public class ActionsController : Controller
    {
        private readonly IFormScanner _formScanner;
        private readonly ILogger<ActionsController> _logger;

        public ActionsController(IFormScanner formScanner, ILogger<ActionsController> logger)
        {
            _formScanner = formScanner;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (TempData["SelectedDirectories"] is string dirsRaw)
            {
                var dirs = dirsRaw.Split(';').ToList();
                return View(dirs);
            }

            return View(new List<string>());
        }

        [HttpPost]
        public IActionResult ProcessSelectedDirectories([FromBody] DirectoriesRequest request)
        {
            if (request.Directories is null || !request.Directories.Any())
            {
                return BadRequest(new { error = "Не передано жодної директорії." });
            }

            TempData["SelectedDirectories"] = string.Join(";", request.Directories);
            TempData["Threads"] = request.Threads;

            _logger.LogInformation("Отримано {Count} директорій для подальшого сканування. Перейдіть у вкладку 'Дії над директоріями' для подальших дій", request.Directories.Count);

            return Json(new
            {
                message = "Директорії успішно отримано",
                count = request.Directories.Count,
                threads = request.Threads
            });
        }

        [HttpPost("/Actions/ScanDirectories")]
        public async Task<IActionResult> ScanDirectories([FromBody] DirectoriesRequest request)
        {
            if (request == null)
            {
                return BadRequest(new { error = "Запит не може бути порожнім" });
            }

            if (request.Directories == null || !request.Directories.Any())
            {
                return BadRequest(new { error = "Не передано жодної директорії" });
            }

            if (request.ScanType == null || !request.ScanType.Any())
            {
                return BadRequest(new { error = "Не обрано типу сканування" });
            }

            if (request.Threads <= 0)
            {
                return BadRequest(new { error = "Кількість потоків повинна бути більша нуля" });
            }

            var results = new List<object>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = request.Threads };

            try
            {
                await Parallel.ForEachAsync(request.Directories, options, async (directory, cancellationToken) =>
                {
                    var content = await FetchPageContentAsync(directory);

                    try
                    {
                            var forms = await _formScanner.FindFormsAsync(content, directory);
                            lock (results)
                            {
                                results.AddRange(forms.Select(form => new
                                {
                                    Source = directory,
                                    ScanType = "forms",
                                    Action = form.Action,
                                    Method = form.Method,
                                    InputFields = form.InputFields,
                                    FileFields = form.FileFields
                                }));
                            }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка під час сканування директорії {Directory}", directory);
                    }
                });

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час обробки запиту");
                return StatusCode(500, new { error = "Помилка під час обробки запиту" });
            }
        }

        private async Task<string> FetchPageContentAsync(string url)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Не вдалось отримати контент сторінки {Url}", url);
                return string.Empty;
            }
        }
    }
}
