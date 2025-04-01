using AbyssScan.Core.Interfaces;

namespace AbyssScan.Services
{
    public class HttpRequester : IHttpRequester
    {        
        private readonly HttpClient _httpClient;
        private readonly IThrottlingService _throttlingService;
        private readonly IUserAgentProvider _userAgentProvider;

        public HttpRequester(HttpClient httpClient, IUserAgentProvider userAgentProvider,  IThrottlingService throttlingService)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _userAgentProvider = userAgentProvider ?? throw new ArgumentNullException(nameof(userAgentProvider));
            _throttlingService = throttlingService ?? throw new ArgumentNullException(nameof(throttlingService));
        }

        public string FixedUserAgent { get; set; }

        public async Task<HttpResponseMessage> SendRequestAsync(string selectedUserAgent, string url, HttpContent content = null, int? customDelay = null, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url)) 
                throw new ArgumentNullException("URL не може бути порожнім", nameof(url));

            if (customDelay.HasValue && customDelay.Value >= 0)
            {
                await Task.Delay(customDelay.Value, cancellationToken);
            }
            else
            {
                await _throttlingService.ApplyDelayAsync();
            }

            var userAgent = _userAgentProvider.GetUserAgent(selectedUserAgent); //налаштування юзер-агента

            var request = new HttpRequestMessage(content == null ? HttpMethod.Get : HttpMethod.Post, url)
            {
                Content = content
            };

            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(userAgent);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            return response;
        }
    }
}
