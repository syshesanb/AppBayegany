namespace BayeganyApp.Models
{
    public class MainSubjectItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Title : $"{Title} ({Code})";
    }

    public class SubSubject1Item
    {
        public int Id { get; set; }
        public int MainSubjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Title : $"{Title} ({Code})";
    }

    public class SubSubject2Item
    {
        public int Id { get; set; }
        public int SubSubject1Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Code { get; set; }

        public override string ToString() => string.IsNullOrWhiteSpace(Code) ? Title : $"{Title} ({Code})";
    }
}
