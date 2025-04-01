using AbyssScan.Core.Interfaces;

namespace AbyssScan.Services
{
    public class UserAgentProvider : IUserAgentProvider
    {
        private readonly string[] _userAgents;
        private readonly ILogger<UserAgentProvider> _logger;

        public UserAgentProvider(string userAgentFilePath, ILogger<UserAgentProvider> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            if (!File.Exists(userAgentFilePath))
            {
                _logger.LogError("User-agent файл не знайдено за шляхом: {Path}", userAgentFilePath);
                throw new FileNotFoundException($"User-Agent файл не знайдено");
            }

            _userAgents = File.ReadAllLines(userAgentFilePath)
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();

            if (_userAgents.Length == 0)
            {
                _logger.LogError("Файл {Path} не містить жодного валідного User-agent", userAgentFilePath);
                throw new InvalidOperationException("Не знайдено жодного агента");
            }

            _logger.LogInformation("User-Agent ініціалізовано в динамічному режимі з {Count} варіантами агентів", _userAgents.Length);
        }

        public async Task<string[]> GetAllUserAgentsAsync()
        {
            return await Task.FromResult(_userAgents);
        }

        public string GetUserAgent(string selectedUserAgent)
        {
            if (string.IsNullOrWhiteSpace(selectedUserAgent))
            {
                _logger.LogError("Статичний User-agent не може бути пустим");
                throw new ArgumentException("Статичний UA не може бути пустим", nameof(selectedUserAgent));
            }

            _logger.LogDebug("Використовується статичний User-Agent: {User-Agent}", selectedUserAgent);
            return selectedUserAgent.Trim();
        }   
    }
}
