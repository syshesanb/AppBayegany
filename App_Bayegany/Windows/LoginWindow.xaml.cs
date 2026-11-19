using System;
using System.Windows;
using BayeganyApp.Database;
using BayeganyApp.Models;

namespace BayeganyApp.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password;

            if (string.IsNullOrEmpty(username))
            {
                ShowError("لطفاً نام کاربری را وارد نمایید.");
                TxtUsername.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                ShowError("لطفاً کلمه عبور را وارد نمایید.");
                TxtPassword.Focus();
                return;
            }

            try
            {
                var user = DatabaseHelper.AuthenticateUser(username, password);
                if (user == null)
                {
                    ShowError("نام کاربری یا کلمه عبور وارد شده صحیح نمی‌باشد.");
                    TxtPassword.SelectAll();
                    TxtPassword.Focus();
                    return;
                }

                // ثبت کاربر جاری در سشن برنامه
                UserSession.CurrentUser = user;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ShowError(string message)
        {
            TxtErrorMessage.Text = message;
            BorderError.Visibility = Visibility.Visible;
        }
    }
}
