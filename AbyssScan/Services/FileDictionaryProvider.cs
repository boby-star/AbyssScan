using AbyssScan.Core.Interfaces;

namespace AbyssScan.Services
{
    public class FileDictionaryProvider : IDictionaryProvider
    {
        private string _currenyDictionaryPath;
        private readonly ILogger<FileDictionaryProvider> _logger;
        private readonly Dictionary<string, string> _fuzzingDictionaries;

        public FileDictionaryProvider(IConfiguration configuration, ILogger<FileDictionaryProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _fuzzingDictionaries = configuration.GetSection("Dictionaries:Fuzzing").Get<Dictionary<string, string>>()
                ?? throw new Exception("Словники для фазингу відсутні");

            _currenyDictionaryPath = _fuzzingDictionaries.First().Value;
            ValidateFileExists(_currenyDictionaryPath);
        }

        public IEnumerable<string> GetAvailableDictionaries()
        {
            _logger.LogDebug("Отримання списку доступних словників");
            return _fuzzingDictionaries.Keys;
        }

        public IEnumerable<string> GetFuzzingDictionaries()
        {
            _logger.LogDebug("Отримання списку словників для фаззингу...");
            return _fuzzingDictionaries.Keys;
        }

        public void SwitchDictionary(string dictionaryName)
        {
            if (!_fuzzingDictionaries.TryGetValue(dictionaryName, out var dictionaryPath))
            {
                _logger.LogWarning("Словник з назвою {Name} не знайдений!", dictionaryName);
            }

            _currenyDictionaryPath = dictionaryPath;

            ValidateFileExists(_currenyDictionaryPath);
        }

        public async Task<IEnumerable<string>> GetEntriesAsync()
        {
            _logger.LogDebug("Читання словника з {Path}", _currenyDictionaryPath);
            var lines = await File.ReadAllLinesAsync(_currenyDictionaryPath);
            return lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Select(line => line.Trim());
        }

        private void ValidateFileExists(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogError("Файл словника {Path} не існує", filePath);
                throw new FileNotFoundException($"Файл словника {filePath} не існує");
            }
                
        }
    }
}
