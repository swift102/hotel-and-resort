using Ganss.Xss;
using System.ComponentModel.DataAnnotations;

namespace hotel_and_resort.Models
{
    public class EmailMessage
    {
        private string _subject;
        private string _body;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string To { get; set; }

        [Required]
        [MaxLength(100)]
        public string Subject
        {
            get => _subject;
            set => _subject = Sanitize(value);
        }

        [Required]
        public string Body
        {
            get => _body;
            set => _body = SanitizeHtml(value);
        }

        private static string Sanitize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            var sanitizer = new HtmlSanitizer();
            return sanitizer.Sanitize(input);
        }

        private static string SanitizeHtml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return input;
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Add("p");
            sanitizer.AllowedTags.Add("br");
            sanitizer.AllowedTags.Add("strong");
            sanitizer.AllowedTags.Add("em");
            sanitizer.AllowedTags.Add("h3");
            return sanitizer.Sanitize(input);
        }
    }
}