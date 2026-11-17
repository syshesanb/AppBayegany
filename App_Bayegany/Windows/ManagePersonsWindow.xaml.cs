using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BayeganyApp.Database;
using BayeganyApp.Models;

namespace BayeganyApp.Windows
{
    public partial class ManagePersonsWindow : Window
    {
        private int? _editingPersonId = null;

        /// <summary>
        /// شخص انتخاب‌شده برای سناریوهایی که پنجره به صورت انتخابی (Picker) باز می‌شود
        /// </summary>
        public PersonItem? SelectedPerson { get; private set; }

        public static readonly DependencyProperty IsSelectionModeProperty =
            DependencyProperty.Register(nameof(IsSelectionMode), typeof(bool), typeof(ManagePersonsWindow), new PropertyMetadata(false));

        /// <summary>
        /// وضعیت فعال بودن حالت انتخاب شخص (در صورت false بودن، دکمه انتخاب غیرفعال است)
        /// </summary>
        public bool IsSelectionMode
        {
            get => (bool)GetValue(IsSelectionModeProperty);
            set => SetValue(IsSelectionModeProperty, value);
        }

        public ManagePersonsWindow(bool isSelectionMode = false)
        {
            InitializeComponent();
            IsSelectionMode = isSelectionMode;
            Loaded += ManagePersonsWindow_Loaded;
        }

        private void ManagePersonsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            CmbPersonType.Items.Clear();
            CmbPersonType.Items.Add("حقیقی");
            CmbPersonType.Items.Add("حقوقی خصوصی");
            CmbPersonType.Items.Add("حقوقی دولتی");
            CmbPersonType.Items.Add("همکار / مشاور");
            CmbPersonType.SelectedIndex = 0;

            RefreshList();
        }

        private void RefreshList()
        {
            var persons = DatabaseHelper.GetAllPersons();
            GridPersons.ItemsSource = persons;
            TxtRecordCount.Text = $"تعداد اشخاص ثبت‌شده: {persons.Count} شخص / شرکت";
        }

        private void BtnSavePerson_Click(object sender, RoutedEventArgs e)
        {
            string fullName = TxtFullName.Text.Trim();
            if (string.IsNullOrWhiteSpace(fullName))
            {
                MessageBox.Show("نام کامل یا عنوان شرکت الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFullName.Focus();
                return;
            }

            try
            {
                string? pType = CmbPersonType.SelectedItem?.ToString();
                string natCode = TxtNationalCode.Text.Trim();
                string contact = TxtContactInfo.Text.Trim();
                string notes = TxtPersonNotes.Text.Trim();

                if (_editingPersonId.HasValue)
                {
                    DatabaseHelper.UpdatePerson(_editingPersonId.Value, fullName, pType, natCode, contact, notes);
                    MessageBox.Show("مشخصات شخص با موفقیت ویرایش و به‌روزرسانی شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    DatabaseHelper.AddPerson(fullName, pType, natCode, contact, notes);
                    MessageBox.Show("شخص/شرکت جدید با موفقیت ثبت شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ResetForm();
                RefreshList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ذخیره‌سازی اطلاعات:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        private void ResetForm()
        {
            _editingPersonId = null;
            TxtFullName.Text = "";
            CmbPersonType.SelectedIndex = 0;
            TxtNationalCode.Text = "";
            TxtContactInfo.Text = "";
            TxtPersonNotes.Text = "";

            BtnSavePerson.Content = "➕ ثبت شخص جدید";
            BtnSavePerson.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16A34A"));
            BtnCancelEdit.Visibility = Visibility.Collapsed;
        }

        private void BtnSelectRow_Click(object sender, RoutedEventArgs e)
        {
            if (!IsSelectionMode) return;

            if (sender is Button btn && btn.Tag is PersonItem person)
            {
                SelectedPerson = person;
                try
                {
                    DialogResult = true;
                    Close();
                    return;
                }
                catch (InvalidOperationException)
                {
                    // پنجره به صورت غیر مودال باز شده است
                    GridPersons.SelectedItem = person;
                    MessageBox.Show($"شخص «{person.FullName}» انتخاب شد.", "انتخاب شخص", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void BtnEditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PersonItem person)
            {
                _editingPersonId = person.Id;
                TxtFullName.Text = person.FullName;
                
                // انتخاب نوع شخص در کمبوباکس
                CmbPersonType.SelectedItem = person.PersonType ?? "حقیقی";
                if (CmbPersonType.SelectedIndex < 0) CmbPersonType.SelectedIndex = 0;

                TxtNationalCode.Text = person.NationalCode ?? "";
                TxtContactInfo.Text = person.ContactInfo ?? "";
                TxtPersonNotes.Text = person.Notes ?? "";

                BtnSavePerson.Content = "💾 ذخیره تغییرات";
                BtnSavePerson.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0284C7"));
                BtnCancelEdit.Visibility = Visibility.Visible;
                TxtFullName.Focus();
            }
        }

        private void BtnDeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PersonItem person)
            {
                var res = MessageBox.Show($"آیا از حذف شخص «{person.FullName}» (شناسه: {person.Id}) اطمینان دارید؟",
                                          "تایید حذف شخص",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    try
                    {
                        DatabaseHelper.DeletePerson(person.Id);
                        if (_editingPersonId == person.Id)
                        {
                            ResetForm();
                        }
                        RefreshList();
                        MessageBox.Show("شخص با موفقیت از سیستم حذف شد.", "عملیات موفق", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"خطا در حذف شخص:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
