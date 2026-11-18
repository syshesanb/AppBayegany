using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class DocumentViewerPopupWindow : Window
    {
        private DocumentItem _doc;
        private readonly string? _ocrQuery;
        private List<DocumentPageItem> _pages = new();

        public DocumentViewerPopupWindow(DocumentItem doc, string? ocrQuery = null)
        {
            InitializeComponent();
            _doc = doc;
            _ocrQuery = ocrQuery;

            Loaded += DocumentViewerPopupWindow_Loaded;
        }

        private void DocumentViewerPopupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDocumentData();
        }

        private void LoadDocumentData()
        {
            TxtHeaderBadge.Text = $"کد بایگانی: {_doc.CodBayegany}";
            TxtCodBayegany.Text = _doc.CodBayegany;
            TxtDocNumber.Text = !string.IsNullOrWhiteSpace(_doc.DocNumber) ? _doc.DocNumber : "-";
            TxtDocDate.Text = !string.IsNullOrWhiteSpace(_doc.DocDate) ? _doc.DocDate : "-";
            TxtDocType.Text = !string.IsNullOrWhiteSpace(_doc.DocType) ? _doc.DocType : "-";
            TxtPageCount.Text = $"{_doc.PageCount} برگه";
            TxtMainPerson.Text = !string.IsNullOrWhiteSpace(_doc.MainPersonName) && _doc.MainPersonName != "-" ? _doc.MainPersonName : "-";
            TxtSubPerson1.Text = !string.IsNullOrWhiteSpace(_doc.SubPerson1Name) && _doc.SubPerson1Name != "-" ? _doc.SubPerson1Name : "-";
            TxtSubPerson2.Text = !string.IsNullOrWhiteSpace(_doc.SubPerson2Name) && _doc.SubPerson2Name != "-" ? _doc.SubPerson2Name : "-";
            TxtSubjectPath.Text = !string.IsNullOrWhiteSpace(_doc.FullSubjectPath) ? _doc.FullSubjectPath : "-";
            TxtNotes.Text = !string.IsNullOrWhiteSpace(_doc.Notes) ? _doc.Notes : "-";

            _pages = DatabaseHelper.GetDocumentPages(_doc.Id);
            ListPagesThumbnails.ItemsSource = _pages;

            if (!string.IsNullOrWhiteSpace(_ocrQuery))
            {
                BorderOcrHighlight.Visibility = Visibility.Visible;
                TxtOcrMatchInfo.Text = $"🔍 عبارت جستجوشده در متن OCR: «{_ocrQuery}»";
            }
            else
            {
                BorderOcrHighlight.Visibility = Visibility.Collapsed;
            }

            if (_pages.Count > 0)
            {
                int targetIdx = 0;
                if (!string.IsNullOrWhiteSpace(_ocrQuery))
                {
                    string normQuery = PersianTextHelper.Normalize(_ocrQuery);
                    int matchIdx = _pages.FindIndex(p =>
                        !string.IsNullOrEmpty(p.PageOcrText) &&
                        PersianTextHelper.Normalize(p.PageOcrText).Contains(normQuery));

                    if (matchIdx >= 0)
                    {
                        targetIdx = matchIdx;
                    }
                }

                ListPagesThumbnails.SelectedIndex = targetIdx;
            }
            else
            {
                DocImageViewer.SetImage(null);
                TxtOcrHeader.Text = "📝 متن استخراج‌شده OCR:";
                TxtOcrContent.Text = !string.IsNullOrWhiteSpace(_doc.FullOcrText)
                    ? _doc.FullOcrText
                    : "هیچ تصویر یا برگه‌ای برای این مدرک ثبت نشده است.";
                TxtStatus.Text = "سند فاقد تصویر است.";
            }
        }

        private void ListPagesThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListPagesThumbnails.SelectedItem is DocumentPageItem page)
            {
                DocImageViewer.SetImage(page.FullImage);

                TxtOcrHeader.Text = $"📝 متن استخراج‌شده OCR (برگه {page.PageNumber} از {_pages.Count}):";
                TxtOcrContent.Text = page.PageOcrText ?? "";

                string imgDimensions = page.FullImage != null
                    ? $"{page.FullImage.PixelWidth} × {page.FullImage.PixelHeight} پیکسل"
                    : "نامشخص";

                TxtStatus.Text = $"برگه {page.PageNumber} از {_pages.Count} | ابعاد تصویر: {imgDimensions} | برای زوم از Ctrl + غلتک ماوس یا دکمه‌های ذره‌بین استفاده نمایید.";
            }
        }

        private void BtnCopyOcr_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtOcrContent.Text))
            {
                Clipboard.SetText(TxtOcrContent.Text);
                MessageBox.Show("متن OCR با موفقیت در کلیپ‌بورد کپی شد.", "کپی شد", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("متنی برای کپی وجود ندارد.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEditDoc_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new DocumentEditWindow(_doc.Id) { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                // بارگذاری مجدد اطلاعات به‌روزشده سند
                var refreshedDocs = DatabaseHelper.GetDocuments(_doc.CodBayegany, null, null, null, null, null, null, null, null, "همه", null);
                var match = refreshedDocs.Find(d => d.Id == _doc.Id);
                if (match != null)
                {
                    _doc = match;
                }
                LoadDocumentData();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
