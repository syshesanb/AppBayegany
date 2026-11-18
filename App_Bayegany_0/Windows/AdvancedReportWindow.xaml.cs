using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using BayeganyApp.Controls;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class AdvancedReportWindow : Window
    {
        private List<DocumentItem> _currentReportItems = new();
        private bool _isLoaded = false;
        private int _currentTotalPages = 0;
        private string _currentMinDate = "-";
        private string _currentMaxDate = "-";

        public AdvancedReportWindow()
        {
            InitializeComponent();
            Loaded += AdvancedReportWindow_Loaded;
        }

        private void AdvancedReportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFilterData();
            _isLoaded = true;
            ApplyMatrixFilter();
        }

        private void LoadFilterData()
        {
            // ۱. بارگذاری موضوعات اصلی
            var mainSubjects = DatabaseHelper.GetAllMainSubjects();
            mainSubjects.Insert(0, new MainSubjectItem { Id = 0, Title = "همه موضوعات اصلی" });
            CmbMainSubject.ItemsSource = mainSubjects;
            CmbMainSubject.SelectedIndex = 0;

            // ۲. بارگذاری اشخاص
            var persons = DatabaseHelper.GetAllPersons();
            var pListMain = new List<PersonItem> { new PersonItem { Id = 0, FullName = "همه اشخاص اصلی" } };
            pListMain.AddRange(persons);
            CmbMainPerson.ItemsSource = pListMain;
            CmbMainPerson.SelectedIndex = 0;

            var pListSub1 = new List<PersonItem> { new PersonItem { Id = 0, FullName = "همه اشخاص فرعی ۱" } };
            pListSub1.AddRange(persons);
            CmbSubPerson1.ItemsSource = pListSub1;
            CmbSubPerson1.SelectedIndex = 0;

            var pListSub2 = new List<PersonItem> { new PersonItem { Id = 0, FullName = "همه اشخاص فرعی ۲" } };
            pListSub2.AddRange(persons);
            CmbSubPerson2.ItemsSource = pListSub2;
            CmbSubPerson2.SelectedIndex = 0;

            // ۳. انواع مدرک
            CmbDocType.Items.Clear();
            CmbDocType.Items.Add("همه");
            CmbDocType.Items.Add("قرارداد");
            CmbDocType.Items.Add("نامه اداری");
            CmbDocType.Items.Add("صورتجلسه");
            CmbDocType.Items.Add("سند مالی و فاکتور");
            CmbDocType.Items.Add("مدرک هویتی");
            CmbDocType.Items.Add("سند ملکی و ثبتی");
            CmbDocType.SelectedIndex = 0;
        }

        private void CmbMainSubject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            if (CmbMainSubject.SelectedItem is MainSubjectItem selectedMain && selectedMain.Id > 0)
            {
                var sub1List = DatabaseHelper.GetSubSubjects1(selectedMain.Id);
                sub1List.Insert(0, new SubSubject1Item { Id = 0, Title = "همه موضوعات فرعی ۱" });
                CmbSubSubject1.ItemsSource = sub1List;
                CmbSubSubject1.IsEnabled = true;
                CmbSubSubject1.SelectedIndex = 0;
            }
            else
            {
                CmbSubSubject1.ItemsSource = null;
                CmbSubSubject1.IsEnabled = false;
            }

            CmbSubSubject2.ItemsSource = null;
            CmbSubSubject2.IsEnabled = false;

            ApplyMatrixFilter();
        }

        private void CmbSubSubject1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;

            if (CmbSubSubject1.SelectedItem is SubSubject1Item selectedSub1 && selectedSub1.Id > 0)
            {
                var sub2List = DatabaseHelper.GetSubSubjects2(selectedSub1.Id);
                sub2List.Insert(0, new SubSubject2Item { Id = 0, Title = "همه موضوعات فرعی ۲" });
                CmbSubSubject2.ItemsSource = sub2List;
                CmbSubSubject2.IsEnabled = true;
                CmbSubSubject2.SelectedIndex = 0;
            }
            else
            {
                CmbSubSubject2.ItemsSource = null;
                CmbSubSubject2.IsEnabled = false;
            }

            ApplyMatrixFilter();
        }

        private void CmbSubSubject2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            ApplyMatrixFilter();
        }

        private void FilterInputs_Changed(object sender, EventArgs e)
        {
            if (!_isLoaded) return;
            ApplyMatrixFilter();
        }

        private void ApplyMatrixFilter()
        {
            int? mainSubId = (CmbMainSubject.SelectedItem as MainSubjectItem)?.Id;
            if (mainSubId == 0) mainSubId = null;

            int? sub1Id = (CmbSubSubject1.SelectedItem as SubSubject1Item)?.Id;
            if (sub1Id == 0) sub1Id = null;

            int? sub2Id = (CmbSubSubject2.SelectedItem as SubSubject2Item)?.Id;
            if (sub2Id == 0) sub2Id = null;

            int? mainPersId = (CmbMainPerson.SelectedItem as PersonItem)?.Id;
            if (mainPersId == 0) mainPersId = null;

            int? sub1PersId = (CmbSubPerson1.SelectedItem as PersonItem)?.Id;
            if (sub1PersId == 0) sub1PersId = null;

            int? sub2PersId = (CmbSubPerson2.SelectedItem as PersonItem)?.Id;
            if (sub2PersId == 0) sub2PersId = null;

            string fromDate = TxtFromDate.Text.Trim();
            string toDate = TxtToDate.Text.Trim();
            string docType = CmbDocType.SelectedItem?.ToString() ?? "همه";
            string ocrKey = TxtOcrSearch.Text.Trim();

            var (results, totalPages, minDate, maxDate) = DatabaseHelper.GetMatrixReport(
                mainSubId, sub1Id, sub2Id,
                mainPersId, sub1PersId, sub2PersId,
                null,
                fromDate, toDate, docType, ocrKey);

            _currentReportItems = results;
            _currentTotalPages = totalPages;
            _currentMinDate = string.IsNullOrEmpty(minDate) ? "-" : minDate;
            _currentMaxDate = string.IsNullOrEmpty(maxDate) ? "-" : maxDate;

            GridReportResults.ItemsSource = _currentReportItems;

            // بروزرسانی کارت‌های آماری
            TxtTotalDocs.Text = $"{_currentReportItems.Count} سند";
            TxtTotalPages.Text = $"{_currentTotalPages} برگه";
            TxtMinDate.Text = _currentMinDate;
            TxtMaxDate.Text = _currentMaxDate;
        }

        private void BtnPickFromDate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShamsiCalendarWindow(TxtFromDate.Text) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtFromDate.Text = dlg.SelectedDate;
            }
        }

        private void BtnPickToDate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShamsiCalendarWindow(TxtToDate.Text) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtToDate.Text = dlg.SelectedDate;
            }
        }

        private void BtnClearSubjectFilter_Click(object sender, RoutedEventArgs e)
        {
            CmbMainSubject.SelectedIndex = 0;
        }

        private void BtnClearPersonFilter_Click(object sender, RoutedEventArgs e)
        {
            CmbMainPerson.SelectedIndex = 0;
            CmbSubPerson1.SelectedIndex = 0;
            CmbSubPerson2.SelectedIndex = 0;
        }

        private void BtnResetAllFilters_Click(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            CmbMainSubject.SelectedIndex = 0;
            CmbSubSubject1.ItemsSource = null;
            CmbSubSubject1.IsEnabled = false;
            CmbSubSubject2.ItemsSource = null;
            CmbSubSubject2.IsEnabled = false;

            CmbMainPerson.SelectedIndex = 0;
            CmbSubPerson1.SelectedIndex = 0;
            CmbSubPerson2.SelectedIndex = 0;

            TxtFromDate.Text = "";
            TxtToDate.Text = "";
            CmbDocType.SelectedIndex = 0;
            TxtOcrSearch.Text = "";

            _isLoaded = true;
            ApplyMatrixFilter();
        }

        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReportItems.Count == 0)
            {
                MessageBox.Show("هیچ سندی برای صدور گزارش وجود ندارد.", "اطلاع", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveDlg = new SaveFileDialog
            {
                Title = "ذخیره فایل اکسل گزارش ماتریسی",
                Filter = "فایل اکسل/CSV (*.csv)|*.csv",
                FileName = $"گزارش_بایگانی_اسناد_{DateTime.Now:yyyyMMdd_HHmm}.csv"
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    ReportService.ExportToCsv(_currentReportItems, saveDlg.FileName);
                    var res = MessageBox.Show("فایل اکسل با موفقیت ذخیره شد.\nآیا مایل به باز کردن آن هستید؟", "عملیات موفق", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = saveDlg.FileName,
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ذخیره فایل اکسل:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (_currentReportItems.Count == 0)
            {
                MessageBox.Show("هیچ سندی برای چاپ گزارش وجود ندارد.", "اطلاع", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ساخت توصیف شرایط فیلتر
            var filterParts = new List<string>();

            if (CmbMainSubject.SelectedItem is MainSubjectItem ms && ms.Id > 0)
            {
                filterParts.Add($"موضوع اصلی: {ms.Title}");
                if (CmbSubSubject1.SelectedItem is SubSubject1Item ss1 && ss1.Id > 0)
                {
                    filterParts.Add($"موضوع فرعی ۱: {ss1.Title}");
                    if (CmbSubSubject2.SelectedItem is SubSubject2Item ss2 && ss2.Id > 0)
                        filterParts.Add($"موضوع فرعی ۲: {ss2.Title}");
                }
            }

            if (CmbMainPerson.SelectedItem is PersonItem mp && mp.Id > 0) filterParts.Add($"شخص اصلی: {mp.FullName}");
            if (CmbSubPerson1.SelectedItem is PersonItem sp1 && sp1.Id > 0) filterParts.Add($"شخص فرعی ۱: {sp1.FullName}");
            if (CmbSubPerson2.SelectedItem is PersonItem sp2 && sp2.Id > 0) filterParts.Add($"شخص فرعی ۲: {sp2.FullName}");

            if (!string.IsNullOrWhiteSpace(TxtFromDate.Text)) filterParts.Add($"از تاریخ: {TxtFromDate.Text}");
            if (!string.IsNullOrWhiteSpace(TxtToDate.Text)) filterParts.Add($"تا تاریخ: {TxtToDate.Text}");
            if (CmbDocType.SelectedIndex > 0) filterParts.Add($"نوع مدرک: {CmbDocType.SelectedItem}");
            if (!string.IsNullOrWhiteSpace(TxtOcrSearch.Text)) filterParts.Add($"جستجوی متن: '{TxtOcrSearch.Text}'");

            string filterDesc = filterParts.Count > 0 ? string.Join(" | ", filterParts) : "تمام اسناد بدون اعمال فیلتر";

            string html = ReportService.GenerateHtmlReport(
                _currentReportItems,
                "گزارش جامع و ماتریسی اسناد و مدارک بایگانی",
                filterDesc,
                _currentTotalPages,
                _currentMinDate,
                _currentMaxDate);

            ReportService.OpenHtmlReportInBrowser(html);
        }

        private void GridReportResults_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (GridReportResults.SelectedItem is DocumentItem selectedDoc)
            {
                var editWin = new DocumentEditWindow(selectedDoc.Id) { Owner = this };
                if (editWin.ShowDialog() == true)
                {
                    ApplyMatrixFilter();
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
