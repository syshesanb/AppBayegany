using System;
using System.Windows;

namespace BayeganyApp.Windows
{
    public partial class OcrViewPopupWindow : Window
    {
        public string ResultOcrText { get; private set; } = "";
        public bool HasChanges { get; private set; } = false;

        public OcrViewPopupWindow(string ocrText, string? docTitle = null, int pageNumber = 1)
        {
            InitializeComponent();
            TxtOcrFull.Text = ocrText ?? "";
            ResultOcrText = TxtOcrFull.Text;

            if (!string.IsNullOrWhiteSpace(docTitle))
            {
                TxtHeaderSubtitle.Text = $"{docTitle} — برگه شماره {pageNumber}";
            }

            Loaded += (s, e) =>
            {
                try
                {
                    TxtOcrFull.ClearValue(FrameworkElement.HeightProperty);
                    TxtOcrFull.Select(0, 0);
                    TxtOcrFull.ScrollToHome();
                    TxtOcrFull.Focus();
                }
                catch { }
            };
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            string txt = TxtOcrFull.Text;
            if (string.IsNullOrWhiteSpace(txt))
            {
                MessageBox.Show("متنی برای کپی کردن وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Clipboard.SetText(txt);
                MessageBox.Show("متن OCR با موفقیت در کلیپ‌بورد کپی شد.", "کپی موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در کپی کردن متن: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("آیا مطمئن هستید که می‌خواهید کل متن OCR را پاک کنید؟", "تایید حذف", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                TxtOcrFull.Text = "";
            }
        }

        private void BtnSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            ResultOcrText = TxtOcrFull.Text;
            HasChanges = true;
            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            ResultOcrText = TxtOcrFull.Text;
            DialogResult = false;
            Close();
        }
    }
}
