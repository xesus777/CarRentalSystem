using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class ProfilePage : Page
    {
        private int _userId;
        private bool _isInitialized = false;

        public ProfilePage(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                var user = Core.Context.Users.FirstOrDefault(u => u.ID_User == _userId);
                if (user == null)
                {
                    CustomDialogWindow.ShowOk("Данные пользователя не найдены!", CustomDialogWindow.DialogIcon.Error);
                    return;
                }

                Core.Context.Entry(user).Reload();
                TxtUserLogin.Text = $"Логин: {user.Login}";

                if (user.FK_Role == 1 || user.FK_Role == 2)
                {
                    TxtClientLastName.Text = $"Фамилия: {(!string.IsNullOrEmpty(user.LastName) ? user.LastName : "Администрация")}";
                    TxtClientFirstName.Text = $"Имя: {(!string.IsNullOrEmpty(user.FirstName) ? user.FirstName : "Сотрудник")}";
                    TxtClientMiddleName.Text = $"Отчество: {user.MiddleName ?? "-"}";
                    TxtClientPhone.Text = $"Телефон: {user.Phone ?? "-"}";
                    TxtClientPassport.Text = $"Паспорт: {user.PassportNumber ?? "-"}";
                    TxtClientLicense.Text = $"Вод. удостоверение: {user.DriverLicense ?? "-"}";
                }
                else
                {
                    var client = Core.Context.Clients.FirstOrDefault(c => c.ID_Client == _userId);
                    if (client != null)
                    {
                        Core.Context.Entry(client).Reload();

                        TxtClientLastName.Text = $"Фамилия: {client.LastName}";
                        TxtClientFirstName.Text = $"Имя: {client.FirstName}";
                        TxtClientMiddleName.Text = $"Отчество: {(string.IsNullOrEmpty(client.MiddleName) ? "-" : client.MiddleName)}";
                        TxtClientPhone.Text = $"Телефон: {client.Phone}";
                        TxtClientPassport.Text = $"Паспорт: {client.PassportNumber}";
                        TxtClientLicense.Text = $"Вод. удостоверение: {client.DriverLicense}";
                    }
                }

                var allOrders = Core.Context.Orders.Where(o => o.FK_Client == _userId).ToList();

                GridOrders.ItemsSource = allOrders.Select(o => new
                {
                    ID_Order = o.ID_Order,
                    CarBrand = o.Cars != null ? o.Cars.Brand : "-",
                    CarModel = o.Cars != null ? o.Cars.Model : "-",
                    StartDate = o.StartDate,
                    EndDate = o.EndDate,
                    TotalCost = o.TotalCost,
                    StatusName = o.OrderStatuses != null ? o.OrderStatuses.StatusName : "-"
                }).OrderByDescending(o => o.ID_Order).ToList();

                GridActiveOrders.ItemsSource = allOrders.Where(o => o.FK_OrderStatus == 2).Select(o => {
                    var daysLeft = (o.EndDate.Date - DateTime.Today).Days;
                    string timeLeftText = daysLeft < 0 ? $"Просрочено на {Math.Abs(daysLeft)} дн." : $"{daysLeft} дн.";
                    return new
                    {
                        ID_Order = o.ID_Order,
                        CarInfo = o.Cars != null ? $"{o.Cars.Brand} {o.Cars.Model}" : "-",
                        LicensePlate = o.Cars != null ? o.Cars.LicensePlate : "-",
                        StartDate = o.StartDate,
                        EndDate = o.EndDate,
                        TotalCost = o.TotalCost,
                        TimeLeft = timeLeftText
                    };
                }).OrderBy(o => o.EndDate).ToList();

            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка при загрузке данных профиля: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnCancelOrder_Click(object sender, RoutedEventArgs e)
        {
            if (GridOrders.SelectedItem == null)
            {
                CustomDialogWindow.ShowOk("Выберите заказ из таблицы для отмены!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                dynamic selectedOrder = GridOrders.SelectedItem;
                int orderId = selectedOrder.ID_Order;

                if (!CustomDialogWindow.ShowYesNo($"Вы действительно хотите отменить бронирование №{orderId}?", CustomDialogWindow.DialogIcon.Question))
                {
                    return;
                }

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);
                if (order != null)
                {
                    if (order.FK_OrderStatus == 1)
                    {
                        order.FK_OrderStatus = 6;
                        if (order.Cars != null) order.Cars.FK_Status = 1;

                        Core.Context.SaveChanges();
                        CustomDialogWindow.ShowOk("Заказ успешно отменен.", CustomDialogWindow.DialogIcon.Info);
                    }
                    else if (order.FK_OrderStatus == 2)
                    {
                        order.FK_OrderStatus = 7;

                        Core.Context.SaveChanges();
                        CustomDialogWindow.ShowOk("Заявка на отмену подтвержденного бронирования отправлена менеджеру.\nОбратите внимание: при отмене удерживается комиссия 5%!", CustomDialogWindow.DialogIcon.Warning);
                    }
                    else
                    {
                        CustomDialogWindow.ShowOk("Нельзя отменить заказ с текущим статусом!", CustomDialogWindow.DialogIcon.Error);
                        return;
                    }

                    LoadUserData();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка при отмене: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnGoToSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new SettingsPage(_userId));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainPage(_userId));
        }
    }
}
