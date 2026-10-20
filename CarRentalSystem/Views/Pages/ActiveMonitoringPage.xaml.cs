using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class ActiveMonitoringPage : Page
    {
        public ActiveMonitoringPage()
        {
            InitializeComponent();
            CarRentalSystem.Services.OrderBackgroundService.CheckAndUpdateExpiredOrders();
            LoadGlobalActiveOrders();
        }

        private void LoadGlobalActiveOrders()
        {
            try
            {
                foreach (var entry in Core.Context.ChangeTracker.Entries())
                {
                    entry.Reload();
                }

                var activeList = Core.Context.Orders
                    .Where(o => o.FK_OrderStatus == 2)
                    .ToList()
                    .Select(o => {
                        string clientFio = "-";
                        string clientPhone = "-";

                        if (o.Clients != null)
                        {
                            clientFio = $"{o.Clients.LastName} {o.Clients.FirstName}";
                            clientPhone = o.Clients.Phone;
                        }
                        else
                        {
                            var userCard = Core.Context.Users.FirstOrDefault(u => u.ID_User == o.FK_Client);
                            if (userCard != null)
                            {
                                clientFio = $"{userCard.LastName} {userCard.FirstName}";
                                clientPhone = userCard.Phone;
                            }
                        }

                        int daysLeft = (o.EndDate.Date - DateTime.Today).Days;
                        string timerText = daysLeft < 0 ? $"ПРОСРОЧКА {Math.Abs(daysLeft)} дн." : $"{daysLeft} дн. осталось";

                        return new
                        {
                            ID_Order = o.ID_Order,
                            ClientName = string.IsNullOrWhiteSpace(clientFio.Trim()) ? "Администрация" : clientFio,
                            ClientPhone = clientPhone ?? "-",
                            CarInfo = o.Cars != null ? $"{o.Cars.Brand} {o.Cars.Model}" : "-",
                            LicensePlate = o.Cars != null ? o.Cars.LicensePlate : "-",
                            StartDate = o.StartDate,
                            EndDate = o.EndDate,
                            TimeLeft = timerText
                        };
                    })
                    .OrderBy(o => o.EndDate)
                    .ToList();

                GridGlobalActive.ItemsSource = activeList;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка загрузки глобального мониторинга: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }
    }
}
