using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BayeganyApp.Database;
using BayeganyApp.Models;

namespace BayeganyApp.Windows
{
    public partial class ManageSubjectsWindow : Window
    {
        private int? _editingMainId = null;
        private int? _editingSub1Id = null;
        private int? _editingSub2Id = null;

        public MainSubjectItem? SelectedMain { get; private set; }
        public SubSubject1Item? SelectedSub1 { get; private set; }
        public SubSubject2Item? SelectedSub2 { get; private set; }
        public string SelectedFullPath { get; private set; } = "";

        public ManageSubjectsWindow()
        {
            InitializeComponent();
            Loaded += ManageSubjectsWindow_Loaded;
        }

        private void ManageSubjectsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshMainSubjects();
        }

        private void RefreshMainSubjects()
        {
            var mainList = DatabaseHelper.GetAllMainSubjects();
            ListMainSubjects.ItemsSource = mainList;
            TxtMainCount.Text = $"({mainList.Count} مورد)";

            ResetMainEditForm();
            ResetSub1EditForm();
            ResetSub2EditForm();

            ListSubSubjects1.ItemsSource = null;
            TxtSub1Count.Text = "(۰ مورد)";
            TxtSub1Header.Text = "۲. موضوعات فرعی ۱ (زیرشاخه) 📂";
            TxtNewSubSubject1.IsEnabled = false;
            BtnSaveSub1.IsEnabled = false;

            ListSubSubjects2.ItemsSource = null;
            TxtSub2Count.Text = "(۰ مورد)";
            TxtSub2Header.Text = "۳. موضوعات فرعی ۲ (ریزموضوع) 📄";
            TxtNewSubSubject2.IsEnabled = false;
            BtnSaveSub2.IsEnabled = false;

            SelectedMain = null;
            SelectedSub1 = null;
            SelectedSub2 = null;
            UpdatePathBreadcrumb();
        }

        private void RefreshSub1List(int mainId)
        {
            var sub1List = DatabaseHelper.GetSubSubjects1(mainId);
            ListSubSubjects1.ItemsSource = sub1List;
            TxtSub1Count.Text = $"({sub1List.Count} مورد)";

            ResetSub1EditForm();
            ResetSub2EditForm();

            ListSubSubjects2.ItemsSource = null;
            TxtSub2Count.Text = "(۰ مورد)";
            TxtSub2Header.Text = "۳. موضوعات فرعی ۲ (ریزموضوع) 📄";
            TxtNewSubSubject2.IsEnabled = false;
            BtnSaveSub2.IsEnabled = false;

            SelectedSub1 = null;
            SelectedSub2 = null;
            UpdatePathBreadcrumb();
        }

        private void RefreshSub2List(int sub1Id)
        {
            var sub2List = DatabaseHelper.GetSubSubjects2(sub1Id);
            ListSubSubjects2.ItemsSource = sub2List;
            TxtSub2Count.Text = $"({sub2List.Count} مورد)";

            ResetSub2EditForm();
            SelectedSub2 = null;
            UpdatePathBreadcrumb();
        }

        private void UpdatePathBreadcrumb()
        {
            if (SelectedMain == null)
            {
                TxtSelectedHierarchy.Text = "(هیچ موضوعی انتخاب نشده است)";
                SelectedFullPath = "";
                BtnConfirmSelection.IsEnabled = false;
                return;
            }

            string path = $"📁 {SelectedMain.Title}";
            if (SelectedSub1 != null)
            {
                path += $" ◀ 📂 {SelectedSub1.Title}";
                if (SelectedSub2 != null)
                {
                    path += $" ◀ 📄 {SelectedSub2.Title}";
                }
            }

            TxtSelectedHierarchy.Text = path;
            SelectedFullPath = path.Replace("📁 ", "").Replace(" ◀ 📂 ", " > ").Replace(" ◀ 📄 ", " > ");
            BtnConfirmSelection.IsEnabled = true;
        }

        // ==================== ۱. مدیریت موضوعات اصلی ====================

        private void ListMainSubjects_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListMainSubjects.SelectedItem is MainSubjectItem main)
            {
                SelectedMain = main;
                TxtSub1Header.Text = $"۲. موضوعات فرعی ۱ ({main.Title}) 📂";
                RefreshSub1List(main.Id);

                TxtNewSubSubject1.IsEnabled = true;
                BtnSaveSub1.IsEnabled = true;
                UpdatePathBreadcrumb();
            }
        }

        private void BtnSelectMain_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MainSubjectItem item)
            {
                ListMainSubjects.SelectedItem = item;
            }
        }

        private void BtnEditMain_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MainSubjectItem item)
            {
                _editingMainId = item.Id;
                TxtNewMainSubject.Text = item.Title;
                BtnSaveMain.Content = "💾 ذخیره";
                BtnSaveMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                BtnCancelMainEdit.Visibility = Visibility.Visible;
                TxtNewMainSubject.Focus();
                TxtNewMainSubject.SelectAll();
            }
        }

        private void BtnDeleteMain_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is MainSubjectItem item)
            {
                var res = MessageBox.Show($"آیا از حذف موضوع اصلی «{item.Title}» اطمینان دارید؟\n⚠️ هشدار: با حذف این موضوع، کلیه موضوعات فرعی ۱ و فرعی ۲ زیرمجموعه آن نیز حذف خواهند شد.",
                                          "تایید حذف موضوع اصلی",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.DeleteMainSubject(item.Id);
                        RefreshMainSubjects();
                        MessageBox.Show("موضوع اصلی و زیرشاخه‌های آن با موفقیت حذف شدند.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطا در حذف موضوع: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnSaveMain_Click(object sender, RoutedEventArgs e)
        {
            string title = TxtNewMainSubject.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("لطفا عنوان موضوع اصلی را وارد نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewMainSubject.Focus();
                return;
            }

            try
            {
                if (_editingMainId.HasValue)
                {
                    DatabaseHelper.UpdateMainSubject(_editingMainId.Value, title);
                    MessageBox.Show("عنوان موضوع اصلی با موفقیت ویرایش شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseHelper.AddMainSubject(title, null, null);
                    MessageBox.Show("موضوع اصلی جدید با موفقیت ثبت شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshMainSubjects();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره موضوع اصلی: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelMainEdit_Click(object sender, RoutedEventArgs e) => ResetMainEditForm();

        private void ResetMainEditForm()
        {
            _editingMainId = null;
            TxtNewMainSubject.Text = "";
            BtnSaveMain.Content = "➕ ثبت";
            BtnSaveMain.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            BtnCancelMainEdit.Visibility = Visibility.Collapsed;
        }

        // ==================== ۲. مدیریت موضوعات فرعی ۱ ====================

        private void ListSubSubjects1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSubSubjects1.SelectedItem is SubSubject1Item sub1)
            {
                SelectedSub1 = sub1;
                TxtSub2Header.Text = $"۳. موضوعات فرعی ۲ ({sub1.Title}) 📄";
                RefreshSub2List(sub1.Id);

                TxtNewSubSubject2.IsEnabled = true;
                BtnSaveSub2.IsEnabled = true;
                UpdatePathBreadcrumb();
            }
        }

        private void BtnSelectSub1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject1Item item)
            {
                ListSubSubjects1.SelectedItem = item;
            }
        }

        private void BtnEditSub1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject1Item item)
            {
                _editingSub1Id = item.Id;
                TxtNewSubSubject1.Text = item.Title;
                BtnSaveSub1.Content = "💾 ذخیره";
                BtnSaveSub1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                BtnCancelSub1Edit.Visibility = Visibility.Visible;
                TxtNewSubSubject1.Focus();
                TxtNewSubSubject1.SelectAll();
            }
        }

        private void BtnDeleteSub1_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject1Item item)
            {
                var res = MessageBox.Show($"آیا از حذف موضوع فرعی ۱ «{item.Title}» اطمینان دارید؟\n⚠️ توجه: کلیه موضوعات فرعی ۲ زیرمجموعه آن نیز حذف خواهند شد.",
                                          "تایید حذف موضوع فرعی ۱",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.DeleteSubSubject1(item.Id);
                        if (SelectedMain != null)
                        {
                            RefreshSub1List(SelectedMain.Id);
                        }
                        MessageBox.Show("موضوع فرعی ۱ و زیرمجموعه‌های آن حذف شدند.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطا در حذف موضوع فرعی ۱: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnSaveSub1_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMain == null)
            {
                MessageBox.Show("ابتدا باید یک موضوع اصلی را از ستون اول انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = TxtNewSubSubject1.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("لطفا عنوان موضوع فرعی ۱ را وارد نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewSubSubject1.Focus();
                return;
            }

            try
            {
                if (_editingSub1Id.HasValue)
                {
                    DatabaseHelper.UpdateSubSubject1(_editingSub1Id.Value, title);
                    MessageBox.Show("عنوان موضوع فرعی ۱ با موفقیت ویرایش شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseHelper.AddSubSubject1(SelectedMain.Id, title, null);
                    MessageBox.Show("موضوع فرعی ۱ جدید با موفقیت ثبت شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshSub1List(SelectedMain.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره موضوع فرعی ۱: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelSub1Edit_Click(object sender, RoutedEventArgs e) => ResetSub1EditForm();

        private void ResetSub1EditForm()
        {
            _editingSub1Id = null;
            TxtNewSubSubject1.Text = "";
            BtnSaveSub1.Content = "➕ ثبت";
            BtnSaveSub1.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            BtnCancelSub1Edit.Visibility = Visibility.Collapsed;
        }

        // ==================== ۳. مدیریت موضوعات فرعی ۲ ====================

        private void ListSubSubjects2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListSubSubjects2.SelectedItem is SubSubject2Item sub2)
            {
                SelectedSub2 = sub2;
                UpdatePathBreadcrumb();
            }
        }

        private void BtnSelectSub2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject2Item item)
            {
                ListSubSubjects2.SelectedItem = item;
            }
        }

        private void BtnEditSub2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject2Item item)
            {
                _editingSub2Id = item.Id;
                TxtNewSubSubject2.Text = item.Title;
                BtnSaveSub2.Content = "💾 ذخیره";
                BtnSaveSub2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                BtnCancelSub2Edit.Visibility = Visibility.Visible;
                TxtNewSubSubject2.Focus();
                TxtNewSubSubject2.SelectAll();
            }
        }

        private void BtnDeleteSub2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is SubSubject2Item item)
            {
                var res = MessageBox.Show($"آیا از حذف موضوع فرعی ۲ «{item.Title}» اطمینان دارید؟",
                                          "تایید حذف موضوع فرعی ۲",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.DeleteSubSubject2(item.Id);
                        if (SelectedSub1 != null)
                        {
                            RefreshSub2List(SelectedSub1.Id);
                        }
                        MessageBox.Show("موضوع فرعی ۲ حذف شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطا در حذف موضوع فرعی ۲: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnSaveSub2_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSub1 == null)
            {
                MessageBox.Show("ابتدا باید یک موضوع فرعی ۱ را از ستون دوم انتخاب کنید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string title = TxtNewSubSubject2.Text.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show("لطفا عنوان موضوع فرعی ۲ را وارد نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtNewSubSubject2.Focus();
                return;
            }

            try
            {
                if (_editingSub2Id.HasValue)
                {
                    DatabaseHelper.UpdateSubSubject2(_editingSub2Id.Value, title);
                    MessageBox.Show("عنوان موضوع فرعی ۲ با موفقیت ویرایش شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseHelper.AddSubSubject2(SelectedSub1.Id, title, null);
                    MessageBox.Show("موضوع فرعی ۲ جدید با موفقیت ثبت شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                RefreshSub2List(SelectedSub1.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره موضوع فرعی ۲: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelSub2Edit_Click(object sender, RoutedEventArgs e) => ResetSub2EditForm();

        private void ResetSub2EditForm()
        {
            _editingSub2Id = null;
            TxtNewSubSubject2.Text = "";
            BtnSaveSub2.Content = "➕ ثبت";
            BtnSaveSub2.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            BtnCancelSub2Edit.Visibility = Visibility.Collapsed;
        }

        private void BtnConfirmSelection_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMain == null)
            {
                MessageBox.Show("حداقل یک موضوع اصلی باید انتخاب شود.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DialogResult = true;
                Close();
            }
            catch (InvalidOperationException)
            {
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
