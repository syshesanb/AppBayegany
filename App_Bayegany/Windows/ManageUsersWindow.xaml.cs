using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BayeganyApp.Database;
using BayeganyApp.Models;

namespace BayeganyApp.Windows
{
    public partial class ManageUsersWindow : Window
    {
        private int? _editingUserId = null;
        private bool _isRoleChangingProgrammatically = false;

        public ManageUsersWindow()
        {
            InitializeComponent();
            Loaded += (s, e) =>
            {
                LoadUsers();
                ClearForm();
            };
        }

        private void LoadUsers()
        {
            try
            {
                var users = DatabaseHelper.GetAllUsers();
                DgUsers.ItemsSource = users;
                TxtStatus.Text = $"تعداد کل کاربران ثبت‌شده: {users.Count} کاربر";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در بارگذاری فهرست کاربران:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgUsers.SelectedItem is UserItem user)
            {
                PopulateFormForEdit(user);
            }
        }

        private void PopulateFormForEdit(UserItem user)
        {
            _editingUserId = user.Id;
            TxtFormTitle.Text = $"✏️ ویرایش مشخصات کاربر: {user.Username}";
            TxtFullName.Text = user.FullName;
            TxtUsername.Text = user.Username;

            TxtPassword.Password = "";
            LblPassword.Text = "کلمه عبور جدید (اختیاری):";
            TxtPasswordHint.Text = "* فقط در صورت تمایل به تغییر رمز عبور تکمیل شود.";

            ChkIsActive.IsChecked = user.IsActive;

            // انتخاب نقش
            _isRoleChangingProgrammatically = true;
            SelectRoleInComboBox(user.Role);
            _isRoleChangingProgrammatically = false;

            // تنظیم دستی تیک‌های مجوز
            ChkCanAddDoc.IsChecked = user.CanAddDoc;
            ChkCanEditDoc.IsChecked = user.CanEditDoc;
            ChkCanDeleteDoc.IsChecked = user.CanDeleteDoc;
            ChkCanManagePersons.IsChecked = user.CanManagePersons;
            ChkCanManageSubjects.IsChecked = user.CanManageSubjects;
            ChkCanViewReports.IsChecked = user.CanViewReports;
            ChkCanBackupRestore.IsChecked = user.CanBackupRestore;
            ChkCanManageUsers.IsChecked = user.CanManageUsers;
        }

        private void SelectRoleInComboBox(string role)
        {
            foreach (ComboBoxItem item in CmbRole.Items)
            {
                string text = item.Content.ToString() ?? "";
                if (text.Contains(role) || role.Contains(text) || 
                    (role.Contains("مدیر") && text.Contains("مدیر")) ||
                    (role.Contains("اپراتور") && text.Contains("اپراتور")) ||
                    (role.Contains("مشاهده") && text.Contains("مشاهده")))
                {
                    CmbRole.SelectedItem = item;
                    return;
                }
            }
            CmbRole.SelectedIndex = 3; // سفارشی
        }

        private void CmbRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _isRoleChangingProgrammatically || ChkCanAddDoc == null || CmbRole?.SelectedItem is not ComboBoxItem selectedItem)
                return;

            string roleText = selectedItem.Content.ToString() ?? "";

            if (roleText.Contains("مدیر سیستم"))
            {
                SetAllPermissions(true);
            }
            else if (roleText.Contains("اپراتور"))
            {
                ChkCanAddDoc.IsChecked = true;
                ChkCanEditDoc.IsChecked = true;
                ChkCanDeleteDoc.IsChecked = false;
                ChkCanManagePersons.IsChecked = true;
                ChkCanManageSubjects.IsChecked = true;
                ChkCanViewReports.IsChecked = true;
                ChkCanBackupRestore.IsChecked = false;
                ChkCanManageUsers.IsChecked = false;
            }
            else if (roleText.Contains("مشاهده‌گر"))
            {
                ChkCanAddDoc.IsChecked = false;
                ChkCanEditDoc.IsChecked = false;
                ChkCanDeleteDoc.IsChecked = false;
                ChkCanManagePersons.IsChecked = false;
                ChkCanManageSubjects.IsChecked = false;
                ChkCanViewReports.IsChecked = true;
                ChkCanBackupRestore.IsChecked = false;
                ChkCanManageUsers.IsChecked = false;
            }
        }

        private void SetAllPermissions(bool value)
        {
            if (ChkCanAddDoc == null) return;
            ChkCanAddDoc.IsChecked = value;
            ChkCanEditDoc.IsChecked = value;
            ChkCanDeleteDoc.IsChecked = value;
            ChkCanManagePersons.IsChecked = value;
            ChkCanManageSubjects.IsChecked = value;
            ChkCanViewReports.IsChecked = value;
            ChkCanBackupRestore.IsChecked = value;
            ChkCanManageUsers.IsChecked = value;
        }

        private void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            string fullName = TxtFullName.Text.Trim();
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;
            string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "اپراتور بایگانی";

            if (string.IsNullOrEmpty(fullName))
            {
                MessageBox.Show("لطفاً نام و نام خانوادگی را وارد نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFullName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(username))
            {
                MessageBox.Show("لطفاً نام کاربری را وارد نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsername.Focus();
                return;
            }

            int excludeId = _editingUserId ?? 0;
            if (DatabaseHelper.IsUsernameTaken(username, excludeId))
            {
                MessageBox.Show("این نام کاربری قبلاً در سامانه ثبت شده است. لطفاً نام دیگری انتخاب نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsername.Focus();
                return;
            }

            bool isNew = _editingUserId == null;
            if (isNew)
            {
                if (string.IsNullOrEmpty(password) || password.Length < 3)
                {
                    MessageBox.Show("برای کاربر جدید وارد کردن کلمه عبور با حداقل ۳ کاراکتر الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPassword.Focus();
                    return;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(password) && password.Length < 3)
                {
                    MessageBox.Show("کلمه عبور جدید باید حداقل ۳ کاراکتر باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPassword.Focus();
                    return;
                }

                // اگر کاربر در حال ویرایش اکانت خودش است، نباید بتواند دسترسی مدیریت خودش را قطع کند
                if (_editingUserId == UserSession.CurrentUser?.Id && ChkCanManageUsers.IsChecked != true)
                {
                    MessageBox.Show("شما نمی‌توانید دسترسی مدیریت را از حساب کاربری جاری خود سلب نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ChkCanManageUsers.IsChecked = true;
                    return;
                }
            }

            var userItem = new UserItem
            {
                Id = _editingUserId ?? 0,
                FullName = fullName,
                Username = username,
                Role = role,
                IsActive = ChkIsActive.IsChecked == true,
                CanAddDoc = ChkCanAddDoc.IsChecked == true,
                CanEditDoc = ChkCanEditDoc.IsChecked == true,
                CanDeleteDoc = ChkCanDeleteDoc.IsChecked == true,
                CanManagePersons = ChkCanManagePersons.IsChecked == true,
                CanManageSubjects = ChkCanManageSubjects.IsChecked == true,
                CanViewReports = ChkCanViewReports.IsChecked == true,
                CanBackupRestore = ChkCanBackupRestore.IsChecked == true,
                CanManageUsers = ChkCanManageUsers.IsChecked == true
            };

            try
            {
                if (isNew)
                {
                    DatabaseHelper.AddUser(userItem, password);
                    MessageBox.Show("کاربر جدید با موفقیت در سامانه ایجاد شد.", "پیام سیستم", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    string? newPwd = string.IsNullOrEmpty(password) ? null : password;
                    DatabaseHelper.UpdateUser(userItem, newPwd);

                    // اگر کاربر جاری ویرایش شده، سشن نیز به‌روزرسانی شود
                    if (userItem.Id == UserSession.CurrentUser?.Id)
                    {
                        UserSession.CurrentUser.FullName = userItem.FullName;
                        UserSession.CurrentUser.Username = userItem.Username;
                        UserSession.CurrentUser.Role = userItem.Role;
                        UserSession.CurrentUser.CanAddDoc = userItem.CanAddDoc;
                        UserSession.CurrentUser.CanEditDoc = userItem.CanEditDoc;
                        UserSession.CurrentUser.CanDeleteDoc = userItem.CanDeleteDoc;
                        UserSession.CurrentUser.CanManagePersons = userItem.CanManagePersons;
                        UserSession.CurrentUser.CanManageSubjects = userItem.CanManageSubjects;
                        UserSession.CurrentUser.CanViewReports = userItem.CanViewReports;
                        UserSession.CurrentUser.CanBackupRestore = userItem.CanBackupRestore;
                        UserSession.CurrentUser.CanManageUsers = userItem.CanManageUsers;
                    }

                    MessageBox.Show("اطلاعات کاربر با موفقیت به‌روزرسانی گردید.", "پیام سیستم", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                LoadUsers();
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در ثبت کاربر:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (DgUsers.SelectedItem is not UserItem selectedUser)
            {
                MessageBox.Show("لطفاً ابتدا یک کاربر را از جدول انتخاب نمایید.", "توجه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedUser.Id == UserSession.CurrentUser?.Id)
            {
                MessageBox.Show("شما نمی‌توانید حساب کاربری جاری خود را حذف نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedUser.CanManageUsers || selectedUser.Role.Contains("مدیر"))
            {
                if (DatabaseHelper.GetActiveAdminCount() <= 1)
                {
                    MessageBox.Show("حذف این کاربر مجاز نیست؛ حداقل یک کاربر با دسترسی مدیریت باید در سامانه فعال باقی بماند.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            var result = MessageBox.Show($"آیا از حذف کاربر «{selectedUser.FullName}» با نام کاربری «{selectedUser.Username}» اطمینان دارید؟",
                "تایید حذف کاربر", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseHelper.DeleteUser(selectedUser.Id);
                    MessageBox.Show("کاربر با موفقیت حذف گردید.", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUsers();
                    ClearForm();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطا در حذف کاربر:\n{ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnNewUser_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void BtnClearForm_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
        }

        private void ClearForm()
        {
            _editingUserId = null;
            if (DgUsers != null) DgUsers.SelectedItem = null;

            if (TxtFormTitle != null) TxtFormTitle.Text = "➕ تعریف کاربر جدید";
            if (TxtFullName != null) TxtFullName.Text = "";
            if (TxtUsername != null) TxtUsername.Text = "";
            if (TxtPassword != null) TxtPassword.Password = "";

            if (LblPassword != null) LblPassword.Text = "کلمه عبور (رمز ورود): *";
            if (TxtPasswordHint != null) TxtPasswordHint.Text = "* حداقل ۳ کاراکتر";

            if (ChkIsActive != null) ChkIsActive.IsChecked = true;
            if (CmbRole != null) CmbRole.SelectedIndex = 1; // اپراتور بایگانی پیش‌فرض

            if (ChkCanAddDoc != null) ChkCanAddDoc.IsChecked = true;
            if (ChkCanEditDoc != null) ChkCanEditDoc.IsChecked = true;
            if (ChkCanDeleteDoc != null) ChkCanDeleteDoc.IsChecked = false;
            if (ChkCanManagePersons != null) ChkCanManagePersons.IsChecked = true;
            if (ChkCanManageSubjects != null) ChkCanManageSubjects.IsChecked = true;
            if (ChkCanViewReports != null) ChkCanViewReports.IsChecked = true;
            if (ChkCanBackupRestore != null) ChkCanBackupRestore.IsChecked = false;
            if (ChkCanManageUsers != null) ChkCanManageUsers.IsChecked = false;

            TxtFullName?.Focus();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
