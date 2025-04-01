using AbyssScan.Core.Interfaces;

namespace AbyssScan.Services
{
    public class ThrottlingService : IThrottlingService
    {
        private readonly int _delay = 2000;

        public ThrottlingService()
        {
        }

        public async Task ApplyDelayAsync()
        {

            await Task.Delay(_delay);
        }
    }
}
