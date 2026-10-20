using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CarRentalSystem.Views.Windows;

namespace CarRentalSystem.Views.Pages
{
    public partial class CarPage : Page
    {
        private int _carId;
        private int _userId;

        public CarPage(int carId, int userId)
        {
            InitializeComponent();
            _carId = carId;
            _userId = userId;
            LoadCarData();
        }

        private void LoadCarData()
        {
            var car = Core.Context.Cars.FirstOrDefault(c => c.ID_Car == _carId);
            if (car != null)
            {
                TxtCarTitle.Text = $"{car.Brand} {car.Model}";
                TxtCategory.Text = $"Класс: {car.CarCategories.CategoryName}";
                TxtYear.Text = car.ManufactureYear.ToString();
                TxtPlate.Text = car.LicensePlate;
                TxtPrice.Text = $"{car.CarCategories.PricePerDay:C} / сутки";

                if (!string.IsNullOrEmpty(car.CarImagePath) && File.Exists(car.CarImagePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(car.CarImagePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ImgCarPhoto.Source = bitmap;
                }
                else
                {
                    ImgCarPhoto.Source = new BitmapImage(new Uri("pack://application:,,,/Images/no_photo.png"));
                }
            }
        }

        private void BtnRent_Click(object sender, RoutedEventArgs e)
        {
            if (_userId == 0)
            {
                CustomDialogWindow.ShowOk("Вы вошли как Гость. Бронирование автомобилей недоступно. Пожалуйста, пройдите регистрацию!", CustomDialogWindow.DialogIcon.Warning);
                NavigationService.Navigate(new AuthPage());
                return;
            }
            NavigationService.Navigate(new BookingPage(_carId, _userId));
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
