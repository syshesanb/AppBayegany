namespace BayeganyApp.Models
{
    public class PersonItem
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PersonType { get; set; } // حقیقی، حقوقی دولتی، حقوقی خصوصی، همکار
        public string? NationalCode { get; set; }
        public string? ContactInfo { get; set; }
        public string? Notes { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(PersonType) ? FullName : $"{FullName} ({PersonType})";
    }
}
