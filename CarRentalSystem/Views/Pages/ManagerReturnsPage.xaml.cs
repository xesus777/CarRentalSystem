using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class ManagerReturnsPage : Page
    {
        public ManagerReturnsPage()
        {
            InitializeComponent();
            CarRentalSystem.Services.OrderBackgroundService.CheckAndUpdateExpiredOrders();
            LoadExpiredOrders();
        }

        private void LoadExpiredOrders()
        {
            try
            {
                var now = DateTime.Now;

                var expiredOrders = Core.Context.Orders
                    .Where(o => o.FK_OrderStatus == 2 && o.EndDate <= now)
                    .ToList()
                    .Select(o => {
                        string clientFio = "-";
                        if (o.Clients != null)
                        {
                            clientFio = $"{o.Clients.LastName} {o.Clients.FirstName}";
                        }
                        else
                        {
                            var userAsClient = Core.Context.Users.FirstOrDefault(u => u.ID_User == o.FK_Client);
                            if (userAsClient != null)
                            {
                                clientFio = $"{userAsClient.LastName} {userAsClient.FirstName}";
                            }
                        }

                        return new
                        {
                            ID_Order = o.ID_Order,
                            CarInfo = o.Cars != null ? $"{o.Cars.Brand} {o.Cars.Model}" : "-",
                            LicensePlate = o.Cars != null ? o.Cars.LicensePlate : "-",
                            ClientName = string.IsNullOrWhiteSpace(clientFio.Trim()) ? "Сотрудник (Система)" : clientFio,
                            EndDate = o.EndDate,
                            CarId = o.FK_Car
                        };
                    })
                    .OrderBy(o => o.EndDate)
                    .ToList();

                GridExpiredOrders.ItemsSource = expiredOrders;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка загрузки списка возврата: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void GridExpiredOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dynamic selected = GridExpiredOrders.SelectedItem;
            if (selected == null)
            {
                TxtSelectedCar.Text = "Машина не выбрана";
                return;
            }

            TxtSelectedCar.Text = $"Выбран: {selected.CarInfo} [{selected.LicensePlate}] по заказу №{selected.ID_Order}";
        }

        private void RadioStatus_Changed(object sender, RoutedEventArgs e)
        {
            if (PanelDamageDescription == null) return;

            if (RadioStatusDamaged.IsChecked == true)
            {
                PanelDamageDescription.Visibility = Visibility.Visible;
            }
            else
            {
                PanelDamageDescription.Visibility = Visibility.Collapsed;
                TxtDamageDescription.Clear();
            }
        }

        private void BtnConfirmReturn_Click(object sender, RoutedEventArgs e)
        {
            if (GridExpiredOrders.SelectedItem == null)
            {
                CustomDialogWindow.ShowOk("Выберите автомобиль из списка для оформления возврата!", CustomDialogWindow.DialogIcon.Warning);
                return;
            }

            try
            {
                dynamic selected = GridExpiredOrders.SelectedItem;
                int orderId = selected.ID_Order;
                int carId = selected.CarId;

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);
                var car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == carId);

                if (order != null && car != null)
                {
                    order.FK_OrderStatus = 4;
                    order.FK_Manager = Core.CurrentUserId;

                    if (RadioStatusOk.IsChecked == true)
                    {
                        car.FK_Status = 1;
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(TxtDamageDescription.Text))
                        {
                            CustomDialogWindow.ShowOk("Пожалуйста, заполните описание дефектов или повреждений автомобиля!", CustomDialogWindow.DialogIcon.Warning);
                            return;
                        }

                        car.FK_Status = 3;
                    }

                    Core.Context.SaveChanges();
                    CustomDialogWindow.ShowOk("Возврат автомобиля успешно зафиксирован. База данных обновлена.", CustomDialogWindow.DialogIcon.Info);

                    TxtDamageDescription.Clear();
                    LoadExpiredOrders();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка при фиксации возврата: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }
    }
}
