using AngleSharp;
using AngleSharp.Dom;

namespace AbyssScan.Infrastructure
{
    public class HtmlParser
    {
        private readonly IBrowsingContext _browsingContext;

        public HtmlParser()
        {
            var config = Configuration.Default.WithDefaultLoader();
            _browsingContext = BrowsingContext.New(config);
        }

        public async Task<IDocument> ParseDocumentAsync(string htmlContent)
        {
            if (string.IsNullOrWhiteSpace(htmlContent))
                throw new ArgumentException("HTML не може бути пустим", nameof(htmlContent));

            return await _browsingContext.OpenAsync(req => req.Content(htmlContent));
        }
    }
}
