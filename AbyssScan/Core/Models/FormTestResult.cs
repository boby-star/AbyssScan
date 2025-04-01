namespace AbyssScan.Core.Models
{
    public class FormTestResult
    {
        public FormFound Form {  get; set; }
        public string Payload {  get; set; }
        public string Response {  get; set; }
        public System.Net.HttpStatusCode StatusCode { get; set; }
    }
}
