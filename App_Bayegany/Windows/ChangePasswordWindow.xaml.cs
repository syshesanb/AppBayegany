using System;
using System.Windows;
using BayeganyApp.Database;
using BayeganyApp.Models;
using BayeganyApp.Utils;

namespace BayeganyApp.Windows
{
    public partial class ChangePasswordWindow : Window
    {
        private readonly UserItem _user;

        public ChangePasswordWindow(UserItem? user = null)
        {
            InitializeComponent();
            _user = user ?? UserSession.CurrentUser ?? throw new InvalidOperationException("کاربر مشخص نیست.");

            TxtHeaderUser.Text = $"{_user.FullName} ({_user.Username})";
            TxtFullName.Text = _user.FullName;
            TxtUsername.Text = _user.Username;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string newFullName = TxtFullName.Text.Trim();
            string newUsername = TxtUsername.Text.Trim();
            string currentPassword = TxtCurrentPassword.Password;
            string newPassword = TxtNewPassword.Password;
            string confirmPassword = TxtConfirmPassword.Password;

            if (string.IsNullOrEmpty(newFullName))
            {
                MessageBox.Show("لطفاً نام و نام خانوادگی را وارد نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtFullName.Focus();
                return;
            }

            if (string.IsNullOrEmpty(newUsername))
            {
                MessageBox.Show("لطفاً نام کاربری را وارد نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(currentPassword))
            {
                MessageBox.Show("برای اعمال هرگونه تغییر، وارد کردن کلمه عبور فعلی الزامی است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtCurrentPassword.Focus();
                return;
            }

            // بررسی صحت کلمه عبور فعلی
            if (!SecurityHelper.VerifyPassword(currentPassword, _user.PasswordHash))
            {
                MessageBox.Show("کلمه عبور فعلی اشتباه است.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                TxtCurrentPassword.SelectAll();
                TxtCurrentPassword.Focus();
                return;
            }

            // بررسی عدم تکراری بودن نام کاربری جدید
            if (!string.Equals(newUsername, _user.Username, StringComparison.OrdinalIgnoreCase))
            {
                if (DatabaseHelper.IsUsernameTaken(newUsername, _user.Id))
                {
                    MessageBox.Show("این نام کاربری قبلاً توسط کاربر دیگری ثبت شده است. لطفاً نام دیگری انتخاب نمایید.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtUsername.Focus();
                    return;
                }
            }

            string? passwordToUpdate = null;
            if (!string.IsNullOrEmpty(newPassword))
            {
                if (newPassword.Length < 3)
                {
                    MessageBox.Show("کلمه عبور جدید باید حداقل ۳ کاراکتر باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtNewPassword.Focus();
                    return;
                }

                if (newPassword != confirmPassword)
                {
                    MessageBox.Show("کلمه عبور جدید با تکرار آن یکسان نمی‌باشد.", "خطا", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtConfirmPassword.Focus();
                    return;
                }

                passwordToUpdate = newPassword;
            }

            try
            {
                bool success = DatabaseHelper.UpdateUserProfile(_user.Id, newUsername, newFullName, passwordToUpdate);
                if (success)
                {
                    _user.FullName = newFullName;
                    _user.Username = newUsername;
                    if (passwordToUpdate != null)
                    {
                        _user.PasswordHash = SecurityHelper.HashPassword(passwordToUpdate);
                    }

                    if (UserSession.CurrentUser?.Id == _user.Id)
                    {
                        UserSession.CurrentUser.FullName = newFullName;
                        UserSession.CurrentUser.Username = newUsername;
                        if (passwordToUpdate != null)
                        {
                            UserSession.CurrentUser.PasswordHash = _user.PasswordHash;
                        }
                    }

                    MessageBox.Show("مشخصات کاربری و رمز عبور با موفقیت به‌روزرسانی شد.", "موفقیت", MessageBoxButton.OK, MessageBoxImage.Information);
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("خطا در ذخیره‌سازی اطلاعات.", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطا در به‌روزرسانی: {ex.Message}", "خطا", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
