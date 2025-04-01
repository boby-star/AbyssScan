namespace AbyssScan.Core.Models
{
    public class FieldDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired {  get; set; }
        public bool IsHidden { get; set; }
        public string Placeholder { get; set; }
        public string Accept { get; set; }
    }
}
