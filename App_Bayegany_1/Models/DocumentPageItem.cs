using System.Windows.Media.Imaging;

namespace BayeganyApp.Models
{
    public class DocumentPageItem
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int PageNumber { get; set; } = 1;
        public byte[]? ImageBlob { get; set; }
        public byte[]? ThumbnailBlob { get; set; }
        public string? PageOcrText { get; set; }
        public string? FileName { get; set; }

        // تصاویر آماده نمایش در WPF
        public BitmapSource? ThumbnailImage { get; set; }
        public BitmapSource? FullImage { get; set; }
    }
}
