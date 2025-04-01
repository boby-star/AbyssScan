namespace AbyssScan.Core.Models
{
    public class FormFound //описує знайдену форму на сторінці
    {
        public string Action {  get; set; }
        public string Method { get; set; }
        public List<FieldDescriptor> InputFields { get; set; } = new();
        public List<FieldDescriptor> FileFields { get; set; } = new();

        public IEnumerable<FieldDescriptor> GetAllFields() => InputFields.Concat(FileFields);
    }
}
