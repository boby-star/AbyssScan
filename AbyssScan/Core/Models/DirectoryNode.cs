namespace AbyssScan.Core.Models
{
    public class DirectoryNode
    {
        public string Url { get; set; }
        public List<DirectoryNode> Children { get; set; } = new List<DirectoryNode>();

        public IEnumerable<DirectoryNode> GetLeafNodes()
        {
            if (!Children.Any())
            {
                return new[] { this };
            }

            return Children.SelectMany(c => c.GetLeafNodes());
        }
    }
}