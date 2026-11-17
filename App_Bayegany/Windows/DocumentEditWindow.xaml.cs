using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using BayeganyApp.Controls;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class DocumentEditWindow : Window
    {
        private int _documentId = 0;
        private DocumentItem _document = new();
        private ObservableCollection<DocumentPageItem> _pages = new();
        private DocumentPageItem? _selectedPage = null;
        private readonly PersianCalendar _pc = new();

        public DocumentEditWindow(int documentId = 0)
        {
            InitializeComponent();
            _documentId = documentId;
            Loaded += DocumentEditWindow_Loaded;
            DocImageViewer.CropRequested += DocImageViewer_CropRequested;
            DocImageViewer.RotateRequested += DocImageViewer_RotateRequested;
        }

        private void DocumentEditWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDropdowns();

            if (_documentId > 0)
            {
                TxtWindowTitle.Text = $"✏️ ویرایش سند بایگانی (شناسه: {_documentId})";
                LoadExistingDocument(_documentId);
            }
            else
            {
                TxtWindowTitle.Text = "➕ ثبت سند و مدرک جدید در بایگانی";
                SetDefaultValuesForNew();
            }

            ListThumbnails.ItemsSource = _pages;
            UpdatePageCounter();
        }

        private void LoadDropdowns()
        {
            // ۱. موضوعات اصلی
            var mainSubjects = DatabaseHelper.GetAllMainSubjects();
            mainSubjects.Insert(0, new MainSubjectItem { Id = 0, Title = "انتخاب موضوع اصلی..." });
            CmbMainSubject.ItemsSource = mainSubjects;
            CmbMainSubject.SelectedIndex = 0;

            // ۲. اشخاص
            var persons = DatabaseHelper.GetAllPersons();
            var pListMain = new List<PersonItem> { new PersonItem { Id = 0, FullName = "انتخاب شخص اصلی..." } };
            pListMain.AddRange(persons);
            CmbMainPerson.ItemsSource = pListMain;
            CmbMainPerson.SelectedIndex = 0;

            var pListSub1 = new List<PersonItem> { new PersonItem { Id = 0, FullName = "انتخاب شخص فرعی ۱ (اختیاری)..." } };
            pListSub1.AddRange(persons);
            CmbSubPerson1.ItemsSource = pListSub1;
            CmbSubPerson1.SelectedIndex = 0;

            var pListSub2 = new List<PersonItem> { new PersonItem { Id = 0, FullName = "انتخاب شخص فرعی ۲ (اختیاری)..." } };
            pListSub2.AddRange(persons);
            CmbSubPerson2.ItemsSource = pListSub2;
            CmbSubPerson2.SelectedIndex = 0;

            // ۳. انواع مدرک
            CmbDocType.Items.Clear();
            CmbDocType.Items.Add("قرارداد");
            CmbDocType.Items.Add("نامه اداری");
            CmbDocType.Items.Add("صورتجلسه");
            CmbDocType.Items.Add("سند مالی و فاکتور");
            CmbDocType.Items.Add("مدرک هویتی");
            CmbDocType.Items.Add("سند ملکی و ثبتی");
            CmbDocType.Items.Add("سایر مدارک");
            CmbDocType.SelectedIndex = 0;

            // ۴. برچسب رنگی
            CmbColorTag.Items.Clear();
            CmbColorTag.Items.Add("عادی");
            CmbColorTag.Items.Add("فوری");
            CmbColorTag.Items.Add("محرمانه");
            CmbColorTag.Items.Add("اقدام شده");
            CmbColorTag.Items.Add("در انتظار پیگیری");
            CmbColorTag.SelectedIndex = 0;
        }

        private void SetDefaultValuesForNew()
        {
            DateTime now = DateTime.Now;
            string todayShamsi = $"{_pc.GetYear(now):D4}/{_pc.GetMonth(now):D2}/{_pc.GetDayOfMonth(now):D2}";

            TxtDocDate.Text = todayShamsi;
            TxtCodBayegany.Text = $"B-{_pc.GetYear(now)}-{new Random().Next(1000, 9999)}";
        }

        private void LoadExistingDocument(int id)
        {
            var docs = DatabaseHelper.GetDocuments();
            var doc = docs.FirstOrDefault(d => d.Id == id);
            if (doc == null) return;

            _document = doc;
            TxtCodBayegany.Text = doc.CodBayegany;
            TxtDocNumber.Text = doc.DocNumber;
            TxtDocDate.Text = doc.DocDate;
            TxtNotes.Text = doc.Notes;

            if (!string.IsNullOrEmpty(doc.DocType)) CmbDocType.SelectedItem = doc.DocType;
            if (!string.IsNullOrEmpty(doc.ColorTag)) CmbColorTag.SelectedItem = doc.ColorTag;

            // تنظیم موضوع اصلی
            if (doc.MainSubjectId.HasValue && doc.MainSubjectId > 0)
            {
                foreach (MainSubjectItem item in CmbMainSubject.Items)
                {
                    if (item.Id == doc.MainSubjectId.Value)
                    {
                        CmbMainSubject.SelectedItem = item;
                        break;
                    }
                }
            }

            // تنظیم موضوع فرعی ۱
            if (doc.SubSubject1Id.HasValue && doc.SubSubject1Id > 0 && CmbSubSubject1.ItemsSource != null)
            {
                foreach (SubSubject1Item item in CmbSubSubject1.Items)
                {
                    if (item.Id == doc.SubSubject1Id.Value)
                    {
                        CmbSubSubject1.SelectedItem = item;
                        break;
                    }
                }
            }

            // تنظیم موضوع فرعی ۲
            if (doc.SubSubject2Id.HasValue && doc.SubSubject2Id > 0 && CmbSubSubject2.ItemsSource != null)
            {
                foreach (SubSubject2Item item in CmbSubSubject2.Items)
                {
                    if (item.Id == doc.SubSubject2Id.Value)
                    {
                        CmbSubSubject2.SelectedItem = item;
                        break;
                    }
                }
            }

            // تنظیم اشخاص
            SetPersonComboSelection(CmbMainPerson, doc.MainPersonId);
            SetPersonComboSelection(CmbSubPerson1, doc.SubPerson1Id);
            SetPersonComboSelection(CmbSubPerson2, doc.SubPerson2Id);

            // بارگذاری برگه‌های تصویر
            var pages = DatabaseHelper.GetDocumentPages(id);
            _pages.Clear();
            foreach (var p in pages)
            {
                _pages.Add(p);
            }

            if (_pages.Count > 0)
            {
                ListThumbnails.SelectedIndex = 0;
            }
        }

        private void SetPersonComboSelection(ComboBox cmb, int? personId)
        {
            if (personId.HasValue && personId.Value > 0)
            {
                foreach (PersonItem p in cmb.Items)
                {
                    if (p.Id == personId.Value)
                    {
                        cmb.SelectedItem = p;
                        break;
                    }
                }
            }
        }

        private void CmbMainSubject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbMainSubject.SelectedItem is MainSubjectItem selectedMain && selectedMain.Id > 0)
            {
                var sub1List = DatabaseHelper.GetSubSubjects1(selectedMain.Id);
                sub1List.Insert(0, new SubSubject1Item { Id = 0, Title = "انتخاب موضوع فرعی ۱..." });
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
        }

        private void CmbSubSubject1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSubSubject1.SelectedItem is SubSubject1Item selectedSub1 && selectedSub1.Id > 0)
            {
                var sub2List = DatabaseHelper.GetSubSubjects2(selectedSub1.Id);
                sub2List.Insert(0, new SubSubject2Item { Id = 0, Title = "انتخاب موضوع فرعی ۲..." });
                CmbSubSubject2.ItemsSource = sub2List;
                CmbSubSubject2.IsEnabled = true;
                CmbSubSubject2.SelectedIndex = 0;
            }
            else
            {
                CmbSubSubject2.ItemsSource = null;
                CmbSubSubject2.IsEnabled = false;
            }
        }

        private void BtnPickDocDate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new ShamsiCalendarWindow(TxtDocDate.Text) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                TxtDocDate.Text = dlg.SelectedDate;
            }
        }

        private void BtnAddMainSubject_Click(object sender, RoutedEventArgs e)
        {
            string title = Microsoft.VisualBasic.Interaction.InputBox("عنوان موضوع اصلی جدید را وارد نمایید:", "تعریف موضوع اصلی جدید", "");
            if (!string.IsNullOrWhiteSpace(title))
            {
                try
                {
                    int newId = DatabaseHelper.AddMainSubject(title, null, null);
                    LoadDropdowns();
                    foreach (MainSubjectItem item in CmbMainSubject.Items)
                    {
                        if (item.Id == newId)
                        {
                            CmbMainSubject.SelectedItem = item;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ثبت موضوع: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddSubSubject1_Click(object sender, RoutedEventArgs e)
        {
            if (CmbMainSubject.SelectedItem is not MainSubjectItem main || main.Id == 0)
            {
                MessageBox.Show("ابتدا یک موضوع اصلی انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = Microsoft.VisualBasic.Interaction.InputBox($"عنوان موضوع فرعی ۱ برای «{main.Title}» را وارد نمایید:", "تعریف موضوع فرعی ۱", "");
            if (!string.IsNullOrWhiteSpace(title))
            {
                try
                {
                    int newId = DatabaseHelper.AddSubSubject1(main.Id, title, null);
                    var sub1List = DatabaseHelper.GetSubSubjects1(main.Id);
                    sub1List.Insert(0, new SubSubject1Item { Id = 0, Title = "انتخاب موضوع فرعی ۱..." });
                    CmbSubSubject1.ItemsSource = sub1List;
                    foreach (SubSubject1Item item in sub1List)
                    {
                        if (item.Id == newId)
                        {
                            CmbSubSubject1.SelectedItem = item;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ثبت موضوع فرعی: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddSubSubject2_Click(object sender, RoutedEventArgs e)
        {
            if (CmbSubSubject1.SelectedItem is not SubSubject1Item sub1 || sub1.Id == 0)
            {
                MessageBox.Show("ابتدا یک موضوع فرعی ۱ انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = Microsoft.VisualBasic.Interaction.InputBox($"عنوان موضوع فرعی ۲ برای «{sub1.Title}» را وارد نمایید:", "تعریف موضوع فرعی ۲", "");
            if (!string.IsNullOrWhiteSpace(title))
            {
                try
                {
                    int newId = DatabaseHelper.AddSubSubject2(sub1.Id, title, null);
                    var sub2List = DatabaseHelper.GetSubSubjects2(sub1.Id);
                    sub2List.Insert(0, new SubSubject2Item { Id = 0, Title = "انتخاب موضوع فرعی ۲..." });
                    CmbSubSubject2.ItemsSource = sub2List;
                    foreach (SubSubject2Item item in sub2List)
                    {
                        if (item.Id == newId)
                        {
                            CmbSubSubject2.SelectedItem = item;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در ثبت موضوع فرعی: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddPerson_Click(object sender, RoutedEventArgs e)
        {
            var pWin = new ManagePersonsWindow { Owner = this };
            if (pWin.ShowDialog() == true && pWin.SelectedPerson != null)
            {
                LoadDropdowns();
                SetPersonComboSelection(CmbMainPerson, pWin.SelectedPerson.Id);
            }
            else
            {
                LoadDropdowns();
            }
        }

        // --- مدیریت برگه‌های تصویر ---

        private void BtnAddImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "انتخاب تصویر مدرک یا برگه اسکن‌شده",
                Filter = "تصاویر (*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.jpg;*.jpeg;*.png;*.webp;*.bmp|همه فایل‌ها (*.*)|*.*",
                Multiselect = true
            };

            if (dlg.ShowDialog() == true)
            {
                foreach (string file in dlg.FileNames)
                {
                    byte[] compressed = ImageHelper.CompressImageFromFile(file, 1600, 80);
                    byte[] thumb = ImageHelper.GenerateThumbnail(compressed);

                    var page = new DocumentPageItem
                    {
                        PageNumber = _pages.Count + 1,
                        ImageBlob = compressed,
                        ThumbnailBlob = thumb,
                        FileName = Path.GetFileName(file),
                        ThumbnailImage = ImageHelper.BytesToBitmapSource(thumb),
                        FullImage = ImageHelper.BytesToBitmapSource(compressed)
                    };

                    _pages.Add(page);
                }

                if (_pages.Count > 0)
                {
                    ListThumbnails.SelectedIndex = _pages.Count - 1;
                }
                UpdatePageCounter();
            }
        }

        private void BtnRemovePage_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPage != null)
            {
                _pages.Remove(_selectedPage);
                // بروزرسانی شماره صفحات
                for (int i = 0; i < _pages.Count; i++)
                {
                    _pages[i].PageNumber = i + 1;
                }
                ListThumbnails.SelectedIndex = _pages.Count > 0 ? 0 : -1;
                UpdatePageCounter();
            }
        }

        private void ListThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // ذخیره متن و چرخش احتمالی برگه قبلی
            if (_selectedPage != null)
            {
                _selectedPage.PageOcrText = TxtPageOcr.Text;
                if (_selectedPage.FullImage != null && DocImageViewer.CurrentRotationAngle != 0)
                {
                    ApplyCurrentRotationToPage(_selectedPage);
                }
            }

            _selectedPage = ListThumbnails.SelectedItem as DocumentPageItem;
            if (_selectedPage != null)
            {
                DocImageViewer.SetImage(_selectedPage.FullImage);
                TxtPageOcr.Text = _selectedPage.PageOcrText ?? "";
            }
            else
            {
                DocImageViewer.SetImage(null);
                TxtPageOcr.Text = "";
            }
        }

        private void BtnViewOcrPopup_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPage == null)
            {
                MessageBox.Show("ابتدا یک برگه تصویر را انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // ذخیره تغییرات دستی احتمالی در متن OCR
                _selectedPage.PageOcrText = TxtPageOcr.Text;

                string docInfo = $"سند بایگانی: {TxtCodBayegany.Text} ({TxtDocNumber.Text})";
                var popup = new OcrViewPopupWindow(TxtPageOcr.Text, docInfo, _selectedPage.PageNumber)
                {
                    Owner = this
                };

                if (popup.ShowDialog() == true && popup.HasChanges)
                {
                    TxtPageOcr.Text = popup.ResultOcrText;
                    _selectedPage.PageOcrText = popup.ResultOcrText;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در نمایش پنجره متن OCR: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearOcr_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPage == null)
            {
                MessageBox.Show("ابتدا یک برگه تصویر را انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var res = MessageBox.Show("آیا از پاک کردن متن OCR این برگه اطمینان دارید؟", "تایید حذف متن OCR", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                TxtPageOcr.Text = "";
                _selectedPage.PageOcrText = "";
                MessageBox.Show("متن OCR این برگه حذف شد.\nاکنون می‌توانید با کلیک مجدد روی دکمه «استخراج متن این صفحه»، متن را از نو استخراج نمایید.", "حذف موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnRunOcr_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPage == null)
            {
                MessageBox.Show("ابتدا یک برگه تصویر را انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // استخراج مستقیم از روی تصویر جاری
                string extractedText = OcrService.ExtractTextFromImage(
                    _selectedPage.ImageBlob,
                    _selectedPage.FileName,
                    null // فورس استخراج مجدد بدون استفاده از کش قبلی
                );

                TxtPageOcr.Text = extractedText;
                _selectedPage.PageOcrText = extractedText;

                MessageBox.Show("متن و اطلاعات با موفقیت از تصویر استخراج و در کادر OCR ذخیره شد.", "OCR موفق", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در پردازش OCR: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void DocImageViewer_CropRequested(object? sender, EventArgs e)
        {
            if (_selectedPage == null || _selectedPage.FullImage == null)
            {
                MessageBox.Show("ابتدا یک برگه تصویر را انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // اعمال زاویه چرخش فعلی نمایشگر روی تصویر جهت ارسال به پنجره برش
                BitmapSource sourceForCrop = _selectedPage.FullImage;
                double currentAngle = DocImageViewer.CurrentRotationAngle;
                if (currentAngle != 0)
                {
                    sourceForCrop = ImageHelper.RotateBitmap(sourceForCrop, currentAngle);
                }

                var cropWin = new CropImageWindow(sourceForCrop) { Owner = this };
                if (cropWin.ShowDialog() == true && cropWin.CroppedBitmapSource != null)
                {
                    byte[] croppedBytes = cropWin.CroppedBytes ?? ImageHelper.BitmapSourceToJpegBytes(cropWin.CroppedBitmapSource);
                    byte[] thumbBytes = ImageHelper.GenerateThumbnail(croppedBytes);

                    _selectedPage.FullImage = cropWin.CroppedBitmapSource;
                    _selectedPage.ImageBlob = croppedBytes;
                    _selectedPage.ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes);
                    _selectedPage.ThumbnailBlob = thumbBytes;

                    // ریست زاویه چرخش در نمایشگر چون چرخش روی تصویر نهایی ذخیره شده است
                    DocImageViewer.ResetView();
                    DocImageViewer.SetImage(_selectedPage.FullImage);

                    // رفرش فهرست بندانگشتی‌ها
                    ListThumbnails.Items.Refresh();

                    MessageBox.Show("تصویر با موفقیت برش داده شد و بخش‌های اضافی حذف گردید.\nدر صورت تمایل می‌توانید با دکمه «استخراج متن این صفحه»، متن OCR را مجدداً از روی تصویر جدید استخراج کنید.", "برش موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در فرآیند برش تصویر: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DocImageViewer_RotateRequested(object? sender, EventArgs e)
        {
            if (_selectedPage == null || _selectedPage.FullImage == null)
            {
                MessageBox.Show("ابتدا یک برگه تصویر را انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // چرخش ۹۰ درجه در جهت عقربه‌های ساعت و بازتولید بایت‌های تصویر و بندانگشتی
                BitmapSource rotated = ImageHelper.RotateBitmap(_selectedPage.FullImage, 90);
                byte[] rotatedBytes = ImageHelper.BitmapSourceToJpegBytes(rotated, 90);
                byte[] thumbBytes = ImageHelper.GenerateThumbnail(rotatedBytes);

                _selectedPage.FullImage = rotated;
                _selectedPage.ImageBlob = rotatedBytes;
                _selectedPage.ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes);
                _selectedPage.ThumbnailBlob = thumbBytes;

                DocImageViewer.SetImage(_selectedPage.FullImage);

                // بروزرسانی بلادرنگ تصویر بندانگشتی در فهرست برگه‌ها
                ListThumbnails.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطا در چرخش تصویر: " + ex.Message, "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void ApplyCurrentRotationToPage(DocumentPageItem page)
        {
            if (page.FullImage == null || DocImageViewer.CurrentRotationAngle == 0) return;

            try
            {
                BitmapSource rotated = ImageHelper.RotateBitmap(page.FullImage, DocImageViewer.CurrentRotationAngle);
                byte[] rotatedBytes = ImageHelper.BitmapSourceToJpegBytes(rotated, 90);
                byte[] thumbBytes = ImageHelper.GenerateThumbnail(rotatedBytes);

                page.FullImage = rotated;
                page.ImageBlob = rotatedBytes;
                page.ThumbnailImage = ImageHelper.BytesToBitmapSource(thumbBytes);
                page.ThumbnailBlob = thumbBytes;

                DocImageViewer.ResetView();
            }
            catch
            {
            }
        }

        private void UpdatePageCounter()
        {
            TxtPageCounter.Text = $"صفحات: {_pages.Count} برگه";
        }

        // --- ذخیره‌سازی نهایی ---

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string codBayegany = TxtCodBayegany.Text.Trim();
            if (string.IsNullOrWhiteSpace(codBayegany))
            {
                MessageBox.Show("لطفاً فیلد «کد بایگانی (CodBayegany)» را وارد نمایید.", "خطا در اعتبارسنجی", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCodBayegany.Focus();
                return;
            }

            string docDate = TxtDocDate.Text.Trim();
            if (string.IsNullOrWhiteSpace(docDate))
            {
                MessageBox.Show("لطفاً فیلد «تاریخ سند» را وارد نمایید.", "خطا در اعتبارسنجی", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtDocDate.Focus();
                return;
            }

            // ذخیره آخرین متن OCR برگه انتخابی و چرخش احتمالی
            if (_selectedPage != null)
            {
                _selectedPage.PageOcrText = TxtPageOcr.Text;
                if (_selectedPage.FullImage != null && DocImageViewer.CurrentRotationAngle != 0)
                {
                    ApplyCurrentRotationToPage(_selectedPage);
                }
            }

            _document.CodBayegany = codBayegany;
            _document.DocNumber = TxtDocNumber.Text.Trim();
            _document.DocDate = docDate;
            _document.DocType = CmbDocType.SelectedItem?.ToString() ?? "قرارداد";
            _document.ColorTag = CmbColorTag.SelectedItem?.ToString() ?? "عادی";
            _document.Notes = TxtNotes.Text.Trim();

            _document.MainSubjectId = (CmbMainSubject.SelectedItem as MainSubjectItem)?.Id > 0 ? (CmbMainSubject.SelectedItem as MainSubjectItem)!.Id : null;
            _document.SubSubject1Id = (CmbSubSubject1.SelectedItem as SubSubject1Item)?.Id > 0 ? (CmbSubSubject1.SelectedItem as SubSubject1Item)!.Id : null;
            _document.SubSubject2Id = (CmbSubSubject2.SelectedItem as SubSubject2Item)?.Id > 0 ? (CmbSubSubject2.SelectedItem as SubSubject2Item)!.Id : null;

            _document.MainPersonId = (CmbMainPerson.SelectedItem as PersonItem)?.Id > 0 ? (CmbMainPerson.SelectedItem as PersonItem)!.Id : null;
            _document.SubPerson1Id = (CmbSubPerson1.SelectedItem as PersonItem)?.Id > 0 ? (CmbSubPerson1.SelectedItem as PersonItem)!.Id : null;
            _document.SubPerson2Id = (CmbSubPerson2.SelectedItem as PersonItem)?.Id > 0 ? (CmbSubPerson2.SelectedItem as PersonItem)!.Id : null;

            _document.Pages = _pages.ToList();

            try
            {
                DatabaseHelper.SaveDocument(_document);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره‌سازی سند:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
