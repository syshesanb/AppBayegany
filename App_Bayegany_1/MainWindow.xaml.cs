using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using BayeganyApp.Controls;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;
using BayeganyApp.Windows;

namespace BayeganyApp
{
    public partial class MainWindow : Window
    {
        private List<DocumentItem> _allDocuments = new();
        private DocumentItem? _selectedDocument = null;
        private bool _isInitialized = false;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadFiltersData();
            _isInitialized = true;
            RefreshDocumentsList();
        }

        private void LoadFiltersData()
        {
            // ۱. موضوعات اصلی
            var mainSubs = DatabaseHelper.GetAllMainSubjects();
            mainSubs.Insert(0, new MainSubjectItem { Id = 0, Title = "همه موضوعات اصلی" });
            CmbFilterMainSubject.ItemsSource = mainSubs;
            CmbFilterMainSubject.SelectedIndex = 0;

            // ۲. اشخاص
            var persons = DatabaseHelper.GetAllPersons();
            var pList = new List<PersonItem> { new PersonItem { Id = 0, FullName = "همه اشخاص / شرکت‌ها" } };
            pList.AddRange(persons);
            CmbFilterPerson.ItemsSource = pList;
            CmbFilterPerson.SelectedIndex = 0;

            // ۳. نوع مدرک
            CmbFilterDocType.Items.Clear();
            CmbFilterDocType.Items.Add("همه انواع مدرک");
            CmbFilterDocType.Items.Add("قرارداد");
            CmbFilterDocType.Items.Add("نامه اداری");
            CmbFilterDocType.Items.Add("صورتجلسه");
            CmbFilterDocType.Items.Add("سند مالی و فاکتور");
            CmbFilterDocType.Items.Add("مدرک هویتی");
            CmbFilterDocType.Items.Add("سند ملکی و ثبتی");
            CmbFilterDocType.SelectedIndex = 0;
        }

        private void CmbFilterMainSubject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (CmbFilterMainSubject.SelectedItem is MainSubjectItem selectedMain && selectedMain.Id > 0)
            {
                var sub1List = DatabaseHelper.GetSubSubjects1(selectedMain.Id);
                sub1List.Insert(0, new SubSubject1Item { Id = 0, Title = "همه موضوعات فرعی ۱" });
                CmbFilterSubSubject1.ItemsSource = sub1List;
                CmbFilterSubSubject1.IsEnabled = true;
                CmbFilterSubSubject1.SelectedIndex = 0;
            }
            else
            {
                CmbFilterSubSubject1.ItemsSource = null;
                CmbFilterSubSubject1.IsEnabled = false;
            }

            CmbFilterSubSubject2.ItemsSource = null;
            CmbFilterSubSubject2.IsEnabled = false;

            RefreshDocumentsList();
        }

        private void CmbFilterSubSubject1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;

            if (CmbFilterSubSubject1.SelectedItem is SubSubject1Item selectedSub1 && selectedSub1.Id > 0)
            {
                var sub2List = DatabaseHelper.GetSubSubjects2(selectedSub1.Id);
                sub2List.Insert(0, new SubSubject2Item { Id = 0, Title = "همه موضوعات فرعی ۲" });
                CmbFilterSubSubject2.ItemsSource = sub2List;
                CmbFilterSubSubject2.IsEnabled = true;
                CmbFilterSubSubject2.SelectedIndex = 0;
            }
            else
            {
                CmbFilterSubSubject2.ItemsSource = null;
                CmbFilterSubSubject2.IsEnabled = false;
            }

            RefreshDocumentsList();
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            if (!_isInitialized) return;
            RefreshDocumentsList();
        }

        private void FilterTextBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // جلوگیری از کلیک هدر جدول برای مرتب‌سازی هنگام فوکوس روی تکست‌باکس
            if (sender is TextBox tb)
            {
                tb.Focus();
                e.Handled = true;
            }
        }

        private void ColFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            ApplyGridFilters();
        }

        private void ApplyGridFilters()
        {
            if (_allDocuments == null) return;

            string fCode = PersianTextHelper.Normalize(ColFilterCodBayegany?.Text);
            string fNum = PersianTextHelper.Normalize(ColFilterDocNumber?.Text);
            string fDate = PersianTextHelper.Normalize(ColFilterDocDate?.Text);
            string fType = PersianTextHelper.Normalize(ColFilterDocType?.Text);
            string fSub = PersianTextHelper.Normalize(ColFilterSubject?.Text);
            string fPer = PersianTextHelper.Normalize(ColFilterPerson?.Text);
            string fPage = PersianTextHelper.Normalize(ColFilterPageCount?.Text);
            string fNotes = PersianTextHelper.Normalize(ColFilterNotes?.Text);
            string fOcr = PersianTextHelper.Normalize(ColFilterOcr?.Text);
            string topOcr = PersianTextHelper.Normalize(TxtFilterOcr?.Text);
            string ocrFilter = !string.IsNullOrEmpty(fOcr) ? fOcr : topOcr;

            var filtered = _allDocuments.Where(d =>
                (string.IsNullOrEmpty(fCode) || PersianTextHelper.Normalize(d.CodBayegany).Contains(fCode)) &&
                (string.IsNullOrEmpty(fNum) || PersianTextHelper.Normalize(d.DocNumber).Contains(fNum)) &&
                (string.IsNullOrEmpty(fDate) || PersianTextHelper.Normalize(d.DocDate).Contains(fDate)) &&
                (string.IsNullOrEmpty(fType) || PersianTextHelper.Normalize(d.DocType).Contains(fType)) &&
                (string.IsNullOrEmpty(fSub) || PersianTextHelper.Normalize(d.FullSubjectPath).Contains(fSub)) &&
                (string.IsNullOrEmpty(fPer) || PersianTextHelper.Normalize(d.MainPersonName).Contains(fPer)) &&
                (string.IsNullOrEmpty(fPage) || PersianTextHelper.Normalize(d.PageCount.ToString()).Contains(fPage)) &&
                (string.IsNullOrEmpty(fNotes) || PersianTextHelper.Normalize(d.Notes).Contains(fNotes)) &&
                (string.IsNullOrEmpty(ocrFilter) || PersianTextHelper.Normalize(d.FullOcrText).Contains(ocrFilter))
            ).ToList();

            GridDocuments.ItemsSource = filtered;
            TxtDocCountHeader.Text = $"فهرست اسناد بایگانی (نمایش {filtered.Count} از {_allDocuments.Count} سند)";
            TxtStatusBottom.Text = $"تعداد {filtered.Count} سند با اعمال فیلترها نمایش داده شد.";

            if (filtered.Count > 0)
            {
                if (_selectedDocument == null || !filtered.Contains(_selectedDocument))
                {
                    GridDocuments.SelectedIndex = 0;
                }
            }
        }

        private void RefreshDocumentsList()
        {
            string search = TxtSearchBox.Text.Trim();
            string topOcr = TxtFilterOcr?.Text.Trim() ?? "";

            int? mainSubId = (CmbFilterMainSubject.SelectedItem as MainSubjectItem)?.Id;
            if (mainSubId == 0) mainSubId = null;

            int? sub1Id = (CmbFilterSubSubject1.SelectedItem as SubSubject1Item)?.Id;
            if (sub1Id == 0) sub1Id = null;

            int? sub2Id = (CmbFilterSubSubject2.SelectedItem as SubSubject2Item)?.Id;
            if (sub2Id == 0) sub2Id = null;

            int? personId = (CmbFilterPerson.SelectedItem as PersonItem)?.Id;
            if (personId == 0) personId = null;

            string docType = CmbFilterDocType.SelectedItem?.ToString() ?? "همه";
            if (docType.StartsWith("همه")) docType = "همه";

            string fromDate = TxtFilterFromDate.Text.Trim();
            string toDate = TxtFilterToDate.Text.Trim();

            _allDocuments = DatabaseHelper.GetDocuments(
                search,
                mainSubId, sub1Id, sub2Id,
                personId, null, null,
                fromDate, toDate, docType,
                topOcr);

            ApplyGridFilters();
        }

        private void GridDocuments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedDocument = GridDocuments.SelectedItem as DocumentItem;
        }

        private void BtnViewDocPopup_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DocumentItem doc)
            {
                OpenDocumentPopup(doc);
            }
        }

        private void Thumbnail_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement elem && elem.Tag is DocumentItem doc)
            {
                OpenDocumentPopup(doc);
            }
        }

        private void OpenDocumentPopup(DocumentItem doc)
        {
            string fOcr = ColFilterOcr?.Text.Trim() ?? "";
            string topOcr = TxtFilterOcr?.Text.Trim() ?? "";
            string ocrQuery = !string.IsNullOrEmpty(fOcr) ? fOcr : topOcr;

            var popup = new DocumentViewerPopupWindow(doc, ocrQuery) { Owner = this };
            popup.ShowDialog();
        }

        private void GridDocuments_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenEditSelectedDoc();
        }

        private void BtnNewDoc_Click(object sender, RoutedEventArgs e)
        {
            var editWin = new DocumentEditWindow(0) { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                RefreshDocumentsList();
            }
        }

        private void BtnEditSelectedDoc_Click(object sender, RoutedEventArgs e) => OpenEditSelectedDoc();

        private void OpenEditSelectedDoc()
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("ابتدا یک سند را از جدول انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWin = new DocumentEditWindow(_selectedDocument.Id) { Owner = this };
            if (editWin.ShowDialog() == true)
            {
                RefreshDocumentsList();
            }
        }

        private void BtnDeleteSelectedDoc_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDocument == null)
            {
                MessageBox.Show("ابتدا یک سند را برای حذف انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show($"آیا از حذف سند با کد بایگانی «{_selectedDocument.CodBayegany}» اطمینان دارید؟\nتمام تصاویر و برگه‌های مربوط به این سند نیز حذف خواهند شد.", "تایید حذف سند", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.DeleteDocument(_selectedDocument.Id);
                    RefreshDocumentsList();
                    MessageBox.Show("سند با موفقیت از بایگانی حذف شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در حذف سند:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnOpenReport_Click(object sender, RoutedEventArgs e)
        {
            var reportWin = new AdvancedReportWindow { Owner = this };
            reportWin.ShowDialog();
        }

        private void BtnManagePersons_Click(object sender, RoutedEventArgs e)
        {
            var pWin = new ManagePersonsWindow(isSelectionMode: false) { Owner = this };
            pWin.ShowDialog();
            LoadFiltersData();
        }

        private void BtnManageSubjects_Click(object sender, RoutedEventArgs e)
        {
            var sWin = new ManageSubjectsWindow { Owner = this };
            sWin.ShowDialog();
            LoadFiltersData();
        }

        private void BtnBackup_Click(object sender, RoutedEventArgs e)
        {
            var saveDlg = new SaveFileDialog
            {
                Title = "ذخیره فایل پشتیبان دیتابیس بایگانی (شامل تمام اسناد و تصاویر)",
                Filter = "فایل پایگاه‌داده SQLite (*.db)|*.db",
                FileName = $"Bayegany_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
            };

            if (saveDlg.ShowDialog() == true)
            {
                try
                {
                    // استفاده از اشتراک‌گذاری ایمن برای خواندن دیتابیس حتی در صورت باز بودن در برنامه
                    using (var src = new FileStream(DatabaseHelper.DbFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var dst = new FileStream(saveDlg.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        src.CopyTo(dst);
                    }

                    MessageBox.Show("پشتیبان‌گیری از دیتابیس با موفقیت انجام شد.\nتمام مشخصات و تصاویر مدارک ذخیره شدند.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در پشتیبان‌گیری:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Title = "انتخاب فایل پشتیبان جهت بازیابی اطلاعات بایگانی",
                Filter = "فایل پایگاه‌داده SQLite (*.db)|*.db|تمام فایل‌ها (*.*)|*.*"
            };

            if (openDlg.ShowDialog() != true) return;

            string selectedFile = openDlg.FileName;
            if (!File.Exists(selectedFile)) return;

            var confirm = MessageBox.Show(
                "هشدار مهم:\nبا بازیابی این فایل، کلیه اسناد و اطلاعات فعلی سامانه با فایل پشتیبان انتخابی جایگزین خواهد شد.\n\n" +
                "(البته یک نسخه پشتیبان اضطراری از داده‌های فعلی به صورت خودکار در پوشه Data ذخیره خواهد شد).\n\n" +
                "آیا از انجام بازیابی اطمینان کامل دارید؟",
                "تأیید بازیابی اطلاعات بایگانی",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                string currentDb = DatabaseHelper.DbFilePath;
                string dataDir = Path.GetDirectoryName(currentDb) ?? "";

                // پاکسازی اتصالات فعال
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // ۱. تهیه پشتیبان اضطراری از دیتابیس فعلی
                if (File.Exists(currentDb))
                {
                    string emergencyBackup = Path.Combine(dataDir, $"bayegany_auto_backup_before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                    try
                    {
                        using (var src = new FileStream(currentDb, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var dst = new FileStream(emergencyBackup, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            src.CopyTo(dst);
                        }
                    }
                    catch { }
                }

                // ۲. جایگزینی دیتابیس با فایل پشتیبان
                using (var src = new FileStream(selectedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var dst = new FileStream(currentDb, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    src.CopyTo(dst);
                }

                // ۳. بارگذاری مجدد جداول و فیلترها در برنامه
                DatabaseHelper.InitializeDatabase();
                LoadFiltersData();
                RefreshDocumentsList();

                Mouse.OverrideCursor = null;
                MessageBox.Show("اطلاعات و مدارک سامانه بایگانی با موفقیت از فایل پشتیبان بازیابی شد.", "بازیابی موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show($"خطا در حین بازیابی اطلاعات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnPickFromDate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShamsiCalendarWindow(TxtFilterFromDate.Text) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtFilterFromDate.Text = dlg.SelectedDate;
            }
        }

        private void BtnPickToDate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShamsiCalendarWindow(TxtFilterToDate.Text) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtFilterToDate.Text = dlg.SelectedDate;
            }
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _isInitialized = false;
            TxtSearchBox.Text = "";
            if (TxtFilterOcr != null) TxtFilterOcr.Text = "";
            CmbFilterMainSubject.SelectedIndex = 0;
            CmbFilterSubSubject1.ItemsSource = null;
            CmbFilterSubSubject1.IsEnabled = false;
            CmbFilterSubSubject2.ItemsSource = null;
            CmbFilterSubSubject2.IsEnabled = false;
            CmbFilterPerson.SelectedIndex = 0;
            CmbFilterDocType.SelectedIndex = 0;
            TxtFilterFromDate.Text = "";
            TxtFilterToDate.Text = "";

            if (ColFilterCodBayegany != null) ColFilterCodBayegany.Text = "";
            if (ColFilterDocNumber != null) ColFilterDocNumber.Text = "";
            if (ColFilterDocDate != null) ColFilterDocDate.Text = "";
            if (ColFilterDocType != null) ColFilterDocType.Text = "";
            if (ColFilterSubject != null) ColFilterSubject.Text = "";
            if (ColFilterPerson != null) ColFilterPerson.Text = "";
            if (ColFilterPageCount != null) ColFilterPageCount.Text = "";
            if (ColFilterNotes != null) ColFilterNotes.Text = "";
            if (ColFilterOcr != null) ColFilterOcr.Text = "";

            _isInitialized = true;
            RefreshDocumentsList();
        }
    }
}
