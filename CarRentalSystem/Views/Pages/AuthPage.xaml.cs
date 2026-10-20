using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class AuthPage : Page
    {
        private int failedAttempts = 0;

        public System.Windows.Window ParentWindow => System.Windows.Window.GetWindow(this);

        public AuthPage()
        {
            InitializeComponent();

            TextBoxLogin.TextChanged += (s, e) =>
                txtHintLogin.Visibility = string.IsNullOrEmpty(TextBoxLogin.Text) ? Visibility.Visible : Visibility.Collapsed;

            PasswordBox.PasswordChanged += (s, e) =>
                txtHintPass.Visibility = string.IsNullOrEmpty(PasswordBox.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ButtonEnter_Click(object sender, RoutedEventArgs e)
        {
            string login = TextBoxLogin.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                CustomDialogWindow.ShowOk("Пожалуйста, заполните все поля ввода!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            string inputHash = AuthService.HashPassword(password);

            var user = Core.Context.Users.FirstOrDefault(u => u.Login == login && u.PasswordHash == inputHash);

            if (user != null)
            {
                if (user.FK_StaffStatus == 2)
                {
                    CustomDialogWindow.ShowOk("Ваша учетная запись временно ЗАМОРОЖЕНА администратором. Доступ в систему заблокирован!", CustomDialogWindow.DialogIcon.Error);
                    return;
                }

                failedAttempts = 0;
                Core.CurrentUserId = user.ID_User;

                CarRentalSystem.Services.ThemeService.SetTheme(user.IsDarkTheme);

                NavigationService.Navigate(new MainPageNavigator());
            }
            else
            {
                failedAttempts++;
                CustomDialogWindow.ShowOk("Неверный логин или пароль!", CustomDialogWindow.DialogIcon.Error);

                if (failedAttempts >= 3)
                {
                    failedAttempts = 0;
                    NavigationService.Navigate(new CaptchaPage());
                }
            }
        }

        private void ButtonGuest_Click(object sender, RoutedEventArgs e)
        {
            
            Core.CurrentUserId = 0;
            NavigationService.Navigate(new MainPage(0));
        }

        private void ButtonReg_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new RegisterPage());
        }
    }
}
