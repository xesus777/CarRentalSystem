using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CarRentalSystem.Services;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class MainPageNavigator : Page
    {
        private int _userRole;

        public MainPageNavigator()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                CarRentalSystem.Services.OrderBackgroundService.CheckAndUpdateExpiredOrders();

                PanelAdminButtons.Visibility = Visibility.Collapsed;
                PanelManagerButtons.Visibility = Visibility.Collapsed;
                PanelClientButtons.Visibility = Visibility.Collapsed;

                if (Core.CurrentUserId == 0)
                {
                    ThemeService.SetTheme(false);
                    TxtRoleTitle.Text = "СЕРВИС АРЕНДЫ";
                    PanelClientButtons.Visibility = Visibility.Visible;

                    if (BtnProfile != null) BtnProfile.Visibility = Visibility.Collapsed;

                    MainNavigatorFrame.Navigate(new MainPage(0));
                    return;
                }

                foreach (var entry in Core.Context.ChangeTracker.Entries())
                {
                    entry.Reload();
                }

                var user = Core.Context.Users.FirstOrDefault(u => u.ID_User == Core.CurrentUserId);
                if (user == null)
                {
                    NavigationService.Navigate(new AuthPage());
                    return;
                }

                ThemeService.SetTheme(user.IsDarkTheme);

                _userRole = user.FK_Role;

                if (_userRole == 1)
                {
                    TxtRoleTitle.Text = "ПАНЕЛЬ АДМИНИСТРАТОРА";
                    PanelAdminButtons.Visibility = Visibility.Visible;
                    MainNavigatorFrame.Navigate(new AdminAnalyticsPage());
                }
                else if (_userRole == 2)
                {
                    TxtRoleTitle.Text = "РАБОЧЕЕ МЕСТО МЕНЕДЖЕРА";
                    PanelManagerButtons.Visibility = Visibility.Visible;
                    MainNavigatorFrame.Navigate(new ManagerOrdersPage());
                }
                else if (_userRole == 3)
                {
                    TxtRoleTitle.Text = "СЕРВИС АРЕНДЫ АВТО";
                    PanelClientButtons.Visibility = Visibility.Visible;

                    if (BtnProfile != null) BtnProfile.Visibility = Visibility.Visible;

                    MainNavigatorFrame.Navigate(new MainPage(Core.CurrentUserId));
                }
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка инициализации навигации: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void BtnAdminAnalytics_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new AdminAnalyticsPage());
        }

        private void BtnAdminCars_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new AdminCarsPage());
        }

        private void BtnAdminStaff_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new AdminStaffPage());
        }

        private void BtnAdminOrders_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new AdminOrdersPage());
        }

        private void BtnManagerOrders_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new ManagerOrdersPage());
        }

        private void BtnManagerReturns_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new ManagerReturnsPage());
        }

        private void BtnActiveMonitor_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new ActiveMonitoringPage());
        }

        private void BtnClientCatalog_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new MainPage(Core.CurrentUserId));
        }

        private void BtnClientProfile_Click(object sender, RoutedEventArgs e)
        {
            MainNavigatorFrame.Navigate(new ProfilePage(Core.CurrentUserId));
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (CustomDialogWindow.ShowYesNo("Вы действительно хотите выйти из учетной записи?", CustomDialogWindow.DialogIcon.Question))
            {
                Core.CurrentUserId = 0;
                ThemeService.SetTheme(false); 
                NavigationService.Navigate(new AuthPage());
            }
        }
    }
}
