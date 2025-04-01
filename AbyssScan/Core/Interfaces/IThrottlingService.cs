namespace AbyssScan.Core.Interfaces
{
    public interface IThrottlingService
    {        
        Task ApplyDelayAsync();
    }
}
