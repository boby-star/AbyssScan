namespace AbyssScan.Core.Interfaces
{
    public interface IHttpRequester
    {
        Task<HttpResponseMessage> SendRequestAsync(string selectedUserAgent, string url, HttpContent? content = null, int? customDelay = null, CancellationToken cancellationToken = default);
    }
}
