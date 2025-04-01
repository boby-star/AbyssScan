namespace AbyssScan.Core.Interfaces
{
    public interface IDictionaryProvider
    {
        Task<IEnumerable<string>> GetEntriesAsync();
        IEnumerable<string> GetAvailableDictionaries();
        IEnumerable<string> GetFuzzingDictionaries();
        public void SwitchDictionary(string dictionaryName);
    }
}
