using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class ManagerOrdersPage : Page
    {
        public ManagerOrdersPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            CarRentalSystem.Services.OrderBackgroundService.CheckAndUpdateExpiredOrders();
            RefreshGrids();
        }

        private void RefreshGrids()
        {
            try
            {
                foreach (var entry in Core.Context.ChangeTracker.Entries())
                {
                    entry.Reload();
                }

                var orders = Core.Context.Orders
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

                        
                        string displayStatus = "-";
                        if (o.OrderStatuses != null)
                        {
                            displayStatus = o.OrderStatuses.StatusName;
                        }
                        else
                        {
                            var dbStatus = Core.Context.OrderStatuses.FirstOrDefault(s => s.ID_OrderStatus == o.FK_OrderStatus);
                            if (dbStatus != null)
                            {
                                displayStatus = dbStatus.StatusName;
                            }
                        }

                        return new
                        {
                            ID_Order = o.ID_Order,
                            ClientName = string.IsNullOrWhiteSpace(clientFio.Trim()) ? "Сотрудник (Система)" : clientFio,
                            CarBrand = o.Cars != null ? o.Cars.Brand : "-",
                            CarModel = o.Cars != null ? o.Cars.Model : "-",
                            StartDate = o.StartDate,
                            EndDate = o.EndDate,
                            TotalCost = o.TotalCost,
                            StatusName = displayStatus, 
                            StatusId = o.FK_OrderStatus
                        };
                    })
                    .OrderByDescending(o => o.ID_Order)
                    .ToList();

                GridNewOrders.ItemsSource = orders.Where(o => o.StatusId == 1).ToList();
                GridCancelOrders.ItemsSource = orders.Where(o => o.StatusId == 7).ToList();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка обновления списков менеджера: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }


        private void TabOrders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PanelNewActions == null || PanelCancelActions == null) return;

            if (TabOrders.SelectedIndex == 0)
            {
                PanelNewActions.Visibility = Visibility.Visible;
                PanelCancelActions.Visibility = Visibility.Collapsed;
            }
            else
            {
                PanelNewActions.Visibility = Visibility.Collapsed;
                PanelCancelActions.Visibility = Visibility.Visible;
            }
        }

        private void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (GridNewOrders.SelectedItem == null) { CustomDialogWindow.ShowOk("Выберите заявку из списка!", CustomDialogWindow.DialogIcon.Warning); return; }

            try
            {
                dynamic selected = GridNewOrders.SelectedItem;
                int orderId = selected.ID_Order;

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);
                if (order != null && order.FK_OrderStatus == 1)
                {
                    order.FK_OrderStatus = 2;
                    order.FK_Manager = Core.CurrentUserId;

                    Core.Context.SaveChanges();
                    CustomDialogWindow.ShowOk("Заявка успешно одобрена, автомобиль передан клиенту.", CustomDialogWindow.DialogIcon.Info);
                    RefreshGrids();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка одобрения: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnReject_Click(object sender, RoutedEventArgs e)
        {
            if (GridNewOrders.SelectedItem == null) return;

            try
            {
                dynamic selected = GridNewOrders.SelectedItem;
                int orderId = selected.ID_Order;

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);
                if (order != null && order.FK_OrderStatus == 1)
                {
                    order.FK_OrderStatus = 5;
                    order.FK_Manager = Core.CurrentUserId;

                    var car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == order.FK_Car);
                    if (car != null) car.FK_Status = 1;

                    Core.Context.SaveChanges();
                    CustomDialogWindow.ShowOk("Заявка отклонена. Автомобиль возвращен в каталог.", CustomDialogWindow.DialogIcon.Info);
                    RefreshGrids();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка отклонения: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnConfirmCancelWithPenalty_Click(object sender, RoutedEventArgs e)
        {
            if (GridCancelOrders.SelectedItem == null) { CustomDialogWindow.ShowOk("Выберите заявку из списка!", CustomDialogWindow.DialogIcon.Warning); return; }

            try
            {
                dynamic selected = GridCancelOrders.SelectedItem;
                int orderId = selected.ID_Order;

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);

                if (order != null && order.FK_OrderStatus == 7)
                {
                    order.FK_OrderStatus = 6;
                    order.FK_Manager = Core.CurrentUserId;

                    decimal companyRevenue = order.TotalCost * 0.05m;
                    order.TotalCost = companyRevenue;

                    var payment = Core.Context.Payments.FirstOrDefault(p => p.FK_Order == order.ID_Order);
                    if (payment != null)
                    {
                        payment.Amount = companyRevenue;
                        payment.FK_PaymentStatus = 1;
                    }

                    if (order.Cars != null) order.Cars.FK_Status = 1;

                    Core.Context.SaveChanges();

                    CustomDialogWindow.ShowOk($"Отмена брони подтверждена!\nКлиенту возвращено 95% стоимости.\nУдержано в пользу компании: {companyRevenue:N2} руб.", CustomDialogWindow.DialogIcon.Info);
                    RefreshGrids();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка транзакции отмены: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnRejectCancelRequest_Click(object sender, RoutedEventArgs e)
        {
            if (GridCancelOrders.SelectedItem == null) return;

            try
            {
                dynamic selected = GridCancelOrders.SelectedItem;
                int orderId = selected.ID_Order;

                var order = Core.Context.Orders.FirstOrDefault(o => o.ID_Order == orderId);
                if (order != null && order.FK_OrderStatus == 7)
                {
                    order.FK_OrderStatus = 2;
                    order.FK_Manager = Core.CurrentUserId;

                    Core.Context.SaveChanges();
                    CustomDialogWindow.ShowOk("В отмене брони отказано. Договор проката остается активным.", CustomDialogWindow.DialogIcon.Info);
                    RefreshGrids();
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка отклонения отмены: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }
    }
}
