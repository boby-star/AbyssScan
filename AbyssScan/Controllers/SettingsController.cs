using AbyssScan.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace AbyssScan.Controllers
{
    public class SettingsController : Controller
    {
        private readonly IDictionaryProvider _dictionaryProvider;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(IDictionaryProvider dictionaryProvider, ILogger<SettingsController> logger)
        {
            _dictionaryProvider = dictionaryProvider;
            _logger = logger;
        }

        public IActionResult Index()
        {
            var fuzzingDictionaries = _dictionaryProvider.GetFuzzingDictionaries();
            return View(fuzzingDictionaries);
        }

        [HttpGet] 
        public async Task<IActionResult> GetDictionaryContent(string dictName)
        {
            try
            {
                _dictionaryProvider.SwitchDictionary(dictName);
                var entries = (await _dictionaryProvider.GetEntriesAsync()).ToList();
                return Json(new { success = true, entries = entries });
            }
            catch (Exception ex)
            {
                _logger.LogError("Помилка при виборі словника {Name}: {Message}.", dictName, ex.Message);
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult ShowDictionary(string dictName)
        {
            try
            {
                _dictionaryProvider.SwitchDictionary(dictName);
                var entries = _dictionaryProvider.GetEntriesAsync();
                ViewBag.DictionaryName = dictName;
                return View("ShowDictionary", entries);
            }
            catch (Exception ex)
            {
                _logger.LogError("Помилка при виборі словника {Name}: {Message}", dictName, ex.Message);
                ModelState.AddModelError("", ex.Message);
                return RedirectToAction("Index");
            }
        }
    }
}
