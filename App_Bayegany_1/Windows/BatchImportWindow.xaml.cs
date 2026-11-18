using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class BatchImportWindow : Window
    {
        private List<string> _imageFiles = new();
        private readonly PersianCalendar _pc = new();

        public BatchImportWindow()
        {
            InitializeComponent();
            Loaded += BatchImportWindow_Loaded;
        }

        private void BatchImportWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CmbBatchMainSubject.ItemsSource = DatabaseHelper.GetAllMainSubjects();
            if (CmbBatchMainSubject.Items.Count > 0) CmbBatchMainSubject.SelectedIndex = 0;

            CmbBatchMainPerson.ItemsSource = DatabaseHelper.GetAllPersons();
            if (CmbBatchMainPerson.Items.Count > 0) CmbBatchMainPerson.SelectedIndex = 0;
        }

        private void BtnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFolderDialog
            {
                Title = "انتخاب پوشه حاوی تصاویر مدارک"
            };

            if (dlg.ShowDialog() == true)
            {
                TxtFolderPath.Text = dlg.FolderName;
                ScanFolder(dlg.FolderName);
            }
        }

        private void ScanFolder(string folder)
        {
            _imageFiles.Clear();
            ListFoundFiles.Items.Clear();

            string[] extensions = { "*.jpg", "*.jpeg", "*.png", "*.webp", "*.bmp" };
            foreach (var ext in extensions)
            {
                try
                {
                    _imageFiles.AddRange(Directory.GetFiles(folder, ext, SearchOption.TopDirectoryOnly));
                }
                catch { }
            }

            foreach (var f in _imageFiles)
            {
                ListFoundFiles.Items.Add(Path.GetFileName(f));
            }

            TxtStatus.Text = $"{_imageFiles.Count} فایل تصویری آماده ورود دسته‌ای به بایگانی است.";
            BtnStartBatch.IsEnabled = _imageFiles.Count > 0;
        }

        private async void BtnStartBatch_Click(object sender, RoutedEventArgs e)
        {
            if (_imageFiles.Count == 0) return;

            BtnStartBatch.IsEnabled = false;
            BatchProgressBar.Value = 0;
            BatchProgressBar.Maximum = _imageFiles.Count;

            int? mainSubId = (CmbBatchMainSubject.SelectedItem as MainSubjectItem)?.Id;
            int? mainPersId = (CmbBatchMainPerson.SelectedItem as PersonItem)?.Id;

            DateTime now = DateTime.Now;
            string docDate = $"{_pc.GetYear(now):D4}/{_pc.GetMonth(now):D2}/{_pc.GetDayOfMonth(now):D2}";

            int importedCount = 0;

            await Task.Run(() =>
            {
                for (int i = 0; i < _imageFiles.Count; i++)
                {
                    string file = _imageFiles[i];
                    try
                    {
                        byte[] compressed = ImageHelper.CompressImageFromFile(file, 1600, 80);
                        byte[] thumb = ImageHelper.GenerateThumbnail(compressed);
                        string fileName = Path.GetFileName(file);

                        var doc = new DocumentItem
                        {
                            CodBayegany = $"BATCH-{_pc.GetYear(now)}-{new Random().Next(1000, 9999)}",
                            DocNumber = Path.GetFileNameWithoutExtension(file),
                            DocDate = docDate,
                            RegDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                            DocType = "نامه اداری",
                            MainSubjectId = mainSubId,
                            MainPersonId = mainPersId,
                            Notes = $"ورود دسته‌ای از فایل: {fileName}",
                            ColorTag = "عادی",
                            Pages = new List<DocumentPageItem>
                            {
                                new DocumentPageItem
                                {
                                    PageNumber = 1,
                                    ImageBlob = compressed,
                                    ThumbnailBlob = thumb,
                                    FileName = fileName,
                                    PageOcrText = $"[ورود دسته‌ای] فایل {fileName}"
                                }
                            }
                        };

                        DatabaseHelper.SaveDocument(doc);
                        importedCount++;
                    }
                    catch { }

                    Dispatcher.Invoke(() =>
                    {
                        BatchProgressBar.Value = i + 1;
                        TxtStatus.Text = $"در حال ذخیره: {i + 1} از {_imageFiles.Count}...";
                    });
                }
            });

            TxtStatus.Text = $"پایان ورود دسته‌ای! {importedCount} مدرک با موفقیت در بایگانی ثبت شدند.";
            MessageBox.Show($"عملیات ورود دسته‌ای به پایان رسید.\nتعداد {importedCount} سند با موفقیت در دیتابیس ثبت و فشرده‌سازی شدند.", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
