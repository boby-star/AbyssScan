using AbyssScan.Core.Interfaces;
using AbyssScan.Core.Models;
using AbyssScan.Infrastructure;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Mvc;

namespace AbyssScan.Services
{
    public class FormScanner : IFormScanner
    {
        private readonly HtmlParser _htmlParser;
        private readonly ILogger _logger;
        private readonly IHttpRequester _httpRequester;

        public FormScanner(HtmlParser htmlParser, ILogger<FormScanner> logger, IHttpRequester httpRequester)
        {
            _htmlParser = htmlParser ?? throw new ArgumentNullException(nameof(htmlParser));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpRequester = httpRequester ?? throw new ArgumentNullException(nameof(_httpRequester));
        }

        public async Task<IEnumerable<FormFound>> FindFormsAsync(string htmlContent, string baseUrl)
        {
            if (string.IsNullOrEmpty(htmlContent))
            {
                _logger.LogError("HTML контент не може бути пустим");
                throw new ArgumentNullException("HTML content не може бути пустим", nameof(htmlContent));
            }

            // Парсимо HTML
            var htmlDocument = await _htmlParser.ParseDocumentAsync(htmlContent);

            // Знаходимо всі <form>
            var formElements = htmlDocument.QuerySelectorAll("form");
            var forms = new List<FormFound>();

            // Для кожної знайденої форми створюємо наш FormFound
            foreach (var formElement in formElements)
            {
                // Визначаємо Action (повна або відносна адреса)
                var formAction = formElement.GetAttribute("action") ?? string.Empty;
                var absoluteActionUrl = string.IsNullOrEmpty(formAction)
                    ? baseUrl
                    : new Uri(new Uri(baseUrl), formAction).ToString();

                // method="GET" або "POST" (за замовчуванням GET)
                var method = formElement.GetAttribute("method")?.ToUpper() ?? "GET";

                // --- Обробка інпутів (input, textarea, select) ---
                var inputDescriptors = new List<FieldDescriptor>();

                // Відберемо всі елементи, що можуть бути полями введення (включно із select і textarea)
                var inputElements = formElement.QuerySelectorAll("input, textarea, select");
                foreach (var e in inputElements)
                {
                    // "name" або "id" (якщо name не задано)
                    var nameAttr = e.GetAttribute("name") ?? e.GetAttribute("id") ?? "unknown-field";

                    // Type: якщо input не має type, то припускаємо "text". Якщо це textarea => "textarea", select => "select"
                    var tagName = e.TagName.ToLower();
                    var typeAttr = e.GetAttribute("type")?.ToLower()
                                   ?? (tagName == "textarea" ? "textarea"
                                      : tagName == "select" ? "select"
                                      : "text");

                    var isRequired = e.HasAttribute("required");
                    var isHidden = typeAttr == "hidden";
                    var placeholder = e.GetAttribute("placeholder") ?? "";
                    var accept = e.GetAttribute("accept") ?? "";

                    inputDescriptors.Add(new FieldDescriptor
                    {
                        Name = nameAttr,
                        Type = typeAttr,
                        IsRequired = isRequired,
                        IsHidden = isHidden,
                        Placeholder = placeholder,
                        Accept = accept
                    });
                }

                // --- Окремо обробляємо FileFields (input[type=file]) ---
                // Якщо хочемо фізично розділяти, можна окремо зберігати:
                var fileElements = formElement.QuerySelectorAll("input[type=file]");
                var fileDescriptors = new List<FieldDescriptor>();

                foreach (var fe in fileElements)
                {
                    var nameAttr = fe.GetAttribute("name") ?? fe.GetAttribute("id") ?? "unknown-file";
                    var typeAttr = "file";
                    var isRequired = fe.HasAttribute("required");
                    var placeholder = fe.GetAttribute("placeholder") ?? "";
                    var accept = fe.GetAttribute("accept") ?? "";

                    fileDescriptors.Add(new FieldDescriptor
                    {
                        Name = nameAttr,
                        Type = typeAttr,
                        IsRequired = isRequired,
                        IsHidden = false, // файл не буває hidden
                        Placeholder = placeholder,
                        Accept = accept
                    });
                }

                // Створюємо саму форму
                var form = new FormFound
                {
                    Action = absoluteActionUrl,
                    Method = method,
                    InputFields = inputDescriptors,
                    FileFields = fileDescriptors
                };

                forms.Add(form);
            }

            return forms;
        }
    }
}
