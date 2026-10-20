using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class RegisterPage : Page
    {
        private bool _isUpdating = false;

        public RegisterPage()
        {
            InitializeComponent();

            this.Loaded += (s, e) => {
                TxtPhone.Text = "+7 (___) ___-__-__";
                TxtPassport.Text = "____ ______";
                TxtDriverLicense.Text = "____ ______";
            };

            SetupPlaceholderEvents(TxtPhone, "+7 (___) ___-__-__");
            SetupPlaceholderEvents(TxtPassport, "____ ______");
            SetupPlaceholderEvents(TxtDriverLicense, "____ ______");
        }

        private void SetupPlaceholderEvents(TextBox textBox, string placeholder)
        {
            textBox.GotFocus += (s, e) => {
                if (textBox.Text == placeholder)
                {
                    textBox.Text = placeholder;
                    textBox.SelectionStart = placeholder.IndexOf('_') != -1 ? placeholder.IndexOf('_') : 4;
                }
            };

            textBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(textBox.Text) || !textBox.Text.Any(char.IsDigit))
                {
                    textBox.Text = placeholder;
                }
            };
        }

        private void TxtLettersOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[а-яА-ЯёЁa-zA-Z]+$");
        }

        private void TxtDigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+$");

            if (!e.Handled)
            {
                int index = textBox.Text.IndexOf('_');
                if (index != -1)
                {
                    textBox.SelectionStart = index;
                    textBox.SelectionLength = 1;
                }
                else if (textBox.Text.Length >= textBox.MaxLength && textBox.SelectionLength == 0)
                {
                    e.Handled = true;
                }
            }
        }

        private void TxtPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdating) return;

            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            string currentText = textBox.Text;
            string digits = new string(currentText.Where(char.IsDigit).ToArray());

            if (digits.StartsWith("7") && digits.Length > 0)
            {
                digits = digits.Substring(1);
            }

            char[] mask = "+7 (___) ___-__-__".ToCharArray();
            int digitIndex = 0;

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] == '_')
                {
                    if (digitIndex < digits.Length)
                    {
                        mask[i] = digits[digitIndex];
                        digitIndex++;
                    }
                }
            }

            string formatted = new string(mask);

            _isUpdating = true;
            int oldSelection = textBox.SelectionStart;
            textBox.Text = formatted;

            int nextUnderline = formatted.IndexOf('_');
            if (nextUnderline != -1)
            {
                textBox.SelectionStart = Math.Min(oldSelection, nextUnderline);
            }
            else
            {
                textBox.SelectionStart = formatted.Length;
            }

            _isUpdating = false;
        }

        private void TxtPassport_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatDocumentMask(sender as TextBox, "____ ______");
        }

        private void TxtDriverLicense_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatDocumentMask(sender as TextBox, "____ ______");
        }

        private void FormatDocumentMask(TextBox textBox, string placeholder)
        {
            if (_isUpdating || textBox == null) return;

            string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
            char[] mask = placeholder.ToCharArray();
            int digitIndex = 0;

            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] == '_')
                {
                    if (digitIndex < digits.Length)
                    {
                        mask[i] = digits[digitIndex];
                        digitIndex++;
                    }
                }
            }

            string formatted = new string(mask);

            _isUpdating = true;
            int oldSelection = textBox.SelectionStart;
            textBox.Text = formatted;

            int nextUnderline = formatted.IndexOf('_');
            if (nextUnderline != -1)
            {
                textBox.SelectionStart = Math.Min(oldSelection, nextUnderline);
            }
            else
            {
                textBox.SelectionStart = formatted.Length;
            }

            _isUpdating = false;
        }

        private void BtnDoRegister_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtRegLogin.Text.Trim();
            string password = TxtRegPassword.Password;
            string passwordConfirm = TxtRegPasswordConfirm.Password;
            string lastName = TxtLastName.Text.Trim();
            string firstName = TxtFirstName.Text.Trim();
            string middleName = TxtMiddleName.Text.Trim();
            string phone = TxtPhone.Text.Trim();
            string passport = TxtPassport.Text.Trim();
            string license = TxtDriverLicense.Text.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(firstName) ||
                phone.Contains('_') || passport.Contains('_') || license.Contains('_'))
            {
                CustomDialogWindow.ShowOk("Пожалуйста, корректно заполните все обязательные поля!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (password != passwordConfirm)
            {
                CustomDialogWindow.ShowOk("Введенные пароли не совпадают!", CustomDialogWindow.DialogIcon.Error);
                return;
            }

            if (password.Length < 6)
            {
                CustomDialogWindow.ShowOk("Пароль должен быть не менее 6 символов!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (Core.Context.Users.Any(u => u.Login == login))
            {
                CustomDialogWindow.ShowOk("Пользователь с таким логином уже существует!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (Core.Context.Users.Any(u => u.PassportNumber == passport) || Core.Context.Users.Any(u => u.DriverLicense == license))
            {
                CustomDialogWindow.ShowOk("Пользователь с такими паспортными данными или правами уже зарегистрирован!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                var newUser = new Users
                {
                    Login = login,
                    PasswordHash = AuthService.HashPassword(password),
                    LastName = lastName,
                    FirstName = firstName,
                    MiddleName = string.IsNullOrEmpty(middleName) ? null : middleName,
                    Phone = phone,
                    PassportNumber = passport,
                    DriverLicense = license,
                    FK_Role = 3,
                    FK_StaffStatus = 1
                };

                Core.Context.Users.Add(newUser);
                Core.Context.SaveChanges();

                CustomDialogWindow.ShowOk("Регистрация успешно завершена!", CustomDialogWindow.DialogIcon.Info);
                NavigationService.Navigate(new AuthPage());
            }
            catch (Exception ex)
            {
                string innerMessage = ex.InnerException?.InnerException?.Message ?? ex.InnerException?.Message ?? ex.Message;
                CustomDialogWindow.ShowOk($"Произошла ошибка при сохранении данных:\n\n{innerMessage}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AuthPage());
        }
    }
}
