using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class AdminStaffPage : Page
    {
        public AdminStaffPage()
        {
            InitializeComponent();
            RefreshStaffList();
        }

        private void RefreshStaffList()
        {
            try
            {
                // ИСПРАВЛЕНО: Извлекаем кисти статусов динамически из ресурсов приложения,
                // чтобы красный и зеленый цвета были контрастными как в светлой, так и в темной теме
                var activeBrush = Application.Current.Resources["CarStatusText"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Green;
                var frozenBrush = Application.Current.Resources["BrushDanger"] as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Red;

                var staff = Core.Context.Users
                    .Where(u => u.FK_Role == 2)
                    .ToList()
                    .Select(u => new
                    {
                        ID_User = u.ID_User,
                        Login = u.Login,
                        StatusName = u.FK_StaffStatus == 2 ? "Заморожен" : "Активен",
                        StatusColor = u.FK_StaffStatus == 2 ? frozenBrush : activeBrush
                    })
                    .ToList();

                ListStaff.ItemsSource = staff;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка выгрузки: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnCreateStaff_Click(object sender, RoutedEventArgs e)
        {
            string login = TxtStaffLogin.Text.Trim();
            string password = TxtStaffPassword.Text.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                CustomDialogWindow.ShowOk("Заполните логин и пароль сотрудника!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            if (Core.Context.Users.Any(u => u.Login == login))
            {
                CustomDialogWindow.ShowOk("Логин уже занят другим пользователем!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                var newStaff = new Users
                {
                    Login = login,
                    PasswordHash = AuthService.HashPassword(password),
                    FK_Role = 2,
                    FK_StaffStatus = 1
                };

                Core.Context.Users.Add(newStaff);
                Core.Context.SaveChanges();

                CustomDialogWindow.ShowOk($"Менеджер {login} успешно добавлен в систему!", CustomDialogWindow.DialogIcon.Info);
                TxtStaffLogin.Clear();
                TxtStaffPassword.Clear();
                RefreshStaffList();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка сохранения: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            int userId = (int)((Button)sender).Tag;

            try
            {
                var employee = Core.Context.Users.FirstOrDefault(u => u.ID_User == userId);
                if (employee != null)
                {
                    employee.FK_StaffStatus = (employee.FK_StaffStatus == 1) ? 2 : 1;
                    Core.Context.SaveChanges();
                    RefreshStaffList();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка изменения статуса: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }
    }
}
