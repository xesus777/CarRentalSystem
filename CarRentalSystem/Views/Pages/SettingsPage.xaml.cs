using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class SettingsPage : Page
    {
        private int _userId;
        private bool _isSilentUpdate = false;

        public SettingsPage(int userId)
        {
            InitializeComponent();
            _userId = userId;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            _isSilentUpdate = true;

            ToggleTheme.IsChecked = ThemeService.IsDarkTheme;
            if (TxtThemeStatus != null)
            {
                TxtThemeStatus.Text = ThemeService.IsDarkTheme ? "Включена 🌙" : "Выключена";
            }

            _isSilentUpdate = false;
        }

        private void UpdateUserTheme(bool isDark)
        {
            try
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.ID_User == _userId);
                if (user != null)
                {
                    user.IsDarkTheme = isDark;
                    Core.Context.SaveChanges();

                    ThemeService.SetTheme(isDark);

                    if (TxtThemeStatus != null)
                    {
                        TxtThemeStatus.Text = isDark ? "Включена 🌙" : "Выключена";
                    }
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка сохранения темы в БД: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void ToggleTheme_Checked(object sender, RoutedEventArgs e)
        {
            if (_isSilentUpdate) return;
            UpdateUserTheme(true);
        }

        private void ToggleTheme_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isSilentUpdate) return;
            UpdateUserTheme(false);
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPass = TxtOldPassword.Password;
            string newPass = TxtNewPassword.Password;

            if (string.IsNullOrEmpty(oldPass) || string.IsNullOrEmpty(newPass))
            {
                CustomDialogWindow.ShowOk("Заполните все текстовые поля паролей!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            var user = Core.Context.Users.FirstOrDefault(u => u.ID_User == _userId);
            if (user != null)
            {
                if (user.PasswordHash != AuthService.HashPassword(oldPass))
                {
                    CustomDialogWindow.ShowOk("Текущий пароль введен неверно!", CustomDialogWindow.DialogIcon.Error);
                    return;
                }

                user.PasswordHash = AuthService.HashPassword(newPass);
                Core.Context.SaveChanges();

                CustomDialogWindow.ShowOk("Новый пароль успешно применен в системе!", CustomDialogWindow.DialogIcon.Info);
                TxtOldPassword.Clear();
                TxtNewPassword.Clear();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ProfilePage(_userId));
        }
    }
}
