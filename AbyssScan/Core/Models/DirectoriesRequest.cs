namespace AbyssScan.Core.Models
{
    public class DirectoriesRequest
    {
        public List<string> Directories { get; set; } = new();
        public int Threads { get; set; } = 1;

        public string ScanType { get; set; }

    }
}
