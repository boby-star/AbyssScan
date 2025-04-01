using System.Collections.Concurrent;

namespace AbyssScan.Services
{
    public class InMemoryLogProvider : ILoggerProvider
    {
        private static ConcurrentQueue<string> _logs = new ConcurrentQueue<string>();

        public ILogger CreateLogger(string categoryName)
        {
            return new InMemoryLogger(categoryName);
        }

        public void Dispose() { }

        public static IEnumerable<string> GetLogs()
        {
            return _logs.ToArray();
        }

        internal class InMemoryLogger : ILogger
        {
            private readonly string _categoryName;

            public InMemoryLogger(string categoryName)
            {
                _categoryName = categoryName;
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public IDisposable BeginScope<TState>(TState state) => null;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                var message = $"{DateTime.Now:HH:mm:ss} [{logLevel}]: {formatter(state, exception)}"; //{_categoryName} - видалив з рядкка

                InMemoryLogProvider._logs.Enqueue(message);

                while (InMemoryLogProvider._logs.Count > 5000)
                {
                    InMemoryLogProvider._logs.TryDequeue(out _);
                }
            }
        }
    }
}
