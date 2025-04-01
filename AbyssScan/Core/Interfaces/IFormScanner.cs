using AbyssScan.Core.Models;

namespace AbyssScan.Core.Interfaces
{
    public interface IFormScanner
    {
        Task<IEnumerable<FormFound>> FindFormsAsync(string htmlContent, string baseUrl);
    }
}
