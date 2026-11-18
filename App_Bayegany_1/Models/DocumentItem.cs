using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace BayeganyApp.Models
{
    public class DocumentItem
    {
        public int Id { get; set; }
        public string CodBayegany { get; set; } = string.Empty;
        public string? DocNumber { get; set; }
        public string? DocDate { get; set; }
        public string? RegDate { get; set; }
        public string? DocType { get; set; }

        public int? MainSubjectId { get; set; }
        public int? SubSubject1Id { get; set; }
        public int? SubSubject2Id { get; set; }

        public int? MainPersonId { get; set; }
        public int? SubPerson1Id { get; set; }
        public int? SubPerson2Id { get; set; }

        public string? Notes { get; set; }
        public string? ColorTag { get; set; }

        // مشخصات استخراج شده از View دیتابیس جهت نمایش سریع در گرید و گزارش‌ها
        public string MainSubjectTitle { get; set; } = "-";
        public string SubSubject1Title { get; set; } = "-";
        public string SubSubject2Title { get; set; } = "-";

        public string MainPersonName { get; set; } = "-";
        public string SubPerson1Name { get; set; } = "-";
        public string SubPerson2Name { get; set; } = "-";

        public int PageCount { get; set; }
        public byte[]? ThumbnailBlob { get; set; }
        public BitmapSource? ThumbnailImage { get; set; }

        public string? FullOcrText { get; set; }

        public string OcrSummary => string.IsNullOrWhiteSpace(FullOcrText) 
            ? "-" 
            : (FullOcrText.Length > 60 
                ? FullOcrText.Substring(0, 60).Replace("\r", " ").Replace("\n", " ") + "..." 
                : FullOcrText.Replace("\r", " ").Replace("\n", " "));

        // لیست کامل برگه‌های این سند
        public List<DocumentPageItem> Pages { get; set; } = new();

        // فیلدهای کمکی نمایش متنی
        public string FullSubjectPath
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(MainSubjectTitle) && MainSubjectTitle != "-") parts.Add(MainSubjectTitle);
                if (!string.IsNullOrWhiteSpace(SubSubject1Title) && SubSubject1Title != "-") parts.Add(SubSubject1Title);
                if (!string.IsNullOrWhiteSpace(SubSubject2Title) && SubSubject2Title != "-") parts.Add(SubSubject2Title);
                return parts.Count > 0 ? string.Join(" > ", parts) : "-";
            }
        }

        public string FullPersonsList
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(MainPersonName) && MainPersonName != "-") parts.Add($"اصلی: {MainPersonName}");
                if (!string.IsNullOrWhiteSpace(SubPerson1Name) && SubPerson1Name != "-") parts.Add($"فرعی ۱: {SubPerson1Name}");
                if (!string.IsNullOrWhiteSpace(SubPerson2Name) && SubPerson2Name != "-") parts.Add($"فرعی ۲: {SubPerson2Name}");
                return parts.Count > 0 ? string.Join(" | ", parts) : "-";
            }
        }
    }
}
