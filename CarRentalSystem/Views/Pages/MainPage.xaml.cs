using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class MainPage : Page
    {
        private int _userId;
        private bool _isInitialized = false;

        public MainPage(int userId)
        {
            InitializeComponent();
            _userId = userId;

            if (_userId == 0 && BtnGuestBack != null)
            {
                BtnGuestBack.Visibility = Visibility.Visible;
            }

            CarRentalSystem.Services.OrderBackgroundService.CheckAndUpdateExpiredOrders();
            InitializeData();
        }

        private void InitializeData()
        {
            try
            {
                var categories = Core.Context.CarCategories
                    .Select(c => new
                    {
                        ID_Category = c.ID_Category,
                        CategoryName = c.CategoryName
                    })
                    .ToList()
                    .Select(c => new CarCategories
                    {
                        ID_Category = c.ID_Category,
                        CategoryName = c.CategoryName
                    })
                    .ToList();

                categories.Insert(0, new CarCategories { ID_Category = 0, CategoryName = "Все классы" });

                ComboCategory.ItemsSource = categories;
                ComboCategory.SelectedIndex = 0;

                if (ComboSort != null)
                {
                    ComboSort.SelectedIndex = 0;
                }

                _isInitialized = true;
                RefreshCarsList();
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка загрузки справочников: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isInitialized)
            {
                InitializeData();
            }

            if (_userId == 0 && BtnGuestBack != null)
            {
                BtnGuestBack.Visibility = Visibility.Visible;
            }
        }

        private void RefreshCarsList()
        {
            if (ListCars == null || !_isInitialized) return;

            try
            {
                var query = Core.Context.Cars
                    .Where(c => c.FK_Status == 1)
                    .AsQueryable();

                string searchText = TxtSearch.Text?.Trim().ToLower() ?? "";

                if (!string.IsNullOrEmpty(searchText))
                {
                    query = query.Where(c => c.Brand.ToLower().Contains(searchText) || c.Model.ToLower().Contains(searchText));
                }

                if (ComboCategory != null && ComboCategory.SelectedItem is CarCategories selectedCategory && selectedCategory.ID_Category > 0)
                {
                    query = query.Where(c => c.FK_Category == selectedCategory.ID_Category);
                }

                var result = query.ToList();

                if (ComboSort != null && ComboSort.SelectedIndex > 0)
                {
                    if (ComboSort.SelectedIndex == 1)
                        result = result.OrderBy(c => c.CarCategories.PricePerDay).ToList();
                    else if (ComboSort.SelectedIndex == 2)
                        result = result.OrderByDescending(c => c.CarCategories.PricePerDay).ToList();
                }

                ListCars.ItemsSource = result;
            }
            catch (Exception ex)
            {
                CustomDialogWindow.ShowOk($"Ошибка фильтрации: {ex.Message}", CustomDialogWindow.DialogIcon.Error);
            }
        }

        private void FilterCriteria_Changed(object sender, SelectionChangedEventArgs e) => RefreshCarsList();
        private void FilterCriteria_Changed(object sender, TextChangedEventArgs e) => RefreshCarsList();

        private void BtnViewCar_Click(object sender, RoutedEventArgs e)
        {
            int carId = (int)((Button)sender).Tag;
            NavigationService.Navigate(new CarPage(carId, _userId));
        }

        private void BtnGuestBack_Click(object sender, RoutedEventArgs e)
        {
            Core.CurrentUserId = 0;
            NavigationService.Navigate(new AuthPage());
        }
    }
}
