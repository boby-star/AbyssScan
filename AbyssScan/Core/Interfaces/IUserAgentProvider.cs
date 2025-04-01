namespace AbyssScan.Core.Interfaces
{
    public interface IUserAgentProvider
    {
        string GetUserAgent(string selectedUserAgent);
        Task<string[]> GetAllUserAgentsAsync();
    }
}
