using System;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class CaptchaPage : Page
    {
        private string _correctCaptchaText;

        public CaptchaPage()
        {
            InitializeComponent();
            GenerateNewCaptcha();
        }

        private void GenerateNewCaptcha()
        {
            _correctCaptchaText = CaptchaService.GenerateText(5);
            ImgCaptcha.Source = CaptchaService.CreateCaptchaImage(_correctCaptchaText, 220, 65);
            TxtCaptchaInput.Clear();
            TxtCaptchaInput.Focus();
        }

        private void SubmitCaptcha_Click(object sender, RoutedEventArgs e)
        {
            if (TxtCaptchaInput.Text.Trim().ToLower() == _correctCaptchaText.ToLower())
            {
                CustomDialogWindow.ShowOk("Проверка безопасности успешно пройдена!", CustomDialogWindow.DialogIcon.Info);

                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }
            else
            {
                CustomDialogWindow.ShowOk("Неверный код с картинки! Попробуйте еще раз.", CustomDialogWindow.DialogIcon.Error);
                GenerateNewCaptcha();
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e) => GenerateNewCaptcha();
    }
}
